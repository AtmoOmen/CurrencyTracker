using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CurrencyTracker.Manager
{
# pragma warning disable CS8602
    public class Tracker : IDisposable
    {
        private int timerInterval = 0;

        public static readonly string[] CurrencyType = new string[]
        {
            "Gil","MGP",
            "StormSeal","SerpentSeal","FlameSeal",
            "WolfMark","TrophyCrystal",
            "AlliedSeal","CenturioSeal","SackOfNut",
            "BicolorGemstone","SkybuildersScript",
            "WhiteCrafterScript","WhiteGatherersScript","PurpleCrafterScript","PurpleGatherersScript",
            "NonLimitedTomestone", "LimitedTomestone", "Poetic"
        };

        private static readonly ushort[] TriggerChatTypes = new ushort[]
        {
            57, 0, 2110, 2105, 62, 3006, 3001
        };

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Stopwatch timer = new Stopwatch();
        private CurrencyInfo currencyInfo = new CurrencyInfo();
        private Transactions transactions = new Transactions();
        private static LanguageManager? Lang;
        private Dictionary<string, Dictionary<string, int>> minTrackValue = new();

        private string dutyLocationName = string.Empty;
        private bool dutyStartFlag = false;

        public delegate void CurrencyChangedHandler(object sender, EventArgs e);
        public event CurrencyChangedHandler? OnCurrencyChanged;

        protected virtual void OnTransactionsUpdate(EventArgs e)
        {
            OnCurrencyChanged?.Invoke(this, e);
        }

        // 测试用 For dev
        private static readonly ushort[] IgnoreChatTypes = new ushort[]
        {
            // 战斗相关 Related to Battle
            2091, 2218, 2857, 2729, 2224, 2222, 2859, 2219, 2221, 4139, 4398, 4270, 4397, 4269, 4400, 4777, 10283, 10537, 10409, 18475, 19113, 4783, 10544, 10929, 19632, 4399, 2223, 2225, 4401, 18734, 12331, 4783, 12331, 12585, 12591, 18605, 10922, 18733, 10928, 4778, 13098, 4922, 10410, 9001, 8235, 8752, 9007, 8236, 8746, 8750, 13104, 13102, 12713, 12719, 6959,
            // 新人频道 Novice Network
            27
        };

        public static bool IsBoundByDuty()
        {
            return Service.Condition[ConditionFlag.BoundByDuty] ||
                   Service.Condition[ConditionFlag.BoundByDuty56] ||
                   Service.Condition[ConditionFlag.BoundByDuty95];
        }

        public Tracker()
        {
            if (Plugin.Instance.Configuration.TrackMode == 0)
            {
                InitializeTimerTracking();
            }
            else if (Plugin.Instance.Configuration.TrackMode == 1)
            {
                InitializeChatTracking();
            }
            LoadMinTrackValue();
            Service.DutyState.DutyStarted += isDutyStarted;
            Service.DutyState.DutyCompleted += isDutyCompleted;
        }

        public void ChangeTracker()
        {
            if (Plugin.Instance.Configuration.TrackMode == 0)
            {
                InitializeTimerTracking();
            }
            else if (Plugin.Instance.Configuration.TrackMode == 1)
            {
                InitializeChatTracking();
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

            UpdateCurrenciesTimer();
            Service.ClientState.TerritoryChanged += OnZoneChange;
            Service.Chat.ChatMessage -= OnChatMessage;
        }

        private void InitializeChatTracking()
        {
            Service.Chat.ChatMessage += OnChatMessage;
            Service.ClientState.TerritoryChanged -= OnZoneChange;

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private void UpdateCurrenciesByChat()
        {
            currencyInfo ??= new CurrencyInfo();

            if (!Service.ClientState.IsLoggedIn) return;

            if (!Plugin.Instance.Configuration.TrackedInDuty)
            {
                if (IsBoundByDuty()) return;
            }

            if (Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51]) return;

            if (!dutyStartFlag) return;

            foreach (var currency in CurrencyType)
            {
                if (currencyInfo.permanentCurrencies.TryGetValue(currency, out uint currencyID))
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
            currencyInfo ??= new CurrencyInfo();
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
                if (currencyInfo.permanentCurrencies.TryGetValue(currency, out uint currencyID))
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

        private void CheckCurrency(string currencyName, uint currencyID, bool isDutyEnd, string currentLocationName = "-1")
        {
            currencyInfo ??= new CurrencyInfo();
            transactions ??= new Transactions();
            Lang = new LanguageManager(Plugin.Instance.Configuration.SelectedLanguage);
            TransactionsConvertor? latestTransaction = transactions.LoadLatestSingleTransaction(currencyName);
            long currencyAmount = currencyInfo.GetCurrencyAmount(currencyID);
            uint locationKey = Service.ClientState.TerritoryType;
            if (currentLocationName == "-1" || currentLocationName == "")
            {
                currentLocationName = Plugin.Instance.TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : Lang.GetText("UnknownLocation");
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
                                    transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName);
                                }
                                else return;
                            }
                            else
                            {
                                if (Math.Abs(currencyChange) >= 0)
                                {
                                    transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName);
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
                                transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName);
                            }
                            else return;
                        }
                    }
                }
                OnTransactionsUpdate(EventArgs.Empty);
            }
            else
            {
                transactions.AddTransaction(DateTime.Now, currencyName, currencyAmount, currencyAmount, currentLocationName);
                OnTransactionsUpdate(EventArgs.Empty);
            }
        }

        private void OnZoneChange(ushort sender)
        {
            if (IsBoundByDuty()) return;

            if (timer.Elapsed.Minutes >= 5 || !timer.IsRunning)
            {
                timer.Restart();
            }
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            var chatmessage = message.TextValue;
            var typeValue = (ushort)type;
#if DEV
            if (Plugin.Instance.PluginInterface.IsDev)
            {
                if (!IgnoreChatTypes.Contains(typeValue))
                {
                    PluginLog.Debug($"[{typeValue}]{chatmessage}");
                }
            }

            if (new[] { "任务结束了", "has ended", "の攻略を終了した" }.Any(chatmessage.Contains))
            {
                PluginLog.Debug("测试信息：某某任务结束了，开始检查货币变化");
                foreach (var currency in CurrencyType)
                {
                    if (currencyInfo.permanentCurrencies.TryGetValue(currency, out uint currencyID))
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

                PluginLog.Debug("测试信息：某某任务结束了，结束检查货币变化");
                dutyStartFlag = false;
                dutyLocationName = string.Empty;
            }
#endif




            if (TriggerChatTypes.Contains(typeValue)) UpdateCurrenciesByChat();

        }

        private void isDutyStarted(object? sender, ushort e)
        {
            if (Plugin.Instance.PluginInterface.IsDev)
            {
                Service.PluginLog.Debug("测试信息：副本开始");
            }
            dutyStartFlag = true;
            uint locationKey = Service.ClientState.TerritoryType;
            dutyLocationName = Plugin.Instance.TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : Lang.GetText("UnknownLocation");
        }

        private void isDutyCompleted(object? sender, ushort e)
        {
            if (Plugin.Instance.PluginInterface.IsDev)
            {
                Service.PluginLog.Debug("测试信息：副本完成");
            }

            dutyStartFlag = false;
            dutyLocationName = string.Empty;
        }

        public void Dispose()
        {
            Service.ClientState.TerritoryChanged -= OnZoneChange;
            Service.Chat.ChatMessage -= OnChatMessage;
            Service.DutyState.DutyStarted -= isDutyStarted;
            Service.DutyState.DutyCompleted -= isDutyCompleted;

            if (Plugin.Instance.Configuration.TrackMode == 0)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
        }
    }
}
