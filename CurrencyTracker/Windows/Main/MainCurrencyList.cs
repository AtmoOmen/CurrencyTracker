using System.IO;

namespace CurrencyTracker.Windows
{
    // 货币列表 / 货币列表顶端工具栏 / 添加自定义货币 / 删除自定义货币 / 重命名货币
    public partial class Main : Window, IDisposable
    {
        // 存储可用货币名称选项的列表框 Listbox Containing Available Currencies' Name
        private void CurrencyListboxUI()
        {
            var childScale = new Vector2(243 + childWidthOffset, ChildframeHeightAdjust());
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
                if (ImGui.Selectable($"##{option}", i == selectedOptionIndex))
                {
                    selectedOptionIndex = i;
                    selectedCurrencyID = option;
                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID));
                    currentView = TransactionFileCategory.Inventory;
                    currentViewID = 0;
                }

                if (ImGui.IsItemHovered() && ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
                {
                    ImGui.SetTooltip(C.AllCurrencies[option]);
                }

                ImGui.SameLine(3.0f);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3.0f);
                var currencyIcon = C.AllCurrencyIcons[option];
                if (currencyIcon == null)
                {
                    C.GetAllCurrencyIcons();
                }
                else
                {
                    ImGui.Image(currencyIcon.ImGuiHandle, ImGuiHelpers.ScaledVector2(20.0f));
                }
                ImGui.SameLine();
                ImGui.Text(C.AllCurrencies[option]);
            }

            ImGui.PopStyleColor(2);
            ImGui.EndChildFrame();
        }

        // 货币列表顶端工具栏 Currency List Listbox Tools
        private void CurrencyListboxToolUI()
        {
            CenterCursorFor(185);

            ImGui.BeginGroup();
            AddCustomCurrencyUI();

            ImGui.SameLine();
            if (ImGui.ArrowButton("UpArrow", ImGuiDir.Up) && selectedOptionIndex > 0)
            {
                SwapOptions(selectedOptionIndex, selectedOptionIndex - 1);
                selectedOptionIndex--;
            }

            ImGui.SameLine();
            DeleteCustomCurrencyUI();

            ImGui.SameLine();
            if (ImGui.ArrowButton("DownArrow", ImGuiDir.Down) && selectedOptionIndex < C.OrderedOptions.Count - 1 && selectedOptionIndex > -1)
            {
                SwapOptions(selectedOptionIndex, selectedOptionIndex + 1);
                selectedOptionIndex++;
            }

            ImGui.SameLine();
            RenameCurrencyUI();
            ImGui.EndGroup();
        }

        // 添加自定义货币 Add Custom Currency
        private void AddCustomCurrencyUI()
        {
            if (IconButton(FontAwesomeIcon.Plus, Service.Lang.GetText("Add"), "CustomCurrencyAdd"))
            {
                ImGui.OpenPopup("CustomCurrency");
            }

            if (ImGui.BeginPopup("CustomCurrency", ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (ItemNames == null) LoadItemsForCCT();

                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("CustomCurrencyTracker"));
                ImGuiComponents.HelpMarker(Service.Lang.GetText("CustomCurrencyHelp"));

                ImGui.AlignTextToFramePadding();
                ImGui.Text($"{Service.Lang.GetText("Now")}:");
                ImGui.SameLine();

                // currencyIDCCT -> ID, selected -> Name
                ImGui.SetNextItemWidth(210);
                if (ImGui.BeginCombo("", ItemNames.TryGetValue(currencyIDCCT, out var selected) ? selected : Service.Lang.GetText("PleaseSelect"), ImGuiComboFlags.HeightLarge))
                {
                    var startIndex = currentItemPageCCT * itemsPerPageCCT;
                    var endIndex = Math.Min(startIndex + itemsPerPageCCT, itemNamesCCT.Count);

                    ImGui.SetNextItemWidth(200f);
                    if (ImGui.InputTextWithHint("##selectflts", Service.Lang.GetText("PleaseSearch"), ref searchFilterCCT, 100))
                    {
                        searchTimerCCT.Restart();
                    }

                    ImGui.SameLine();

                    // 首页 First Page
                    if (IconButton(FontAwesomeIcon.Backward, "None", "CCTFirstPage")) currentItemPageCCT = 0; ImGui.SameLine();

                    // 上一页 Previous Page
                    if (ImGui.ArrowButton("CustomPreviousPage", ImGuiDir.Left) && currentItemPageCCT > 0) currentItemPageCCT--; ImGui.SameLine();

                    // 下一页 Next Page
                    if (itemNamesCCT.Count > 0)
                    {
                        if (ImGui.ArrowButton("CustomNextPage", ImGuiDir.Right) && currentItemPageCCT < (itemNamesCCT.Count / itemsPerPageCCT) - 1)
                        {
                            currentItemPageCCT++;
                        }
                    }
                    else
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                        ImGui.ArrowButton("CustomNextPage", ImGuiDir.Right);
                        ImGui.PopStyleVar();
                    }
                    ImGui.SameLine();

                    // 尾页 Last Page
                    if (itemNamesCCT.Count > 0)
                    {
                        if (IconButton(FontAwesomeIcon.Forward, "None", "CCTLastPage"))
                        {
                            currentItemPageCCT = (itemNamesCCT.Count / itemsPerPageCCT) - 1;
                        }
                    }
                    else
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                        IconButton(FontAwesomeIcon.Forward, "None", "CCTLastPage");
                        ImGui.PopStyleVar();
                    }

                    // 鼠标滚轮控制翻页 Mouse wheel control for flipping pages
                    if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel > 0 && currentItemPageCCT > 0) currentItemPageCCT--;
                    if (itemNamesCCT.Count > 0 && ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel < 0 && currentItemPageCCT < (itemNamesCCT.Count / itemsPerPageCCT) - 1) currentItemPageCCT++;

                    ImGui.Separator();

                    if (itemNamesCCT.Count > 0)
                    {
                        foreach (var itemName in itemNamesCCT.Skip(startIndex).Take(endIndex - startIndex))
                        {
                            var itemKeyPair = ItemNames.FirstOrDefault(x => x.Value == itemName);
                            if (ImGui.Selectable(itemName))
                            {
                                currencyIDCCT = itemKeyPair.Key;
                            }

                            if (ImGui.IsWindowAppearing() && currencyIDCCT == itemKeyPair.Key)
                            {
                                ImGui.SetScrollHereY();
                            }
                        }
                    }

                    ImGui.EndCombo();
                }

                if (ImGui.IsItemClicked())
                {
                    if (ItemNames == null)
                    {
                        LoadItemsForCCT();
                    }

                    if (itemNamesCCT.Count == 0 || itemNamesCCT == null || itemNamesCCT.Count != itemCountsCCT)
                    {
                        itemNamesCCT = ApplyCCTFilter();
                    }
                }

                ImGui.SameLine();

                if (IconButton(FontAwesomeIcon.Plus, "None", "AddCustomCurrency"))
                {
                    if (selected.IsNullOrEmpty())
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                        return;
                    }

                    if (C.AllCurrencies.ContainsValue(selected) || C.AllCurrencies.ContainsKey(currencyIDCCT))
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("CustomCurrencyHelp1"));
                        return;
                    }

                    C.CustomCurrencies.Add(currencyIDCCT, selected);
                    C.isUpdated = true;
                    C.Save();

                    selectedStates.Add(currencyIDCCT, new());
                    selectedTransactions.Add(currencyIDCCT, new());
                    selectedOptionIndex = C.OrderedOptions.Count;
                    selectedCurrencyID = currencyIDCCT;
                    ReloadOrderedOptions();

                    Service.Tracker.CheckAllCurrencies("", "", RecordChangeType.All, 1);
                    currentTypeTransactions = Transactions.LoadAllTransactions(selectedCurrencyID);

                    searchFilterCCT = string.Empty;
                    itemNamesCCT.Remove(selected);
                    currencyIDCCT = 0;

                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        // 加载自定义货币追踪里的所有物品 Load All Items for CCT
        public static void LoadItemsForCCT()
        {
            var items = Service.DataManager.GetExcelSheet<Item>()
                .Select(x => new { x.RowId, Name = x.Name?.ToString() })
                .Where(x => !x.Name.IsNullOrEmpty() && !filterNamesForCCT.Contains(x.Name))
                .ToList();

            ItemNames = items.ToDictionary(x => x.RowId, x => $"{x.Name}");
            itemNamesCCT = ItemNames.Values.ToList();
        }

        // 按搜索结果显示自定义货币追踪里的物品 Show On-Demand Items Based On Filter
        private List<string> ApplyCCTFilter(string searchFilterCCT = "")
        {
            if (!string.IsNullOrEmpty(searchFilterCCT))
            {
                var isChineseSimplified = C.SelectedLanguage == "ChineseSimplified";
                return itemNamesCCT
                    .Where(itemName => itemName.Contains(searchFilterCCT, StringComparison.OrdinalIgnoreCase)
                        || (isChineseSimplified && PinyinHelper.GetPinyin(itemName, "").Contains(searchFilterCCT, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            else
            {
                var currencyNames = C.AllCurrencies.Keys.Select(CurrencyInfo.CurrencyLocalName).ToHashSet();
                var items = ItemNames.Values
                    .Where(itemName => !currencyNames.Contains(itemName))
                    .ToList();

                itemCountsCCT = (uint)items.Count;
                return items;
            }
        }

        // 延迟加载搜索结果 Used to handle too-fast CCT items loading
        private void SearchTimerCCTElapsed(object? sender, ElapsedEventArgs e)
        {
            itemNamesCCT = ApplyCCTFilter(searchFilterCCT);
            currentItemPageCCT = 0;
        }

        // 删除自定义货币 Delete Custom Currency
        private void DeleteCustomCurrencyUI()
        {
            if (selectedCurrencyID != 0 && !C.PresetCurrencies.ContainsKey(selectedCurrencyID))
            {
                IconButton(FontAwesomeIcon.Trash, Service.Lang.GetText("DeleteCurrency"), "ToolsDelete");
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
                {
                    if (selectedCurrencyID == 0)
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                        return;
                    }
                    if (!C.AllCurrencies.ContainsKey(selectedCurrencyID))
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("CustomCurrencyHelp2"));
                        return;
                    }

                    C.CustomCurrencies.Remove(selectedCurrencyID);
                    C.isUpdated = true;
                    C.Save();

                    selectedStates.Remove(selectedCurrencyID);
                    selectedTransactions.Remove(selectedCurrencyID);
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

        // 修改货币本地名称 Rename Currency
        private void RenameCurrencyUI()
        {
            if (selectedCurrencyID == 0)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                IconButton(FontAwesomeIcon.Pen, "None", "RenameCurrency");
                ImGui.PopStyleVar();
            }
            else
            {
                if (IconButton(FontAwesomeIcon.Pen, Service.Lang.GetText("Rename"), "RenameCurrency"))
                {
                    ImGui.OpenPopup("CurrencyRename");
                    if (C.AllCurrencies.TryGetValue(selectedCurrencyID, out var currencyName))
                    {
                        editedCurrencyName = currencyName;
                    }
                }
            }

            if (ImGui.BeginPopup("CurrencyRename", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Now")}:");

                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
                ImGui.Image(C.AllCurrencyIcons[selectedCurrencyID].ImGuiHandle, ImGuiHelpers.ScaledVector2(16.0f));

                ImGui.SameLine();
                ImGui.Text(C.AllCurrencies.TryGetValue(selectedCurrencyID, out var currencyName) ? currencyName : "Unknown");

                ImGui.SetNextItemWidth(Math.Max(ImGui.CalcTextSize(currencyName).X + 115, 150));
                ImGui.InputText($"##CurrencyRename", ref editedCurrencyName, 150, ImGuiInputTextFlags.AutoSelectAll);

                if (!editedCurrencyName.IsNullOrEmpty() && editedCurrencyName != C.AllCurrencies[selectedCurrencyID])
                {
                    if (ImGui.Button(Service.Lang.GetText("Confirm")))
                    {
                        RenameCurrencyHandler(editedCurrencyName);
                        ImGui.CloseCurrentPopup();
                    }
                }
                else
                {
                    ImGui.Button(Service.Lang.GetText("Confirm"));
                }

                ImGui.SameLine();
                if (ImGui.Button(Service.Lang.GetText("Reset")))
                {
                    RenameCurrencyHandler(CurrencyInfo.CurrencyLocalName(selectedCurrencyID));
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        // 用于处理货币名变更 Used to handle currency rename
        private void RenameCurrencyHandler(string editedCurrencyName)
        {
            if (C.AllCurrencies.ContainsValue(editedCurrencyName))
            {
                Service.Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp1"));
                return;
            }

            var filePaths = new Dictionary<string, string>();
            var categories = new List<TransactionFileCategory> { TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag, TransactionFileCategory.PremiumSaddleBag };
            foreach (var category in categories)
            {
                var editedFilePath = Path.Join(P.PlayerDataFolder, $"{editedCurrencyName}{Transactions.GetTransactionFileSuffix(category, 0)}.txt");
                var selectedFilePath = Transactions.GetTransactionFilePath(selectedCurrencyID, category, 0);
                filePaths[selectedFilePath] = editedFilePath;
                Service.Log.Debug($"{selectedFilePath} | {editedFilePath}");
            }
            foreach (var retainer in C.CharacterRetainers[P.CurrentCharacter.ContentID])
            {
                var editedFilePath = Path.Join(P.PlayerDataFolder, $"{editedCurrencyName}{Transactions.GetTransactionFileSuffix(TransactionFileCategory.Retainer, retainer.Key)}.txt");
                var selectedFilePath = Transactions.GetTransactionFilePath(selectedCurrencyID, TransactionFileCategory.Retainer, retainer.Key);
                filePaths[selectedFilePath] = editedFilePath;
                Service.Log.Debug($"{selectedFilePath} | {editedFilePath}");
            }

            if (filePaths.Values.Any(x => File.Exists(x)))
            {
                Service.Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp1"));
                return;
            }

            if (C.PresetCurrencies.TryGetValue(selectedCurrencyID, out var currencyName) || C.CustomCurrencies.TryGetValue(selectedCurrencyID, out currencyName))
            {
                if (C.PresetCurrencies.ContainsKey(selectedCurrencyID))
                {
                    C.PresetCurrencies[selectedCurrencyID] = editedCurrencyName;
                }
                else
                {
                    C.CustomCurrencies[selectedCurrencyID] = editedCurrencyName;
                }
                C.isUpdated = true;
                C.Save();

                foreach (var path in filePaths)
                {
                    Service.Log.Debug($"Try moving file from {path.Key} to {path.Value}");
                    if (File.Exists(path.Key))
                    {
                        File.Move(path.Key, path.Value);
                    }
                }
                UpdateTransactions();
            }
        }
    }
}
