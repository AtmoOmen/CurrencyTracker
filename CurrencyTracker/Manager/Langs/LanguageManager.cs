namespace CurrencyTracker.Manager
{
    // Language Names in Game:
    // { "Japanese", "日本語" },
    // { "English", "English" },
    // { "German", "Deutsch" },
    // { "French", "Français" },
    // { "Spanish", "Español" },
    // { "ChineseSimplified", "简体中文" },
    // { "ChineseTraditional", "繁體中文" },
    public class LanguageManager
    {
        private readonly ResourceManager? resourceManager;

        public static readonly List<TranslationInfo> LanguageNames = new()
        {
            new TranslationInfo { Language = "English", DisplayName = "English", Translators = "AtmoOmen" },
            new TranslationInfo { Language = "Spanish", DisplayName = "Español", Translators = "Risu"},
            new TranslationInfo { Language = "German", DisplayName = "Deutsch", Translators = "vyrnius"},
            new TranslationInfo { Language = "ChineseSimplified", DisplayName = "简体中文", Translators = "AtmoOmen"},
            new TranslationInfo { Language = "ChineseTraditional", DisplayName = "繁體中文", Translators = "Fluxus"}
        };

        public LanguageManager(string languageName)
        {
            if (!LanguageNames.Any(x => x.Language == languageName))
            {
                languageName = "English";
            }

            var resourceName = "CurrencyTracker.Manager.Langs." + languageName;

            resourceManager = new ResourceManager(resourceName, typeof(LanguageManager).Assembly);
        }

        public string GetText(string key, params object[] args)
        {
            var format = resourceManager.GetString(key);
            if (format.IsNullOrEmpty())
            {
                Service.Log.Error($"Localization String {key} Not Found in Current Language!");
                return key;
            }

            return string.Format(format, args);
        }
    }

    public class TranslationInfo
    {
        public string Language { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Translators { get; set; } = null!;
    }
}
