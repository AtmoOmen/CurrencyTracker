using System.Linq;
using System.Text;
using CurrencyTracker.Infos;
using CurrencyTracker.Windows;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Events.EventDataTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Interop.Game.Helpers;
using OmenTools.OmenService;
using OmenTools.Threading;

namespace CurrencyTracker.Trackers.Components;

public unsafe class CurrencyAddonExpand : TrackerComponentBase
{
    private const string    ADDON_NAME       = "Currency";
    private const int       CURRENCY_NODE_ID = 12;
    private const int       GIL_TEXT_NODE_ID = 5;
    private const NodeFlags NODE_FLAGS_MASK  = NodeFlags.EmitsEvents | NodeFlags.RespondToMouse | NodeFlags.HasCollision;

    private static long?              CurrencyAmountCache;
    private static IAddonEventHandle? MouseoverHandle;
    private static IAddonEventHandle? MouseoutHandle;

    protected override void OnInit()
    {
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup,   ADDON_NAME, OnCurrencyUI);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreDraw,     ADDON_NAME, OnCurrencyUI);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, ADDON_NAME, OnCurrencyUI);
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
        if (!isAdd && MouseoverHandle != null && MouseoutHandle != null)
        {
            DService.Instance().AddonEvent.RemoveEvent(MouseoverHandle);
            MouseoverHandle = null;
            DService.Instance().AddonEvent.RemoveEvent(MouseoutHandle);
            MouseoutHandle = null;
        }

        if (Throttler.Shared.Throttle("CurrencyAddonExpand", 1000))
            CurrencyAmountCache = CurrencyInfo.GetCharacterCurrencyAmount(1, P.CurrentCharacter);

        if (!AddonHelper.TryGetByName(ADDON_NAME, out var addon)) return;

        var componentNode = addon->GetNodeById(CURRENCY_NODE_ID)->GetAsAtkComponentNode();
        if (componentNode == null) return;

        var gilNode = componentNode->Component->GetTextNodeById(GIL_TEXT_NODE_ID)->GetAsAtkTextNode();
        if (gilNode == null) return;

        if (isAdd)
        {
            gilNode->AtkResNode.NodeFlags |= NODE_FLAGS_MASK;

            MouseoverHandle ??= DService.Instance().AddonEvent.AddEvent((nint)addon, (nint)gilNode, AddonEventType.MouseOver, DisplayAndHideTooltip);
            MouseoutHandle  ??= DService.Instance().AddonEvent.AddEvent((nint)addon, (nint)gilNode, AddonEventType.MouseOut,  DisplayAndHideTooltip);

            if (CurrencyAmountCache != null)
                gilNode->SetText(((long)CurrencyAmountCache).ToString("#,0"));
        }
        else
            gilNode->AtkResNode.NodeFlags &= ~NODE_FLAGS_MASK;
    }

    private static void DisplayAndHideTooltip(AddonEventType type, AddonEventData data)
    {
        var addonId        = ((AtkUnitBase*)data.AddonPointer)->Id;
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
        if (AddonHelper.TryGetByName(ADDON_NAME, out var addon))
        {
            addon->FireCloseCallback();
            addon->Close(true);
        }

        DService.Instance().AddonLifecycle.UnregisterListener(OnCurrencyUI);
        CurrencyAmountCache = null;
    }
}
