namespace CurrencyTracker.Manager
{
    public class LanguageManager
    {
        private readonly ResourceManager? resourceManager;
        private readonly ResourceManager? fbResourceManager;

        public static readonly List<TranslationInfo> LanguageNames = new()
        {
            new TranslationInfo { Language = "English", DisplayName = "English", Translators = new string[1] { "AtmoOmen" } },
            new TranslationInfo { Language = "Spanish", DisplayName = "Español", Translators = new string[1] { "Risu" } },
            new TranslationInfo { Language = "German", DisplayName = "Deutsch", Translators = new string[1] { "vyrnius" }},
            new TranslationInfo { Language = "French", DisplayName = "Français", Translators = new string[2] { "Khyne Cael", "Lexideru" } },
            new TranslationInfo { Language = "ChineseSimplified", DisplayName = "简体中文", Translators = new string[1] { "AtmoOmen" }},
            new TranslationInfo { Language = "ChineseTraditional", DisplayName = "繁體中文", Translators = new string[1] { "Fluxus" }}
        };

        public LanguageManager(string languageName)
        {
            if (!LanguageNames.Any(x => x.Language == languageName))
            {
                languageName = "English";
            }

            var resourceName = "CurrencyTracker.Manager.Langs." + languageName;
            resourceManager = new(resourceName, typeof(LanguageManager).Assembly);

            if (languageName == "ChineseTraditional")
            {
                fbResourceManager = new("CurrencyTracker.Manager.Langs.ChineseSimplified", typeof(LanguageManager).Assembly);
            }
            else
            {
                fbResourceManager = new("CurrencyTracker.Manager.Langs.English", typeof(LanguageManager).Assembly);
            }
        }

        public string GetText(string key, params object[] args)
        {
            if (!Plugin.Configuration.CustomNoteContents.TryGetValue(key, out var format))
            {
                format = resourceManager.GetString(key) ?? fbResourceManager.GetString(key);
            }
            if (format.IsNullOrEmpty())
            {
                Service.Log.Error($"Localization String {key} Not Found in Current Language!");
                return key;
            }
            return string.Format(format, args);
        }

        public string GetOrigText(string key)
        {
            return resourceManager.GetString(key) ?? fbResourceManager.GetString(key);
        }
    }

    public class TranslationInfo
    {
        public string Language { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string[] Translators { get; set; } = null!;
    }
}
