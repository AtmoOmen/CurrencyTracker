using System.Collections.Generic;
using System.IO;
using System.Linq;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    internal string editedCurrencyName = string.Empty;

    private void RenameCurrencyUI()
    {
        ImGui.SetNextItemWidth(currencyTextWidth + ImGui.GetStyle().FramePadding.X * 2);
        if (ImGui.InputText("##currencyName", ref editedCurrencyName, 100, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (!editedCurrencyName.IsNullOrWhitespace() &&
                editedCurrencyName != Service.Config.AllCurrencies[selectedCurrencyID])
            {
                RenameCurrencyHandler(editedCurrencyName);
                isEditingCurrencyName = false;
            }
        }

        if (!editedCurrencyName.IsNullOrWhitespace()) ImGuiOm.TooltipHover(editedCurrencyName);

        if (ImGui.IsItemDeactivated())
        {
            isEditingCurrencyName = false;
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("ResetCurrencyNamePopup");
        }

        ImGui.SetWindowFontScale(1f);
        using var popup = ImRaii.Popup("ResetCurrencyNamePopup");
        if (popup.Success)
        {
            if (ImGuiOm.Selectable(Service.Lang.GetText("Reset")))
            {
                RenameCurrencyHandler(CurrencyInfo.GetCurrencyLocalName(selectedCurrencyID));
                isEditingCurrencyName = false;
            }
        }
    }

    internal void RenameCurrencyHandler(string editedCurrencyName)
    {
        var (isFilesExisted, filePaths) = ConstructFilePathsRC(editedCurrencyName);

        if (Service.Config.AllCurrencies.ContainsValue(editedCurrencyName) || !isFilesExisted)
        {
            Service.Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp1"));
            return;
        }

        if (UpdateCurrencyNameRC(selectedCurrencyID, editedCurrencyName))
        {
            foreach (var (sourcePath, targetPath) in filePaths)
            {
                Service.Log.Debug($"Moving file from {sourcePath} to {targetPath}");
                File.Move(sourcePath, targetPath);
            }

            Main.UpdateTransactions(selectedCurrencyID, Main.currentView, Main.currentViewID);
            Main.ReloadOrderedOptions();
        }
    }

    private (bool, Dictionary<string, string>) ConstructFilePathsRC(string editedCurrencyName)
    {
        var filePaths = new Dictionary<string, string>();
        var categories = new List<TransactionFileCategory>
        {
            TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag, TransactionFileCategory.PremiumSaddleBag
        };

        categories.AddRange(Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID].Keys
                             .Select(x => TransactionFileCategory.Retainer));

        foreach (var category in categories)
        {
            if (category == TransactionFileCategory.Retainer)
            {
                foreach (var retainer in Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID])
                {
                    AddFilePath(filePaths, category, retainer.Key, editedCurrencyName);
                }
            }
            else
            {
                AddFilePath(filePaths, category, 0, editedCurrencyName);
            }
        }

        return (filePaths.Values.All(path => !File.Exists(path)), filePaths);

        void AddFilePath(IDictionary<string, string> filePaths, TransactionFileCategory category, ulong key, string editedCurrencyName)
        {
            var editedFilePath = Path.Join(P.PlayerDataFolder, $"{editedCurrencyName}{TransactionsHandler.GetTransactionFileSuffix(category, key)}.txt");
            var originalFilePath = TransactionsHandler.GetTransactionFilePath(selectedCurrencyID, category, key);
            if (!File.Exists(originalFilePath)) return;
            filePaths[originalFilePath] = editedFilePath;
        }
    }

    private bool UpdateCurrencyNameRC(uint currencyId, string newName)
    {
        if (!Service.Config.PresetCurrencies.ContainsKey(currencyId) &&
            !Service.Config.CustomCurrencies.ContainsKey(currencyId)) return false;

        var targetCurrency = Service.Config.PresetCurrencies.ContainsKey(currencyId) ? Service.Config.PresetCurrencies : Service.Config.CustomCurrencies;
        targetCurrency[currencyId] = newName;
        Configuration.IsUpdated = true;
        Service.Config.Save();

        return true;
    }
}
