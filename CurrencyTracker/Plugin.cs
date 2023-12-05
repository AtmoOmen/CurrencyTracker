namespace CurrencyTracker
{
    public sealed class Plugin : IDalamudPlugin
    {
        public static string Name => "Currency Tracker";
        public const string CommandName = "/ct";

        public CharacterInfo? CurrentCharacter { get; set; }
        public string PlayerDataFolder => GetCurrentCharcterDataFolder();

        public DalamudPluginInterface PluginInterface { get; init; }
        public Main? Main { get; private set; }
        public Graph? Graph { get; private set; }
        public RecordSettings? RecordSettings { get; private set; }

        public WindowSystem WindowSystem = new("CurrencyTracker");
        public static Configuration? Configuration = null!;
        public static Plugin Instance = null!;


        public Plugin(DalamudPluginInterface pluginInterface)
        {
            Instance = this;
            PluginInterface = pluginInterface;

            ConfigHandler(pluginInterface);

            Service.Initialize(pluginInterface);

            if (Service.ClientState.LocalPlayer != null || Service.ClientState.LocalContentId != 0)
            {
                CurrentCharacter = GetCurrentCharacter();
            }

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
            Configuration.CurrentActiveCharacter ??= new();

            CurrentCharacter = GetCurrentCharacter();

            if (WindowSystem.Windows.Contains(Main) && Main.selectedCurrencyID != 0)
            {
                Main.currentTypeTransactions = Transactions.LoadAllTransactions(Main.selectedCurrencyID);
            }

            Service.Tracker.InitializeTracking();
        }

        public CharacterInfo GetCurrentCharacter()
        {
            if (Service.ClientState.LocalContentId == 0 || Service.ClientState.LocalPlayer == null || !Service.ClientState.IsLoggedIn) return null;

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name?.RawString;
            var contentID = Service.ClientState.LocalContentId;

            if (playerName.IsNullOrEmpty() || serverName.IsNullOrEmpty() || contentID == 0)
            {
                Service.Log.Error("Fail to load current character info");
            }

            var dataFolderName = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

            if (CurrentCharacter != null && (CurrentCharacter.ContentID == contentID || (CurrentCharacter.Name == playerName && CurrentCharacter.Server == serverName)))
            {
                return CurrentCharacter;
            }

            Configuration.CurrentActiveCharacter ??= new();

            var existingCharacter = Configuration.CurrentActiveCharacter.FirstOrDefault(x => x.ContentID == contentID || (x.Name == playerName && x.Server == serverName));
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
                    ContentID = contentID,
                };
                Configuration.CurrentActiveCharacter.Add(CurrentCharacter);
            }

            if (!Directory.Exists(dataFolderName))
            {
                Directory.CreateDirectory(dataFolderName);
                Service.Log.Debug("Successfully create character info directory.");
            }

            Configuration.Save();

            return CurrentCharacter;
        }

        private string GetCurrentCharcterDataFolder()
        {
            CurrentCharacter ??= GetCurrentCharacter();

            if (CurrentCharacter == null) return string.Empty;

            var path = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{CurrentCharacter.Name}_{CurrentCharacter.Server}");
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

            if (File.Exists(configPath))
            {
                ParseOldConfiguration(configPath);
            }

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);
        }

        public static void ParseOldConfiguration(string jsonFilePath)
        {
            if (jsonFilePath.IsNullOrEmpty() || !File.Exists(jsonFilePath))
            {
                return;
            }

            var json = File.ReadAllText(jsonFilePath);
            var jsonObj = JObject.Parse(json);

            var dicts = new Dictionary<string, string>[]
            {
                jsonObj["CustomCurrencies"]?.ToObject<Dictionary<string, string>>(),
                jsonObj["PresetCurrencies"]?.ToObject<Dictionary<string, string>>()
            };

            foreach (var originalDict in dicts)
            {
                if (originalDict == null)
                {
                    continue;
                }

                var swappedDict = new JObject();

                foreach (var entry in originalDict)
                {
                    if (uint.TryParse(entry.Key, out var _))
                    {
                        swappedDict.Add(entry.Key, JToken.FromObject(entry.Value));
                    }
                    else
                    {
                        swappedDict.Add(entry.Value, JToken.FromObject(entry.Key));
                    }
                }

                if (originalDict == dicts[0])
                {
                    jsonObj["CustomCurrencies"] = swappedDict;
                }
                else
                {
                    jsonObj["PresetCurrencies"] = swappedDict;
                }
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
            if (args.IsNullOrEmpty())
            {
                Main.IsOpen = !Main.IsOpen;
            }
            else
            {
                var matchingCurrencies = FindMatchingCurrencies(Configuration.AllCurrencies.Values.ToList(), args);
                if (matchingCurrencies.Count > 1)
                {
                    Service.Chat.PrintError($"{Service.Lang.GetText("CommandHelp2")}:");
                    foreach (var currency in matchingCurrencies)
                    {
                        Service.Chat.PrintError(currency);
                    }
                }
                else if (matchingCurrencies.Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("CommandHelp3"));
                }
                else
                {
                    var currencyName = matchingCurrencies.FirstOrDefault();
                    var currencyID = Configuration.AllCurrencies.FirstOrDefault(x => x.Value == currencyName).Key;
                    if (!Main.IsOpen)
                    {
                        Main.selectedCurrencyID = currencyID;
                        Main.selectedOptionIndex = Configuration.OrderedOptions.IndexOf(currencyID);
                        Main.currentTypeTransactions = Main.ApplyFilters(Transactions.LoadAllTransactions(currencyID));
                        Main.IsOpen = true;
                    }
                    else
                    {
                        if (currencyID == Main.selectedCurrencyID)
                        {
                            Main.IsOpen = !Main.IsOpen;
                        }
                        else
                        {
                            Main.selectedCurrencyID = currencyID;
                            Main.selectedOptionIndex = Configuration.OrderedOptions.IndexOf(currencyID);
                            Main.currentTypeTransactions = Main.ApplyFilters(Transactions.LoadAllTransactions(currencyID));
                        }
                    }
                }
            }
        }

        private List<string> FindMatchingCurrencies(List<string> currencyList, string partialName)
        {
            var isChineseSimplified = Configuration.SelectedLanguage == "ChineseSimplified";
            partialName = partialName.Normalize(NormalizationForm.FormKC);

            var exactMatch = currencyList
                .FirstOrDefault(currency => string.Equals(currency, partialName, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return new List<string> { exactMatch };
            }

            return currencyList
                .Where(currency =>
                {
                    var normalizedCurrency = currency.Normalize(NormalizationForm.FormKC);
                    if (isChineseSimplified)
                    {
                        var pinyin = PinyinHelper.GetPinyin(normalizedCurrency, "");
                        return normalizedCurrency.Contains(partialName, StringComparison.OrdinalIgnoreCase) || pinyin.Contains(partialName, StringComparison.OrdinalIgnoreCase);
                    }
                    return normalizedCurrency.Contains(partialName, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }

        private void WindowsHandler()
        {
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Main = new Main(this);
            WindowSystem.AddWindow(Main);
            Service.Tracker.OnCurrencyChanged += Main.UpdateTransactionsEvent;

            Graph = new Graph(this);
            WindowSystem.AddWindow(Graph);

            RecordSettings = new RecordSettings(this);
            WindowSystem.AddWindow(RecordSettings);
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            var currentCharacter = GetCurrentCharacter();
            if (currentCharacter == null)
            {
                return;
            }

            Main.IsOpen = !Main.IsOpen;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();

            Main.Dispose();
            Graph.Dispose();
            RecordSettings.Dispose();

            Service.Tracker.OnCurrencyChanged -= Main.UpdateTransactionsEvent;
            Service.Tracker.Dispose();
            Service.ClientState.Login -= HandleLogin;
            Service.ClientState.Logout -= HandleLogout;

            Service.CommandManager.RemoveHandler(CommandName);
        }
    }
}
