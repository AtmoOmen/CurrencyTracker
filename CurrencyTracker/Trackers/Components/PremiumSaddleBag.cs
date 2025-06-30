using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class PremiumSaddleBag : TrackerComponentBase
{

    public static readonly InventoryType[] PSaddleBagInventories =
    [
        InventoryType.PremiumSaddleBag1, InventoryType.PremiumSaddleBag1
    ];

    internal static Dictionary<uint, long> InventoryItemCount = [];
    private string windowTitle = string.Empty;

    private static TaskHelper? TaskHelper;

    protected override void OnInit()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = int.MaxValue };

        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnPremiumSaddleBag);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnPremiumSaddleBag);
    }

    private void OnPremiumSaddleBag(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostSetup:
                windowTitle = GetWindowTitle(args.Addon, 86);
                TaskHelper.Enqueue(PSaddleBagScanner);

                break;
            case AddonEvent.PreFinalize:
                TaskHelper.Abort();

                TrackerManager.CheckCurrencies(InventoryItemCount.Keys, string.Empty, string.Empty, 0, 21, TransactionFileCategory.SaddleBag);
                TrackerManager.CheckCurrencies(InventoryItemCount.Keys, string.Empty, $"({windowTitle})", 0, 21);

                InventoryItemCount.Clear();

                break;
        }
    }

    private static bool? PSaddleBagScanner()
    {
        InventoryScanner(PSaddleBagInventories, ref InventoryItemCount);

        return false;
    }

    protected override void OnUninit()
    {
        DService.AddonLifecycle.UnregisterListener(OnPremiumSaddleBag);

        windowTitle = string.Empty;
        InventoryItemCount.Clear();

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
