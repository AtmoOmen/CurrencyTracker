using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tasks;
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

    private static TaskManager? TaskManager;

    public void Init()
    {
        TaskManager ??= new TaskManager() { TimeLimitMS = int.MaxValue, ShowDebug = false };

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnPremiumSaddleBag);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnPremiumSaddleBag);
    }

    private void OnPremiumSaddleBag(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostSetup:
                windowTitle = GetWindowTitle(args.Addon, 86);
                TaskManager.Enqueue(PSaddleBagScanner);

                break;
            case AddonEvent.PreFinalize:
                TaskManager.Abort();

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

        TaskManager?.Abort();
    }
}
