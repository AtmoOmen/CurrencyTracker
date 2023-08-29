using CurrencyTracker.Manager;
using CurrencyTracker.Manger;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace CurrencyTracker.Windows;

public class Main : Window, IDisposable
{
    // 时间聚类 Time Clustering
    private int clusterHour;

    // 倒序排序开关 Reverse Sorting Switch
    private bool isReversed;

    // 副本内记录开关 Duty Tracking Switch
    private bool isTrackedinDuty;

    // 收支筛选开关 Income/Expense Filtering Switch
    private bool isChangeFilterEnabled;

    // 时间筛选开关 Time Filtering Switch
    private bool isTimeFilterEnabled;

    // 筛选时间段的起始 Filtering Time Period
    private DateTime filterStartDate = DateTime.MinValue;

    private DateTime filterEndDate = DateTime.Now;

    // 筛选模式：0为大于，1为小于 Filtering Mode: 0 for Above, 1 for Below
    private int filterMode;

    // 用户指定的筛选值 User-Specified Filtering Value
    private int filterValue;

    // 每页显示的交易记录数 Number of Transaction Records Displayed Per Page
    private int transactionsPerPage = 20;

    // 当前页码 Current Page Number
    private int currentPage;

    // 自定义追踪物品ID Custom Tracked Currency ID
    private uint customCurrency = uint.MaxValue;

    // CSV文件名 CSV File Name
    private string fileName = string.Empty;

    // 最小记录值 Minimum Tracking Value
    private int minTrackValue;

    // 默认选中的选项 Default Selected Option
    internal int selectedOptionIndex = -1;

    // 选择的语言 Selected Language
    private string playerLang = string.Empty;

    // 当前选中的货币名称 Currently Selected Currency Name
    internal string? selectedCurrencyName;

    // 搜索框值 Search Filter
    private static string searchFilter = string.Empty;

    // 合并的临界值 Merge Threshold
    private int mergeThreshold;

    // 当前页索引 Current Page Index
    private int visibleStartIndex;

    private int visibleEndIndex;

    private Transactions transactions = new Transactions();
    private TransactionsConvertor? transactionsConvertor = null!;
    private CurrencyInfo? currencyInfo = null!;
    internal static readonly LanguageManager Lang = new LanguageManager();
    private List<string> permanentCurrencyName = new List<string>();
    internal List<string> options = new List<string>();
    internal List<TransactionsConvertor> currentTypeTransactions = new List<TransactionsConvertor>();

    public Main(Plugin plugin) : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;

        Initialize(plugin);
    }

    public void Dispose()
    {
    }

    // 初始化 Initialize
    private void Initialize(Plugin plugin)
    {
        transactions ??= new Transactions();

        isReversed = plugin.Configuration.ReverseSort;
        isTrackedinDuty = plugin.Configuration.TrackedInDuty;
        minTrackValue = plugin.Configuration.MinTrackValue;

        LoadOptions();
        LoadLanguage(plugin);
    }

    // 将预置货币类型、玩家自定义的货币类型加入选项列表 Add preset currencies and player-customed currencies to the list of options
    private void LoadOptions()
    {
        currencyInfo ??= new CurrencyInfo();
        foreach (var currency in Tracker.CurrencyType)
        {
            if (currencyInfo.permanentCurrencies.TryGetValue(currency, out uint currencyID))
            {
                string? currencyName = currencyInfo.CurrencyLocalName(currencyID);
                permanentCurrencyName.Add(currencyName);
                options.Add(currencyName);
            }
        }
        foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
        {
            if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out _))
            {
                options.Add(currency);
            }
        }
    }

    // 处理插件语言表达 Handel the plugin UI's language
    private void LoadLanguage(Plugin plugin)
    {
        playerLang = plugin.Configuration.SelectedLanguage;

        if (string.IsNullOrEmpty(playerLang))
        {
            playerLang = Service.ClientState.ClientLanguage.ToString();
            // 不受支持的语言 => 英语 Not Supported Languages => English
            if (playerLang != "ChineseSimplified" && playerLang != "English")
            {
                playerLang = "English";
            }
        }

        Lang.LoadLanguage(playerLang);
    }

    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn) return;
        transactions ??= new Transactions();

        if (Plugin.Instance.PluginInterface.IsDev)
        {
            FeaturesUnderTest();
        }

        ImGui.TextColored(ImGuiColors.DalamudYellow, Lang.GetText("ConfigLabel"));

        ReverseSort();
        ImGui.SameLine();
        TimeClustering();
        ImGui.SameLine();
        SortByChange();

        SortByTime();

        ImGui.TextColored(ImGuiColors.DalamudYellow, Lang.GetText("ConfigLabel1"));

        TrackInDuty();
        ImGui.SameLine();
        MinTrackChangeInDuty();
        ImGui.SameLine();
        CustomCurrencyToTrack();
        ImGui.SameLine();
        MergeTransactions();
        ImGui.SameLine();
        ClearExceptions();
        ImGui.SameLine();
        ExportToCSV();
        ImGui.SameLine();
        OpenDataFolder();
        ImGui.SameLine();
        LanguageSwitch();
        ImGui.SameLine();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        AvailabelCurrenciesListBox();

        TransactionsChildframe();
    }

    // 测试用功能区 Some features shown still under testing
    private void FeaturesUnderTest()
    {
    }

    // 倒序排序 Reverse Sort
    private void ReverseSort()
    {
        if (ImGui.Checkbox(Lang.GetText("ReverseSort"), ref isReversed))
        {
            Plugin.Instance.Configuration.ReverseSort = isReversed;
            Plugin.Instance.Configuration.Save();
        }
    }

    // 时间聚类 Time Clustering
    private void TimeClustering()
    {
        ImGui.Text(Lang.GetText("ClusterByTime"));

        ImGui.SameLine();
        ImGui.SetNextItemWidth(115);
        if (ImGui.InputInt(Lang.GetText("ClusterInterval"), ref clusterHour, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (clusterHour <= 0)
            {
                clusterHour = 0;
            }
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker($"{Lang.GetText("ClusterByTimeHelp1")}{clusterHour}{Lang.GetText("ClusterByTimeHelp2")}");
    }

    // 按收支数筛选 Sort By Change
    private void SortByChange()
    {
        ImGui.Checkbox(Lang.GetText("ChangeFilterEnabled"), ref isChangeFilterEnabled);

        if (isChangeFilterEnabled)
        {
            ImGui.SameLine();
            ImGui.Text(Lang.GetText("ChangeFilterLabel"));

            ImGui.SameLine();
            ImGui.RadioButton($"{Lang.GetText("Greater")}##FilterMode", ref filterMode, 0);
            ImGui.SameLine();
            ImGui.RadioButton($"{Lang.GetText("Less")}##FilterMode", ref filterMode, 1);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(130);
            ImGui.InputInt($"{Lang.GetText("ChangeFilterValueLabel")}##FilterValue", ref filterValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue);
        }
    }

    // 按收支数筛选 Sort By Time
    private void SortByTime()
    {
        ImGui.Checkbox($"{Lang.GetText("FilterByTime")}##TimeFilter", ref isTimeFilterEnabled);

        if (isTimeFilterEnabled)
        {
            int startYear = filterEndDate.Year;
            int startMonth = filterStartDate.Month;
            int startDay = filterStartDate.Day;
            int endYear = filterEndDate.Year;
            int endMonth = filterEndDate.Month;
            int endDay = filterEndDate.Day;

            ImGui.SameLine();
            ImGui.Text(Lang.GetText("TimeFilterLabel"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(125);
            if (ImGui.InputInt($"{Lang.GetText("Year")}##StartYear", ref startYear, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterStartDate = new DateTime(startYear, startMonth, startDay);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt($"{Lang.GetText("Month")}##StartMonth", ref startMonth, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterStartDate = new DateTime(startYear, startMonth, startDay);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt($"{Lang.GetText("Day")}##StartDay", ref startDay, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterStartDate = new DateTime(startYear, startMonth, startDay);
            }

            ImGui.SameLine();
            ImGui.Text(Lang.GetText("TimeFilterLabel1"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(125);
            if (ImGui.InputInt($"{Lang.GetText("Year")}##EndYear", ref endYear, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterEndDate = new DateTime(endYear, endMonth, endDay);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt($"{Lang.GetText("Month")}##EndMonth", ref endMonth, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterEndDate = new DateTime(endYear, endMonth, DateTime.DaysInMonth(endYear, endMonth));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt($"{Lang.GetText("Day")}##EndDay", ref endDay, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterEndDate = new DateTime(endYear, endMonth, endDay);
            }
            ImGui.SameLine();
            ImGui.Text(Lang.GetText("TimeFilterLabel2"));
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

    // 副本内最小记录值 Minimum Change Permitted to Create a New Transaction When in Duty
    private void MinTrackChangeInDuty()
    {
        ImGui.Text(Lang.GetText("MinimumRecordValue"));

        ImGui.SameLine();
        ImGui.SetNextItemWidth(135);
        if (ImGui.InputInt("##MinTrackValue", ref minTrackValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (minTrackValue < 0) minTrackValue = 0;
            Plugin.Instance.Configuration.MinTrackValue = minTrackValue;
            Plugin.Instance.Configuration.Save();
        }
        ImGuiComponents.HelpMarker($"{Lang.GetText("MinimumRecordValueHelp")}{minTrackValue}{Lang.GetText("MinimumRecordValueHelp1")}");
    }

    // 自定义货币追踪 Custom Currencies To Track
    private void CustomCurrencyToTrack()
    {
        if (ImGui.Button(Lang.GetText("CustomCurrencyLabel")))
        {
            ImGui.OpenPopup("CustomCurrency");
        }

        if (ImGui.BeginPopup("CustomCurrency"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, Lang.GetText("CustomCurrencyLabel1"));
            ImGuiComponents.HelpMarker(Lang.GetText("CustomCurrencyHelp"));
            ImGui.Text(Lang.GetText("CustomCurrencyLabel2"));
            if (ImGui.BeginCombo("", Plugin.Instance.ItemNames.TryGetValue(customCurrency, out var selected) ? selected : Lang.GetText("CustomCurrencyLabel3")))
            {
                ImGui.SetNextItemWidth(200f);
                ImGui.InputTextWithHint("##selectflts", Lang.GetText("CustomCurrencyLabel4"), ref searchFilter, 50);
                ImGui.Separator();

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

                    if (searchFilter != string.Empty && !x.Value.Contains(searchFilter)) continue;

                    if (ImGui.Selectable(x.Value))
                    {
                        customCurrency = x.Key;
                    }

                    if (ImGui.IsWindowAppearing() && customCurrency == x.Key)
                    {
                        ImGui.SetScrollHereY();
                    }
                }
                ImGui.EndCombo();
            }

            if (ImGui.Button($"{Lang.GetText("Add")}{selected}"))
            {
#pragma warning disable CS8604 // 引用类型参数可能为 null。
                if (options.Contains(selected))
                {
                    Service.Chat.Print(Lang.GetText("CustomCurrencyHelp1"));
                    return;
                }
#pragma warning restore CS8604 // 引用类型参数可能为 null。
                // 配置保存一份
                Plugin.Instance.Configuration.CustomCurrencies.Add(selected, customCurrency);
                Plugin.Instance.Configuration.CustomCurrencyType.Add(selected);
                Plugin.Instance.Configuration.Save();
                options.Add(selected);
            }
            ImGui.SameLine();
            if (ImGui.Button($"{Lang.GetText("Delete")}{selected}"))
            {
#pragma warning disable CS8604 // 引用类型参数可能为 null。
                if (!options.Contains(selected))
                {
                    Service.Chat.Print(Lang.GetText("CustomCurrencyHelp2"));
                    return;
                }
#pragma warning restore CS8604 // 引用类型参数可能为 null。
                Plugin.Instance.Configuration.CustomCurrencies.Remove(selected);
                Plugin.Instance.Configuration.CustomCurrencyType.Remove(selected);
                Plugin.Instance.Configuration.Save();
                options.Remove(selected);
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
            ImGui.Text(Lang.GetText("MergeTransactionsLabel1"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("##MergeThreshold", ref mergeThreshold, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue);
            if (mergeThreshold < 0)
            {
                mergeThreshold = 0;
            }

            ImGui.SameLine();
            if (ImGui.Button(Lang.GetText("Confirm")))
            {
                if (!string.IsNullOrEmpty(selectedCurrencyName))
                {
                    if (mergeThreshold == 0)
                    {
                        Service.Chat.PrintError(Lang.GetText("MergeTransactionsHelp"));
                        return;
                    }
                    else
                    {
                        var mergeCount = transactions.MergeTransactionsByLocationAndThreshold(selectedCurrencyName, mergeThreshold);
                        if (mergeCount > 0)
                            Service.Chat.Print($"{Lang.GetText("MergeTransactionsHelp1")}{mergeCount}{Lang.GetText("MergeTransactionsHelp2")}");
                        else
                        {
                            Service.Chat.PrintError(Lang.GetText("TransactionsHelp"));
                            return;
                        }
                    }
                }
                else
                {
                    Service.Chat.PrintError(Lang.GetText("TransactionsHelp1"));
                    return;
                }
            }

            ImGui.SameLine();
            ImGuiComponents.HelpMarker($"{Lang.GetText("MergeTransactionsHelp3")}{Lang.GetText("MergeTransactionsHelp4")}{Lang.GetText("TransactionsHelp2")}");
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
            if (ImGui.Button(Lang.GetText("Confirm"))) ClearExceptionRecords();
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
                if (selectedCurrencyName != null)
                {
                    ExportToCsv(currentTypeTransactions, fileName);
                }
                else
                {
                    Service.Chat.Print(Lang.GetText("ExportCsvMessage"));
                    return;
                }
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

    // 界面语言切换功能 UI Language Switch
    private void LanguageSwitch()
    {
        if (ImGui.Button("Languages"))
        {
            ImGui.OpenPopup(str_id: "LanguagesList");
        }
        if (ImGui.BeginPopup("LanguagesList"))
        {
            if (ImGui.Button("English"))
            {
                Lang.LoadLanguage("English");

                playerLang = "English";
                Plugin.Instance.Configuration.SelectedLanguage = playerLang;
                Plugin.Instance.Configuration.Save();
            }
            if (ImGui.Button("简体中文/Simplified Chinese"))
            {
                Lang.LoadLanguage("ChineseSimplified");

                playerLang = "ChineseSimplified";
                Plugin.Instance.Configuration.SelectedLanguage = playerLang;
                Plugin.Instance.Configuration.Save();
            }
            ImGui.EndPopup();
        }
    }

    // 存储可用货币名称选项的列表框 Listbox Containing Available Currencies' Name
    private void AvailabelCurrenciesListBox()
    {
        ImGui.SetNextItemWidth(240);
        if (ImGui.ListBox("", ref selectedOptionIndex, options.ToArray(), options.Count, 19))
        {
            selectedCurrencyName = options[selectedOptionIndex];
        }
    }

    // 显示收支记录的表格子窗体 Childframe Used to Show Transactions in Form
    private void TransactionsChildframe()
    {
        if (string.IsNullOrEmpty(selectedCurrencyName))
            return;

        float ListBoxHeight = ImGui.GetFrameHeight() * 19 - 25;
        Vector2 childScale = new Vector2(ImGui.GetWindowWidth() - 100, ListBoxHeight);

        ImGui.SameLine();

        if (ImGui.BeginChildFrame(1, childScale, ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            currentTypeTransactions = transactions.LoadAllTransactions(selectedCurrencyName);

            if (clusterHour > 0)
            {
                TimeSpan interval = TimeSpan.FromHours(clusterHour);
                currentTypeTransactions = transactions.ClusterTransactionsByTime(currentTypeTransactions, interval);
            }

            if (isChangeFilterEnabled)
                currentTypeTransactions = ApplyChangeFilter(currentTypeTransactions);

            if (isTimeFilterEnabled)
                currentTypeTransactions = ApplyDateTimeFilter(currentTypeTransactions);

            int pageCount = (int)Math.Ceiling((double)currentTypeTransactions.Count / transactionsPerPage);
            if (pageCount > 0)
            {
                currentPage = Math.Clamp(currentPage, 0, pageCount - 1);
            }
            else
            {
                return;
            }

            ImGui.Text(Lang.GetText("TransactionsPerPage"));

            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
                transactionsPerPage = Math.Max(transactionsPerPage, 0);

            ImGui.SameLine();
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 360) / 2);
            if (ImGui.Button(Lang.GetText("PreviousPage")) && currentPage > 0)
                currentPage--;

            ImGui.SameLine();
            ImGui.Text($"{Lang.GetText("Di")}{currentPage + 1}{Lang.GetText("Page")} / {Lang.GetText("Gong")}{pageCount}{Lang.GetText("Page")}");

            ImGui.SameLine();
            if (ImGui.Button(Lang.GetText("NextPage")) && currentPage < pageCount - 1)
                currentPage++;

            visibleStartIndex = currentPage * transactionsPerPage;
            visibleEndIndex = Math.Min(visibleStartIndex + transactionsPerPage, currentTypeTransactions.Count);

            ImGui.Separator();

            ImGui.Columns(4, "LogColumns");
            ImGui.Text(Lang.GetText("Column"));
            ImGui.NextColumn();
            ImGui.Text(Lang.GetText("Column1"));
            ImGui.NextColumn();
            ImGui.Text(Lang.GetText("Column2"));
            ImGui.NextColumn();
            ImGui.Text(Lang.GetText("Column3"));
            ImGui.NextColumn();
            ImGui.Separator();

            for (int i = visibleStartIndex; i < visibleEndIndex; i++)
            {
                var transaction = currentTypeTransactions[i];
                ImGui.Text(transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"));
                ImGui.NextColumn();
                ImGui.Text(transaction.Amount.ToString("#,##0"));
                ImGui.NextColumn();
                ImGui.Text(transaction.Change.ToString("+ #,##0;- #,##0;0"));
                ImGui.NextColumn();
                ImGui.Text(transaction.LocationName);
                ImGui.NextColumn();
            }

            ImGui.EndChildFrame();
        }
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

    // 导出当前显示的交易记录为 CSV 文件 Export Transactions Now Shown on Screen To a .csv File
    private void ExportToCsv(List<TransactionsConvertor> transactions, string FileName)
    {
        if (transactions == null || transactions.Count == 0)
        {
            Service.Chat.Print(Lang.GetText("ExportCsvMessage1"));

            return;
        }

        var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
        var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
        string playerDataFolder = Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

        string NowTime = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");
        string finalFileName = $"{FileName}_{selectedCurrencyName}_{NowTime}.csv";
        string filePath = Path.Join(playerDataFolder ?? "", finalFileName);

        using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            writer.WriteLine(Lang.GetText("ExportCsvMessage2"));

            foreach (var transaction in transactions)
            {
                string line = $"{transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")},{transaction.Amount},{transaction.Change},{transaction.LocationName}";
                writer.WriteLine(line);
            }
        }
        Service.Chat.Print($"{Lang.GetText("ExportCsvMessage3")}{filePath}");
    }

    // 异常记录清除 Clear Exception Transactions
    private void ClearExceptionRecords()
    {
        transactionsConvertor = new TransactionsConvertor();
        if (string.IsNullOrEmpty(selectedCurrencyName))
        {
            Service.Chat.PrintError(Lang.GetText("TransactionsHelp1"));

            return;
        }

        var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
        var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
        string playerDataFolder = Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

        string filePath = Path.Join(playerDataFolder ?? "", $"{selectedCurrencyName}.txt");

        List<TransactionsConvertor> allTransactions = TransactionsConvertor.FromFile(filePath, TransactionsConvertor.FromFileLine);
        List<TransactionsConvertor> recordsToRemove = new List<TransactionsConvertor>();

        for (int i = 0; i < allTransactions.Count; i++)
        {
            var transaction = allTransactions[i];

            if (i == 0 && transaction.Change == transaction.Amount)
            {
                continue;
            }

            if (transaction.Change == 0 || transaction.Change == transaction.Amount)
            {
                recordsToRemove.Add(transaction);
            }
        }

        if (recordsToRemove.Count > 0)
        {
            foreach (var record in recordsToRemove)
            {
                allTransactions.Remove(record);
            }

            transactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);

            Service.Chat.Print($"{Lang.GetText("ClearExTransactionsHelp2")}{recordsToRemove.Count}{Lang.GetText("ClearExTransactionsHelp3")}");
        }
        else
        {
            Service.Chat.PrintError(Lang.GetText("TransactionsHelp"));
        }
    }
}
