using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using Dalamud.Plugin;
using LanguageManager = CurrencyTracker.Manager.Langs.LanguageManager;

namespace CurrencyTracker.Manager;

public class Service
{
    public static void Init(IDalamudPluginInterface pi)
    {
        DService.Init(pi);

        Config = pi.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(pi);
        
        InitLanguage();
        InitCharacter();

        TrackerManager.Init();
        CurrencyInfo.Init();
    }

    public static void Uninit()
    {
        TrackerManager.Dispose();
        CurrencyInfo.Uninit();

        Config.Uninit();
        
        DService.Uninit();
    }

    private static void InitLanguage()
    {
        var playerLang = Config.SelectedLanguage;

        if (string.IsNullOrEmpty(playerLang))
        {
            playerLang = DService.Instance().ClientState.ClientLanguage.ToString();
            if (LanguageManager.LanguageNames.All(x => x.Language != playerLang))
                playerLang = "English";

            Config.SelectedLanguage = playerLang;
            Config.Save();
        }

        Lang = new(playerLang);
    }

    private static void InitCharacter()
    {
        if (LocalPlayerState.ContentID == 0)
            return;
        
        P.CurrentCharacter = P.GetCurrentCharacter();
    }

    public static Configuration   Config { get; set; } = null!;
    public static LanguageManager Lang   { get; set; } = null!;
}
