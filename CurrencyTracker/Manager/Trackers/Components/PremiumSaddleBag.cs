using System.Collections.Generic;
using CurrencyTracker.Helpers.TaskHelper;
using CurrencyTracker.Infos;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace CurrencyTracker.Manager.Trackers.Components;

public class PremiumSaddleBag : ITrackerComponent
{
    public bool Initialized { get; set; }

    public static readonly InventoryType[] PSaddleBagInventories =
    [
        InventoryType.PremiumSaddleBag1, InventoryType.PremiumSaddleBag1
    ];

    internal static Dictionary<uint, long> InventoryItemCount = [];
    private string windowTitle = string.Empty;

    private static TaskHelper? TaskHelper;

    public void Init()
    {
        TaskHelper ??= new TaskHelper() { TimeLimitMS = int.MaxValue };

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnPremiumSaddleBag);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnPremiumSaddleBag);
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

                Tracker.CheckCurrencies(InventoryItemCount.Keys, "", "", 0, 21,
                                                TransactionFileCategory.SaddleBag);
                Tracker.CheckCurrencies(InventoryItemCount.Keys, "", $"({windowTitle})", 0, 21);

                InventoryItemCount.Clear();

                break;
        }
    }

    private static bool? PSaddleBagScanner()
    {
        InventoryScanner(PSaddleBagInventories, ref InventoryItemCount);

        return false;
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(OnPremiumSaddleBag);

        windowTitle = string.Empty;
        InventoryItemCount.Clear();

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
