using System.Numerics;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static int _dragDropIndex = -1;

    private static unsafe void CurrencyListboxUI()
    {
        var style = ImGui.GetStyle();
        var childScale = new Vector2((180 * ImGuiHelpers.GlobalScale) + Service.Config.ChildWidthOffset,
                                     ImGui.GetContentRegionAvail().Y);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, style.Colors[(int)ImGuiCol.FrameBg]);
        if (ImGui.BeginChild("CurrencyList", childScale, false, ImGuiWindowFlags.NoScrollbar))
        {
            CurrencyListboxToolUI();

            ImGui.Separator();

            for (var i = 0; i < Service.Config.OrderedOptions.Count; i++)
            {
                var id = Service.Config.OrderedOptions[i];
                var currencyName = Service.Config.AllCurrencies[id];
                var currencyIcon = Service.Config.AllCurrencyIcons[id].ImGuiHandle;

                ImGui.PushID(id.ToString());
                ImGui.Indent(3f);
                if (ImGuiOm.SelectableImageWithText(currencyIcon, ImGuiHelpers.ScaledVector2(20f), currencyName,
                                                    id == SelectedCurrencyID))
                    LoadCurrencyTransactions(id);

                ImGui.Unindent(3f);

                if (ImGui.BeginDragDropSource())
                {
                    if (ImGui.SetDragDropPayload("CurrencyListReorder", nint.Zero, 0)) _dragDropIndex = i;

                    ImGui.TextColored(ImGuiColors.DalamudYellow, currencyName);

                    ImGui.EndDragDropSource();
                }

                if (ImGui.BeginDragDropTarget())
                {
                    if (_dragDropIndex >= 0 || ImGui.AcceptDragDropPayload("CurrencyListReorder").NativePtr != null)
                    {
                        SwapOptions(_dragDropIndex, i);
                        _dragDropIndex = -1;
                    }

                    ImGui.EndDragDropTarget();
                }

                if (ImGui.BeginPopupContextItem())
                {
                    var imageSize = ImGuiHelpers.ScaledVector2(20f);
                    ImGui.Image(currencyIcon, imageSize);

                    ImGui.SameLine();
                    ImGui.SetCursorPosY(10f);
                    ImGui.Text($"{currencyName} ({id})");

                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }

            ImGui.EndChild();
        }

        ImGui.PopStyleColor();
    }

    public static void LoadCurrencyTransactions(
        uint ID, TransactionFileCategory view = TransactionFileCategory.Inventory, ulong viewID = 0)
    {
        SelectedCurrencyID = ID;
        currentTypeTransactions =
            ApplyFilters(TransactionsHandler.LoadAllTransactions(SelectedCurrencyID));
        currentView = view;
        currentViewID = 0;
    }

    private static void CurrencyListboxToolUI()
    {
        var buttonWidth = ImGui.GetContentRegionMax().X / 3;
        AddCustomCurrencyUI(buttonWidth);

        ImGui.SameLine(0, 0);
        DeleteCustomCurrencyUI(buttonWidth);

        ImGui.SameLine(0, 0);
        CurrencySettingsUI(buttonWidth);
    }

    private static void SwapOptions(int index1, int index2)
    {
        if (index1 < 0 || index1 >= Service.Config.OrderedOptions.Count || index2 < 0 ||
            index2 >= Service.Config.OrderedOptions.Count) return;

        (Service.Config.OrderedOptions[index2], Service.Config.OrderedOptions[index1]) =
            (Service.Config.OrderedOptions[index1], Service.Config.OrderedOptions[index2]);

        TaskManager.Abort();
        TaskManager.DelayNext(500);
        TaskManager.Enqueue(Service.Config.Save);
    }

    private static void DeleteCustomCurrencyUI(float buttonWidth)
    {
        ImGui.BeginDisabled(
            SelectedCurrencyID == 0 || Service.Config.PresetCurrencies.ContainsKey(SelectedCurrencyID));

        ButtonIconSelectable("DeleteCurrency", buttonWidth, FontAwesomeIcon.Trash,
                             $"{Service.Lang.GetText("Delete")} ({Service.Lang.GetText("DoubleRightClick")})");

        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
        {
            var localName = CurrencyInfo.GetCurrencyLocalName(SelectedCurrencyID);
            if (Service.Config.CustomCurrencies[SelectedCurrencyID] != localName)
                P.CurrencySettings.RenameCurrencyHandler(localName);

            Service.Config.CustomCurrencies.Remove(SelectedCurrencyID);
            Service.Config.Save();

            SelectedCurrencyID = 0;
            ReloadOrderedOptions();
        }

        ImGui.EndDisabled();
    }

    private static void CurrencySettingsUI(float buttonWidth)
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0);
        if (ButtonIconSelectable("CurrencySettings", buttonWidth, FontAwesomeIcon.Cog))
            P.CurrencySettings.IsOpen ^= true;
        ImGui.EndDisabled();
    }

    public static bool ButtonIconSelectable(string id, float buttonWidth, FontAwesomeIcon icon, string tooltip = "")
    {
        ImGui.PushID(id);

        var style = ImGui.GetStyle();
        var padding = style.FramePadding.X;

        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        ImGui.PushFont(UiBuilder.IconFont);
        var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{id}",
                                  new Vector2(buttonWidth, ImGui.CalcTextSize(icon.ToIconString()).Y + (2 * padding)));
        ImGui.PopFont();
        ImGui.PopStyleColor();

        if (!tooltip.IsNullOrEmpty()) ImGuiOm.TooltipHover(tooltip);

        ImGui.PopID();

        return result;
    }
}
