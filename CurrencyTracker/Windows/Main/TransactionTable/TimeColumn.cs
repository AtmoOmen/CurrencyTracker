namespace CurrencyTracker.Windows;

public partial class Main
{
    private bool isClusteredByTime;
    private bool isTimeFilterEnabled;

    private DateTime filterStartDate = DateTime.Now - TimeSpan.FromDays(1);
    private DateTime filterEndDate = DateTime.Now;
    private DatePicker startDatePicker = new(Service.Lang.GetText("WeekDays"));
    private DatePicker endDatePicker = new(Service.Lang.GetText("WeekDays"));

    private string lastLangTF = string.Empty;

    private int clusterHour;
    private bool startDateEnable;
    private bool endDateEnable;

    private void TimeColumnHeaderUI()
    {
        ImGui.Selectable($" {Service.Lang.GetText("Time")}");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            if (lastLangTF != Service.Lang.Language)
            {
                startDatePicker = new(Service.Lang.GetText("WeekDays"));
                endDatePicker = new(Service.Lang.GetText("WeekDays"));
                lastLangTF = Service.Lang.Language;
            }
            ImGui.OpenPopup("TimeFunctions");
        }

        using (var popup = ImRaii.Popup("TimeFunctions", ImGuiWindowFlags.NoTitleBar))
        {
            if (popup)
            {
                ClusterByTimeUI();
                FilterByTimeUI();
            }
        }
    }

    private void ClusterByTimeUI()
    {
        if (ImGui.Checkbox(Service.Lang.GetText("ClusterByTime"), ref isClusteredByTime)) OnDateSelected();

        if (isClusteredByTime)
        {
            ImGui.SetNextItemWidth(115f);
            if (ImGui.InputInt(Service.Lang.GetText("Hours"), ref clusterHour, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                clusterHour = Math.Max(0, clusterHour);
                OnDateSelected();
            }

            ImGui.SameLine();
            ImGuiOm.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ClusterByTimeHelp1", clusterHour)}");
        }
    }

    private void FilterByTimeUI()
    {
        if (ImGui.Checkbox($"{Service.Lang.GetText("FilterByTime")}##TimeFilter", ref isTimeFilterEnabled)) OnDateSelected();

        DateInput(ref filterStartDate, "StartDate", ref startDateEnable, ref endDateEnable);
        DateInput(ref filterEndDate, "EndDate", ref endDateEnable, ref startDateEnable);

        ImGui.Separator();

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

    private void OnDateSelected()
    {
        searchTimer.Restart();
    }

    private void TimeColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        var isLeftCtrl = ImGui.IsKeyDown(ImGuiKey.LeftCtrl);
        var isRightMouse = ImGui.IsMouseDown(ImGuiMouseButton.Right);
        var flag = (isLeftCtrl || isRightMouse) ? ImGuiSelectableFlags.SpanAllColumns : ImGuiSelectableFlags.None;
        var timeString = transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss");

        if ((!isLeftCtrl) ? ImGui.Selectable($"{timeString}##{i}") : ImGui.Selectable($"{timeString}##_{i}", ref selected, flag))
        {
            if (isLeftCtrl && !isRightMouse)
            {
                selectedStates[selectedCurrencyID][i] = selected;

                CheckAndUpdateSelectedStates(selected, transaction);
            }
        }

        if (isLeftCtrl && isRightMouse)
        {
            if (ImGui.IsItemHovered())
            {
                selectedStates[selectedCurrencyID][i] = selected = true;

                CheckAndUpdateSelectedStates(selected, transaction);
            }
        }

        ImGuiOm.ClickToCopy(timeString, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);

        return;

        void CheckAndUpdateSelectedStates(bool selected, TransactionsConvertor transaction)
        {
            var selectedList = selectedTransactions[selectedCurrencyID];

            if (selected)
            {
                if (!selectedList.Any(t => IsTransactionEqual(t, transaction))) selectedList.Add(transaction);
            }
            else
            {
                selectedList.RemoveAll(t => IsTransactionEqual(t, transaction));
            }
        }
    }
}
