using System;

namespace CurrencyTracker.Trackers;

public abstract class TrackerComponentBase
{
    public bool Initialized { get; private set; }

    public virtual void Init()
    {
        if (Initialized) return;

        try
        {
            OnInit();
            Initialized = true;
            
            DService.Instance().Log.Debug($"Loaded component {GetType().Name}");
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    public virtual void Uninit()
    {
        try
        {
            OnUninit();
            Initialized = false;
            
            DService.Instance().Log.Debug($"Unloaded component {GetType().Name}");
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    protected abstract void OnInit();

    protected abstract void OnUninit();

    protected virtual void HandleError(Exception ex)
    {
        try
        {
            OnUninit();
        }
        catch
        {
            // ignored
        }
        
        Initialized = false;
        
        DService.Instance().Log.Error(ex, $"Failed to load component {GetType().Name}: {ex.Message}");
    }
} 
