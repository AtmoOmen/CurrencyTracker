namespace CurrencyTracker;

public sealed class Plugin : IDalamudPlugin
{
    public static string Name => "Currency Tracker";
    public const string CommandName = "/ct";

    public CharacterInfo? CurrentCharacter { get; set; }
    public string PlayerDataFolder => GetCurrentCharacterDataFolder();

    public DalamudPluginInterface PluginInterface { get; init; }
    public Main? Main { get; private set; }
    public Graph? Graph { get; private set; }
    public Settings? Settings { get; private set; }
    public CurrencySettings? CurrencySettings { get; private set; }

    public WindowSystem WindowSystem = new("CurrencyTracker");
    internal static Plugin P = null!;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        P = this;
        PluginInterface = pluginInterface;

        ConfigHandler(pluginInterface);

        Service.Initialize(pluginInterface);

        Service.ClientState.Login += HandleLogin;
        Service.ClientState.Logout += HandleLogout;

        CommandHandler();

        WindowsHandler();
    }

    private void HandleLogout()
    {
        Service.Tracker.UninitializeTracking();
        CurrentCharacter = null;
    }

    private void HandleLogin()
    {
        CurrentCharacter = GetCurrentCharacter();

        if (WindowSystem.Windows.Contains(Main) && Main.selectedCurrencyID != 0)
            Main.currentTypeTransactions = TransactionsHandler.LoadAllTransactions(Main.selectedCurrencyID);

        Service.Tracker.InitializeTracking();
    }

    public CharacterInfo? GetCurrentCharacter()
    {
        if (Service.ClientState.LocalContentId == 0 || Service.ClientState.LocalPlayer == null ||
            !Service.ClientState.IsLoggedIn) return null;

        var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
        var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name?.RawString;
        var contentID = Service.ClientState.LocalContentId;

        if (playerName.IsNullOrEmpty() || serverName.IsNullOrEmpty() || contentID == 0)
            Service.Log.Error("Fail to load current character info");

        var dataFolderName = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

        if (CurrentCharacter != null && (CurrentCharacter.ContentID == contentID ||
                                         (CurrentCharacter.Name == playerName &&
                                          CurrentCharacter.Server == serverName))) return CurrentCharacter;

        var existingCharacter =
            Service.Config.CurrentActiveCharacter.FirstOrDefault(
                x => x.ContentID == contentID || (x.Name == playerName && x.Server == serverName));
        if (existingCharacter != null)
        {
            existingCharacter.Server = serverName;
            existingCharacter.Name = playerName;
            existingCharacter.ContentID = contentID;
            CurrentCharacter = existingCharacter;
            Service.Log.Debug("Successfully load current character info.");
        }
        else
        {
            CurrentCharacter = new CharacterInfo
            {
                Name = playerName,
                Server = serverName,
                ContentID = contentID
            };
            Service.Config.CurrentActiveCharacter.Add(CurrentCharacter);
        }

        if (!Directory.Exists(dataFolderName))
        {
            Directory.CreateDirectory(dataFolderName);
            Service.Log.Debug("Successfully create character info directory.");
        }

        Service.Config.Save();

        return CurrentCharacter;
    }

    private string GetCurrentCharacterDataFolder()
    {
        CurrentCharacter ??= GetCurrentCharacter();

        if (CurrentCharacter == null) return string.Empty;

        var path = Path.Join(PluginInterface.ConfigDirectory.FullName,
                             $"{CurrentCharacter.Name}_{CurrentCharacter.Server}");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Service.Log.Debug("Successfully create character info directory.");
        }

        return path;
    }

    private void ConfigHandler(DalamudPluginInterface pluginInterface)
    {
        var configDirectory = Path.GetDirectoryName(pluginInterface.GetPluginConfigDirectory());
        var configPath = Path.Combine(configDirectory, "CurrencyTracker.json");

        if (File.Exists(configPath)) ParseOldConfiguration(configPath);

        Service.Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Service.Config.Initialize(PluginInterface);
    }

    public static void ParseOldConfiguration(string jsonFilePath)
    {
        if (jsonFilePath.IsNullOrEmpty() || !File.Exists(jsonFilePath)) return;

        var json = File.ReadAllText(jsonFilePath);
        var jsonObj = JObject.Parse(json);

        var dicts = new[]
        {
            jsonObj["CustomCurrencies"]?.ToObject<Dictionary<string, string>>(),
            jsonObj["PresetCurrencies"]?.ToObject<Dictionary<string, string>>()
        };

        foreach (var originalDict in dicts)
        {
            if (originalDict == null) continue;

            var swappedDict = new JObject();

            foreach (var entry in originalDict)
                if (uint.TryParse(entry.Key, out var _))
                    swappedDict.Add(entry.Key, JToken.FromObject(entry.Value));
                else
                    swappedDict.Add(entry.Value, JToken.FromObject(entry.Key));

            if (originalDict == dicts[0])
                jsonObj["CustomCurrencies"] = swappedDict;
            else
                jsonObj["PresetCurrencies"] = swappedDict;
        }

        var outputJson = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
        File.WriteAllText(jsonFilePath, outputJson);
    }

    private void CommandHandler()
    {
        var helpMessage = $"{Service.Lang.GetText("CommandHelp")}\n{Service.Lang.GetText("CommandHelp1")}";
        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = helpMessage });
    }

    public void OnCommand(string command, string args)
    {
        if (Main.visibleColumns == Array.Empty<string>())
            Main.visibleColumns = Service.Config.ColumnsVisibility.Where(c => c.Value).Select(c => c.Key).ToArray();
        if (args.IsNullOrEmpty())
        {
            Main.IsOpen = !Main.IsOpen;
            return;
        }

        var matchingCurrencies = FindMatchingCurrencies(Service.Config.AllCurrencies.Values.ToList(), args);
        var matchCount = matchingCurrencies.Count;

        switch (matchCount)
        {
            case 0:
                Service.Chat.PrintError(Service.Lang.GetText("CommandHelp3"));
                break;
            case 1:
                var currencyName = matchingCurrencies[0];
                var currencyPair = Service.Config.AllCurrencies.FirstOrDefault(x => x.Value == currencyName);
                var currencyID = currencyPair.Key;

                if (!Main.IsOpen || currencyID != Main.selectedCurrencyID)
                {
                    Main.IsOpen = true;
                    Main.selectedCurrencyID = currencyID;
                    Main.currentTypeTransactions = Main.ApplyFilters(TransactionsHandler.LoadAllTransactions(currencyID));
                }
                else
                    Main.IsOpen = false;

                break;
            default:
                Service.Chat.PrintError($"{Service.Lang.GetText("CommandHelp2")}:");
                foreach (var currency in matchingCurrencies) Service.Chat.PrintError(currency);
                break;
        }
    }

    private List<string> FindMatchingCurrencies(List<string> currencyList, string partialName)
    {
        var isCS = Service.Config.SelectedLanguage == "ChineseSimplified";
        partialName = partialName.Normalize(NormalizationForm.FormKC);

        var exactMatch = currencyList
            .FirstOrDefault(currency => string.Equals(currency, partialName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null) return new List<string> { exactMatch };

        var matchingCurrencies = new List<string>();
        foreach (var currency in currencyList)
        {
            var normalizedCurrency = currency.Normalize(NormalizationForm.FormKC);
            if (normalizedCurrency.Contains(partialName, StringComparison.OrdinalIgnoreCase))
            {
                matchingCurrencies.Add(currency);
                continue;
            }

            if (isCS)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedCurrency, "");
                if (pinyin.Contains(partialName, StringComparison.OrdinalIgnoreCase)) matchingCurrencies.Add(currency);
            }
        }

        return matchingCurrencies;
    }

    private void WindowsHandler()
    {
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        Main = new Main(this);
        WindowSystem.AddWindow(Main);

        Graph = new Graph(this);
        WindowSystem.AddWindow(Graph);

        Settings = new Settings(this);
        WindowSystem.AddWindow(Settings);

        CurrencySettings = new CurrencySettings(this);
        WindowSystem.AddWindow(CurrencySettings);
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void DrawConfigUI()
    {
        var currentCharacter = GetCurrentCharacter();
        if (currentCharacter == null) return;

        if (Main.visibleColumns == Array.Empty<string>())
            Main.visibleColumns = Service.Config.ColumnsVisibility.Where(c => c.Value).Select(c => c.Key).ToArray();

        Main.IsOpen = !Main.IsOpen;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        Main.Dispose();
        Graph.Dispose();
        Settings.Dispose();
        CurrencySettings.Dispose();

        Service.Tracker.Dispose();
        Service.ClientState.Login -= HandleLogin;
        Service.ClientState.Logout -= HandleLogout;

        Service.CommandManager.RemoveHandler(CommandName);

        Service.Config.Uninitialize();
    }
}
