using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;

namespace Saplin.StorageSpeedMeter
{
    public class CachePurger : ICachePurger
    {
        const long blockSize = 64 * 1024 * 1024;
        const long defaultMemCapacity = (long)16 * 1024 * 1024 * 1024;
        const long blocksToWrite = 16; //1GB
        const long fileExtraToUse = 256 * 1024 * 1024;
        FileStream stream;
        long startPosition;
        Func<long> freeMem;
        Random rand = new Random();

        public CachePurger(TestFile file, Func<long> freeMem)
        {
            stream = file.ServiceStream;
            startPosition = file.TestAreaSizeBytes;
            this.freeMem = freeMem;
        }

        public void Purge()
        {
            PurgeOnce();
            GC.Collect(2, GCCollectionMode.Forced, true);
            //PurgeOnce();
        }

        private void PurgeOnce()
        {
            var blocks = new List<byte[]>();
            try
            {
                var memCapacity = freeMem != null ? freeMem()*8/10 // Android can kill process if mem comes to end
                    : (RamDiskUtil.TotalRam == 0 ? defaultMemCapacity : RamDiskUtil.TotalRam);
                var blocksInMemMax = memCapacity / blockSize;
                byte[] block = null;

                for (int i = 0; i < blocksInMemMax; i++)
                {
                    Debug.WriteLine("AllockBlock: " + i);
                    block = AllocBlock();

                    if (block != null) blocks.Add(block);
                    else break;
                }

                stream.Seek(startPosition, SeekOrigin.Begin);
                var fileExtra = 0;
                var blockIndex = 0;

                if (blocks.Count > 0) blocks.RemoveAt(blocks.Count - 1); // JIC remove few blocks and let GC free up mem if needed
                if (blocks.Count > 0) blocks.RemoveAt(blocks.Count - 1);
                if (blocks.Count > 0) blocks.RemoveAt(blocks.Count - 1);

                if (blocks.Count > 0)
                {
                    for (int i = 0; i < blocksToWrite; i++)
                    {
                        if (fileExtra >= fileExtraToUse)
                        {
                            stream.Seek(startPosition, SeekOrigin.Begin);
                            fileExtra = 0;
                        }

                        if (blockIndex >= blocks.Count) blockIndex = 0;

                        for (int k = 0; k < block.Length; k += 64)
                            block[k] = (byte)rand.Next();

                        stream.Write(blocks[blockIndex], 0, blocks[blockIndex].Length);
                    }
                }
            }
            finally
            {
                blocks = null;
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

                Array.Clear(block,0, block.Length);

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
