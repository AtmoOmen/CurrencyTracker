using Lumina.Excel.GeneratedSheets2;

namespace CurrencyTracker.Manager.Trackers.Components;

public class DutyRewards : ITrackerComponent
{
    public bool Initialized { get; set; }

    private static Dictionary<uint, string> ContentNames = new(); // Territory ID - ContentName

    private static readonly uint[] IgnoredContents =
    {
        // Triple Triad Related
        579, 940, 941,
        // Eureka
        732, 763, 795, 827,
        // Bozja
        920, 975
    };

    private bool isDutyStarted;
    private string contentName = string.Empty;

    private InventoryHandler? inventoryHandler;

    public void Init()
    {
        ContentNames = Service.DataManager.GetExcelSheet<ContentFinderCondition>()
                              .Where(x => !x.Name.ToString().IsNullOrEmpty() && IgnoredContents.All(y => y != x.TerritoryType.Row))
                              .DistinctBy(x => x.TerritoryType.Row)
                              .ToDictionary(x => x.TerritoryType.Row, x => x.Name.ToString());

        if (Flags.IsBoundByDuty()) CheckDutyStart();

        Service.ClientState.TerritoryChanged += OnZoneChange;

        Initialized = true;
    }

    private void CheckDutyStart()
    {
        if (isDutyStarted) return;

        if (ContentNames.TryGetValue(CurrentLocationID, out var dutyName))
        {
            isDutyStarted = true;
            contentName = dutyName;
            HandlerManager.ChatHandler.isBlocked = true;
            inventoryHandler = new InventoryHandler();

            Service.Log.Debug($"Duty {dutyName} Starts");
        }
    }

    private void OnZoneChange(ushort obj)
    {
        if (Flags.IsBoundByDuty())
            CheckDutyStart();
        else if (isDutyStarted) CheckDutyEnd();
    }

    private void CheckDutyEnd()
    {
        if (!isDutyStarted) return;

        Service.Log.Debug($"Duty {contentName} Ends, Currency Change Check Starts.");

        var items = inventoryHandler?.Items ?? new HashSet<uint>();
        Service.Tracker.CheckCurrencies(items, PreviousLocationName,
                                        Plugin.Configuration.ComponentProp["RecordContentName"]
                                            ? $"({contentName})"
                                            : "", RecordChangeType.All, 2);

        isDutyStarted = false;
        contentName = string.Empty;
        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);

        Service.Log.Debug("Currency Change Check Completes.");
    }

    public void Uninit()
    {
        Service.ClientState.TerritoryChanged -= OnZoneChange;
        HandlerManager.Nullify(ref inventoryHandler);
        isDutyStarted = false;
        contentName = string.Empty;

        Initialized = false;
    }
}
