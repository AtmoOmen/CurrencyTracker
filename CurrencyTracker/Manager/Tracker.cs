using CurrencyTracker.Manger;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using System;
using System.Diagnostics;
using System.Threading;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;

namespace CurrencyTracker.Manager
{
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

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Stopwatch timer = new Stopwatch();
        private CurrencyInfo currencyInfo = new CurrencyInfo();
        private Transactions transactions = new Transactions();
        private static readonly LanguageManager Lang = new LanguageManager();

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
                UpdateCurrenciesTimer();
                Service.ClientState.TerritoryChanged += OnZoneChange;
                Service.Chat.ChatMessage -= OnChatMessage;
            }

            if (Plugin.Instance.Configuration.TrackMode == 1)
            {
                Service.Chat.ChatMessage += OnChatMessage;
                Service.ClientState.TerritoryChanged -= OnZoneChange;

                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
        }

        private void UpdateCurrenciesByChat()
        {
            currencyInfo ??= new CurrencyInfo();

            if (!Service.ClientState.IsLoggedIn) return;

            if (!Plugin.Instance.Configuration.TrackedInDuty)
            {
                if (IsBoundByDuty()) return;
            }

            foreach (var currency in CurrencyType)
            {
                if (currencyInfo.permanentCurrencies.TryGetValue(currency, out uint currencyID))
                {
                    string? currencyName = currencyInfo.CurrencyLocalName(currencyID);
                    if (currencyName != "Unknown" && currencyName != null)
                    {
                        CheckCurrency(currencyName, currencyID);
                    }
                }
            }
            foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
            {
                if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out uint currencyID))
                {
                    if (currency != "Unknown" && currency != null)
                    {
                        CheckCurrency(currency, currencyID);
                    }
                }
            }
        }

        private void UpdateCurrenciesTimer()
        {
            currencyInfo ??= new CurrencyInfo();
            timerInterval = Plugin.Instance.Configuration.TimerInterval;

                timerInterval = Plugin.Instance.Configuration.TimerInterval;
                Service.Framework.RunOnTick(UpdateCurrenciesTimer, TimeSpan.FromMilliseconds(timerInterval), 0, cancellationTokenSource.Token);

            if (!Service.ClientState.IsLoggedIn) return;

            if (!Plugin.Instance.Configuration.TrackedInDuty)
            {
                if (IsBoundByDuty()) return;
            }

            foreach (var currency in CurrencyType)
            {
                if (currencyInfo.permanentCurrencies.TryGetValue(currency, out uint currencyID))
                {
                    string? currencyName = currencyInfo.CurrencyLocalName(currencyID);
                    if (currencyName != "Unknown" && currencyName != null)
                    {
                        CheckCurrency(currencyName, currencyID);
                    }
                }
            }
            foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
            {
                if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out uint currencyID))
                {
                    if (currency != "Unknown" && currency != null)
                    {
                        CheckCurrency(currency, currencyID);
                    }
                }
            }
        }

        private void CheckCurrency(string currencyName, uint currencyID)
        {
            currencyInfo ??= new CurrencyInfo();
            transactions ??= new Transactions();
            Lang.LoadLanguage(Plugin.Instance.Configuration.SelectedLanguage);
            TransactionsConvertor? latestTransaction = transactions.LoadLatestSingleTransaction(currencyName);
            long currencyAmount = currencyInfo.GetCurrencyAmount(currencyID);
            uint locationKey = Service.ClientState.TerritoryType;
            string currentLocationName = Plugin.Instance.TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : Lang.GetText("UnknownLocation");
            if (latestTransaction != null)
            {
                long currencyChange = currencyAmount - latestTransaction.Amount;
                // 检查金额变化
                if (currencyChange == 0)
                {
                    return;
                }
                else
                {
                    // 检查是否启用副本内最小记录值 Check if enable tracking in duty
                    if (Plugin.Instance.Configuration.MinTrackValue != 0)
                    {
                        // 检查是否在副本里
                        if (IsBoundByDuty())
                        {
                            // 检查变化量是否大于等于设定的最小记录值
                            if (Math.Abs(currencyChange) >= Plugin.Instance.Configuration.MinTrackValue)
                            {
                                transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName);
                            }
                            else return;
                        }
                        else transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName);
                    }
                    else transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, currentLocationName);
                }
            }
            // 如果 latestTransaction 为空，则代表不存在该货币种类的数据文件，创建新的数据文件
            else
            {
                transactions.AddTransaction(DateTime.Now, currencyName, currencyAmount, currencyAmount, currentLocationName);
            }
        }

        private void OnZoneChange(object? sender, ushort e)
        {
            if (IsBoundByDuty()) return;

            if (timer.Elapsed.Minutes >= 5 || !timer.IsRunning)
            {
                timer.Restart();
            }
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            UpdateCurrenciesByChat();
        }

        public void Dispose()
        {
            Service.ClientState.TerritoryChanged -= OnZoneChange;
            Service.Chat.ChatMessage -= OnChatMessage;


            if (Plugin.Instance.Configuration.TrackMode == 0)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                return;
            }
        }
    }
}
