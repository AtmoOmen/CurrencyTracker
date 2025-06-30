using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace CurrencyTracker.Manager.Trackers.Components;

public class SaddleBag : TrackerComponentBase
{

    public static readonly InventoryType[] SaddleBagInventories =
    [
        InventoryType.SaddleBag1, InventoryType.SaddleBag2
    ];

    internal static Dictionary<uint, long> InventoryItemCount = [];
    private string windowTitle = string.Empty;

    protected override void OnInit()
    {
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnSaddleBag);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnSaddleBag);
    }

    private void OnSaddleBag(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostSetup:
            {
                windowTitle = GetWindowTitle(args.Addon, 86);
                DService.Framework.Update += SaddleBagScanner;

                break;
            }
            case AddonEvent.PreFinalize:
            {
                DService.Framework.Update -= SaddleBagScanner;
                DService.Framework.Update -= SaddleBagScanner;

                    TrackerManager.CheckCurrencies(InventoryItemCount.Keys, "", "", 0, 21,
                                                TransactionFileCategory.SaddleBag);
                TrackerManager.CheckCurrencies(InventoryItemCount.Keys, "", $"({windowTitle})", 0, 21);

                InventoryItemCount.Clear();

                break;
            }
        }
    }

    private static void SaddleBagScanner(IFramework framework)
    {
        InventoryScanner(SaddleBagInventories, ref InventoryItemCount);
    }

    protected override void OnUninit()
    {
        DService.Framework.Update -= SaddleBagScanner;
        DService.AddonLifecycle.UnregisterListener(OnSaddleBag);

        windowTitle = string.Empty;
        InventoryItemCount.Clear();
    }
}
