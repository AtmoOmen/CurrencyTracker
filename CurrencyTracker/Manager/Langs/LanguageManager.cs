namespace CurrencyTracker.Manager;

public class LanguageManager
{
    public static string LangsDirectory { get; private set; } = null!;
    public string Language { get; private set; } = null!;

    private readonly Dictionary<string, string>? resourceData;
    private readonly Dictionary<string, string>? fbResourceData;

    public static readonly TranslationInfo[] LanguageNames = new TranslationInfo[]
    {
        new() { Language = "English", DisplayName = "English", Translators = new string[1] { "AtmoOmen" } },
        new() { Language = "Spanish", DisplayName = "Español", Translators = new string[1] { "Risu" } },
        new() { Language = "German", DisplayName = "Deutsch", Translators = new string[1] { "vyrnius" }},
        new() { Language = "French", DisplayName = "Français", Translators = new string[2] { "Khyne Cael", "Lexideru" } },
        new() { Language = "ChineseSimplified", DisplayName = "简体中文", Translators = new string[1] { "AtmoOmen" }},
        new() { Language = "ChineseTraditional", DisplayName = "繁體中文", Translators = new string[1] { "Fluxus" }}
    };

    public LanguageManager(string languageName, bool isDev = false, string devLangPath = "")
    {
        LangsDirectory = Path.Join(Path.GetDirectoryName(Plugin.Instance.PluginInterface.AssemblyLocation.FullName), "Manager", "Langs");

        if (isDev)
        {
            resourceData = LoadResourceFile(devLangPath);
        }
        else
        {
            if (LanguageNames.All(x => x.Language != languageName))
            {
                languageName = "English";
            }

            var resourcePath = Path.Join(LangsDirectory, languageName + ".resx");
            if (!File.Exists(resourcePath)) LanguageUpdater.DownloadLanguageFilesAsync().GetAwaiter().GetResult();
            resourceData = LoadResourceFile(resourcePath);
        }

        var fbResourcePath = languageName == "ChineseTraditional"
            ? Path.Join(LangsDirectory, "ChineseSimplified.resx")
            : Path.Join(LangsDirectory, "English.resx");

        fbResourceData = LoadResourceFile(fbResourcePath);

        Language = languageName;
    }

    private Dictionary<string, string> LoadResourceFile(string filePath)
    {
        var data = new Dictionary<string, string>();

        if (!File.Exists(filePath))
        {
            LanguageUpdater.DownloadLanguageFilesAsync().GetAwaiter().GetResult();
            if (!File.Exists(filePath)) return data;
        }

        var doc = XDocument.Load(filePath);
        var dataElements = doc.Root.Elements("data");
        foreach (var element in dataElements)
        {
            var name = element.Attribute("name")?.Value;
            var value = element.Element("value")?.Value;
            if (!string.IsNullOrEmpty(name) && value != null)
            {
                data[name] = value;
            }
        }

        return data;
    }

    public string GetText(string key, params object[] args)
    {
        if (!Plugin.Configuration.CustomNoteContents.TryGetValue(key, out var format))
        {
            format = resourceData.TryGetValue(key, out var resValue) ? resValue : fbResourceData.TryGetValue(key, out var fbResValue) ? fbResValue : null;
        }

        if (string.IsNullOrEmpty(format))
        {
            Service.Log.Error($"Localization String {key} Not Found in Current Language!");
            return key;
        }

        return string.Format(format, args);
    }

    public string GetOrigText(string key)
    {
        return resourceData.TryGetValue(key, out var resValue) ? resValue : fbResourceData.TryGetValue(key, out var fbResValue) ? fbResValue : key;
    }

    public SeString GetSeString(string key, params object[] args)
    {
        if (!Plugin.Configuration.CustomNoteContents.TryGetValue(key, out var format))
        {
            format = resourceData.TryGetValue(key, out var resValue) ? resValue : fbResourceData.TryGetValue(key, out var fbResValue) ? fbResValue : null;
        }

        var ssb = new SeStringBuilder();
        var regex = new Regex(@"\{(\d+)\}");

        var lastIndex = 0;
        foreach (var match in regex.Matches(format).Cast<Match>())
        {
            ssb.AddText(format.Substring(lastIndex, match.Index - lastIndex));

            var argIndex = int.Parse(match.Groups[1].Value);

            if (argIndex >= 0 && argIndex < args.Length)
            {
                var arg = args[argIndex];
                if (arg is SeString seStringArg)
                {
                    ssb.Append(seStringArg);
                }
                else
                {
                    ssb.AddText(arg.ToString());
                }
            }

            lastIndex = match.Index + match.Length;
        }

        ssb.AddText(format.Substring(lastIndex));

        return ssb.Build();
    }
}

public class TranslationInfo
{
    public string Language { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string[] Translators { get; set; } = null!;
}
