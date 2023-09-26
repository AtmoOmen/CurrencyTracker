using CurrencyTracker.Manager;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    public Main(Plugin plugin) : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;

        Initialize(plugin);
    }

    public void Dispose()
    {
    }

#pragma warning disable CS8602
#pragma warning disable CS8604

    // 初始化 Initialize
    private void Initialize(Plugin plugin)
    {
        transactions ??= new Transactions();

        isReversed = plugin.Configuration.ReverseSort;
        isTrackedinDuty = plugin.Configuration.TrackedInDuty;
        recordMode = plugin.Configuration.TrackMode;
        timerInterval = plugin.Configuration.TimerInterval;
        transactionsPerPage = plugin.Configuration.RecordsPerPage;
        ordedOptions = plugin.Configuration.OrdedOptions;
        hiddenOptions = plugin.Configuration.HiddenOptions;
        isChangeColoring = plugin.Configuration.ChangeTextColoring;
        positiveChangeColor = plugin.Configuration.PositiveChangeColor;
        negativeChangeColor = plugin.Configuration.NegativeChangeColor;

        LoadOptions();
        LoadLanguage(plugin);
        LoadCustomMinTrackValue();
    }

    // 将预置货币类型、玩家自定义的货币类型加入选项列表 Add preset currencies and player-customed currencies to the list of options
    private void LoadOptions()
    {
        currencyInfo ??= new CurrencyInfo();
        HashSet<string> addedOptions = new HashSet<string>();

        foreach (var currency in Tracker.CurrencyType)
        {
            if (currencyInfo.permanentCurrencies.TryGetValue(currency, out uint currencyID))
            {
                string? currencyName = currencyInfo.CurrencyLocalName(currencyID);

                if (!addedOptions.Contains(currencyName) && !hiddenOptions.Contains(currencyName))
                {
                    permanentCurrencyName.Add(currencyName);
                    options.Add(currencyName);
                    addedOptions.Add(currencyName);
                    selectedStates.Add(currencyName, new List<bool>());
                    selectedTransactions.Add(currencyName, new List<TransactionsConvertor>());
                }
            }
        }

        foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
        {
            if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out _))
            {
                if (!addedOptions.Contains(currency))
                {
                    options.Add(currency);
                    addedOptions.Add(currency);
                    selectedStates.Add(currency, new List<bool>());
                    selectedTransactions.Add(currency, new List<TransactionsConvertor>());
                }
            }
        }

        if (ordedOptions == null)
        {
            ordedOptions = options;
            Plugin.Instance.Configuration.OrdedOptions = ordedOptions;
            Plugin.Instance.Configuration.Save();
        }
        else
        {
            ReloadOrderedOptions();
        }
    }

    // 处理插件语言表达 Handel the plugin UI's language
    private void LoadLanguage(Plugin plugin)
    {
        playerLang = plugin.Configuration.SelectedLanguage;

        if (string.IsNullOrEmpty(playerLang))
        {
            playerLang = Service.ClientState.ClientLanguage.ToString();
        }

        Lang = new LanguageManager(playerLang);
    }

    // 初始化自定义货币最小记录值
    private void LoadCustomMinTrackValue()
    {
        HashSet<string> addedCurrencies = new HashSet<string>();
        foreach (var currency in options)
        {
            if (Plugin.Instance.Configuration.MinTrackValueDic["InDuty"].ContainsKey(currency) && Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"].ContainsKey(currency))
                continue;
            if (!addedCurrencies.Contains(currency))
            {
                Plugin.Instance.Configuration.MinTrackValueDic["InDuty"].Add(currency, 0);
                Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"].Add(currency, 0);
                Plugin.Instance.Configuration.Save();
                addedCurrencies.Add(currency);
            }
        }
    }

    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn) return;
        transactions ??= new Transactions();

        if (!showSortOptions) ImGui.TextColored(ImGuiColors.DalamudGrey, Lang.GetText("ConfigLabel"));
        else ImGui.TextColored(ImGuiColors.DalamudYellow, Lang.GetText("ConfigLabel"));
        if (ImGui.IsItemClicked())
        {
            showSortOptions = !showSortOptions;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Lang.GetText("ConfigLabelHelp"));
        }

        if (showSortOptions)
        {
            ReverseSort();
            ImGui.SameLine();
            TimeClustering();
            ImGui.SameLine();
            SortByLocation();
            ImGui.SameLine();
            SortByChange();

            if (isTimeFilterEnabled)
            {
                SortByTime();
            }
            else
            {
                ImGui.SameLine();
                SortByTime();
            }
        }

        if (!showSortOptions && !showRecordOptions) ImGui.SameLine();

        if (!showRecordOptions) ImGui.TextColored(ImGuiColors.DalamudGrey, Lang.GetText("ConfigLabel1"));
        else ImGui.TextColored(ImGuiColors.DalamudYellow, Lang.GetText("ConfigLabel1"));
        if (ImGui.IsItemClicked())
        {
            showRecordOptions = !showRecordOptions;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Lang.GetText("ConfigLabelHelp"));
        }
        if (showRecordOptions)
        {
            TrackInDuty();
            ImGui.SameLine();
            MinRecordValueInDuty();
            ImGui.SameLine();
            MergeTransactions();
            ImGui.SameLine();
            CustomCurrencyTracker();
            ImGui.SameLine();
            RecordMode();
            ImGui.SameLine();
            ClearExceptions();
        }

        if (!showRecordOptions && !showOthers) ImGui.SameLine();

        if (!showOthers) ImGui.TextColored(ImGuiColors.DalamudGrey, Lang.GetText("ConfigLabel2"));
        else ImGui.TextColored(ImGuiColors.DalamudYellow, Lang.GetText("ConfigLabel2"));
        if (ImGui.IsItemClicked())
        {
            showOthers = !showOthers;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Lang.GetText("ConfigLabelHelp"));
        }
        if (showOthers)
        {
            ExportToCSV();
            ImGui.SameLine();
            OpenDataFolder();
            ImGui.SameLine();
            OpenGitHubPage();
            ImGui.SameLine();
            LanguageSwitch();
            if (Plugin.Instance.PluginInterface.IsDev)
            {
                FeaturesUnderTest();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        CurrenciesList();

        TransactionsChildframe();
    }

    // 测试用功能区 Some features still under testing
    private void FeaturesUnderTest()
    {
    }

    // 倒序排序 Reverse Sort
    private void ReverseSort()
    {
        if (ImGui.Checkbox($"{Lang.GetText("ReverseSort")}##InverseSort", ref isReversed))
        {
            Plugin.Instance.Configuration.ReverseSort = isReversed;
            Plugin.Instance.Configuration.Save();
            UpdateTransactions();
        }
    }

    // 时间聚类 Time Clustering
    private void TimeClustering()
    {
        if (!isClusteredByTime) 
        {
            if (ImGui.Checkbox(Lang.GetText("ClusterByTime"), ref isClusteredByTime))
            {
                UpdateTransactions();
            }
        }
        else 
        {
            if (ImGui.Checkbox("", ref isClusteredByTime))
            {
                UpdateTransactions();
            }
        }
        

        if (isClusteredByTime)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(115);
            if (ImGui.InputInt(Lang.GetText("ClusterInterval"), ref clusterHour, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (clusterHour <= 0)
                {
                    clusterHour = 0;
                }
                UpdateTransactions();

            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker($"{Lang.GetText("ClusterByTimeHelp1")} {clusterHour}{Lang.GetText("ClusterByTimeHelp2")}");
        }
    }

    // 按收支数筛选 Sort By Change
    private void SortByChange()
    {
        if (!isChangeFilterEnabled)
        {
            if (ImGui.Checkbox($"{Lang.GetText("ChangeFilterEnabled")}##ChangeFilter", ref isChangeFilterEnabled))
            {
                UpdateTransactions();

            }
        }
        else 
        {
            if (ImGui.Checkbox("##ChangeFilter", ref isChangeFilterEnabled))
            {
                UpdateTransactions();
            }
        }
        

        if (isChangeFilterEnabled)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton($"{Lang.GetText("Greater")}##FilterMode", ref filterMode, 0)) UpdateTransactions();
            ImGui.SameLine();
            if (ImGui.RadioButton($"{Lang.GetText("Less")}##FilterMode", ref filterMode, 1)) UpdateTransactions();

            ImGui.SameLine();
            ImGui.SetNextItemWidth(130);
            if (ImGui.InputInt($"##FilterValue", ref filterValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue)) UpdateTransactions();
            ImGuiComponents.HelpMarker($"{Lang.GetText("CurrentSettings")}:\n{Lang.GetText("ChangeFilterLabel")} {(Lang.GetText(filterMode == 0 ? "Greater" : filterMode == 1 ? "Less" : ""))} {filterValue} {Lang.GetText("ChangeFilterValueLabel")}");
        }
    }

    // 按收支数筛选 Sort By Time
    private void SortByTime()
    {
        if (!isTimeFilterEnabled) { if (ImGui.Checkbox($"{Lang.GetText("FilterByTime")}##TimeFilter", ref isTimeFilterEnabled)) UpdateTransactions(); }
        else { if (ImGui.Checkbox("##TimeFilter", ref isTimeFilterEnabled)) UpdateTransactions(); }

        if (isTimeFilterEnabled)
        {
            int startYear = filterStartDate.Year;
            int startMonth = filterStartDate.Month;
            int startDay = filterStartDate.Day;
            int endYear = filterEndDate.Year;
            int endMonth = filterEndDate.Month;
            int endDay = filterEndDate.Day;

            ImGui.SameLine();
            ImGui.SetNextItemWidth(125);
            if (ImGui.InputInt($"{Lang.GetText("Year")}##StartYear", ref startYear, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterStartDate = new DateTime(startYear, startMonth, startDay);
                UpdateTransactions();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt($"{Lang.GetText("Month")}##StartMonth", ref startMonth, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterStartDate = new DateTime(startYear, startMonth, startDay);
                UpdateTransactions();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt($"{Lang.GetText("Day")}##StartDay", ref startDay, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterStartDate = new DateTime(startYear, startMonth, startDay);
                UpdateTransactions();
            }

            ImGui.SameLine();
            ImGui.Text("~");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(125);
            if (ImGui.InputInt($"{Lang.GetText("Year")}##EndYear", ref endYear, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterEndDate = new DateTime(endYear, endMonth, endDay);
                UpdateTransactions();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt($"{Lang.GetText("Month")}##EndMonth", ref endMonth, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterEndDate = new DateTime(endYear, endMonth, DateTime.DaysInMonth(endYear, endMonth));
                UpdateTransactions();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt($"{Lang.GetText("Day")}##EndDay", ref endDay, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterEndDate = new DateTime(endYear, endMonth, endDay);
                UpdateTransactions();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker($"{Lang.GetText("TimeFilterLabel")} {filterStartDate.ToString("yyyy/MM/dd")} {Lang.GetText("TimeFilterLabel1")} {filterEndDate.ToString("yyyy/MM/dd")} {Lang.GetText("TimeFilterLabel2")}");
        }
    }

    // 按地点筛选 Sort By Location
    private void SortByLocation()
    {
        if (!isLocationFilterEnabled) { if (ImGui.Checkbox($"{Lang.GetText("LocationFilter")}##LocationFilter", ref isLocationFilterEnabled)) UpdateTransactions(); }
        else { if(ImGui.Checkbox("##LocationFilter", ref isLocationFilterEnabled)) UpdateTransactions(); }

        if (isLocationFilterEnabled)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("##LocationSearch", ref searchLocationName, 80))
            {
                UpdateTransactions();
            }
        }
    }

    // 是否在副本内记录数据 Track in Duty Switch
    private void TrackInDuty()
    {
        if (ImGui.Checkbox(Lang.GetText("TrackInDuty"), ref isTrackedinDuty))
        {
            Plugin.Instance.Configuration.TrackedInDuty = isTrackedinDuty;
            Plugin.Instance.Configuration.Save();
        }

        ImGuiComponents.HelpMarker(Lang.GetText("TrackInDutyHelp"));
    }

    // 最小记录值 Minimum Change Permitted to Create a New Transaction
    private void MinRecordValueInDuty()
    {
        if (!isTrackedinDuty) return;
        if (ImGui.Button(Lang.GetText("MinimumRecordValue")))
        {
            if (selectedCurrencyName != null)
            {
                ImGui.OpenPopup("MinTrackValue");
                inDutyMinTrackValue = Plugin.Instance.Configuration.MinTrackValueDic["InDuty"][selectedCurrencyName];
                outDutyMinTrackValue = Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"][selectedCurrencyName];
            }
            else
            {
                Service.Chat.PrintError(Lang.GetText("TransactionsHelp1"));
                return;
            }
        }

        if (ImGui.BeginPopup("MinTrackValue"))
        {
            if (selectedCurrencyName != null)
            {
                ImGui.Text($"{Lang.GetText("Now")}:");
                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.DalamudYellow, selectedCurrencyName);
                ImGui.Separator();
                ImGui.Text($"{Lang.GetText("MinimumRecordValueLabel")}{Plugin.Instance.Configuration.MinTrackValueDic["InDuty"][selectedCurrencyName]}");
                ImGui.SetNextItemWidth(175);
                ImGui.InputInt("##MinInDuty", ref inDutyMinTrackValue, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue);
                if (inDutyMinTrackValue < 0) inDutyMinTrackValue = 0;
                ImGui.Text($"{Lang.GetText("MinimumRecordValueLabel1")}{Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"][selectedCurrencyName]}");
                ImGui.SetNextItemWidth(175);
                ImGui.InputInt("##MinOutDuty", ref outDutyMinTrackValue, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue);
                if (inDutyMinTrackValue < 0) inDutyMinTrackValue = 0;
                if (ImGui.Button(Lang.GetText("MinimumRecordValueLabel2")))
                {
                    Plugin.Instance.Configuration.MinTrackValueDic["InDuty"][selectedCurrencyName] = inDutyMinTrackValue;
                    Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"][selectedCurrencyName] = outDutyMinTrackValue;
                    Plugin.Instance.Configuration.Save();
                }
                ImGuiComponents.HelpMarker($"{Lang.GetText("MinimumRecordValueHelp")}{Plugin.Instance.Configuration.MinTrackValueDic["InDuty"][selectedCurrencyName]}{Lang.GetText("MinimumRecordValueHelp1")}{Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"][selectedCurrencyName]}{Lang.GetText("MinimumRecordValueHelp2")}");
            }
            else
            {
                return;
            }
            ImGui.EndPopup();
        }
    }

    // 自定义货币追踪 Custom Currencies To Track
    private void CustomCurrencyTracker()
    {
        if (ImGui.Button(Lang.GetText("CustomCurrencyLabel")))
        {
            ImGui.OpenPopup("CustomCurrency");
        }

        if (ImGui.BeginPopup("CustomCurrency", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, Lang.GetText("CustomCurrencyLabel1"));
            ImGuiComponents.HelpMarker(Lang.GetText("CustomCurrencyHelp"));
            ImGui.Text(Lang.GetText("CustomCurrencyLabel2"));
            if (ImGui.BeginCombo("", Plugin.Instance.ItemNames.TryGetValue(customCurrency, out var selected) ? selected : Lang.GetText("CustomCurrencyLabel3"), ImGuiComboFlags.HeightLarge))
            {
                int startIndex = currentItemPage * itemsPerPage;
                int endIndex = Math.Min(startIndex + itemsPerPage, Plugin.Instance.ItemNames.Count);
                int pageCount = (int)Math.Ceiling((double)Plugin.Instance.ItemNames.Count / itemsPerPage) - 4;

                ImGui.SetNextItemWidth(200f);
                ImGui.InputTextWithHint("##selectflts", Lang.GetText("CustomCurrencyLabel4"), ref searchFilter, 50);
                ImGui.SameLine();
                if (Widgets.IconButton(FontAwesomeIcon.Backward))
                    currentItemPage = 0;
                ImGui.SameLine();
                if (ImGui.ArrowButton("CustomPreviousPage", ImGuiDir.Left) && currentItemPage > 0)
                    currentItemPage--;
                ImGui.SameLine();
                if (ImGui.ArrowButton("CustomNextPage", ImGuiDir.Right) && currentItemPage < pageCount)
                    currentItemPage++;
                ImGui.SameLine();
                if (Widgets.IconButton(FontAwesomeIcon.Forward))
                    currentItemPage = pageCount;
                ImGui.Separator();

                

                int visibleItems = 0;

                foreach (var x in Plugin.Instance.ItemNames)
                {
                    var shouldSkip = false;
                    foreach (var y in permanentCurrencyName)
                    {
                        if (x.Value.Contains(y))
                        {
                            shouldSkip = true;
                            break;
                        }
                    }
                    if (shouldSkip)
                    {
                        continue;
                    }

                    if (searchFilter != string.Empty && !x.Value.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) continue;

                    // 只显示当前页的项
                    if (visibleItems >= startIndex && visibleItems < endIndex)
                    {
                        if (ImGui.Selectable(x.Value))
                        {
                            customCurrency = x.Key;
                        }

                        if (ImGui.IsWindowAppearing() && customCurrency == x.Key)
                        {
                            ImGui.SetScrollHereY();
                        }
                    }

                    visibleItems++;
                }

                ImGui.EndCombo();
            }


            if (ImGui.Button($"{Lang.GetText("Add")}{selected}"))
            {
                if (string.IsNullOrEmpty(selected))
                {
                    Service.Chat.PrintError(Lang.GetText("TransactionsHelp1"));
                    return;
                }
                if (options.Contains(selected))
                {
                    Service.Chat.PrintError(Lang.GetText("CustomCurrencyHelp1"));
                    return;
                }
                if (Plugin.Instance.Configuration.CustomCurrencyType.Contains(selected))
                    Plugin.Instance.Configuration.CustomCurrencies.Add(selected, customCurrency);
                Plugin.Instance.Configuration.CustomCurrencyType.Add(selected);

                if (!Plugin.Instance.Configuration.MinTrackValueDic["InDuty"].ContainsKey(selected) && !Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"].ContainsKey(selected))
                {
                    Plugin.Instance.Configuration.MinTrackValueDic["InDuty"].Add(selected, 0);
                    Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"].Add(selected, 0);
                }
                Plugin.Instance.Configuration.Save();
                options.Add(selected);
                selectedStates.Add(selected, new List<bool>());
                selectedTransactions.Add(selected, new List<TransactionsConvertor>());
                ReloadOrderedOptions();
            }

            ImGui.SameLine();

            if (ImGui.Button($"{Lang.GetText("Delete")}{selected}"))
            {
                if (string.IsNullOrEmpty(selected))
                {
                    Service.Chat.PrintError(Lang.GetText("TransactionsHelp1"));
                    return;
                }
                if (!options.Contains(selected))
                {
                    Service.Chat.PrintError(Lang.GetText("CustomCurrencyHelp2"));
                    return;
                }
                Plugin.Instance.Configuration.CustomCurrencies.Remove(selected);
                Plugin.Instance.Configuration.CustomCurrencyType.Remove(selected);
                Plugin.Instance.Configuration.Save();
                options.Remove(selected);
                selectedStates.Remove(selected);
                selectedTransactions.Remove(selected);
                ReloadOrderedOptions();
            }

            ImGui.EndPopup();
        }
    }

    // 按临界值合并记录 Merge Transactions By Threshold
    private void MergeTransactions()
    {
        transactions ??= new Transactions();

        if (ImGui.Button(Lang.GetText("MergeTransactionsLabel")))
        {
            ImGui.OpenPopup("MergeTransactions");
        }

        if (ImGui.BeginPopup("MergeTransactions"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, Lang.GetText("MergeTransactionsLabel4"));
            ImGui.Text(Lang.GetText("MergeTransactionsLabel1"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("##MergeThreshold", ref mergeThreshold, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue);
            if (mergeThreshold < 0)
            {
                mergeThreshold = 0;
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker($"{Lang.GetText("MergeTransactionsHelp3")}{Lang.GetText("TransactionsHelp2")}");

            // 双向合并 Two-Way Merge
            if (ImGui.Button(Lang.GetText("MergeTransactionsLabel2")))
            {
                int mergeCount = MergeTransactions(false);
                if (mergeCount == 0)
                    return;
            }

            ImGui.SameLine();

            // 单向合并 One-Way Merge
            if (ImGui.Button(Lang.GetText("MergeTransactionsLabel3")))
            {
                int mergeCount = MergeTransactions(true);
                if (mergeCount == 0)
                    return;
            }
            ImGui.EndPopup();
        }
    }

    // 清除异常记录 Clear Exceptional Transactions
    private void ClearExceptions()
    {
        if (ImGui.Button(Lang.GetText("ClearExTransactionsLabel")))
        {
            ImGui.OpenPopup("ClearExceptionNote");
        }

        if (ImGui.BeginPopup("ClearExceptionNote"))
        {
            if (ImGui.Button(Lang.GetText("Confirm")))
            {
                if (string.IsNullOrEmpty(selectedCurrencyName))
                {
                    Service.Chat.PrintError(Lang.GetText("TransactionsHelp1"));
                    return;
                }

                var removedCount = transactions.ClearExceptionRecords(selectedCurrencyName);
                if (removedCount > 0)
                {
                    Service.Chat.Print($"{Lang.GetText("ClearExTransactionsHelp2")}{removedCount}{Lang.GetText("ClearExTransactionsHelp3")}");
                    UpdateTransactions();
                }
                else
                {
                    Service.Chat.PrintError(Lang.GetText("TransactionsHelp"));
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker($"{Lang.GetText("ClearExTransactionsHelp")}{Lang.GetText("ClearExTransactionsHelp1")}{Lang.GetText("TransactionsHelp2")}");
            ImGui.EndPopup();
        }
    }

    // 导出数据为.CSV文件 Export Transactions To a .csv File
    private void ExportToCSV()
    {
        if (ImGui.Button(Lang.GetText("ExportCsv")))
        {
            ImGui.OpenPopup(str_id: "ExportFileRename");
        }

        if (ImGui.BeginPopup("ExportFileRename"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, Lang.GetText("FileRenameLabel"));
            ImGui.Text(Lang.GetText("FileRenameLabel1"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText($"_{selectedCurrencyName}_{Lang.GetText("FileRenameLabel2")}.csv", ref fileName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (selectedCurrencyName == null)
                {
                    Service.Chat.PrintError(Lang.GetText("TransactionsHelp1"));
                    return;
                }
                if (currentTypeTransactions == null || currentTypeTransactions.Count == 0)
                {
                    Service.Chat.PrintError(Lang.GetText("ExportCsvMessage1"));
                    return;
                }
                var filePath = transactions.ExportToCsv(currentTypeTransactions, fileName, selectedCurrencyName, Lang.GetText("ExportCsvMessage2"));
                Service.Chat.Print($"{Lang.GetText("ExportCsvMessage3")}{filePath}");
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"{Lang.GetText("FileRenameHelp1")}{selectedCurrencyName}_{Lang.GetText("FileRenameLabel2")}.csv");
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Lang.GetText("FileRenameHelp"));
            ImGui.EndPopup();
        }
    }

    // 打开数据文件夹 Open Folder Containing Data Files
    private void OpenDataFolder()
    {
        if (ImGui.Button(Lang.GetText("OpenDataFolder")))
        {
            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            string playerDataFolder = Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start \"\" \"{playerDataFolder}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = playerDataFolder
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = playerDataFolder
                    });
                }
                else
                {
                    PluginLog.Error("Unsupported OS");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error :{ex.Message}");
            }
        }
    }

    // 打开插件 GitHub 页面 Open Plugin GitHub Page
    private void OpenGitHubPage()
    {
        if (ImGui.Button("GitHub"))
        {
            string url = "https://github.com/AtmoOmen/CurrencyTracker";
            ProcessStartInfo psi = new ProcessStartInfo();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = url;
                psi.UseShellExecute = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                psi.FileName = "xdg-open";
                psi.ArgumentList.Add(url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                psi.FileName = "open";
                psi.ArgumentList.Add(url);
            }
            else
            {
                PluginLog.Error("Unsupported OS");
                return;
            }

            Process.Start(psi);
        }
    }

    // 界面语言切换功能 Language Switch
    private void LanguageSwitch()
    {
        var AvailableLangs = Lang.AvailableLanguage();

        var lang = string.Empty;

        if (Widgets.IconButton(FontAwesomeIcon.Globe, "Languages"))
        {
            ImGui.OpenPopup(str_id: "LanguagesList");
        }

        if (ImGui.BeginPopup("LanguagesList"))
        {
            foreach (var langname in AvailableLangs)
            {
                var langquery = from pair in LanguageManager.LanguageNames
                                where pair.Value == langname
                                select pair.Key;
                var language = langquery.FirstOrDefault();
                if (language.IsNullOrEmpty())
                {
                    Service.Chat.PrintError(Lang.GetText("UnknownCurrency"));
                    return;
                }
                if (ImGui.Button(langname))
                {
                    Lang = new LanguageManager(language);
                    Graph.Lang = new LanguageManager(language);

                    playerLang = language;

                    Plugin.Instance.Configuration.SelectedLanguage = playerLang;
                    Plugin.Instance.Configuration.Save();
                }
            }
            ImGui.EndPopup();
        }
    }

    // 记录模式切换 Record Mode Change
    private void RecordMode()
    {
        if (ImGui.Button($"{Lang.GetText("TrackModeLabel")}"))
        {
            ImGui.OpenPopup("RecordMode");
        }
        if (ImGui.BeginPopup("RecordMode"))
        {
            if (ImGui.RadioButton($"{Lang.GetText("TrackModeLabel1")}##RecordMode", ref recordMode, 0))
            {
                Plugin.Instance.Configuration.TrackMode = recordMode;
                Plugin.Instance.Configuration.Save();
                Service.Tracker.ChangeTracker();
            }
            if (recordMode == 0)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(135);
                if (ImGui.InputInt($"{Lang.GetText("TrackModeLabel3")}##TimerInterval", ref timerInterval, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (timerInterval < 100) timerInterval = 100;
                    Plugin.Instance.Configuration.TimerInterval = timerInterval;
                    Plugin.Instance.Configuration.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"{Lang.GetText("TrackModeHelp3")}");
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker($"{Lang.GetText("TrackModeHelp")}{timerInterval}{Lang.GetText("TrackModeHelp1")}");
            if (ImGui.RadioButton($"{Lang.GetText("TrackModeLabel2")}##RecordMode", ref recordMode, 1))
            {
                Plugin.Instance.Configuration.TrackMode = recordMode;
                Plugin.Instance.Configuration.Save();
                Service.Tracker.ChangeTracker();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker($"{Lang.GetText("TrackModeHelp2")}");
            ImGui.EndPopup();
        }
    }

    // 图表工具栏 Table Tools
    private void TableTools()
    {
        ImGui.Text($"{Lang.GetText("Now")}: {selectedTransactions[selectedCurrencyName].Count} {Lang.GetText("Transactions")}");
        ImGui.Separator();

        if (ImGui.Selectable(Lang.GetText("Unselect")))
        {
            if (selectedTransactions[selectedCurrencyName].Count == 0)
            {
                Service.Chat.PrintError(Lang.GetText("NoTransactionsSelected"));
                return;
            }
            selectedStates[selectedCurrencyName].Clear();
            selectedTransactions[selectedCurrencyName].Clear();
        }

        if (ImGui.Selectable(Lang.GetText("SelectAll")))
        {
            selectedTransactions[selectedCurrencyName].Clear();

            foreach (var transaction in currentTypeTransactions)
            {
                selectedTransactions[selectedCurrencyName].Add(transaction);
            }

            for (int i = 0; i < selectedStates[selectedCurrencyName].Count; i++)
            {
                selectedStates[selectedCurrencyName][i] = true;
            }
        }

        if (ImGui.Selectable(Lang.GetText("InverseSelect")))
        {
            for (int i = 0; i < selectedStates[selectedCurrencyName].Count; i++)
            {
                selectedStates[selectedCurrencyName][i] = !selectedStates[selectedCurrencyName][i];
            }

            foreach (var transaction in currentTypeTransactions)
            {
                bool exists = selectedTransactions[selectedCurrencyName].Any(selectedTransaction => Widgets.IsTransactionEqual(selectedTransaction, transaction));

                if (exists)
                {
                    selectedTransactions[selectedCurrencyName].RemoveAll(t => Widgets.IsTransactionEqual(t, transaction));
                }
                else
                {
                    selectedTransactions[selectedCurrencyName].Add(transaction);
                }
            }
        }

        if (ImGui.Selectable(Lang.GetText("Copy")))
        {
            string columnData = string.Empty;
            int count = selectedTransactions[selectedCurrencyName].Count;

            for (int t = 0; t < count; t++)
            {
                var record = selectedTransactions[selectedCurrencyName][t];
                string change = $"{record.Change:+ #,##0;- #,##0;0}";
                columnData += $"{record.TimeStamp} | {record.Amount} | {change} | {record.LocationName}";

                if (t < count - 1)
                {
                    columnData += "\n";
                }
            }

            if (!string.IsNullOrEmpty(columnData))
            {
                ImGui.SetClipboardText(columnData);
                Service.Chat.Print($"{Lang.GetText("CopyTransactionsHelp")} {selectedTransactions[selectedCurrencyName].Count} {Lang.GetText("CopyTransactionsHelp1")}");
            }
            else
            {
                Service.Chat.PrintError(Lang.GetText("NoTransactionsSelected"));
                return;
            }
        }

        if (ImGui.Selectable(Lang.GetText("Delete")))
        {
            if (selectedTransactions[selectedCurrencyName].Count == 0)
            {
                Service.Chat.PrintError(Lang.GetText("NoTransactionsSelected"));
                return;
            }
            foreach (var selectedTransaction in selectedTransactions[selectedCurrencyName])
            {
                var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
                var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
                string filePath = Path.Combine(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}", $"{selectedCurrencyName}.txt");
                var editedTransactions = transactions.LoadAllTransactions(selectedCurrencyName);
                var foundTransaction = editedTransactions.FirstOrDefault(t => Widgets.IsTransactionEqual(t, selectedTransaction));

                if (foundTransaction != null)
                {
                    editedTransactions.Remove(foundTransaction);
                }

                transactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
            }
            selectedStates[selectedCurrencyName].Clear();
            selectedTransactions[selectedCurrencyName].Clear();
        }

        if (ImGui.Selectable(Lang.GetText("Export")))
        {
            if (selectedTransactions[selectedCurrencyName].Count == 0)
            {
                Service.Chat.PrintError(Lang.GetText("NoTransactionsSelected"));
                return;
            }
            var filePath = transactions.ExportToCsv(selectedTransactions[selectedCurrencyName], "", selectedCurrencyName, Lang.GetText("ExportCsvMessage2"));
            Service.Chat.Print($"{Lang.GetText("ExportCsvMessage3")}{filePath}");
        }

        ImGui.Selectable(Lang.GetText("Merge"), ref isOnMergingTT, ImGuiSelectableFlags.DontClosePopups);

        if (isOnMergingTT)
        {
            if (isOnEdit) isOnEdit = !isOnEdit;

            ImGui.Separator();
            ImGui.Text($"{Lang.GetText("Location")}:");
            ImGui.SetNextItemWidth(210);

            if (selectedTransactions[selectedCurrencyName].Count != 0)
            {
                editedLocationName = selectedTransactions[selectedCurrencyName].FirstOrDefault().LocationName;
            }
            else editedLocationName = string.Empty;

            if (ImGui.InputTextWithHint("", Lang.GetText("EditHelp"), ref editedLocationName, 80, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (selectedTransactions[selectedCurrencyName].Count == 0)
                {
                    Service.Chat.PrintError(Lang.GetText("NoTransactionsSelected"));
                    return;
                }

                if (selectedTransactions[selectedCurrencyName].Count == 1)
                {
                    Service.Chat.PrintError(Lang.GetText("MergeTransactionsHelp4"));
                    return;
                }

                if (editedLocationName.IsNullOrWhitespace())
                {
                    Service.Chat.PrintError(Lang.GetText("EditHelp1"));
                    return;
                }

                var mergeCount = transactions.MergeSpecificTransactions(selectedCurrencyName, editedLocationName, selectedTransactions[selectedCurrencyName]);
                Service.Chat.Print($"{Lang.GetText("MergeTransactionsHelp1")}{mergeCount}{Lang.GetText("MergeTransactionsHelp2")}");

                UpdateTransactions();
                isOnMergingTT = false;
            }
        }

        ImGui.Selectable(Lang.GetText("Edit"), ref isOnEdit, ImGuiSelectableFlags.DontClosePopups);

        if (isOnEdit)
        {
            if (isOnMergingTT) isOnMergingTT = !isOnMergingTT;

            ImGui.Separator();
            ImGui.Text($"{Lang.GetText("Location")}:");
            ImGui.SetNextItemWidth(210);

            if (selectedTransactions[selectedCurrencyName].Count != 0)
            {
                editedLocationName = selectedTransactions[selectedCurrencyName].FirstOrDefault().LocationName;
            }
            else editedLocationName = string.Empty;

            if (ImGui.InputTextWithHint("", Lang.GetText("EditHelp"), ref editedLocationName, 80, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (selectedTransactions[selectedCurrencyName].Count == 0)
                {
                    Service.Chat.PrintError(Lang.GetText("NoTransactionsSelected"));
                    return;
                }

                if (editedLocationName.IsNullOrWhitespace())
                {
                    Service.Chat.PrintError(Lang.GetText("EditHelp1"));
                    return;
                }

                foreach (var selectedTransaction in selectedTransactions[selectedCurrencyName])
                {
                    var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
                    var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
                    string filePath = Path.Combine(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}", $"{selectedCurrencyName}.txt");
                    var editedTransactions = transactions.LoadAllTransactions(selectedCurrencyName);

                    int index = -1;
                    for (int i = 0; i < editedTransactions.Count; i++)
                    {
                        if (Widgets.IsTransactionEqual(editedTransactions[i], selectedTransaction))
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index != -1)
                    {
                        editedTransactions[index].LocationName = editedLocationName;
                        transactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                    }
                }

                Service.Chat.Print($"{Lang.GetText("EditHelp2")} {selectedTransactions[selectedCurrencyName].Count} {Lang.GetText("EditHelp3")} {editedLocationName}");

                UpdateTransactions();

                isOnEdit = false;
            }
        }
    }

    // 顶端工具栏 Transactions Paging Tools
    private void TransactionsPagingTools()
    {
        if (currentTypeTransactions.Count == 0) return;
        int pageCount = (int)Math.Ceiling((double)currentTypeTransactions.Count / transactionsPerPage);
        currentPage = Math.Clamp(currentPage, 0, pageCount - 1);

        if (pageCount == 0)
        {
            if (Plugin.Instance.Graph.IsOpen) Plugin.Instance.Graph.IsOpen = false;
            return;
        }

        float buttonWidth = ImGui.CalcTextSize(Lang.GetText("    ")).X;
        float buttonPosX = graphsRightAligned
            ? ImGui.GetWindowWidth() - 177 - buttonWidth
            : (ImGui.GetWindowWidth() - 360) / 2 - 57 - buttonWidth;

        ImGui.SetCursorPosX(buttonPosX);

        if (Widgets.IconButton(FontAwesomeIcon.ChartBar, Lang.GetText("Graphs")))
        {
            if (selectedCurrencyName != null && currentTypeTransactions.Count != 1 && currentTypeTransactions != null)
            {
                LinePlotData = currentTypeTransactions.Select(x => x.Amount).ToArray();
                Plugin.Instance.Graph.IsOpen = !Plugin.Instance.Graph.IsOpen;
            }
            else return;
        }

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            graphsRightAligned = !graphsRightAligned;

        ImGui.SameLine();
        float pageButtonPosX = (ImGui.GetWindowWidth() - 360) / 2 - 40;
        ImGui.SetCursorPosX(pageButtonPosX);

        if (Widgets.IconButton(FontAwesomeIcon.Backward))
            currentPage = 0;

        ImGui.SameLine();

        if (ImGui.ArrowButton("PreviousPage", ImGuiDir.Left) && currentPage > 0)
            currentPage--;

        ImGui.SameLine();
        ImGui.Text($"{Lang.GetText("Di")}{currentPage + 1}{Lang.GetText("Page")} / {Lang.GetText("Gong")}{pageCount}{Lang.GetText("Page")}");

        if (ImGui.IsItemClicked())
        {
            ImGui.OpenPopup("TransactionsPerPage");
        }

        if (ImGui.BeginPopup("TransactionsPerPage"))
        {
            ImGui.Text(Lang.GetText("TransactionsPerPage"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);

            if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
            {
                transactionsPerPage = Math.Max(transactionsPerPage, 0);
                Plugin.Instance.Configuration.RecordsPerPage = transactionsPerPage;
                Plugin.Instance.Configuration.Save();
            }

            ImGui.EndPopup();
        }

        ImGui.SameLine();

        if (ImGui.ArrowButton("NextPage", ImGuiDir.Right) && currentPage < pageCount - 1)
            currentPage++;

        ImGui.SameLine();

        if (Widgets.IconButton(FontAwesomeIcon.Forward) && currentPage >= 0)
            currentPage = pageCount;

        visibleStartIndex = currentPage * transactionsPerPage;
        visibleEndIndex = Math.Min(visibleStartIndex + transactionsPerPage, currentTypeTransactions.Count);
    }

    // 收支文本染色 Change Text Coloring
    private void ChangeTextColoring()
    {
        if (ImGui.IsItemClicked())
        {
            ImGui.OpenPopup("ChangeTextColoring");
        }

        if (ImGui.BeginPopup("ChangeTextColoring"))
        {
            ImGui.Text(Lang.GetText("ChangeTextColoring"));
            ImGui.SameLine();
            if (ImGui.Checkbox("##ChangeColoring", ref isChangeColoring))
            {
                Plugin.Instance.Configuration.ChangeTextColoring = isChangeColoring;
                Plugin.Instance.Configuration.Save();
            }
            ImGui.Separator();

            if (ImGui.ColorButton("##PositiveColor", positiveChangeColor))
            {
                ImGui.OpenPopup("PositiveColor");
            }
            ImGui.SameLine();
            ImGui.Text(Lang.GetText("PositiveChange"));

            if (ImGui.BeginPopup("PositiveColor"))
            {
                if (ImGui.ColorPicker4("", ref positiveChangeColor))
                {
                    isChangeColoring = true;
                    Plugin.Instance.Configuration.ChangeTextColoring = isChangeColoring;
                    Plugin.Instance.Configuration.PositiveChangeColor = positiveChangeColor;
                    Plugin.Instance.Configuration.Save();
                }
                ImGui.EndPopup();
            }

            ImGui.SameLine();
            if (ImGui.ColorButton("##NegativeColor", negativeChangeColor))
            {
                ImGui.OpenPopup("NegativeColor");
            }
            ImGui.SameLine();
            ImGui.Text(Lang.GetText("NegativeChange"));

            if (ImGui.BeginPopup("NegativeColor"))
            {
                if (ImGui.ColorPicker4("", ref negativeChangeColor))
                {
                    isChangeColoring = true;
                    Plugin.Instance.Configuration.ChangeTextColoring = isChangeColoring;
                    Plugin.Instance.Configuration.NegativeChangeColor = negativeChangeColor;
                    Plugin.Instance.Configuration.Save();
                }
                ImGui.EndPopup();
            }

            ImGui.EndPopup();
        }
    }

    // 存储可用货币名称选项的列表框 Listbox Containing Available Currencies' Name
    private void CurrenciesList()
    {
        var ChildFrameHeight = ChildframeHeightAdjust();

        Vector2 childScale = new Vector2(243, ChildFrameHeight);
        if (ImGui.BeginChildFrame(2, childScale, ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.SetCursorPosX(42);
            if (string.IsNullOrWhiteSpace(selectedCurrencyName) || selectedOptionIndex == -1 || !permanentCurrencyName.Contains(selectedCurrencyName))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                Widgets.IconButton(FontAwesomeIcon.EyeSlash);
                ImGui.PopStyleVar();
            }
            else
            {
                Widgets.IconButton(FontAwesomeIcon.EyeSlash, Lang.GetText("Hide"));
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
                {
                    if (string.IsNullOrWhiteSpace(selectedCurrencyName) || selectedOptionIndex == -1 || !permanentCurrencyName.Contains(selectedCurrencyName)) return;

                    options.Remove(selectedCurrencyName);
                    hiddenOptions.Add(selectedCurrencyName);
                    if (!Plugin.Instance.Configuration.HiddenOptions.Contains(selectedCurrencyName))
                        Plugin.Instance.Configuration.HiddenOptions.Add(selectedCurrencyName);
                    Plugin.Instance.Configuration.Save();
                    ReloadOrderedOptions();
                    selectedCurrencyName = string.Empty;
                    selectedOptionIndex = -1;
                }
            }

            ImGui.SameLine();
            if (ImGui.ArrowButton("UpArrow", ImGuiDir.Up) && selectedOptionIndex > 0)
            {
                SwapOptions(selectedOptionIndex, selectedOptionIndex - 1);
                selectedOptionIndex--;
            }
            ImGui.SameLine();
            if (ImGui.ArrowButton("DownArrow", ImGuiDir.Down) && selectedOptionIndex < ordedOptions.Count - 1 && selectedOptionIndex > -1)
            {
                SwapOptions(selectedOptionIndex, selectedOptionIndex + 1);
                selectedOptionIndex++;
            }
            ImGui.SameLine();

            if (hiddenOptions.Count == 0)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                Widgets.IconButton(FontAwesomeIcon.TrashRestore);
                ImGui.PopStyleVar();
            }
            else
            {
                Widgets.IconButton(FontAwesomeIcon.TrashRestore, Lang.GetText("OrderChangeLabel1"));
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
                {
                    if (hiddenOptions.Count == 0)
                    {
                        Service.Chat.PrintError(Lang.GetText("OrderChangeHelp"));
                        return;
                    }
                    HashSet<string> addedOptions = new HashSet<string>();

                    foreach (var option in hiddenOptions)
                    {
                        if (!addedOptions.Contains(option))
                        {
                            options.Add(option);
                            permanentCurrencyName.Add(option);
                            addedOptions.Add(option);
                        }
                    }
                    hiddenOptions.Clear();
                    Plugin.Instance.Configuration.HiddenOptions.Clear();
                    Plugin.Instance.Configuration.Save();
                    Service.Chat.Print($"{Lang.GetText("OrderChangeHelp1")} {addedOptions.Count} {Lang.GetText("OrderChangeHelp2")}");
                    ReloadOrderedOptions();
                }
            }

            ImGui.Separator();
            ImGui.SetNextItemWidth(235);
            for (int i = 0; i < ordedOptions.Count; i++)
            {
                string option = ordedOptions[i];
                bool isSelected = i == selectedOptionIndex;

                if (ImGui.Selectable(option, isSelected))
                {
                    selectedOptionIndex = i;
                    selectedCurrencyName = option;

                    currentTypeTransactions = transactions.LoadAllTransactions(selectedCurrencyName);
                    lastTransactions = currentTypeTransactions;
                }
            }

            ImGui.EndChildFrame();
        }
    }

    // 显示收支记录 Childframe Used to Show Transactions in Form
    private void TransactionsChildframe()
    {
        if (string.IsNullOrEmpty(selectedCurrencyName))
            return;
        if (Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas])
            return;
        if (Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas51])
            return;
        if (currentTypeTransactions.Count == 0) return;

        var childFrameHeight = ChildframeHeightAdjust();
        Vector2 childScale = new Vector2(ImGui.GetWindowWidth() - 100, childFrameHeight);

        ImGui.SameLine();

        if (ImGui.BeginChildFrame(1, childScale, ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            currentTypeTransactions = ApplyFilters(currentTypeTransactions);

            TransactionsPagingTools();


            if (ImGui.BeginTable("Transactions", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable, new Vector2(ImGui.GetWindowWidth() - 175, 1)))
            {
                ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, ImGui.CalcTextSize((currentTypeTransactions.Count + 1).ToString()).X + 10, 0);
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.None, 150, 0);
                ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.None, 130, 0);
                ImGui.TableSetupColumn("Change", ImGuiTableColumnFlags.None, 100, 0);
                ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.None, 150, 0);
                ImGui.TableSetupColumn("Selected", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 30, 0);

                ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

                ImGui.TableNextColumn();
                ImGui.Text("");

                ImGui.TableNextColumn();
                ImGui.Text(Lang.GetText("Time"));

                ImGui.TableNextColumn();
                ImGui.Text(Lang.GetText("Amount"));

                ImGui.TableNextColumn();
                ImGui.Text(Lang.GetText("Change"));
                ChangeTextColoring();

                ImGui.TableNextColumn();
                ImGui.Text(Lang.GetText("Location"));

                ImGui.TableNextColumn();

                if (Widgets.IconButton(FontAwesomeIcon.EllipsisH))
                {
                    ImGui.OpenPopup("TableTools");
                }

                ImGui.TableNextRow();

                if (currentTypeTransactions.Count == 0) return;
                for (int i = visibleStartIndex; i < visibleEndIndex; i++)
                {
                    var transaction = currentTypeTransactions[i];
                    while (selectedStates[selectedCurrencyName].Count <= i)
                    {
                        selectedStates[selectedCurrencyName].Add(false);
                    }

                    bool selected = selectedStates[selectedCurrencyName][i];

                    ImGui.TableNextColumn();
                    if (isReversed)
                    {
                        ImGui.SetCursorPosX(Widgets.SetColumnCenterAligned((currentTypeTransactions.Count - i).ToString(), 0, 8));
                        ImGui.Text((currentTypeTransactions.Count - i).ToString());
                    }
                    else
                    {
                        ImGui.SetCursorPosX(Widgets.SetColumnCenterAligned((i + 1).ToString(), 0, 8));
                        ImGui.Text((i + 1).ToString());
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) && ImGui.IsMouseDown(ImGuiMouseButton.Right))
                    {
                        ImGui.Selectable(transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"), selected, ImGuiSelectableFlags.SpanAllColumns);
                        if (ImGui.IsItemHovered())
                        {
                            selectedStates[selectedCurrencyName][i] = selected = true;

                            if (selected)
                            {
                                bool exists = selectedTransactions[selectedCurrencyName].Any(t => Widgets.IsTransactionEqual(t, transaction));

                                if (!exists)
                                {
                                    selectedTransactions[selectedCurrencyName].Add(transaction);
                                }
                            }
                            else
                            {
                                selectedTransactions[selectedCurrencyName].RemoveAll(t => Widgets.IsTransactionEqual(t, transaction));
                            }
                        }
                    }
                    else if(ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
                    {
                        if (ImGui.Selectable(transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"), ref selected, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            selectedStates[selectedCurrencyName][i] = selected;

                            if (selected)
                            {
                                bool exists = selectedTransactions[selectedCurrencyName].Any(t => Widgets.IsTransactionEqual(t, transaction));

                                if (!exists)
                                {
                                    selectedTransactions[selectedCurrencyName].Add(transaction);
                                }
                            }
                            else
                            {
                                selectedTransactions[selectedCurrencyName].RemoveAll(t => Widgets.IsTransactionEqual(t, transaction));
                            }
                        }
                    }
                    else
                    {
                        ImGui.Selectable(transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"));
                    }

                    if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
                    {
                        ImGui.SetClipboardText(transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"));
                        Service.Chat.Print($"{Lang.GetText("CopiedToClipboard")}: {transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")}");
                    }

                    ImGui.TableNextColumn();
                    ImGui.Selectable(transaction.Amount.ToString("#,##0"));

                    if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
                    {
                        ImGui.SetClipboardText(transaction.Amount.ToString("#,##0"));
                        Service.Chat.Print($"{Lang.GetText("CopiedToClipboard")}: {transaction.Amount.ToString("#,##0")}");
                    }

                    ImGui.TableNextColumn();
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
                        Service.Chat.Print($"{Lang.GetText("CopiedToClipboard")} : {transaction.Change.ToString("+ #,##0;- #,##0;0")}");
                    }

                    ImGui.TableNextColumn();
                    ImGui.Selectable(transaction.LocationName);

                    if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
                    {
                        ImGui.SetClipboardText(transaction.LocationName);
                        Service.Chat.Print($"{Lang.GetText("CopiedToClipboard")}: {transaction.LocationName}");
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.Checkbox($"##select_{i}", ref selected))
                    {
                        selectedStates[selectedCurrencyName][i] = selected;

                        if (selected)
                        {
                            bool exists = selectedTransactions[selectedCurrencyName].Any(t => Widgets.IsTransactionEqual(t, transaction));

                            if (!exists)
                            {
                                selectedTransactions[selectedCurrencyName].Add(transaction);
                            }
                        }
                        else
                        {
                            selectedTransactions[selectedCurrencyName].RemoveAll(t => Widgets.IsTransactionEqual(t, transaction));
                        }
                    }

                    ImGui.TableNextRow();
                }

                if (ImGui.BeginPopup("TableTools"))
                {
                    TableTools();
                    ImGui.EndPopup();
                }

                ImGui.EndTable();
            }

            ImGui.EndChildFrame();
        }
    }
}
