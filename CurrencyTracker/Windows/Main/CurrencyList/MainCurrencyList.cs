namespace CurrencyTracker.Windows
{
    // 货币列表 / 货币列表顶端工具栏 / 添加自定义货币 / 删除自定义货币 / 重命名货币
    public partial class Main : Window, IDisposable
    {
        // 存储可用货币名称选项的列表框 Listbox Containing Available Currencies' Name
        private void CurrencyListboxUI()
        {
            selectedOptionIndex = C.OrderedOptions.IndexOf(selectedCurrencyID);

            var childScale = new Vector2(243 + C.ChildWidthOffset, ChildframeHeightAdjust());
            if (!ImGui.BeginChildFrame(2, childScale, ImGuiWindowFlags.NoScrollbar)) return;

            CurrencyListboxToolUI();
            ImGui.Separator();
            ImGui.SetNextItemWidth(235);

            var style = ImGui.GetStyle();
            var headerHoveredColor = style.Colors[(int)ImGuiCol.HeaderHovered];
            var textSelectedColor = style.Colors[(int)ImGuiCol.Header];
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, headerHoveredColor with { W = 0.2f });
            ImGui.PushStyleColor(ImGuiCol.Header, textSelectedColor with { W = 0.2f });

            for (var i = 0; i < C.OrderedOptions.Count; i++)
            {
                var option = C.OrderedOptions[i];
                var currencyName = C.AllCurrencies[option];
                if (ImGui.Selectable($"##{option}", i == selectedOptionIndex))
                {
                    selectedCurrencyID = option;
                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID));
                    currentView = TransactionFileCategory.Inventory;
                    currentViewID = 0;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(currencyName);
                }

                ImGui.SameLine(3.0f);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3.0f);
                var currencyIcon = C.AllCurrencyIcons[option];
                ImGui.Image(currencyIcon.ImGuiHandle, ImGuiHelpers.ScaledVector2(20.0f));

                ImGui.SameLine();
                ImGui.Text(currencyName);
            }

            ImGui.PopStyleColor(2);
            ImGui.EndChildFrame();
        }

        // 货币列表顶端工具栏 Currency List Listbox Tools
        private void CurrencyListboxToolUI()
        {
            CenterCursorFor(184);

            AddCustomCurrencyUI();

            ImGui.SameLine();
            if (ImGui.ArrowButton("UpArrow", ImGuiDir.Up)) SwapOptions(selectedOptionIndex, selectedOptionIndex - 1);

            ImGui.SameLine();
            DeleteCustomCurrencyUI();

            ImGui.SameLine();
            if (ImGui.ArrowButton("DownArrow", ImGuiDir.Down)) SwapOptions(selectedOptionIndex, selectedOptionIndex + 1);

            ImGui.SameLine();
            CurrencySettingsUI();
        }        

        // 删除自定义货币 Delete Custom Currency
        private void DeleteCustomCurrencyUI()
        {
            if (selectedCurrencyID != 0 && !C.PresetCurrencies.ContainsKey(selectedCurrencyID))
            {
                IconButton(FontAwesomeIcon.Trash, $"{Service.Lang.GetText("Delete")} ({Service.Lang.GetText("DoubleRightClick")})", "ToolsDelete");
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
                {
                    if (selectedCurrencyID == 0)
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                        return;
                    }
                    if (!C.AllCurrencyID.Contains(selectedCurrencyID))
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("CustomCurrencyHelp2"));
                        return;
                    }

                    var localName = CurrencyInfo.GetCurrencyLocalName(selectedCurrencyID);
                    if (C.CustomCurrencies[selectedCurrencyID] != localName) RenameCurrencyHandler(localName);

                    C.CustomCurrencies.Remove(selectedCurrencyID);
                    C.Save();

                    selectedCurrencyID = 0;
                    ReloadOrderedOptions();
                }
            }
            else
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                IconButton(FontAwesomeIcon.Trash);
                ImGui.PopStyleVar();
            }
        }

        // 货币设置 Currency Settings
        private void CurrencySettingsUI()
        {
            if (selectedCurrencyID == 0)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                IconButton(FontAwesomeIcon.Edit, "", "CurrencySettings");
                ImGui.PopStyleVar();
            }
            else
            {
                if (IconButton(FontAwesomeIcon.Edit, "", "CurrencySettings"))
                {
                    ImGui.OpenPopup("CurrencySettings");
                    if (C.AllCurrencies.TryGetValue(selectedCurrencyID, out var currencyName))
                    {
                        editedCurrencyName = currencyName;
                    }
                }
            }

            if (ImGui.BeginPopup("CurrencySettings"))
            {
                if (ItemNames == null) LoadTerrioriesNamesCS();

                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Now")}:");

                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
                ImGui.Image(C.AllCurrencyIcons[selectedCurrencyID].ImGuiHandle, ImGuiHelpers.ScaledVector2(16.0f));

                ImGui.SameLine();
                ImGui.Text(C.AllCurrencies.TryGetValue(selectedCurrencyID, out var currencyName) ? currencyName : "Unknown");

                var currencyBarWidth = ImGui.CalcTextSize($"{Service.Lang.GetText("Now")}:").X + 8f + (16f * ImGuiHelpers.GlobalScale) + 8f + ImGui.CalcTextSize(currencyName).X;

                ImGui.Separator();

                RenameCurrencyUI(currencyName, currencyBarWidth);

                TerrioryRestrictedUI(currencyName, currencyBarWidth);

                ImGui.EndPopup();
            }
        }

        // 修改货币本地名称 Rename Currency
        private void RenameCurrencyUI(string currencyName, float currencyBarWidth)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Rename")}:");

            ImGui.SetNextItemWidth(Math.Max(currencyBarWidth - 80f, 210f)); // 80f = 2 * 8f (Default Spacing Size of ImGui.SameLine) + 2 * 32f (Default Size of IconButton)
            ImGui.InputText($"##CurrencyRename", ref editedCurrencyName, 150, ImGuiInputTextFlags.AutoSelectAll);
            if (!editedCurrencyName.IsNullOrEmpty()) HoverTooltip(editedCurrencyName);

            ImGui.SameLine();
            if (!editedCurrencyName.IsNullOrEmpty() && editedCurrencyName != C.AllCurrencies[selectedCurrencyID])
            {
                if (IconButton(FontAwesomeIcon.Check, Service.Lang.GetText("Confirm"), "RenameCurrencyConfirm"))
                {
                    RenameCurrencyHandler(editedCurrencyName);
                    ImGui.CloseCurrentPopup();
                }
            }
            else
            {
                IconButton(FontAwesomeIcon.Check, Service.Lang.GetText("Confirm"), "RenameCurrencyConfirm");
            }

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.Sync, Service.Lang.GetText("Reset"), "RenameCurrencyReset"))
            {
                RenameCurrencyHandler(CurrencyInfo.GetCurrencyLocalName(selectedCurrencyID));
            }
        }

        // 用于处理货币名变更 Used to handle currency rename
        private void RenameCurrencyHandler(string editedCurrencyName)
        {
            var filePaths = new Dictionary<string, string>();
            var categories = new List<TransactionFileCategory> { TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag, TransactionFileCategory.PremiumSaddleBag };
            categories.AddRange(C.CharacterRetainers[P.CurrentCharacter.ContentID].Keys.Select(x => TransactionFileCategory.Retainer));

            foreach (var category in categories)
            {
                var key = category == TransactionFileCategory.Retainer ? C.CharacterRetainers[P.CurrentCharacter.ContentID].First().Key : 0;
                var editedFilePath = Path.Join(P.PlayerDataFolder, $"{editedCurrencyName}{Transactions.GetTransactionFileSuffix(category, key)}.txt");
                var selectedFilePath = Transactions.GetTransactionFilePath(selectedCurrencyID, category, key);
                filePaths[selectedFilePath] = editedFilePath;
            }

            if (C.AllCurrencies.ContainsValue(editedCurrencyName) || filePaths.Values.Any(File.Exists))
            {
                Service.Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp1"));
            }
            else if (C.PresetCurrencies.TryGetValue(selectedCurrencyID, out var currencyName) || C.CustomCurrencies.TryGetValue(selectedCurrencyID, out currencyName))
            {
                var targetCurrency = C.PresetCurrencies.ContainsKey(selectedCurrencyID) ? C.PresetCurrencies : C.CustomCurrencies;
                targetCurrency[selectedCurrencyID] = editedCurrencyName;
                C.isUpdated = true;
                C.Save();

                foreach (var path in filePaths)
                {
                    Service.Log.Debug($"Moving file from {path.Key} to {path.Value}");
                    if (File.Exists(path.Key))
                    {
                        File.Move(path.Key, path.Value);
                    }
                }
                UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
            }
        }

        // 延迟加载搜索结果 Used to handle too-fast CS Terrories Names loading
        private void SearchTimerCSElapsed(object? sender, ElapsedEventArgs e)
        {
            LoadTerrioriesNamesCS();
        }

        // 加载地名 Load Terriories Names for CS
        private void LoadTerrioriesNamesCS()
        {
            if (searchFilterCS.IsNullOrEmpty())
            {
                TerritoryNamesCS = TerrioryHandler.TerritoryNames;
            }
            else
            {
                TerritoryNamesCS = TerrioryHandler.TerritoryNames
                    .Where(x => x.Value.Contains(searchFilterCS, StringComparison.OrdinalIgnoreCase) || x.Key.ToString().Contains(searchFilterCS, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(x => x.Key, x => x.Value);
            }
        }

        // 地区限制 Terriories Restricted UI
        private void TerrioryRestrictedUI(string currencyName, float currencyBarWidth)
        {
            var rules = C.CurrencyRules[selectedCurrencyID];
            var isBlacklist = !rules.RegionRulesMode;

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-AreaRestriction")}:");

            if (ImGui.RadioButton($"{Service.Lang.GetText("Blacklist")}", isBlacklist))
            {
                rules.RegionRulesMode = false;
                C.Save();
            }

            ImGui.SameLine();
            if (ImGui.RadioButton($"{Service.Lang.GetText("Whitelist")}", !isBlacklist))
            {
                rules.RegionRulesMode = true;
                C.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-SelectArea")}:");

            ImGui.SetNextItemWidth(Math.Max(currencyBarWidth - 75f, 215f));
            if (ImGui.BeginCombo("##AreaResticted", TerrioryHandler.TerritoryNames.TryGetValue(selectedAreaIDCS, out var selectedAreaName) ? selectedAreaName : Service.Lang.GetText("PleaseSelect"), ImGuiComboFlags.HeightLarge))
            {
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 8f);
                ImGui.TextUnformatted("");
                ImGui.SameLine(8f, 0);
                if (ImGui.InputText("", ref searchFilterCS, 50))
                {
                    searchTimerCS.Restart();
                }
                ImGui.PopItemWidth();

                foreach (var area in TerritoryNamesCS)
                {
                    if (ImGui.Selectable($"{area.Key} | {area.Value}"))
                    {
                        selectedAreaIDCS = area.Key;
                    }
                }
                ImGui.EndCombo();
            }
            if (!selectedAreaName.IsNullOrEmpty()) HoverTooltip(selectedAreaName);

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.Plus, "", "AddRestrictedAreas") && !rules.RestrictedAreas.Contains(selectedAreaIDCS))
            {
                rules.RestrictedAreas.Add(selectedAreaIDCS);
                selectedAreaIDCS = 0;
                C.Save();
            }
            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.TrashAlt, "", "DeleteRestrictedAreas") && rules.RestrictedAreas.Contains(selectedAreaIDCS))
            {
                rules.RestrictedAreas.Remove(selectedAreaIDCS);
                selectedAreaIDCS = 0;
                C.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-RestrictedArea")}:");

            ImGui.SetNextItemWidth(Math.Max(currencyBarWidth - 3f, 285f));
            if (ImGui.BeginCombo("##RestictedAreas", rules.RestrictedAreas.Any() ? TerrioryHandler.TerritoryNames[rules.RestrictedAreas.FirstOrDefault()] : "", ImGuiComboFlags.HeightLarge))
            {
                foreach (var area in rules.RestrictedAreas)
                {
                    ImGui.Selectable($"{area} | {TerrioryHandler.TerritoryNames[area]}");
                }

                ImGui.EndCombo();
            }
        }
    }
}
