using CurrencyTracker.Manager;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.DutyState;
using Dalamud.Game.Gui;
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

    [PluginService] public static ClientState ClientState { get; private set; } = null!;
    [PluginService] public static Framework Framework { get; private set; } = null!;
    [PluginService] public static Condition Condition { get; private set; } = null!;
    [PluginService] public static DataManager DataManager { get; private set; } = null!;
    [PluginService] public static ChatGui Chat { get; private set; } = null!;
    [PluginService] public static CommandManager CommandManager { get; set; } = null!;
    [PluginService] public static DutyState DutyState { get; private set; } = null!;
    public static Tracker Tracker { get; set; } = null!;
    public static Configuration Configuration { get; set; } = null!;
    public static Transactions Transactions { get; set; } = null!;
}
