using System.Collections.Generic;
using System.IO;
using System.Linq;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers;
using CurrencyTracker.Manager.Trackers.Components;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Windows;
using Dalamud.Interface.Internal;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets2;

namespace CurrencyTracker.Manager.Infos;

public static class CurrencyInfo
{
    public static readonly uint[] DefaultCustomCurrencies =
    [
        20, 21, 22, 25, 27, 28, 29, 10307, 25199, 25200, 26807, 28063, 33913, 33914, 36656
    ];
    public static readonly uint[] PresetCurrencies =
    [
        1, GetSpecialTomestoneId(2), GetSpecialTomestoneId(3)
    ];

    public static readonly Dictionary<ulong, Dictionary<uint, long>> CurrencyAmountCache = [];

    public static void Init()
    {
        Tracker.CurrencyChanged += OnCurrencyChanged;
    }

    private static void OnCurrencyChanged(uint currencyId, TransactionFileCategory category, ulong id)
    {
        CurrencyAmountCache[P.CurrentCharacter.ContentID][currencyId] = GetCharacterCurrencyAmount(currencyId, P.CurrentCharacter);
    }

    public static string GetCurrencyName(uint currencyID)
    {
        return Service.Config.AllCurrencies.TryGetValue(currencyID, out var currencyName)
                   ? currencyName
                   : GetCurrencyLocalName(currencyID);
    }

    public static string GetCurrencyLocalName(uint currencyID)
    {
        if (LuminaCache<Item>.Instance.GetRow(currencyID) is { } currencyItem)
        {
            var currencyName = currencyItem.Name.ToDalamudString().TextValue;

            return currencyName;
        }

        return "Unknown";
    }

    public static unsafe long GetCurrencyAmount(uint currencyID, TransactionFileCategory category = 0, ulong ID = 0)
    {
        return category switch
        {
            TransactionFileCategory.Inventory => InventoryManager.Instance()->GetInventoryItemCount(currencyID),
            TransactionFileCategory.SaddleBag =>
                SaddleBag.InventoryItemCount.GetValueOrDefault(currencyID, 0),
            TransactionFileCategory.PremiumSaddleBag =>
                PremiumSaddleBag.InventoryItemCount.GetValueOrDefault(currencyID, 0),
            TransactionFileCategory.Retainer =>
                Retainer.InventoryItemCount.TryGetValue(ID, out var retainer) &&
                retainer.TryGetValue(currencyID, out var retainerAmount)
                    ? retainerAmount
                    : 0,

            _ => 0
        };
    }

    public static long GetCharacterCurrencyAmount(uint currencyID, CharacterInfo character)
    {
        if (!CurrencyAmountCache.TryGetValue(character.ContentID, out var characterCache))
        {
            characterCache = [];
            CurrencyAmountCache[character.ContentID] = characterCache;
        }

        if (characterCache.TryGetValue(currencyID, out var characterCurrencyAmount))
        {
            return characterCurrencyAmount;
        }

        var amount = 0L;
        var categories = new[]
        {
            TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag,
            TransactionFileCategory.PremiumSaddleBag
        };

        foreach (var category in categories)
        {
            var currencyAmount = GetCurrencyAmountFromFile(currencyID, character, category);
            amount += currencyAmount ?? 0;
        }

        if (Service.Config.CharacterRetainers.TryGetValue(character.ContentID, out var value))
        {
            foreach (var retainer in value)
            {
                var currencyAmount =
                    GetCurrencyAmountFromFile(currencyID, character, TransactionFileCategory.Retainer, retainer.Key);
                amount += currencyAmount ?? 0;
            }
        }

        characterCache[currencyID] = amount;
        return amount;
    }

    public static Dictionary<TransactionFileCategoryInfo, long> GetCharacterCurrencyAmountDictionary(
        uint currencyID, CharacterInfo character)
    {
        var amountDic = new Dictionary<TransactionFileCategoryInfo, long>();

        foreach (var category in new[]
                 {
                     TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag,
                     TransactionFileCategory.PremiumSaddleBag
                 }) AddCurrencyAmountToDictionary(currencyID, character, category, 0, amountDic);

        if (Service.Config.CharacterRetainers.TryGetValue(character.ContentID, out var retainers))
        {
            foreach (var retainer in retainers)
                AddCurrencyAmountToDictionary(currencyID, character, TransactionFileCategory.Retainer, retainer.Key,
                                              amountDic);
        }

        return amountDic;

        void AddCurrencyAmountToDictionary(
            uint currencyID, CharacterInfo character, TransactionFileCategory category, ulong ID,
            IDictionary<TransactionFileCategoryInfo, long> dictionary)
        {
            var currencyAmount = GetCurrencyAmountFromFile(currencyID, character, category, ID);
            var key = new TransactionFileCategoryInfo { Category = category, Id = ID };
            dictionary[key] = currencyAmount ?? 0;
        }
    }


    public static long? GetCurrencyAmountFromFile(
        uint currencyID, CharacterInfo character, TransactionFileCategory category = 0, ulong ID = 0)
    {
        var latestTransaction = TransactionsHandler.LoadLatestSingleTransaction(currencyID, character, category, ID);

        return latestTransaction?.Amount;
    }

    private static uint GetSpecialTomestoneId(int row)
    {
        return LuminaCache<TomestonesItem>.Instance
                                          .First(tomestone => tomestone.Tomestones.Row == row)
                                          .Item.Row;
    }

    public static IDalamudTextureWrap? GetIcon(uint currencyID)
    {
        if (Service.DataManager.GetExcelSheet<Item>()!.GetRow(currencyID) is { Icon: var iconId })
            return Service.TextureProvider.GetIcon(iconId);

        Service.Log.Warning($"Failed to get {currencyID} {GetCurrencyLocalName(currencyID)} icon");
        return null;
    }

    public static void RenameCurrency(uint currencyID, string editedCurrencyName)
    {
        var (isFilesExisted, filePaths) = ConstructFilePaths(currencyID, editedCurrencyName);

        if (Service.Config.AllCurrencies.ContainsValue(editedCurrencyName) || !isFilesExisted)
        {
            Service.Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp1"));
            return;
        }

        if (UpdateCurrencyName(currencyID, editedCurrencyName))
        {
            foreach (var (sourcePath, targetPath) in filePaths)
            {
                Service.Log.Debug($"Moving file from {sourcePath} to {targetPath}");
                File.Move(sourcePath, targetPath);
            }

            Main.UpdateTransactions(currencyID, Main.currentView, Main.currentViewID);
            Main.ReloadOrderedOptions();

            Service.Config.Save();
        }

        bool UpdateCurrencyName(uint currencyId, string newName)
        {
            if (!Service.Config.PresetCurrencies.ContainsKey(currencyId) &&
                !Service.Config.CustomCurrencies.ContainsKey(currencyId)) return false;

            var targetCurrency = Service.Config.PresetCurrencies.ContainsKey(currencyId)
                                     ? Service.Config.PresetCurrencies
                                     : Service.Config.CustomCurrencies;
            targetCurrency[currencyId] = newName;
            Configuration.IsUpdated = true;
            Service.Config.Save();

            return true;
        }

        (bool, Dictionary<string, string>) ConstructFilePaths(uint currencyID, string editedCurrencyName)
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
                        AddFilePath(filePaths, category, retainer.Key, editedCurrencyName);
                }
                else
                    AddFilePath(filePaths, category, 0, editedCurrencyName);

            return (filePaths.Values.All(path => !File.Exists(path)), filePaths);

            void AddFilePath(
                IDictionary<string, string> filePaths, TransactionFileCategory category, ulong key,
                string editedCurrencyName)
            {
                var editedFilePath = Path.Join(P.PlayerDataFolder,
                                               $"{editedCurrencyName}{TransactionsHandler.GetTransactionFileSuffix(category, key)}.txt");
                var originalFilePath = TransactionsHandler.GetTransactionFilePath(currencyID, category, key);
                if (!File.Exists(originalFilePath)) return;
                filePaths[originalFilePath] = editedFilePath;
            }
        }
    }

    public static void Uninit()
    {
        Tracker.CurrencyChanged -= OnCurrencyChanged;
    }
}
