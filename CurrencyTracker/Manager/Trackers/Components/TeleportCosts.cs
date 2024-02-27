using Dalamud.Utility.Signatures;
using Lumina.Excel.GeneratedSheets2;

namespace CurrencyTracker.Manager.Trackers.Components;

public class TeleportCosts : ITrackerComponent
{
    public bool Initialized { get; set; }

    private delegate void ActorControlSelfDelegate(
        uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6,
        ulong targetId, byte param7);
    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ActorControlSelf))]
    private Hook<ActorControlSelfDelegate>? actorControlSelfHook;

    private delegate byte TeleportActionSelfDelegate(long p1, uint p2, byte p3);
    [Signature("E8 ?? ?? ?? ?? 48 8B 4B 10 84 C0 48 8B 01 74 2C ?? ?? ?? ?? ?? ?? ?? ??",
               DetourName = nameof(TeleportActionSelf))]
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

        Service.Framework.Update += OnUpdate;
        Service.Hook.InitializeFromAttributes(this);
        actorControlSelfHook?.Enable();
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
                             Name = P.PluginInterface.Sanitizer.Sanitize(
                                 row.PlaceName.Value?.Name?.ToString())
                         })
                         .Where(x => !x.Name.IsNullOrEmpty())
                         .ToDictionary(x => x.RowId, x => x.Name);
    }

    private byte TeleportActionSelf(long p1, uint p2, byte p3)
    {
        if (!AetheryteNames.TryGetValue(p2, out tpDestination))
            Service.Log.Warning($"Unknown Aetheryte Name {tpDestination}");

        return teleportActionSelfHook.Original(p1, p2, p3);
    }

    private void ActorControlSelf(
        uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6,
        ulong targetId, byte param7)
    {
        actorControlSelfHook.Original(category, eventId, param1, param2, param3, param4, param5, param6, targetId,
                                      param7);

        if (eventId == 517 && param1 is 4590 or 4591 && param2 != 0)
        {
            HandlerManager.ChatHandler.isBlocked = true;
            isReadyTP = true;
        }
    }

    private void OnUpdate(IFramework framework)
    {
        if (!isReadyTP) return;

        switch (Service.Condition[ConditionFlag.BetweenAreas])
        {
            case true when Service.Condition[ConditionFlag.BetweenAreas51]:
                tpBetweenAreas = true;
                break;
            case true:
                tpInAreas = true;
                break;
        }

        if (IsStillOnTeleport()) return;

        if (tpBetweenAreas)
        {
            Service.Tracker.CheckCurrencies(TpCostCurrencies, PreviousLocationName,
                                            $"({Service.Lang.GetText("TeleportTo", Service.Config.ComponentProp["RecordDesAetheryteName"] ? tpDestination : CurrentLocationName)})");
        }
        else if (tpInAreas)
        {
            Service.Tracker.CheckCurrencies(TpCostCurrencies, PreviousLocationName,
                                            Service.Config.ComponentProp["RecordDesAetheryteName"]
                                                ? $"({Service.Lang.GetText("TeleportTo", tpDestination)})"
                                                : $"{Service.Lang.GetText("TeleportWithinArea")}");
        }

        if (!IsStillOnTeleport())
        {
            ResetStates();
            HandlerManager.ChatHandler.isBlocked = false;
        }
    }

    private void ResetStates()
    {
        isReadyTP = tpBetweenAreas = tpInAreas = false;
        tpDestination = string.Empty;
    }

    private static bool IsStillOnTeleport() => Flags.BetweenAreas() || Flags.OccupiedInEvent();

    public void Uninit()
    {
        ResetStates();

        Service.Framework.Update -= OnUpdate;
        actorControlSelfHook?.Dispose();
        teleportActionSelfHook?.Dispose();
    }
}
