using System;
using System.IO;
using System.Threading;

namespace Saplin.StorageSpeedMeter
{
    class MockFileStream : FileStream
    {
        Random random = new Random();

        public MockFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options) : base(path, mode, access, share, bufferSize, options)
        {
        }

        public override void Write(byte[] array, int offset, int count)
        {
            if (count < 5000) // RandomTests with small blocks
            {
                //Thread.Sleep(1);
                var arr = new int[150+random.Next(1, 5)* random.Next(1, 5) * random.Next(1, 5)];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = random.Next();

                arr = null;
            }

            else Thread.Sleep(random.Next(7, 20));
        }

        public override int Read(byte[] array, int offset, int count)
        {
            Write(array, offset, count);

            return 0;
        }

        public override long Length => 1024*1024*1024;

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }
    }
}
