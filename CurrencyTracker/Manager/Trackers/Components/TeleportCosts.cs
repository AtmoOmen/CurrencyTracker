using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tasks;
using CurrencyTracker.Manager.Tools;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Lumina.Excel.GeneratedSheets2;

namespace CurrencyTracker.Manager.Trackers.Components;

public class TeleportCosts : ITrackerComponent
{
    public bool Initialized { get; set; }

    private delegate void ActorControlSelfDelegate(uint category, uint eventId, uint a3, uint a4, uint a5, uint a6, uint a7, uint a8, ulong targetId, byte a10);
    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ActorControlSelf))]
    private Hook<ActorControlSelfDelegate>? actorControlSelfHook;

    private delegate byte TeleportActionSelfDelegate(long p1, uint p2, byte p3);
    [Signature("E8 ?? ?? ?? ?? 48 8B 4B 10 84 C0 48 8B 01 74 2C ?? ?? ?? ?? ?? ?? ?? ??",
               DetourName = nameof(TeleportActionSelf))]
    private Hook<TeleportActionSelfDelegate>? teleportActionSelfHook;

    private static Dictionary<uint, string> AetheryteNames = [];
    private static readonly uint[] TpCostCurrencies = [1, 7569];

    private static string tpDestination = string.Empty; // Aetheryte Name

    private static TaskManager? TaskManager;

    public void Init()
    {
        TaskManager ??= new TaskManager { AbortOnTimeout = true, TimeLimitMS = 60000, ShowDebug = false };

        Service.Hook.InitializeFromAttributes(this);
        actorControlSelfHook?.Enable();
        teleportActionSelfHook?.Enable();

        AetheryteNames = Service.DataManager.GetExcelSheet<Aetheryte>()
                                .Select(row => new
                                {
                                    row.RowId,
                                    Name = P.PluginInterface.Sanitizer.Sanitize(
                                        row.PlaceName.Value?.Name?.ToString())
                                })
                                .Where(x => !string.IsNullOrEmpty(x.Name))
                                .ToDictionary(x => x.RowId, x => x.Name);
    }

    private byte TeleportActionSelf(long p1, uint p2, byte p3)
    {
        if (!AetheryteNames.TryGetValue(p2, out tpDestination))
            Service.Log.Warning($"Unknown Aetheryte Name {p2}");

        return teleportActionSelfHook.Original(p1, p2, p3);
    }

    private void ActorControlSelf(uint category, uint eventId, uint a3, uint a4, uint a5, uint a6, uint a7, uint a8, ulong targetId, byte a10)
    {
        actorControlSelfHook.Original(category, eventId, a3, a4, a5, a6, a7, a8, targetId,
                                      a10);

        if (eventId == 517 && a3 is 4590 or 4591 && a4 != 0)
        {
            HandlerManager.ChatHandler.isBlocked = true;
            TaskManager.Enqueue(GetTeleportType);
        }
    }

    private static bool? GetTeleportType()
    {
        switch (Service.Condition[ConditionFlag.BetweenAreas])
        {
            case true when Service.Condition[ConditionFlag.BetweenAreas51]:
                TaskManager.Enqueue(() => GetTeleportResult(true));
                break;
            case true:
                TaskManager.Enqueue(() => GetTeleportResult(false));
                break;
        }

        return true;
    }

    private static bool? GetTeleportResult(bool isBetweenArea)
    {
        if (IsStillOnTeleport()) return false;

        if (isBetweenArea)
        {
            Tracker.CheckCurrencies(TpCostCurrencies, PreviousLocationName,
                                            $"({Service.Lang.GetText("TeleportTo", Service.Config.ComponentProp["RecordDesAetheryteName"] ? tpDestination : CurrentLocationName)})");

        }
        else
        {
            Tracker.CheckCurrencies(TpCostCurrencies, PreviousLocationName,
                                            Service.Config.ComponentProp["RecordDesAetheryteName"]
                                                ? $"({Service.Lang.GetText("TeleportTo", tpDestination)})"
                                                : $"{Service.Lang.GetText("TeleportWithinArea")}");
        }

        tpDestination = string.Empty;
        HandlerManager.ChatHandler.isBlocked = false;
        return true;
    }

    private static bool IsStillOnTeleport() => Flags.BetweenAreas() || Flags.OccupiedInEvent();

    public void Uninit()
    {
        TaskManager?.Abort();
        actorControlSelfHook?.Dispose();
        teleportActionSelfHook?.Dispose();
    }
}
