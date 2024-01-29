using Lumina.Excel.GeneratedSheets2;

namespace CurrencyTracker.Manager.Trackers.Components;

public class TeleportCosts : ITrackerComponent
{
    public bool Initialized { get; set; }

    private const string ActorControlSig = "E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64";

    private delegate void ActorControlSelfDelegate(
        uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6,
        ulong targetId, byte param7);

    private Hook<ActorControlSelfDelegate>? actorControlSelfHook;

    private const string TeleportActionSig = "E8 ?? ?? ?? ?? 48 8B 4B 10 84 C0 48 8B 01 74 2C ?? ?? ?? ?? ?? ?? ?? ??";

    private delegate byte TeleportActionSelfDelegate(long p1, uint p2, byte p3);

    private Hook<TeleportActionSelfDelegate>? teleportActionSelfHook;

    private static Dictionary<uint, string> AetheryteNames = new();

    private static readonly uint[] TpCostCurrencies = { 1, 7569 };

    private bool isReadyTP;
    private bool tpBetweenAreas;
    private bool tpInAreas;
    private string tpDestination = string.Empty; // Aetheryte Name

    public void Init()
    {
        GetAetherytes();

        var actorControlSelfPtr = Service.SigScanner.ScanText(ActorControlSig);
        actorControlSelfHook ??=
            Service.Hook.HookFromAddress<ActorControlSelfDelegate>(actorControlSelfPtr, ActorControlSelf);
        actorControlSelfHook?.Enable();

        var teleportActionSelfPtr = Service.SigScanner.ScanText(TeleportActionSig);
        teleportActionSelfHook ??=
            Service.Hook.HookFromAddress<TeleportActionSelfDelegate>(teleportActionSelfPtr, TeleportActionSelf);
        teleportActionSelfHook?.Enable();
    }

    private static void GetAetherytes()
    {
        var sheet = Service.DataManager.GetExcelSheet<Aetheryte>()!;
        AetheryteNames.Clear();
        AetheryteNames = sheet
                         .Select(row => new
                         {
                             row.RowId,
                             Name = Plugin.Instance.PluginInterface.Sanitizer.Sanitize(
                                 row.PlaceName.Value?.Name?.ToString())
                         })
                         .Where(x => !x.Name.IsNullOrEmpty())
                         .ToDictionary(x => x.RowId, x => x.Name);
    }

    private byte TeleportActionSelf(long p1, uint p2, byte p3)
    {
        try
        {
            if (!AetheryteNames.TryGetValue(p2, out tpDestination))
                Service.Log.Warning($"Unknown Aetheryte Name {tpDestination}");
        }
        catch (Exception e)
        {
            Service.Log.Warning(e.Message);
            Service.Log.Warning(e.StackTrace ?? "Unknown");
        }

        return teleportActionSelfHook.OriginalDisposeSafe(p1, p2, p3);
    }

    private void ActorControlSelf(
        uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6,
        ulong targetId, byte param7)
    {
        actorControlSelfHook.Original(category, eventId, param1, param2, param3, param4, param5, param6, targetId,
                                      param7);

        if (eventId != 517)
            return;

        try
        {
            if (param1 is 4590 or 4591 && param2 != 0) TeleportWithCost();
        }
        catch (Exception e)
        {
            Service.Log.Warning(e.Message);
            Service.Log.Warning(e.StackTrace ?? "Unknown");
        }
    }

    public void TeleportWithCost()
    {
        HandlerManager.ChatHandler.isBlocked = true;

        isReadyTP = true;

        Service.Framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!isReadyTP)
        {
            Service.Framework.Update -= OnFrameworkUpdate;
            return;
        }

        switch (Service.Condition[ConditionFlag.BetweenAreas])
        {
            case true when Service.Condition[ConditionFlag.BetweenAreas51]:
                tpBetweenAreas = true;
                break;
            case true:
                tpInAreas = true;
                break;
        }

        if (Flags.BetweenAreas() || Flags.OccupiedInEvent()) return;

        if (tpBetweenAreas)
            Service.Tracker.CheckCurrencies(TpCostCurrencies, PreviousLocationName,
                                            $"({Service.Lang.GetText("TeleportTo", Plugin.Configuration.ComponentProp["RecordDesAetheryteName"] ? tpDestination : CurrentLocationName)})");
        else if (tpInAreas)
            Service.Tracker.CheckCurrencies(TpCostCurrencies, PreviousLocationName,
                                            Plugin.Configuration.ComponentProp["RecordDesAetheryteName"]
                                                ? $"({Service.Lang.GetText("TeleportTo", tpDestination)})"
                                                : $"{Service.Lang.GetText("TeleportWithinArea")}");

        if (!Flags.BetweenAreas() && !Flags.OccupiedInEvent())
        {
            ResetStates();
            HandlerManager.ChatHandler.isBlocked = false;
        }
    }

    private void ResetStates()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        isReadyTP = tpBetweenAreas = tpInAreas = false;
        tpDestination = string.Empty;
    }

    public void Uninit()
    {
        ResetStates();
        actorControlSelfHook?.Dispose();
        teleportActionSelfHook?.Dispose();
    }
}
