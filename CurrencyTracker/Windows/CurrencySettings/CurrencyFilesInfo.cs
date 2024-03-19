using System.Collections.Generic;
using System.IO;
using System.Linq;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Transactions;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private uint? lastCurrency;
    private string? lastCurrencyName;
    private CharacterInfo? lastCharacter;
    private string? lastLanguage;
    private Dictionary<string, string> filesInfo = new();

    public void CurrencyFilesInfoUI()
    {
        UiStateWatcherCFI();

        if (filesInfo.Any())
        {
            if (ImGui.CollapsingHeader($"{Service.Lang.GetText("DataFiles")}"))
            {
                foreach (var file in filesInfo)
                {
                    if (ImGui.Selectable($"{file.Key}")) OpenAndSelectFile(file.Value);

                    ImGuiOm.TooltipHover(Path.GetFileName(file.Value));
                }
            }
        }
    }

    public void UiStateWatcherCFI()
    {
        var shouldUpdate = false;

        if (P.CurrentCharacter != null && lastCharacter != P.CurrentCharacter)
        {
            lastCharacter = P.CurrentCharacter;
            shouldUpdate = true;
        }

        if (Main.SelectedCurrencyID != 0 &&
            (lastCurrency != Main.SelectedCurrencyID || lastCurrencyName != Service.Config.AllCurrencies[Main.SelectedCurrencyID]))
        {
            lastCurrency = Main.SelectedCurrencyID;
            lastCurrencyName = Service.Config.AllCurrencies[Main.SelectedCurrencyID];
            shouldUpdate = true;
        }

        if (!string.IsNullOrEmpty(Service.Config.SelectedLanguage) && lastLanguage != Service.Config.SelectedLanguage)
        {
            lastLanguage = Service.Config.SelectedLanguage;
            shouldUpdate = true;
        }

        if (shouldUpdate) GetCurrencyFilesInfoCFI();
    }

    public void GetCurrencyFilesInfoCFI()
    {
        var filePaths = new Dictionary<string, string>();
        var categories = new List<TransactionFileCategory>
        {
            TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag,
            TransactionFileCategory.PremiumSaddleBag
        };
        categories.AddRange(Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID].Keys
                             .Select(_ => TransactionFileCategory.Retainer));

        foreach (var category in categories)
            if (category == TransactionFileCategory.Retainer)
            {
                foreach (var retainer in Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID])
                    AddFilePath(category, retainer.Key);
            }
            else
                AddFilePath(category, 0);

        filesInfo = filePaths;

        return;

        void AddFilePath(TransactionFileCategory category, ulong key)
        {
            var name = GetSelectedViewName(category, key);
            var filePath = TransactionsHandler.GetTransactionFilePath(Main.SelectedCurrencyID, category, key);
            if (!File.Exists(filePath)) return;
            filePaths[name] = filePath;
        }
    }
}
