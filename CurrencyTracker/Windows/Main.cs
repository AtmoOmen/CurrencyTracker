using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Numerics;
using System.IO;
using System.Collections.Generic;
using ImGuiNET;
using Dalamud.Logging;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Components;
using CurrencyTracker.Manager;
using CurrencyTracker.Manger;
namespace CurrencyTracker.Windows;

public class Main : Window, IDisposable
{
    // 时间聚类 Time Clustering
    private int clusterHour = 0;
    // 倒序排序开关 Reverse Sorting Switch
    private bool isReversed = false;
    // 副本内记录开关 Duty Tracking Switch
    private bool isTrackedinDuty = false;
    // 收支筛选开关 Income/Expense Filtering Switch
    private bool isChangeFilterEnabled = false;
    // 时间筛选开关 Time Filtering Switch
    private bool isTimeFilterEnabled = false;
    // 筛选时间段的起始 Filtering Time Period
    private DateTime filterStartDate = DateTime.MinValue;
    private DateTime filterEndDate = DateTime.Now;
    // 筛选模式：0为大于，1为小于 Filtering Mode: 0 for Above, 1 for Below
    private int filterMode = 0;
    // 用户指定的筛选值 User-Specified Filtering Value
    private int filterValue = 0;
    // 每页显示的交易记录数 Number of Transaction Records Displayed Per Page
    private int transactionsPerPage = 20;
    // 当前页码 Current Page Number
    private int currentPage = 0;
    // 自定义追踪物品ID Custom Tracked Currency ID
    private uint customCurrency = uint.MaxValue;
    // CSV文件名 CSV File Name
    private string fileName = string.Empty;
    // 最小记录值 Minimum Tracking Value
    private int minTrackValue = 0;
    // 默认选中的选项 Default Selected Option
    private int selectedOptionIndex = -1;
    // 选择的语言 Selected Language
    private string playerLang = string.Empty;
    // 当前选中的货币名称 Currently Selected Currency Name
    private string? selectedCurrencyName;
    // 搜索框值 Search Filter
    private static string searchFilter = string.Empty;

    private Transactions? transactions = null!;
    private TransactionsConvetor? transactionsConvetor = null!;
    private CurrencyInfo? currencyInfo = null!;
    private LanguageManager? lang;
    private List<string> permanentCurrencyName = new List<string>();
    private List<string> options = new List<string>();
    private List<TransactionsConvetor> currentTypeTransactions = new List<TransactionsConvetor>();



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
        
        LoadOptions(plugin);
        LoadLanguage(plugin);
        
    }

    // 将预置货币类型、玩家自定义的货币类型加入选项列表 Add preset currencies and player-customed currencies to the list of options
    private void LoadOptions(Plugin plugin)
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
        foreach (var currency in Plugin.GetPlugin.Configuration.CustomCurrencyType)
        {
            if (Plugin.GetPlugin.Configuration.CustomCurrecies.TryGetValue(currency, out uint currencyID))
            {
                options.Add(currency);
            }
        }
    }

    // 处理插件语言表达 Handel the plugin UI's language
    private void LoadLanguage(Plugin plugin)
    {
        lang = new LanguageManager();
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

        lang.LoadLanguage(playerLang);
    }



    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn) return;
        transactions ??= new Transactions();


        if (Plugin.GetPlugin.PluginInterface.IsDev)
        {
            FeaturesUnderTest();
        }

#pragma warning disable CS8602 // 解引用可能出现空引用。
        ImGui.TextColored(ImGuiColors.DalamudYellow, lang.GetText("ConfigLabel"));
#pragma warning restore CS8602 // 解引用可能出现空引用。

        ReverseSort();
        ImGui.SameLine();
        TimeClustering();
        ImGui.SameLine();
        SortByChange();

        SortByTime();

        ImGui.TextColored(ImGuiColors.DalamudYellow, lang.GetText("ConfigLabel1"));

        TrackInDuty();
        ImGui.SameLine();
        MinTrackChangeInDuty();
        ImGui.SameLine();
        CustomCurrencyToTrack();
        ImGui.SameLine();
        ExportToCSV();
        ImGui.SameLine();
        OpenDataFolder();
        ImGui.SameLine();
        LanguageSwitch();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        AvailabelCurrenciesListBox();

        TransactionsChildframe();
    }

    // 测试用功能区 Some features shown still under testing
    private void FeaturesUnderTest()
    {
        if (ImGui.Button("移除所有异常记录"))
        {
            ClearExceptionRecords();
        }
    }

    // 倒序排序 Reverse Sort
    private void ReverseSort()
    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
        if (ImGui.Checkbox(lang.GetText("ReverseSort"), ref isReversed))
        {
            Plugin.GetPlugin.Configuration.ReverseSort = isReversed;
            Plugin.GetPlugin.Configuration.Save();
        }
#pragma warning restore CS8602 // 解引用可能出现空引用。
    }

    // 时间聚类 Time Clustering
    private void TimeClustering()
    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
        ImGui.Text(lang.GetText("ClusterByTime"));
#pragma warning restore CS8602 // 解引用可能出现空引用。
        ImGui.SameLine();
        ImGui.SetNextItemWidth(115);
        if (ImGui.InputInt(lang.GetText("ClusterInterval"), ref clusterHour, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (clusterHour <= 0)
            {
                clusterHour = 0;
            }
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(lang.GetText("ClusterByTimeHelp1") + $"{clusterHour}" + lang.GetText("ClusterByTimeHelp2"));
    }

    // 按收支数筛选 Sort By Change
    private void SortByChange()
    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
        ImGui.Checkbox(lang.GetText("ChangeFilterEnabled"), ref isChangeFilterEnabled);
#pragma warning restore CS8602 // 解引用可能出现空引用。
        if (isChangeFilterEnabled)
        {
            ImGui.SameLine();
            ImGui.Text(lang.GetText("ChangeFilterLabel"));

            ImGui.SameLine();
            ImGui.RadioButton(lang.GetText("Greater") + "##FilterMode", ref filterMode, 0);
            ImGui.SameLine();
            ImGui.RadioButton(lang.GetText("Less") + "##FilterMode", ref filterMode, 1);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(130);
            ImGui.InputInt(lang.GetText("ChangeFilterValueLabel") + "##FilterValue", ref filterValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue);
        }
    }

    // 按收支数筛选 Sort By Time
    private void SortByTime()
    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
        ImGui.Checkbox(lang.GetText("FilterByTime") + "##TimeFilter", ref isTimeFilterEnabled);
#pragma warning restore CS8602 // 解引用可能出现空引用。
        if (isTimeFilterEnabled)
        {
            int startYear = filterEndDate.Year;
            int startMonth = filterStartDate.Month;
            int startDay = filterStartDate.Day;
            int endYear = filterEndDate.Year;
            int endMonth = filterEndDate.Month;
            int endDay = filterEndDate.Day;

            ImGui.SameLine();
            ImGui.Text(lang.GetText("TimeFilterLabel"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(125);
            if (ImGui.InputInt(lang.GetText("Year") + "##StartYear", ref startYear, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterStartDate = new DateTime(startYear, startMonth, startDay);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt(lang.GetText("Month") + "##StartMonth", ref startMonth, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterStartDate = new DateTime(startYear, startMonth, startDay);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt(lang.GetText("Day") + "##StartDay", ref startDay, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterStartDate = new DateTime(startYear, startMonth, startDay);
            }

            ImGui.SameLine();
            ImGui.Text(lang.GetText("TimeFilterLabel1"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(125);
            if (ImGui.InputInt(lang.GetText("Year") + "##EndYear", ref endYear, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterEndDate = new DateTime(endYear, endMonth, endDay);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt(lang.GetText("Month") + "##EndMonth", ref endMonth, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterEndDate = new DateTime(endYear, endMonth, DateTime.DaysInMonth(endYear, endMonth));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt(lang.GetText("Day") + "##EndDay", ref endDay, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                filterEndDate = new DateTime(endYear, endMonth, endDay);
            }
            ImGui.SameLine();
            ImGui.Text(lang.GetText("TimeFilterLabel2"));
        }
    }

    // 是否在副本内记录数据 Track in Duty Switch
    private void TrackInDuty()
    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
        if (ImGui.Checkbox(lang.GetText("TrackInDuty"), ref isTrackedinDuty))
        {
            Plugin.GetPlugin.Configuration.TrackedInDuty = isTrackedinDuty;
            Plugin.GetPlugin.Configuration.Save();
        }
#pragma warning restore CS8602 // 解引用可能出现空引用。
        ImGuiComponents.HelpMarker(lang.GetText("TrackInDutyHelp"));
    }

    // 副本内最小记录值 Minimum Change Permitted to Create a New Transaction When in Duty
    private void MinTrackChangeInDuty()
    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
        ImGui.Text(lang.GetText("MinimumRecordValue"));
#pragma warning restore CS8602 // 解引用可能出现空引用。
        ImGui.SameLine();
        ImGui.SetNextItemWidth(135);
        if (ImGui.InputInt("##MinTrackValue", ref minTrackValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (minTrackValue < 0) minTrackValue = 0;
            Plugin.GetPlugin.Configuration.MinTrackValue = minTrackValue;
            Plugin.GetPlugin.Configuration.Save();
        }
        ImGuiComponents.HelpMarker(lang.GetText("MinimumRecordValueHelp") + $"{minTrackValue}" + lang.GetText("MinimumRecordValueHelp1"));
    }

    // 自定义货币追踪 Custom Currencies To Track
    private void CustomCurrencyToTrack()
    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
        if (ImGui.Button(lang.GetText("CustomCurrencyLabel")))
        {
            ImGui.OpenPopup("CustomCurrency");
        }
#pragma warning restore CS8602 // 解引用可能出现空引用。
        if (ImGui.BeginPopup("CustomCurrency"))
        {

            ImGui.TextColored(ImGuiColors.DalamudYellow, lang.GetText("CustomCurrencyLabel1"));
            ImGuiComponents.HelpMarker(lang.GetText("CustomCurrencyHelp"));
            ImGui.Text(lang.GetText("CustomCurrencyLabel2"));
            if (ImGui.BeginCombo("", Plugin.GetPlugin.ItemNames.TryGetValue(customCurrency, out var selected) ? selected : lang.GetText("CustomCurrencyLabel3")))
            {
                ImGui.SetNextItemWidth(200f);
                ImGui.InputTextWithHint("##selectflts", lang.GetText("CustomCurrencyLabel4"), ref searchFilter, 50);
                ImGui.Separator();

                foreach (var x in Plugin.GetPlugin.ItemNames)
                {
                    // 检查并跳过存在于插件预设货币的物品
                    bool shouldSkip = false;
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

            if (ImGui.Button(lang.GetText("Add") + $"{selected}"))
            {
#pragma warning disable CS8604 // 引用类型参数可能为 null。
                if (options.Contains(selected))
                {
                    Service.Chat.Print(lang.GetText("CustomCurrencyHelp1"));
                    return;
                }
#pragma warning restore CS8604 // 引用类型参数可能为 null。
                              // 配置保存一份
                Plugin.GetPlugin.Configuration.CustomCurrecies.Add(selected, customCurrency);
                Plugin.GetPlugin.Configuration.CustomCurrencyType.Add(selected);
                Plugin.GetPlugin.Configuration.Save();
                options.Add(selected);

            }
            ImGui.SameLine();
            if (ImGui.Button(lang.GetText("Delete") + $"{selected}"))
            {
#pragma warning disable CS8604 // 引用类型参数可能为 null。
                if (!options.Contains(selected))
                {
                    Service.Chat.Print(lang.GetText("CustomCurrencyHelp2"));
                    return;
                }
#pragma warning restore CS8604 // 引用类型参数可能为 null。
                Plugin.GetPlugin.Configuration.CustomCurrecies.Remove(selected);
                Plugin.GetPlugin.Configuration.CustomCurrencyType.Remove(selected);
                Plugin.GetPlugin.Configuration.Save();
                options.Remove(selected);
            }
            ImGui.EndPopup();
        }
    }

    // 导出数据为.CSV文件 Export Transactions To a .csv File
    private void ExportToCSV()
    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
        if (ImGui.Button(lang.GetText("ExportCsv")))
        {
            ImGui.OpenPopup(str_id: "ExportFileRename");
        }
#pragma warning restore CS8602 // 解引用可能出现空引用。
        if (ImGui.BeginPopup("ExportFileRename"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, lang.GetText("FileRenameLabel"));
            ImGui.Text(lang.GetText("FileRenameLabel1"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText($"_{selectedCurrencyName}_" + lang.GetText("FileRenameLabel2") + ".csv", ref fileName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (selectedCurrencyName != null)
                {
                    ExportToCsv(currentTypeTransactions, fileName);
                }
                else
                {
                    Service.Chat.Print(lang.GetText("ExportCsvMessage"));
                    return;
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(lang.GetText("FileRenameHelp"));
            ImGui.EndCombo();
        }
    }

    // 打开数据文件夹 Open Folder Containing Data Files
    private void OpenDataFolder()
    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
        if (ImGui.Button(lang.GetText("OpenDataFolder")))
        {
            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            string playerDataFolder = Path.Join(Plugin.GetPlugin.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

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
                    PluginLog.Error("不受支持的操作系统 / Unsupported Operating System");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("错误 / Error :" + ex.Message);
            }
        }
#pragma warning restore CS8602 // 解引用可能出现空引用。
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
#pragma warning disable CS8602 // 解引用可能出现空引用。
                lang.LoadLanguage("English");
#pragma warning restore CS8602 // 解引用可能出现空引用。
                playerLang = "English";
                Plugin.GetPlugin.Configuration.SelectedLanguage = playerLang;
                Plugin.GetPlugin.Configuration.Save();
            }
            if (ImGui.Button("简体中文/Simplified Chinese"))
            {
#pragma warning disable CS8602 // 解引用可能出现空引用。
                lang.LoadLanguage("ChineseSimplified");
#pragma warning restore CS8602 // 解引用可能出现空引用。
                playerLang = "ChineseSimplified";
                Plugin.GetPlugin.Configuration.SelectedLanguage = playerLang;
                Plugin.GetPlugin.Configuration.Save();
            }
            ImGui.EndCombo();
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
        float ListBoxHeight = ImGui.GetFrameHeight() * 19 - 25;
        Vector2 childScale = new Vector2(ImGui.GetWindowWidth() - 100, ListBoxHeight);

        ImGui.SameLine();
        if (ImGui.BeginChildFrame(1, childScale, ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            if (!string.IsNullOrEmpty(selectedCurrencyName))
            {
#pragma warning disable CS8602 // 解引用可能出现空引用。
                currentTypeTransactions = transactions.LoadAllTransactions(selectedCurrencyName);
#pragma warning restore CS8602 // 解引用可能出现空引用。

                if (clusterHour > 0)
                {
                    TimeSpan interval = TimeSpan.FromHours(clusterHour);
                    currentTypeTransactions = transactions.ClusterTransactionsByTime(currentTypeTransactions, interval);
                }

                if (isChangeFilterEnabled)
                {
                    currentTypeTransactions = ApplyChangeFilter(currentTypeTransactions);
                }

                if (isTimeFilterEnabled)
                {
                    currentTypeTransactions = ApplyDateTimeFilter(currentTypeTransactions);
                }

                int pageCount = (int)Math.Ceiling((double)currentTypeTransactions.Count / transactionsPerPage);
                currentPage = Math.Max(0, Math.Min(currentPage, pageCount - 1));

                int startIndex = currentPage * transactionsPerPage;
                int endIndex = Math.Min(startIndex + transactionsPerPage, currentTypeTransactions.Count);

                List<TransactionsConvetor> displayedTransactions = currentTypeTransactions.GetRange(startIndex, endIndex - startIndex);

                // 单页记录数
#pragma warning disable CS8602 // 解引用可能出现空引用。
                ImGui.Text(lang.GetText("TransactionsPerPage"));
#pragma warning restore CS8602 // 解引用可能出现空引用。
                ImGui.SameLine();
                ImGui.SetNextItemWidth(120);
                if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
                {
                    if (transactionsPerPage < 0) transactionsPerPage = 0;
                }

                // 翻页组件
                ImGui.SameLine();
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 360) / 2);
                if (ImGui.Button(lang.GetText("PreviousPage")) && currentPage > 0)
                {
                    currentPage--;
                }
                ImGui.SameLine();
                ImGui.Text(lang.GetText("Di") + $"{currentPage + 1}" + lang.GetText("Page") + " / " + lang.GetText("Gong") + $"{pageCount}" + lang.GetText("Page"));
                ImGui.SameLine();
                if (ImGui.Button(lang.GetText("NextPage")) && currentPage < pageCount - 1)
                {
                    currentPage++;
                }
                ImGui.Separator();

                ImGui.Columns(4, "LogColumns");
                ImGui.Text(lang.GetText("Column"));
                ImGui.NextColumn();
                ImGui.Text(lang.GetText("Column1"));
                ImGui.NextColumn();
                ImGui.Text(lang.GetText("Column2"));
                ImGui.NextColumn();
                ImGui.Text(lang.GetText("Column3"));
                ImGui.NextColumn();
                ImGui.Separator();

                foreach (var transaction in displayedTransactions)
                {
                    ImGui.Text(transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"));
                    ImGui.NextColumn();
                    ImGui.Text(transaction.Amount.ToString("#,##0"));
                    ImGui.NextColumn();
                    ImGui.Text(transaction.Change.ToString("+ #,##0;- #,##0;0"));
                    ImGui.NextColumn();
                    ImGui.Text(transaction.LocationName);
                    ImGui.NextColumn();
                }
            }

            ImGui.EndChildFrame();
        }
    }



    // 按收支隐藏不符合要求的交易记录 Hide Unmatched Transactions By Change
    private List<TransactionsConvetor> ApplyChangeFilter(List<TransactionsConvetor> transactions)
    {
        List<TransactionsConvetor> filteredTransactions = new List<TransactionsConvetor>();

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
    private List<TransactionsConvetor> ApplyDateTimeFilter(List<TransactionsConvetor> transactions)
    {
        List<TransactionsConvetor> filteredTransactions = new List<TransactionsConvetor>();

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
    private void ExportToCsv(List<TransactionsConvetor> transactions, string FileName)
    {
        if (transactions == null || transactions.Count == 0)
        {
#pragma warning disable CS8602 // 解引用可能出现空引用。
            Service.Chat.Print(lang.GetText("ExportCsvMessage1"));
#pragma warning restore CS8602 // 解引用可能出现空引用。
            return;
        }

        var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
        var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
        string playerDataFolder = Path.Join(Plugin.GetPlugin.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

        string NowTime = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");
        string finalFileName = $"{FileName}_{selectedCurrencyName}_{NowTime}.csv";
        string filePath = Path.Join(playerDataFolder ?? "", finalFileName);

        using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
        {
#pragma warning disable CS8602 // 解引用可能出现空引用。
            writer.WriteLine(lang.GetText("ExportCsvMessage2"));
#pragma warning restore CS8602 // 解引用可能出现空引用。
            foreach (var transaction in transactions)
            {
                string line = $"{transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")},{transaction.Amount},{transaction.Change},{transaction.LocationName}";
                writer.WriteLine(line);
            }
        }
        Service.Chat.Print(lang.GetText("ExportCsvMessage3") + $"{filePath}");
    }

    // 异常记录清除(测试用) Clear Exception Records(Still under Testing)
    private void ClearExceptionRecords()
    { 
        transactionsConvetor = new TransactionsConvetor();
        if (string.IsNullOrEmpty(selectedCurrencyName))
        {
            Service.Chat.Print("请选择一种货币类型。");
            return;
        }

        var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
        var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
        string playerDataFolder = Path.Join(Plugin.GetPlugin.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

        string filePath = Path.Join(playerDataFolder ?? "", $"{selectedCurrencyName}.txt");
        PluginLog.Debug($"当前读取文件路径: {filePath}");

        List<TransactionsConvetor> allTransactions = TransactionsConvetor.FromFile(filePath, TransactionsConvetor.FromFileLine);
        List<TransactionsConvetor> recordsToRemove = new List<TransactionsConvetor>();

        for (int i = 0; i < allTransactions.Count; i++)
        {
            var transaction = allTransactions[i];

            if (i == 0 && transaction.Change == transaction.Amount)
            {
                continue; // 不清除第一条满足条件的记录
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

            // 将清除后的交易记录重新写入文件
            transactionsConvetor.WriteTransactionsToFile(filePath, allTransactions);
            Service.Chat.Print($"已成功清除 {recordsToRemove.Count} 条异常记录。");
        }
        else
        {
            Service.Chat.Print("未找到符合条件的异常记录。");
        }
    }
}
