using System.Threading.Tasks;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Langs;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;
using LanguageManager = CurrencyTracker.Manager.Langs.LanguageManager;

namespace CurrencyTracker.Windows;

public partial class Main
{
    internal bool isLangDownloading;
    internal bool isLangDownloaded;

    private void OthersUI()
    {
        OpenDataFolderUI();

        ImGui.SameLine();
        MultiCharaStatsUI();

        ImGui.SameLine();
        GraphWindowUI();

        ImGui.SameLine();
        HelpPageUI();

        ImGui.SameLine();
        LanguageSwitchUI();
        if (P.PluginInterface.IsDev) TestingFeaturesUI();
    }

    // 打开数据文件夹 Open Data Folder
    private static void OpenDataFolderUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Folder, Service.Lang.GetText("OpenDataFolder")))
            OpenDirectory(P.PlayerDataFolder);
    }

    // 图表窗口 Graphs Window
    private static void GraphWindowUI()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || currentTypeTransactions.Count <= 1);
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ChartBar, Service.Lang.GetText("Graphs")))
            P.Graph.IsOpen = !P.Graph.IsOpen;
        ImGui.EndDisabled();
    }

    // 帮助页面 Help Page
    private static void HelpPageUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.QuestionCircle, $"{Service.Lang.GetText("NeedHelp")}"))
            ImGui.OpenPopup("NeedHelp");

        if (ImGui.BeginPopup("NeedHelp"))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.TankBlue);
            if (ImGui.Button("GitHub Issue"))
                Util.OpenLink("https://github.com/AtmoOmen/CurrencyTracker/issues");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedPurple);
            if (ImGui.Button("Discord Thread"))
                Util.OpenLink("https://discord.com/channels/581875019861328007/1019646133305344090/threads/1163039624957010021");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            if (ImGui.Button("QQ 频道"))
                Util.OpenLink("https://pd.qq.com/s/fttirpnql");
            ImGui.EndPopup();
        }
    }

    // 界面语言切换功能 Language Switch
    private void LanguageSwitchUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Globe, "Languages")) 
            ImGui.OpenPopup("LanguagesList");

        if (ImGui.BeginPopup("LanguagesList"))
        {
            ImGui.BeginGroup();
            for (var i = 0; i < LanguageManager.LanguageNames.Length; i++)
            {
                var languageInfo = LanguageManager.LanguageNames[i];
                if (ImGui.Selectable(languageInfo.DisplayName, Service.Config.SelectedLanguage == languageInfo.Language))
                    Service.Lang.SwitchLanguage(languageInfo.Language);
                ImGuiOm.TooltipHover($"By: {string.Join(", ", languageInfo.Translators)}");

                if (i + 1 != LanguageManager.LanguageNames.Length) ImGui.Separator();
            }
            ImGui.EndGroup();

            ImGui.Separator();
            ImGui.Separator();

            if (ImGuiOm.ButtonIconSelectable("UpdateTranslations",
                                             isLangDownloading
                                                 ? FontAwesomeIcon.Spinner
                                                 : FontAwesomeIcon.CloudDownloadAlt,
                                             Service.Lang.GetText("UpdateTranslations")))
            {
                if (!isLangDownloading)
                {
                    isLangDownloading = true;
                    isLangDownloaded = false;
                    Task.Run(async () =>
                    {
                        await LanguageUpdater.DownloadLanguageFilesAsync();
                        isLangDownloading = false;
                        isLangDownloaded = true;
                        Service.Lang.SwitchLanguage(Service.Config.SelectedLanguage);
                    });
                }
            }

            ImGui.Separator();

            if (ImGuiOm.ButtonIconSelectable("HelpTranslate", FontAwesomeIcon.Language, Service.Lang.GetText("HelpTranslate")))
                Util.OpenLink("https://crowdin.com/project/dalamud-currencytracker");

            ImGui.EndPopup();
        }
    }

    private static void TestingFeaturesUI() { }
}
