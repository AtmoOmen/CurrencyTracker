using CurrencyTracker.Manager.Trackers.Components;
using CurrencyTracker.Manager.Trackers;
using Dalamud.Hooking;
using System;
using System.Linq;

namespace CurrencyTracker.Manager;

internal class HookManager
{
    private Configuration? C = Plugin.Instance.Configuration;
    private Plugin? P = Plugin.Instance;

    private const string ActorControlSig = "E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64";

    private delegate void ActorControlSelfDelegate(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, UInt64 targetId, byte param7);

    private Hook<ActorControlSelfDelegate> actorControlSelfHook;

    public HookManager(Plugin plugin)
    {
        var actorControlSelfPtr = Service.SigScanner.ScanText(ActorControlSig);
        actorControlSelfHook = Service.GameInteropProvider.HookFromAddress<ActorControlSelfDelegate>(actorControlSelfPtr, ActorControlSelf);
        actorControlSelfHook.Enable();
    }

    private void ActorControlSelf(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, ulong targetId, byte param7)
    {
        actorControlSelfHook.Original(category, eventId, param1, param2, param3, param4, param5, param6, targetId, param7);

        if (eventId != 517)
            return;

        try
        {
            var instance = ComponentManager.Components.OfType<TeleportCosts>().FirstOrDefault();
            switch (param1)
            {
                case 4590:
                    instance.TeleportWithCost((int)param2);
                    break;

                case 4591:
                    instance.TeleportWithCost(-1);
                    break;
            }
        }
        catch (Exception e)
        {
            Service.PluginLog.Warning(e.Message);
            Service.PluginLog.Warning(e.StackTrace ?? "Unknown");
        }
    }

    public void Dispose()
    {
        actorControlSelfHook.Dispose();
    }
}
