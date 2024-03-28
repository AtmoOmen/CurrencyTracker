using System;
using CurrencyTracker.Manager;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OmenTools.ImGuiOm;
using OmenTools.Widgets;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static bool isClusteredByTime;
    private static bool isTimeFilterEnabled;

    private static DateTime filterStartDate = DateTime.Now - TimeSpan.FromDays(1);
    private static DateTime filterEndDate = DateTime.Now;
    private static DatePicker startDatePicker = new(Service.Lang.GetText("WeekDays"));
    private static DatePicker endDatePicker = new(Service.Lang.GetText("WeekDays"));

    private static int clusterHour;
    private static bool startDateEnable;
    private static bool endDateEnable;

    private static void TimeColumnHeaderUI()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        ImGuiOm.SelectableFillCell($"{Service.Lang.GetText("Time")}");
        ImGui.EndDisabled();

        if (ImGui.BeginPopupContextItem("TimerColumnHeaderFunctions"))
        {
            ClusterByTimeUI();
            FilterByTimeUI();
            ImGui.EndPopup();
        }
    }

    private static void ClusterByTimeUI()
    {
        if (ImGui.Checkbox(Service.Lang.GetText("ClusterByTime"), ref isClusteredByTime)) 
            RefreshTransactionsView();

        if (isClusteredByTime)
        {
            ImGui.SetNextItemWidth(115f);
            if (ImGui.InputInt(Service.Lang.GetText("Hours"), ref clusterHour, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                clusterHour = Math.Max(0, clusterHour);
                RefreshTransactionsView();
            }

            ImGui.SameLine();
            ImGuiOm.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ClusterByTimeHelp1", clusterHour)}");
        }
    }

    private static void FilterByTimeUI()
    {
        if (ImGui.Checkbox($"{Service.Lang.GetText("FilterByTime")}##TimeFilter", ref isTimeFilterEnabled)) 
            RefreshTransactionsView();

        DateInput(ref filterStartDate, "StartDate", ref startDateEnable, ref endDateEnable);
        DateInput(ref filterEndDate, "EndDate", ref endDateEnable, ref startDateEnable);

        if (startDateEnable || endDateEnable) ImGui.Separator();

        if (startDateEnable)
        {
            startDatePicker.Draw(ref filterStartDate);
        }

        if (endDateEnable)
        {
            endDatePicker.Draw(ref filterEndDate);
        }

        return;

        void DateInput(ref DateTime date, string label, ref bool bool1, ref bool bool2)
        {
            if (ImGui.Button($"{date:yyyy-MM-dd}"))
            {
                bool1 = !bool1;
                bool2 = false;

                if (!isTimeFilterEnabled) isTimeFilterEnabled = true;
            }

            ImGui.SameLine();
            ImGui.Text(Service.Lang.GetText(label));
        }
    }

    private static void TimeColumnCellUI(int i, DisplayTransaction transaction)
    {
        var isLeftCtrl = ImGui.IsKeyDown(ImGuiKey.LeftCtrl);
        var isRightMouse = ImGui.IsMouseDown(ImGuiMouseButton.Right);
        var flag = (isLeftCtrl || isRightMouse) ? ImGuiSelectableFlags.SpanAllColumns : ImGuiSelectableFlags.None;
        var timeString = transaction.Transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss");

        var selected = transaction.Selected;
        if (!isLeftCtrl ? ImGuiOm.Selectable($"{timeString}##{i}") : ImGuiOm.Selectable($"{timeString}##{i}", ref selected, flag))
        {
            if (isLeftCtrl && !isRightMouse)
            {
                transaction.Selected = selected;
            }
        }

        if (isLeftCtrl && isRightMouse && ImGui.IsItemHovered())
        {
            transaction.Selected = true;
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && isLeftCtrl && selected)
            ImGui.OpenPopup($"TableToolsTimeColumn{i}");

        if (ImGui.BeginPopup($"TableToolsTimeColumn{i}"))
        {
            CheckboxColumnToolUI();
            ImGui.EndPopup();
        }

        ImGuiOm.ClickToCopy(timeString, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);
    }

    private static void SwitchDatePickerLanguage(string lang)
    {
        startDatePicker = new(Service.Lang.GetText("WeekDays"));
        endDatePicker = new(Service.Lang.GetText("WeekDays"));
    }
}
