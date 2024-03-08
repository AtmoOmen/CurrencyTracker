using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tools;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using IntervalUtility;
using OmenTools.ImGuiOm;
using static CurrencyTracker.Manager.Tools.Helpers;

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
        IntervalManagerUI();
        AlertNotificationUI();
    }

    private void SelectAlertTypeUI()
    {
        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("AlertType")}:");

        ImGui.SameLine();
        ImGuiOm.HelpMarker(Service.Lang.GetText("AlertIntervalHelp"));

        if (ImGui.RadioButton(Service.Lang.GetText("Amount"), ref alertMode, 0)) selectedInterval = null;
        ImGui.SameLine();
        if (ImGui.RadioButton(Service.Lang.GetText("Change"), ref alertMode, 1)) selectedInterval = null;
    }

    private void SelectContainerTypeUI()
    {
        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ContainerType")}:");
        ImGui.SetNextItemWidth(alertIntervalWidth.X + 56);
        using (var combo = ImRaii.Combo("##IntervalAlertViewSelect", GetSelectedViewName(viewIA, idIA)))
        {
            if (combo)
            {
                DrawViewSelectableIA(TransactionFileCategory.Inventory, 0);
                foreach (var retainer in C.CharacterRetainers[P.CurrentCharacter.ContentID])
                    DrawViewSelectableIA(TransactionFileCategory.Retainer, retainer.Key);
                DrawViewSelectableIA(TransactionFileCategory.SaddleBag, 0);
                DrawViewSelectableIA(TransactionFileCategory.PremiumSaddleBag, 0);
            }
        }
    }

    private void IntervalManagerUI()
    {
        ImGui.Separator();

        var intervals = GetOrCreateIntervals(selectedCurrencyID, alertMode, viewIA, idIA);

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("IntervalList")}:");

        ImGui.SetNextItemWidth(alertIntervalWidth.X - M.checkboxColumnWidth + 48);
        using (var combo = ImRaii.Combo("##IntervalSelectCombo",
                                        selectedInterval != null ? selectedInterval.ToIntervalString() :
                                        intervals.Any() ? Service.Lang.GetText("PleaseSelect") : ""))
        {
            if (combo)
            {
                foreach (var interval in intervals)
                    if (ImGui.Selectable(interval.ToIntervalString()))
                        selectedInterval = new Interval<int>(interval.Start, interval.End);
            }
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("DeleteInterval", FontAwesomeIcon.TrashAlt))
        {
            if (selectedInterval == null) return;
            RemoveIntervalHandler(selectedCurrencyID, selectedInterval.Start, selectedInterval.End);
            C.Save();
        }

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("IntervalInput")}:");

        ImGui.SameLine();
        ImGuiOm.HelpMarker(Service.Lang.GetText("AlertIntervalHelp1"));

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
        if (ImGuiOm.ButtonIconWithTextVertical(FontAwesomeIcon.Plus, Service.Lang.GetText("Add")))
        {
            AddIntervalHandler(selectedCurrencyID, intervalStart, intervalEnd);
            C.Save();
        }
    }

    private void AlertNotificationUI()
    {
        ImGui.Separator();

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("NotificationType")}:");

        var b1 = C.AlertNotificationChat;
        if (ImGui.Checkbox(
                C.AlertNotificationChat ? "##AlertNotificationChat" : $"{Service.Lang.GetText("BackupHelp5")}", ref b1))
        {
            C.AlertNotificationChat = b1;
            C.Save();
        }

        if (b1)
        {
            var paramsEP = new string[]
            {
                Service.Lang.GetText("AlertType"), Service.Lang.GetText("ParamEP-Value"),
                Service.Lang.GetText("ParamEP-CurrencyName"), Service.Lang.GetText("ContainerType"),
                Service.Lang.GetText("Interval")
            };
            var key = "AlertIntervalMessage";
            var textToShow = C.CustomNoteContents.TryGetValue(key, out var value)
                                 ? value
                                 : Service.Lang.GetOrigText(key);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(alertIntervalWidth.X - M.checkboxColumnWidth + 8);
            if (ImGui.InputText("##AlertNotificationChatNote", ref textToShow, 50))
            {
                C.CustomNoteContents[key] = textToShow;
                C.Save();
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
            if (ImGuiOm.ButtonIcon($"ResetContent_{key}", FontAwesomeIcon.Sync, Service.Lang.GetText("Reset")))
            {
                C.CustomNoteContents[key] = Service.Lang.GetOrigText(key);
                C.Save();
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

    private void AddIntervalHandler(uint currencyID, int start, int end)
    {
        if (end != -1 && start > end) return;

        var intervals = GetOrCreateIntervals(currencyID, alertMode, viewIA, idIA);
        var newInterval = CreateInterval(start, end);

        if (!intervals.Contains(newInterval))
        {
            intervals.Add(newInterval);
            C.Save();
        }
    }

    private void RemoveIntervalHandler(uint currencyID, int? start, int? end)
    {
        var intervals = GetOrCreateIntervals(currencyID, alertMode, viewIA, idIA);
        var newInterval = new Interval<int>(start, end);

        if (intervals.Remove(newInterval))
        {
            C.Save();
            selectedInterval = null;
        }
    }

    public static List<Interval<int>> GetOrCreateIntervals(
        uint currencyID, int alertMode, TransactionFileCategory view, ulong ID)
    {
        var intervalList = alertMode == 0
                               ? Service.Config.CurrencyRules[currencyID].AlertedAmountIntervals
                               : Service.Config.CurrencyRules[currencyID].AlertedChangeIntervals;
        var key = GetTransactionViewKeyString(view, ID);

        if (!intervalList.TryGetValue(key, out var intervals)) intervalList[key] = new List<Interval<int>>();

        return intervalList[key];
    }


    private static Interval<int> CreateInterval(int start, int end)
    {
        int? end1 = start == -1 ? null : start;
        int? end2 = end == -1 ? null : end;
        return new Interval<int>(end1, end2);
    }
}
