namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    public Main(Plugin plugin) : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;

        Initialize(plugin);
    }

    // 初始化 Initialize
    private void Initialize(Plugin plugin)
    {
        transactionsPerPage = C.RecordsPerPage;
        isChangeColoring = C.ChangeTextColoring;
        positiveChangeColor = C.PositiveChangeColor;
        negativeChangeColor = C.NegativeChangeColor;
        childWidthOffset = C.ChildWidthOffset;

        if (filterEndDate.Month == 1 && filterEndDate.Day == 1) filterStartDate = new DateTime(DateTime.Now.Year - 1, 12, 31);
        else filterStartDate = filterStartDate = filterEndDate.AddDays(-1);

        searchTimer.Elapsed += SearchTimerElapsed;
        searchTimer.AutoReset = false;

        searchTimerCCT.Elapsed += SearchTimerCCTElapsed;
        searchTimerCCT.AutoReset = false;

        LoadOptions();
    }

    // 将预置货币类型、玩家自定义的货币类型加入选项列表 Add preset currencies and player-customed currencies to the list of options
    private void LoadOptions()
    {
        var addedOptions = new HashSet<uint>();

        foreach (var currency in C.AllCurrencies)
        {
            if (!addedOptions.Contains(currency.Key))
            {
                addedOptions.Add(currency.Key);
                selectedStates.Add(currency.Key, new());
                selectedTransactions.Add(currency.Key, new());
            }
        }

        if (C.OrderedOptions.Count == 0 || C.OrderedOptions == null)
        {
            C.OrderedOptions = C.AllCurrencies.Keys.ToList();
            C.Save();
        }
        else
        {
            ReloadOrderedOptions();
        }

        if (itemNamesCCT.Count == 0 || itemNamesCCT == null)
        {
            itemNamesCCT = InitCCTItems();
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
            MergeTransactionUI();
            ImGui.SameLine();
            ClearExceptionUI();
            ImGui.SameLine();
            ExportDataUI();
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
            OpenDataFolderUI();
            ImGui.SameLine();
            OpenGitHubUI();
            ImGui.SameLine();
            HelpPageUI();
            ImGui.SameLine();
            LanguageSwitchUI();
            if (P.PluginInterface.IsDev)
            {
                FeaturesUnderTest();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        CurrencyListboxUI();

        TransactionTableUI();
    }

    // 测试用功能区 Some features still under testing
    private void FeaturesUnderTest()
    {
    }

    // (临时)记录设置 (Temp)Record Settings
    private void TempRecordSettings()
    {
        /*
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
                    Service.Tracker.UninitSpecialExchange();
                    Service.Tracker.InitExchangeCompletes();
                    Service.Tracker.InitSpecialExchange();
                }
                else
                {
                    Service.Tracker.UninitExchangeCompletes();
                    Service.Tracker.UninitSpecialExchange();
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
                    Service.Tracker.UninitWarpCosts();
                    Service.Tracker.InitTeleportCosts();
                    Service.Tracker.InitWarpCosts();
                }
                else
                {
                    Service.Tracker.UninitTeleportCosts();
                    Service.Tracker.UninitWarpCosts();
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
        */
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
        if (IconButton(FontAwesomeIcon.ChartBar, Service.Lang.GetText("Graphs")) && pageCount > 0)
        {
            if (selectedCurrencyID != 0 && currentTypeTransactions.Count != 1 && currentTypeTransactions != null)
            {
                P.Graph.IsOpen = !P.Graph.IsOpen;
            }
            else return;
        }

        ImGui.SameLine();

        // 首页 First Page
        var pageButtonPosX = (ImGui.GetWindowWidth() - 360) / 2 - 40;
        ImGui.SetCursorPosX(pageButtonPosX);
        if (IconButton(FontAwesomeIcon.Backward))
            currentPage = 0;

        ImGui.SameLine();

        // 上一页 Last Page
        if (ImGui.ArrowButton("PreviousPage", ImGuiDir.Left) && currentPage > 0)
            currentPage--;

        ImGui.SameLine();

        // 页数显示 Pages
        ImGui.Text($"{Service.Lang.GetText("PageComponent", currentPage + 1, pageCount)}");

        ImGui.SameLine();

        // 下一页 Next Page
        if (ImGui.ArrowButton("NextPage", ImGuiDir.Right) && currentPage < pageCount - 1)
            currentPage++;

        ImGui.SameLine();

        // 尾页 Final Page
        if (IconButton(FontAwesomeIcon.Forward) && currentPage >= 0)
            currentPage = pageCount;

        ImGui.SameLine();

        // 表格外观
        if (IconButton(FontAwesomeIcon.Table, Service.Lang.GetText("TableAppearance"), "TableAppearance"))
            ImGui.OpenPopup("TableAppearence");

        if (ImGui.BeginPopup("TableAppearence"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ColumnsDisplayed")}:");

            ColumnDisplayCheckbox("ShowTimeColumn", "Time");
            ImGui.SameLine();
            ColumnDisplayCheckbox("ShowAmountColumn", "Amount");
            ImGui.SameLine();
            ColumnDisplayCheckbox("ShowChangeColumn", "Change");
            ColumnDisplayCheckbox("ShowOrderColumn", "Order");
            ImGui.SameLine();
            ColumnDisplayCheckbox("ShowLocationColumn", "Location");
            ImGui.SameLine();
            ColumnDisplayCheckbox("ShowNoteColumn", "Note");
            ImGui.SameLine();
            ColumnDisplayCheckbox("ShowCheckboxColumn", "Checkbox");

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ChildframeWidthOffset")}:");
            ImGui.SetNextItemWidth(150);
            ImGui.SameLine();
            if (ImGui.InputInt("##ChildframesWidthOffset", ref childWidthOffset, 10))
            {
                childWidthOffset = Math.Max(-240, childWidthOffset);
                C.ChildWidthOffset = childWidthOffset;
                C.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("TransactionsPerPage"));
            ImGui.SetNextItemWidth(150);
            ImGui.SameLine();
            if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
            {
                transactionsPerPage = Math.Max(transactionsPerPage, 1);
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

    public void Dispose()
    {
        searchTimer.Elapsed -= SearchTimerElapsed;
        searchTimer.Stop();
        searchTimer.Dispose();

        searchTimerCCT.Elapsed -= SearchTimerCCTElapsed;
        searchTimerCCT.Stop();
        searchTimerCCT.Dispose();
    }
}
