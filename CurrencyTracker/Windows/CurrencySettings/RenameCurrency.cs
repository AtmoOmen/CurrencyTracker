namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    internal string editedCurrencyName = string.Empty;

    private void RenameCurrencyUI()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Rename")}:");

        ImGui.BeginGroup();
        ImGui.SetNextItemWidth(currencyInfoGroupWidth - (2 * M.checkboxColumnWidth) - 24);
        ImGui.InputText($"##CurrencyRename", ref editedCurrencyName, 150, ImGuiInputTextFlags.AutoSelectAll);

        if (!editedCurrencyName.IsNullOrEmpty()) ImGuiOm.TooltipHover(editedCurrencyName);

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("RenameCurrencyConfirm", FontAwesomeIcon.Check, Service.Lang.GetText("Confirm")))
        {
            if (!editedCurrencyName.IsNullOrEmpty() && editedCurrencyName != C.AllCurrencies[selectedCurrencyID])
            {
                RenameCurrencyHandler(editedCurrencyName);
            }
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("RenameCurrencyReset", FontAwesomeIcon.Sync, Service.Lang.GetText("Reset")))
        {
            RenameCurrencyHandler(CurrencyInfo.GetCurrencyLocalName(selectedCurrencyID));
        }
        ImGui.EndGroup();
    }

    internal void RenameCurrencyHandler(string editedCurrencyName)
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

            M.UpdateTransactions(selectedCurrencyID, M.currentView, M.currentViewID);
            M.ReloadOrderedOptions();
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
            var editedFilePath = Path.Join(P.PlayerDataFolder, $"{editedCurrencyName}{TransactionsHandler.GetTransactionFileSuffix(category, key)}.txt");
            filePaths[TransactionsHandler.GetTransactionFilePath(selectedCurrencyID, category, key)] = editedFilePath;
        }

        return (filePaths.Values.All(path => !File.Exists(path)), filePaths);
    }

    private bool UpdateCurrencyNameRC(uint currencyId, string newName)
    {
        if (!C.PresetCurrencies.TryGetValue(currencyId, out var _) && !C.CustomCurrencies.TryGetValue(currencyId, out var _)) return false;

        var targetCurrency = C.PresetCurrencies.ContainsKey(currencyId) ? C.PresetCurrencies : C.CustomCurrencies;
        targetCurrency[currencyId] = newName;
        C.IsUpdated = true;
        C.Save();

        return true;
    }
}
