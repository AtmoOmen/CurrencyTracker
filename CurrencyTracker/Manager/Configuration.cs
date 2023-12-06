namespace CurrencyTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool FisrtOpen { get; set; } = true;
        public List<CharacterInfo> CurrentActiveCharacter { get; set; } = new();
        public Dictionary<uint, string> PresetCurrencies
        {
            set
            {
                presetCurrencies = value;
                isUpdated = true;
            }
            get
            {
                return presetCurrencies;
            }
        }
        public Dictionary<uint, string> CustomCurrencies
        {
            set
            {
                customCurrencies = value;
                isUpdated = true;
            }
            get
            {
                return customCurrencies;
            }
        }
        public List<uint> OrderedOptions { get; set; } = new();
        public bool ReverseSort { get; set; } = false;
        public string SelectedLanguage { get; set; } = string.Empty;
        public int RecordsPerPage { get; set; } = 20;
        public bool ChangeTextColoring { get; set; } = true;
        public Vector4 PositiveChangeColor { get; set; } = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 NegativeChangeColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        public int ChildWidthOffset { get; set; } = 0;
        public int ExportDataFileType { get; set; } = 0;
        public Dictionary<ulong, Dictionary<ulong, string>> CharacterRetainers { get; set; } = new(); // Content ID - Retainer ID : Retainer Name
        public Dictionary<string, bool> ColumnVisibility { get; set; } = new()
        {
            { "ShowTimeColumn", true },
            { "ShowAmountColumn", true },
            { "ShowChangeColumn", true },
            { "ShowLocationColumn", true },
            { "ShowNoteColumn", true },
            { "ShowOrderColumn", true },
            { "ShowCheckboxColumn", true }
        };
        public Dictionary<string, bool> ComponentEnabled { get; set; } = new()
        {
            { "DutyRewards", true},
            { "Exchange", true},
            { "FateRewards", true},
            { "GoldSaucer", true},
            { "IslandSanctuary", true},
            { "MobDrops", true},
            { "PremiumSaddleBag", true },
            { "QuestRewards", true},
            { "Retainer", true },
            { "SaddleBag", true },
            { "SpecialExchange", true},
            { "TeleportCosts", true},
            { "Trade", true},
            { "TripleTriad", true},
            { "WarpCosts", true},
        };
        public Dictionary<string, bool> ComponentProp { get; set; } = new()
        {
            // DutyRewards
            { "RecordContentName", true },
            // TeleportCosts
            { "RecordDesAetheryteName", false },
            { "RecordDesAreaName", true }
        };


        [JsonIgnore]
        public bool isUpdated;
        [JsonIgnore]
        public Dictionary<uint, IDalamudTextureWrap?> AllCurrencyIcons
        {
            get
            {
                if (allCurrencyIcons == null || isUpdated)
                {
                    GetAllCurrencyIcons();
                }
                return allCurrencyIcons;
            }
        }
        [JsonIgnore]
        public Dictionary<uint, string> AllCurrencies
        {
            get
            {
                if (allCurrencies == null || isUpdated)
                {
                    allCurrencies = GetAllCurrencies();
                    GetAllCurrencyIcons();
                }
                return allCurrencies;
            }
        }

        private Dictionary<uint, IDalamudTextureWrap?>? allCurrencyIcons = new();
        private Dictionary<uint, string>? allCurrencies = new();
        private Dictionary<uint, string> presetCurrencies = new();
        private Dictionary<uint, string> customCurrencies = new();

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;


        public void GetAllCurrencyIcons()
        {
            allCurrencyIcons.Clear();

            foreach (var currency in allCurrencies)
            {
                allCurrencyIcons.Add(currency.Key, CurrencyInfo.GetIcon(currency.Key));
            }

            isUpdated = false;

            Service.Log.Debug("Successfully reacquire all currency icons");
        }

        private Dictionary<uint, string> GetAllCurrencies()
        {
            Service.Log.Debug("Successfully reacquire all currencies");
            isUpdated = false;
            return PresetCurrencies.Concat(CustomCurrencies).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
        }
    }
}
