using Mono.Unix.Native;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    class MacOsUncachedFileStream : FileStream
    {
        public bool CachedDisabled { get; private set; } = false;

        public MacOsUncachedFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, bool enableMemCache) : base(path, mode, access, share, bufferSize, options)
        {
            if (!enableMemCache) //diasble cache for already open file stream
            {
                var r = Syscall.fcntl((int)SafeFileHandle.DangerousGetHandle(), FcntlCommand.F_NOCACHE, 1);

                CachedDisabled = true;
            }

        }
    }
}
