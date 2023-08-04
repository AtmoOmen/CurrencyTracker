using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager;

public class Tracker : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Stopwatch timer = new();
    public static bool IsBoundByDuty()
    {
        return Service.Condition[ConditionFlag.BoundByDuty] ||
               Service.Condition[ConditionFlag.BoundByDuty56] ||
               Service.Condition[ConditionFlag.BoundByDuty95];
    }

    public Tracker()
    {
        UpdateCurrencies();

        Service.ClientState.TerritoryChanged += OnZoneChange;
    }

    private void UpdateCurrencies()
    {
        Service.Framework.RunOnTick(UpdateCurrencies, TimeSpan.FromMilliseconds(2500), 0, cancellationTokenSource.Token);

        if (!Service.ClientState.IsLoggedIn) return;

        // 更新货币的逻辑
    }

    private void OnZoneChange(object? sender, ushort e)
    {
        if (IsBoundByDuty()) return;

        if (timer.Elapsed.Minutes >= 5 || timer.IsRunning == false)
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
