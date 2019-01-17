using Mono.Unix.Native;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    class MacOsUncachedFileStream : FileStream
    {
        public MacOsUncachedFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, bool enableMemCache) : base(path, mode, access, share, bufferSize, options)
        {
            if (!enableMemCache) Syscall.fcntl((int)SafeFileHandle.DangerousGetHandle(), FcntlCommand.F_NOCACHE); //diasble cache for already open file stream
            //Syscall.fcntl((int)SafeFileHandle.DangerousGetHandle(), (FcntlCommand)51);
        }
    }
}
