using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Trackers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Lumina.Excel.Sheets;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class TeleportCosts : TrackerComponentBase
{
    private delegate void ActorControlSelfDelegate
        (uint category, uint eventId, uint a3, uint a4, uint a5, uint a6, uint a7, uint a8, ulong targetId, byte a10);
    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ActorControlSelfDetour))]
    private Hook<ActorControlSelfDelegate>? ActorControlSelfHook;

    private delegate byte TeleportActionSelfDelegate(long p1, uint p2, byte p3);
    [Signature("E8 ?? ?? ?? ?? 48 8B 4B 10 84 C0 48 8B 01 74 2C ?? ?? ?? ?? ?? ?? ?? ??", DetourName = nameof(TeleportActionSelfDetour))]
    private Hook<TeleportActionSelfDelegate>? TeleportActionSelfHook;

    private static Dictionary<uint, string> AetheryteNames = [];
    private static readonly uint[] TpCostCurrencies = [1, 7569];

    private static string tpDestination = string.Empty; // Aetheryte Name

    private static TaskHelper? TaskHelper;

    protected override void OnInit()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = 60000 };

        DService.Hook.InitializeFromAttributes(this);
        ActorControlSelfHook?.Enable();
        TeleportActionSelfHook?.Enable();

        AetheryteNames = LuminaGetter.Get<Aetheryte>()
                                .Select(row => new
                                {
                                    row.RowId,
                                    Name = P.PI.Sanitizer.Sanitize(
                                        row.PlaceName.ValueNullable?.Name.ToString())
                                })
                                .Where(x => !string.IsNullOrEmpty(x.Name))
                                .ToDictionary(x => x.RowId, x => x.Name);
    }

    private byte TeleportActionSelfDetour(long p1, uint p2, byte p3)
    {
        if (!AetheryteNames.TryGetValue(p2, out tpDestination))
            DService.Log.Warning($"Unknown Aetheryte Name {p2}");

        return TeleportActionSelfHook.Original(p1, p2, p3);
    }

    private void ActorControlSelfDetour(uint category, uint eventId, uint a3, uint a4, uint a5, uint a6, uint a7, uint a8, ulong targetId, byte a10)
    {
        ActorControlSelfHook.Original(category, eventId, a3, a4, a5, a6, a7, a8, targetId,
                                      a10);

        if (eventId == 517 && a3 is 4590 or 4591 && a4 != 0)
        {
            HandlerManager.ChatHandler.IsBlocked = true;
            TaskHelper.Enqueue(GetTeleportType);
        }
    }

    private static bool? GetTeleportType()
    {
        switch (DService.Condition[ConditionFlag.BetweenAreas])
        {
            case true when DService.Condition[ConditionFlag.BetweenAreas51]:
                TaskHelper.Enqueue(() => GetTeleportResult(true));
                break;
            case true:
                TaskHelper.Enqueue(() => GetTeleportResult(false));
                break;
        }

        return true;
    }

    private static bool? GetTeleportResult(bool isBetweenArea)
    {
        if (IsStillOnTeleport()) return false;

        if (isBetweenArea)
        {
            TrackerManager.CheckCurrencies(TpCostCurrencies, PreviousLocationName,
                                            $"({Service.Lang.GetText("TeleportTo", Service.Config.ComponentProp["RecordDesAetheryteName"] ? tpDestination : CurrentLocationName)})");

        }
        else
        {
            TrackerManager.CheckCurrencies(TpCostCurrencies, PreviousLocationName,
                                            Service.Config.ComponentProp["RecordDesAetheryteName"]
                                                ? $"({Service.Lang.GetText("TeleportTo", tpDestination)})"
                                                : $"{Service.Lang.GetText("TeleportWithinArea")}");
        }

        tpDestination = string.Empty;
        HandlerManager.ChatHandler.IsBlocked = false;
        return true;
    }

    private static bool IsStillOnTeleport() => BetweenAreas || OccupiedInEvent;

    protected override void OnUninit()
    {
        ActorControlSelfHook?.Dispose();
        ActorControlSelfHook = null;

        TeleportActionSelfHook?.Dispose();
        TeleportActionSelfHook = null;

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
