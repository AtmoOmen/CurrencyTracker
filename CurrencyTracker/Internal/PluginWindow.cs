using CurrencyTracker.Windows;
using OmenTools.OmenService;

namespace CurrencyTracker.Internal;

internal static class PluginWindow
{
    private static DrawScopesHandle WindowStylesHandle;

    public static void Init()
    {
        var manager = WindowManager.Instance();

        manager.AddWindow<Main>();
        manager.AddWindow<GraphWindow>();
        manager.AddWindow<Settings>();
        manager.AddWindow<CurrencySettings>();

        WindowStylesHandle = manager.RegDrawScopes(() => FontManager.Instance().UIFont.Push());
        
        DService.Instance().UIBuilder.OpenMainUi   += ToggleMainWindow;
        DService.Instance().UIBuilder.OpenConfigUi += ToggleSettingsWindow;
    }
    
    public static void Uninit()
    {
        var manager = WindowManager.Instance();
        
        manager.RemoveWindow<Main>();
        manager.RemoveWindow<GraphWindow>();
        manager.RemoveWindow<Settings>();
        manager.RemoveWindow<CurrencySettings>();
        
        manager.UnregDrawScopes(WindowStylesHandle);
        WindowStylesHandle = default;
        
        DService.Instance().UIBuilder.OpenMainUi   -= ToggleMainWindow;
        DService.Instance().UIBuilder.OpenConfigUi -= ToggleSettingsWindow;
    }

    private static void ToggleMainWindow()
    {
        if (WindowManager.Instance().Get<Main>() is not { } main)
            return;
        
        if (!GameState.IsLoggedIn)
            return;
        
        main.IsOpen ^= true;
    }
    
    private static void ToggleSettingsWindow()
    {
        if (WindowManager.Instance().Get<Settings>() is not { } settings)
            return;
        
        if (!GameState.IsLoggedIn)
            return;
        
        settings.IsOpen ^= true;
    }
}
