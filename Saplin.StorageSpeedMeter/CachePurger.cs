using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Threading;

namespace Saplin.StorageSpeedMeter
{
    public class CachePurger : ICachePurger
    {
        const long blockSize = 16 * 1024 * 1024;
        const long defaultMemCapacity = (long)16 * 1024 * 1024 * 1024;
        const long blocksToWrite = 100; 
        const long fileExtraToUse = blockSize * 100;
        const int purgeTimeMs = 7000;
        FileStream stream;
        long startPosition;
        Func<long> freeMem;
        Func<bool> checkBreakCalled;
        Random rand = new Random();
        string filePath;

        public CachePurger(string filePath, Func<long> freeMem, Func<bool> checkBreakCalled)
        {
            startPosition = 0;
            this.filePath = filePath;
            this.freeMem = freeMem;
            this.checkBreakCalled = checkBreakCalled;

            if (checkBreakCalled == null) throw new ArgumentException("checkBreakCalled can't be null");
        }

        public void Purge()
        {
            if (checkBreakCalled()) return;
            try
            {
                stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                PurgeOnce();
            }
            finally
            {
                if (!checkBreakCalled())
                {
                    //stream.Flush(true);
                    if (!checkBreakCalled()) Thread.Sleep(300);
                }

                stream?.Close();
                File.Delete(filePath);
            }
        }

        public void Release()
        {
            blocks = null;
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration);
            Debug.WriteLine("Blocks released, GC.Collect() called");
        }

        List<byte[]> blocks = null;

        const float freeMemCeilingCoef = 1.0f;//0.87f;
		const long freeMemThreshold = 50 * 1024 * 1024;

        private void PurgeOnce()
        {
            blocks = new List<byte[]>();

            Debug.WriteLine("Cache Purge, creating 3 blocks");

            try
            {
                byte[] block = null;

                    for (var i = 0; i < 3; i++)
                    {
                        block = AllocBlock();
                        if (block != null) blocks.Add(block);
                        else break;
                    }

            }
            catch { }

            Debug.WriteLine("Writign fake file");

            try { 
                stream.Seek(startPosition, SeekOrigin.Begin);
                long fileExtra = 0;
                var blockIndex = 0;

                if (blocks.Count > 0)
                {
                    var r = (byte)rand.Next();
                    var sw = new Stopwatch();
                    sw.Start();
                    for (int i = 0; i < blocksToWrite; i++)
                    {
                        if (checkBreakCalled() || sw.ElapsedMilliseconds > purgeTimeMs) { break; }

                        if (fileExtra >= fileExtraToUse)
                        {
                            stream.Seek(startPosition, SeekOrigin.Begin);
                            fileExtra = 0;
                        }

                        if (blockIndex >= blocks.Count) blockIndex = 0;

                        for (int k = 0; k < blocks[blockIndex].Length; k += 256)
                            blocks[blockIndex][k] = r++;

                        stream.Write(blocks[blockIndex], 0, blocks[blockIndex].Length);

                        fileExtra += blockSize;
                        blockIndex++;
                    }
                    sw.Stop();
                }
            }
            catch{}

            try
            {
                byte[] block = null;

                var memCapacity = freeMem != null ? (long)(freeMem() * freeMemCeilingCoef) // Android can kill process if mem comes to end
                    : (RamDiskUtil.TotalRam == 0 ? defaultMemCapacity : RamDiskUtil.TotalRam);
                var blocksInMemMax = memCapacity / blockSize;

                for (var i = 0; i < blocksInMemMax - 3; i++)
                {
                    if (checkBreakCalled()) return;
                    Debug.WriteLine("AllockBlock: " + i);

                    block = AllocBlock();

                    if (block != null) blocks.Add(block);
                    else break;

                    if (freeMem() < freeMemThreshold) break;

                    // pregressively slow down executuion to give OS memory manager more time to react, e.g. call onMemoryTrim event on Android
                    if ((float)i / blocksInMemMax > 0.7f) Thread.Sleep(1);
                    else if ((float)i / blocksInMemMax > 0.8f) Thread.Sleep(5);
                    else if ((float)i / blocksInMemMax > 0.9f) Thread.Sleep(10);
                }
            }
            catch { }
        }

        public byte[] AllocBlock()
        {
            byte[] block = null;

            try
            {
                block = new byte[blockSize];

                if (checkBreakCalled()) return null;
                Array.Clear(block,0, block.Length);

                var randN = (byte)rand.Next();

                if (checkBreakCalled()) return null;
                for (int i = 0; i < block.Length; i+=128)
                    block[i] = randN++;

            }
            catch 
            { }

            return block;
        }

        public void SetBreackCheckFunc(Func<bool> checkBreakCalled)
        {
            this.checkBreakCalled = checkBreakCalled;
        }
    }
}
