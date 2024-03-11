using System.Numerics;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    internal static uint selectedCurrencyID;
    internal int selectedOptionIndex = -1;
    private int currencyListboxWidth = 150;

    private void CurrencyListboxUI()
    {
        selectedOptionIndex = Service.Config.OrderedOptions.IndexOf(selectedCurrencyID);

        var style = ImGui.GetStyle();
        var childScale = new Vector2((180 * ImGuiHelpers.GlobalScale) + Service.Config.ChildWidthOffset, ImGui.GetContentRegionAvail().Y);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, style.Colors[(int)ImGuiCol.FrameBg]);
        using (var child = ImRaii.Child("CurrencyList", childScale, false, ImGuiWindowFlags.NoScrollbar))
        {
            if (child)
            {
                CurrencyListboxToolUI();

                ImGui.Separator();

                for (var i = 0; i < Service.Config.OrderedOptions.Count; i++)
                {
                    var option = Service.Config.OrderedOptions[i];
                    var currencyName = Service.Config.AllCurrencies[option];
                    if (ImGuiOm.SelectableImageWithText(Service.Config.AllCurrencyIcons[option].ImGuiHandle, ImGuiHelpers.ScaledVector2(20f), currencyName, i == selectedOptionIndex))
                    {
                        selectedCurrencyID = option;
                        currentTypeTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(selectedCurrencyID));
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
        if (index1 < 0 || index1 >= Service.Config.OrderedOptions.Count || index2 < 0 || index2 >= Service.Config.OrderedOptions.Count) return;

        (Service.Config.OrderedOptions[index2], Service.Config.OrderedOptions[index1]) = (Service.Config.OrderedOptions[index1], Service.Config.OrderedOptions[index2]);
        Service.Config.Save();
    }

    private void DeleteCustomCurrencyUI()
    {
        ImGui.BeginDisabled(selectedCurrencyID == 0 || Service.Config.PresetCurrencies.ContainsKey(selectedCurrencyID));
        ImGuiOm.ButtonIcon("ToolsDelete", FontAwesomeIcon.Trash, $"{Service.Lang.GetText("Delete")} ({Service.Lang.GetText("DoubleRightClick")})");
        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
        {
            var localName = CurrencyInfo.GetCurrencyLocalName(selectedCurrencyID);
            if (Service.Config.CustomCurrencies[selectedCurrencyID] != localName) P.CurrencySettings.RenameCurrencyHandler(localName);

            Service.Config.CustomCurrencies.Remove(selectedCurrencyID);
            Service.Config.Save();

            selectedCurrencyID = 0;
            ReloadOrderedOptions();
        }
        ImGui.EndDisabled();
    }

    private void CurrencySettingsUI()
    {
        ImGui.BeginDisabled(selectedCurrencyID == 0);
        if (ImGuiOm.ButtonIcon("CurrencySettings", FontAwesomeIcon.Cog))
        {
            P.CurrencySettings.IsOpen = !P.CurrencySettings.IsOpen;
        }
        ImGui.EndDisabled();
    }
}
