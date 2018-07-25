using System;
using System.IO;
using System.Runtime.InteropServices;


namespace Saplin.StorageSpeedMeter
{
    public class TestFile : IDisposable
    {
        bool disposed = false;
        const int buffer = 4 * 1024;
        string path;
        string folderPath;

        public FileStream WriteStream
        {
            get;
        }

        public FileStream ReadStream//FILE_FLAG_NO_BUFFERING attribute breakes unaligned writes, separate read/write hadnles/stream ar needed with different CreateFile attribtes
        {
            get;
        }

        public TestFile(string drivePath)
        {
            path = RamDiskUtil.GetTempFilePath(drivePath);
            folderPath = System.IO.Path.GetDirectoryName(path);

            WriteStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, buffer, FileOptions.WriteThrough);

            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) //mscOS
            {
                ReadStream = new MacOsUncachedFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buffer, FileOptions.None);
            }
            else //Windows
            {
                ReadStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buffer, (FileOptions)0x20000000/*FILE_FLAG_NO_BUFFERING*/);
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                ReadStream.Dispose();
                WriteStream.Dispose();
            }

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