using System;
using System.Collections.Generic;
using System.Text;

namespace Saplin.StorageSpeedMeter
{
    public class WriteBufferFlusher
    {
        public Action<string> OpenFile { get; private set;}
        public Action Flush { get; private set;}
        public Action CloseFile { get; private set; }

        public WriteBufferFlusher(Action<string> openFileByName, Action flush, Action closeFile)
        {
            OpenFile = openFileByName;
            Flush = flush;
            CloseFile = closeFile;
        }
    }
}
