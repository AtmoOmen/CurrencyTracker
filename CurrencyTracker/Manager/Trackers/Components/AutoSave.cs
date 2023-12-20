namespace CurrencyTracker.Manager.Trackers.Components
{
    public class AutoSave : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        public static DateTime LastAutoSave = DateTime.MinValue;
        private static Timer AutoSaveTimer = new(1000);

        Configuration? C = Plugin.Configuration;

        public void Init()
        {
            LastAutoSave = DateTime.Now;

            AutoSaveTimer = new(1000);
            AutoSaveTimer.Elapsed += OnAutoSave;
            AutoSaveTimer.AutoReset = true;
            AutoSaveTimer.Enabled = true;

            Initialized = true;
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
                var filePath = Transactions.BackupTransactions(Plugin.Instance.PlayerDataFolder, Plugin.Configuration.MaxBackupFilesCount);
                if (C.AutoSaveMessage) Service.Chat.Print(Service.Lang.GetText("BackupHelp4", filePath));
            }
            else if (C.AutoSaveMode == 1)
            {
                var failCharacters = C.CurrentActiveCharacter
                        .Where(character => Transactions.BackupTransactions(Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{character.Name}_{character.Server}"), Plugin.Configuration.MaxBackupFilesCount).IsNullOrEmpty())
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

            Initialized = false;
        }
    }
}
