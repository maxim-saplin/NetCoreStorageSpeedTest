using System;

namespace Saplin.StorageSpeedMeter
{
    public interface ICachePurger
    {
        void Purge();
        void Release();
        void SetBreackCheckFunc(Func<bool> checkBreakCalled);
    }
}
