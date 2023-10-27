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

        private CurrencyInfo currencyInfo = new CurrencyInfo();
        private Transactions transactions = new Transactions();

        public static readonly string[] CurrencyType = new string[]
        {
            "Gil",
            "NonLimitedTomestone", "LimitedTomestone"
        };

        private static readonly ushort[] TriggerChatTypes = new ushort[]
        {
            57, 0, 2110, 2105, 62, 3006, 3001, 2238
        };

        private static readonly ushort[] IgnoreChatTypes = new ushort[]
        {
            // 战斗相关 Related to Battle
            2091, 2218, 2857, 2729, 2224, 2222, 2859, 2219, 2221, 4139, 4398, 4270, 4397, 4269, 4400, 4777, 10283, 10537, 10409, 18475, 19113, 4783, 10544, 10929, 19632, 4399, 2223, 2225, 4401, 18734, 12331, 4783, 12331, 12585, 12591, 18605, 10922, 18733, 10928, 4778, 13098, 4922, 10410, 9001, 8235, 8752, 9007, 8236, 8746, 8750, 13104, 13102, 12713, 12719, 6959, 2874, 2831, 8749,
            // 新人频道 Novice Network
            27
        };

        private static readonly string[] IgnoreChatContent = new string[]
        {
            "海雾村", "薰衣草苗圃", "高脚孤丘", "白银乡", "穹顶皓天",
            "Mist", "Lavender Beds", "Goblet", "Shirogane", "Empyreum",
            "ミスト・ヴィレッジ", "ラベンダーベッド", "ゴブレットビュート", "シロガネ", "エンピレアム",
            "Dorf des Nebels","Lavendelbeete","Kelchkuppe","Shirogane","Empyreum",
            "Brumée","Lavandière","La Coupe","Shirogane","Empyrée",
        };

        public delegate void CurrencyChangedHandler(object sender, EventArgs e);

        public event CurrencyChangedHandler? OnCurrencyChanged;

        private Dictionary<string, Dictionary<string, int>> minTrackValue = new();
        public static string NonLimitedTomestoneName = string.Empty;
        public static string LimitedTomestoneName = string.Empty;
        private string currentTargetName = string.Empty;

        // ID - Name
        public static Dictionary<uint, string> TerritoryNames = new();

        // ID - Name
        public static Dictionary<uint, string> ItemNames = new();

        public Tracker()
        {
            LoadConstantNames();
            LoadMinTrackValue();
            DealWithCurrencies();

            // Timer Mode has been abandoned
            if (C.TrackMode == 0)
            {
                Plugin.Instance.Configuration.TrackMode = 1;
                Plugin.Instance.Configuration.Save();
            }

            if (C.TrackMode == 1)
            {
                InitializeChatTracking();
                Service.PluginLog.Debug("Currency Tracker Activated");
            }

            if (C.RecordTeleport) InitTeleportCosts();
            if (C.TrackedInDuty) InitDutyRewards();
            if (C.RecordQuestName) InitQuests();
            if (C.RecordMGPSource) InitGoldSacuer();
            if (C.RecordTrade) InitTrade();
        }

        private void LoadMinTrackValue()
        {
            if (Plugin.Instance.Configuration.MinTrackValueDic != null && Plugin.Instance.Configuration.MinTrackValueDic.ContainsKey("InDuty") && Plugin.Instance.Configuration.MinTrackValueDic.ContainsKey("OutOfDuty"))
            {
                minTrackValue = Plugin.Instance.Configuration.MinTrackValueDic;
            }
        }

        public void InitializeChatTracking()
        {
            Service.Chat.ChatMessage -= OnChatMessage;
            Service.Chat.ChatMessage += OnChatMessage;

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.Framework.Update += OnFrameworkUpdate;

            Service.ClientState.TerritoryChanged -= OnZoneChange;
            Service.ClientState.TerritoryChanged += OnZoneChange;

            Service.Condition.ConditionChange -= OnConditionChanged;
            Service.Condition.ConditionChange += OnConditionChanged;

            UpdateCurrenciesByChat();
        }

        internal void UpdateCurrenciesByChat()
        {
            if (!Service.ClientState.IsLoggedIn) return;

            if (!Plugin.Instance.Configuration.TrackedInDuty)
            {
                if (IsBoundByDuty()) return;
            }

            if (Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51])
            {
                return;
            }

            foreach (var currency in CurrencyType)
            {
                if (CurrencyInfo.presetCurrencies.TryGetValue(currency, out var currencyID))
                {
                    CheckCurrency(currencyID, false);
                }
            }
            foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
            {
                if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out var currencyID))
                {
                    CheckCurrency(currencyID, false);
                }
            }
        }

        // 检查货币情况 Check the currency
        private void CheckCurrency(uint currencyID, bool ForceRecording, string currentLocationName = "-1", string currencyNote = "-1", long fCurrencyChange = 0)
        {
            var currencyName = currencyInfo.CurrencyLocalName(currencyID);
            if (currencyName.IsNullOrEmpty())
            {
                Service.PluginLog.Error("Invalid Currency!");
                return;
            }
            var latestTransaction = transactions.LoadLatestSingleTransaction(currencyName);

            var currencyAmount = currencyInfo.GetCurrencyAmount(currencyID);
            uint locationKey = Service.ClientState.TerritoryType;

            if (currentLocationName == "-1" || currentLocationName == "")
            {
                currentLocationName = TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");
            }

            if (currencyNote == "-1" || currencyNote == "")
            {
                currencyNote = string.Empty;
            }

            minTrackValue = Plugin.Instance.Configuration.MinTrackValueDic;

            if (latestTransaction != null)
            {
                var currencyChange = currencyAmount - latestTransaction.Amount;
                if (fCurrencyChange != 0)
                {
                    currencyChange = fCurrencyChange;
                }
                if (currencyChange == 0)
                {
                    return;
                }
                else
                {
                    if (IsBoundByDuty())
                    {
                        var inDutyMinTrackValue = minTrackValue["InDuty"];
                        if (inDutyMinTrackValue.ContainsKey(currencyName))
                        {
                            var currencyThreshold = inDutyMinTrackValue[currencyName];
                            if (!ForceRecording)
                            {
                                if (Math.Abs(currencyChange) >= currencyThreshold)
                                {
                                    transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName, currencyNote);
                                }
                                else return;
                            }
                            else
                            {
                                if (Math.Abs(currencyChange) >= 0)
                                {
                                    transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName, currencyNote);
                                }
                                else return;
                            }
                        }
                    }
                    else
                    {
                        var outOfDutyMinTrackValue = minTrackValue["OutOfDuty"];
                        if (outOfDutyMinTrackValue.ContainsKey(currencyName))
                        {
                            var currencyThreshold = outOfDutyMinTrackValue[currencyName];
                            if (Math.Abs(currencyChange) >= currencyThreshold)
                            {
                                transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName, currencyNote);
                            }
                            else return;
                        }
                    }
                }
                OnTransactionsUpdate(EventArgs.Empty);
                Service.PluginLog.Debug($"{currencyName} has changed, update the transactions data.");
            }
            else
            {
                if (currencyAmount > 0)
                {
                    transactions.AddTransaction(DateTime.Now, currencyName, currencyAmount, currencyAmount, currentLocationName, currencyNote);
                    OnTransactionsUpdate(EventArgs.Empty);
                    Service.PluginLog.Debug($"{currencyName} has changed, update the transactions data.");
                }
            }
        }

        private void DealWithCurrencies()
        {
            if (CurrencyInfo.presetCurrencies.TryGetValue("NonLimitedTomestone", out var NonLimitedTomestoneID))
            {
                string? currencyName = currencyInfo.CurrencyLocalName(NonLimitedTomestoneID);
                if (currencyName != "Unknown" && currencyName != null)
                {
                    NonLimitedTomestoneName = currencyName;
                }
            }

            if (CurrencyInfo.presetCurrencies.TryGetValue("LimitedTomestone", out var LimitedTomestoneID))
            {
                string? currencyName = currencyInfo.CurrencyLocalName(LimitedTomestoneID);
                if (currencyName != "Unknown" && currencyName != null)
                {
                    LimitedTomestoneName = currencyName;
                }
            }

            if (Plugin.Instance.Configuration.FisrtOpen)
            {
                foreach (var currencyID in CurrencyInfo.defaultCurrenciesToAdd)
                {
                    string? currencyName = currencyInfo.CurrencyLocalName(currencyID);

                    if (currencyName.IsNullOrEmpty()) continue;

                    if (!Plugin.Instance.Configuration.CustomCurrencyType.Contains(currencyName) && !Plugin.Instance.Configuration.CustomCurrencies.ContainsKey(currencyName))
                    {
                        Plugin.Instance.Configuration.CustomCurrencyType.Add(currencyName);
                        Plugin.Instance.Configuration.CustomCurrencies.Add(currencyName, currencyID);
                    }

                    if (!Plugin.Instance.Configuration.MinTrackValueDic["InDuty"].ContainsKey(currencyName) && !Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"].ContainsKey(currencyName))
                    {
                        Plugin.Instance.Configuration.MinTrackValueDic["InDuty"].Add(currencyName, 0);
                        Plugin.Instance.Configuration.MinTrackValueDic["OutOfDuty"].Add(currencyName, 0);
                    }
                }

                Plugin.Instance.Configuration.FisrtOpen = false;
                Plugin.Instance.Configuration.Save();
            }
        }

        private void LoadConstantNames()
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

        public void Dispose()
        {
            UninitTeleportCosts();
            UninitDutyRewards();
            UninitGoldSacuer();
            UninitTrade();

            Service.ClientState.TerritoryChanged -= OnZoneChange;
            Service.Chat.ChatMessage -= OnChatMessage;
            Service.Framework.Update -= OnFrameworkUpdate;
            Service.Condition.ConditionChange -= OnConditionChanged;
        }
    }
}
