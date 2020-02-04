using Mono.Unix.Native;
using System.IO;
using System.Runtime.InteropServices;

namespace Saplin.StorageSpeedMeter
{
    class PosixUncachedFileStream : FileStream
    {
        public bool CachedDisabled { get; private set; } = false;

        public PosixUncachedFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, bool enableMemCache) : base(path, mode, access, share, bufferSize, options)
        {
            if (!enableMemCache) //diasble cache for already open file stream
            {
                if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                    Syscall.fcntl((int)SafeFileHandle.DangerousGetHandle(), FcntlCommand.F_NOCACHE, 1);

                if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    Syscall.posix_fadvise((int)SafeFileHandle.DangerousGetHandle(), 0, 0, PosixFadviseAdvice.POSIX_FADV_DONTNEED);

                CachedDisabled = true;
            }

        }

        public void EmptyMemCacheAfterWritesIfNeeded()
        {
            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux) && CachedDisabled)
                Syscall.posix_fadvise((int)SafeFileHandle.DangerousGetHandle(), 0, 0, PosixFadviseAdvice.POSIX_FADV_DONTNEED);
        }
    }
}
