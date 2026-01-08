using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;

namespace CurrencyTracker.Manager.Langs;

public partial class LanguageManager
{
    public class TranslationInfo
    {
        public string   Language    { get; set; } = null!;
        public string   DisplayName { get; set; } = null!;
        public string[] Translators { get; set; } = null!;
    }

    public static string  LangsDirectory { get; private set; } = null!;
    public        string? Language       { get; private set; }

    public delegate void LanguageChangeDelegate(string language);

    public event LanguageChangeDelegate? LanguageChange;

    private Dictionary<string, string>? resourceData;
    private Dictionary<string, string>? fbResourceData;

    public static readonly TranslationInfo[] LanguageNames =
    [
        new() { Language = "English", DisplayName = "English", Translators = ["AtmoOmen"] },
        new() { Language = "Spanish", DisplayName = "Español", Translators = ["Risu", "Raleo"] },
        new() { Language = "German", DisplayName = "Deutsch", Translators = ["vyrnius", "alex97000", "Another09"] },
        new() { Language = "French", DisplayName = "Français", Translators = ["Khyne Cael", "Lexideru"] },
        new() { Language = "Japanese", DisplayName = "日本語", Translators = ["stoat"] },
        new() { Language = "Korean", DisplayName = "한국어", Translators = ["solarx2"] },
        new() { Language = "ChineseSimplified", DisplayName = "简体中文", Translators = ["AtmoOmen"] },
        new() { Language = "ChineseTraditional", DisplayName = "繁體中文", Translators = ["Fluxus", "AtmoOmen"] },
    ];

    public LanguageManager(string languageName, bool isDev = false, string devLangPath = "")
        => SwitchLanguage(languageName, isDev, devLangPath);

    private static Dictionary<string, string> LoadResourceFile(string filePath)
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
            if (!string.IsNullOrEmpty(name) && value != null) data[name] = value;
        }

        return data;
    }

    public void SwitchLanguage(string languageName, bool isDev = false, string devLangPath = "")
    {
        if (languageName == Language) return;

        LangsDirectory = Path.Join(Path.GetDirectoryName(P.PI.AssemblyLocation.FullName), "Manager", "Langs");

        if (isDev)
            resourceData = LoadResourceFile(devLangPath);
        else
        {
            if (LanguageNames.All(x => x.Language != languageName)) languageName = "English";

            var resourcePath = Path.Join(LangsDirectory, languageName + ".resx");
            if (!File.Exists(resourcePath)) LanguageUpdater.DownloadLanguageFilesAsync().GetAwaiter().GetResult();

            resourceData = LoadResourceFile(resourcePath);
        }

        var fbPath = languageName switch
        {
            "ChineseTraditional" => Path.Join(LangsDirectory, "ChineseSimplified.resx"),
            _ => Path.Join(LangsDirectory, "English.resx"),
        };

        fbResourceData = LoadResourceFile(fbPath);

        Language = languageName;
        LanguageChange?.Invoke(languageName);

        DService.Instance().Command.RemoveHandler(CommandName);
        DService.Instance().Command.AddHandler(CommandName, new CommandInfo(P.OnCommand)
        {
            HelpMessage = GetText("CommandHelp") + "\n" + GetText("CommandHelp1"),
        });

        Service.Config.SelectedLanguage = languageName;
        Service.Config.Save();
    }

    public string GetText(string key, params object[] args)
    {
        if (!Service.Config.CustomNoteContents.TryGetValue(key, out var format))
            format = resourceData.TryGetValue(key, out var resValue) ? resValue : fbResourceData.GetValueOrDefault(key);

        if (string.IsNullOrEmpty(format))
        {
            DService.Instance().Log.Error($"Localization String {key} Not Found in Current Language!");
            return key;
        }

        return string.Format(format, args);
    }

    public string GetOrigText(string key) 
        => resourceData.TryGetValue(key, out var resValue) ? resValue : fbResourceData.GetValueOrDefault(key, key);

    public SeString GetSeString(string key, params object[] args)
    {
        if (!Service.Config.CustomNoteContents.TryGetValue(key, out var format))
            format = resourceData.TryGetValue(key, out var resValue) ? resValue : fbResourceData.GetValueOrDefault(key);

        var ssb = new SeStringBuilder();
        var regex = GetSeStringRegex();

        var lastIndex = 0;
        foreach (var match in regex.Matches(format).Cast<Match>())
        {
            ssb.AddText(format[lastIndex..match.Index]);

            var argIndex = int.Parse(match.Groups[1].Value);

            if (argIndex >= 0 && argIndex < args.Length)
            {
                var arg = args[argIndex];
                if (arg is SeString seStringArg)
                    ssb.Append(seStringArg);
                else
                    ssb.AddText(arg.ToString());
            }

            lastIndex = match.Index + match.Length;
        }

        ssb.AddText(format[lastIndex..]);

        return ssb.Build();
    }

    [GeneratedRegex("\\{(\\d+)\\}")]
    private static partial Regex GetSeStringRegex();
}
