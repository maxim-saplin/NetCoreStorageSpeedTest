using System;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class TestFile : IDisposable
    {
        bool disposed = false;
        const int buffer = 4 * 1024;
        string path;
        string folderPath;

        public FileStream Stream
        {
            get;
        }

        public TestFile(string drivePath)
        {
            path = RamDiskUtil.GetTempFilePath(drivePath);
            folderPath = System.IO.Path.GetDirectoryName(path);

            Stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, buffer, FileOptions.WriteThrough);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing) Stream.Dispose();

            System.IO.File.Delete(path);

            disposed = true;
        }

        public string Path
        {
            get
            {
                return path;
            }
        }


        public string FolderPath
        {
            get
            {
                return folderPath;
            }
        }
    }
}