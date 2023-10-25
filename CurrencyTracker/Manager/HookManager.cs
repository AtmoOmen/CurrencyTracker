using Dalamud.Hooking;
using System;

namespace CurrencyTracker.Manager;

internal class HookManager
{
    private Configuration? C = Plugin.Instance.Configuration;
    private Plugin? P = Plugin.Instance;

    // From Tracky Track, now used to control teleport cost
    private const string ActorControlSig = "E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64";

    private delegate void ActorControlSelfDelegate(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, UInt64 targetId, byte param7);

    private Hook<ActorControlSelfDelegate> actorControlSelfHook;

    public HookManager(Plugin plugin)
    {
        var actorControlSelfPtr = Service.SigScanner.ScanText(ActorControlSig);
        actorControlSelfHook = Service.GameInteropProvider.HookFromAddress<ActorControlSelfDelegate>(actorControlSelfPtr, ActorControlSelf);
        actorControlSelfHook.Enable();
    }

    private void ActorControlSelf(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, UInt64 targetId, byte param7)
    {
        actorControlSelfHook.Original(category, eventId, param1, param2, param3, param4, param5, param6, targetId, param7);

        if (eventId != 517)
            return;

        if (!C.RecordTeleport)
        {
            return;
        }

        try
        {
            switch (param1)
            {
                case 4590:
                    Service.Tracker.TeleportWithCost((int)param2);
                    break;

                case 4591:
                    Service.Tracker.TeleportWithCost(-1);
                    break;
            }
        }
        catch (Exception e)
        {
            Service.PluginLog.Warning(e.Message);
            Service.PluginLog.Warning(e.StackTrace ?? "Unknown");
        }

        Service.PluginLog.Debug($"Cate {category} id {eventId} param1 {param1} param2 {param2} param3 {param3} param4 {param4} param5 {param5} param6 {param6} target {targetId} param7 {param7}");
    }

    public void Dispose()
    {
        actorControlSelfHook.Dispose();
    }
}
