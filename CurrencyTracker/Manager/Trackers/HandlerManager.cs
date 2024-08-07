using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Trackers.Handlers;

namespace CurrencyTracker.Manager.Trackers;

public class HandlerManager
{
    public static HashSet<ITrackerHandler> Handlers { get; private set; } = new();
    public static ChatHandler? ChatHandler { get; private set; }

    public static void Init()
    {
        if (!Handlers.Any())
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                                .Where(t => t.GetInterfaces().Contains(typeof(ITrackerHandler)) &&
                                            t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in types)
            {
                if (type.Name.Contains("InventoryHandler")) continue;

                var instance = Activator.CreateInstance(type);
                if (instance is ITrackerHandler handler) Handlers.Add(handler);
            }

            ChatHandler = Handlers.OfType<ChatHandler>().FirstOrDefault();
        }

        foreach (var handler in Handlers)
            try
            {
                if (!handler.Initialized)
                {
                    handler.Init();
                    handler.Initialized = true;
                    Service.Log.Debug($"Loaded {handler.GetType().Name} handler");
                }
                else
                    Service.Log.Debug($"{handler.GetType().Name} has been loaded, skip.");
            }
            catch (Exception ex)
            {
                handler.Uninit();
                handler.Initialized = false;

                Service.Log.Error($"Failed to load handler {handler.GetType().Name} due to error: {ex.Message}");
                Service.Log.Error(ex.StackTrace ?? "Unknown");
            }
    }

    public static void Nullify<T>(ref T handler) where T : ITrackerHandler?
    {
        if (handler == null) return;

        try
        {
            handler.Uninit();
            handler.Initialized = false;
            handler = default;
        }
        catch (Exception ex)
        {
            Service.Log.Error($"Failed to unload handler {handler.GetType().Name} due to error: {ex.Message}");
            Service.Log.Error(ex.StackTrace ?? "Unknown");
        }
    }

    public static void Uninit()
    {
        foreach (var handler in Handlers)
            try
            {
                handler.Uninit();
                handler.Initialized = false;
                Service.Log.Debug($"Unloaded {handler.GetType().Name} module");
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Failed to unload handler {handler.GetType().Name} due to error: {ex.Message}");
                Service.Log.Error(ex.StackTrace ?? "Unknown");
            }
    }
}
