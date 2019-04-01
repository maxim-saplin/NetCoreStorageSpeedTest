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
        const long blocksToWrite = 16; //1GB
        const long fileExtraToUse = 256 * 1024 * 1024;
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
                    stream.Flush(true);
                    if (!checkBreakCalled()) Thread.Sleep(300);
                };

                stream?.Close();
                File.Delete(filePath);
            }
        }

        public void Release()
        {
            blocks = null;
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            Debug.WriteLine("Blocks release, GC.Collect called");
        }

        List<byte[]> blocks = null;

        private void PurgeOnce()
        {
            blocks = new List<byte[]>();
            try
            {
                var memCapacity = freeMem != null ? freeMem() * 87 / 100 // Android can kill process if mem comes to end
                    : (RamDiskUtil.TotalRam == 0 ? defaultMemCapacity : RamDiskUtil.TotalRam);
                var blocksInMemMax = memCapacity / blockSize;
                byte[] block = null;

                for (int i = 0; i < blocksInMemMax; i++)
                {
                    if (checkBreakCalled()) return;
                    Debug.WriteLine("AllockBlock: " + i);
                    block = AllocBlock();

                    if (block != null) blocks.Add(block);
                    else break;
                }

                stream.Seek(startPosition, SeekOrigin.Begin);
                long fileExtra = 0;
                var blockIndex = 0;

                if (blocks.Count > 0) blocks.RemoveAt(blocks.Count - 1); // JIC remove few blocks and let GC free up mem if needed
                if (blocks.Count > 0) blocks.RemoveAt(blocks.Count - 1);
                if (blocks.Count > 0) blocks.RemoveAt(blocks.Count - 1);

                if (blocks.Count > 0)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    for (int i = 0; i < blocksToWrite; i++)
                    {
                        if (checkBreakCalled() || sw.ElapsedMilliseconds > purgeTimeMs) { return; }

                        if (fileExtra >= fileExtraToUse)
                        {
                            stream.Seek(startPosition + blockSize, SeekOrigin.Begin);
                            fileExtra = 0;
                        }

                        if (blockIndex >= blocks.Count) blockIndex = 0;

                        for (int k = 0; k < block.Length; k += 64)
                            blocks[blockIndex][k] = (byte)rand.Next();

                        stream.Write(blocks[blockIndex], 0, blocks[blockIndex].Length);

                        fileExtra += blockSize;
                        blockIndex++;
                    }
                    sw.Stop();
                }
            }
            catch{}
            finally
            {
                //blocks = null;
            }
        }

        public byte[] AllocBlock()
        {
            byte[] block = null;
            //MemoryFailPoint memFailPoint = null;

            try
            {
                //memFailPoint = new MemoryFailPoint((int)(blockSize / 1024 / 1024));
                block = new byte[blockSize];

                if (checkBreakCalled()) return null;
                Array.Clear(block,0, block.Length);

                if (checkBreakCalled()) return null;
                for (int i = 0; i < block.Length; i+=64)
                    block[i] = (byte)rand.Next();

            }
            catch 
            { }
            finally
            {
                //memFailPoint?.Dispose();
            }

            return block;
        }
    }
}
