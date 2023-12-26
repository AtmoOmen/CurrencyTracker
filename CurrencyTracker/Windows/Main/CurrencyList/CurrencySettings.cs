namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private string editedCurrencyName = string.Empty;
    private Dictionary<uint, string>? TerritoryNamesTRRC;
    private string searchFilterTRRC = string.Empty;
    private uint selectedAreaIDTRRC = 0;
    private readonly Timer searchTimerTRRC = new(100);

    private void CurrencySettingsUI()
    {
        if (selectedCurrencyID != 0)
        {
            if (IconButton(FontAwesomeIcon.Edit, "", "CurrencySettings"))
            {
                if (C.AllCurrencies.TryGetValue(selectedCurrencyID, out var currencyName)) editedCurrencyName = currencyName;

                ImGui.OpenPopup("CurrencySettings");
            }
        }
        else
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            IconButton(FontAwesomeIcon.Edit, "", "CurrencySettings");
            ImGui.PopStyleVar();
        }

        if (ImGui.BeginPopup("CurrencySettings"))
        {
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

    private void RenameCurrencyUI(string currencyName, float currencyBarWidth)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Rename")}:");

        ImGui.SetNextItemWidth(Math.Max(currencyBarWidth - 80f, 210f)); // 80f = 2 * 8f (Default Spacing Size of ImGui.SameLine) + 2 * 32f (Default Size of IconButton)
        ImGui.InputText($"##CurrencyRename", ref editedCurrencyName, 150, ImGuiInputTextFlags.AutoSelectAll);
        if (!editedCurrencyName.IsNullOrEmpty()) HoverTooltip(editedCurrencyName);

        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.Check, Service.Lang.GetText("Confirm"), "RenameCurrencyConfirm"))
        {
            if (!editedCurrencyName.IsNullOrEmpty() && editedCurrencyName != C.AllCurrencies[selectedCurrencyID])
            {
                RenameCurrencyHandler(editedCurrencyName);
                ImGui.CloseCurrentPopup();
            }
        }

        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.Sync, Service.Lang.GetText("Reset"), "RenameCurrencyReset"))
        {
            RenameCurrencyHandler(CurrencyInfo.GetCurrencyLocalName(selectedCurrencyID));
        }
    }

    private void RenameCurrencyHandler(string editedCurrencyName)
    {
        var (isFilesExisted, filePaths) = ConstructFilePathsRC(editedCurrencyName);

        if (C.AllCurrencies.ContainsValue(editedCurrencyName) || !isFilesExisted)
        {
            Service.Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp1"));
            return;
        }

        if (UpdateCurrencyNameRC(selectedCurrencyID, editedCurrencyName))
        {
            foreach (var (sourcePath, targetPath) in filePaths.Where(pair => File.Exists(pair.Key)))
            {
                Service.Log.Debug($"Moving file from {sourcePath} to {targetPath}");
                File.Move(sourcePath, targetPath);
            }

            UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
        }
    }

    private (bool, Dictionary<string, string>) ConstructFilePathsRC(string editedCurrencyName)
    {
        var filePaths = new Dictionary<string, string>();
        var categories = new List<TransactionFileCategory> { TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag, TransactionFileCategory.PremiumSaddleBag };
        categories.AddRange(C.CharacterRetainers[P.CurrentCharacter.ContentID].Keys.Select(x => TransactionFileCategory.Retainer));

        foreach (var category in categories)
        {
            var key = category == TransactionFileCategory.Retainer ? C.CharacterRetainers[P.CurrentCharacter.ContentID].First().Key : 0;
            var editedFilePath = Path.Join(P.PlayerDataFolder, $"{editedCurrencyName}{Transactions.GetTransactionFileSuffix(category, key)}.txt");
            filePaths[Transactions.GetTransactionFilePath(selectedCurrencyID, category, key)] = editedFilePath;
        }

        return (filePaths.Values.All(path => !File.Exists(path)), filePaths);
    }

    private bool UpdateCurrencyNameRC(uint currencyId, string newName)
    {
        if (!C.PresetCurrencies.TryGetValue(currencyId, out var _) && !C.CustomCurrencies.TryGetValue(currencyId, out var _)) return false;

        var targetCurrency = C.PresetCurrencies.ContainsKey(currencyId) ? C.PresetCurrencies : C.CustomCurrencies;
        targetCurrency[currencyId] = newName;
        C.Save();

        return true;
    }

    private void TerrioryRestrictedUI(string currencyName, float currencyBarWidth)
    {
        var rules = C.CurrencyRules[selectedCurrencyID];
        var isBlacklist = !rules.RegionRulesMode;

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

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-SelectArea")}:");

        ImGui.SetNextItemWidth(Math.Max(currencyBarWidth - 75f, 215f));
        if (ImGui.BeginCombo("##AreaResticted", TerrioryHandler.TerritoryNames.TryGetValue(selectedAreaIDTRRC, out var selectedAreaName) ? selectedAreaName : Service.Lang.GetText("PleaseSelect"), ImGuiComboFlags.HeightLarge))
        {
            ImGui.TextUnformatted("");
            ImGui.SameLine(8f, 0);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
            if (ImGui.InputText("", ref searchFilterTRRC, 50)) searchTimerTRRC.Restart();

            foreach (var area in TerritoryNamesTRRC) if (ImGui.Selectable($"{area.Key} | {area.Value}")) selectedAreaIDTRRC = area.Key;
            ImGui.EndCombo();
        }

        if (!selectedAreaName.IsNullOrEmpty()) HoverTooltip(selectedAreaName);

        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.Plus, "", "AddRestrictedAreas") && !rules.RestrictedAreas.Contains(selectedAreaIDTRRC))
        {
            rules.RestrictedAreas.Add(selectedAreaIDTRRC);
            selectedAreaIDTRRC = 0;
            C.Save();
        }

        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.TrashAlt, "", "DeleteRestrictedAreas") && rules.RestrictedAreas.Contains(selectedAreaIDTRRC))
        {
            rules.RestrictedAreas.Remove(selectedAreaIDTRRC);
            selectedAreaIDTRRC = 0;
            C.Save();
        }

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-RestrictedArea")}:");

        ImGui.SetNextItemWidth(Math.Max(currencyBarWidth - 3f, 285f));
        using (var combo = ImRaii.Combo("##RestictedAreas", rules.RestrictedAreas.Any() ? TerrioryHandler.TerritoryNames[rules.RestrictedAreas.FirstOrDefault()] : Service.Lang.GetText("PleaseSelect"), ImGuiComboFlags.HeightLarge))
        {
            if (combo)
            {
                foreach (var area in rules.RestrictedAreas)
                {
                    ImGui.Selectable($"{area} | {TerrioryHandler.TerritoryNames[area]}");
                }
            }
        }
    }

    private void LoadDataTRRC()
    {
        if (searchFilterTRRC.IsNullOrEmpty())
        {
            TerritoryNamesTRRC = TerrioryHandler.TerritoryNames;
        }
        else
        {
            TerritoryNamesTRRC = TerrioryHandler.TerritoryNames
                .Where(x => x.Value.Contains(searchFilterTRRC, StringComparison.OrdinalIgnoreCase) || x.Key.ToString().Contains(searchFilterTRRC, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    private void SearchTimerTRRCElapsed(object? sender, ElapsedEventArgs e)
    {
        LoadDataTRRC();
    }
}
