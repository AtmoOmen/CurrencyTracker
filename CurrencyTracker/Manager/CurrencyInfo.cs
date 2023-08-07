using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using KamiLib.Caching;
using Lumina.Excel.GeneratedSheets;

namespace CurrencyTracker.Manager;

public class CurrencyInfo : IDisposable
{
    // 存储一般货币的ID的字典（这里的string非货币名）
    public Dictionary<string, uint> permanentCurrencies = new Dictionary<string, uint>
    {
        { "StormSeal", 20 },
        { "SerpentSeal", 21 },
        { "FlameSeal", 22 },
        { "WolfMark", 25 },
        { "TrophyCrystal", 36656 },
        { "AlliedSeal", 27 },
        { "CenturioSeal", 10307 },
        { "SackOfNut", 26533 },
        { "BicolorGemstone", 26807 },
        { "Poetic", 28 },
        { "WhiteCrafterScript", 25199 },
        { "WhiteGatherersScript", 25200 },
        { "PurpleCrafterScript", 33913 },
        { "PurpleGatherersScript", 33914 },
        { "SkybuildersScript", 28063 },
        { "Gil", 1 },
        { "MGP", 29 },
        { "NonLimitedTomestone", GetNonLimitedTomestoneId() },
        { "LimitedTomestone", GetLimitedTomestoneId() }
    };

    // 传入货币ID后，获取货币于当前语言环境的名称
    public string CurrencyLocalName(uint currencyID)
    {
        if (LuminaCache<Item>.Instance.GetRow(currencyID) is { } currencyItem)
        {
            // 获取物品名称
            string CurrencyName = currencyItem.Name.ToDalamudString().TextValue;
            return CurrencyName;
        }
        return "未知货币";
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




