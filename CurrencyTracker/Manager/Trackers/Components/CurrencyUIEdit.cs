using System.Text;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tasks;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class CurrencyUIEdit : ITrackerComponent
{
    public bool Initialized { get; set; }

    private static IAddonEventHandle? mouseoverHandle;
    private static IAddonEventHandle? mouseoutHandle;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Currency", OnCurrencyUI);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "Currency", OnCurrencyUI);
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
        var inventoryAmount = CurrencyInfo.GetCurrencyAmount(1).ToString("N0");
        var tooltipBuilder = new StringBuilder($"{Service.Lang.GetText("Inventory")}: {inventoryAmount}");

        if (Service.Config.CharacterRetainers.TryGetValue(P.CurrentCharacter.ContentID,
                                                          out var retainers))
        {
            foreach (var retainer in retainers)
            {
                var retainerAmount = CurrencyInfo.GetCurrencyAmountFromFile(
                    1, P.CurrentCharacter, TransactionFileCategory.Retainer, retainer.Key);
                if (retainerAmount.HasValue)
                {
                    tooltipBuilder.AppendLine();
                    tooltipBuilder.Append($"{retainer.Value}: {retainerAmount.Value.ToString("N0")}");
                }
            }
        }

        switch (type)
        {
            case AddonEventType.MouseOver:
                AtkStage.GetSingleton()->TooltipManager.ShowTooltip(addonId, (AtkResNode*)node, tooltipBuilder.ToString());
                break;
            case AddonEventType.MouseOut:
                AtkStage.GetSingleton()->TooltipManager.HideTooltip(addonId);
                break;
        }
    }

    public void Uninit()
    {
        ProcessCurrencyNode(false);

        Service.AddonLifecycle.UnregisterListener(OnCurrencyUI);
    }
}
