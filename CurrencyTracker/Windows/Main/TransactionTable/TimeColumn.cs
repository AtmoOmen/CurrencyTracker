using System;
using CurrencyTracker.Manager;
using Dalamud.Interface.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;
using OmenTools.Widgets;

namespace CurrencyTracker.Windows;

public class TimeColumn : TableColumn
{
    internal static bool IsClusteredByTime;
    internal static bool IsTimeFilterEnabled;

    internal static DateTime FilterStartDate = DateTime.Now - TimeSpan.FromDays(1);
    internal static DateTime FilterEndDate = DateTime.Now;
    internal static readonly DatePicker StartDatePicker = new(Service.Lang.GetText("WeekDays"));
    internal static readonly DatePicker EndDatePicker = new(Service.Lang.GetText("WeekDays"));

    internal static int ClusterHour;
    internal static bool startDateEnable;
    internal static bool endDateEnable;
    internal static bool? timeColumnSelectMode;

    public override void Header()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || CurrentTransactions.Count <= 0);
        ImGuiOm.SelectableFillCell($"{Service.Lang.GetText("Time")}");
        ImGui.EndDisabled();

        if (ImGui.BeginPopupContextItem("TimerColumnHeaderFunctions"))
        {
            ClusterByTimeUI();
            FilterByTimeUI();
            ImGui.EndPopup();
        }
    }

    public override void Cell(int i, DisplayTransaction transaction)
    {
        if (i < 0) return;
        var selected = transaction.Selected;
        var isLeftCtrl = ImGui.IsKeyDown(ImGuiKey.LeftCtrl);
        var isRightMouse = ImGui.IsMouseDown(ImGuiMouseButton.Right);
        var flag = (selected || isLeftCtrl) ? ImGuiSelectableFlags.SpanAllColumns : ImGuiSelectableFlags.None;
        var timeString = transaction.Transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss");

        if (ImGuiOm.Selectable($"{timeString}##{i}", selected, flag))
            if (flag is ImGuiSelectableFlags.SpanAllColumns) transaction.Selected ^= true;

        switch (isLeftCtrl)
        {
            case true when isRightMouse && ImGui.IsItemHovered():
                timeColumnSelectMode ??= !transaction.Selected;
                transaction.Selected = (bool)timeColumnSelectMode;
                break;
            case false:
                timeColumnSelectMode = null;
                break;
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && selected && !isLeftCtrl)
            ImGui.OpenPopup($"TableToolbarFromTimeColumn{i}");

        if (ImGui.BeginPopup($"TableToolbarFromTimeColumn{i}"))
        {
            CheckboxColumn.CheckboxColumnToolUI();
            ImGui.EndPopup();
        }

        if (!transaction.Selected) ImGuiOm.ClickToCopy(timeString, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);
    }

    private static void ClusterByTimeUI()
    {
        if (ImGui.Checkbox(Service.Lang.GetText("ClusterByTime"), ref IsClusteredByTime)) 
            RefreshTable();

        if (IsClusteredByTime)
        {
            ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
            ImGui.InputInt(Service.Lang.GetText("Hours"), ref ClusterHour);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                ClusterHour = Math.Max(0, ClusterHour);
                RefreshTable();
            }

            ImGui.SameLine();
            ImGuiOm.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ClusterByTimeHelp1", ClusterHour)}");
        }
    }

    private static void FilterByTimeUI()
    {
        if (ImGui.Checkbox($"{Service.Lang.GetText("FilterByTime")}##TimeFilter", ref IsTimeFilterEnabled)) 
            RefreshTable();

        DateInput(ref FilterStartDate, "StartDate", ref startDateEnable, ref endDateEnable);
        DateInput(ref FilterEndDate, "EndDate", ref endDateEnable, ref startDateEnable);

        if (startDateEnable || endDateEnable) ImGui.Separator();

        if (startDateEnable)
            if (StartDatePicker.Draw(ref FilterStartDate)) RefreshTable();

        if (endDateEnable)
            if (EndDatePicker.Draw(ref FilterEndDate)) RefreshTable();

        return;

        void DateInput(ref DateTime date, string label, ref bool bool1, ref bool bool2)
        {
            if (ImGui.Button($"{date:yyyy-MM-dd}"))
            {
                bool1 = !bool1;
                bool2 = false;
            }

            ImGui.SameLine();
            ImGui.Text(Service.Lang.GetText(label));
        }
    }
}
