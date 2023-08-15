using CurrencyTracker.Manger;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using System;
using System.Diagnostics;
using System.Threading;

namespace CurrencyTracker.Manager
{
    public class Tracker : IDisposable
    {
        public static readonly string[] CurrencyType = new string[]
        {
            // 金币和金碟币
            "Gil","MGP",
            // 军票
            "StormSeal","SerpentSeal","FlameSeal",
            // PVP
            "WolfMark","TrophyCrystal",
            // 怪物狩猎
            "AlliedSeal","CenturioSeal","SackOfNut",
            // 双色和天穹振兴票
            "BicolorGemstone","SkybuildersScript",
            // 生产采集
            "WhiteCrafterScript","WhiteGatherersScript","PurpleCrafterScript","PurpleGatherersScript",
            // 神典石
            "NonLimitedTomestone", "LimitedTomestone", "Poetic"
        };
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Stopwatch timer = new Stopwatch();
        private CurrencyInfo? currencyInfo = new CurrencyInfo();
        private Transactions? transactions = new Transactions();
        private LanguageManager lang;

        public static bool IsBoundByDuty()
        {
            return Service.Condition[ConditionFlag.BoundByDuty] ||
                   Service.Condition[ConditionFlag.BoundByDuty56] ||
                   Service.Condition[ConditionFlag.BoundByDuty95];
        }

        public Tracker()
        {
            UpdateCurrenciesTimer();

            Service.ClientState.TerritoryChanged += OnZoneChange;
        }

        private void UpdateCurrenciesTimer()
        {
            currencyInfo ??= new CurrencyInfo();
            Service.Framework.RunOnTick(UpdateCurrenciesTimer, TimeSpan.FromMilliseconds(500), 0, cancellationTokenSource.Token);

            if (!Service.ClientState.IsLoggedIn) return;

            if (!Plugin.GetPlugin.Configuration.TrackedInDuty)
            {
                if (IsBoundByDuty()) return;
            }


            foreach (var currency in CurrencyType)
            {
                if (currencyInfo.permanentCurrencies.TryGetValue(currency, out uint currencyID))
                {
                    string? currencyName = currencyInfo.CurrencyLocalName(currencyID);
                    if (currencyName != "未知货币" && currencyName != null)
                    {
                        CheckCurrency(currencyName, currencyID);
                    }
                }

            }
            foreach(var currency in Plugin.GetPlugin.Configuration.CustomCurrencyType)
            {
                if (Plugin.GetPlugin.Configuration.CustomCurrecies.TryGetValue(currency, out uint currencyID))
                {
                    if (currency != "未知货币" && currency != null)
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
            lang = new LanguageManager();
            lang.LoadLanguage(Plugin.GetPlugin.Configuration.SelectedLanguage);
            TransactionsConvetor? latestTransaction = transactions.LoadLatestSingleTransaction(currencyName);
            long currencyAmount = currencyInfo.GetCurrencyAmount(currencyID);
            uint locationKey = Service.ClientState.TerritoryType;
            string currentLocationName = Plugin.GetPlugin.TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : lang.GetText("UnknownLocation");
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
                    // 检查是否启用副本内最小记录值
                    if (Plugin.GetPlugin.Configuration.MinTrackValue != 0)
                    {
                        // 检查是否在副本里
                        if (IsBoundByDuty())
                        {
                            // 检查变化量是否大于等于设定的最小记录值
                            if (Math.Abs(currencyChange) >= Plugin.GetPlugin.Configuration.MinTrackValue)
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
            else
            {
                var lockoutRemaining = TimeSpan.FromMinutes(5) - timer.Elapsed;
                PluginLog.Debug($"区域变更信息抑制,距离下次触发警告还剩余 '{lockoutRemaining}' 分钟");
            }
        }

        public void Dispose()
        {
            Service.ClientState.TerritoryChanged -= OnZoneChange;

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
    }
}
