using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CurrencyTracker.Manager.Infos;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using OmenTools.Helpers;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    public readonly ConcurrentDictionary<CharacterInfo, CharacterCurrencyInfo> characterCurrencyInfos = new();
    private IEnumerable<CharacterCurrencyInfo>? charactersToShow;

    private readonly Timer searchTimerMCS = new(100);
    private string searchFilterMCS = string.Empty;
    private int currentPageMCS = 0;

    private void MultiCharaStatsUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ChartColumn, Service.Lang.GetText("MultiCharaStats")))
        {
            if (!characterCurrencyInfos.Any()) LoadDataMCS();

            LoadSearchResultMCS();
            ImGui.OpenPopup("MultiCharStats");
        }

        using (var popup = ImRaii.Popup("MultiCharStats"))
        {
            if (popup.Success)
            {
                ImGui.BeginGroup();
                ImGui.SetNextItemWidth(240f);
                if (ImGui.InputTextWithHint("##selectFilterMultiCharaStats", Service.Lang.GetText("PleaseSearch"), ref searchFilterMCS, 100)) searchTimerMCS.Restart();

                ImGui.SameLine();
                if (ImGui.ArrowButton("CustomPreviousPage", ImGuiDir.Left))
                {
                    if (currentPageMCS > 0)
                    {
                        currentPageMCS--;
                        LoadSearchResultMCS();
                    }
                }

                ImGui.SameLine();
                if (ImGui.ArrowButton("CustomNextPage", ImGuiDir.Right))
                {
                    if (currentPageMCS < (characterCurrencyInfos.Count - 1) / 10)
                    {
                        currentPageMCS++;
                        LoadSearchResultMCS();
                    }
                }

                ImGui.SameLine();
                if (ImGuiOm.ButtonIcon("MCSRefresh", FontAwesomeIcon.Sync)) searchTimerMCS.Restart();
                ImGui.EndGroup();

                var itemWidth = (int)ImGui.GetItemRectSize().X;

                foreach (var characterCurrencyInfo in charactersToShow)
                {
                    ImGui.SetNextItemWidth(itemWidth);
                    if (ImGui.BeginCombo($"##{characterCurrencyInfo.Character.Name}@{characterCurrencyInfo.Character.Server}", $"{characterCurrencyInfo.Character.Name}@{characterCurrencyInfo.Character.Server}", ImGuiComboFlags.HeightLarge))
                    {
                        ImGui.BeginGroup();
                        foreach (var currency in C.AllCurrencies)
                        {
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
                            ImGui.Image(C.AllCurrencyIcons[currency.Key].ImGuiHandle, ImGuiHelpers.ScaledVector2(16.0f));

                            ImGui.SameLine();
                            ImGui.Text($"{currency.Value}");
                        }
                        ImGui.EndGroup();

                        ImGui.SameLine();
                        ImGui.Spacing();

                        ImGui.SameLine();
                        ImGui.BeginGroup();
                        foreach (var currency in C.AllCurrencies)
                        {
                            var amount = characterCurrencyInfo.CurrencyAmount.GetValueOrDefault(currency.Key, 0);
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
                            ImGui.Text($"{amount:N0}");
                        }
                        ImGui.EndGroup();

                        ImGui.EndCombo();
                    }
                }
            }
        }
    }

    internal void LoadDataMCS()
    {
        Parallel.ForEach(C.CurrentActiveCharacter, character =>
        {
            var existingInfo = characterCurrencyInfos.GetOrAdd(character, new CharacterCurrencyInfo { Character = character });
            existingInfo.GetCharacterCurrencyAmount();
        });
    }

    private void LoadSearchResultMCS()
    {
        var contentId = P.CurrentCharacter.ContentID;
        var skipCount = currentPageMCS * 10;

        charactersToShow = characterCurrencyInfos.Values
            .Select(characterCurrencyInfo => new
            {
                Info = characterCurrencyInfo,
                IsCurrent = characterCurrencyInfo.Character.ContentID == contentId,
                MatchesFilter = string.IsNullOrEmpty(searchFilterMCS) ||
                                characterCurrencyInfo.Character.Name.Contains(searchFilterMCS, StringComparison.OrdinalIgnoreCase) ||
                                characterCurrencyInfo.Character.Server.Contains(searchFilterMCS, StringComparison.OrdinalIgnoreCase)
            })
            .Where(x => x.MatchesFilter)
            .OrderByDescending(x => x.IsCurrent)
            .Select(x => x.Info)
            .Skip(skipCount)
            .Take(10);
    }

    private void SearchTimerMCSElapsed(object? sender, ElapsedEventArgs e)
    {
        currentPageMCS = 0;
        LoadSearchResultMCS();
    }

}

