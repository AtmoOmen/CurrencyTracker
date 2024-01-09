namespace CurrencyTracker.Manager.Infos;

public interface ITrackerComponent
{
    void Init();

    void Uninit();

    bool Initialized { get; set; }
}
