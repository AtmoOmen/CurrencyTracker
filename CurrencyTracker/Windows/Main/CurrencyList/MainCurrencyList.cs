namespace CurrencyTracker.Windows;

public partial class Main
{
    internal uint selectedCurrencyID;
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

                for (var i = 0; i < C.OrderedOptions.Count; i++)
                {
                    var option = C.OrderedOptions[i];
                    var currencyName = C.AllCurrencies[option];
                    if (ImGuiOm.SelectableImageWithText(C.AllCurrencyIcons[option].ImGuiHandle, ImGuiHelpers.ScaledVector2(20f), currencyName, i == selectedOptionIndex))
                    {
                        selectedCurrencyID = option;
                        currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID));
                        currentView = TransactionFileCategory.Inventory;
                        currentViewID = 0;
                    }

                    ImGuiOm.TooltipHover(currencyName);

                    ImGui.SameLine();
                    ImGui.Text(currencyName);
                }
            }
        }
        ImGui.PopStyleColor();
    }

    private void CurrencyListboxToolUI()
    {
        ImGuiOm.CenterAlignFor(currencyListboxWidth);
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
            ImGuiOm.ButtonIcon("ToolsDelete", FontAwesomeIcon.Trash, isDeleteValid ? $"{Service.Lang.GetText("Delete")} ({Service.Lang.GetText("DoubleRightClick")})" : "");
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
            if (ImGuiOm.ButtonIcon("CurrencySettings", FontAwesomeIcon.Cog))
            {
                if (selectedCurrencyID != 0)
                {
                    P.CurrencySettings.IsOpen = !P.CurrencySettings.IsOpen;
                }
            }
        }
    }
}
