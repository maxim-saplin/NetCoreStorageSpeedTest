namespace Saplin.StorageSpeedMeter
{
    public interface ICachePurger
    {
        void Purge();
        void Release();
    }
}
