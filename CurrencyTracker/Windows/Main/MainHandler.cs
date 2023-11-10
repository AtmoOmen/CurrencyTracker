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
    // 用于处理选项顺序 Used to handle options' positions.
    private void ReloadOrderedOptions()
    {
        var areEqual = ordedOptions.All(options.Contains) && options.All(ordedOptions.Contains);
        if (!areEqual)
        {
            var additionalElements = options.Except(ordedOptions).ToList();
            ordedOptions.AddRange(additionalElements);

            var missingElements = ordedOptions.Except(options).ToList();
            ordedOptions.RemoveAll(item => missingElements.Contains(item));

            Plugin.Instance.Configuration.OrdedOptions = ordedOptions;
            Plugin.Instance.Configuration.Save();
        }
    }

    // 用于处理选项位置变化 Used to handle option's position change.
    private void SwapOptions(int index1, int index2)
    {
        (ordedOptions[index2], ordedOptions[index1]) = (ordedOptions[index1], ordedOptions[index2]);

        Plugin.Instance.Configuration.OrdedOptions = ordedOptions;
        Plugin.Instance.Configuration.Save();
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
        query = query.Normalize(NormalizationForm.FormKC);
        if (query.IsNullOrEmpty())
        {
            return transactions;
        }

        var filteredTransactions = new List<TransactionsConvertor>();
        var isChineseSimplified = C.SelectedLanguage == "ChineseSimplified";

        foreach (var transaction in transactions)
        {
            var normalizedLocation = transaction.LocationName.Normalize(NormalizationForm.FormKC);
            var pinyin = isChineseSimplified ? PinyinHelper.GetPinyin(normalizedLocation, "") : string.Empty;

            if (normalizedLocation.Contains(query, StringComparison.OrdinalIgnoreCase) || (isChineseSimplified && pinyin.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                filteredTransactions.Add(transaction);
            }
        }
        return filteredTransactions;
    }

    // 按备注显示交易记录 Hide Unmatched Transactions By Note
    private List<TransactionsConvertor> ApplyNoteFilter(List<TransactionsConvertor> transactions, string query)
    {
        query = query.Normalize(NormalizationForm.FormKC);
        if (query.IsNullOrEmpty())
        {
            return transactions;
        }

        List<TransactionsConvertor> filteredTransactions = new List<TransactionsConvertor>();
        var isChineseSimplified = C.SelectedLanguage == "ChineseSimplified";

        foreach (var transaction in transactions)
        {
            var normalizedNote = transaction.Note.Normalize(NormalizationForm.FormKC);
            var pinyin = isChineseSimplified ? PinyinHelper.GetPinyin(normalizedNote, "") : string.Empty;

            if (normalizedNote.Contains(query, StringComparison.OrdinalIgnoreCase) || (isChineseSimplified && pinyin.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                filteredTransactions.Add(transaction);
            }
        }
        return filteredTransactions;
    }

    // 按搜索结果显示自定义货币追踪里的物品 Show On-Demand Items Based On Filter
    private static List<string> ApplyCCTFilter(string searchFilter)
    {
        return Tracker.ItemNamesSet.Where(itemName => itemName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // 延迟加载收支记录 Used to handle too-fast transactions loading
    private void SearchTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        UpdateTransactions();
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
    private List<TransactionsConvertor> ApplyFilters(List<TransactionsConvertor> currentTypeTransactions)
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
        ImGui.Separator();

        if (Widgets.IconButton(FontAwesomeIcon.Backward, "None", "LastYear") && enableStartDate)
        {
            currentDate = currentDate.AddYears(-1);
            searchTimer.Stop();
            searchTimer.Start();
        }

        ImGui.SameLine();

        if (ImGui.ArrowButton("LastMonth", ImGuiDir.Left) && enableStartDate)
        {
            currentDate = currentDate.AddMonths(-1);
            searchTimer.Stop();
            searchTimer.Start();
        }

        ImGui.SameLine();
        ImGui.Text($"{currentDate.Year}.{string.Format("{0:MM}", currentDate)}");
        ImGui.SameLine();

        if (ImGui.ArrowButton("NextMonth", ImGuiDir.Right))
        {
            currentDate = currentDate.AddMonths(1);
            searchTimer.Stop();
            searchTimer.Start();
        }

        ImGui.SameLine();

        if (Widgets.IconButton(FontAwesomeIcon.Forward, "None", "NextYear") && enableStartDate)
        {
            currentDate = currentDate.AddYears(1);
            searchTimer.Stop();
            searchTimer.Start();
        }

        if (ImGui.BeginTable("DatePicker", 7, ImGuiTableFlags.NoBordersInBody))
        {
            var weekDaysData = Service.Lang.GetText("WeekDays");
            var weekDays = weekDaysData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var day in weekDays)
            {
                ImGui.TableNextColumn();
                ImGui.Text(day);
            }

            ImGui.TableNextRow(ImGuiTableRowFlags.None);

            var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

            var daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);

            for (var i = 0; i < firstDayOfWeek; i++)
            {
                ImGui.TableNextColumn();
                ImGui.Text("");
            }

            for (var day = 1; day <= daysInMonth; day++)
            {
                ImGui.TableNextColumn();
                if (currentDate.Day == day)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.6f, 1.0f, 1.0f));
                    if (ImGui.Selectable(day.ToString(), selectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
                    {
                        currentDate = new DateTime(currentDate.Year, currentDate.Month, day);
                    }
                    ImGui.PopStyleColor();
                }
                else
                {
                    if (enableStartDate && (currentDate.Year == filterEndDate.Year && currentDate.Month == filterEndDate.Month && day >= filterEndDate.Day) || currentDate.Year > filterEndDate.Year || currentDate.Month > filterEndDate.Month)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                        ImGui.Text(day.ToString());
                        ImGui.PopStyleColor();
                    }
                    else if (!enableStartDate && (currentDate.Year == filterStartDate.Year && currentDate.Month == filterStartDate.Month && day <= filterStartDate.Day) || currentDate.Year < filterStartDate.Year || currentDate.Month < filterStartDate.Month)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                        ImGui.Text(day.ToString());
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        if (ImGui.Selectable(day.ToString(), selectTimeDeco, ImGuiSelectableFlags.DontClosePopups))
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
    }

    // 用于处理货币名变更 Used to handle currency rename
    private void CurrencyRenameHandler(string editedCurrencyName)
    {
        var editedFilePath = Path.Combine(P.PlayerDataFolder, $"{editedCurrencyName}.txt");

        if (C.PresetCurrencies.Keys.Concat(C.CustomCurrencies.Keys).Contains(editedCurrencyName))
        {
            Service.Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp1"));
            return;
        }

        if (File.Exists(editedFilePath))
        {
            Service.Chat.PrintError($"{Service.Lang.GetText("CurrencyRenameHelp2", editedFilePath)}");
            return;
        }

        if (C.PresetCurrencies.ContainsKey(selectedCurrencyName))
        {
            var currencyID = C.PresetCurrencies[selectedCurrencyName];

            C.PresetCurrencies.Remove(selectedCurrencyName);
            C.PresetCurrencies.Add(editedCurrencyName, currencyID);
        }

        if (C.CustomCurrencies.ContainsKey(selectedCurrencyName))
        {
            var currencyID = C.CustomCurrencies[selectedCurrencyName];

            C.CustomCurrencies.Remove(selectedCurrencyName);
            C.CustomCurrencies.Add(editedCurrencyName, currencyID);
        }

        if (C.OrdedOptions.Contains(selectedCurrencyName))
        {
            var index = C.OrdedOptions.IndexOf(selectedCurrencyName);

            C.OrdedOptions[index] = editedCurrencyName;
        }

        C.Save();

        if (File.Exists(Path.Combine(P.PlayerDataFolder, $"{selectedCurrencyName}.txt")))
        {
            File.Move(Path.Combine(P.PlayerDataFolder, $"{selectedCurrencyName}.txt"), editedFilePath);
        }

        selectedCurrencyName = editedCurrencyName;
        options.Clear();
        selectedStates.Clear();
        selectedTransactions.Clear();

        LoadOptions();
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
