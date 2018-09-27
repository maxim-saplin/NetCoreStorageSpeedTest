using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;

namespace Saplin.StorageSpeedMeter
{
    public class CachePurger : ICachePurger
    {
        const long blockSize = 128 * 1024 * 1024;
        const long defaultMemCapacity = (long)16 * 1024 * 1024 * 1024;
        const long blocksToWrite = 16; //1GB
        FileStream stream;
        long startPosition;
        Func<long> freeMem;

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
                var memCapacity = freeMem != null ? freeMem()
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

                //stream.Seek(startPosition, SeekOrigin.Begin);

                //if (blocks.Count > 0)
                //{
                //    for (int i = 0; i < blocksToWrite; i++)
                //    {
                //        stream.Write(blocks[Math.Min(i, blocks.Count - 1)], 0, blocks[Math.Min(i, blocks.Count - 1)].Length);
                //    }
                //}
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
