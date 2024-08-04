using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OmenTools.ImGuiOm;
using Task = System.Threading.Tasks.Task;

namespace CurrencyTracker.Windows;

public partial class Main
{
    internal static List<CharacterCurrencyInfo> CharacterCurrencyInfos = [];
    private static List<CharacterCurrencyInfo>? _characterCurrencyDicMCS;
    private static string searchFilterMCS = string.Empty;
    private static int _currentPageMCS;

    private static void MultiCharaStatsUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ChartColumn, Service.Lang.GetText("MultiCharaStats")))
        {
            if (CharacterCurrencyInfos.Count <= 0) LoadDataMCS();
            _characterCurrencyDicMCS ??= CharacterCurrencyInfos;

            ImGui.OpenPopup("Multi-Chara Stats##CurrencyTracker");
        }

        using var popup = ImRaii.Popup("Multi-Chara Stats##CurrencyTracker", 
                                       ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse);
        if (!popup.Success) return;

        PresetFont.Axis14.Push();
        ImGui.SetWindowFontScale(1.2f);
        MCSSubWindowUI();
        ImGui.SetWindowFontScale(1f);
        PresetFont.Axis14.Pop();
    }

    private static void MCSSubWindowUI()
    {
        if (_characterCurrencyDicMCS == null)
        {
            ImGui.CloseCurrentPopup();
            return;
        }

        var itemCount = _characterCurrencyDicMCS.Count;
        var startIndex = _currentPageMCS * 10;
        var endIndex = Math.Min(startIndex + 10, itemCount);

        ImGui.BeginGroup();
        ImGui.SetNextItemWidth(240f);
        ImGui.InputTextWithHint("##selectFilterMultiCharaStats", Service.Lang.GetText("PleaseSearch"), ref searchFilterMCS, 100);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _currentPageMCS = 0;
            _characterCurrencyDicMCS = string.IsNullOrWhiteSpace(searchFilterMCS)
                                           ? CharacterCurrencyInfos
                                           : CharacterCurrencyInfos
                                             .Where(x => x.Character.Name.Contains(searchFilterMCS,
                                                             StringComparison.OrdinalIgnoreCase) ||
                                                         x.Character.Server.Contains(
                                                             searchFilterMCS,
                                                             StringComparison.OrdinalIgnoreCase))
                                             .ToList();
        }

        ImGui.SameLine();
        ImGui.PushID("MultiCharaStatsPagingComponent");
        PagingComponent(
            () => _currentPageMCS = 0,
            () => { if (_currentPageMCS > 0) _currentPageMCS--; },
            () => { if (_currentPageMCS < (itemCount / 10) - 1) _currentPageMCS++; },
            () => { _currentPageMCS = (itemCount / 10) - 1; });
        ImGui.PopID();

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("MCSRefresh", FontAwesomeIcon.Sync, "", true)) LoadDataMCS();
        ImGui.EndGroup();

        var itemWidth = (int)ImGui.GetItemRectSize().X;
        var items = _characterCurrencyDicMCS.Skip(startIndex).Take(endIndex - startIndex);
        foreach (var info in items)
        {
            var previewValue = $"{info.Character.Name}@{info.Character.Server}";

            ImGui.SetNextItemWidth(itemWidth);
            if (ImGui.CollapsingHeader(previewValue))
            {
                if (ImGui.BeginTable($"###{info.Character.ContentID}", 2, ImGuiTableFlags.Borders))
                {
                    foreach (var currency in Service.Config.AllCurrencies)
                    {
                        var amount = info.CurrencyAmount.GetValueOrDefault(currency.Key, 0);
                        if (amount == 0) continue;

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();

                        ImGui.Image(Service.Config.AllCurrencyIcons[currency.Key].GetWrapOrEmpty().ImGuiHandle,
                                    ImGuiHelpers.ScaledVector2(16.0f));

                        ImGui.SameLine();
                        ImGui.Text($"{currency.Value}");

                        ImGui.SameLine();
                        ImGui.Spacing();

                        ImGui.TableNextColumn();
                        ImGui.Text($"{amount:N0}");
                    }

                    ImGui.EndTable();
                }
            }

            if (ImGui.BeginPopupContextItem($"{info.Character.ContentID}"))
            {
                if (ImGui.MenuItem(Service.Lang.GetText("Delete")))
                {
                    if (Service.Config.CurrentActiveCharacter.Remove(info.Character))
                    {
                        Service.Config.Save();
                        ImGui.CloseCurrentPopup();

                        Task.Run(() =>
                        {
                            LoadDataMCS();
                            _characterCurrencyDicMCS = CharacterCurrencyInfos;
                        });
                    }
                }

                ImGui.EndPopup();
            }
        }
    }

    internal static void LoadDataMCS()
    {
        CharacterCurrencyInfos.Clear();
        CharacterCurrencyInfos = Service.Config.CurrentActiveCharacter
                                        .OrderByDescending(c => c.ContentID == Service.ClientState.LocalContentId)
                                        .Select(x => new CharacterCurrencyInfo(x))
                                        .ToList();
    }
}
