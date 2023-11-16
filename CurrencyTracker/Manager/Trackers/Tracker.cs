using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private enum RecordChangeType
        {
            All,
            Positive,
            Negative
        }

        private static readonly ushort[] TriggerChatTypes = new ushort[]
        {
            57, 0, 2110, 2105, 62, 3006, 3001, 2238, 2622
        };

        private static Dictionary<uint, string> _territoryNames = new();
        private static Dictionary<uint, string> _itemNames = new();
        private static HashSet<string> _itemNamesSet = new();

        public delegate void CurrencyChangedHandler(object sender, EventArgs e);

        public event CurrencyChangedHandler? OnCurrencyChanged;

        private Configuration? C = Plugin.Instance.Configuration;
        private Plugin? P = Plugin.Instance;

        public Tracker()
        {
            LoadConstantNames();
            InitCurrencies();

            InitializeTracking();
            Service.PluginLog.Debug("Currency Tracker Activated");
        }

        public void InitializeTracking()
        {
            Dispose();
            DebindChatEvent();

            Service.Chat.ChatMessage += OnChatMessage;
            Service.Framework.Update += OnFrameworkUpdate;
            Service.ClientState.TerritoryChanged += OnZoneChange;

            if (C.RecordTeleport)
            {
                InitTeleportCosts();
                InitWarpCosts();
            }
            if (C.TrackedInDuty) InitDutyRewards();
            if (C.RecordQuestName) InitQuests();
            if (C.RecordMGPSource) InitGoldSacuer();
            if (C.RecordTrade) InitTrade();
            if (C.WaitExComplete) 
            {
                InitExchangeCompletes();
                InitRepairCosts();
            }
            if (C.RecordTripleTriad) InitTripleTriad();
            if (C.RecordFate) InitFateRewards();
            if (C.RecordIsland) IsInIslandCheck();

            UpdateCurrencies();
        }

        public void UpdateCurrencies()
        {
            if (!Service.ClientState.IsLoggedIn || Flags.BetweenAreas()) return;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value);
            }
        }

        // 检查货币情况 Check the currency
        private bool CheckCurrency(uint currencyID, string locationName = "", string noteContent = "", RecordChangeType recordChangeType = RecordChangeType.All)
        {
            var currencyName = C.AllCurrencies.FirstOrDefault(x => x.Value == currencyID).Key;
            if (currencyName.IsNullOrEmpty()) return false;

            var currencyAmount = CurrencyInfo.GetCurrencyAmount(currencyID);
            uint locationKey = Service.ClientState.TerritoryType;
            locationName = locationName.IsNullOrEmpty()
                ? TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation")
                : locationName;

            var latestTransaction = Transactions.LoadLatestSingleTransaction(currencyName);

            if (latestTransaction != null)
            {
                var currencyChange = currencyAmount - latestTransaction.Amount;

                if (currencyChange != 0 && (recordChangeType == RecordChangeType.All || (recordChangeType == RecordChangeType.Positive && currencyChange > 0) || (recordChangeType == RecordChangeType.Negative && currencyChange < 0)))
                {
                    Transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, locationName, noteContent);
                    OnTransactionsUpdate(EventArgs.Empty);
                    Service.PluginLog.Debug($"{currencyName}({currencyID}) Changed: Update Transactions Data");
                    return true;
                }
            }
            else if (currencyAmount > 0 && (recordChangeType == RecordChangeType.All || recordChangeType == RecordChangeType.Positive))
            {
                Transactions.AddTransaction(DateTime.Now, currencyName, currencyAmount, currencyAmount, locationName, noteContent);
                OnTransactionsUpdate(EventArgs.Empty);
                Service.PluginLog.Debug($"{currencyName}({currencyID}) Changed: Update Transactions Data");
                return true;
            }
            return false;
        }

        private void InitCurrencies()
        {
            foreach (var currency in CurrencyInfo.PresetCurrencies)
            {
                if (!C.PresetCurrencies.ContainsValue(currency.Value))
                {
                    var currencyName = CurrencyInfo.CurrencyLocalName(currency.Value);
                    if (!currencyName.IsNullOrEmpty())
                    {
                        C.PresetCurrencies.Add(currencyName, currency.Value);
                    }
                }
            }

            C.PresetCurrencies = C.PresetCurrencies.Where(kv => CurrencyInfo.PresetCurrencies.ContainsValue(kv.Value))
                                       .ToDictionary(kv => kv.Key, kv => kv.Value);
            C.Save();

            if (C.FisrtOpen)
            {
                foreach (var currencyID in CurrencyInfo.defaultCurrenciesToAdd)
                {
                    var currencyName = CurrencyInfo.CurrencyLocalName(currencyID);

                    if (currencyName.IsNullOrEmpty()) continue;

                    if (!C.CustomCurrencies.ContainsValue(currencyID))
                    {
                        C.CustomCurrencies.Add(currencyName, currencyID);
                    }
                }

                C.FisrtOpen = false;
                C.Save();
            }
        }

        private static void LoadConstantNames()
        {
            _territoryNames = Service.DataManager.GetExcelSheet<TerritoryType>()
                .Where(x => !string.IsNullOrEmpty(x.PlaceName?.Value?.Name?.ToString()))
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.PlaceName?.Value?.Name}");

            _itemNames = Service.DataManager.GetExcelSheet<Item>()
                .Where(x => !string.IsNullOrEmpty(x.Name?.ToString()))
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.Name}");

            _itemNamesSet = new HashSet<string>(_itemNames.Values);
        }

        private unsafe string GetWindowTitle(AddonArgs args, uint windowNodeID, uint[]? textNodeIDs = null)
        {
            textNodeIDs ??= new uint[] { 3, 4 };

            var UI = (AtkUnitBase*)args.Addon;

            if (UI == null || UI->RootNode == null || UI->RootNode->ChildNode == null || UI->UldManager.NodeList == null)
                return string.Empty;

            var windowNode = (AtkComponentBase*)UI->GetComponentNodeById(windowNodeID);
            if (windowNode == null)
                return string.Empty;

            // 国服和韩服特别处理逻辑 For CN and KR Client
            var textNode3 = windowNode->GetTextNodeById(textNodeIDs[0])->GetAsAtkTextNode()->NodeText.ToString();
            var textNode4 = windowNode->GetTextNodeById(textNodeIDs[1])->GetAsAtkTextNode()->NodeText.ToString();

            var windowTitle = !textNode4.IsNullOrEmpty() ? textNode4 : textNode3;

            return windowTitle;
        }

        public static HashSet<string> ItemNamesSet
        {
            get
            {
                return _itemNamesSet;
            }
        }

        public static Dictionary<uint, string> ItemNames
        {
            get
            {
                return _itemNames;
            }
        }

        public static Dictionary<uint, string> TerritoryNames
        {
            get
            {
                return _territoryNames;
            }
        }

        public void Dispose()
        {
            UninitTeleportCosts();
            UninitDutyRewards();
            UninitGoldSacuer();
            UninitTrade();
            UninitExchangeCompletes();
            UninitTripleTriad();
            UninitQuests();
            UninitFateRewards();
            UninitIslandRewards();
            UninitWarpCosts();
            UninitRepairCosts();

            Service.ClientState.TerritoryChanged -= OnZoneChange;
            DebindChatEvent();
            Service.Framework.Update -= OnFrameworkUpdate;
        }
    }
}
