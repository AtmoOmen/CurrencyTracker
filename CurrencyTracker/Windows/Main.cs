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
    private Transactions? transactions = null!;
    private CurrencyInfo? currencyInfo = null!;
    private readonly List<string> options = new List<string>();
    private string? selectedCurrencyName;
    private List<TransactionsConvetor> currentTypeTransactions = new List<TransactionsConvetor>();

    public Dictionary<string, string> CurrencyName = new Dictionary<string, string> { };


    public Main(Plugin plugin) : base("Currency Tracker 设置")
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
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn) return;
        transactions ??= new Transactions();

        ImGui.TextColored(ImGuiColors.DalamudYellow, "记录筛选排序选项:");
        if (ImGui.Checkbox("倒序排序    ", ref isReversed))
        {
            Plugin.GetPlugin.Configuration.ReverseSort = isReversed;
            Plugin.GetPlugin.Configuration.Save();
        }

        ImGui.SameLine();
        ImGui.Text("按时间聚类:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(115);
        if (ImGui.InputInt("小时", ref clusterHour, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (clusterHour <= 0)
            {
                clusterHour = 0;
            }
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker($"当前设置: 以 {clusterHour}小时 为间隔显示数据");
        ImGui.SameLine();
        ImGui.Text("    ");
        ImGui.SameLine();
        ImGui.Checkbox("收支筛选", ref isFilterEnabled);
        if (isFilterEnabled)
        {
            ImGui.SameLine();
            ImGui.Text("仅显示收支");

            ImGui.SameLine();
            ImGui.RadioButton("大于##FilterMode", ref filterMode, 0);
            ImGui.SameLine();
            ImGui.RadioButton("小于##FilterMode", ref filterMode, 1);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(130);
            ImGui.InputInt("的记录##FilterValue", ref filterValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue);
        }

        if (ImGui.Checkbox("记录副本内数据", ref isTrackedinDuty))
        {
            Plugin.GetPlugin.Configuration.TrackedInDuty = isTrackedinDuty;
            Plugin.GetPlugin.Configuration.Save();
        }
        ImGuiComponents.HelpMarker("部分副本、特殊场景探索中会给予货币奖励\n插件默认当玩家在副本、特殊场景探索中时暂停记录，等待玩家退出它们时再计算期间货币的总增减\n" +
            "你可以通过开启本项目关闭此功能\n\n注: 这可能会使插件记录大量小额数据，使得记录量大幅增加");
        ImGui.SameLine();
        ImGui.Text("    ");
        ImGui.SameLine();
        ImGui.Text("每页显示记录数:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        ImGui.InputInt("", ref transactionsPerPage);
        ImGui.SameLine();
        ImGui.Text("            ");
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 200);
        if (ImGui.Button("导出当前记录为CSV文件"))
        {
            ImGui.OpenPopup(str_id: "ExportFileRename");
        }

        if (ImGui.BeginPopup("ExportFileRename"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, "请输入文件名: (按Enter键确认)");
            ImGui.Text("文件名: ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText($"_{selectedCurrencyName}_当前时间.csv", ref fileName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if(selectedCurrencyName != null)
                {
                    ExportToCsv(currentTypeTransactions, fileName);
                }
                else
                {
                    Service.Chat.Print("导出CSV文件错误: 当前未选中任何货币类型");
                    return;
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("注1: 为避免错误，请勿输入过长的文件名\n注2: 导出的数据将会应用当前所启用的一切筛选条件(不包含分页)");
            ImGui.EndCombo();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(180);
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
                if (ImGui.Button("上一页") && currentPage > 0)
                {
                    currentPage--;
                }
                ImGui.SameLine();
                ImGui.Text($"第 {currentPage + 1} 页 / 共 {pageCount} 页");
                ImGui.SameLine();
                if (ImGui.Button("下一页") && currentPage < pageCount - 1)
                {
                    currentPage++;
                }
                ImGui.Separator();

                ImGui.Columns(3, "MoneyLogColumns");
                ImGui.Text("时间");
                ImGui.NextColumn();
                ImGui.Text("货币数");
                ImGui.NextColumn();
                ImGui.Text("收支");
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
            Service.Chat.Print("导出CSV文件错误: 当前货币下无收支记录");
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
            writer.WriteLine("时间,货币数,收支");
            foreach (var transaction in transactions)
            {
                string line = $"{transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")},{transaction.Amount},{transaction.Change}";
                writer.WriteLine(line);
            }
            writer.WriteLine();
            writer.WriteLine($"当前货币:,{selectedCurrencyName},");
            if (Plugin.GetPlugin.Configuration.ReverseSort) writer.WriteLine("规则:倒序排列,,");
            if (clusterHour > 0) writer.WriteLine($"规则:时间聚类,时间间隔:{clusterHour}小时,");
            if (isFilterEnabled)
            {
                switch (filterMode)
                {
                    case 0:
                        writer.WriteLine($"规则:收支筛选,显示大于{filterValue}的记录,");
                        break;
                    case 1:
                        writer.WriteLine($"规则:收支筛选,显示小于{filterValue}的记录,");
                        break;
                    default:
                        writer.WriteLine($"规则:收支筛选,筛选模式非法请上报,");
                        break;
                }
            }
        }
        Service.Chat.Print($"已导出CSV文件至: {filePath}");
    }
}
