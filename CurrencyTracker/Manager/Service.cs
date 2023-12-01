namespace CurrencyTracker;

public class Service
{
    public static void Initialize(DalamudPluginInterface pluginInterface)
    {
        Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(pluginInterface);
        pluginInterface.Create<Service>();
    }

    [PluginService] public static IAetheryteList AetheryteList { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static Framework Framework { get; private set; } = null!;
    [PluginService] public static Dalamud.Game.ClientState.Conditions.Condition Condition { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static ChatGui Chat { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] public static IDutyState DutyState { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] public static IAddonEventManager AddonEventManager { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; set; } = null!;
    public static SigScanner SigScanner { get; private set; } = new();
    public static Tracker Tracker { get; set; } = null!;
    public static Configuration Configuration { get; set; } = null!;
    public static LanguageManager Lang { get; set; } = null!;
}
