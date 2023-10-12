using CurrencyTracker.Windows;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CurrencyTracker.Manager
{
# pragma warning disable CS8602

    public class Tracker : IDisposable
    {
        public static readonly string[] CurrencyType = new string[]
        {
            "Gil",
            "NonLimitedTomestone", "LimitedTomestone"
        };

        private static readonly string[] DutyEndStrings = new[] { "任务结束了", "has ended", "の攻略を終了した", "wurde beendet", "prend fin" };

        private static readonly ushort[] TriggerChatTypes = new ushort[]
        {
            57, 0, 2110, 2105, 62, 3006, 3001
        };

        private static readonly ushort[] IgnoreChatTypes = new ushort[]
        {
            // 战斗相关 Related to Battle
            2091, 2218, 2857, 2729, 2224, 2222, 2859, 2219, 2221, 4139, 4398, 4270, 4397, 4269, 4400, 4777, 10283, 10537, 10409, 18475, 19113, 4783, 10544, 10929, 19632, 4399, 2223, 2225, 4401, 18734, 12331, 4783, 12331, 12585, 12591, 18605, 10922, 18733, 10928, 4778, 13098, 4922, 10410, 9001, 8235, 8752, 9007, 8236, 8746, 8750, 13104, 13102, 12713, 12719, 6959, 2874, 2831, 8749,
            // 新人频道 Novice Network
            27
        };

        private static bool IsBoundByDuty()
        {
            return Service.Condition[ConditionFlag.BoundByDuty] ||
                   Service.Condition[ConditionFlag.BoundByDuty56] ||
                   Service.Condition[ConditionFlag.BoundByDuty95];
        }

        private CurrencyInfo currencyInfo = new CurrencyInfo();
        private Transactions transactions = new Transactions();
        internal static LanguageManager Lang = new LanguageManager(Plugin.Instance.Configuration.SelectedLanguage);

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public delegate void CurrencyChangedHandler(object sender, EventArgs e);

        public event CurrencyChangedHandler? OnCurrencyChanged;

        private Dictionary<string, Dictionary<string, int>> minTrackValue = new();
        private string dutyLocationName = string.Empty;
        private int timerInterval = 500;
        public string NonLimitedTomestoneName = string.Empty;
        public string LimitedTomestoneName = string.Empty;

        public string previousLocationName = string.Empty;
        public string currentLocationName = string.Empty;
        public uint teleportCost = 0;

        public virtual void OnTransactionsUpdate(EventArgs e)
        {
            OnCurrencyChanged?.Invoke(this, e);
        }

        public Tracker()
        {
            if (Plugin.Instance.Configuration.TrackMode == 0)
            {
                InitializeTimerTracking();
                Service.PluginLog.Debug("Timer Mode Activated");
            }
            else if (Plugin.Instance.Configuration.TrackMode == 1)
            {
                InitializeChatTracking();
                Service.PluginLog.Debug("Chat Mode Activated");
            }

            LoadMinTrackValue();

            Service.DutyState.DutyStarted += isDutyStarted;
            Service.ClientState.TerritoryChanged += OnZoneChange;

            Service.ClientState.EnterPvP += isPvPEntered;
            Service.ClientState.LeavePvP += isPvPLeft;

            DealWithCurrencies();

            previousLocationName = currentLocationName = Plugin.Instance.TerritoryNames.TryGetValue(Service.ClientState.TerritoryType, out var currentLocation) ? currentLocation : Lang.GetText("UnknownLocation");
            Service.PluginLog.Debug($"当前区域: {previousLocationName}");
        }

        public void ChangeTracker()
        {
            if (Plugin.Instance.Configuration.TrackMode == 0)
            {
                InitializeTimerTracking();
                Service.PluginLog.Debug("Timer Mode Activated");
            }
            else if (Plugin.Instance.Configuration.TrackMode == 1)
            {
                InitializeChatTracking();
                Service.PluginLog.Debug("Chat Mode Activated");
            }
        }

        private void LoadMinTrackValue()
        {
            if (Plugin.Instance.Configuration.MinTrackValueDic != null && Plugin.Instance.Configuration.MinTrackValueDic.ContainsKey("InDuty") && Plugin.Instance.Configuration.MinTrackValueDic.ContainsKey("OutOfDuty"))
            {
                minTrackValue = Plugin.Instance.Configuration.MinTrackValueDic;
            }
        }

        private void InitializeTimerTracking()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            Service.Chat.ChatMessage -= OnChatMessage;

            UpdateCurrenciesTimer();
        }

        public void InitializeChatTracking()
        {
            Service.Chat.ChatMessage -= OnChatMessage;

            Service.Chat.ChatMessage += OnChatMessage;

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            UpdateCurrenciesByChat();
        }

        private void UpdateCurrenciesByChat()
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
                if (CurrencyInfo.presetCurrencies.TryGetValue(currency, out uint currencyID))
                {
                    string? currencyName = currencyInfo.CurrencyLocalName(currencyID);
                    if (currencyName != "Unknown" && currencyName != null)
                    {
                        CheckCurrency(currencyName, currencyID, false);
                    }
                }
            }
            foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
            {
                if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out uint currencyID))
                {
                    if (currency != "Unknown" && currency != null)
                    {
                        CheckCurrency(currency, currencyID, false);
                    }
                }
            }
        }

        private void UpdateCurrenciesTimer()
        {
            timerInterval = Plugin.Instance.Configuration.TimerInterval;

            Service.Framework.RunOnTick(UpdateCurrenciesTimer, TimeSpan.FromMilliseconds(timerInterval), 0, cancellationTokenSource.Token);

            if (!Service.ClientState.IsLoggedIn) return;

            if (!Plugin.Instance.Configuration.TrackedInDuty)
            {
                if (IsBoundByDuty()) return;
            }

            if (Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51]) return;

            foreach (var currency in CurrencyType)
            {
                if (CurrencyInfo.presetCurrencies.TryGetValue(currency, out uint currencyID))
                {
                    string? currencyName = currencyInfo.CurrencyLocalName(currencyID);
                    if (currencyName != "Unknown" && currencyName != null)
                    {
                        CheckCurrency(currencyName, currencyID, false);
                    }
                }
            }
            foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
            {
                if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out uint currencyID))
                {
                    if (currency != "Unknown" && currency != null)
                    {
                        CheckCurrency(currency, currencyID, false);
                    }
                }
            }
        }

        private void CheckCurrency(string currencyName, uint currencyID, bool isDutyEnd, string currentLocationName = "-1", string currencyNote = "-1")
        {
            TransactionsConvertor? latestTransaction = transactions.LoadLatestSingleTransaction(currencyName);

            long currencyAmount = currencyInfo.GetCurrencyAmount(currencyID);
            uint locationKey = Service.ClientState.TerritoryType;

            if (currentLocationName == "-1" || currentLocationName == "")
            {
                currentLocationName = Plugin.Instance.TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : Lang.GetText("UnknownLocation");
            }

            if (currencyNote == "-1" || currencyNote == "")
            {
                currencyNote = string.Empty;
            }

            minTrackValue = Plugin.Instance.Configuration.MinTrackValueDic;

            if (latestTransaction != null)
            {
                long currencyChange = currencyAmount - latestTransaction.Amount;
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
                            if (!isDutyEnd)
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
            }
            else
            {
                transactions.AddTransaction(DateTime.Now, currencyName, currencyAmount, currencyAmount, currentLocationName, currencyNote);
                OnTransactionsUpdate(EventArgs.Empty);
            }
        }

        private void OnZoneChange(ushort sender)
        {
            if (IsBoundByDuty()) return;

            currentLocationName = Plugin.Instance.TerritoryNames.TryGetValue(Service.ClientState.TerritoryType, out var currentLocation) ? currentLocation : Lang.GetText("UnknownLocation");

            if (teleportCost != 0)
            {
                if (currentLocationName != previousLocationName)
                {
                    string? currencyName = currencyInfo.CurrencyLocalName(1);

                    string filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
                    var editedTransactions = transactions.LoadAllTransactions(currencyName);

                    editedTransactions.LastOrDefault().Note = $"{Lang.GetText("TeleportTo")} {currentLocationName}";

                    Plugin.Instance.Main.transactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);

                    Plugin.Instance.Main.UpdateTransactions();

                    teleportCost = 0;
                    previousLocationName = currentLocationName;

                }
            }
            else
            {
                if (currentLocation != previousLocationName)
                {
                    previousLocationName = currentLocationName;
                }
            }

        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            var chatmessage = message.TextValue;
            var typeValue = (ushort)type;

            if (TriggerChatTypes.Contains(typeValue))
            {
                UpdateCurrenciesByChat();
            }

            if (DutyEndStrings.Any(chatmessage.Contains))
            {
                Service.PluginLog.Debug("Duty End, Currency Change Check Start.");
                foreach (var currency in CurrencyType)
                {
                    if (CurrencyInfo.presetCurrencies.TryGetValue(currency, out uint currencyID))
                    {
                        string? currencyName = currencyInfo.CurrencyLocalName(currencyID);
                        if (currencyName != "Unknown" && currencyName != null)
                        {
                            CheckCurrency(currencyName, currencyID, true, dutyLocationName);
                        }
                    }
                }
                foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
                {
                    if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out uint currencyID))
                    {
                        if (currency != "Unknown" && currency != null)
                        {
                            CheckCurrency(currency, currencyID, true, dutyLocationName);
                        }
                    }
                }

                Service.PluginLog.Debug("Currency Change Check Completes.");
                dutyLocationName = string.Empty;
            }

            if (Plugin.Instance.PluginInterface.IsDev)
            {
                if (!IgnoreChatTypes.Contains(typeValue))
                {
                    Service.PluginLog.Debug($"[{typeValue}]{chatmessage}");
                }
            }
        }

        private void isDutyStarted(object? sender, ushort e)
        {
            Service.PluginLog.Debug("Duty Start");
            uint locationKey = Service.ClientState.TerritoryType;
            dutyLocationName = Plugin.Instance.TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : Lang.GetText("UnknownLocation");
        }

        private void isPvPEntered()
        {
        }

        private void isPvPLeft()
        {
        }

        public void TeleportingCostGil(uint GilAmount)
        {
            teleportCost = GilAmount;

            if (teleportCost != 0)
            {
                if (CurrencyInfo.presetCurrencies.TryGetValue("Gil", out uint currencyID))
                {
                    string? currencyName = currencyInfo.CurrencyLocalName(currencyID);
                    if (currencyName != "Unknown" && currencyName != null)
                    {
                        CheckCurrency(currencyName, currencyID, false, previousLocationName, Lang.GetText("TeleportWithinArea"));
                    }
                }
            }
        }

        private void DealWithCurrencies()
        {
            if (CurrencyInfo.presetCurrencies.TryGetValue("NonLimitedTomestone", out uint NonLimitedTomestoneID))
            {
                string? currencyName = currencyInfo.CurrencyLocalName(NonLimitedTomestoneID);
                if (currencyName != "Unknown" && currencyName != null)
                {
                    NonLimitedTomestoneName = currencyName;
                }
            }

            if (CurrencyInfo.presetCurrencies.TryGetValue("LimitedTomestone", out uint LimitedTomestoneID))
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

        public void Dispose()
        {
            Service.ClientState.TerritoryChanged -= OnZoneChange;
            Service.Chat.ChatMessage -= OnChatMessage;
            Service.DutyState.DutyStarted -= isDutyStarted;

            Service.ClientState.EnterPvP -= isPvPEntered;
            Service.ClientState.LeavePvP -= isPvPLeft;

            if (Plugin.Instance.Configuration.TrackMode == 0)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
        }
    }
}
