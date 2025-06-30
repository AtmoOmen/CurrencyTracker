using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Trackers;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OmenTools;
using OmenTools.Infos;
using LanguageManager = CurrencyTracker.Manager.Langs.LanguageManager;

namespace CurrencyTracker.Manager;

public class Service
{
    public static void Init(IDalamudPluginInterface PI)
    {
        DService.Init(PI);
        
        Config = PI.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(PI);

        InitLanguage();
        InitCharacter();
        PresetFont.Init();

        Tracker.Init();
        CurrencyInfo.Init();
    }

    public static void Uninit()
    {
        Tracker.Dispose();
        CurrencyInfo.Uninit();
        
        Config.Uninit();
        
        DService.Uninit();
    }

    private static void InitLanguage()
    {
        var playerLang = Config.SelectedLanguage;
        if (string.IsNullOrEmpty(playerLang))
        {
            playerLang = DService.ClientState.ClientLanguage.ToString();
            if (LanguageManager.LanguageNames.All(x => x.Language != playerLang))
                playerLang = "English";

            Config.SelectedLanguage = playerLang;
            Config.Save();
        }

        Lang = new LanguageManager(playerLang);
    }

    private static void InitCharacter()
    {
        if (DService.ObjectTable.LocalPlayer != null && LocalPlayerState.ContentID != 0)
            P.CurrentCharacter = P.GetCurrentCharacter();
    }
    
    public static                 Configuration           Config            { get; set; }         = null!;
    public static                 LanguageManager         Lang              { get; set; }         = null!;
}
