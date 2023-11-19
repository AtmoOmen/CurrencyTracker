using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Trackers;
using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using TinyPinyin;

namespace CurrencyTracker.Windows;

public partial class Main
{
    // 用于处理选项增减 Used to handle options changes.
    private void ReloadOrderedOptions()
    {
        var orderedOptionsSet = new HashSet<string>(C.OrdedOptions);
        var allCurrenciesSet = new HashSet<string>(C.AllCurrencies.Keys);

        if (!orderedOptionsSet.SetEquals(allCurrenciesSet))
        {
            orderedOptionsSet.UnionWith(allCurrenciesSet);
            orderedOptionsSet.IntersectWith(allCurrenciesSet);

            ordedOptions = orderedOptionsSet.ToList();

            C.OrdedOptions = ordedOptions;
            C.Save();
        }
    }

    // 用于处理选项位置变化 Used to handle option's position change.
    private void SwapOptions(int index1, int index2)
    {
        (ordedOptions[index2], ordedOptions[index1]) = (ordedOptions[index1], ordedOptions[index2]);

        C.OrdedOptions = ordedOptions;
        C.Save();
    }

    // 按收支隐藏不符合要求的交易记录 Hide Unmatched Transactions By Change
    private List<TransactionsConvertor> ApplyChangeFilter(List<TransactionsConvertor> transactions)
    {
        var filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            var isTransactionValid = filterMode == 0 ?
                transaction.Change > filterValue :
                transaction.Change < filterValue;

            if (isTransactionValid)
            {
                filteredTransactions.Add(transaction);
            }
        }
        return filteredTransactions;
    }

    // 按时间间隔聚类交易记录 Cluster Transactions By Interval
    private static List<TransactionsConvertor> ClusterTransactionsByTime(List<TransactionsConvertor> transactions, TimeSpan interval)
    {
        var clusteredTransactions = new Dictionary<DateTime, TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            var clusterTime = transaction.TimeStamp.AddTicks(-(transaction.TimeStamp.Ticks % interval.Ticks));
            if (!clusteredTransactions.TryGetValue(clusterTime, out var cluster))
            {
                cluster = new TransactionsConvertor
                {
                    TimeStamp = clusterTime,
                    Amount = 0,
                    Change = 0,
                    LocationName = string.Empty
                };
                clusteredTransactions.Add(clusterTime, cluster);
            }

            if (!transaction.LocationName.IsNullOrEmpty() && !transaction.LocationName.Equals(Service.Lang.GetText("UnknownLocation")))
            {
                if (string.IsNullOrWhiteSpace(cluster.LocationName))
                {
                    cluster.LocationName = transaction.LocationName;
                }
                else
                {
                    var locationNames = cluster.LocationName.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (locationNames.Length < 3)
                    {
                        cluster.LocationName += $", {transaction.LocationName}";
                    }
                }
            }

            cluster.Change += transaction.Change;

            if (cluster.TimeStamp <= transaction.TimeStamp)
            {
                cluster.Amount = transaction.Amount;
            }
        }

        foreach (var cluster in clusteredTransactions.Values)
        {
            if (!cluster.LocationName.EndsWith("..."))
            {
                cluster.LocationName = cluster.LocationName.TrimEnd() + "...";
            }
        }

        return clusteredTransactions.Values.ToList();
    }

    // 按时间显示交易记录 Hide Unmatched Transactions By Time
    private List<TransactionsConvertor> ApplyDateTimeFilter(List<TransactionsConvertor> transactions)
    {
        var filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            if (transaction.TimeStamp >= filterStartDate && transaction.TimeStamp <= filterEndDate.AddDays(1))
            {
                filteredTransactions.Add(transaction);
            }
        }
        return filteredTransactions;
    }

    // 按地点名显示交易记录 Hide Unmatched Transactions By Location
    private List<TransactionsConvertor> ApplyLocationFilter(List<TransactionsConvertor> transactions, string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return transactions;
        }

        var queries = query.Split(',')
                           .Select(q => q.Trim().Normalize(NormalizationForm.FormKC))
                           .Where(q => !string.IsNullOrEmpty(q))
                           .ToList();
        var isChineseSimplified = C.SelectedLanguage == "ChineseSimplified";
        var filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            var normalizedLocation = transaction.LocationName.Normalize(NormalizationForm.FormKC);

            if (queries.Any(q => normalizedLocation.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                filteredTransactions.Add(transaction);
            }
            else if (isChineseSimplified)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedLocation, "");
                if (queries.Any(q => pinyin.Contains(q, StringComparison.OrdinalIgnoreCase)))
                {
                    filteredTransactions.Add(transaction);
                }
            }
        }

        return filteredTransactions;
    }


    // 按备注显示交易记录 Hide Unmatched Transactions By Note
    private List<TransactionsConvertor> ApplyNoteFilter(List<TransactionsConvertor> transactions, string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return transactions;
        }

        var queries = query.Split(',')
                           .Select(q => q.Trim().Normalize(NormalizationForm.FormKC))
                           .Where(q => !string.IsNullOrEmpty(q))
                           .ToList();
        var isChineseSimplified = C.SelectedLanguage == "ChineseSimplified";
        var filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            var normalizedNote = transaction.Note.Normalize(NormalizationForm.FormKC);

            if (queries.Any(q => normalizedNote.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                filteredTransactions.Add(transaction);
            }
            else if (isChineseSimplified)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedNote, "");
                if (queries.Any(q => pinyin.Contains(q, StringComparison.OrdinalIgnoreCase)))
                {
                    filteredTransactions.Add(transaction);
                }
            }
        }

        return filteredTransactions;
    }

    // 初始化自定义货币追踪内的物品 Initialize Items in Custom Currency Tracker
    private List<string> InitCCTItems()
    {
        var itemNamesSet = new HashSet<string>(Tracker.ItemNamesSet, StringComparer.OrdinalIgnoreCase);

        var items = itemNamesSet
            .Where(itemName => !C.AllCurrencies.Values.Any(option => itemName.Equals(CurrencyInfo.CurrencyLocalName(option))) && !filterNamesForCCT.Any(filter => itemName.Equals(filter)))
            .ToList();

        CCTItemCounts = (uint)items.Count;
        return items;
    }

    // 按搜索结果显示自定义货币追踪里的物品 Show On-Demand Items Based On Filter
    private List<string> ApplyCCTFilter(string searchFilter)
    {
        if (CCTItemNames.Count > 0)
        {
            return CCTItemNames.Where(itemName => itemName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) || (C.SelectedLanguage == "ChineseSimplified" && PinyinHelper.GetPinyin(itemName, "").Contains(searchFilter, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        else
        {
            return Tracker.ItemNamesSet.Where(itemName => itemName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) && C.AllCurrencies.Values.All(option => !itemName.Contains(CurrencyInfo.CurrencyLocalName(option))) && !filterNamesForCCT.Any(filter => itemName.Contains(filter)))
                .ToList();
        }
    }

    // 延迟加载收支记录 Used to handle too-fast transactions loading
    private void SearchTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        UpdateTransactions();
    }

    // 延迟加载搜索结果 Used to handle too-fast CCT items loading
    private void SearchTimerCCTElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!searchFilter.IsNullOrEmpty())
        {
            currentItemPage = 0;
            CCTItemNames = ApplyCCTFilter(searchFilter);
        }
        else
        {
            CCTItemNames = InitCCTItems();
        }
    }

    // 合并交易记录用 Used to simplified merging transactions code
    private int MergeTransactions(bool oneWay)
    {
        if (string.IsNullOrEmpty(selectedCurrencyName))
        {
            Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
            return 0;
        }

        var threshold = (mergeThreshold == 0) ? int.MaxValue : mergeThreshold;
        var mergeCount = Transactions.MergeTransactionsByLocationAndThreshold(selectedCurrencyName, threshold, oneWay);

        if (mergeCount > 0)
            Service.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");
        else
            Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp"));

        UpdateTransactions();
        return mergeCount;
    }

    // 调整列表框和表格高度用 Used to adjust the height of listbox and chart
    private float ChildframeHeightAdjust()
    {
        var trueCount = Convert.ToInt32(showOthers) + Convert.ToInt32(showRecordOptions);
        var ChildFrameHeight = ImGui.GetWindowHeight() - 245;

        if (trueCount == 2) ChildFrameHeight = ImGui.GetWindowHeight() - 185;
        if (trueCount == 1) ChildFrameHeight = ImGui.GetWindowHeight() - 150;
        if (trueCount == 0) ChildFrameHeight = ImGui.GetWindowHeight() - 85;

        return ChildFrameHeight;
    }

    // 调整文本长度用 Used to adjust the length of the text in header columns.
    private static string CalcNumSpaces()
    {
        var fontSize = ImGui.GetFontSize() / 2;
        var numSpaces = (int)(ImGui.GetColumnWidth() / fontSize);
        var spaces = new string('　', numSpaces);

        return spaces;
    }

    // 应用筛选器 Apply Filters
    internal List<TransactionsConvertor> ApplyFilters(List<TransactionsConvertor> currentTypeTransactions)
    {
        if (isClusteredByTime && clusterHour > 0)
        {
            var interval = TimeSpan.FromHours(clusterHour);
            currentTypeTransactions = ClusterTransactionsByTime(currentTypeTransactions, interval);
        }

        if (isChangeFilterEnabled)
            currentTypeTransactions = ApplyChangeFilter(currentTypeTransactions);

        if (isTimeFilterEnabled)
            currentTypeTransactions = ApplyDateTimeFilter(currentTypeTransactions);

        if (isLocationFilterEnabled)
            currentTypeTransactions = ApplyLocationFilter(currentTypeTransactions, searchLocationName);

        if (isNoteFilterEnabled)
            currentTypeTransactions = ApplyNoteFilter(currentTypeTransactions, searchNoteContent);

        if (isReversed)
        {
            currentTypeTransactions = currentTypeTransactions.OrderByDescending(item => item.TimeStamp).ToList();
        }

        return currentTypeTransactions;
    }

    // 日期筛选器 Date Picker
    private void CreateDatePicker(ref DateTime currentDate, bool enableStartDate)
    {
        if (ImGui.BeginChildFrame(4, new Vector2(320, 215), ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Separator();

            ImGui.SetCursorPosX((ImGui.GetWindowWidth()) / 8);

            // 上一年 Last Year
            if (Widgets.IconButton(FontAwesomeIcon.Backward, "None", "LastYear"))
            {
                currentDate = currentDate.AddYears(-1);
                searchTimer.Stop();
                searchTimer.Start();
            }

            ImGui.SameLine();

            // 上一月 Last Month
            if (ImGui.ArrowButton("LastMonth", ImGuiDir.Left))
            {
                currentDate = currentDate.AddMonths(-1);
                searchTimer.Stop();
                searchTimer.Start();
            }

            ImGui.SameLine();
            ImGui.Text($"{currentDate.Year}.{string.Format("{0:MM}", currentDate)}");
            ImGui.SameLine();

            // 下一月 Next Month
            if (ImGui.ArrowButton("NextMonth", ImGuiDir.Right))
            {
                currentDate = currentDate.AddMonths(1);
                searchTimer.Stop();
                searchTimer.Start();
            }

            ImGui.SameLine();

            // 下一年 Next Year
            if (Widgets.IconButton(FontAwesomeIcon.Forward, "None", "NextYear"))
            {
                currentDate = currentDate.AddYears(1);
                searchTimer.Stop();
                searchTimer.Start();
            }

            if (ImGui.BeginTable("DatePicker", 7, ImGuiTableFlags.NoBordersInBody))
            {
                // 表头 Header Column
                var weekDaysData = Service.Lang.GetText("WeekDays");
                var weekDays = weekDaysData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var day in weekDays)
                {
                    ImGui.TableNextColumn();
                    Widgets.TextCentered(day);
                }

                ImGui.TableNextRow(ImGuiTableRowFlags.None);

                var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

                var daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);

                // 不存在于该月的日期 Date not exsited in this month
                for (var i = 0; i < firstDayOfWeek; i++)
                {
                    ImGui.TableNextColumn();
                    Widgets.TextCentered("");
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
                        if (Widgets.SelectableCentered(day.ToString(), selectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
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
                            Widgets.TextCentered(day.ToString());
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            // 可选中的日期 Selectable Date
                            if (Widgets.SelectableCentered(day.ToString(), selectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
                            {
                                currentDate = new DateTime(currentDate.Year, currentDate.Month, day);
                                searchTimer.Stop();
                                searchTimer.Start();
                            }
                        }
                    }
                }
                ImGui.EndTable();
            }
            ImGui.EndChildFrame();
        }
    }

    // 显示列勾选框 Displayed Column Checkbox
    private void ColumnDisplayCheckbox(Action<bool> setDisplayColumn, ref bool isShowColumn, string text)
    {
        if (ImGui.Checkbox($"{Service.Lang.GetText(text)}##Display{text}Column", ref isShowColumn))
        {
            setDisplayColumn(isShowColumn);
            C.Save();
        }
    }

    // 用于处理货币名变更 Used to handle currency rename
    private void CurrencyRenameHandler(string editedCurrencyName)
    {
        var editedFilePath = Path.Combine(P.PlayerDataFolder, $"{editedCurrencyName}.txt");

        if (C.AllCurrencies.ContainsKey(editedCurrencyName) || File.Exists(editedFilePath))
        {
            Service.Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp1"));
            return;
        }


        if (C.PresetCurrencies.TryGetValue(selectedCurrencyName, out var value))
        {
            C.PresetCurrencies.Remove(selectedCurrencyName);
            C.PresetCurrencies.Add(editedCurrencyName, value);
        }

        if (C.CustomCurrencies.TryGetValue(selectedCurrencyName, out var value2))
        {
            C.CustomCurrencies.Remove(selectedCurrencyName);
            C.CustomCurrencies.Add(editedCurrencyName, value2);
        }

        selectedStates.Remove(selectedCurrencyName);
        selectedStates.Add(editedCurrencyName, new());
        selectedTransactions.Remove(selectedCurrencyName);
        selectedTransactions.Add(editedCurrencyName, new());

        var index = C.OrdedOptions.IndexOf(selectedCurrencyName);
        if (index != -1)
        {
            C.OrdedOptions[index] = editedCurrencyName;
        }

        C.Save();

        var selectedFilePath = Path.Combine(P.PlayerDataFolder, $"{selectedCurrencyName}.txt");
        if (File.Exists(selectedFilePath))
        {
            File.Move(selectedFilePath, editedFilePath);
        }

        selectedCurrencyName = editedCurrencyName;
    }

    // 用于在记录新增时更新记录 Used to update transactions when transactions added
    public void UpdateTransactionsEvent(object sender, EventArgs e)
    {
        if (selectedCurrencyName != null)
        {
            UpdateTransactions();
        }
    }

    // 用于在筛选时更新记录 Used to update transactions
    public void UpdateTransactions(int ifClearSelectedStates = 1)
    {
        if (currentTypeTransactions == null || selectedCurrencyName.IsNullOrEmpty())
        {
            return;
        }

        if (ifClearSelectedStates == 1)
        {
            selectedStates[selectedCurrencyName].Clear();
            selectedTransactions[selectedCurrencyName].Clear();
        }

        Transactions.ReorderTransactions(selectedCurrencyName);
        currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyName));
    }
}
