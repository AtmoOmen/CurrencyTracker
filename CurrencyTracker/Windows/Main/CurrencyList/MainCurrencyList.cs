namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    internal uint selectedCurrencyID = 0;
    internal int selectedOptionIndex = -1;
    private int currencyListboxWidth = 150;

    private void CurrencyListboxUI()
    {
        selectedOptionIndex = C.OrderedOptions.IndexOf(selectedCurrencyID);

        var style = ImGui.GetStyle();
        var childScale = new Vector2((180 * ImGuiHelpers.GlobalScale) + C.ChildWidthOffset, ImGui.GetContentRegionAvail().Y);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, style.Colors[(int)ImGuiCol.FrameBg]);
        using (var child = ImRaii.Child("CurrencyList", childScale, false, ImGuiWindowFlags.NoScrollbar))
        {
            if (child)
            {
                CurrencyListboxToolUI();

                ImGui.Separator();

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
            }
        }
        ImGui.PopStyleColor(3);
    }

    private void CurrencyListboxToolUI()
    {
        CenterCursorFor(currencyListboxWidth);
        ImGui.BeginGroup();
        AddCustomCurrencyUI();

        ImGui.SameLine();
        if (ImGui.ArrowButton("UpArrow", ImGuiDir.Up)) SwapOptions(selectedOptionIndex, selectedOptionIndex - 1);

        ImGui.SameLine();
        DeleteCustomCurrencyUI();

        ImGui.SameLine();
        if (ImGui.ArrowButton("DownArrow", ImGuiDir.Down)) SwapOptions(selectedOptionIndex, selectedOptionIndex + 1);

        ImGui.SameLine();
        CurrencySettingsUI();
        ImGui.EndGroup();

        currencyListboxWidth = (int)ImGui.GetItemRectSize().X;
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
            if (C.CustomCurrencies[selectedCurrencyID] != localName) P.CurrencySettings.RenameCurrencyHandler(localName);

            C.CustomCurrencies.Remove(selectedCurrencyID);
            C.Save();

            selectedCurrencyID = 0;
            ReloadOrderedOptions();
        }
    }

    private void CurrencySettingsUI()
    {
        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, selectedCurrencyID != 0 ? 1f : 0.5f))
        {
            if (IconButton(FontAwesomeIcon.Cog, "", "CurrencySettings"))
            {
                if (selectedCurrencyID != 0)
                {
                    P.CurrencySettings.IsOpen = true;
                }
            }
        }
    }
}
