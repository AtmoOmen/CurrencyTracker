using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;

namespace CurrencyTracker.Manager.Trackers;

public class HandlerManager
{
    public static HashSet<TrackerHandlerBase> Handlers    { get; private set; } = [];
    
    public static ChatHandler?                ChatHandler { get; private set; }

    private static readonly HashSet<string> BlacklistHandlerNames = ["InventoryHandler"];

    public static void Init()
    {
        if (Handlers.Count == 0)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                                .Where(t => typeof(TrackerHandlerBase).IsAssignableFrom(t) &&
                                            t is { IsInterface: false, IsAbstract: false } &&
                                            t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in types)
            {
                if (type.Name.Contains("InventoryHandler")) continue;

                var instance = Activator.CreateInstance(type);
                if (instance is not TrackerHandlerBase handler) continue;
                
                Handlers.Add(handler);
            }

            ChatHandler = Handlers.OfType<ChatHandler>().FirstOrDefault();
        }

        foreach (var handler in Handlers)
        {
            if (handler.Initialized) continue;
            handler.Init();
        }
    }

    public static void Nullify<T>(ref T? handler) where T : TrackerHandlerBase?
    {
        if (handler == null) return;

        handler.Uninit();
        handler = null;
    }

    public static void Uninit() => 
        Handlers.ForEach(x => x.Uninit());
}
