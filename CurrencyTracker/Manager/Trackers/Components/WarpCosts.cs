using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class WarpCosts : ITrackerComponent
{
    public bool Initialized { get; set; }

    // 有效的 NPC 传送对话内容 Valid Content Shown in Addon
    private static readonly List<string> ValidWarpText = [];
    private static readonly uint[] tpCostCurrencies = [1, 7569];

    // 包含金币传送点的区域 Territories that Have a Gil-Cost Warp
    private HashSet<uint> ValidGilWarpTerritories = [];

    private delegate nint AddonReceiveEventDelegate(
        AtkEventListener* self, AtkEventType eventType, uint eventParam, AtkEvent* eventData, ulong* inputData);
    private Hook<AddonReceiveEventDelegate>? SelectYesHook;

    private static TaskHelper? TaskManager;

    public void Init()
    {
        TaskManager ??= new TaskHelper { TimeLimitMS = 60000 };

        ValidGilWarpTerritories = LuminaGetter.Get<Warp>()
                                        .Where(x => LuminaGetter.Get<WarpCondition>()
                                                                .Any(y => y.Gil != 0 && x.WarpCondition.Value.RowId == y.RowId))
                                        .Select(x => x.TerritoryType.Value.RowId)
                                        .ToHashSet();

        ValidWarpText.Clear();
        ValidWarpText.Add(LuminaWrapper.GetItemName(1));

        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup,   "SelectYesno", WarpConfirmationCheck);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectYesno", WarpConfirmationCheck);
    }

    private void WarpConfirmationCheck(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostSetup:
                var addon = (AddonSelectYesno*)SelectYesno;
                var address = (nint)addon->YesButton->AtkComponentBase.AtkEventListener.VirtualTable[2].ReceiveEvent;
                SelectYesHook ??= DService.Hook.HookFromAddress<AddonReceiveEventDelegate>(address, SelectYesDetour);
                SelectYesHook?.Enable();
                break;
            case AddonEvent.PreFinalize:
                SelectYesHook?.Dispose();
                SelectYesHook = null;
                break;
        }
    }

    private nint SelectYesDetour(AtkEventListener* self, AtkEventType eventType, uint eventParam, AtkEvent* eventData, ulong* inputData)
    {
        if (eventType == AtkEventType.MouseClick)
        {
            if (!ValidGilWarpTerritories.Contains(GameState.TerritoryType) || !IsAddonAndNodesReady(SelectYesno)) 
                return SelectYesHook.Original(self, eventType, eventParam, eventData, inputData);

            var addon = (AddonSelectYesno*)SelectYesno;
            var text = addon->PromptText->NodeText.ExtractText();
            if (string.IsNullOrEmpty(text)) return SelectYesHook.Original(self, eventType, eventParam, eventData, inputData);

            if (ValidWarpText.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                HandlerManager.ChatHandler.isBlocked = true;
                TaskManager.Enqueue(GetTeleportType);
            }
        }

        return SelectYesHook.Original(self, eventType, eventParam, eventData, inputData);
    }

    private static bool? GetTeleportType()
    {
        switch (DService.Condition[ConditionFlag.BetweenAreas])
        {
            case true when DService.Condition[ConditionFlag.BetweenAreas51]:
                TaskManager.Enqueue(() => GetTeleportResult(true));
                break;
            case true:
                TaskManager.Enqueue(() => GetTeleportResult(false));
                break;
            default:
                return false;
        }

        return true;
    }

    private static bool? GetTeleportResult(bool isBetweenArea)
    {
        if (IsStillOnTeleport()) return false;

        if (isBetweenArea)
        {
            Tracker.CheckCurrencies(tpCostCurrencies, PreviousLocationName,
                                            $"({Service.Lang.GetText("TeleportTo", CurrentLocationName)})",
                                            RecordChangeType.Negative, 15);
        }
        else
        {
            Tracker.CheckCurrencies(tpCostCurrencies, CurrentLocationName,
                                            $"({Service.Lang.GetText("TeleportWithinArea")})",
                                            RecordChangeType.Negative, 16);
        }

        HandlerManager.ChatHandler.isBlocked = false;
        return true;
    }

    private static bool IsStillOnTeleport() => 
        BetweenAreas || OccupiedInEvent;

    public void Uninit()
    {
        DService.AddonLifecycle.UnregisterListener(WarpConfirmationCheck);

        SelectYesHook?.Dispose();
        SelectYesHook = null;

        TaskManager?.Abort();
        TaskManager = null;
    }
}
