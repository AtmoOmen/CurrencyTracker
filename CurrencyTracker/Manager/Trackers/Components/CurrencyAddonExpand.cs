using System.Linq;
using System.Text;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tasks;
using CurrencyTracker.Windows;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class CurrencyAddonExpand : ITrackerComponent
{
    public bool Initialized { get; set; }

    private static IAddonEventHandle? mouseoverHandle;
    private static IAddonEventHandle? mouseoutHandle;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Currency", OnCurrencyUI);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "Currency", OnCurrencyUI);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Currency", OnCurrencyUI);
    }

    private static void OnCurrencyUI(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostSetup or AddonEvent.PreDraw:
                ProcessCurrencyNode(true);
                break;
            case AddonEvent.PreFinalize:
                ProcessCurrencyNode(false);
                break;
        }
    }

    private static void ProcessCurrencyNode(bool isAdd)
    {
        if (!isAdd && mouseoverHandle != null && mouseoutHandle != null)
        {
            Service.AddonEventManager.RemoveEvent(mouseoverHandle);
            mouseoverHandle = null;
            Service.AddonEventManager.RemoveEvent(mouseoutHandle);
            mouseoutHandle = null;
        }

        if (!EzThrottler.Throttle("CurrencyAddonExpand", 1000)) return;

        if (!TryGetAddonByName<AtkUnitBase>("Currency", out var addon)) return;

        var componentNode = addon->GetNodeById(12)->GetAsAtkComponentNode();
        if (componentNode == null) return;
        var gilNode = componentNode->Component->GetTextNodeById(5)->GetAsAtkTextNode();
        if (gilNode == null) return;

        const NodeFlags nodeFlagsMask = NodeFlags.EmitsEvents | NodeFlags.RespondToMouse | NodeFlags.HasCollision;
    
        if (isAdd)
        {
            gilNode->AtkResNode.NodeFlags |= nodeFlagsMask;

            mouseoverHandle ??= Service.AddonEventManager.AddEvent((nint)addon, (nint)gilNode, AddonEventType.MouseOver, DisplayAndHideTooltip);
            mouseoutHandle ??= Service.AddonEventManager.AddEvent((nint)addon, (nint)gilNode, AddonEventType.MouseOut, DisplayAndHideTooltip);

            var currencyAmount = CurrencyInfo.GetCharacterCurrencyAmount(1, P.CurrentCharacter);
            gilNode->SetText(currencyAmount.ToString("#,0"));
        }
        else
        {
            gilNode->AtkResNode.NodeFlags &= ~nodeFlagsMask;
        }
    }

    private static void DisplayAndHideTooltip(AddonEventType type, nint addon, nint node)
    {
        var addonId = ((AtkUnitBase*)addon)->ID;
        var tooltipBuilder = new StringBuilder();

        if (Main.CharacterCurrencyInfos.Count == 0) Main.LoadDataMCS();
        Main.CharacterCurrencyInfos
            .FirstOrDefault(x => x.Character.ContentID == Service.ClientState.LocalContentId).SubCurrencyAmount
            .TryGetValue(1, out var infoDic);

        foreach (var source in infoDic)
        {
            if (source.Value == 0) continue;
            tooltipBuilder.Append($"{GetSelectedViewName(source.Key.Category, source.Key.ID)}: {source.Value:N0}");
            tooltipBuilder.AppendLine();
        }

        switch (type)
        {
            case AddonEventType.MouseOver:
                AtkStage.GetSingleton()->TooltipManager.ShowTooltip(addonId, (AtkResNode*)node, tooltipBuilder.ToString().Trim());
                break;
            case AddonEventType.MouseOut:
                AtkStage.GetSingleton()->TooltipManager.HideTooltip(addonId);
                break;
        }
    }

    public void Uninit()
    {
        var addon = (AtkUnitBase*)Service.GameGui.GetAddonByName("Currency");
        if (addon != null)
        {
            addon->FireCloseCallback();
            addon->Close(true);
        }

        Service.AddonLifecycle.UnregisterListener(OnCurrencyUI);
    }
}
