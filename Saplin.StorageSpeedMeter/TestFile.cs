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

        public bool ReadOnly //FILE_FLAG_NO_BUFFERING attribute breakes unaligned writes, separate read/write hadnles ar needed with different attribtes
        {
            get; protected set;
        }

        public TestFile(string drivePath, bool read = false)
        {
            path = RamDiskUtil.GetTempFilePath(drivePath);
            folderPath = System.IO.Path.GetDirectoryName(path);

            Stream = (ReadOnly = read) ? 
                new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buffer, FileOptions.WriteThrough):// (FileOptions)0x20000000/*FILE_FLAG_NO_BUFFERING*/) :
                new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, buffer, FileOptions.WriteThrough);
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

            if (!ReadOnly) System.IO.File.Delete(path);

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