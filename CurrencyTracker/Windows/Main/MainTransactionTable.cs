namespace CurrencyTracker.Windows
{
    // 数据表格 / 数据表格翻页组件 / 数据表格信息栏
    // 序号列功能 (倒序排序) / 时间列功能 (按时间聚类, 按时间筛选) / 收支列功能 (按收支筛选, 按收支正负染色) / 地点列功能 / 备注列功能 / 勾选框列功能
    public partial class Main : Window, IDisposable
    {

        // 收支记录表格 Table used to show transactions
        private void TransactionTableUI()
        {
            if (selectedCurrencyID == 0) return;

            var windowWidth = ImGui.GetWindowWidth() - C.ChildWidthOffset - 100;

            ImGui.SameLine();

            if (ImGui.BeginChildFrame(1, new Vector2(windowWidth, ChildframeHeightAdjust()), ImGuiWindowFlags.AlwaysVerticalScrollbar))
            {
                TransactionTablePagingUI(windowWidth);

                var orderColumnWidth = (int)ImGui.CalcTextSize((currentTypeTransactions.Count + 1).ToString()).X + 10;

                var columnCount = C.ColumnsVisibility.Count(c => c.Value);
                if (columnCount == 0) return;

                using(var table = ImRaii.Table("Transactions", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable, new Vector2(windowWidth - 175, 1)))
                {
                    if (table)
                    {
                        foreach (var column in C.ColumnsVisibility)
                        {
                            if (!column.Value) continue;
                            var flags = column.Key == "Order" || column.Key == "Checkbox" ? ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize : ImGuiTableColumnFlags.None;
                            var width = column.Key == "Order" ? ImGui.CalcTextSize((currentTypeTransactions.Count + 1).ToString()).X + 10 : column.Key == "Checkbox" ? 30 : 150;
                            ImGui.TableSetupColumn(column.Key, flags, width, 0);
                        }

                        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                        foreach (var column in C.ColumnsVisibility)
                        {
                            if (!column.Value) continue;
                            ImGui.TableNextColumn();
                            if (ColumnHeaderActions.TryGetValue(column.Key, out var headerAction)) headerAction.Invoke();
                        }

                        if (currentTypeTransactions != null && currentTypeTransactions.Any())
                        {
                            while (selectedStates[selectedCurrencyID].Count < currentTypeTransactions.Count)
                            {
                                selectedStates[selectedCurrencyID].Add(false);
                            }

                            ImGui.TableNextRow();
                            for (var i = visibleStartIndex; i < visibleEndIndex; i++)
                            {
                                foreach (var column in C.ColumnsVisibility)
                                {
                                    if (!column.Value) continue;
                                    ImGui.TableNextColumn();
                                    if (ColumnCellActions.TryGetValue(column.Key, out var cellAction)) cellAction.Invoke(i, selectedStates[selectedCurrencyID][i], currentTypeTransactions[i]);
                                }
                                ImGui.TableNextRow();
                            }

                            TransactionTableInfoBarUI();
                        }
                    }
                }

                ImGui.EndChildFrame();
            }
        }

        // 表格翻页组件 Table Paging Components
        private void TransactionTablePagingUI(float windowWidth)
        {
            var pageCount = (currentTypeTransactions.Any()) ? (int)Math.Ceiling((double)currentTypeTransactions.Count / C.RecordsPerPage) : 0;
            currentPage = (pageCount > 0) ? Math.Clamp(currentPage, 0, pageCount - 1) : 0;

            // 视图切换 Table View Switch
            ImGui.SetCursorPosX((windowWidth / 3));
            TableViewSwitchUI();

            // 首页 First Page
            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.Backward)) currentPage = 0;

            // 上一页 Last Page
            ImGui.SameLine();
            if (ImGui.ArrowButton("PreviousPage", ImGuiDir.Left) && currentPage > 0) currentPage--;

            // 页数显示 Pages
            ImGui.SameLine();
            ImGui.Text($"{Service.Lang.GetText("PageComponent", currentPage + 1, pageCount)}");

            // 下一页 Next Page
            ImGui.SameLine();
            if (ImGui.ArrowButton("NextPage", ImGuiDir.Right) && currentPage < pageCount - 1) currentPage++;

            // 尾页 Final Page
            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.Forward) && currentPage >= 0) currentPage = pageCount;

            // 表格外观 Table Appearence
            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.Table, Service.Lang.GetText("TableAppearance"), "TableAppearance")) ImGui.OpenPopup("TableAppearence");

            if (ImGui.BeginPopup("TableAppearence"))
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ColumnsDisplayed")}:");

                ColumnDisplayCheckbox("Time");
                ImGui.SameLine();
                ColumnDisplayCheckbox("Amount");
                ImGui.SameLine();
                ColumnDisplayCheckbox("Change");

                ColumnDisplayCheckbox("Order");
                ImGui.SameLine();
                ColumnDisplayCheckbox("Location");
                ImGui.SameLine();
                ColumnDisplayCheckbox("Note");
                ImGui.SameLine();
                ColumnDisplayCheckbox("Checkbox");

                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ChildframeWidthOffset")}:");

                ImGui.SetNextItemWidth(150f);
                ImGui.SameLine();
                var childWidthOffset = C.ChildWidthOffset;
                if (ImGui.InputInt("##ChildframesWidthOffset", ref childWidthOffset, 10))
                {
                    childWidthOffset = Math.Max(-240, Math.Min(childWidthOffset, (int)windowWidth - 700));
                    C.ChildWidthOffset = childWidthOffset;
                    C.Save();
                }

                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("TransactionsPerPage")}:");

                ImGui.SetNextItemWidth(150);
                ImGui.SameLine();
                var transactionsPerPage = C.RecordsPerPage;
                if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
                {
                    transactionsPerPage = Math.Max(transactionsPerPage, 1);
                    C.RecordsPerPage = transactionsPerPage;
                    C.Save();
                }

                ImGui.EndPopup();
            }

            visibleStartIndex = currentPage * C.RecordsPerPage;
            visibleEndIndex = Math.Min(visibleStartIndex + C.RecordsPerPage, currentTypeTransactions.Count);

            // 鼠标滚轮控制 Logic controlling Mouse Wheel Filpping
            {
                if (!ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
                {
                    if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel > 0 && currentPage > 0)
                        currentPage--;

                    if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel < 0 && currentPage < pageCount - 1)
                        currentPage++;
                }
            }
        }

        // 切换数据表格视图 Switch the view of the transaction table
        private void TableViewSwitchUI()
        {
            if (IconButton(FontAwesomeIcon.Bars, "None"))
            {
                ImGui.OpenPopup("TableViewSwitch");
            }

            if (ImGui.BeginPopup("TableViewSwitch"))
            {
                var boolUI = false;
                if (ImGui.Selectable(Service.Lang.GetText("Inventory"), boolUI, ImGuiSelectableFlags.DontClosePopups))
                {
                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, 0, 0));
                }

                foreach(var retainer in C.CharacterRetainers[P.CurrentCharacter.ContentID])
                {
                    if (ImGui.Selectable($"{retainer.Value}##{retainer.Key}", boolUI, ImGuiSelectableFlags.DontClosePopups))
                    {
                        currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, TransactionFileCategory.Retainer, retainer.Key));
                    }
                }

                if (ImGui.Selectable(Service.Lang.GetText("SaddleBag"), boolUI, ImGuiSelectableFlags.DontClosePopups))
                {
                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, TransactionFileCategory.SaddleBag, 0));
                    currentView = TransactionFileCategory.SaddleBag;
                    currentViewID = 0;
                }

                if (ImGui.Selectable(Service.Lang.GetText("PSaddleBag"), boolUI, ImGuiSelectableFlags.DontClosePopups))
                {
                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, TransactionFileCategory.PremiumSaddleBag, 0));
                }
                ImGui.EndPopup();
            }
        }

        // 显示列勾选框 Displayed Column Checkbox
        private void ColumnDisplayCheckbox(string boolName)
        {
            var isShowColumn = C.ColumnsVisibility[boolName];
            if (ImGui.Checkbox($"{Service.Lang.GetText(boolName)}##Display{boolName}Column", ref isShowColumn))
            {
                C.ColumnsVisibility[boolName] = isShowColumn;
                C.Save();
            }
        }

        // 表格信息栏 Table Info Bar
        private void TransactionTableInfoBarUI()
        {
            if (selectedCurrencyID != 0 && selectedTransactions[selectedCurrencyID].Any())
            {
                var transactions = selectedTransactions[selectedCurrencyID];
                ImGui.TextColored(ImGuiColors.DalamudGrey, Service.Lang.GetText("SelectedTransactionsInfo", transactions.Count, transactions.Sum(x => x.Change), Math.Round(transactions.Average(x => x.Change), 2), transactions.Max(x => x.Change), transactions.Min(x => x.Change)));
            }
        }

        // 序号列标题栏 Order Column Header
        private void OrderColumnHeaderUI()
        {
            ImGui.SetCursorPosX(1f);
            if (SelectableIconButton(C.ReverseSort ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown, "", "OrderControl", new Vector2(ImGui.GetContentRegionAvail().X + 10f, 20.0f * ImGuiHelpers.GlobalScale)))
            {
                C.ReverseSort = !C.ReverseSort;
                C.Save();

                searchTimer.Restart();
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
            ImGui.Selectable($" {Service.Lang.GetText("Time")}");
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
                if (ImGui.InputInt(Service.Lang.GetText("Hours"), ref clusterHour, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
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
                SelectableClickToCopy(transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"), transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"), i);
            }
        }

        // 金额列标题栏 Amount Column Header
        private void AmountColumnHeaderUI()
        {
            ImGui.Text($" {Service.Lang.GetText("Amount")}");
        }

        // 金额列单元格 Amount Column Cell
        private void AmountColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
        {
            SelectableClickToCopy(transaction.Amount.ToString("#,##0"), null, i);
        }

        // 收支列标题栏 Change Column Header
        private void ChangeColumnHeaderUI()
        {
            ImGui.Selectable($" {Service.Lang.GetText("Change")}");
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
            var isChangeColoring = C.ChangeTextColoring;
            if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeTextColoring")}##ChangeColoring", ref isChangeColoring))
            {
                C.ChangeTextColoring = isChangeColoring;
                C.Save();
            }

            if (C.ChangeTextColoring)
            {
                var positiveChangeColor = C.PositiveChangeColor;
                var negativeChangeColor = C.NegativeChangeColor;

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
                    C.ChangeTextColoring = true;
                    saveColorAction(color);
                    C.Save();
                }
                ImGui.EndPopup();
            }
        }

        // 收支列单元格 Change Column Cell
        private void ChangeColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
        {
            var textColor = C.ChangeTextColoring
                ? transaction.Change > 0 ? C.PositiveChangeColor : transaction.Change < 0 ? C.NegativeChangeColor : new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                : new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

            ImGui.PushStyleColor(ImGuiCol.Text, textColor);
            SelectableClickToCopy(transaction.Change.ToString("+ #,##0;- #,##0;0"), null, i);
            ImGui.PopStyleColor();
        }

        // 地点列标题栏 Location Column Header
        private void LocationColumnHeaderUI()
        {
            ImGui.Selectable($" {Service.Lang.GetText("Location")}");
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

            if (!transaction.LocationName.IsNullOrEmpty())
            {
                TextTooltip(transaction.LocationName);
            }

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
                    var failCount = Transactions.EditSpecificTransactions(selectedCurrencyID, new List<TransactionsConvertor> { transaction }, editedLocationName, "", currentView, currentViewID);

                    if (failCount == 0)
                    {
                        searchTimer.Restart();
                    }
                    else
                    {
                        Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                        Service.Log.Debug($"{currentView} {currentViewID}");
                    }
                }

                ImGui.EndPopup();
            }
        }

        // 备注列标题栏 Note Column Header
        private void NoteColumnHeaderUI()
        {
            ImGui.Selectable($" {Service.Lang.GetText("Note")}");
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

            if (!transaction.Note.IsNullOrEmpty())
            {
                TextTooltip(transaction.Note);
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
                    var failCount = Transactions.EditSpecificTransactions(selectedCurrencyID, new List<TransactionsConvertor> { transaction }, "", editedNoteContent, currentView, currentViewID);

                    if (failCount == 0)
                    {
                        searchTimer.Restart();
                    }
                    else
                    {
                        Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                        Service.Log.Debug($"{currentView} {currentViewID}");
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

        // 勾选框列工具栏 Checkbox Column Tools (CBCT)
        private void CheckboxColumnToolUI()
        {
            ImGui.Text($"{Service.Lang.GetText("Now")}: {selectedTransactions[selectedCurrencyID].Count} {Service.Lang.GetText("Transactions")}");
            ImGui.Separator();

            UnselectCBCTUI();
            SelectAllCBCTUI();
            InverseSelectCBCTUI();
            CopyCBCTUI();
            DeleteCBCTUI();
            ExportCBCTUI();
            MergeCBCTUI();
            EditCBCTUI();
        }

        // 取消选择 Unselect
        private void UnselectCBCTUI()
        {
            if (ImGui.Selectable(Service.Lang.GetText("Unselect")) )
            {
                if (selectedTransactions[selectedCurrencyID].Any())
                {
                    selectedStates[selectedCurrencyID].Clear();
                    selectedTransactions[selectedCurrencyID].Clear();
                }
                else
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                }
            }
        }

        // 全选 Select All
        private void SelectAllCBCTUI()
        {
            if (ImGui.Selectable(Service.Lang.GetText("SelectAll")))
            {
                selectedTransactions[selectedCurrencyID] = new List<TransactionsConvertor>(currentTypeTransactions);
                selectedStates[selectedCurrencyID] = Enumerable.Repeat(true, selectedStates[selectedCurrencyID].Count).ToList();
            }
        }

        // 反选 Inverse Select
        private void InverseSelectCBCTUI()
        {
            if (ImGui.Selectable(Service.Lang.GetText("InverseSelect")))
            {
                selectedStates[selectedCurrencyID] = selectedStates[selectedCurrencyID].Select(state => !state).ToList();

                var newTransactions = new List<TransactionsConvertor>();
                foreach (var transaction in currentTypeTransactions)
                {
                    if (!selectedTransactions[selectedCurrencyID].Any(t => IsTransactionEqual(t, transaction)))
                    {
                        newTransactions.Add(transaction);
                    }
                }
                selectedTransactions[selectedCurrencyID] = newTransactions;
            }
        }

        // 复制 Copy
        private void CopyCBCTUI()
        {
            if (ImGui.Selectable(Service.Lang.GetText("Copy")))
            {
                if (selectedTransactions[selectedCurrencyID].Any())
                {
                    var header = C.ExportDataFileType == 0 ? Service.Lang.GetText("ExportFileCSVHeader") : Service.Lang.GetText("ExportFileMDHeader1");
                    var columnData = $"{header}\n" + string.Join("\n", selectedTransactions[selectedCurrencyID].Select(record =>
                    {
                        var change = $"{record.Change:+ #,##0;- #,##0;0}";
                        return C.ExportDataFileType == 0 ? $"{record.TimeStamp},{record.Amount},{change},{record.LocationName},{record.Note}" : $"| {record.TimeStamp} | {record.Amount} | {change} | {record.LocationName} | {record.Note}";
                    }));

                    ImGui.SetClipboardText(columnData);
                    Service.Chat.Print($"{Service.Lang.GetText("CopyTransactionsHelp", selectedTransactions[selectedCurrencyID].Count)}");
                }
                else
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                }
            }
        }

        // 删除 Delete
        private void DeleteCBCTUI()
        {
            if (ImGui.Selectable(Service.Lang.GetText("Delete")))
            {
                if (selectedTransactions[selectedCurrencyID].Any())
                {
                    var filePath = GetTransactionFilePath(selectedCurrencyID, currentView, currentViewID);
                    var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyID, currentView, currentViewID);
                    editedTransactions.RemoveAll(t => selectedTransactions[selectedCurrencyID].Any(s => IsTransactionEqual(s, t)));
                    TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                    UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
                }
                else
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                }
            }
        }

        // 导出 Export
        private void ExportCBCTUI()
        {
            if (ImGui.Selectable(Service.Lang.GetText("Export")))
            {
                if (selectedTransactions[selectedCurrencyID].Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }
                var filePath = Transactions.ExportData(selectedTransactions[selectedCurrencyID], "", selectedCurrencyID, C.ExportDataFileType, currentView, currentViewID);
                Service.Chat.Print($"{Service.Lang.GetText("ExportFileMessage")}{filePath}");
            }
        }

        // 合并 Merge
        private void MergeCBCTUI()
        {
            if (ImGui.Selectable(Service.Lang.GetText("Merge"), ref isOnMergingTT, ImGuiSelectableFlags.DontClosePopups))
            {
                if (isOnMergingTT && selectedTransactions[selectedCurrencyID].Any())
                {
                    var firstTransaction = selectedTransactions[selectedCurrencyID].FirstOrDefault(t => !t.LocationName.IsNullOrEmpty() && !t.Note.IsNullOrEmpty());
                    if (firstTransaction != null)
                    {
                        editedLocationName = firstTransaction.LocationName;
                        editedNoteContent = firstTransaction.Note;
                    }
                }
            }

            if (!isOnMergingTT) return;

            if (isOnEdit) isOnEdit = !isOnEdit;

            ImGui.Separator();

            ImGui.Text($"{Service.Lang.GetText("Location")}:");
            ImGui.SetNextItemWidth(210);
            ImGui.InputText("##MergeLocationName", ref editedLocationName, 80);

            ImGui.Text($"{Service.Lang.GetText("Note")}:");
            ImGui.SetNextItemWidth(210);
            ImGui.InputText("##MergeNoteContent", ref editedNoteContent, 150);

            if (ImGui.SmallButton(Service.Lang.GetText("Confirm")))
            {
                if (selectedTransactions[selectedCurrencyID].Count < 2 || editedLocationName.IsNullOrWhitespace())
                {
                    return;
                }

                var mergeCount = Transactions.MergeSpecificTransactions(selectedCurrencyID, editedLocationName, selectedTransactions[selectedCurrencyID], editedNoteContent.IsNullOrEmpty() ? "-1" : editedNoteContent, currentView, currentViewID);
                Service.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");

                UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
                isOnMergingTT = false;
            }
        }

        // 编辑 Edit
        private void EditCBCTUI()
        {
            if (ImGui.Selectable(Service.Lang.GetText("Edit"), ref isOnEdit, ImGuiSelectableFlags.DontClosePopups))
            {
                if (selectedTransactions[selectedCurrencyID].Any())
                {
                    if (isOnEdit)
                    {
                        var firstTransaction = selectedTransactions[selectedCurrencyID].FirstOrDefault(t => !t.LocationName.IsNullOrEmpty() && !t.Note.IsNullOrEmpty());
                        if (firstTransaction != null)
                        {
                            editedLocationName = firstTransaction.LocationName;
                            editedNoteContent = firstTransaction.Note;
                        }
                        if (isOnMergingTT) isOnMergingTT = !isOnMergingTT;
                    }
                }
                else
                {
                    isOnEdit = false;
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }
            }

            if (!isOnEdit) return;

            ImGui.Separator();

            ImGui.Text($"{Service.Lang.GetText("Location")}:");

            ImGui.SetNextItemWidth(210);
            if (ImGui.InputTextWithHint("##EditLocationName", Service.Lang.GetText("PressEnterToConfirm"), ref editedLocationName, 80, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                EditLocationName();
            }

            ImGui.Text($"{Service.Lang.GetText("Note")}:");

            ImGui.SetNextItemWidth(210);
            if (ImGui.InputTextWithHint("##EditNoteContent", Service.Lang.GetText("PressEnterToConfirm"), ref editedNoteContent, 80, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                EditNoteContent();
            }

            if (!editedNoteContent.IsNullOrEmpty())
            {
                ImGui.TextWrapped(editedNoteContent);
            }
        }

        // 编辑地名 Edit Location Name
        private void EditLocationName()
        {
            if (editedLocationName.IsNullOrWhitespace())
            {
                return;
            }

            var failCount = Transactions.EditSpecificTransactions(selectedCurrencyID, selectedTransactions[selectedCurrencyID], editedLocationName, "", currentView, currentViewID);

            EditResultHandler(failCount, editedLocationName, "");
        }

        // 编辑备注 Edit Note Content
        private void EditNoteContent()
        {
            if (editedNoteContent.IsNullOrWhitespace())
            {
                return;
            }

            var failCount = Transactions.EditSpecificTransactions(selectedCurrencyID, selectedTransactions[selectedCurrencyID], "", editedNoteContent, currentView, currentViewID);

            EditResultHandler(failCount, "", editedNoteContent);
        }

        // 编辑结果处理 Handle Eidt Result
        private void EditResultHandler(int failCount, string locationName = "", string noteContent = "")
        {
            if (failCount == 0)
            {
                Service.Chat.Print(Service.Lang.GetText("EditHelp1", selectedTransactions[selectedCurrencyID].Count, locationName.IsNullOrEmpty() ? Service.Lang.GetText("Note") : Service.Lang.GetText("Location")) + " " + (locationName.IsNullOrEmpty() ? noteContent : locationName));

                UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
            }
            else if (failCount > 0 && failCount < selectedTransactions[selectedCurrencyID].Count)
            {
                Service.Chat.Print(Service.Lang.GetText("EditHelp1", selectedTransactions[selectedCurrencyID].Count - failCount, locationName.IsNullOrEmpty() ? Service.Lang.GetText("Note") : Service.Lang.GetText("Location")) + " " +(locationName.IsNullOrEmpty() ? noteContent : locationName));
                Service.Chat.PrintError($"({Service.Lang.GetText("EditFailed")}: {failCount})");

                UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
            }
            else
            {
                Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
            }

            isOnEdit = false;
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
