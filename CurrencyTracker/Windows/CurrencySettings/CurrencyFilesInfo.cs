using System.Collections.Generic;
using System.IO;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Utilities;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    public static void CurrencyFilesInfoUI()
    {
        if (ImGui.CollapsingHeader($"{Service.Lang.GetText("DataFiles")}"))
        {
            var filesInfo = GetCurrencyFilesInfoCFI();
            foreach (var file in filesInfo)
            {
                if (ImGui.Selectable($"{file.Key}")) 
                    FileHelper.OpenAndSelectFile(file.Value);

                ImGuiOm.TooltipHover(Path.GetFileName(file.Value));
            }
        }
    }

    public static Dictionary<string, string> GetCurrencyFilesInfoCFI()
    {
        var filePaths = new Dictionary<string, string>();
        var retainers = Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID];

        foreach (var retainer in retainers.Keys)
            AddFilePath(TransactionFileCategory.Retainer, retainer);

        AddFilePath(TransactionFileCategory.Inventory, 0);
        AddFilePath(TransactionFileCategory.SaddleBag, 0);
        AddFilePath(TransactionFileCategory.PremiumSaddleBag, 0);

        return filePaths;

        void AddFilePath(TransactionFileCategory category, ulong key)
        {
            var name     = category.GetSelectedViewName(key);
            var filePath = TransactionsHandler.GetTransactionFilePath(Main.SelectedCurrencyID, category, key);

            if (!File.Exists(filePath)) return;

            lock (filePaths)
            {
                filePaths[name] = filePath;
            }
        }
    }

}
