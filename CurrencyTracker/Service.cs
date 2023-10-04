using CurrencyTracker.Manager;
using Dalamud.Plugin.Services;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace CurrencyTracker;

public class Service
{
    public static void Initialize(DalamudPluginInterface pluginInterface)
    {
        Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(pluginInterface);
        Transactions = new Transactions();
        pluginInterface.Create<Service>();
    }

    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] public static IDutyState DutyState { get; private set; } = null!;
    [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;
    public static Tracker Tracker { get; set; } = null!;
    public static Configuration Configuration { get; set; } = null!;
    public static Transactions Transactions { get; set; } = null!;
}
