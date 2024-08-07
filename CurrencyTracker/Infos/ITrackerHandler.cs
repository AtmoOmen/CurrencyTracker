namespace CurrencyTracker.Infos;

public interface ITrackerHandler
{
    void Init();

    void Uninit();

    bool isBlocked { get; set; }
    bool Initialized { get; set; }
}
