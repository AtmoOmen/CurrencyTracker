using CurrencyTracker.Manager;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace CurrencyTracker.Windows;

public partial class Main
{
#pragma warning disable CS8602
#pragma warning disable CS8604
    // 用于处理选项顺序 Used to handle options' positions.
    private void ReloadOrderedOptions()
    {
        bool areEqual = ordedOptions.All(options.Contains) && options.All(ordedOptions.Contains);
        if (!areEqual)
        {
            List<string> additionalElements = options.Except(ordedOptions).ToList();
            ordedOptions.AddRange(additionalElements);

            List<string> missingElements = ordedOptions.Except(options).ToList();
            ordedOptions.RemoveAll(item => missingElements.Contains(item));

            Plugin.Instance.Configuration.OrdedOptions = ordedOptions;
            Plugin.Instance.Configuration.Save();
        }
    }

    // 用于处理选项位置变化 Used to handle option's position change.
    private void SwapOptions(int index1, int index2)
    {
        string temp = ordedOptions[index1];
        ordedOptions[index1] = ordedOptions[index2];
        ordedOptions[index2] = temp;

        Plugin.Instance.Configuration.OrdedOptions = ordedOptions;
        Plugin.Instance.Configuration.Save();
    }

    // 按收支隐藏不符合要求的交易记录 Hide Unmatched Transactions By Change
    private List<TransactionsConvertor> ApplyChangeFilter(List<TransactionsConvertor> transactions)
    {
        List<TransactionsConvertor> filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            bool isTransactionValid = filterMode == 0 ?
                transaction.Change > filterValue :
                transaction.Change < filterValue;

            if (isTransactionValid)
            {
                filteredTransactions.Add(transaction);
            }
        }
        return filteredTransactions;
    }

    // 按时间显示交易记录 Hide Unmatched Transactions By Time
    private List<TransactionsConvertor> ApplyDateTimeFilter(List<TransactionsConvertor> transactions)
    {
        List<TransactionsConvertor> filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            if (transaction.TimeStamp >= filterStartDate && transaction.TimeStamp <= filterEndDate)
            {
                filteredTransactions.Add(transaction);
            }
        }
        return filteredTransactions;
    }

    // 按地点名显示交易记录 Hide Unmatched Transactions By Location
    private List<TransactionsConvertor> ApplyLocationFilter(List<TransactionsConvertor> transactions, string LocationName)
    {
        LocationName = LocationName.Normalize(NormalizationForm.FormKC);
        if (LocationName.IsNullOrWhitespace())
        {
            return transactions;
        }

        List<TransactionsConvertor> filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            var normalizedLocation = transaction.LocationName.Normalize(NormalizationForm.FormKC);

            if (normalizedLocation.IndexOf(LocationName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                filteredTransactions.Add(transaction);
            }
        }
        return filteredTransactions;
    }

    // 合并交易记录用 Used to simplified merging transactions code
    private int MergeTransactions(bool oneWay)
    {
        if (string.IsNullOrEmpty(selectedCurrencyName))
        {
            Service.Chat.PrintError(Lang.GetText("TransactionsHelp1"));
            return 0;
        }

        int threshold = (mergeThreshold == 0) ? int.MaxValue : mergeThreshold;
        int mergeCount = transactions.MergeTransactionsByLocationAndThreshold(selectedCurrencyName, threshold, oneWay);

        if (mergeCount > 0)
            Service.Chat.Print($"{Lang.GetText("MergeTransactionsHelp1")}{mergeCount}{Lang.GetText("MergeTransactionsHelp2")}");
        else
            Service.Chat.PrintError(Lang.GetText("TransactionsHelp"));

        UpdateTransactions();
        return mergeCount;
    }

    // 调整列表框和表格高度用 Used to adjust the height of listbox and chart
    private float ChildframeHeightAdjust()
    {
        var trueCount = Convert.ToInt32(showOthers) + Convert.ToInt32(showRecordOptions) + Convert.ToInt32(showSortOptions);
        var ChildFrameHeight = ImGui.GetWindowHeight() - 245;

        if (showRecordOptions)
        {
            if (trueCount == 2) ChildFrameHeight = ImGui.GetWindowHeight() - 210;
            if (trueCount == 1) ChildFrameHeight = ImGui.GetWindowHeight() - 175;
        }
        else
        {
            if (trueCount == 2) ChildFrameHeight = ImGui.GetWindowHeight() - 210;
            if (trueCount == 1) ChildFrameHeight = ImGui.GetWindowHeight() - 150;
            if (trueCount == 0) ChildFrameHeight = ImGui.GetWindowHeight() - 85;
        }

        if (showSortOptions) if (isTimeFilterEnabled) ChildFrameHeight -= 35;

        return ChildFrameHeight;
    }

    // 应用筛选器 Apply Filters
    private List<TransactionsConvertor> ApplyFilters(List<TransactionsConvertor> currentTypeTransactions)
    {
        if (isReversed)
        {
            currentTypeTransactions = currentTypeTransactions.OrderByDescending(item => item.TimeStamp).ToList();
        }

        if (isClusteredByTime && clusterHour > 0)
        {
            TimeSpan interval = TimeSpan.FromHours(clusterHour);
            currentTypeTransactions = transactions.ClusterTransactionsByTime(currentTypeTransactions, interval);
        }

        if (isChangeFilterEnabled)
            currentTypeTransactions = ApplyChangeFilter(currentTypeTransactions);

        if (isTimeFilterEnabled)
            currentTypeTransactions = ApplyDateTimeFilter(currentTypeTransactions);

        if (isLocationFilterEnabled)
            currentTypeTransactions = ApplyLocationFilter(currentTypeTransactions, searchLocationName);

        return currentTypeTransactions;
    }

    // 用于在记录新增时更新记录 Used to update transactions when transactions added
    public void HandleEvent(object sender, EventArgs e)
    {
        if (selectedCurrencyName != null)
        {
            currentTypeTransactions = transactions.LoadAllTransactions(selectedCurrencyName);
            selectedStates[selectedCurrencyName].Clear();
            selectedTransactions[selectedCurrencyName].Clear();
            PluginLog.Debug("事件触发，已重新加载货币数据");
        }
        else if (!Plugin.Instance.Main.IsOpen)
        {
            PluginLog.Debug("事件触发，窗口未打开，不进行重新加载");
        }
        else
        {
            PluginLog.Debug("事件触发，但未选择货币类型，不进行重新加载");
        }
    }

    // 用于在筛选时更新记录
    private void UpdateTransactions()
    {
        if (currentTypeTransactions == null || selectedCurrencyName.IsNullOrWhitespace())
        {
            return;
        }

        selectedStates[selectedCurrencyName].Clear();
        selectedTransactions[selectedCurrencyName].Clear();

        currentTypeTransactions = transactions.LoadAllTransactions(selectedCurrencyName);
    }
}
