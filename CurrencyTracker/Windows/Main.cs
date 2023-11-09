using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Trackers;
using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

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
        searchTimer.Elapsed -= SearchTimerElapsed;
        searchTimer.Stop();
    }

    // 初始化 Initialize
    private void Initialize(Plugin plugin)
    {
        isReversed = C.ReverseSort;
        recordMode = C.TrackMode;
        transactionsPerPage = C.RecordsPerPage;
        ordedOptions = C.OrdedOptions;
        hiddenOptions = C.HiddenOptions;
        isChangeColoring = C.ChangeTextColoring;
        positiveChangeColor = C.PositiveChangeColor;
        negativeChangeColor = C.NegativeChangeColor;
        exportDataFileType = C.ExportDataFileType;
        isShowLocationColumn = C.ShowLocationColumn;
        isShowNoteColumn = C.ShowNoteColumn;
        isShowOrderColumn = C.ShowOrderColumn;

        // 临时 Temp
        isRecordContentName = C.RecordContentName;
        isRecordTeleportDes = C.RecordTeleportDes;
        isRecordTeleport = C.RecordTeleport;
        isTrackinDuty = C.TrackedInDuty;
        isWaitExComplete = C.WaitExComplete;
        isRecordMGPSource = C.RecordMGPSource;
        isRecordTripleTriad = C.RecordTripleTriad;
        isRecordQuestName = C.RecordQuestName;
        isRecordTrade = C.RecordTrade;
        isRecordFate = C.RecordFate;
        isRecordIsland = C.RecordIsland;

        if (filterEndDate.Month == 1 && filterEndDate.Day == 1) filterStartDate = new DateTime(DateTime.Now.Year - 1, 12, 31);
        else filterStartDate = filterStartDate = filterEndDate.AddDays(-1);

        searchTimer.Elapsed += SearchTimerElapsed;
        searchTimer.AutoReset = false;

        LoadOptions();
    }

    // 将预置货币类型、玩家自定义的货币类型加入选项列表 Add preset currencies and player-customed currencies to the list of options
    private void LoadOptions()
    {
        var addedOptions = new HashSet<string>();

        foreach (var currency in C.PresetCurrencies.Keys.Concat(C.CustomCurrencies.Keys))
        {
            if (!addedOptions.Contains(currency) && !hiddenOptions.Contains(currency))
            {
                options.Add(currency);
                addedOptions.Add(currency);
                selectedStates.Add(currency, new List<bool>());
                selectedTransactions.Add(currency, new List<TransactionsConvertor>());
            }
        }

        if (ordedOptions == null)
        {
            ordedOptions = options;
            C.OrdedOptions = ordedOptions;
            C.Save();
        }
        else
        {
            ReloadOrderedOptions();
        }
    }

    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn) return;

        if (!showRecordOptions) ImGui.TextColored(ImGuiColors.DalamudGrey, Service.Lang.GetText("ConfigLabel1"));
        else ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("ConfigLabel1"));
        if (ImGui.IsItemClicked())
        {
            showRecordOptions = !showRecordOptions;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Service.Lang.GetText("ConfigLabelHelp"));
        }
        if (showRecordOptions)
        {
            TempRecordSettings();
            ImGui.SameLine();
            MergeTransactions();
            ImGui.SameLine();
            ClearExceptions();
        }

        if (!showRecordOptions && !showOthers) ImGui.SameLine();

        if (!showOthers) ImGui.TextColored(ImGuiColors.DalamudGrey, Service.Lang.GetText("ConfigLabel2"));
        else ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("ConfigLabel2"));
        if (ImGui.IsItemClicked())
        {
            showOthers = !showOthers;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Service.Lang.GetText("ConfigLabelHelp"));
        }
        if (showOthers)
        {
            ExportData();
            ImGui.SameLine();
            OpenDataFolder();
            ImGui.SameLine();
            OpenGitHubPage();
            ImGui.SameLine();
            HelpPages();
            ImGui.SameLine();
            LanguageSwitch();
            if (P.PluginInterface.IsDev)
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
        /*
        ImGui.SameLine();
        if (!Service.ClientState.IsPvP)
        {
            ImGui.Text($"当前区域ID:{Service.ClientState.TerritoryType}, 当前区域名: {Tracker.TerritoryNames[Service.ClientState.TerritoryType]}");
            ImGui.SameLine();
            if (Tracker.IsBoundByDuty())
            {
                ImGui.Text($"副本名:{Tracker.ContentNames[Service.ClientState.TerritoryType]}");
            }
        }
        ImGui.SameLine();
        var Wards = new int[]
        {
            425, 426, 427, 2341, 3687
        };
        Dictionary<uint, string> WardNamesDE = Service.DataManager.GetExcelSheet<PlaceName>(Dalamud.ClientLanguage.German)
                .Where(x => x.RowId == 425 || x.RowId == 426 || x.RowId == 427 || x.RowId == 2341 || x.RowId == 3687)
                .ToDictionary(
                x => x.RowId,
                x => x.Name.RawString);

        Dictionary<uint, string> WardNamesFR = Service.DataManager.GetExcelSheet<PlaceName>(Dalamud.ClientLanguage.French)
                .Where(x => x.RowId == 425 || x.RowId == 426 || x.RowId == 427 || x.RowId == 2341 || x.RowId == 3687)
                .ToDictionary(
                x => x.RowId,
                x => x.Name.RawString
            );
        if (ImGui.Button("德语法语"))
        {
            var name = string.Empty;
            foreach (var entry in WardNamesDE)
            {
                name += $"{entry.Value},";
            }
            name += "\n";
            foreach (var entry in WardNamesFR)
            {
                name += $"{entry.Value},";
            }
            ImGui.SetClipboardText(name);
        }

        if (ImGui.Button("获取测试数据"))
        {
            testResult = currencyInfo.GetRetainerAmount(1);
            testResult2 = currencyInfo.GetRetainerID();
        }
        ImGui.SameLine();
        ImGui.Text($"测试1:{testResult}测试2:{testResult2}");
        */
    }

    // (临时)记录设置 (Temp)Record Settings
    private void TempRecordSettings()
    {
        if (ImGui.Button(Service.Lang.GetText("RecordSettings") + "[DEV]"))
        {
            ImGui.OpenPopup("RecordSettings");
        }

        if (ImGui.BeginPopup("RecordSettings"))
        {
            // 副本 Content/Duty
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("Content"));
            ImGui.Separator();

            // 是否记录副本内数据 If Track in Duty
            if (ImGui.Checkbox(Service.Lang.GetText("TrackInDuty"), ref isTrackinDuty))
            {
                C.TrackedInDuty = isTrackinDuty;
                C.Save();
                if (isTrackinDuty)
                {
                    Service.Tracker.UninitDutyRewards();
                    Service.Tracker.InitDutyRewards();
                }
                else
                {
                    Service.Tracker.UninitDutyRewards();
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Service.Lang.GetText("TrackInDutyHelp"));

            // 是否记录副本名称 If Record Content Name
            if (isTrackinDuty)
            {
                ImGui.BulletText("");
                ImGui.SameLine();
                if (ImGui.Checkbox(Service.Lang.GetText("RecordContentName"), ref isRecordContentName))
                {
                    C.RecordContentName = isRecordContentName;
                    C.Save();
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker(Service.Lang.GetText("RecordContentNameHelp"));
            }

            // 一般 General
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("General"));
            ImGui.Separator();

            // 是否等待交换完成 If Wait For Exchange to Complete
            if (ImGui.Checkbox(Service.Lang.GetText("WaitExchange"), ref isWaitExComplete))
            {
                C.WaitExComplete = isWaitExComplete;
                C.Save();

                if (isWaitExComplete)
                {
                    Service.Tracker.UninitExchangeCompletes();
                    Service.Tracker.InitExchangeCompletes();
                }
                else
                {
                    Service.Tracker.UninitExchangeCompletes();
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Service.Lang.GetText("WaitExchangeHelp"));

            // 是否记录传送费 If Record TP Costs
            if (ImGui.Checkbox(Service.Lang.GetText("RecordTPCosts"), ref isRecordTeleport))
            {
                C.RecordTeleport = isRecordTeleport;
                C.Save();

                if (isRecordTeleport)
                {
                    Service.Tracker.UninitTeleportCosts();
                    Service.Tracker.InitTeleportCosts();
                }
                else
                {
                    Service.Tracker.UninitTeleportCosts();
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Service.Lang.GetText("RecordTPCostsHelp"));

            // 是否记录传送地点 If Record TP Destination
            if (isRecordTeleport)
            {
                ImGui.BulletText("");
                ImGui.SameLine();
                if (ImGui.Checkbox(Service.Lang.GetText("RecordTPDest"), ref isRecordTeleportDes))
                {
                    C.RecordTeleportDes = isRecordTeleportDes;
                    C.Save();
                }
            }

            // 是否记录任务名 If Record Quest Name
            if (ImGui.Checkbox(Service.Lang.GetText("RecordQuestName"), ref isRecordQuestName))
            {
                C.RecordQuestName = isRecordQuestName;
                C.Save();

                if (isRecordQuestName)
                {
                    Service.Tracker.UninitQuests();
                    Service.Tracker.InitQuests();
                }
                else
                {
                    Service.Tracker.UninitQuests();
                }
            }

            // 是否记录交易对象 If Record Trade Target
            if (ImGui.Checkbox(Service.Lang.GetText("RecordTrade"), ref isRecordTrade))
            {
                C.RecordTrade = isRecordTrade;
                C.Save();
                if (isRecordTrade)
                {
                    Service.Tracker.UninitTrade();
                    Service.Tracker.InitTrade();
                }
                else
                {
                    Service.Tracker.UninitTrade();
                }
            }

            // 是否记录 FATE 名称 If Record FATE name
            if (ImGui.Checkbox(Service.Lang.GetText("RecordFateName"), ref isRecordFate))
            {
                C.RecordFate = isRecordFate;
                C.Save();
                if (isRecordFate)
                {
                    Service.Tracker.UninitFateRewards();
                    Service.Tracker.InitFateRewards();
                }
                else
                {
                    Service.Tracker.UninitFateRewards();
                }
            }

            // 金碟 Gold Saucer
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("GoldSaucer"));
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Service.Lang.GetText("RecordGoldSaucerHelp"));
            ImGui.Separator();

            if (ImGui.Checkbox(Service.Lang.GetText("RecordMGPSource"), ref isRecordMGPSource))
            {
                C.RecordMGPSource = isRecordMGPSource;
                C.Save();
                if (isRecordMGPSource)
                {
                    Service.Tracker.UninitGoldSacuer();
                    Service.Tracker.InitGoldSacuer();
                }
                else
                {
                    Service.Tracker.UninitGoldSacuer();
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Service.Lang.GetText("RecordMGPSourceHelp"));

            // 记录九宫幻卡 Record Triple Triad Result
            if (ImGui.Checkbox(Service.Lang.GetText("RecordTripleTriad"), ref isRecordTripleTriad))
            {
                C.RecordTripleTriad = isRecordTripleTriad;
                C.Save();

                if (isRecordTripleTriad)
                {
                    Service.Tracker.UninitTripleTriad();
                    Service.Tracker.InitTripleTriad();
                }
                else
                {
                    Service.Tracker.UninitTripleTriad();
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Service.Lang.GetText("RecordTripleTriadHelp"));

            // 无人岛 Island Sancutary
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("IslandSanctuary"));
            ImGui.Separator();

            if (ImGui.Checkbox(Service.Lang.GetText("RecordIsland"), ref isRecordIsland))
            {
                C.RecordIsland = isRecordIsland;
                C.Save();

                if (isRecordIsland && Service.ClientState.TerritoryType == 1055)
                {
                    Service.Tracker.UninitIslandRewards();
                    Service.Tracker.InitIslandRewards();
                }
                else
                {
                    Service.Tracker.UninitIslandRewards();
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Service.Lang.GetText("RecordIslandHelp"));

            ImGui.EndPopup();
        }
    }

    // 帮助页面 Help Pages
    private void HelpPages()
    {
        if (Widgets.IconButton(FontAwesomeIcon.Question, $"{Service.Lang.GetText("NeedHelp")}?", "NeedHelp"))
        {
            ImGui.OpenPopup("NeedHelp");
        }

        if (ImGui.BeginPopup("NeedHelp"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Guide")}:");
            ImGui.Separator();
            if (ImGui.Button($"{Service.Lang.GetText("OperationGuide")} (GitHub)"))
            {
                Widgets.OpenUrl("https://github.com/AtmoOmen/CurrencyTracker/wiki/Operations");
            }

            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("SuggestOrReport")}?");
            ImGui.Separator();
            ImGui.Text("GitHub - AtmoOmen, Discord - AtmoOmen#0");
            if (ImGui.Button("GitHub Issue"))
            {
                Widgets.OpenUrl("https://github.com/AtmoOmen/CurrencyTracker/issues");
            }
            ImGui.SameLine();
            if (ImGui.Button("Discord Thread"))
            {
                Widgets.OpenUrl("https://discord.com/channels/581875019861328007/1019646133305344090/threads/1163039624957010021");
            }
            if (C.SelectedLanguage == "ChineseSimplified")
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, "请加入下面的 QQ 频道，在 XIVLauncher/Dalamud 分栏下\n" +
                    "选择 插件问答帮助 频道，然后 @AtmoOmen 向我提问\n" +
                    "(如果你是国服用户, 请注意, 你的问题/建议可能已在更新的版本中已被修复/采纳)");
                if (ImGui.Button("QQ频道【艾欧泽亚泛獭保护协会】"))
                {
                    Widgets.OpenUrl("https://pd.qq.com/s/fttirpnql");
                }
            }

            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("HelpTranslate")}!");
            ImGui.Separator();
            if (ImGui.Button($"Crowdin"))
            {
                Widgets.OpenUrl("https://crowdin.com/project/dalamud-currencytracker");
            }
            ImGui.SameLine();
            ImGui.Text($"{Service.Lang.GetText("HelpTranslateHelp")}!");

            ImGui.EndPopup();
        }
    }

    // 倒序排序 Reverse Sort
    private void ReverseSort()
    {
        ImGui.SetCursorPosX(Widgets.SetColumnCenterAligned("     ", 0, 8));
        if (isReversed)
        {
            if (ImGui.ArrowButton("UpSort", ImGuiDir.Up))
            {
                isReversed = !isReversed;
                C.ReverseSort = isReversed;
                C.Save();
                searchTimer.Stop();
                searchTimer.Start();
            }
        }
        else
        {
            if (ImGui.ArrowButton("DownSort", ImGuiDir.Down))
            {
                isReversed = !isReversed;
                C.ReverseSort = isReversed;
                C.Save();
                searchTimer.Stop();
                searchTimer.Start();
            }
        }
    }

    // 与时间相关的功能 Functions related to Time
    private void TimeFunctions()
    {
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("TimeFunctions");
        }

        if (ImGui.BeginPopup("TimeFunctions", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
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
                    if (clusterHour <= 0)
                    {
                        clusterHour = 0;
                    }
                    searchTimer.Stop();
                    searchTimer.Start();
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n" +
                    $"{Service.Lang.GetText("ClusterByTimeHelp1", clusterHour)}");
            }

            if (ImGui.Checkbox($"{Service.Lang.GetText("FilterByTime")}##TimeFilter", ref isTimeFilterEnabled))
            {
                searchTimer.Stop();
                searchTimer.Start();
            }

            var StartDateString = filterStartDate.ToString("yyyy-MM-dd");
            ImGui.SetNextItemWidth(120);
            ImGui.InputText(Service.Lang.GetText("StartDate"), ref StartDateString, 100, ImGuiInputTextFlags.ReadOnly);
            if (ImGui.IsItemClicked())
            {
                startDateEnable = !startDateEnable;
                if (endDateEnable) endDateEnable = false;
            }

            var EndDateString = filterEndDate.ToString("yyyy-MM-dd");
            ImGui.SetNextItemWidth(120);
            ImGui.InputText(Service.Lang.GetText("EndDate"), ref EndDateString, 100, ImGuiInputTextFlags.ReadOnly);
            if (ImGui.IsItemClicked())
            {
                endDateEnable = !endDateEnable;
                if (startDateEnable) startDateEnable = false;
            }

            if (startDateEnable)
            {
                CreateDatePicker(ref filterStartDate, true);
            }

            if (endDateEnable)
            {
                CreateDatePicker(ref filterEndDate, false);
            }

            ImGui.EndPopup();
        }
    }

    // 与地点相关的功能 Functions related to Location
    private void LocationFunctions()
    {
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("LocationSearch");
        }
        if (ImGui.BeginPopup("LocationSearch"))
        {
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputTextWithHint("##LocationSearch", Service.Lang.GetText("PleaseSearch"), ref searchLocationName, 80))
            {
                if (!searchLocationName.IsNullOrEmpty())
                {
                    isLocationFilterEnabled = true;
                    searchTimer.Stop();
                    searchTimer.Start();
                }
                else
                {
                    isLocationFilterEnabled = false;
                    searchTimer.Stop();
                    UpdateTransactions();
                }
            }

            ImGui.EndPopup();
        }
    }

    // 与备注相关的功能 Functions related to Note
    private void NoteFunctions()
    {
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("NoteSearch");
        }
        if (ImGui.BeginPopup("NoteSearch"))
        {
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputTextWithHint("##NoteSearch", Service.Lang.GetText("PleaseSearch"), ref searchNoteContent, 80))
            {
                if (!searchNoteContent.IsNullOrEmpty())
                {
                    isNoteFilterEnabled = true;
                    searchTimer.Stop();
                    searchTimer.Start();
                }
                else
                {
                    isNoteFilterEnabled = false;
                    searchTimer.Stop();
                    UpdateTransactions();
                }
            }

            ImGui.EndPopup();
        }
    }

    // 与收支相关的功能 Functions related to Change
    private void ChangeFunctions()
    {
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("ChangeFunctions");
        }

        if (ImGui.BeginPopup("ChangeFunctions"))
        {
            if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeFilterEnabled")}##ChangeFilter", ref isChangeFilterEnabled))
            {
                searchTimer.Stop();
                searchTimer.Start();
            }

            if (isChangeFilterEnabled)
            {
                if (ImGui.RadioButton($"{Service.Lang.GetText("Greater")}##FilterMode", ref filterMode, 0))
                {
                    searchTimer.Stop();
                    searchTimer.Start();
                }
                ImGui.SameLine();
                if (ImGui.RadioButton($"{Service.Lang.GetText("Less")}##FilterMode", ref filterMode, 1))
                {
                    searchTimer.Stop();
                    searchTimer.Start();
                }

                ImGui.SetNextItemWidth(130);
                if (ImGui.InputInt($"##FilterValue", ref filterValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    searchTimer.Stop();
                    searchTimer.Start();
                }
                ImGuiComponents.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ChangeFilterLabel", Service.Lang.GetText(filterMode == 0 ? "Greater" : filterMode == 1 ? "Less" : ""), filterValue)}");
            }

            if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeTextColoring")}##ChangeColoring", ref isChangeColoring))
            {
                C.ChangeTextColoring = isChangeColoring;
                C.Save();
            }

            if (isChangeColoring)
            {
                if (ImGui.ColorButton("##PositiveColor", positiveChangeColor))
                {
                    ImGui.OpenPopup("PositiveColor");
                }
                ImGui.SameLine();
                ImGui.Text(Service.Lang.GetText("PositiveChange"));

                if (ImGui.BeginPopup("PositiveColor"))
                {
                    if (ImGui.ColorPicker4("", ref positiveChangeColor))
                    {
                        isChangeColoring = true;
                        C.ChangeTextColoring = isChangeColoring;
                        C.PositiveChangeColor = positiveChangeColor;
                        C.Save();
                    }
                    ImGui.EndPopup();
                }

                ImGui.SameLine();
                if (ImGui.ColorButton("##NegativeColor", negativeChangeColor))
                {
                    ImGui.OpenPopup("NegativeColor");
                }
                ImGui.SameLine();
                ImGui.Text(Service.Lang.GetText("NegativeChange"));

                if (ImGui.BeginPopup("NegativeColor"))
                {
                    if (ImGui.ColorPicker4("", ref negativeChangeColor))
                    {
                        isChangeColoring = true;
                        C.ChangeTextColoring = isChangeColoring;
                        C.NegativeChangeColor = negativeChangeColor;
                        C.Save();
                    }
                    ImGui.EndPopup();
                }
            }

            ImGui.EndPopup();
        }
    }

    // 修改货币本地名称 Change Currency Name
    private void RenameCurrency()
    {
        if (selectedCurrencyName.IsNullOrEmpty())
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            Widgets.IconButton(FontAwesomeIcon.Pen, "None", "RenameCurrency");
            ImGui.PopStyleVar();
        }
        else
        {
            if (Widgets.IconButton(FontAwesomeIcon.Pen, Service.Lang.GetText("Rename"), "RenameCurrency"))
            {
                ImGui.OpenPopup("CurrencyRename");
                editedCurrencyName = selectedCurrencyName;
            }
        }

        if (ImGui.BeginPopup("CurrencyRename", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Now")}:");
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
            ImGui.Image(CurrencyInfo.GetIcon(C.PresetCurrencies.Concat(C.CustomCurrencies).FirstOrDefault(x => x.Key == selectedCurrencyName).Value).ImGuiHandle, ImGuiHelpers.ScaledVector2(16.0f));
            ImGui.SameLine();
            ImGui.Text(selectedCurrencyName);

            ImGui.SetNextItemWidth(ImGui.CalcTextSize(selectedCurrencyName).X + 85);
            ImGui.InputText($"##CurrencyRename", ref editedCurrencyName, 150, ImGuiInputTextFlags.AutoSelectAll);

            if (ImGui.Button(Service.Lang.GetText("Confirm")))
            {
                if (editedCurrencyName.IsNullOrEmpty())
                {
                    Service.Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp"));
                    return;
                }

                CurrencyRenameHandler(editedCurrencyName);

                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button(Service.Lang.GetText("Reset")))
            {
                var currencyName = C.PresetCurrencies.ContainsKey(selectedCurrencyName) ? CurrencyInfo.CurrencyLocalName(C.PresetCurrencies[selectedCurrencyName]) : CurrencyInfo.CurrencyLocalName(C.CustomCurrencies[selectedCurrencyName]);
                CurrencyRenameHandler(currencyName);

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    // 自定义货币追踪 Custom Currencies To Track
    private void CustomCurrencyTracker()
    {
        if (Widgets.IconButton(FontAwesomeIcon.Plus, Service.Lang.GetText("Add"), "CustomCurrencyAdd"))
        {
            ImGui.OpenPopup("CustomCurrency");
        }

        if (ImGui.BeginPopup("CustomCurrency", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("CustomCurrencyTracker"));
            ImGuiComponents.HelpMarker(Service.Lang.GetText("CustomCurrencyHelp"));
            ImGui.Text($"{Service.Lang.GetText("Now")}:");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(210);

            if (ImGui.BeginCombo("", Tracker.ItemNames.TryGetValue(customCurrency, out var selected) ? selected : Service.Lang.GetText("PleaseSelect"), ImGuiComboFlags.HeightLarge))
            {
                var startIndex = currentItemPage * itemsPerPage;
                var endIndex = Math.Min(startIndex + itemsPerPage, CCTItemNames.Count);

                ImGui.SetNextItemWidth(200f);
                if (ImGui.InputTextWithHint("##selectflts", Service.Lang.GetText("PleaseSearch"), ref searchFilter, 50))
                {
                    currentItemPage = 0;
                    CCTItemNames = ApplyCCTFilter(searchFilter);
                }
                ImGui.SameLine();
                if (Widgets.IconButton(FontAwesomeIcon.Backward, "None", "CCTFirstPage"))
                    currentItemPage = 0;
                ImGui.SameLine();
                if (ImGui.ArrowButton("CustomPreviousPage", ImGuiDir.Left) && currentItemPage > 0)
                    currentItemPage--;
                ImGui.SameLine();
                if (CCTItemNames.Count > 0)
                {
                    if (ImGui.ArrowButton("CustomNextPage", ImGuiDir.Right) && currentItemPage < (CCTItemNames.Count / itemsPerPage) - 1)
                    {
                        currentItemPage++;
                    }
                }
                else
                {
                    ImGui.ArrowButton("CustomNextPage", ImGuiDir.Right);
                }
                ImGui.SameLine();
                if(CCTItemNames.Count > 0)
                {
                    if (Widgets.IconButton(FontAwesomeIcon.Forward, "None", "CCTLastPage"))
                    {
                        currentItemPage = (CCTItemNames.Count / itemsPerPage) - 1;
                    }
                }
                else
                {
                    Widgets.IconButton(FontAwesomeIcon.Forward, "None", "CCTLastPage");
                }

                if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel > 0 && currentItemPage > 0)
                    currentItemPage--;

                if (CCTItemNames.Count > 0)
                {
                    if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel < 0 && currentItemPage < (CCTItemNames.Count / itemsPerPage) - 1)
                    {
                        currentItemPage++;
                        Service.PluginLog.Debug($"当前页{currentItemPage}");
                    }
                }

                ImGui.Separator();

                var visibleItems = 0;

                var filterNamesForCCTSet = new HashSet<string>(filterNamesForCCT);

                if (!searchFilter.IsNullOrEmpty() && CCTItemNames.Count > 0)
                {
                    foreach (var itemName in CCTItemNames)
                    {
                        var isCurrencyMatch = options.All(y => CurrencyInfo.CurrencyLocalName(C.AllCurrencies[y]) != itemName);
                        var isFilteredNameMatch = !filterNamesForCCTSet.Any(filter => itemName.Contains(filter));

                        if (isCurrencyMatch && isFilteredNameMatch)
                        {
                            if (visibleItems >= startIndex && visibleItems < endIndex)
                            {
                                var itemKeyPair = Tracker.ItemNames.FirstOrDefault(x => x.Value == itemName);
                                if (ImGui.Selectable(itemName))
                                {
                                    customCurrency = itemKeyPair.Key;
                                }

                                if (ImGui.IsWindowAppearing() && customCurrency == itemKeyPair.Key)
                                {
                                    ImGui.SetScrollHereY();
                                }
                            }
                            visibleItems++;
                        }

                        if (visibleItems >= endIndex)
                        {
                            break;
                        }
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();

            if (Widgets.IconButton(FontAwesomeIcon.Plus, "None", "AddCustomCurrency"))
            {
                if (string.IsNullOrEmpty(selected))
                {
                    Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                    return;
                }

                if (options.Contains(selected))
                {
                    Service.Chat.PrintError(Service.Lang.GetText("CustomCurrencyHelp1"));
                    return;
                }

                if (!C.CustomCurrencies.ContainsKey(selected))
                {
                    C.CustomCurrencies.Add(selected, customCurrency);
                }

                C.Save();
                options.Add(selected);
                selectedStates.Add(selected, new List<bool>());
                selectedTransactions.Add(selected, new List<TransactionsConvertor>());
                ReloadOrderedOptions();

                if (recordMode == 1)
                {
                    Service.Tracker.InitializeTracking();
                    Service.Tracker.OnTransactionsUpdate(EventArgs.Empty);
                }

                selectedOptionIndex = ordedOptions.Count - 1;
                selectedCurrencyName = selected;
                currentTypeTransactions = Transactions.LoadAllTransactions(selectedCurrencyName);
                lastTransactions = currentTypeTransactions;
                searchFilter = string.Empty;

                customCurrency = 0;

                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    // 按临界值合并记录 Merge Transactions By Threshold
    private void MergeTransactions()
    {
        if (ImGui.Button(Service.Lang.GetText("MergeTransactionsLabel")))
        {
            ImGui.OpenPopup("MergeTransactions");
        }

        if (ImGui.BeginPopup("MergeTransactions"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("MergeTransactionsLabel4"));
            ImGui.Text(Service.Lang.GetText("Threshold"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("##MergeThreshold", ref mergeThreshold, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue);
            if (mergeThreshold < 0)
            {
                mergeThreshold = 0;
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker($"{Service.Lang.GetText("MergeTransactionsHelp3")}{Service.Lang.GetText("TransactionsHelp2")}");

            // 双向合并 Two-Way Merge
            if (ImGui.Button(Service.Lang.GetText("TwoWayMerge")))
            {
                int mergeCount = MergeTransactions(false);
                if (mergeCount == 0)
                    return;
            }

            ImGui.SameLine();

            // 单向合并 One-Way Merge
            if (ImGui.Button(Service.Lang.GetText("OneWayMerge")))
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
        if (ImGui.Button(Service.Lang.GetText("ClearExTransactionsLabel")))
        {
            ImGui.OpenPopup("ClearExceptionNote");
        }

        if (ImGui.BeginPopup("ClearExceptionNote"))
        {
            if (ImGui.Button(Service.Lang.GetText("Confirm")))
            {
                if (string.IsNullOrEmpty(selectedCurrencyName))
                {
                    Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                    return;
                }

                var removedCount = Transactions.ClearExceptionRecords(selectedCurrencyName);
                if (removedCount > 0)
                {
                    Service.Chat.Print($"{Service.Lang.GetText("ClearExTransactionsHelp2", removedCount)}");
                    UpdateTransactions();
                }
                else
                {
                    Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp"));
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker($"{Service.Lang.GetText("ClearExTransactionsHelp")}{Service.Lang.GetText("ClearExTransactionsHelp1")}\n{Service.Lang.GetText("TransactionsHelp2")}");
            ImGui.EndPopup();
        }
    }

    // 导出数据为.CSV文件 Export Transactions To a .csv File
    private void ExportData()
    {
        if (ImGui.Button(Service.Lang.GetText("Export")))
        {
            ImGui.OpenPopup(str_id: "ExportFileRename");
        }

        if (ImGui.BeginPopup("ExportFileRename"))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ExportFileType")}:");
            ImGui.SameLine();

            if (ImGui.RadioButton(".csv", ref exportDataFileType, 0))
            {
                C.ExportDataFileType = exportDataFileType;
                C.Save();
            }
            ImGui.SameLine();
            if (ImGui.RadioButton(".md", ref exportDataFileType, 1))
            {
                C.ExportDataFileType = exportDataFileType;
                C.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Service.Lang.GetText("ExportFileHelp"));

            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("FileRenameLabel"));
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(Service.Lang.GetText("ExportFileHelp1"));
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText($"_{selectedCurrencyName}_{Service.Lang.GetText("FileRenameLabel2")}{(exportDataFileType == 0 ? ".csv" : ".md")}", ref fileName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (selectedCurrencyName == null)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                    return;
                }
                if (currentTypeTransactions == null || currentTypeTransactions.Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("ExportCsvMessage1"));
                    return;
                }
                var filePath = Transactions.ExportData(currentTypeTransactions, fileName, selectedCurrencyName, exportDataFileType);
                Service.Chat.Print($"{Service.Lang.GetText("ExportCsvMessage3")}{filePath}");
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"{Service.Lang.GetText("FileRenameHelp1")} {selectedCurrencyName}_{Service.Lang.GetText("FileRenameLabel2")}.csv");
            }
            ImGui.EndPopup();
        }
    }

    // 打开数据文件夹 Open Folder Containing Data Files
    private void OpenDataFolder()
    {
        if (ImGui.Button(Service.Lang.GetText("OpenDataFolder")))
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start \"\" \"{P.PlayerDataFolder}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = P.PlayerDataFolder
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = P.PlayerDataFolder
                    });
                }
                else
                {
                    Service.PluginLog.Error("Unsupported OS");
                }
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"Error :{ex.Message}");
            }
        }
    }

    // 打开插件 GitHub 页面 Open Plugin GitHub Page
    private static void OpenGitHubPage()
    {
        if (ImGui.Button("GitHub"))
        {
            Widgets.OpenUrl("https://github.com/AtmoOmen/CurrencyTracker");
        }
    }

    // 界面语言切换功能 Language Switch
    private void LanguageSwitch()
    {
        var lang = string.Empty;

        if (Widgets.IconButton(FontAwesomeIcon.Globe, "Languages"))
        {
            ImGui.OpenPopup(str_id: "LanguagesList");
        }

        if (ImGui.BeginPopup("LanguagesList"))
        {
            foreach (var languageInfo in LanguageManager.LanguageNames)
            {
                if (ImGui.Button(languageInfo.DisplayName) && languageInfo.Language != playerLang)
                {
                    Service.Lang = new LanguageManager(languageInfo.Language);
                    playerLang = languageInfo.Language;
                    C.SelectedLanguage = playerLang;
                    C.Save();

                    Service.CommandManager.RemoveHandler(Plugin.CommandName);
                    Service.CommandManager.AddHandler(Plugin.CommandName, new CommandInfo(P.OnCommand)
                    {
                        HelpMessage = Service.Lang.GetText("CommandHelp") + "\n" + Service.Lang.GetText("CommandHelp1")
                    });
                }

                Widgets.TextTooltip($"By: {languageInfo.Translators}");
            }

            ImGui.EndPopup();
        }
    }

    // 货币列表顶端工具栏 Listbox tools
    private void ListboxTools()
    {
        ImGui.SetCursorPosX(7);
        CustomCurrencyTracker();

        ImGui.SameLine();

        if (ImGui.ArrowButton("UpArrow", ImGuiDir.Up) && selectedOptionIndex > 0)
        {
            SwapOptions(selectedOptionIndex, selectedOptionIndex - 1);
            selectedOptionIndex--;
        }

        ImGui.SameLine();
        {
            if (string.IsNullOrWhiteSpace(selectedCurrencyName) || selectedOptionIndex == -1)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                Widgets.IconButton(FontAwesomeIcon.EyeSlash);
                ImGui.PopStyleVar();
            }
            else if (!C.PresetCurrencies.Keys.Any(x => x == selectedCurrencyName))
            {
                Widgets.IconButton(FontAwesomeIcon.Trash, Service.Lang.GetText("DeleteCurrency"), "ToolsDelete", new Vector2(39, 30));
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
                {
                    if (string.IsNullOrEmpty(selectedCurrencyName))
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                        return;
                    }
                    if (!options.Contains(selectedCurrencyName))
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("CustomCurrencyHelp2"));
                        return;
                    }
                    C.CustomCurrencies.Remove(selectedCurrencyName);
                    C.Save();
                    options.Remove(selectedCurrencyName);
                    selectedStates.Remove(selectedCurrencyName);
                    selectedTransactions.Remove(selectedCurrencyName);
                    ReloadOrderedOptions();
                    selectedCurrencyName = string.Empty;
                }
            }
            else
            {
                Widgets.IconButton(FontAwesomeIcon.EyeSlash, Service.Lang.GetText("HideCurrency"));
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
                {
                    if (string.IsNullOrWhiteSpace(selectedCurrencyName) || selectedOptionIndex == -1 || !CurrencyInfo.PresetCurrencies.Keys.Any(key => CurrencyInfo.CurrencyLocalName(CurrencyInfo.PresetCurrencies[key]) == selectedCurrencyName)) return;

                    options.Remove(selectedCurrencyName);
                    selectedStates.Remove(selectedCurrencyName);
                    selectedTransactions.Remove(selectedCurrencyName);
                    hiddenOptions.Add(selectedCurrencyName);
                    if (!C.HiddenOptions.Contains(selectedCurrencyName))
                        C.HiddenOptions.Add(selectedCurrencyName);
                    C.Save();
                    ReloadOrderedOptions();
                    selectedCurrencyName = string.Empty;
                    selectedOptionIndex = -1;
                }
            }
        }

        ImGui.SameLine();

        if (ImGui.ArrowButton("DownArrow", ImGuiDir.Down) && selectedOptionIndex < ordedOptions.Count - 1 && selectedOptionIndex > -1)
        {
            SwapOptions(selectedOptionIndex, selectedOptionIndex + 1);
            selectedOptionIndex++;
        }

        ImGui.SameLine();

        RenameCurrency();

        ImGui.SameLine();

        if (hiddenOptions.Count == 0)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            Widgets.IconButton(FontAwesomeIcon.TrashRestore);
            ImGui.PopStyleVar();
        }
        else
        {
            Widgets.IconButton(FontAwesomeIcon.TrashRestore, Service.Lang.GetText("RestoreHidden"));
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
            {
                if (hiddenOptions.Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("OrderChangeHelp"));
                    return;
                }
                var addedOptions = new HashSet<string>();

                foreach (var option in hiddenOptions)
                {
                    if (!addedOptions.Contains(option))
                    {
                        options.Add(option);
                        selectedStates.Add(option, new List<bool>());
                        selectedTransactions.Add(option, new List<TransactionsConvertor>());
                        addedOptions.Add(option);
                    }
                }
                hiddenOptions.Clear();
                C.HiddenOptions.Clear();
                C.Save();
                Service.Chat.Print($"{Service.Lang.GetText("OrderChangeHelp1")} {addedOptions.Count} {Service.Lang.GetText("OrderChangeHelp2")}");
                ReloadOrderedOptions();
            }
        }
    }

    // 图表工具栏 Table Tools
    private void TableTools()
    {
        ImGui.Text($"{Service.Lang.GetText("Now")}: {selectedTransactions[selectedCurrencyName].Count} {Service.Lang.GetText("Transactions")}");
        ImGui.Separator();

        // 取消选择 Unselect
        if (ImGui.Selectable(Service.Lang.GetText("Unselect")))
        {
            if (selectedTransactions[selectedCurrencyName].Count == 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }
            selectedStates[selectedCurrencyName].Clear();
            selectedTransactions[selectedCurrencyName].Clear();
        }

        // 全选 Select All
        if (ImGui.Selectable(Service.Lang.GetText("SelectAll")))
        {
            selectedTransactions[selectedCurrencyName].Clear();

            foreach (var transaction in currentTypeTransactions)
            {
                selectedTransactions[selectedCurrencyName].Add(transaction);
            }

            for (var i = 0; i < selectedStates[selectedCurrencyName].Count; i++)
            {
                selectedStates[selectedCurrencyName][i] = true;
            }
        }

        // 反选 Inverse Select
        if (ImGui.Selectable(Service.Lang.GetText("InverseSelect")))
        {
            for (var i = 0; i < selectedStates[selectedCurrencyName].Count; i++)
            {
                selectedStates[selectedCurrencyName][i] = !selectedStates[selectedCurrencyName][i];
            }

            foreach (var transaction in currentTypeTransactions)
            {
                var exists = selectedTransactions[selectedCurrencyName].Any(selectedTransaction => Widgets.IsTransactionEqual(selectedTransaction, transaction));

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

        // 复制 Copy
        if (ImGui.Selectable(Service.Lang.GetText("Copy")))
        {
            if (selectedTransactions[selectedCurrencyName].Count == 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var columnData = string.Empty;
            var header = string.Empty;

            if (exportDataFileType == 0)
            {
                header = Service.Lang.GetText("ExportFileCSVHeader");
            }
            else if (exportDataFileType == 1)
            {
                header = Service.Lang.GetText("ExportFileMDHeader1");
            }

            var count = selectedTransactions[selectedCurrencyName].Count;

            columnData += header;

            for (var t = 0; t < count; t++)
            {
                var record = selectedTransactions[selectedCurrencyName][t];
                var change = $"{record.Change:+ #,##0;- #,##0;0}";

                if (exportDataFileType == 0)
                {
                    columnData += $"\n{record.TimeStamp},{record.Amount},{change},{record.LocationName},{record.Note}";
                }
                else if (exportDataFileType == 1)
                {
                    columnData += $"\n{record.TimeStamp} | {record.Amount} | {change} | {record.LocationName} | {record.Note}";
                }
            }

            if (!string.IsNullOrEmpty(columnData))
            {
                ImGui.SetClipboardText(columnData);
                Service.Chat.Print($"{Service.Lang.GetText("CopyTransactionsHelp", selectedTransactions[selectedCurrencyName].Count)}");
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
            if (selectedTransactions[selectedCurrencyName].Count == 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }
            foreach (var selectedTransaction in selectedTransactions[selectedCurrencyName])
            {
                var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
                var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
                string filePath = Path.Combine(P.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}", $"{selectedCurrencyName}.txt");
                var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyName);
                var foundTransaction = editedTransactions.FirstOrDefault(t => Widgets.IsTransactionEqual(t, selectedTransaction));

                if (foundTransaction != null)
                {
                    editedTransactions.Remove(foundTransaction);
                }

                TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
            }
            UpdateTransactions();
        }

        // 导出 Export
        if (ImGui.Selectable(Service.Lang.GetText("Export")))
        {
            if (selectedTransactions[selectedCurrencyName].Count == 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }
            var filePath = Transactions.ExportData(selectedTransactions[selectedCurrencyName], "", selectedCurrencyName, exportDataFileType);
            Service.Chat.Print($"{Service.Lang.GetText("ExportCsvMessage3")}{filePath}");
        }

        // 合并 Merge
        if (ImGui.Selectable(Service.Lang.GetText("Merge"), ref isOnMergingTT, ImGuiSelectableFlags.DontClosePopups))
        {
            if (isOnMergingTT)
            {
                if (selectedTransactions[selectedCurrencyName].Count != 0)
                {
                    editedLocationName = selectedTransactions[selectedCurrencyName].FirstOrDefault().LocationName;
                    editedNoteContent = selectedTransactions[selectedCurrencyName].FirstOrDefault().Note;
                }
                else
                {
                    editedLocationName = string.Empty;
                    editedNoteContent = string.Empty;
                }
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
                if (selectedTransactions[selectedCurrencyName].Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }

                if (selectedTransactions[selectedCurrencyName].Count == 1)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("MergeTransactionsHelp4"));
                    return;
                }

                if (editedLocationName.IsNullOrWhitespace())
                {
                    Service.Chat.PrintError(Service.Lang.GetText("EditHelp1"));
                    return;
                }

                var mergeCount = Transactions.MergeSpecificTransactions(selectedCurrencyName, editedLocationName, selectedTransactions[selectedCurrencyName], editedNoteContent.IsNullOrEmpty() ? "-1" : editedNoteContent);
                Service.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");

                UpdateTransactions();
                isOnMergingTT = false;
            }
        }

        // 编辑 Edit
        if (ImGui.Selectable(Service.Lang.GetText("Edit"), ref isOnEdit, ImGuiSelectableFlags.DontClosePopups))
        {
            if (isOnEdit)
            {
                if (selectedTransactions[selectedCurrencyName].Count != 0)
                {
                    editedLocationName = selectedTransactions[selectedCurrencyName].FirstOrDefault().LocationName;
                    editedNoteContent = selectedTransactions[selectedCurrencyName].FirstOrDefault().Note;
                }
                else
                {
                    editedLocationName = string.Empty;
                    editedNoteContent = string.Empty;
                }
            }
        }

        if (isOnEdit)
        {
            if (isOnMergingTT) isOnMergingTT = !isOnMergingTT;

            ImGui.Separator();

            ImGui.Text($"{Service.Lang.GetText("Location")}:");
            ImGui.SetNextItemWidth(210);
            if (ImGui.InputTextWithHint("##EditLocationName", Service.Lang.GetText("EditHelp"), ref editedLocationName, 80, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (selectedTransactions[selectedCurrencyName].Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }

                if (editedLocationName.IsNullOrWhitespace())
                {
                    Service.Chat.PrintError(Service.Lang.GetText("EditHelp1"));
                    return;
                }

                var filePath = Path.Combine(P.PlayerDataFolder, $"{selectedCurrencyName}.txt");
                var failCounts = 0;

                foreach (var selectedTransaction in selectedTransactions[selectedCurrencyName])
                {
                    var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyName);

                    var index = -1;
                    for (var i = 0; i < editedTransactions.Count; i++)
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
                        TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                    }
                    else
                    {
                        failCounts++;
                    }
                }

                if (failCounts == 0)
                {
                    Service.Chat.Print($"{Service.Lang.GetText("EditLocationHelp", selectedTransactions[selectedCurrencyName].Count)}");

                    UpdateTransactions();
                }
                else if (failCounts > 0 && failCounts < selectedTransactions[selectedCurrencyName].Count)
                {
                    Service.Chat.Print($"{Service.Lang.GetText("EditLocationHelp", selectedTransactions[selectedCurrencyName].Count - failCounts)}");
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
                if (selectedTransactions[selectedCurrencyName].Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                    return;
                }

                var filePath = Path.Combine(P.PlayerDataFolder, $"{selectedCurrencyName}.txt");
                var failCounts = 0;

                foreach (var selectedTransaction in selectedTransactions[selectedCurrencyName])
                {
                    var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyName);

                    var index = -1;
                    for (var i = 0; i < editedTransactions.Count; i++)
                    {
                        if (Widgets.IsTransactionEqual(editedTransactions[i], selectedTransaction))
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
                    Service.Chat.Print($"{Service.Lang.GetText("EditHelp2")} {selectedTransactions[selectedCurrencyName].Count} {Service.Lang.GetText("EditHelp4")} {editedNoteContent}");

                    UpdateTransactions();
                }
                else if (failCounts > 0 && failCounts < selectedTransactions[selectedCurrencyName].Count)
                {
                    Service.Chat.Print($"{Service.Lang.GetText("EditHelp2")} {selectedTransactions[selectedCurrencyName].Count - failCounts} {Service.Lang.GetText("EditHelp3")} {editedLocationName}");
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

    // 顶端翻页工具栏 Transactions Paging Tools
    private void TransactionsPagingTools()
    {
        var pageCount = (currentTypeTransactions.Count > 0) ? (int)Math.Ceiling((double)currentTypeTransactions.Count / transactionsPerPage) : 0;
        currentPage = (pageCount > 0) ? Math.Clamp(currentPage, 0, pageCount - 1) : 0;

        if (pageCount == 0)
        {
            if (P.Graph.IsOpen) P.Graph.IsOpen = false;
        }

        // 图表 Graphs
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 360) / 2 - 57 - ImGui.CalcTextSize("    ").X);
        if (Widgets.IconButton(FontAwesomeIcon.ChartBar, Service.Lang.GetText("Graphs")) && pageCount > 0)
        {
            if (selectedCurrencyName != null && currentTypeTransactions.Count != 1 && currentTypeTransactions != null)
            {
                LinePlotData = currentTypeTransactions.Select(x => x.Amount).ToArray();
                P.Graph.IsOpen = !P.Graph.IsOpen;
            }
            else return;
        }

        ImGui.SameLine();

        // 首页 First Page
        var pageButtonPosX = (ImGui.GetWindowWidth() - 360) / 2 - 40;
        ImGui.SetCursorPosX(pageButtonPosX);
        if (Widgets.IconButton(FontAwesomeIcon.Backward))
            currentPage = 0;

        ImGui.SameLine();

        // 上一页 Last Page
        if (ImGui.ArrowButton("PreviousPage", ImGuiDir.Left) && currentPage > 0)
            currentPage--;

        ImGui.SameLine();

        // 页数显示 Pages
        ImGui.Text($"{Service.Lang.GetText("PageComponent", currentPage + 1, pageCount)}");

        // 每页显示记录条数 Transactions Per Page
        if (ImGui.IsItemClicked())
        {
            ImGui.OpenPopup("TransactionsPerPage");
        }

        if (ImGui.BeginPopup("TransactionsPerPage"))
        {
            ImGui.Text(Service.Lang.GetText("TransactionsPerPage"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);

            if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
            {
                transactionsPerPage = Math.Max(transactionsPerPage, 0);
                C.RecordsPerPage = transactionsPerPage;
                C.Save();
            }

            ImGui.EndPopup();
        }

        ImGui.SameLine();

        // 下一页 Next Page
        if (ImGui.ArrowButton("NextPage", ImGuiDir.Right) && currentPage < pageCount - 1)
            currentPage++;

        ImGui.SameLine();

        // 尾页 Final Page
        if (Widgets.IconButton(FontAwesomeIcon.Forward) && currentPage >= 0)
            currentPage = pageCount;

        ImGui.SameLine();

        // 表格外观
        if (Widgets.IconButton(FontAwesomeIcon.Table, Service.Lang.GetText("TableAppearance"), "TableAppearance"))
            ImGui.OpenPopup("TableAppearence");

        if (ImGui.BeginPopup("TableAppearence"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ColumnsDisplayed")}:");

            if (ImGui.Checkbox($"{Service.Lang.GetText("Order")}##DisplayOrderColumn", ref isShowOrderColumn))
            {
                C.ShowOrderColumn = isShowOrderColumn;
                C.Save();
            }

            ImGui.SameLine();

            if (ImGui.Checkbox($"{Service.Lang.GetText("Location")}##DisplayLocationColumn", ref isShowLocationColumn))
            {
                C.ShowLocationColumn = isShowLocationColumn;
                C.Save();
            }

            ImGui.SameLine();

            if (ImGui.Checkbox($"{Service.Lang.GetText("Note")}##DisplayNoteColumn", ref isShowNoteColumn))
            {
                C.ShowNoteColumn = isShowNoteColumn;
                C.Save();
            }

            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("TransactionsPerPage"));
            ImGui.SetNextItemWidth(150);

            if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
            {
                transactionsPerPage = Math.Max(transactionsPerPage, 0);
                C.RecordsPerPage = transactionsPerPage;
                C.Save();
            }

            ImGui.EndPopup();
        }

        visibleStartIndex = currentPage * transactionsPerPage;
        visibleEndIndex = Math.Min(visibleStartIndex + transactionsPerPage, currentTypeTransactions.Count);

        // 鼠标滚轮控制 Logic controlling Mouse Wheel Filpping
        {
            if (!ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
            {
                if ((ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel > 0) && currentPage > 0)
                    currentPage--;

                if ((ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel < 0) && currentPage < pageCount - 1)
                    currentPage++;
            }
        }
    }

    // 存储可用货币名称选项的列表框 Listbox Containing Available Currencies' Name
    private void CurrenciesList()
    {
        var ChildFrameHeight = ChildframeHeightAdjust();

        var childScale = new Vector2(243, ChildFrameHeight);
        if (ImGui.BeginChildFrame(2, childScale, ImGuiWindowFlags.NoScrollbar))
        {
            ListboxTools();

            ImGui.Separator();

            ImGui.SetNextItemWidth(235);
            for (var i = 0; i < ordedOptions.Count; i++)
            {
                var option = ordedOptions[i];
                var isSelected = i == selectedOptionIndex;

                var headerHoveredColor = ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderHovered];
                var textSelectedColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Header];
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, headerHoveredColor with { W = 0.2f });
                ImGui.PushStyleColor(ImGuiCol.Header, textSelectedColor with { W = 0.2f });
                if (ImGui.Selectable($"##{option}", isSelected))
                {
                    selectedOptionIndex = i;
                    selectedCurrencyName = option;

                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyName));
                    lastTransactions = currentTypeTransactions;
                }
                ImGui.PopStyleColor(2);

                ImGui.SameLine(3.0f);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3.0f);
                ImGui.Image(CurrencyInfo.GetIcon(C.PresetCurrencies.Concat(C.CustomCurrencies).FirstOrDefault(x => x.Key == option).Value).ImGuiHandle, ImGuiHelpers.ScaledVector2(20.0f));
                ImGui.SameLine();
                ImGui.Text(option);
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

        if (isFirstTime)
        {
            Service.Tracker.UpdateCurrencies();
            isFirstTime = false;
        }

        var childFrameHeight = ChildframeHeightAdjust();
        var childScale = new Vector2(ImGui.GetWindowWidth() - 100, childFrameHeight);

        ImGui.SameLine();

        if (ImGui.BeginChildFrame(1, childScale, ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            TransactionsPagingTools();

            var columnCount = 4 + Convert.ToInt32(isShowOrderColumn) + Convert.ToInt32(isShowLocationColumn) + Convert.ToInt32(isShowNoteColumn);

            if (ImGui.BeginTable("Transactions", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable, new Vector2(ImGui.GetWindowWidth() - 175, 1)))
            {
                if (isShowOrderColumn) ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, ImGui.CalcTextSize((currentTypeTransactions.Count + 1).ToString()).X + 10, 0);
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.None, 150, 0);
                ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.None, 130, 0);
                ImGui.TableSetupColumn("Change", ImGuiTableColumnFlags.None, 100, 0);
                if (isShowLocationColumn) ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.None, 150, 0);
                if (isShowNoteColumn) ImGui.TableSetupColumn("Note", ImGuiTableColumnFlags.None, 160, 0);
                ImGui.TableSetupColumn("Selected", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 30, 0);

                ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

                if (isShowOrderColumn)
                {
                    ImGui.TableNextColumn();
                    ReverseSort();
                }

                ImGui.TableNextColumn();

                ImGui.Selectable($" {Service.Lang.GetText("Time")}{CalcNumSpaces()}");
                TimeFunctions();

                ImGui.TableNextColumn();
                ImGui.Text($" {Service.Lang.GetText("Amount")}{CalcNumSpaces()}");

                ImGui.TableNextColumn();
                ImGui.Selectable($" {Service.Lang.GetText("Change")}{CalcNumSpaces()}");
                ChangeFunctions();

                if (isShowLocationColumn)
                {
                    ImGui.TableNextColumn();
                    ImGui.Selectable($" {Service.Lang.GetText("Location")}{CalcNumSpaces()}");
                    LocationFunctions();
                }

                if (isShowNoteColumn)
                {
                    ImGui.TableNextColumn();
                    ImGui.Selectable($" {Service.Lang.GetText("Note")}{CalcNumSpaces()}");
                    NoteFunctions();
                }

                ImGui.TableNextColumn();
                if (Widgets.IconButton(FontAwesomeIcon.EllipsisH))
                {
                    ImGui.OpenPopup("TableTools");
                }

                ImGui.TableNextRow();

                if (currentTypeTransactions.Count > 0)
                {
                    for (var i = visibleStartIndex; i < visibleEndIndex; i++)
                    {
                        var transaction = currentTypeTransactions[i];
                        while (selectedStates[selectedCurrencyName].Count <= i)
                        {
                            selectedStates[selectedCurrencyName].Add(false);
                        }

                        var selected = selectedStates[selectedCurrencyName][i];

                        // 序号 Order Number
                        if (isShowOrderColumn)
                        {
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
                        }

                        // 时间 Time
                        ImGui.TableNextColumn();
                        if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) && ImGui.IsMouseDown(ImGuiMouseButton.Right))
                        {
                            ImGui.Selectable($"{transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")}##_{i}", selected, ImGuiSelectableFlags.SpanAllColumns);
                            if (ImGui.IsItemHovered())
                            {
                                selectedStates[selectedCurrencyName][i] = selected = true;

                                if (selected)
                                {
                                    var exists = selectedTransactions[selectedCurrencyName].Any(t => Widgets.IsTransactionEqual(t, transaction));

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
                        else if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
                        {
                            if (ImGui.Selectable($"{transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")}##_{i}", ref selected, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                selectedStates[selectedCurrencyName][i] = selected;

                                if (selected)
                                {
                                    var exists = selectedTransactions[selectedCurrencyName].Any(t => Widgets.IsTransactionEqual(t, transaction));

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
                            ImGui.Selectable($"{transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")}##_{i}");
                        }

                        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
                        {
                            ImGui.SetClipboardText(transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"));
                            Service.Chat.Print($"{Service.Lang.GetText("CopiedToClipboard")}: {transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")}");
                        }

                        // 货币数 Amount
                        ImGui.TableNextColumn();
                        ImGui.Selectable($"{transaction.Amount.ToString("#,##0")}##_{i}");

                        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
                        {
                            ImGui.SetClipboardText(transaction.Amount.ToString("#,##0"));
                            Service.Chat.Print($"{Service.Lang.GetText("CopiedToClipboard")}: {transaction.Amount.ToString("#,##0")}");
                        }

                        // 收支 Change
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
                            Service.Chat.Print($"{Service.Lang.GetText("CopiedToClipboard")} : {transaction.Change.ToString("+ #,##0;- #,##0;0")}");
                        }

                        // 地名 Location
                        if (isShowLocationColumn)
                        {
                            ImGui.TableNextColumn();
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
                                    var filePath = Path.Combine(P.PlayerDataFolder, $"{selectedCurrencyName}.txt");
                                    var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyName);
                                    var index = -1;

                                    for (var d = 0; d < editedTransactions.Count; d++)
                                    {
                                        if (Widgets.IsTransactionEqual(editedTransactions[d], transaction))
                                        {
                                            index = d;
                                            break;
                                        }
                                    }
                                    if (index != -1)
                                    {
                                        editedTransactions[index].LocationName = editedLocationName;
                                        TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                                        searchTimer.Stop();
                                        searchTimer.Start();
                                    }
                                    else
                                    {
                                        Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                                    }
                                }

                                ImGui.EndPopup();
                            }
                        }

                        // 备注 Note
                        if (isShowNoteColumn)
                        {
                            ImGui.TableNextColumn();
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
                                    var filePath = Path.Combine(P.PlayerDataFolder, $"{selectedCurrencyName}.txt");
                                    var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyName);
                                    var index = -1;

                                    for (var d = 0; d < editedTransactions.Count; d++)
                                    {
                                        if (Widgets.IsTransactionEqual(editedTransactions[d], transaction))
                                        {
                                            index = d;
                                            break;
                                        }
                                    }
                                    if (index != -1)
                                    {
                                        editedTransactions[index].Note = editedNoteContent;
                                        TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                                        searchTimer.Stop();
                                        searchTimer.Start();
                                    }
                                    else
                                    {
                                        Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                                    }
                                }

                                ImGui.EndPopup();
                            }
                        }

                        // 勾选框 Checkboxes
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
                }

                ImGui.EndTable();
            }

            ImGui.EndChildFrame();
        }
    }
}
