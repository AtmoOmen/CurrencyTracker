namespace CurrencyTracker;

public class Service
{
    public static void Initialize(DalamudPluginInterface pluginInterface)
    {
        Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(pluginInterface);
        pluginInterface.Create<Service>();

        InitLanguage();
        InitCharacter();
        Tracker = new Tracker();
    }

    private static void InitLanguage()
    {
        var playerLang = Configuration.SelectedLanguage;
        if (playerLang.IsNullOrEmpty())
        {
            playerLang = ClientState.ClientLanguage.ToString();
            if (LanguageManager.LanguageNames.All(x => x.Language != playerLang))
            {
                playerLang = "English";
            }
            Configuration.SelectedLanguage = playerLang;
            Configuration.Save();
        }

        Lang = new LanguageManager(playerLang);
    }

    private static void InitCharacter()
    {
        if (ClientState.LocalPlayer != null || ClientState.LocalContentId != 0)
        {
            Plugin.Instance.CurrentCharacter = Plugin.Instance.GetCurrentCharacter();
        }
    }

    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IGameInventory GameInventory { get; private set; } = null!;
    [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] public static IAddonEventManager AddonEventManager { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; set; } = null!;
    public static SigScanner SigScanner { get; private set; } = new();
    public static Tracker Tracker { get; set; } = null!;
    public static Configuration Configuration { get; set; } = null!;
    public static LanguageManager Lang { get; set; } = null!;
}
