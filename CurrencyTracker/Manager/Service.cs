using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Internal;
using CurrencyTracker.Manager.Tracker;
using Dalamud.Plugin;
using OmenTools.OmenService;
using LanguageManager = CurrencyTracker.Manager.Langs.LanguageManager;

namespace CurrencyTracker.Manager;

public class Service
{
    public static void Init(IDalamudPluginInterface pi)
    {
        DService.Init(pi);

        InitLanguage();
        InitCharacter();

        TrackerManager.Init();
        CurrencyInfo.Init();
        
        PluginWindow.Init();
        PluginCommand.Init();
    }

    public static void Uninit()
    {
        PluginCommand.Uninit();
        PluginWindow.Uninit();
        
        TrackerManager.Dispose();
        CurrencyInfo.Uninit();
        
        DService.Uninit();
    }

    private static void InitLanguage()
    {
        var playerLang = PluginConfig.Instance().SelectedLanguage;

        if (string.IsNullOrEmpty(playerLang))
        {
            playerLang = DService.Instance().ClientState.ClientLanguage.ToString();
            if (LanguageManager.LanguageNames.All(x => x.Language != playerLang))
                playerLang = "English";

            PluginConfig.Instance().SelectedLanguage = playerLang;
            PluginConfig.Instance().Save();
        }

        Lang = new(playerLang);
    }

    private static void InitCharacter()
    {
        if (LocalPlayerState.ContentID == 0)
            return;

        P.CurrentCharacter = P.GetCurrentCharacter();
    }

    public static LanguageManager Lang   { get; set; } = null!;
}
