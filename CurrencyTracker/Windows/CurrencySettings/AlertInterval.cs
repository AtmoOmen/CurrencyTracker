using System;
using System.Numerics;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Tools;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using IntervalUtility;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private int alertMode; // 0 - Amount; 1 - Change;
    private int intervalStart;
    private int intervalEnd = 1;
    private Interval<int>? selectedInterval;
    private TransactionFileCategory viewIA = TransactionFileCategory.Inventory;
    private ulong idIA;
    private Vector2 alertIntervalWidth = new(200, 100);

    private void IntervalAlertUI()
    {
        SelectAlertTypeUI();

        SelectContainerTypeUI();

        ImGui.Separator();
        IntervalManagerUI();

        AlertNotificationUI();
    }

    private void SelectAlertTypeUI()
    {
        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("AlertType")}:");

        ImGui.SameLine();
        ImGuiOm.HelpMarker(Service.Lang.GetText("AlertIntervalHelp"), 20f, FontAwesomeIcon.InfoCircle, true);

        if (ImGui.RadioButton(Service.Lang.GetText("Amount"), ref alertMode, 0)) selectedInterval = null;
        ImGui.SameLine();
        if (ImGui.RadioButton(Service.Lang.GetText("Change"), ref alertMode, 1)) selectedInterval = null;
    }

    private void SelectContainerTypeUI()
    {
        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ContainerType")}:");
        ImGui.SetNextItemWidth(alertIntervalWidth.X + 56);
        using var combo = ImRaii.Combo("##IntervalAlertViewSelect", GetSelectedViewName(viewIA, idIA));
        if (combo)
        {
            DrawViewSelectableIA(TransactionFileCategory.Inventory, 0);
            foreach (var retainer in Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID])
                DrawViewSelectableIA(TransactionFileCategory.Retainer, retainer.Key);
            DrawViewSelectableIA(TransactionFileCategory.SaddleBag, 0);
            DrawViewSelectableIA(TransactionFileCategory.PremiumSaddleBag, 0);
        }
    }

    private void IntervalManagerUI()
    {
        var intervals = CurrencyInterval.LoadIntervals(Main.SelectedCurrencyID, alertMode, new TransactionFileCategoryInfo(viewIA, idIA));

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("IntervalList")}:");

        ImGui.SetNextItemWidth(alertIntervalWidth.X - CheckboxColumn.CheckboxWidth + 48);
        if (ImGui.BeginCombo("##IntervalSelectCombo",
                             selectedInterval != null ? selectedInterval.ToIntervalString() :
                             intervals.Count != 0 ? Service.Lang.GetText("PleaseSelect") : ""))
        {
            foreach (var interval in intervals)
                if (ImGui.Selectable(interval.ToIntervalString(), selectedInterval == interval))
                    selectedInterval = new Interval<int>(interval.Start, interval.End);
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("DeleteInterval", FontAwesomeIcon.TrashAlt, "", true))
        {
            if (selectedInterval != null)
            {
                CurrencyInterval.RemoveInterval(Main.SelectedCurrencyID, alertMode, new TransactionFileCategoryInfo(viewIA, idIA), selectedInterval);
                selectedInterval = null;
            }
        }

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("IntervalInput")}:");

        ImGui.SameLine();
        ImGuiOm.HelpMarker(Service.Lang.GetText("AlertIntervalHelp1"), 20f, FontAwesomeIcon.InfoCircle, true);

        ImGui.BeginGroup();
        ImGui.SetNextItemWidth(radioButtonsTRWidth);
        if (ImGui.InputInt($"{Service.Lang.GetText("Minimum")}##MinIntervalStart", ref intervalStart, 1000, 1000000))
            intervalStart = intervalStart != -1 ? Math.Max(0, intervalStart) : -1;

        ImGui.SetNextItemWidth(radioButtonsTRWidth);
        if (ImGui.InputInt($"{Service.Lang.GetText("Maximum")}##MaxIntervalEnd", ref intervalEnd, 1000, 1000000))
            intervalEnd = intervalEnd != -1 ? Math.Max(intervalEnd, intervalStart) : -1;
        ImGui.EndGroup();
        alertIntervalWidth = ImGui.GetItemRectSize();

        ImGui.SameLine();
        if (ImGuiOm.ButtonIconWithTextVertical(FontAwesomeIcon.Plus, Service.Lang.GetText("Add"), true))
        {
            var interval = CurrencyInterval.CreateInterval(intervalStart, intervalEnd);
            if (interval != null)
            {
                CurrencyInterval.AddInterval(Main.SelectedCurrencyID, alertMode, new TransactionFileCategoryInfo(viewIA, idIA), interval);
            }
        }
    }

    private void AlertNotificationUI()
    {
        ImGui.Separator();

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("NotificationType")}:");

        var isAlertInChat = Service.Config.AlertNotificationChat;
        if (ImGui.Checkbox(
                Service.Config.AlertNotificationChat ? "##AlertNotificationChat" : $"{Service.Lang.GetText("BackupHelp5")}", ref isAlertInChat))
        {
            Service.Config.AlertNotificationChat = isAlertInChat;
            Service.Config.Save();
        }

        if (isAlertInChat)
        {
            var paramsEP = new[]
            {
                Service.Lang.GetText("AlertType"), Service.Lang.GetText("ParamEP-Value"),
                Service.Lang.GetText("ParamEP-CurrencyName"), Service.Lang.GetText("ContainerType"),
                Service.Lang.GetText("Interval")
            };
            var textToShow = Service.Config.CustomNoteContents.TryGetValue("AlertIntervalMessage", out var value)
                                 ? value
                                 : Service.Lang.GetOrigText("AlertIntervalMessage");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(alertIntervalWidth.X - CheckboxColumn.CheckboxWidth + 8);
            if (ImGui.InputText("##AlertNotificationChatNote", ref textToShow, 50))
            {
                Service.Config.CustomNoteContents["AlertIntervalMessage"] = textToShow;
                Service.Config.Save();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                if (!string.IsNullOrEmpty(textToShow))
                {
                    ImGui.Text(textToShow);
                    ImGui.Separator();
                    for (var i = 0; i < paramsEP.Length; i++) ImGui.Text("{" + i + "}" + $" - {paramsEP[i]}");
                }

                ImGui.EndTooltip();
            }

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("ResetContent_AlertIntervalMessage", FontAwesomeIcon.Sync, Service.Lang.GetText("Reset"), true))
            {
                Service.Config.CustomNoteContents.Remove("AlertIntervalMessage");
                Service.Config.Save();
            }
        }
    }

    private void DrawViewSelectableIA(TransactionFileCategory category, ulong ID)
    {
        var text = GetSelectedViewName(category, ID);
        var boolUI = category == viewIA && ID == idIA;
        if (ImGui.Selectable($"{text}##{category}_{ID}", boolUI))
        {
            selectedInterval = null;
            viewIA = category;
            idIA = ID;
        }
    }
}
