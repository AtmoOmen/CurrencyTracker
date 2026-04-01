using System.Collections.Generic;
using System.IO;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Utilities;

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
        var databasePath = TransactionsHandler.GetDatabasePath();
        if (!File.Exists(databasePath))
            return [];

        return new Dictionary<string, string>
        {
            ["SQLite 数据库"] = databasePath
        };
    }
}
