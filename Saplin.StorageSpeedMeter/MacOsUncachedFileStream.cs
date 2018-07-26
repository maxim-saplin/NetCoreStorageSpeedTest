using Mono.Unix.Native;
using System.IO;


namespace Saplin.StorageSpeedMeter
{
    class MacOsUncachedFileStream : FileStream
    {
        public MacOsUncachedFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options) : base(path, mode, access, share, bufferSize, options)
        {
            Syscall.fcntl((int)SafeFileHandle.DangerousGetHandle(), FcntlCommand.F_NOCACHE); //diasble cache for alresdy open file stream
        }
    }

}
