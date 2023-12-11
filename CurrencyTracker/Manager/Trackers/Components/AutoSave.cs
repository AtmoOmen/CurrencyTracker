namespace CurrencyTracker.Manager.Trackers.Components
{
    public class AutoSave : ITrackerComponent
    {
        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public static DateTime LastAutoSave = DateTime.MinValue;
        private static Timer? AutoSaveTimer;

        private bool _initialized = false;

        Configuration? C = Plugin.Configuration;

        public void Init()
        {
            LastAutoSave = DateTime.Now;

            AutoSaveTimer = new(1000);
            AutoSaveTimer.Elapsed += OnAutoSave;
            AutoSaveTimer.AutoReset = true;
            AutoSaveTimer.Enabled = true;

            _initialized = true;
        }

        private void OnAutoSave(object? sender, ElapsedEventArgs e)
        {
            if (DateTime.Now >= LastAutoSave + TimeSpan.FromMinutes(C.AutoSaveInterval))
            {
                AutoSaveHandler();
                LastAutoSave = DateTime.Now;
            }
        }

        private void AutoSaveHandler()
        {
            if (C.AutoSaveMode == 0)
            {
                var filePath = Main.BackupHandler(Plugin.Instance.PlayerDataFolder);
                if (C.AutoSaveMessage) Service.Chat.Print(Service.Lang.GetText("BackupHelp4", filePath));
            }
            else if (C.AutoSaveMode == 1)
            {
                var failCharacters = C.CurrentActiveCharacter
                        .Where(character => Main.BackupHandler(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{character.Name}_{character.Server}")).IsNullOrEmpty())
                        .Select(character => $"{character.Name}@{character.Server}")
                        .ToList();

                var successCount = C.CurrentActiveCharacter.Count - failCharacters.Count;
                if (C.AutoSaveMessage)
                {
                    Service.Chat.Print(Service.Lang.GetText("BackupHelp1", successCount) + (failCharacters.Any() ? Service.Lang.GetText("BackupHelp2", failCharacters.Count) : ""));

                    if (failCharacters.Any())
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("BackupHelp3"));
                        foreach (var chara in failCharacters)
                        {
                            Service.Chat.PrintError(chara);
                        }
                    }
                }
            }
        }

        public void Uninit()
        {
            AutoSaveHandler();

            AutoSaveTimer.Stop();
            AutoSaveTimer.Dispose();
            LastAutoSave = DateTime.MinValue;

            _initialized = false;
        }
    }
}
