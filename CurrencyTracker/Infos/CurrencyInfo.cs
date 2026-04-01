using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Internal;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Trackers.Components;
using CurrencyTracker.Windows;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using OmenTools.Interop.Game.Lumina;

namespace CurrencyTracker.Infos;

public static class CurrencyInfo
{
    public static readonly uint[] DefaultCustomCurrencies =
    [
        20, 21, 22, 25, 27, 28, 29, 10307, 26807, 28063, 33913, 33914, 36656, 41784, 41785
    ];

    public static readonly uint[] PresetCurrencies =
    [
        1, GetSpecialTomestoneID(2), GetSpecialTomestoneID(3)
    ];

    public static readonly Dictionary<ulong, Dictionary<uint, long>> CurrencyAmountCache = [];

    public static void Init() =>
        TrackerManager.CurrencyChanged += OnCurrencyChanged;

    private static void OnCurrencyChanged(uint currencyId, TransactionFileCategory category, ulong id)
    {
        if (!CurrencyAmountCache.ContainsKey(P.CurrentCharacter.ContentID))
            CurrencyAmountCache.Add(P.CurrentCharacter.ContentID, []);
        CurrencyAmountCache[P.CurrentCharacter.ContentID][currencyId] = GetCharacterCurrencyAmount(currencyId, P.CurrentCharacter, true);
    }

    public static string GetName(uint currencyID)
    {
        return PluginConfig.Instance().AllCurrencies.TryGetValue(currencyID, out var currencyName)
                   ? currencyName
                   : GetLocalName(currencyID);
    }

    public static string GetLocalName(uint currencyID) =>
        LuminaWrapper.GetItemName(currencyID);

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

    public static long GetCharacterCurrencyAmount(uint currencyID, CharacterInfo character, bool isOverride = false)
    {
        if (!CurrencyAmountCache.TryGetValue(character.ContentID, out var characterCache))
        {
            characterCache                           = [];
            CurrencyAmountCache[character.ContentID] = characterCache;
        }

        if (!isOverride)
            if (characterCache.TryGetValue(currencyID, out var characterCurrencyAmount))
                return characterCurrencyAmount;

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

        if (PluginConfig.Instance().CharacterRetainers.TryGetValue(character.ContentID, out var value))
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

    public static Dictionary<TransactionFileCategoryInfo, long> GetCharacterCurrencyAmountDictionary
    (
        uint          currencyID,
        CharacterInfo character
    )
    {
        var amountDic = new Dictionary<TransactionFileCategoryInfo, long>();

        foreach (var category in new[]
                 {
                     TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag,
                     TransactionFileCategory.PremiumSaddleBag
                 }) AddCurrencyAmountToDictionary(currencyID, character, category, 0, amountDic);

        if (PluginConfig.Instance().CharacterRetainers.TryGetValue(character.ContentID, out var retainers))
        {
            foreach (var retainer in retainers)
            {
                AddCurrencyAmountToDictionary
                (
                    currencyID,
                    character,
                    TransactionFileCategory.Retainer,
                    retainer.Key,
                    amountDic
                );
            }
        }

        return amountDic;

        void AddCurrencyAmountToDictionary
        (
            uint                                           currencyID,
            CharacterInfo                                  character,
            TransactionFileCategory                        category,
            ulong                                          ID,
            IDictionary<TransactionFileCategoryInfo, long> dictionary
        )
        {
            var currencyAmount = GetCurrencyAmountFromFile(currencyID, character, category, ID);
            var key            = new TransactionFileCategoryInfo(category, ID);
            dictionary[key] = currencyAmount ?? 0;
        }
    }

    public static long? GetCurrencyAmountFromFile
    (
        uint                    currencyID,
        CharacterInfo           character,
        TransactionFileCategory category = 0,
        ulong                   ID       = 0
    )
    {
        var latestTransaction = TransactionsHandler.LoadLatestSingleTransaction(currencyID, character, category, ID);

        return latestTransaction?.Amount;
    }

    private static uint GetSpecialTomestoneID(int row) =>
        LuminaGetter.Get<TomestonesItem>()
                    .FirstOrNull(x => x.Tomestones.RowId == row)?
                    .Item.RowId ??
        0;

    public static IDalamudTextureWrap GetIcon(uint currencyID) =>
        DService.Instance().Texture.GetFromGameIcon(new(LuminaGetter.GetRow<Item>(currencyID).GetValueOrDefault().Icon)).GetWrapOrEmpty();

    public static void RenameCurrency(uint currencyID, string editedCurrencyName)
    {
        if (PluginConfig.Instance().AllCurrencies.ContainsValue(editedCurrencyName))
        {
            DService.Instance().Chat.PrintError(Service.Lang.GetText("CurrencyRenameHelp1"));
            return;
        }

        if (!PluginConfig.Instance().TryRenameCurrency(currencyID, editedCurrencyName)) return;

        Main.UpdateTransactions(currencyID, Main.currentView, Main.currentViewID);
        Main.ReloadOrderedOptions();
        PluginConfig.Instance().Save();
    }

    public static void Uninit() =>
        TrackerManager.CurrencyChanged -= OnCurrencyChanged;
}
