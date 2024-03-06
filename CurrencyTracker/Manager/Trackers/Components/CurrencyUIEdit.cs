namespace CurrencyTracker.Manager.Trackers.Components;

public class CurrencyUIEdit : ITrackerComponent
{
    public bool Initialized { get; set; }

    private IAddonEventHandle? mouseoverHandle;
    private IAddonEventHandle? mouseoutHandle;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "Currency", OnCurrencyUI);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Currency", OnCurrencyUI);
    }

    private void OnCurrencyUI(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PreDraw:
                UITextEdit(args);
                break;

            case AddonEvent.PostSetup:
                UITooltipEdit(args);
                break;
        }
    }

    private static unsafe void UITextEdit(AddonArgs args)
    {
        var UI = (AtkUnitBase*)args.Addon;
        if (!HelpersOm.IsAddonAndNodesReady(UI)) return;

        var gilNode = (AtkComponentBase*)UI->GetComponentNodeById(12);
        var gilTextNode = gilNode->GetTextNodeById(5);

        gilTextNode->GetAsAtkTextNode()->SetText(CurrencyInfo
                                                 .GetCharacterCurrencyAmount(1, P.CurrentCharacter)
                                                 .ToString("#,0"));
    }

    private unsafe void UITooltipEdit(AddonArgs args)
    {
        var UI = (AtkUnitBase*)args.Addon;
        if (!HelpersOm.IsAddonAndNodesReady(UI)) return;

        var gilTextNode = ((AtkComponentBase*)UI->GetComponentNodeById(12))->GetTextNodeById(5);
        if (gilTextNode == null) return;

        gilTextNode->NodeFlags |= NodeFlags.EmitsEvents | NodeFlags.RespondToMouse | NodeFlags.HasCollision;

        mouseoverHandle =
            Service.AddonEventManager.AddEvent((nint)UI, (nint)gilTextNode, AddonEventType.MouseOver,
                                               TooltipHandler);
        mouseoutHandle =
            Service.AddonEventManager.AddEvent((nint)UI, (nint)gilTextNode, AddonEventType.MouseOut,
                                               TooltipHandler);
    }

    private static unsafe void TooltipHandler(AddonEventType type, nint addon, nint node)
    {
        var addonId = ((AtkUnitBase*)addon)->ID;
        var inventoryAmount =
            CurrencyInfo.GetCurrencyAmount(1).ToString("N0");
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

        var tooltip = tooltipBuilder.ToString();

        switch (type)
        {
            case AddonEventType.MouseOver:
                AtkStage.GetSingleton()->TooltipManager.ShowTooltip(addonId, (AtkResNode*)node, tooltip);
                break;

            case AddonEventType.MouseOut:
                AtkStage.GetSingleton()->TooltipManager.HideTooltip(addonId);
                break;
        }
    }

    public unsafe void Uninit()
    {
        Service.AddonEventManager.RemoveEvent(mouseoverHandle);
        Service.AddonEventManager.RemoveEvent(mouseoutHandle);

        var UI = (AtkUnitBase*)Service.GameGui.GetAddonByName("Currency");
        if (HelpersOm.IsAddonAndNodesReady(UI))
        {
            var gilTextNode = ((AtkComponentBase*)UI->GetComponentNodeById(12))->GetTextNodeById(5);
            if (gilTextNode == null) return;
            gilTextNode->NodeFlags &= ~(NodeFlags.EmitsEvents | NodeFlags.RespondToMouse | NodeFlags.HasCollision);
        }

        Service.AddonLifecycle.UnregisterListener(OnCurrencyUI);
    }
}
