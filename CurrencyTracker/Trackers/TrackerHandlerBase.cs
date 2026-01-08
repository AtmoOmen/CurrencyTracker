using System;

namespace CurrencyTracker.Trackers;

public abstract class TrackerHandlerBase
{
    public bool Initialized { get; private set; }

    public bool IsBlocked { get; set; }

    public virtual void Init()
    {
        if (Initialized) return;

        try
        {
            OnInit();
            Initialized = true;
            
            DService.Instance().Log.Debug($"Loaded handler {GetType().Name}");
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
            IsBlocked   = false;
            
            DService.Instance().Log.Debug($"Unloaded handler {GetType().Name}");
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
        IsBlocked = false;
        
        DService.Instance().Log.Error(ex, $"Failed to load handler {GetType().Name}: {ex.Message}");
    }
} 
