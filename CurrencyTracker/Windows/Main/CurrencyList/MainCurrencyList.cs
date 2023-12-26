namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    internal uint selectedCurrencyID = 0;
    internal int selectedOptionIndex = -1;

    private void CurrencyListboxUI()
    {
        selectedOptionIndex = C.OrderedOptions.IndexOf(selectedCurrencyID);

        var childScale = new Vector2(243 + C.ChildWidthOffset, ChildframeHeightAdjust());
        if (ImGui.BeginChildFrame(2, childScale, ImGuiWindowFlags.NoScrollbar)) 
        {
            CurrencyListboxToolUI();

            ImGui.Separator();

            ImGui.SetNextItemWidth(235);
            var style = ImGui.GetStyle();
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, style.Colors[(int)ImGuiCol.HeaderHovered] with { W = 0.2f });
            ImGui.PushStyleColor(ImGuiCol.Header, style.Colors[(int)ImGuiCol.Header] with { W = 0.2f });
            for (var i = 0; i < C.OrderedOptions.Count; i++)
            {
                var option = C.OrderedOptions[i];
                var currencyName = C.AllCurrencies[option];
                if (ImGui.Selectable($"##{option}", i == selectedOptionIndex))
                {
                    selectedCurrencyID = option;
                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID));
                    currentView = TransactionFileCategory.Inventory;
                    currentViewID = 0;
                }

                HoverTooltip(currencyName);

                ImGui.SameLine(3.0f);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3.0f);
                ImGui.Image(C.AllCurrencyIcons[option].ImGuiHandle, ImGuiHelpers.ScaledVector2(20.0f));

                ImGui.SameLine();
                ImGui.Text(currencyName);
            }
            ImGui.PopStyleColor(2);
            ImGui.EndChildFrame();
        }
    }

    private void CurrencyListboxToolUI()
    {
        CenterCursorFor(184);

        AddCustomCurrencyUI();

        ImGui.SameLine();
        if (ImGui.ArrowButton("UpArrow", ImGuiDir.Up)) SwapOptions(selectedOptionIndex, selectedOptionIndex - 1);

        ImGui.SameLine();
        DeleteCustomCurrencyUI();

        ImGui.SameLine();
        if (ImGui.ArrowButton("DownArrow", ImGuiDir.Down)) SwapOptions(selectedOptionIndex, selectedOptionIndex + 1);

        ImGui.SameLine();
        CurrencySettingsUI();
    }

    private void SwapOptions(int index1, int index2)
    {
        if (index1 < 0 || index1 >= C.OrderedOptions.Count || index2 < 0 || index2 >= C.OrderedOptions.Count) return;

        (C.OrderedOptions[index2], C.OrderedOptions[index1]) = (C.OrderedOptions[index1], C.OrderedOptions[index2]);
        C.Save();
    }

    private void DeleteCustomCurrencyUI()
    {
        var isDeleteValid = selectedCurrencyID != 0 && !C.PresetCurrencies.ContainsKey(selectedCurrencyID);

        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, !isDeleteValid))
        {
            IconButton(FontAwesomeIcon.Trash, isDeleteValid ? $"{Service.Lang.GetText("Delete")} ({Service.Lang.GetText("DoubleRightClick")})" : "", "ToolsDelete");
        }
        if (isDeleteValid && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
        {
            var localName = CurrencyInfo.GetCurrencyLocalName(selectedCurrencyID);
            if (C.CustomCurrencies[selectedCurrencyID] != localName) RenameCurrencyHandler(localName);

            C.CustomCurrencies.Remove(selectedCurrencyID);
            C.Save();

            selectedCurrencyID = 0;
            ReloadOrderedOptions();
        }
    }        
}
