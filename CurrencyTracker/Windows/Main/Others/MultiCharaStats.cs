namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private readonly ConcurrentDictionary<CharacterInfo, CharacterCurrencyInfo> characterCurrencyInfos = new();
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
            if (popup)
            {
                ImGui.SetNextItemWidth(240f);
                if (ImGui.InputTextWithHint("##selectFilterMultiCharaStats", Service.Lang.GetText("PleaseSearch"), ref searchFilterMCS, 100))
                {
                    searchTimerMCS.Restart();
                }

                ImGui.SameLine();
                if (ImGui.ArrowButton("CustomPreviousPage", ImGuiDir.Left) && currentPageMCS > 0)
                {
                    currentPageMCS--;
                    LoadSearchResultMCS();
                }

                ImGui.SameLine();
                if (ImGui.ArrowButton("CustomNextPage", ImGuiDir.Right) && currentPageMCS < (characterCurrencyInfos.Count - 1) / 10)
                {
                    currentPageMCS++;
                    LoadSearchResultMCS();
                }

                ImGui.SameLine();
                if (IconButton(FontAwesomeIcon.Sync, "", "MCSRefresh"))
                {
                    searchTimerMCS.Restart();
                }

                foreach (var characterCurrencyInfo in charactersToShow)
                {
                    if (ImGui.BeginCombo($"##{characterCurrencyInfo.Character.Name}@{characterCurrencyInfo.Character.Server}", $"{characterCurrencyInfo.Character.Name}@{characterCurrencyInfo.Character.Server}", ImGuiComboFlags.HeightLarge))
                    {
                        foreach (var currency in C.AllCurrencies)
                        {
                            var amount = characterCurrencyInfo.CurrencyAmount.TryGetValue(currency.Key, out var value) ? value : 0;
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
                            ImGui.Image(C.AllCurrencyIcons[currency.Key].ImGuiHandle, ImGuiHelpers.ScaledVector2(16.0f));
                            ImGui.SameLine();
                            ImGui.Text($"{currency.Value} - {amount}");
                        }
                        ImGui.EndCombo();
                    }
                }
            }
        }
    }

    private void LoadDataMCS()
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
                MatchesFilter = searchFilterMCS.IsNullOrEmpty() ||
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

