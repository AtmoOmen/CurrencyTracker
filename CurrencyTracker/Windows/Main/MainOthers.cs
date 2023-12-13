namespace CurrencyTracker.Windows
{
    // 打开数据文件夹 / 打开 GitHub / 帮助页面 / 多语言切换 / 测试功能
    public partial class Main : Window, IDisposable
    {
        private void OthersUI()
        {
            OpenDataFolderUI();
            ImGui.SameLine();
            MultiCharaStatsUI();
            ImGui.SameLine();
            GraphWindowUI();
            ImGui.SameLine();
            HelpPageUI();
            ImGui.SameLine();
            LanguageSwitchUI();
            if (P.PluginInterface.IsDev) TestingFeaturesUI();
        }

        // 打开数据文件夹 Open Data Folder
        private void OpenDataFolderUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Folder, Service.Lang.GetText("OpenDataFolder")))
            {
                OpenDirectory(P.PlayerDataFolder);
            }
        }

        // 多角色数据界面 Multi-Chara Stats
        private void MultiCharaStatsUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ChartColumn, Service.Lang.GetText("MultiCharaStats")))
            {
                if (!characterCurrencyInfos.Any()) LoadMultiCharaData();
                LoadMultiCharaSearch();
                ImGui.OpenPopup("MultiCharStats");
            }

            if (ImGui.BeginPopup("MultiCharStats"))
            {
                ImGui.SetNextItemWidth(240f);
                if (ImGui.InputTextWithHint("##selectfltsmultichara", Service.Lang.GetText("PleaseSearch"), ref searchFilterMCS, 100))
                {
                    searchTimerMCS.Restart();
                }
                ImGui.SameLine();
                if (ImGui.ArrowButton("CustomPreviousPage", ImGuiDir.Left) && currentPageMCS > 0)
                {
                    currentPageMCS--;
                    LoadMultiCharaSearch();
                }
                ImGui.SameLine();
                if (ImGui.ArrowButton("CustomNextPage", ImGuiDir.Right) && currentPageMCS < (characterCurrencyInfos.Count - 1) / itemsPerPageCCT)
                {
                    currentPageMCS++;
                    LoadMultiCharaSearch();
                }
                ImGui.SameLine();
                if (IconButton(FontAwesomeIcon.Sync, "None", "MCSRefresh"))
                {
                    LoadMultiCharaData();
                    LoadMultiCharaSearch();
                }

                foreach (var characterCurrencyInfo in charactersToShow)
                {
                    if (ImGui.BeginCombo($"##{characterCurrencyInfo.Character.Name}@{characterCurrencyInfo.Character.Server}", $"{characterCurrencyInfo.Character.Name}@{characterCurrencyInfo.Character.Server}", ImGuiComboFlags.HeightLarge))
                    {
                        foreach (var currency in C.AllCurrencies)
                        {
                            var amount = characterCurrencyInfo.CurrencyAmount.TryGetValue(currency.Key, out var value) ? value : 0;
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
                            ImGui.Image(C.AllCurrencyIcons[currency.Key].ImGuiHandle, ImGuiHelpers.ScaledVector2(16.0f));
                            ImGui.SameLine();
                            ImGui.Text($"{currency.Value} - {amount}");
                        }
                        ImGui.EndCombo();
                    }
                }
                ImGui.EndPopup();
            }
        }

        // 加载多角色数据 Load data for Multi-Chara Stats
        private void LoadMultiCharaData()
        {
            Parallel.ForEach(C.CurrentActiveCharacter, character =>
            {
                if (!characterCurrencyInfos.TryGetValue(character, out var existingInfo))
                {
                    existingInfo = new CharacterCurrencyInfo { Character = character };
                    characterCurrencyInfos.Add(character, existingInfo);
                }
                existingInfo.GetCharacterCurrencyAmount();
            });
        }

        // 加载多角色搜索数据 Load data for Multi-Chara Stats
        private void LoadMultiCharaSearch()
        {
            charactersToShow = characterCurrencyInfos.Values
                .Where(IsMCSMatch)
                .OrderByDescending(characterCurrencyInfo => characterCurrencyInfo.Character.ContentID == P.CurrentCharacter.ContentID)
                .Skip(currentPageMCS * itemsPerPageCCT)
                .Take(itemsPerPageCCT);
        }

        private bool IsMCSMatch(CharacterCurrencyInfo characterCurrencyInfo)
        {
            if (searchFilterMCS.IsNullOrEmpty())
            {
                return true;
            }

            return characterCurrencyInfo.Character.Name.Contains(searchFilterMCS, StringComparison.OrdinalIgnoreCase)
                || characterCurrencyInfo.Character.Server.Contains(searchFilterMCS, StringComparison.OrdinalIgnoreCase);
        }

        // 延迟多角色数据搜索 Lagged MCS Searching
        private void SearchTimerMCSElapsed(object? sender, ElapsedEventArgs e)
        {
            currentPageMCS = 0;
            LoadMultiCharaSearch();
        }

        // 图表窗口 Graphs Window
        private void GraphWindowUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ChartBar, Service.Lang.GetText("Graphs")))
            {
                if (selectedCurrencyID != 0 &&  currentTypeTransactions != null && currentTypeTransactions.Count > 1)
                {
                    P.Graph.IsOpen = !P.Graph.IsOpen;
                }
            }
        }

        // 帮助页面 Help Page
        private void HelpPageUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.QuestionCircle, $"{Service.Lang.GetText("NeedHelp")}"))
            {
                ImGui.OpenPopup("NeedHelp");
            }

            if (ImGui.BeginPopup("NeedHelp"))
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Guide")}:");
                ImGui.Separator();
                if (ImGui.Button($"{Service.Lang.GetText("OperationGuide")} (GitHub)"))
                {
                    Util.OpenLink("https://github.com/AtmoOmen/CurrencyTracker/wiki/Operations");
                }

                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("SuggestOrReport")}?");
                ImGui.Separator();
                ImGui.Text("GitHub - AtmoOmen, Discord - AtmoOmen#0");
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.TankBlue);
                if (ImGui.Button("GitHub Issue"))
                {
                    Util.OpenLink("https://github.com/AtmoOmen/CurrencyTracker/issues");
                }
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedPurple);
                if (ImGui.Button("Discord Thread"))
                {
                    Util.OpenLink("https://discord.com/channels/581875019861328007/1019646133305344090/threads/1163039624957010021");
                }
                ImGui.PopStyleColor();
                if (C.SelectedLanguage == "ChineseSimplified")
                {
                    ImGui.TextColored(ImGuiColors.DalamudYellow, "如果你是国服玩家:\n" +
                        "加入下面的 QQ 频道，在 XIVLauncher/Dalamud 分栏下\n" +
                        "选择 插件问答帮助 频道, 直接 @AtmoOmen 向我提问\n");
                    if (ImGui.Button("【艾欧泽亚泛獭保护协会】"))
                    {
                        Util.OpenLink("https://pd.qq.com/s/fttirpnql");
                    }
                }

                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("HelpTranslate")}!");
                ImGui.Separator();
                if (ImGui.Button($"Crowdin"))
                {
                    Util.OpenLink("https://crowdin.com/project/dalamud-currencytracker");
                }

                ImGui.EndPopup();
            }
        }

        // 界面语言切换功能 Language Switch
        private void LanguageSwitchUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Globe, "Languages"))
            {
                ImGui.OpenPopup("LanguagesList");
            }

            if (ImGui.BeginPopup("LanguagesList"))
            {
                foreach (var languageInfo in LanguageManager.LanguageNames)
                {
                    if (ImGui.Button(languageInfo.DisplayName) && languageInfo.Language != C.SelectedLanguage)
                    {
                        C.SelectedLanguage = languageInfo.Language;
                        C.Save();

                        Service.Lang = new LanguageManager(C.SelectedLanguage);
                        Service.CommandManager.RemoveHandler(Plugin.CommandName);
                        Service.CommandManager.AddHandler(Plugin.CommandName, new CommandInfo(P.OnCommand)
                        {
                            HelpMessage = Service.Lang.GetText("CommandHelp") + "\n" + Service.Lang.GetText("CommandHelp1")
                        });
                    }

                    TextTooltip($"By: {languageInfo.Translators}");
                }

                ImGui.EndPopup();
            }
        }

        // 测试功能 Features still under testing
        private unsafe void TestingFeaturesUI()
        {
        }
    }
}
