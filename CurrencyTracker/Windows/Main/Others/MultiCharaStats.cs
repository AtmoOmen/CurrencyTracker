using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Infos;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;
using Task = System.Threading.Tasks.Task;

namespace CurrencyTracker.Windows;

public partial class Main
{
    internal static ConcurrentDictionary<CharacterInfo, CharacterCurrencyInfo> CharacterCurrencyInfos = new();
    private static Dictionary<CharacterInfo, CharacterCurrencyInfo>? _characterCurrencyDicMCS;
    private string searchFilterMCS = string.Empty;
    private int _currentPageMCS;

    private void MultiCharaStatsUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ChartColumn, Service.Lang.GetText("MultiCharaStats")))
        {
            Task.Run(() =>
            {
                if (!CharacterCurrencyInfos.Any()) LoadDataMCS();
                _characterCurrencyDicMCS ??= CharacterCurrencyInfos.ToDictionary(x => x.Key, x => x.Value);
            });

            ImGui.OpenPopup("MultiCharStats");
        }

        if (ImGui.BeginPopup("MultiCharStats"))
        {
            if (_characterCurrencyDicMCS == null) return;
            var itemCount = _characterCurrencyDicMCS.Count;
            var startIndex = _currentPageMCS * 10;
            var endIndex = Math.Min(startIndex + 10, itemCount);

            ImGui.BeginGroup();
            ImGui.SetNextItemWidth(240f);
            if (ImGui.InputTextWithHint("##selectFilterMultiCharaStats", Service.Lang.GetText("PleaseSearch"),
                                        ref searchFilterMCS, 100))
                LoadSearchResultMCS();

            ImGui.SameLine();
            ImGui.PushID("MultiCharaStatsPagingComponent");
            PagingComponent(
                () => _currentPageMCS = 0,
                () => { if (_currentPageMCS > 0) _currentPageMCS--; },
                () => { if (_currentPageMCS < (itemCount / 10) - 1) _currentPageMCS++; },
                () => { _currentPageMCS = (itemCount / 10) - 1; });
            ImGui.PopID();

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("MCSRefresh", FontAwesomeIcon.Sync))
                LoadDataMCS();
            ImGui.EndGroup();

            var itemWidth = (int)ImGui.GetItemRectSize().X;

            var items = _characterCurrencyDicMCS.Skip(startIndex).Take(endIndex - startIndex);
            foreach (var info in items)
            {
                var previewValue = $"{info.Value.Character.Name}@{info.Value.Character.Server}";
                ImGui.SetNextItemWidth(itemWidth);
                if (ImGui.BeginCombo($"###{previewValue}", previewValue, ImGuiComboFlags.HeightLarge))
                {
                    if (ImGui.BeginTable($"###{previewValue}", 2, ImGuiTableFlags.Borders))
                    {
                        foreach (var currency in Service.Config.AllCurrencies)
                        {
                            var amount = info.Value.CurrencyAmount.GetValueOrDefault(currency.Key, 0);
                            if (amount == 0) continue;

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Image(Service.Config.AllCurrencyIcons[currency.Key].ImGuiHandle,
                                        ImGuiHelpers.ScaledVector2(16.0f));
                            ImGui.SameLine();
                            ImGui.Text($"{currency.Value}");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{amount:N0}");
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndCombo();
                }

                if (ImGui.BeginPopupContextItem($"{info.Value.Character.ContentID}"))
                {
                    if (ImGui.MenuItem(Service.Lang.GetText("Delete")))
                    {
                        if (Service.Config.CurrentActiveCharacter.Remove(info.Key))
                        {
                            Service.Config.Save();
                            ImGui.CloseCurrentPopup();

                            Task.Run(() =>
                            {
                                CharacterCurrencyInfos.Clear();
                                LoadDataMCS();

                                _characterCurrencyDicMCS.Clear();
                                _characterCurrencyDicMCS =
                                    CharacterCurrencyInfos.ToDictionary(x => x.Key, x => x.Value);
                            });
                        }
                    }

                    ImGui.EndPopup();
                }
            }

            ImGui.EndPopup();
        }
    }

    internal static void LoadDataMCS()
    {
        var sortedCharacters = Service.Config.CurrentActiveCharacter
                                      .OrderBy(c => c.ContentID == Service.ClientState.LocalContentId)
                                      .ToArray();

        foreach (var character in sortedCharacters)
        {
            var info = CharacterCurrencyInfos.GetOrAdd(
                character, _ => new CharacterCurrencyInfo { Character = character });
            info.GetCharacterCurrencyAmount();
        }
    }


    private void LoadSearchResultMCS()
    {
        TaskManager.Abort();

        TaskManager.DelayNext(250);
        TaskManager.Enqueue(() =>
        {
            _currentPageMCS = 0;

            _characterCurrencyDicMCS = string.IsNullOrWhiteSpace(searchFilterMCS)
                                           ? CharacterCurrencyInfos.ToDictionary(x => x.Key, x => x.Value)
                                           : CharacterCurrencyInfos
                                             .Where(x => x.Key.Name.Contains(searchFilterMCS,
                                                                             StringComparison.OrdinalIgnoreCase) ||
                                                         x.Key.Server.Contains(
                                                             searchFilterMCS, StringComparison.OrdinalIgnoreCase))
                                             .ToDictionary(x => x.Key, x => x.Value);
        });
    }
}
