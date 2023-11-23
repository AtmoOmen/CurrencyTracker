namespace CurrencyTracker.Manager.Libs
{
    public interface ITrackerComponent
    {
        void Init();

        void Uninit();

        bool Initialized { get; set; }
    }
}
