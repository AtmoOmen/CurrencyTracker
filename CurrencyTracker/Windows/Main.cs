using System;
using System.Collections.Generic;
using Dalamud.Interface.Colors;
using System.Text;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;
using CurrencyTracker.Manager;
using Dalamud.Interface.Components;
using System.IO;
using CurrencyTracker.Manger;

namespace CurrencyTracker.Windows;

public class Main : Window, IDisposable
{
    // 时间聚类小时数
    int clusterHour = 0;
    // 倒序排序开关
    bool isReversed = false;
    // 副本内记录开关
    bool isTrackedinDuty = false;
    // 筛选开关
    bool isFilterEnabled = false;
    // 筛选模式：0为大于，1为小于
    int filterMode = 0;
    // 用户指定的筛选值 (我想用long，但为啥Imgui只有InputInt没有InputLong)
    int filterValue = 0;
    // 每页显示的交易记录数
    int transactionsPerPage = 20;
    // 当前页码
    int currentPage = 0;
    // CSV文件名
    string fileName = string.Empty;


    // 默认选中的选项
    int selectedOptionIndex = -1;
    // 选择的语言
    string playerLang = string.Empty;

    private Transactions? transactions = null!;
    private CurrencyInfo? currencyInfo = null!;
    private readonly LanguageManager lang;
    private readonly List<string> options = new List<string>();
    private string? selectedCurrencyName;
    private List<TransactionsConvetor> currentTypeTransactions = new List<TransactionsConvetor>();

    public Dictionary<string, string> CurrencyName = new Dictionary<string, string> { };


    public Main(Plugin plugin) : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;

        transactions ??= new Transactions();
        currencyInfo ??= new CurrencyInfo();
        isReversed = plugin.Configuration.ReverseSort;
        isTrackedinDuty = plugin.Configuration.TrackedInDuty;
        foreach(var currency in Tracker.CurrencyType)
        {
            if (currencyInfo.permanentCurrencies.TryGetValue(currency, out uint currencyID))
            {
                string? currencyName = currencyInfo.CurrencyLocalName(currencyID);
                CurrencyName.Add(currency, currencyName);
                options.Add(currencyName);
            }
        }

        playerLang = plugin.Configuration.SelectedLanguage;
        if (string.IsNullOrEmpty(playerLang))
        {
            playerLang = Service.ClientState.ClientLanguage.ToString();
        }
        lang = new LanguageManager();
        lang.LoadLanguage(playerLang);
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn) return;
        transactions ??= new Transactions();

        ImGui.TextColored(ImGuiColors.DalamudYellow, lang.GetText("ConfigLabel"));
        if (ImGui.Checkbox(lang.GetText("ReverseSort"), ref isReversed))
        {
            Plugin.GetPlugin.Configuration.ReverseSort = isReversed;
            Plugin.GetPlugin.Configuration.Save();
        }

        ImGui.SameLine();
        ImGui.Text(lang.GetText("ClusterByTime"));
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
        ImGui.SameLine();
        ImGui.Text("    ");
        ImGui.SameLine();
        ImGui.Checkbox(lang.GetText("FilterEnabled"), ref isFilterEnabled);
        if (isFilterEnabled)
        {
            ImGui.SameLine();
            ImGui.Text(lang.GetText("FilterLabel"));

            ImGui.SameLine();
            ImGui.RadioButton(lang.GetText("Greater") + "##FilterMode", ref filterMode, 0);
            ImGui.SameLine();
            ImGui.RadioButton(lang.GetText("Less") + "##FilterMode", ref filterMode, 1);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(130);
            ImGui.InputInt(lang.GetText("FilterValueLabel") + "##FilterValue", ref filterValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue);
        }

        if (ImGui.Checkbox(lang.GetText("TrackInDuty"), ref isTrackedinDuty))
        {
            Plugin.GetPlugin.Configuration.TrackedInDuty = isTrackedinDuty;
            Plugin.GetPlugin.Configuration.Save();
        }
        ImGuiComponents.HelpMarker(lang.GetText("TrackInDutyHelp"));
        ImGui.SameLine();
        ImGui.Text("    ");
        ImGui.SameLine();
        ImGui.Text(lang.GetText("TransactionsPerPage"));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        ImGui.InputInt("", ref transactionsPerPage);
        ImGui.SameLine();
        ImGui.Text("            ");


        // 语言选项 Language Options
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 305);
        if (ImGui.Button(lang.GetText("Languages")))
        {
            ImGui.OpenPopup(str_id: "LanguagesList");
        }
        if (ImGui.BeginPopup("LanguagesList"))
        {
            if (ImGui.Button("English"))
            {
                lang.LoadLanguage("English");
                playerLang = "English";
                Plugin.GetPlugin.Configuration.SelectedLanguage = playerLang;
                Plugin.GetPlugin.Configuration.Save();
            }
            if (ImGui.Button("Simplified Chinese/简体中文"))
            {
                lang.LoadLanguage("ChineseSimplified");
                playerLang = "ChineseSimplified";
                Plugin.GetPlugin.Configuration.SelectedLanguage = playerLang;
                Plugin.GetPlugin.Configuration.Save();
            }
            ImGui.EndCombo();
        }


        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 200);
        if (ImGui.Button(lang.GetText("ExportCsv")))
        {
            ImGui.OpenPopup(str_id: "ExportFileRename");
        }

        if (ImGui.BeginPopup("ExportFileRename"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, lang.GetText("FileRenameLabel"));
            ImGui.Text(lang.GetText("FileRenameLabel1"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText($"_{selectedCurrencyName}_" + lang.GetText("FileRenameLabel2") + ".csv", ref fileName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if(selectedCurrencyName != null)
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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.SetNextItemWidth(240);
        if (ImGui.ListBox("", ref selectedOptionIndex, options.ToArray(), options.Count, 15))
        {
            selectedCurrencyName = options[selectedOptionIndex];
        }

        // 获取列表框高度再加一些奇异搞笑的处理以让它看起来真的和列表框一样高
        float ListBoxHeight = ImGui.GetFrameHeight() * 15 - 15;
        Vector2 childScale = new Vector2(ImGui.GetWindowWidth() - 100, ListBoxHeight);
        ImGui.SameLine();
        if (ImGui.BeginChildFrame(1, childScale, ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            if (!string.IsNullOrEmpty(selectedCurrencyName))
            {
                currentTypeTransactions = transactions.LoadAllTransactions(selectedCurrencyName);

                if (clusterHour > 0)
                {
                    TimeSpan interval = TimeSpan.FromHours(clusterHour);
                    currentTypeTransactions = transactions.ClusterTransactionsByTime(currentTypeTransactions, interval);
                }

                if (isFilterEnabled)
                {
                    currentTypeTransactions = ApplyTransactionFilter(currentTypeTransactions);
                }

                int pageCount = (int)Math.Ceiling((double)currentTypeTransactions.Count / transactionsPerPage);
                currentPage = Math.Max(0, Math.Min(currentPage, pageCount - 1)); // 限制当前页码在合法范围内

                int startIndex = currentPage * transactionsPerPage;
                int endIndex = Math.Min(startIndex + transactionsPerPage, currentTypeTransactions.Count);

                List<TransactionsConvetor> displayedTransactions = currentTypeTransactions.GetRange(startIndex, endIndex - startIndex);

                // 尝试用一种奇异的方式使这个翻页组件居中
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 360) / 2);
                if (ImGui.Button(lang.GetText("PreviousPage")) && currentPage > 0)
                {
                    currentPage--;
                }
                ImGui.SameLine();
                ImGui.Text(lang.GetText("Di")+ $"{currentPage + 1}" + lang.GetText("Page") + " / " + lang.GetText("Gong") + $"{pageCount}" + lang.GetText("Page"));
                ImGui.SameLine();
                if (ImGui.Button(lang.GetText("NextPage")) && currentPage < pageCount - 1)
                {
                    currentPage++;
                }
                ImGui.Separator();

                ImGui.Columns(3, "LogColumns");
                ImGui.Text(lang.GetText("Column"));
                ImGui.NextColumn();
                ImGui.Text(lang.GetText("Column1"));
                ImGui.NextColumn();
                ImGui.Text(lang.GetText("Column2"));
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
                }
            }

            ImGui.EndChildFrame();
        }
    }

    // 按收支隐藏不符合要求的交易记录 (总感觉记录到 Configuration 里再传递给 Transactions 类很奇怪，就写在这里了)
    private List<TransactionsConvetor> ApplyTransactionFilter(List<TransactionsConvetor> transactions)
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

    // 导出当前显示的交易记录为 CSV 文件
    // (因为筛选大部分都是在主界面写的逻辑，所以导出也放这里了)
    private void ExportToCsv(List<TransactionsConvetor> transactions, string FileName)
    {
        if (transactions == null || transactions.Count == 0)
        {
            Service.Chat.Print(lang.GetText("ExportCsvMessage1"));
            return;
        }

        var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
        var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
        string playerDataFolder = Path.Join(Plugin.GetPlugin.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

        string NowTime = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");
        string finalFileName = $"{FileName}_{selectedCurrencyName}_{NowTime}.csv";
        string filePath = Path.Join(playerDataFolder ?? "", finalFileName);

        using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8)) // 指定 UTF-8 编码
        {
            writer.WriteLine(lang.GetText("ExportCsvMessage2"));
            foreach (var transaction in transactions)
            {
                string line = $"{transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")},{transaction.Amount},{transaction.Change}";
                writer.WriteLine(line);
            }
        }
        Service.Chat.Print(lang.GetText("ExportCsvMessage3") + $"{filePath}");
    }
}
