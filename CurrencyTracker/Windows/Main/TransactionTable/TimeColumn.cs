namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private bool isClusteredByTime = false;
    private bool isTimeFilterEnabled = false;
    private readonly bool selectTimeDeco = false; // Always False

    private int clusterHour = 0;
    private DateTime filterStartDate = DateTime.Now - TimeSpan.FromDays(1);
    private DateTime filterEndDate = DateTime.Now;
    private DateTime filterViewDate = DateTime.Now;
    private bool startDateEnable;
    private bool endDateEnable;

    private void TimeColumnHeaderUI()
    {
        ImGui.Selectable($" {Service.Lang.GetText("Time")}");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("TimeFunctions");
        }

        using (var popup = ImRaii.Popup("TimeFunctions", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
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
            Widgets.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ClusterByTimeHelp1", clusterHour)}");
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
            DatePicker(ref filterStartDate, ref filterViewDate, true, new Vector2(320, 215));
        }

        if (endDateEnable)
        {
            DatePicker(ref filterEndDate, ref filterViewDate, false, new Vector2(320, 215));
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

    private void DatePicker(ref DateTime currentDate, ref DateTime viewDate, bool selectMode, Vector2 childframeSize)
    {
        if (ImGui.BeginChildFrame(Convert.ToUInt32(selectMode) + 10, childframeSize, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Separator();

            ImGui.SetCursorPosX(childframeSize.X / 8);
            if (IconButton(FontAwesomeIcon.Backward, "", "LastYear")) viewDate = viewDate.AddYears(-1);

            ImGui.SameLine();
            if (ImGui.ArrowButton("LastMonth", ImGuiDir.Left)) viewDate = viewDate.AddMonths(-1);

            ImGui.SameLine();
            ImGui.Text($"{viewDate:yyyy.MM}");

            ImGui.SameLine();
            if (ImGui.ArrowButton("NextMonth", ImGuiDir.Right)) viewDate = viewDate.AddMonths(1);

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.Forward, "", "NextYear")) viewDate = viewDate.AddYears(1);

            using (var table = ImRaii.Table("DatePicker", 7, ImGuiTableFlags.NoBordersInBody))
            {
                if (table)
                {
                    var weekDays = Service.Lang.GetText("WeekDays").Split(',');
                    foreach (var day in weekDays)
                    {
                        ImGui.TableNextColumn();
                        TextCentered(day);
                    }

                    ImGui.TableNextRow(ImGuiTableRowFlags.None);
                    var firstDayOfMonth = new DateTime(viewDate.Year, viewDate.Month, 1);
                    var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
                    var daysInMonth = DateTime.DaysInMonth(viewDate.Year, viewDate.Month);

                    for (var i = 0; i < firstDayOfWeek; i++)
                    {
                        ImGui.TableNextColumn();
                        TextCentered("");
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
                            if (SelectableCentered(day.ToString(), selectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
                            {
                                currentDate = currentDay;
                            }
                            ImGui.PopStyleColor();
                        }
                        else if (isSelectable)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                            TextCentered(day.ToString());
                            ImGui.PopStyleColor();
                        }
                        else if (SelectableCentered(day.ToString(), selectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
                        {
                            currentDate = currentDay;
                            searchTimer.Restart();
                        }
                    }
                }
            }
            ImGui.EndChildFrame();
        }
    }

    private void TimeColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        var isLeftCtrl = ImGui.IsKeyDown(ImGuiKey.LeftCtrl);
        var isRightMouse = ImGui.IsMouseDown(ImGuiMouseButton.Right);
        var flag = (isLeftCtrl || isRightMouse) ? ImGuiSelectableFlags.SpanAllColumns : ImGuiSelectableFlags.None;
        var timeString = transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss");

        if ((!isLeftCtrl) ? SelectableClickToCopy(timeString, null, i) : ImGui.Selectable($"{timeString}##_{i}", ref selected, flag))
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
    }

    private void CheckAndUpdateSelectedStates(bool selected, TransactionsConvertor transaction)
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
