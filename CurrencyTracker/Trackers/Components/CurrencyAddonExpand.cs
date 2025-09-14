using System.Linq;
using System.Text;
using CurrencyTracker.Infos;
using CurrencyTracker.Trackers;
using CurrencyTracker.Windows;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Events.EventDataTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class CurrencyAddonExpand : TrackerComponentBase
{

    private static long? CurrencyAmountCache;
    private static IAddonEventHandle? mouseoverHandle;
    private static IAddonEventHandle? mouseoutHandle;

    private const string AddonName = "Currency";
    private const int CurrencyNodeId = 12;
    private const int GilTextNodeId = 5;
    private const NodeFlags NodeFlagsMask = NodeFlags.EmitsEvents | NodeFlags.RespondToMouse | NodeFlags.HasCollision;

    protected override void OnInit()
    {
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, AddonName, OnCurrencyUI);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, AddonName, OnCurrencyUI);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, AddonName, OnCurrencyUI);
    }

    private static void OnCurrencyUI(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostSetup:
            case AddonEvent.PreDraw:
                CurrencyAmountCache ??= CurrencyInfo.GetCharacterCurrencyAmount(1, P.CurrentCharacter);
                ProcessCurrencyNode(true);
                break;
            case AddonEvent.PreFinalize:
                ProcessCurrencyNode(false);
                CurrencyAmountCache = null;
                break;
        }
    }

    private static void ProcessCurrencyNode(bool isAdd)
    {
        if (!isAdd && mouseoverHandle != null && mouseoutHandle != null)
        {
            DService.AddonEvent.RemoveEvent(mouseoverHandle);
            mouseoverHandle = null;
            DService.AddonEvent.RemoveEvent(mouseoutHandle);
            mouseoutHandle = null;
        }

        if (Throttler.Throttle("CurrencyAddonExpand", 1000))
            CurrencyAmountCache = CurrencyInfo.GetCharacterCurrencyAmount(1, P.CurrentCharacter);

        if (!TryGetAddonByName<AtkUnitBase>(AddonName, out var addon)) return;

        var componentNode = addon->GetNodeById(CurrencyNodeId)->GetAsAtkComponentNode();
        if (componentNode == null) return;

        var gilNode = componentNode->Component->GetTextNodeById(GilTextNodeId)->GetAsAtkTextNode();
        if (gilNode == null) return;

        if (isAdd)
        {
            gilNode->AtkResNode.NodeFlags |= NodeFlagsMask;

            mouseoverHandle ??= DService.AddonEvent.AddEvent((nint)addon, (nint)gilNode, AddonEventType.MouseOver, DisplayAndHideTooltip);
            mouseoutHandle ??= DService.AddonEvent.AddEvent((nint)addon, (nint)gilNode, AddonEventType.MouseOut, DisplayAndHideTooltip);

            if (CurrencyAmountCache != null)
                gilNode->SetText(((long)CurrencyAmountCache).ToString("#,0"));
        }
        else
            gilNode->AtkResNode.NodeFlags &= ~NodeFlagsMask;
    }

    private static void DisplayAndHideTooltip(AddonEventType type, AddonEventData data)
    {
        var addonId = ((AtkUnitBase*)data.AddonPointer)->Id;
        var tooltipBuilder = new StringBuilder();
        
        if (Main.CharacterCurrencyInfos.Count == 0) Main.LoadDataMCS();
        Main.CharacterCurrencyInfos
            .FirstOrDefault(x => x.Character.ContentID == LocalPlayerState.ContentID).SubCurrencyAmount
            .TryGetValue(1, out var infoDic);

        foreach (var source in infoDic)
        {
            if (source.Value == 0) continue;
            tooltipBuilder.Append($"{source.Key.Category.GetSelectedViewName(source.Key.ID)}: {source.Value:N0}");
            tooltipBuilder.AppendLine();
        }

        switch (type)
        {
            case AddonEventType.MouseOver:
                AtkStage.Instance()->TooltipManager.ShowTooltip(addonId, (AtkResNode*)data.NodeTargetPointer, tooltipBuilder.ToString().Trim());
                break;
            case AddonEventType.MouseOut:
                AtkStage.Instance()->TooltipManager.HideTooltip(addonId);
                break;
        }
    }

    protected override void OnUninit()
    {
        if (TryGetAddonByName<AtkUnitBase>(AddonName, out var addon))
        {
            addon->FireCloseCallback();
            addon->Close(true);
        }

        DService.AddonLifecycle.UnregisterListener(OnCurrencyUI);
        CurrencyAmountCache = null;
    }
}
