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
    private static string windowTitle = string.Empty;

    protected override void OnInit()
    {
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnSaddleBag);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnSaddleBag);
    }

    private static unsafe void OnSaddleBag(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostSetup:
            {
                windowTitle                          =  args.Addon.ToStruct()->GetWindowTitle();
                DService.Instance().Framework.Update += SaddleBagScanner;

                break;
            }
            case AddonEvent.PreFinalize:
            {
                DService.Instance().Framework.Update -= SaddleBagScanner;
                DService.Instance().Framework.Update -= SaddleBagScanner;

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
        DService.Instance().Framework.Update -= SaddleBagScanner;
        DService.Instance().AddonLifecycle.UnregisterListener(OnSaddleBag);

        windowTitle = string.Empty;
        InventoryItemCount.Clear();
    }
}
