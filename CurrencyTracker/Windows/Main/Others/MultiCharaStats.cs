using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Infos;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    internal static ConcurrentDictionary<CharacterInfo, CharacterCurrencyInfo> characterCurrencyInfos = new();
    private static Dictionary<CharacterInfo, CharacterCurrencyInfo>? _characterCurrencyDicMCS;
    private string searchFilterMCS = string.Empty;
    private int _currentPageMCS;

    private void MultiCharaStatsUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ChartColumn, Service.Lang.GetText("MultiCharaStats")))
        {
            if (!characterCurrencyInfos.Any()) LoadDataMCS();

            ImGui.OpenPopup("MultiCharStats");
            _characterCurrencyDicMCS ??= characterCurrencyInfos.ToDictionary(x => x.Key, x => x.Value);
        }

        if (ImGui.BeginPopup("MultiCharStats"))
        {
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

            var items = _characterCurrencyDicMCS.Values.Skip(startIndex).Take(endIndex - startIndex);
            foreach (var info in items)
            {
                ImGui.SetNextItemWidth(itemWidth);
                if (ImGui.BeginCombo($"##{info.Character.Name}@{info.Character.Server}",
                                     $"{info.Character.Name}@{info.Character.Server}", ImGuiComboFlags.HeightLarge))
                {
                    ImGui.BeginGroup();
                    foreach (var currency in Service.Config.AllCurrencies)
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
                        ImGui.Image(Service.Config.AllCurrencyIcons[currency.Key].ImGuiHandle,
                                    ImGuiHelpers.ScaledVector2(16.0f));

                        ImGui.SameLine();
                        ImGui.Text($"{currency.Value}");
                    }

                    ImGui.EndGroup();

                    ImGui.SameLine();
                    ImGui.Spacing();

                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    foreach (var currency in Service.Config.AllCurrencies)
                    {
                        var amount = info.CurrencyAmount.GetValueOrDefault(currency.Key, 0);
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
                        ImGui.Text($"{amount:N0}");
                    }

                    ImGui.EndGroup();

                    ImGui.EndCombo();
                }
            }

            ImGui.EndPopup();
        }
    }

    internal static void LoadDataMCS()
    {
        var currentCharacter =
            Service.Config.CurrentActiveCharacter.FirstOrDefault(
                x => x.ContentID == Service.ClientState.LocalContentId);
        if (currentCharacter != null)
            characterCurrencyInfos.GetOrAdd(currentCharacter,
                                            new CharacterCurrencyInfo { Character = currentCharacter });

        Parallel.ForEach(Service.Config.CurrentActiveCharacter, character =>
        {
            var existingInfo =
                characterCurrencyInfos.GetOrAdd(character, new CharacterCurrencyInfo { Character = character });
            existingInfo.GetCharacterCurrencyAmount();
        });
    }

    private void LoadSearchResultMCS()
    {
        TaskManager.Abort();

        TaskManager.DelayNext(250);
        TaskManager.Enqueue(() =>
        {
            _currentPageMCS = 0;

            _characterCurrencyDicMCS = string.IsNullOrWhiteSpace(searchFilterMCS)
                                           ? characterCurrencyInfos.ToDictionary(x => x.Key, x => x.Value)
                                           : characterCurrencyInfos
                                             .Where(x => x.Key.Name.Contains(searchFilterMCS,
                                                                             StringComparison.OrdinalIgnoreCase) ||
                                                         x.Key.Server.Contains(
                                                             searchFilterMCS, StringComparison.OrdinalIgnoreCase))
                                             .ToDictionary(x => x.Key, x => x.Value);
        });
    }
}
