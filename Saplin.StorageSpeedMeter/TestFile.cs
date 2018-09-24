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

        protected internal bool writeBuffering, enableMemCache;

        public FileStream WriteStream
        {
            get;
        }

        public FileStream ReadStream//FILE_FLAG_NO_BUFFERING attribute breakes unaligned writes, separate read/write hadnles/stream are needed with different CreateFile attribtes
        {
            get;
        }

        public FileStream ServiceStream
        {
            get;
        }

        public long TestAreaSizeBytes
        {
            get;
        }

        /// <summary>
        /// Opens write streams to test file and prepares the stream for tests (e.g. disabling OS file cache, disabling device's write buffers)
        /// </summary>
        /// <param name="drivePath">Drive to test and store the temp file, the contructor attempts to find user folder in case system drive is selected and writing to rout is resricted</param>
        /// <param name="writeBuffering">If set to <c>true</c> FileOptions.WriteThrough is used when creating System.IO.FileStream - whether to use write buffering or not</param>
        public TestFile(string drivePath, long testAreaSizeBytes, bool writeBuffering = false, bool enableMemCache = false, string filePath = null)
        {
            path = string.IsNullOrEmpty(filePath) ? RamDiskUtil.GetTempFilePath(drivePath) : filePath;
            folderPath = System.IO.Path.GetDirectoryName(path);

            TestAreaSizeBytes = testAreaSizeBytes;

            this.writeBuffering = writeBuffering;
            this.enableMemCache = enableMemCache;

            // FileOptions.WriteThrough doesn;t seem to give consistent behaviour across platforms
            // FileStram.Flush(true) is used instead in tests

            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) //macOS
            {
                WriteStream = new MacOsUncachedFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, buffer, 
                    /*!writeBuffering ? FileOptions.WriteThrough : */FileOptions.None,
                    enableMemCache);
                ReadStream = new MacOsUncachedFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buffer, FileOptions.None, enableMemCache);
            }
            else //Windows and rest
            {
                WriteStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, buffer, /*!writeBuffering ? FileOptions.WriteThrough :*/ FileOptions.None);
                ReadStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buffer, enableMemCache ? FileOptions.None : (FileOptions)0x20000000/*FILE_FLAG_NO_BUFFERING*/);
            }

            ServiceStream = WriteStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
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

        ~TestFile()
        {
            Dispose(false);
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