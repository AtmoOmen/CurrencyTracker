using System.Collections.Generic;
using System.Resources;

namespace CurrencyTracker.Manager
{
    // Language Names in Game:
    // { "Japanese", "日本語" },
    //{ "English", "English" },
    //{ "German", "Deutsch" },
    //{ "French", "Français" },
    //{ "ChineseSimplified", "简体中文" },
    public class LanguageManager
    {
        private ResourceManager? resourceManager;

        public static readonly Dictionary<string, string> LanguageNames = new Dictionary<string, string>
        {
            { "English", "English" },
            { "ChineseSimplified", "简体中文" },
        };

        public LanguageManager(string languageName)
        {
            if (!LanguageNames.ContainsKey(languageName))
            {
                languageName = "English";
            }

            var resourceName = "CurrencyTracker.Manager.Langs." + languageName;

            resourceManager = new ResourceManager(resourceName, typeof(LanguageManager).Assembly);
        }

        public List<string> AvailableLanguage()
        {
            var availablelangs = new List<string>();
            foreach (var language in LanguageNames.Keys)
            {
                if (LanguageNames.ContainsKey(language))
                {
                    var languagename = LanguageNames[language];
                    availablelangs.Add(languagename);
                }
            }
            return availablelangs;
        }

        public string GetText(string key)
        {
            return resourceManager.GetString(key) ?? key;
        }
    }
}