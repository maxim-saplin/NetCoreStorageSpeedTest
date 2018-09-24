using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;

namespace Saplin.StorageSpeedMeter
{
    public class CachePurger : ICachePurger
    {
        const long blockSize = 128 * 1024 * 1024;
        const long blocksToWrite = 8; //1GB
        FileStream stream;
        long startPosition;

        public CachePurger(TestFile file)
        {
            stream = file.ServiceStream;
            startPosition = file.TestAreaSizeBytes;
        }

        public void Purge()
        {
            var blocks = new List<byte[]>();
            try
            {
                var memCapacity = RamDiskUtil.TotalRam;
                var blocksInMemMax = memCapacity / blockSize;
                byte[] block = null;

                for (int i = 0; i < blocksInMemMax; i++)
                {
                    block = AllocBlock();

                    if (block != null) blocks.Add(block);
                    else break;
                }

                stream.Seek(startPosition, SeekOrigin.Begin);

                if (blocks.Count > 0)
                {
                    for (int i = 0; i < blocksToWrite; i++)
                    {
                        stream.Write(blocks[Math.Min(i, blocks.Count - 1)], 0, blocks[Math.Min(i, blocks.Count - 1)].Length);
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
            MemoryFailPoint memFailPoint = null;

            try
            {
                memFailPoint = new MemoryFailPoint((int)(blockSize / 1024 / 1024));
                block = new byte[blockSize];
                block[0] = 1;
                block[block.Length - 1] = 1;
            }
            catch { }
            finally
            {
                memFailPoint?.Dispose();
            }

            return block;
        }
    }
}
