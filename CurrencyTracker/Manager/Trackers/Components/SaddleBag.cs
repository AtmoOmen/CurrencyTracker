using System.Collections.Generic;
using CurrencyTracker.Infos;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace CurrencyTracker.Manager.Trackers.Components;

public class SaddleBag : ITrackerComponent
{
    public bool Initialized { get; set; }

    public static readonly InventoryType[] SaddleBagInventories =
    [
        InventoryType.SaddleBag1, InventoryType.SaddleBag2
    ];

    internal static Dictionary<uint, long> InventoryItemCount = [];
    private string windowTitle = string.Empty;

    public void Init()
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

                    Tracker.CheckCurrencies(InventoryItemCount.Keys, "", "", 0, 21,
                                                TransactionFileCategory.SaddleBag);
                Tracker.CheckCurrencies(InventoryItemCount.Keys, "", $"({windowTitle})", 0, 21);

                InventoryItemCount.Clear();

                break;
            }
        }
    }

    private static void SaddleBagScanner(IFramework framework)
    {
        InventoryScanner(SaddleBagInventories, ref InventoryItemCount);
    }

    public void Uninit()
    {
        DService.Framework.Update -= SaddleBagScanner;
        DService.AddonLifecycle.UnregisterListener(OnSaddleBag);

        windowTitle = string.Empty;
        InventoryItemCount.Clear();
    }
}
