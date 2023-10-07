using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using KamiLib.Caching;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyTracker.Manager;

public class CurrencyInfo : IDisposable
{
    // 存储一般货币的ID的字典（这里的string非货币名）
    public Dictionary<string, uint> permanentCurrencies = new Dictionary<string, uint>
    {
        { "Gil", 1 },
        { "NonLimitedTomestone", GetNonLimitedTomestoneId() },
        { "LimitedTomestone", GetLimitedTomestoneId() }
    };

    // 传入货币ID后，获取货币于当前语言环境的名称
    public string CurrencyLocalName(uint currencyID)
    {
        if (LuminaCache<Item>.Instance.GetRow(currencyID) is { } currencyItem)
        {
            string CurrencyName = currencyItem.Name.ToDalamudString().TextValue;

            return CurrencyName;
        }
        else return "Unknown";
    }

    // 传入货币ID后，获取货币当前的数量
    public long GetCurrencyAmount(uint currencyID)
    {
        unsafe
        {
            return InventoryManager.Instance()->GetInventoryItemCount(currencyID);
        }
    }

    private static uint GetNonLimitedTomestoneId()
    {
        return LuminaCache<TomestonesItem>.Instance
            .Where(tomestone => tomestone.Tomestones.Row is 2)
            .First()
            .Item.Row;
    }

    private static uint GetLimitedTomestoneId()
    {
        return LuminaCache<TomestonesItem>.Instance
            .Where(tomestone => tomestone.Tomestones.Row is 3)
            .First()
            .Item.Row;
    }

    public void Dispose()
    {
    }
}
