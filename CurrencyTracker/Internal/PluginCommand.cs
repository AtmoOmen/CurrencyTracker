using System.Text;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Utilities;
using CurrencyTracker.Windows;
using OmenTools.OmenService;
using TinyPinyin;

namespace CurrencyTracker.Internal;

internal class PluginCommand
{
    private const string MAIN_COMMAND = "/ct";
    
    public static void Init()
    {
        var manager = CommandManager.Instance();
        manager.MainCommand = new(MAIN_COMMAND, new(OnMainCommand)
        {
            HelpMessage = $"{Service.Lang.GetText("CommandHelp")}\n{Service.Lang.GetText("CommandHelp1")}"
        });
    }

    public static void Uninit()
    {
        var manager = CommandManager.Instance();

        manager.MainCommand = null;
    }
    
    private static void OnMainCommand(string command, string args)
    {
        var mainWindow = WindowManager.Instance().Get<Main>();
        
        if (string.IsNullOrEmpty(args))
        {
            mainWindow.IsOpen ^= true;
            return;
        }

        var matchingCurrencies = FindMatchingCurrencies(PluginConfig.Instance().AllCurrencies.Values.ToList(), args);
        var matchCount         = matchingCurrencies.Count;

        switch (matchCount)
        {
            case 0:
                DService.Instance().Chat.PrintError(Service.Lang.GetText("CommandHelp3"));
                break;
            case 1:
                var currencyName = matchingCurrencies[0];
                var currencyPair = PluginConfig.Instance().AllCurrencies.FirstOrDefault(x => x.Value == currencyName);
                var currencyID   = currencyPair.Key;

                if (!mainWindow.IsOpen || currencyID != Main.SelectedCurrencyID)
                {
                    mainWindow.IsOpen              = true;
                    Main.SelectedCurrencyID  = currencyID;
                    Main.currentTransactions = Main.ApplyFilters(TransactionsHandler.LoadAllTransactions(currencyID)).ToDisplayTransaction();
                }
                else
                    mainWindow.IsOpen = false;

                break;
            default:
                DService.Instance().Chat.PrintError($"{Service.Lang.GetText("CommandHelp2")}:");
                foreach (var currency in matchingCurrencies) DService.Instance().Chat.PrintError(currency);
                break;
        }

        return;

        static List<string> FindMatchingCurrencies(IReadOnlyCollection<string> currencyList, string partialName)
        {
            partialName = partialName.Normalize(NormalizationForm.FormKC);
            var isSimplified = PluginConfig.Instance().SelectedLanguage == "ChineseSimplified";

            var exactMatch = currencyList.FirstOrDefault(currency => string.Equals(currency, partialName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return [exactMatch];

            return currencyList
                   .Where(currency => IsMatch(currency.Normalize(NormalizationForm.FormKC)))
                   .ToList();

            bool IsMatch(string normalizedCurrency)
            {
                return normalizedCurrency.Contains(partialName, StringComparison.OrdinalIgnoreCase) ||
                       isSimplified && PinyinHelper.GetPinyin(normalizedCurrency, "").Contains(partialName, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
