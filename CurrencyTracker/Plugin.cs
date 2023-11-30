namespace CurrencyTracker
{
    public sealed class Plugin : IDalamudPlugin
    {
        public static string Name => "Currency Tracker";
        public DalamudPluginInterface PluginInterface { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("CurrencyTracker");
        internal Main Main { get; init; }
        internal Graph Graph { get; init; }
        internal RecordSettings RecordSettings { get; init; }
        public CharacterInfo? CurrentCharacter { get; set; }
        public static Plugin Instance = null!;

        internal const string CommandName = "/ct";

        public string PlayerDataFolder = string.Empty;
        private string playerLang = string.Empty;

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            Instance = this;
            PluginInterface = pluginInterface;

            if (File.Exists(Path.Combine(Path.GetDirectoryName(pluginInterface.GetPluginConfigDirectory()), "CurrencyTracker.json")))
            {
                ParseOldConfiguration(Path.Combine(Path.GetDirectoryName(pluginInterface.GetPluginConfigDirectory()), "CurrencyTracker.json"));
            }
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            Service.Initialize(pluginInterface);
            InitLanguage();

            // 已登录 Have Logged in
            if (Service.ClientState.LocalPlayer != null || Service.ClientState.LocalContentId != 0)
            {
                CurrentCharacter = GetCurrentCharacter();
            }

            Service.ClientState.Login += HandleLogin;
            Service.ClientState.Logout += HandleLogout;
            Service.Tracker = new Tracker();

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = Service.Lang.GetText("CommandHelp") + "\n" + Service.Lang.GetText("CommandHelp1")
            });

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

        private void HandleLogout()
        {
            Service.Tracker.UninitializeTracking();
        }

        private void HandleLogin()
        {
            if (Configuration.CurrentActiveCharacter == null)
            {
                Configuration.CurrentActiveCharacter = new();
            }

            CurrentCharacter = GetCurrentCharacter();
            GetCurrentCharcterDataFolder();

            if (WindowSystem.Windows.Contains(Main) && Main.selectedCurrencyID != 0)
            {
                Main.currentTypeTransactions = Transactions.LoadAllTransactions(Main.selectedCurrencyID);
                Main.lastTransactions = Main.currentTypeTransactions;
            }

            Service.Tracker.InitializeTracking();
        }

        public CharacterInfo GetCurrentCharacter()
        {
            if (Service.ClientState.LocalContentId == 0) return null;

            if (CurrentCharacter != null && CurrentCharacter.Name == Service.ClientState.LocalPlayer.Name.TextValue)
            {
                return CurrentCharacter;
            }

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            var dataFolderName = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

            if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(serverName))
            {
                throw new InvalidOperationException("Can't load current character info");
            }

            if (Configuration.CurrentActiveCharacter == null)
            {
                Configuration.CurrentActiveCharacter = new List<CharacterInfo>();
            }

            var existingCharacter = Configuration.CurrentActiveCharacter.FirstOrDefault(x => x.Name == playerName);
            if (existingCharacter != null)
            {
                existingCharacter.Server = serverName;
                existingCharacter.Name = playerName;
                CurrentCharacter = existingCharacter;
                Service.Log.Debug("Successfully load current character info.");
            }
            else
            {
                CurrentCharacter = new CharacterInfo
                {
                    Name = playerName,
                    Server = serverName,
                };
                Configuration.CurrentActiveCharacter.Add(CurrentCharacter);
            }

            if (!Directory.Exists(dataFolderName))
            {
                Directory.CreateDirectory(dataFolderName);
                Service.Log.Debug("Successfully create character info directory.");
            }

            PlayerDataFolder = dataFolderName;

            Configuration.Save();

            return CurrentCharacter;
        }

        private void GetCurrentCharcterDataFolder()
        {
            if (Service.ClientState.LocalPlayer != null)
            {
                var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
                var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
                var dataFolderName = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

                if (!Directory.Exists(dataFolderName))
                {
                    Directory.CreateDirectory(dataFolderName);
                    Service.Log.Debug("Successfully create character info directory.");
                }

                PlayerDataFolder = dataFolderName;
            }
        }

        internal void OnCommand(string command, string args)
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
                        Main.lastTransactions = Main.currentTypeTransactions;
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
                            Main.lastTransactions = Main.currentTypeTransactions;
                        }
                    }
                }
            }
        }

        private void InitLanguage()
        {
            playerLang = Configuration.SelectedLanguage;
            if (string.IsNullOrEmpty(playerLang))
            {
                playerLang = Service.ClientState.ClientLanguage.ToString();
                if (!LanguageManager.LanguageNames.Any(x => x.Language == playerLang))
                {
                    playerLang = "English";
                }
                Configuration.SelectedLanguage = playerLang;
                Configuration.Save();
            }

            Service.Lang = new LanguageManager(playerLang);
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
                .Where(currency => MatchesCurrency(currency, partialName, isChineseSimplified))
                .ToList();
        }

        private static bool MatchesCurrency(string currency, string partialName, bool isChineseSimplified)
        {
            var normalizedCurrency = currency.Normalize(NormalizationForm.FormKC);

            if (isChineseSimplified)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedCurrency, "");
                return normalizedCurrency.Contains(partialName, StringComparison.OrdinalIgnoreCase) || pinyin.Contains(partialName, StringComparison.OrdinalIgnoreCase);
            }

            return normalizedCurrency.Contains(partialName, StringComparison.OrdinalIgnoreCase);
        }

        public static void ParseOldConfiguration(string jsonFilePath)
        {
            if (jsonFilePath.IsNullOrEmpty() || !File.Exists(jsonFilePath))
            {
                return;
            }

            var json = File.ReadAllText(jsonFilePath);
            var jsonObj = JObject.Parse(json);

            Dictionary<string, string>[] dicts = new Dictionary<string, string>[]
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
