namespace CurrencyTracker.Windows
{
    // 数据表格 /
    // 序号列功能 (倒序排序) / 时间列功能 (按时间聚类, 按时间筛选) / 收支列功能 (按收支筛选, 按收支正负染色) / 地点列功能 / 备注列功能 / 勾选框列功能
    public partial class Main : Window, IDisposable
    {
        // 显示收支记录 Table used to show transactions
        private void TransactionTableUI()
        {
            if (selectedCurrencyID == 0)
                return;
            if (Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51])
                return;

            var childFrameHeight = ChildframeHeightAdjust();
            var childScale = new Vector2(ImGui.GetWindowWidth() - 100 - childWidthOffset, childFrameHeight);

            ImGui.SameLine();

            if (ImGui.BeginChildFrame(1, childScale, ImGuiWindowFlags.AlwaysVerticalScrollbar))
            {
                TransactionsPagingTools();

                var columns = new bool[] { C.ColumnVisibility["ShowCheckboxColumn"], C.ColumnVisibility["ShowTimeColumn"], C.ColumnVisibility["ShowAmountColumn"], C.ColumnVisibility["ShowChangeColumn"], C.ColumnVisibility["ShowOrderColumn"], C.ColumnVisibility["ShowLocationColumn"], C.ColumnVisibility["ShowNoteColumn"] };
                var columnCount = columns.Count(c => c);

                if (columnCount == 0) return;

                if (ImGui.BeginTable("Transactions", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable, new Vector2(ImGui.GetWindowWidth() - 175, 1)))
                {
                    if (C.ColumnVisibility["ShowOrderColumn"]) ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, ImGui.CalcTextSize((currentTypeTransactions.Count + 1).ToString()).X + 10, 0);
                    if (C.ColumnVisibility["ShowTimeColumn"]) ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.None, 150, 0);
                    if (C.ColumnVisibility["ShowAmountColumn"]) ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.None, 130, 0);
                    if (C.ColumnVisibility["ShowChangeColumn"]) ImGui.TableSetupColumn("Change", ImGuiTableColumnFlags.None, 100, 0);
                    if (C.ColumnVisibility["ShowLocationColumn"]) ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.None, 150, 0);
                    if (C.ColumnVisibility["ShowNoteColumn"]) ImGui.TableSetupColumn("Note", ImGuiTableColumnFlags.None, 160, 0);
                    if (C.ColumnVisibility["ShowCheckboxColumn"]) ImGui.TableSetupColumn("Selected", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 30, 0);

                    ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

                    if (C.ColumnVisibility["ShowOrderColumn"])
                    {
                        ImGui.TableNextColumn();
                        OrderColumnHeaderUI();
                    }

                    if (C.ColumnVisibility["ShowTimeColumn"])
                    {
                        ImGui.TableNextColumn();
                        TimeColumnHeaderUI();
                    }

                    if (C.ColumnVisibility["ShowAmountColumn"])
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($" {Service.Lang.GetText("Amount")}{CalcNumSpaces()}");
                    }

                    if (C.ColumnVisibility["ShowChangeColumn"])
                    {
                        ImGui.TableNextColumn();
                        ImGui.Selectable($" {Service.Lang.GetText("Change")}{CalcNumSpaces()}");
                        ChangeColumnHeaderUI();
                    }

                    if (C.ColumnVisibility["ShowLocationColumn"])
                    {
                        ImGui.TableNextColumn();
                        ImGui.Selectable($" {Service.Lang.GetText("Location")}{CalcNumSpaces()}");
                        LocationColumnHeaderUI();
                    }

                    if (C.ColumnVisibility["ShowNoteColumn"])
                    {
                        ImGui.TableNextColumn();
                        ImGui.Selectable($" {Service.Lang.GetText("Note")}{CalcNumSpaces()}");
                        NoteColumnHeaderUI();
                    }

                    if (C.ColumnVisibility["ShowCheckboxColumn"])
                    {
                        ImGui.TableNextColumn();
                        CheckboxColumnHeaderUI();
                    }

                    ImGui.TableNextRow();

                    if (currentTypeTransactions.Count <= 0)
                    {
                        ImGui.EndTable();
                        return;
                    }

                    for (var i = visibleStartIndex; i < visibleEndIndex; i++)
                    {
                        var transaction = currentTypeTransactions[i];
                        while (selectedStates[selectedCurrencyID].Count <= i)
                        {
                            selectedStates[selectedCurrencyID].Add(false);
                        }

                        var selected = selectedStates[selectedCurrencyID][i];

                        // 序号 Order Number
                        if (C.ColumnVisibility["ShowOrderColumn"])
                        {
                            ImGui.TableNextColumn();
                            OrderColumnCellUI(i, selected, transaction);
                        }

                        // 时间 Time
                        if (C.ColumnVisibility["ShowTimeColumn"])
                        {
                            ImGui.TableNextColumn();
                            TimeColumnCellUI(i, selected, transaction);
                        }

                        // 货币数 Amount
                        if (C.ColumnVisibility["ShowAmountColumn"])
                        {
                            ImGui.TableNextColumn();
                            AmountColumnCellUI(i, selected, transaction);
                        }

                        // 收支 Change
                        if (C.ColumnVisibility["ShowChangeColumn"])
                        {
                            ImGui.TableNextColumn();
                            ChangeColumnCellUI(i, selected, transaction);
                        }

                        // 地名 Location
                        if (C.ColumnVisibility["ShowLocationColumn"])
                        {
                            ImGui.TableNextColumn();
                            LocationColumnCellUI(i, selected, transaction);
                        }

                        // 备注 Note
                        if (C.ColumnVisibility["ShowNoteColumn"])
                        {
                            ImGui.TableNextColumn();
                            NoteColumnCellUI(i, selected, transaction);
                        }

                        // 勾选框 Checkboxes
                        if (C.ColumnVisibility["ShowCheckboxColumn"])
                        {
                            ImGui.TableNextColumn();
                            CheckboxColumnCellUI(i, selected, transaction);
                        }

                        ImGui.TableNextRow();
                    }

                    ImGui.EndTable();
                }

                TransactionTableInfoBarUI();

                ImGui.EndChildFrame();
            }
        }

        private void TransactionTableInfoBarUI()
        {
            if (selectedCurrencyID != 0 && selectedTransactions[selectedCurrencyID].Count > 0)
            {
                var transactions = selectedTransactions[selectedCurrencyID];
                ImGui.TextColored(ImGuiColors.DalamudGrey, Service.Lang.GetText("SelectedTransactionsInfo", transactions.Count, transactions.Sum(x => x.Change), Math.Round(transactions.Average(x => x.Change), 2), transactions.Max(x => x.Change), transactions.Min(x => x.Change)));
            }
        }

        // 序号列标题栏 Order Column Header
        private void OrderColumnHeaderUI()
        {
            ImGui.SetCursorPosX(SetColumnCenterAligned("     ", 0, 8));
            if (ImGui.ArrowButton(C.ReverseSort ? "UpSort" : "DownSort", C.ReverseSort ? ImGuiDir.Up : ImGuiDir.Down))
            {
                C.ReverseSort = !C.ReverseSort;
                C.Save();

                searchTimer.Stop();
                searchTimer.Start();
            }
        }

        // 序号列单元格 Order Column Cell
        private void OrderColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
        {
            if (C.ReverseSort)
            {
                ImGui.SetCursorPosX(SetColumnCenterAligned((currentTypeTransactions.Count - i).ToString(), 0, 8));
                ImGui.Text((currentTypeTransactions.Count - i).ToString());
            }
            else
            {
                ImGui.SetCursorPosX(SetColumnCenterAligned((i + 1).ToString(), 0, 8));
                ImGui.Text((i + 1).ToString());
            }
        }

        // 时间列标题栏 Time Column Header
        private void TimeColumnHeaderUI()
        {
            ImGui.Selectable($" {Service.Lang.GetText("Time")}{CalcNumSpaces()}");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("TimeFunctions");
            }

            if (ImGui.BeginPopup("TimeFunctions", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ClusterByTimeUI();

                FilterByTimeUI();

                ImGui.EndPopup();
            }
        }

        // 按时间聚类 Cluster by Time Interval
        private void ClusterByTimeUI()
        {
            if (ImGui.Checkbox(Service.Lang.GetText("ClusterByTime"), ref isClusteredByTime))
            {
                searchTimer.Stop();
                searchTimer.Start();
            }

            if (isClusteredByTime)
            {
                ImGui.SetNextItemWidth(115);
                if (ImGui.InputInt(Service.Lang.GetText("ClusterInterval"), ref clusterHour, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    clusterHour = Math.Max(0, clusterHour);
                    searchTimer.Stop();
                    searchTimer.Start();
                }

                ImGui.SameLine();
                ImGuiComponents.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ClusterByTimeHelp1", clusterHour)}");
            }
        }

        // 按时间筛选 Filter by Time Period
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
                DatePicker(ref filterStartDate, true);
            }

            if (endDateEnable)
            {
                DatePicker(ref filterEndDate, false);
            }
        }

        // 日期输入 Date Input
        private static void DateInput(ref DateTime date, string label, ref bool bool1, ref bool bool2)
        {
            var dateString = date.ToString("yyyy-MM-dd");

            if (ImGui.Button($"{dateString}"))
            {
                bool1 = !bool1;
                bool2 = false;
            }
            ImGui.SameLine();
            ImGui.Text($"{Service.Lang.GetText(label)}");

        }

        // 日期选择器 Date Picker
        private void DatePicker(ref DateTime currentDate, bool enableStartDate)
        {
            if (ImGui.BeginChildFrame(4, new Vector2(320, 215), ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.Separator();

                // 上一年 Last Year
                ImGui.SetCursorPosX((ImGui.GetWindowWidth()) / 8);
                if (IconButton(FontAwesomeIcon.Backward, "None", "LastYear"))
                {
                    currentDate = currentDate.AddYears(-1);
                    searchTimer.Restart();
                }

                // 上一月 Last Month
                ImGui.SameLine();
                if (ImGui.ArrowButton("LastMonth", ImGuiDir.Left))
                {
                    currentDate = currentDate.AddMonths(-1);
                    searchTimer.Restart();
                }

                ImGui.SameLine();
                ImGui.Text($"{currentDate.Year}.{string.Format("{0:MM}", currentDate)}");

                // 下一月 Next Month
                ImGui.SameLine();
                if (ImGui.ArrowButton("NextMonth", ImGuiDir.Right))
                {
                    currentDate = currentDate.AddMonths(1);
                    searchTimer.Restart();
                }

                // 下一年 Next Year
                ImGui.SameLine();
                if (IconButton(FontAwesomeIcon.Forward, "None", "NextYear"))
                {
                    currentDate = currentDate.AddYears(1);
                    searchTimer.Restart();
                }

                if (ImGui.BeginTable("DatePicker", 7, ImGuiTableFlags.NoBordersInBody))
                {
                    // 表头 Header Column
                    var weekDaysData = Service.Lang.GetText("WeekDays");
                    var weekDays = weekDaysData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var day in weekDays)
                    {
                        ImGui.TableNextColumn();
                        TextCentered(day);
                    }

                    ImGui.TableNextRow(ImGuiTableRowFlags.None);

                    var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                    var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
                    var daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);

                    // 不存在于该月的日期 Date not exsited in this month
                    for (var i = 0; i < firstDayOfWeek; i++)
                    {
                        ImGui.TableNextColumn();
                        TextCentered("");
                    }

                    // 日期绘制 Draw Dates
                    for (var day = 1; day <= daysInMonth; day++)
                    {
                        ImGui.TableNextColumn();
                        var currentDay = new DateTime(currentDate.Year, currentDate.Month, day);
                        if (currentDate.Day == day)
                        {
                            // 选中的日期 Selected Date
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.6f, 1.0f, 1.0f));
                            if (SelectableCentered(day.ToString(), selectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
                            {
                                currentDate = new DateTime(currentDate.Year, currentDate.Month, day);
                            }
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            // 其余不可选中的日期 Date that cannot be selected
                            if ((enableStartDate && currentDay >= filterEndDate) || (!enableStartDate && currentDay <= filterStartDate))
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                                TextCentered(day.ToString());
                                ImGui.PopStyleColor();
                            }
                            else
                            {
                                // 可选中的日期 Selectable Date
                                if (SelectableCentered(day.ToString(), selectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
                                {
                                    currentDate = new DateTime(currentDate.Year, currentDate.Month, day);
                                    searchTimer.Restart();
                                }
                            }
                        }
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChildFrame();
            }
        }

        // 时间列单元格 Time Column Cell
        private void TimeColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) && ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                ImGui.Selectable($"{transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")}##_{i}", selected, ImGuiSelectableFlags.SpanAllColumns);
                if (ImGui.IsItemHovered())
                {
                    selectedStates[selectedCurrencyID][i] = selected = true;

                    if (selected)
                    {
                        var exists = selectedTransactions[selectedCurrencyID].Any(t => IsTransactionEqual(t, transaction));

                        if (!exists)
                        {
                            selectedTransactions[selectedCurrencyID].Add(transaction);
                        }
                    }
                    else
                    {
                        selectedTransactions[selectedCurrencyID].RemoveAll(t => IsTransactionEqual(t, transaction));
                    }
                }
            }
            else if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
            {
                if (ImGui.Selectable($"{transaction.TimeStamp:yyyy/MM/dd HH:mm:ss}##_{i}", ref selected, ImGuiSelectableFlags.SpanAllColumns))
                {
                    selectedStates[selectedCurrencyID][i] = selected;

                    if (selected)
                    {
                        var exists = selectedTransactions[selectedCurrencyID].Any(t => IsTransactionEqual(t, transaction));

                        if (!exists)
                        {
                            selectedTransactions[selectedCurrencyID].Add(transaction);
                        }
                    }
                    else
                    {
                        selectedTransactions[selectedCurrencyID].RemoveAll(t => IsTransactionEqual(t, transaction));
                    }
                }
            }
            else
            {
                ImGui.Selectable($"{transaction.TimeStamp:yyyy/MM/dd HH:mm:ss}##_{i}");
            }

            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
            {
                ImGui.SetClipboardText(transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"));
                Service.Chat.Print($"{Service.Lang.GetText("CopiedToClipboard")}: {transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")}");
            }
        }

        // 金额列单元格 Amount Column Cell
        private void AmountColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
        {
            ImGui.Selectable($"{transaction.Amount:#,##0}##_{i}");

            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
            {
                ImGui.SetClipboardText(transaction.Amount.ToString("#,##0"));
                Service.Chat.Print($"{Service.Lang.GetText("CopiedToClipboard")}: {transaction.Amount.ToString("#,##0")}");
            }
        }

        // 收支列标题栏 Change Column Header
        private void ChangeColumnHeaderUI()
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("ChangeFunctions");
            }

            if (ImGui.BeginPopup("ChangeFunctions"))
            {
                FilterByChangeUI();
                ColoringByChangeUI();

                ImGui.EndPopup();
            }
        }

        // 按收支筛选 Filter by Change
        private void FilterByChangeUI()
        {
            if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeFilterEnabled")}##ChangeFilter", ref isChangeFilterEnabled))
            {
                searchTimer.Restart();
            }

            if (isChangeFilterEnabled)
            {
                if (ImGui.RadioButton($"{Service.Lang.GetText("Greater")}##FilterMode", ref filterMode, 0))
                {
                    searchTimer.Restart();
                }

                ImGui.SameLine();
                if (ImGui.RadioButton($"{Service.Lang.GetText("Less")}##FilterMode", ref filterMode, 1))
                {
                    searchTimer.Restart();
                }

                ImGui.SetNextItemWidth(130);
                if (ImGui.InputInt($"##FilterValue", ref filterValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    searchTimer.Restart();
                }
                ImGuiComponents.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ChangeFilterLabel", Service.Lang.GetText(filterMode == 0 ? "Greater" : filterMode == 1 ? "Less" : ""), filterValue)}");
            }
        }

        // 按收支正负染色 Coloring Text by Change
        private void ColoringByChangeUI()
        {
            if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeTextColoring")}##ChangeColoring", ref isChangeColoring))
            {
                C.ChangeTextColoring = isChangeColoring;
                C.Save();
            }

            if (isChangeColoring)
            {
                ColoringByChangeHandler("PositiveColor", Service.Lang.GetText("PositiveChange"), ref positiveChangeColor, color => C.PositiveChangeColor = color);
                ImGui.SameLine();
                ColoringByChangeHandler("NegativeColor", Service.Lang.GetText("NegativeChange"), ref negativeChangeColor, color => C.NegativeChangeColor = color);
            }
        }

        // 按收支正负染色处理器 Coloring Text by Change Handler
        private void ColoringByChangeHandler(string popupId, string text, ref Vector4 color, Action<Vector4> saveColorAction)
        {
            if (ImGui.ColorButton($"##{popupId}", color))
            {
                ImGui.OpenPopup(popupId);
            }
            ImGui.SameLine();
            ImGui.Text(text);

            if (ImGui.BeginPopup(popupId))
            {
                if (ImGui.ColorPicker4("", ref color))
                {
                    isChangeColoring = true;
                    C.ChangeTextColoring = isChangeColoring;
                    saveColorAction(color);
                    C.Save();
                }
                ImGui.EndPopup();
            }
        }

        // 收支列单元格 Change Column Cell
        private void ChangeColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
        {
            if (isChangeColoring)
            {
                if (transaction.Change > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, positiveChangeColor);
                }
                else if (transaction.Change == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, negativeChangeColor);
                }
                ImGui.Selectable(transaction.Change.ToString("+ #,##0;- #,##0;0"));
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.Selectable(transaction.Change.ToString("+ #,##0;- #,##0;0"));
            }

            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
            {
                ImGui.SetClipboardText(transaction.Change.ToString("+ #,##0;- #,##0;0"));
                Service.Chat.Print($"{Service.Lang.GetText("CopiedToClipboard")} : {transaction.Change.ToString("+ #,##0;- #,##0;0")}");
            }
        }

        // 地点列标题栏 Location Column Header
        private void LocationColumnHeaderUI()
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("LocationSearch");
            }

            if (ImGui.BeginPopup("LocationSearch"))
            {
                ImGui.SetNextItemWidth(250);
                if (ImGui.InputTextWithHint("##LocationSearch", Service.Lang.GetText("PleaseSearch"), ref searchLocationName, 80))
                {
                    isLocationFilterEnabled = !searchLocationName.IsNullOrEmpty();
                    searchTimer.Restart();
                }

                ImGui.EndPopup();
            }
        }

        // 地点列单元格 Location Column Cell
        private void LocationColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
        {
            ImGui.Selectable($"{transaction.LocationName}##_{i}");

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
            {
                ImGui.OpenPopup($"EditLocationName##_{i}");
                editedLocationName = transaction.LocationName;
            }

            if (ImGui.BeginPopup($"EditLocationName##_{i}"))
            {
                if (!editedLocationName.IsNullOrEmpty())
                {
                    ImGui.TextWrapped(editedLocationName);
                }
                ImGui.SetNextItemWidth(270);
                if (ImGui.InputText($"##EditLocationContent_{i}", ref editedLocationName, 150, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
                {
                    var filePath = Path.Combine(P.PlayerDataFolder, $"{C.AllCurrencies[selectedCurrencyID]}.txt");
                    var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyID);
                    var index = -1;

                    for (var d = 0; d < editedTransactions.Count; d++)
                    {
                        if (IsTransactionEqual(editedTransactions[d], transaction))
                        {
                            index = d;
                            break;
                        }
                    }
                    if (index != -1)
                    {
                        editedTransactions[index].LocationName = editedLocationName;
                        TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                        searchTimer.Restart();
                    }
                    else
                    {
                        Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                    }
                }

                ImGui.EndPopup();
            }
        }

        // 备注列标题栏 Note Column Header
        private void NoteColumnHeaderUI()
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("NoteSearch");
            }
            if (ImGui.BeginPopup("NoteSearch"))
            {
                ImGui.SetNextItemWidth(250);
                if (ImGui.InputTextWithHint("##NoteSearch", Service.Lang.GetText("PleaseSearch"), ref searchNoteContent, 80))
                {
                    isNoteFilterEnabled = !searchNoteContent.IsNullOrEmpty();
                    searchTimer.Restart();
                }

                ImGui.EndPopup();
            }
        }

        // 备注列单元格 Note Column Cell
        private void NoteColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
        {
            ImGui.Selectable($"{transaction.Note}##_{i}");

            if (ImGui.IsItemHovered())
            {
                if (!transaction.Note.IsNullOrEmpty())
                {
                    ImGui.SetTooltip(transaction.Note);
                }
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
            {
                ImGui.OpenPopup($"EditTransactionNote##_{i}");
                editedNoteContent = transaction.Note;
            }

            if (ImGui.BeginPopup($"EditTransactionNote##_{i}"))
            {
                if (!editedNoteContent.IsNullOrEmpty())
                {
                    ImGui.TextWrapped(editedNoteContent);
                }
                ImGui.SetNextItemWidth(270);
                if (ImGui.InputText($"##EditNoteContent_{i}", ref editedNoteContent, 150, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
                {
                    var filePath = Path.Combine(P.PlayerDataFolder, $"{C.AllCurrencies[selectedCurrencyID]}.txt");
                    var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyID);
                    var index = -1;

                    for (var d = 0; d < editedTransactions.Count; d++)
                    {
                        if (IsTransactionEqual(editedTransactions[d], transaction))
                        {
                            index = d;
                            break;
                        }
                    }
                    if (index != -1)
                    {
                        editedTransactions[index].Note = editedNoteContent;
                        TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                        searchTimer.Restart();
                    }
                    else
                    {
                        Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                    }
                }

                ImGui.EndPopup();
            }
        }

        // 勾选框列标题栏 Checkbox Column Header
        private void CheckboxColumnHeaderUI()
        {
            if (IconButton(FontAwesomeIcon.EllipsisH))
            {
                ImGui.OpenPopup("TableTools");
            }

            if (ImGui.BeginPopup("TableTools"))
            {
                CheckboxColumnToolUI();
                ImGui.EndPopup();
            }
        }

        // 勾选框列工具栏 Checkbox Column Tools
        private void CheckboxColumnToolUI()
        {
            ImGui.Text($"{Service.Lang.GetText("Now")}: {selectedTransactions[selectedCurrencyID].Count} {Service.Lang.GetText("Transactions")}");
            ImGui.Separator();

            // 取消选择 Unselect
            if (ImGui.Selectable(Service.Lang.GetText("Unselect")))
            {
                if (selectedTransactions[selectedCurrencyID].Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }
                selectedStates[selectedCurrencyID].Clear();
                selectedTransactions[selectedCurrencyID].Clear();
            }

            // 全选 Select All
            if (ImGui.Selectable(Service.Lang.GetText("SelectAll")))
            {
                selectedTransactions[selectedCurrencyID].Clear();

                foreach (var transaction in currentTypeTransactions)
                {
                    selectedTransactions[selectedCurrencyID].Add(transaction);
                }

                for (var i = 0; i < selectedStates[selectedCurrencyID].Count; i++)
                {
                    selectedStates[selectedCurrencyID][i] = true;
                }
            }

            // 反选 Inverse Select
            if (ImGui.Selectable(Service.Lang.GetText("InverseSelect")))
            {
                for (var i = 0; i < selectedStates[selectedCurrencyID].Count; i++)
                {
                    selectedStates[selectedCurrencyID][i] = !selectedStates[selectedCurrencyID][i];
                }

                foreach (var transaction in currentTypeTransactions)
                {
                    var exists = selectedTransactions[selectedCurrencyID].Any(selectedTransaction => Widgets.IsTransactionEqual(selectedTransaction, transaction));

                    if (exists)
                    {
                        selectedTransactions[selectedCurrencyID].RemoveAll(t => Widgets.IsTransactionEqual(t, transaction));
                    }
                    else
                    {
                        selectedTransactions[selectedCurrencyID].Add(transaction);
                    }
                }
            }

            // 复制 Copy
            if (ImGui.Selectable(Service.Lang.GetText("Copy")))
            {
                if (selectedTransactions[selectedCurrencyID].Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }

                var header = C.ExportDataFileType == 0 ? Service.Lang.GetText("ExportFileCSVHeader") : Service.Lang.GetText("ExportFileMDHeader1");
                var columnData = header + string.Join("\n", selectedTransactions[selectedCurrencyID].Select(record =>
                {
                    var change = $"{record.Change:+ #,##0;- #,##0;0}";
                    return C.ExportDataFileType == 0 ? $"{record.TimeStamp},{record.Amount},{change},{record.LocationName},{record.Note}" : $"{record.TimeStamp} | {record.Amount} | {change} | {record.LocationName} | {record.Note}";
                }));

                if (!columnData.IsNullOrEmpty())
                {
                    ImGui.SetClipboardText(columnData);
                    Service.Chat.Print($"{Service.Lang.GetText("CopyTransactionsHelp", selectedTransactions[selectedCurrencyID].Count)}");
                }
                else
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }
            }

            // 删除 Delete
            if (ImGui.Selectable(Service.Lang.GetText("Delete")))
            {
                if (selectedTransactions[selectedCurrencyID].Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }
                var filePath = Path.Combine(P.PlayerDataFolder, $"{C.AllCurrencies[selectedCurrencyID]}.txt");
                var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyID);
                editedTransactions.RemoveAll(t => selectedTransactions[selectedCurrencyID].Any(s => IsTransactionEqual(s, t)));
                TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                UpdateTransactions();
            }

            // 导出 Export
            if (ImGui.Selectable(Service.Lang.GetText("Export")))
            {
                if (selectedTransactions[selectedCurrencyID].Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }
                var filePath = Transactions.ExportData(selectedTransactions[selectedCurrencyID], "", selectedCurrencyID, C.ExportDataFileType);
                Service.Chat.Print($"{Service.Lang.GetText("ExportCsvMessage3")}{filePath}");
            }

            // 合并 Merge
            if (ImGui.Selectable(Service.Lang.GetText("Merge"), ref isOnMergingTT, ImGuiSelectableFlags.DontClosePopups))
            {
                if (isOnMergingTT && selectedTransactions[selectedCurrencyID].Count != 0)
                {
                    var firstTransaction = selectedTransactions[selectedCurrencyID].FirstOrDefault();
                    editedLocationName = firstTransaction?.LocationName ?? string.Empty;
                    editedNoteContent = firstTransaction?.Note ?? string.Empty;
                }
            }

            if (isOnMergingTT)
            {
                if (isOnEdit) isOnEdit = !isOnEdit;

                ImGui.Separator();

                ImGui.Text($"{Service.Lang.GetText("Location")}:");
                ImGui.SetNextItemWidth(210);
                ImGui.InputText("##MergeLocationName", ref editedLocationName, 80);

                ImGui.Text($"{Service.Lang.GetText("Note")}:");
                ImGui.SetNextItemWidth(210);
                ImGui.InputText("##MergeNoteContent", ref editedNoteContent, 150);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"{Service.Lang.GetText("MergeNoteHelp")}");
                }

                if (ImGui.SmallButton(Service.Lang.GetText("Confirm")))
                {
                    if (selectedTransactions[selectedCurrencyID].Count == 0)
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                        return;
                    }

                    if (selectedTransactions[selectedCurrencyID].Count == 1)
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("MergeTransactionsHelp4"));
                        return;
                    }

                    if (editedLocationName.IsNullOrWhitespace())
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("EditHelp1"));
                        return;
                    }

                    var mergeCount = Transactions.MergeSpecificTransactions(selectedCurrencyID, editedLocationName, selectedTransactions[selectedCurrencyID], editedNoteContent.IsNullOrEmpty() ? "-1" : editedNoteContent);
                    Service.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");

                    UpdateTransactions();
                    isOnMergingTT = false;
                }
            }

            // 编辑 Edit
            if (ImGui.Selectable(Service.Lang.GetText("Edit"), ref isOnEdit, ImGuiSelectableFlags.DontClosePopups) && isOnEdit)
            {
                var firstTransaction = selectedTransactions[selectedCurrencyID].FirstOrDefault();
                editedLocationName = firstTransaction?.LocationName ?? string.Empty;
                editedNoteContent = firstTransaction?.Note ?? string.Empty;
            }

            if (isOnEdit)
            {
                if (isOnMergingTT) isOnMergingTT = !isOnMergingTT;

                ImGui.Separator();

                ImGui.Text($"{Service.Lang.GetText("Location")}:");
                ImGui.SetNextItemWidth(210);
                if (ImGui.InputTextWithHint("##EditLocationName", Service.Lang.GetText("EditHelp"), ref editedLocationName, 80, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (selectedTransactions[selectedCurrencyID].Count == 0)
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                        return;
                    }

                    if (editedLocationName.IsNullOrWhitespace())
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("EditHelp1"));
                        return;
                    }

                    var filePath = Path.Combine(P.PlayerDataFolder, $"{C.AllCurrencies[selectedCurrencyID]}.txt");
                    var failCounts = 0;

                    foreach (var selectedTransaction in selectedTransactions[selectedCurrencyID])
                    {
                        var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyID);

                        var index = -1;
                        for (var i = 0; i < editedTransactions.Count; i++)
                        {
                            if (IsTransactionEqual(editedTransactions[i], selectedTransaction))
                            {
                                index = i;
                                break;
                            }
                        }

                        if (index != -1)
                        {
                            editedTransactions[index].LocationName = editedLocationName;
                            TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                        }
                        else
                        {
                            failCounts++;
                        }
                    }

                    if (failCounts == 0)
                    {
                        Service.Chat.Print($"{Service.Lang.GetText("EditLocationHelp", selectedTransactions[selectedCurrencyID].Count)}");

                        UpdateTransactions();
                    }
                    else if (failCounts > 0 && failCounts < selectedTransactions[selectedCurrencyID].Count)
                    {
                        Service.Chat.Print($"{Service.Lang.GetText("EditLocationHelp", selectedTransactions[selectedCurrencyID].Count - failCounts)}");
                        Service.Chat.PrintError($"({Service.Lang.GetText("EditFailed")}: {failCounts})");

                        UpdateTransactions();
                    }
                    else
                    {
                        Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                    }
                }

                ImGui.Text($"{Service.Lang.GetText("Note")}:");
                ImGui.SetNextItemWidth(210);
                if (ImGui.InputTextWithHint("##EditNoteContent", Service.Lang.GetText("EditHelp"), ref editedNoteContent, 80, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (selectedTransactions[selectedCurrencyID].Count == 0)
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                        return;
                    }

                    var filePath = Path.Combine(P.PlayerDataFolder, $"{C.AllCurrencies[selectedCurrencyID]}.txt");
                    var failCounts = 0;

                    foreach (var selectedTransaction in selectedTransactions[selectedCurrencyID])
                    {
                        var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyID);

                        var index = -1;
                        for (var i = 0; i < editedTransactions.Count; i++)
                        {
                            if (IsTransactionEqual(editedTransactions[i], selectedTransaction))
                            {
                                index = i;
                                break;
                            }
                        }

                        if (index != -1)
                        {
                            editedTransactions[index].Note = editedNoteContent;
                            TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                        }
                        else
                        {
                            failCounts++;
                        }
                    }

                    if (failCounts == 0)
                    {
                        Service.Chat.Print($"{Service.Lang.GetText("EditHelp2")} {selectedTransactions[selectedCurrencyID].Count} {Service.Lang.GetText("EditHelp4")} {editedNoteContent}");

                        UpdateTransactions();
                    }
                    else if (failCounts > 0 && failCounts < selectedTransactions[selectedCurrencyID].Count)
                    {
                        Service.Chat.Print($"{Service.Lang.GetText("EditHelp2")} {selectedTransactions[selectedCurrencyID].Count - failCounts} {Service.Lang.GetText("EditHelp3")} {editedLocationName}");
                        Service.Chat.PrintError($"({Service.Lang.GetText("EditFailed")}: {failCounts})");

                        UpdateTransactions();
                    }
                    else
                    {
                        Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                    }
                }

                if (!editedNoteContent.IsNullOrEmpty())
                {
                    ImGui.TextWrapped(editedNoteContent);
                }
            }
        }

        // 勾选框列单元格 Checkbox Column Cell
        private void CheckboxColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
        {
            if (ImGui.Checkbox($"##select_{i}", ref selected))
            {
                selectedStates[selectedCurrencyID][i] = selected;

                if (selected)
                {
                    var exists = selectedTransactions[selectedCurrencyID].Any(t => IsTransactionEqual(t, transaction));

                    if (!exists)
                    {
                        selectedTransactions[selectedCurrencyID].Add(transaction);
                    }
                }
                else
                {
                    selectedTransactions[selectedCurrencyID].RemoveAll(t => IsTransactionEqual(t, transaction));
                }
            }
        }
    }
}
