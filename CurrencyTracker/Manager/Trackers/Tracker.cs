using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private Configuration? C = Plugin.Instance.Configuration;
        private Plugin? P = Plugin.Instance;

        private static readonly ushort[] TriggerChatTypes = new ushort[]
        {
            57, 0, 2110, 2105, 62, 3006, 3001, 2238, 2622
        };

        private static readonly ushort[] IgnoreChatTypes = new ushort[]
        {
            // 战斗相关 Related to Battle
            2091, 2218, 2857, 2729, 2224, 2222, 2859, 2219, 2221, 4139, 4398, 4270, 4397, 4269, 4400, 4777, 10283, 10537, 10409, 18475, 19113, 4783, 10544, 10929, 19632, 4399, 2223, 2225, 4401, 18734, 12331, 4783, 12331, 12585, 12591, 18605, 10922, 18733, 10928, 4778, 13098, 4922, 10410, 9001, 8235, 8752, 9007, 8236, 8746, 8750, 13104, 13102, 12713, 12719, 6959, 2874, 2831, 8749,
            // 新人频道 Novice Network
            27
        };

        public delegate void CurrencyChangedHandler(object sender, EventArgs e);

        public event CurrencyChangedHandler? OnCurrencyChanged;

        private string currentTargetName = string.Empty;

        // ID - Name
        public static Dictionary<uint, string> TerritoryNames = new();

        // ID - Name
        public static Dictionary<uint, string> ItemNames = new();

        public Tracker()
        {
            LoadConstantNames();
            InitCurrencies();

            // Timer Mode has been abandoned
            if (C.TrackMode == 0)
            {
                Plugin.Instance.Configuration.TrackMode = 1;
                Plugin.Instance.Configuration.Save();
            }

            if (C.TrackMode == 1)
            {
                InitializeTracking();
                Service.PluginLog.Debug("Currency Tracker Activated");
            }
        }

        public void InitializeTracking()
        {
            Dispose();

            DebindChatEvent();
            Service.Chat.ChatMessage += OnChatMessage;

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.Framework.Update += OnFrameworkUpdate;

            Service.ClientState.TerritoryChanged -= OnZoneChange;
            Service.ClientState.TerritoryChanged += OnZoneChange;

            if (C.RecordTeleport) InitTeleportCosts();
            if (C.TrackedInDuty) InitDutyRewards();
            if (C.RecordQuestName) InitQuests();
            if (C.RecordMGPSource) InitGoldSacuer();
            if (C.RecordTrade) InitTrade();
            if (C.WaitExComplete) InitExchangeCompletes();
            if (C.RecordTripleTriad) InitTripleTriad();

            UpdateCurrencies();
        }

        internal void UpdateCurrencies()
        {
            if (!Service.ClientState.IsLoggedIn || Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51]) return;

            foreach (var currency in C.PresetCurrencies.Values.Concat(C.CustomCurrencies.Values))
            {
                CheckCurrency(currency, false);
            }
        }

        // 检查货币情况 Check the currency
        private void CheckCurrency(uint currencyID, bool ForceRecording, string currentLocationName = "-1", string currencyNote = "-1", long fCurrencyChange = 0)
        {
            var currencyName = C.PresetCurrencies.FirstOrDefault(x => x.Value == currencyID).Key ?? C.CustomCurrencies.FirstOrDefault(x => x.Value == currencyID).Key;
            if (currencyName.IsNullOrEmpty())
            {
                Service.PluginLog.Error("Invalid Currency!");
                return;
            }

            var currencyAmount = CurrencyInfo.GetCurrencyAmount(currencyID);
            uint locationKey = Service.ClientState.TerritoryType;
            if (currentLocationName == "-1" || currentLocationName == "")
            {
                currentLocationName = TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");
            }
            if (currencyNote == "-1" || currencyNote == "")
            {
                currencyNote = string.Empty;
            }

            var latestTransaction = Transactions.LoadLatestSingleTransaction(currencyName);

            if (latestTransaction != null)
            {
                var currencyChange = fCurrencyChange != 0 ? fCurrencyChange : currencyAmount - latestTransaction.Amount;

                if (currencyChange == 0) return;
                else if (Math.Abs(currencyChange) >= 0)
                {
                    Transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName, currencyNote);
                }
                else return;
                OnTransactionsUpdate(EventArgs.Empty);
                Service.PluginLog.Debug($"{currencyName} has changed, update the transactions data.");
            }
            else if (currencyAmount > 0)
            {
                Transactions.AddTransaction(DateTime.Now, currencyName, currencyAmount, currencyAmount, currentLocationName, currencyNote);
                OnTransactionsUpdate(EventArgs.Empty);
                Service.PluginLog.Debug($"{currencyName} has changed, update the transactions data.");
            }
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
            TerritoryNames = Service.DataManager.GetExcelSheet<TerritoryType>()
                .Where(x => !string.IsNullOrEmpty(x.PlaceName?.Value?.Name?.ToString()))
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.PlaceName?.Value?.Name}");

            ItemNames = Service.DataManager.GetExcelSheet<Item>()
                .Where(x => !string.IsNullOrEmpty(x.Name?.ToString()))
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.Name}");
        }

        private void DebindChatEvent()
        {
            for (var i = 0; i < 5;  i++)
            {
                Service.Chat.ChatMessage -= OnChatMessage;
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

            Service.ClientState.TerritoryChanged -= OnZoneChange;
            DebindChatEvent();
            Service.Framework.Update -= OnFrameworkUpdate;
        }
    }
}
