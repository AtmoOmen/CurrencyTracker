namespace CurrencyTracker.Windows;

public partial class Main
{
    private bool isClusteredByTime;
    private bool isTimeFilterEnabled;
    private const bool SelectTimeDeco = false; // Always False

    private int clusterHour;
    private DateTime filterStartDate = DateTime.Now - TimeSpan.FromDays(1);
    private DateTime filterEndDate = DateTime.Now;
    private DateTime filterViewDate = DateTime.Now;
    private bool startDateEnable;
    private bool endDateEnable;
    private Vector2 datePickerRegion = new(400);
    private int datePickerPagingWidth = 250;

    private void TimeColumnHeaderUI()
    {
        ImGui.Selectable($" {Service.Lang.GetText("Time")}");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
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
        if (ImGui.Checkbox(Service.Lang.GetText("ClusterByTime"), ref isClusteredByTime))
        {
            searchTimer.Restart();
        }

        if (isClusteredByTime)
        {
            ImGui.SetNextItemWidth(115f);
            if (ImGui.InputInt(Service.Lang.GetText("Hours"), ref clusterHour, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                clusterHour = Math.Max(0, clusterHour);
                searchTimer.Restart();
            }

            ImGui.SameLine();
            ImGuiOm.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ClusterByTimeHelp1", clusterHour)}");
        }
    }

    private void FilterByTimeUI()
    {
        if (ImGui.Checkbox($"{Service.Lang.GetText("FilterByTime")}##TimeFilter", ref isTimeFilterEnabled))
        {
            searchTimer.Restart();
        }

        DateInput(ref filterStartDate, "StartDate", ref startDateEnable, ref endDateEnable);
        DateInput(ref filterEndDate, "EndDate", ref endDateEnable, ref startDateEnable);

        if (startDateEnable)
        {
            DatePicker(ref filterStartDate, ref filterViewDate, true);
        }

        if (endDateEnable)
        {
            DatePicker(ref filterEndDate, ref filterViewDate, false);
        }
    }

    private static void DateInput(ref DateTime date, string label, ref bool bool1, ref bool bool2)
    {
        if (ImGui.Button($"{date:yyyy-MM-dd}"))
        {
            bool1 = !bool1;
            bool2 = false;
        }

        ImGui.SameLine();
        ImGui.Text(Service.Lang.GetText(label));
    }

    private void DatePicker(ref DateTime currentDate, ref DateTime viewDate, bool selectMode)
    {
        using (var child = ImRaii.Child($"DatePicker {selectMode}", datePickerRegion, false, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar))
        {
            if (child)
            {
                ImGui.BeginGroup();
                ImGui.Separator();

                ImGuiOm.CenterAlignFor(datePickerPagingWidth);
                ImGui.BeginGroup();
                if (ImGuiOm.ButtonIcon("LastYear", FontAwesomeIcon.Backward)) viewDate = viewDate.AddYears(-1);

                ImGui.SameLine();
                if (ImGui.ArrowButton("LastMonth", ImGuiDir.Left)) viewDate = viewDate.AddMonths(-1);

                ImGui.SameLine();
                ImGui.Text($"{viewDate:yyyy.MM}");

                ImGui.SameLine();
                if (ImGui.ArrowButton("NextMonth", ImGuiDir.Right)) viewDate = viewDate.AddMonths(1);

                ImGui.SameLine();
                if (ImGuiOm.ButtonIcon("NextYear", FontAwesomeIcon.Forward)) viewDate = viewDate.AddYears(1);
                ImGui.EndGroup();
                datePickerPagingWidth = (int)ImGui.GetItemRectSize().X;

                using (var table = ImRaii.Table("DatePicker", 7, ImGuiTableFlags.NoBordersInBody))
                {
                    if (table)
                    {
                        var weekDays = Service.Lang.GetText("WeekDays").Split(',');
                        foreach (var day in weekDays)
                        {
                            ImGui.TableNextColumn();
                            ImGuiOm.TextCentered($"{day}_{viewDate}", day);
                        }

                        ImGui.TableNextRow(ImGuiTableRowFlags.None);
                        var firstDayOfMonth = new DateTime(viewDate.Year, viewDate.Month, 1);
                        var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
                        var daysInMonth = DateTime.DaysInMonth(viewDate.Year, viewDate.Month);

                        for (var i = 0; i < firstDayOfWeek; i++)
                        {
                            ImGui.TableNextColumn();
                            ImGuiOm.TextCentered($"{i}_{viewDate}", "");
                        }

                        for (var day = 1; day <= daysInMonth; day++)
                        {
                            ImGui.TableNextColumn();
                            var currentDay = new DateTime(viewDate.Year, viewDate.Month, day);
                            var isCurrentDate = currentDate.Year == viewDate.Year && currentDate.Month == viewDate.Month && currentDate.Day == day;
                            var isSelectable = selectMode ? currentDay >= filterEndDate : currentDay <= filterStartDate;

                            if (isCurrentDate)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.6f, 1.0f, 1.0f));
                                if (ImGuiOm.SelectableTextCentered(day.ToString(), SelectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
                                {
                                    currentDate = currentDay;
                                }
                                ImGui.PopStyleColor();
                            }
                            else if (isSelectable)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                                ImGuiOm.TextCentered(day.ToString(), day.ToString());
                                ImGui.PopStyleColor();
                            }
                            else if (ImGuiOm.SelectableTextCentered(day.ToString(), SelectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
                            {
                                currentDate = currentDay;
                                searchTimer.Restart();
                            }
                        }
                    }
                }

                ImGui.EndGroup();
                datePickerRegion = ImGui.GetItemRectSize();
            }
        }
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
