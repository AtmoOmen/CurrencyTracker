using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static readonly Dictionary<string, Action> ColumnHeaderActions = new()
    {
        { "Order", OrderColumnHeaderUI },
        { "Time", TimeColumnHeaderUI },
        { "Amount", AmountColumnHeaderUI },
        { "Change", ChangeColumnHeaderUI },
        { "Location", LocationColumnHeaderUI },
        { "Note", NoteColumnHeaderUI },
        { "Checkbox", CheckboxColumnHeaderUI }
    };

    private static readonly Dictionary<string, Action<int, DisplayTransaction>> ColumnCellActions = new()
    {
        { "Order", OrderColumnCellUI },
        { "Time", TimeColumnCellUI },
        { "Amount", AmountColumnCellUI },
        { "Change", ChangeColumnCellUI },
        { "Location", LocationColumnCellUI },
        { "Note", NoteColumnCellUI },
        { "Checkbox", CheckboxColumnCellUI }
    };

    internal static string[] visibleColumns = [];
    internal static List<DisplayTransaction> currentTypeTransactions = [];

    private static int currentPage;
    private static int visibleStartIndex;
    private static int visibleEndIndex;
    internal static TransactionFileCategory currentView = TransactionFileCategory.Inventory;
    internal static ulong currentViewID;
    private static int tablePagingComponentsWidth = 300;

    private static void TransactionTableUI()
    {
        if (SelectedCurrencyID == 0) return;

        var windowWidth = ImGui.GetContentRegionAvail().X - Service.Config.ChildWidthOffset - (185 * ImGuiHelpers.GlobalScale);

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);
        if (ImGui.BeginChild("TransactionsTable", new Vector2(windowWidth, ImGui.GetContentRegionAvail().Y),
                             false, ImGuiWindowFlags.NoScrollbar))
        {
            TransactionTablePagingUI(windowWidth);

            if (visibleColumns.Length == 0) return;

            ImGui.SetCursorPosX(5);
            if (ImGui.BeginTable("Transaction", visibleColumns.Length,
                                 ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
                                 ImGuiTableFlags.Resizable, new Vector2(windowWidth - 10, 1)))
            {
                SetupTableColumns(visibleColumns);

                if (currentTypeTransactions.Count > 0)
                {
                    for (var i = visibleStartIndex; i < visibleEndIndex; i++)
                    {
                        ImGui.TableNextRow();
                        foreach (var column in visibleColumns)
                        {
                            ImGui.TableNextColumn();
                            ColumnCellActions[column].Invoke(i, currentTypeTransactions[i]);
                        }
                    }
                }

                ImGui.EndTable();
            }

            TransactionTableInfoBarUI();

            ImGui.EndChild();
        }
        ImGui.PopStyleColor();
    }

    private static void SetupTableColumns(string[] columns)
    {
        var orderColumnWidth = ImGui.CalcTextSize((currentTypeTransactions.Count + 1).ToString()).X + 10;

        foreach (var column in columns)
        {
            var flags = column is "Order" or "Checkbox"
                            ? ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize
                            : ImGuiTableColumnFlags.None;
            var width = column switch
            {
                "Order" => orderColumnWidth,
                "Checkbox" => checkboxColumnWidth,
                _ => 150
            };
            ImGui.TableSetupColumn(column, flags, width, 0);
        }

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        foreach (var column in columns)
        {
            ImGui.TableNextColumn();
            ColumnHeaderActions[column].Invoke();
        }
    }

    private static void TransactionTablePagingUI(float windowWidth)
    {
        var pageCount = currentTypeTransactions.Count != 0
                            ? (int)Math.Ceiling((double)currentTypeTransactions.Count / Service.Config.RecordsPerPage)
                            : 0;
        currentPage = pageCount > 0 ? Math.Clamp(currentPage, 0, pageCount - 1) : 0;

        ImGuiOm.CenterAlignFor(tablePagingComponentsWidth);
        ImGui.BeginGroup();

        TableViewSwitchUI();

        ImGui.SameLine();
        ImGui.BeginDisabled(pageCount <= 0);
        if (ImGuiOm.ButtonIcon("FirstPageTransactionTable", FontAwesomeIcon.Backward)) currentPage = 0;
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.BeginDisabled(currentPage <= 0);
        if (ImGui.ArrowButton("PreviousPage", ImGuiDir.Left)) currentPage--;
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.Text($"{Service.Lang.GetText("PageComponent", currentPage + 1, pageCount)}");

        ImGui.SameLine();
        ImGui.BeginDisabled(currentPage >= pageCount - 1);
        if (ImGui.ArrowButton("NextPage", ImGuiDir.Right)) currentPage++;
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.BeginDisabled(pageCount <= 0);
        if (ImGuiOm.ButtonIcon("LastPageTransactionPage", FontAwesomeIcon.Forward)) currentPage = pageCount;
        ImGui.EndDisabled();

        ImGui.SameLine();
        TableAppearanceUI(windowWidth);

        ImGui.EndGroup();
        tablePagingComponentsWidth = (int)ImGui.GetItemRectSize().X;

        visibleStartIndex = currentPage * Service.Config.RecordsPerPage;
        visibleEndIndex = Math.Min(visibleStartIndex + Service.Config.RecordsPerPage, currentTypeTransactions.Count);

        // 鼠标滚轮控制 Logic controlling Mouse Wheel Flipping
        if (!ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
        {
            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel > 0 &&
                currentPage > 0)
                currentPage--;

            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel < 0 &&
                currentPage < pageCount - 1)
                currentPage++;
        }
    }

    private static void TableViewSwitchUI()
    {
        if (ImGuiOm.ButtonIcon("TableViewSwitch", FontAwesomeIcon.Bars)) ImGui.OpenPopup("TableViewSwitch");

        using var popup = ImRaii.Popup("TableViewSwitch");
        if (popup.Success)
        {
            const bool boolUI = false;
            if (ImGui.Selectable(Service.Lang.GetText("Inventory"), boolUI, ImGuiSelectableFlags.DontClosePopups))
            {
                currentTypeTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(SelectedCurrencyID)).ToDisplayTransaction();
            }

            foreach (var retainer in Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID])
                if (ImGui.Selectable($"{retainer.Value}##{retainer.Key}", boolUI,
                                     ImGuiSelectableFlags.DontClosePopups))
                {
                    currentTypeTransactions =
                        ApplyFilters(TransactionsHandler.LoadAllTransactions(
                                         SelectedCurrencyID, TransactionFileCategory.Retainer, retainer.Key)).Select(transaction => new DisplayTransaction
                        {
                            Transaction = transaction,
                            Selected = false
                        }).ToList();;

                    currentView = TransactionFileCategory.Retainer;
                    currentViewID = retainer.Key;
                }

            if (ImGui.Selectable(Service.Lang.GetText("SaddleBag"), boolUI, ImGuiSelectableFlags.DontClosePopups))
            {
                currentTypeTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(SelectedCurrencyID, TransactionFileCategory.SaddleBag)).ToDisplayTransaction();
                currentView = TransactionFileCategory.SaddleBag;
                currentViewID = 0;
            }

            if (ImGui.Selectable(Service.Lang.GetText("PSaddleBag"), boolUI, ImGuiSelectableFlags.DontClosePopups))
            {
                currentTypeTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(SelectedCurrencyID,
                                                           TransactionFileCategory.PremiumSaddleBag)).ToDisplayTransaction();
            }
        }
    }

    private static void TableAppearanceUI(float windowWidth)
    {
        if (ImGuiOm.ButtonIcon("TableAppearance", FontAwesomeIcon.Table, Service.Lang.GetText("TableAppearance")))
            ImGui.OpenPopup("TableAppearance");

        using var popup = ImRaii.Popup("TableAppearance");
        if (popup.Success)
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ColumnsDisplayed")}:");

            ImGui.BeginGroup();
            using (var table = ImRaii.Table("##ColumnsDisplay", 4, ImGuiTableFlags.NoBordersInBody))
            {
                if (table)
                {
                    foreach (var column in Service.Config.ColumnsVisibility.Keys)
                    {
                        ImGui.TableNextColumn();
                        ColumnDisplayCheckbox(column);
                    }
                }
            }

            ImGui.EndGroup();

            var tableWidth = ImGui.GetItemRectSize().X;
            var textWidthOffset = $"{Service.Lang.GetText("ChildframeWidthOffset")}:";
            var widthWidthOffset = tableWidth - ImGui.CalcTextSize(textWidthOffset).X;
            var textPerPage = $"{Service.Lang.GetText("TransactionsPerPage")}:";
            var widthPerPage = tableWidth - ImGui.CalcTextSize(textPerPage).X;

            ImGui.Separator();

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, textWidthOffset);

            var childWidthOffset = Service.Config.ChildWidthOffset;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(widthWidthOffset);
            if (ImGui.InputInt("##ChildFrameWidthOffset", ref childWidthOffset, 10))
            {
                childWidthOffset = Math.Max(-240, Math.Min(childWidthOffset, (int)windowWidth - 700));
                Service.Config.ChildWidthOffset = childWidthOffset;
                Service.Config.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, textPerPage);

            var transactionsPerPage = Service.Config.RecordsPerPage;
            ImGui.SetNextItemWidth(widthPerPage);
            ImGui.SameLine();
            if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
            {
                transactionsPerPage = Math.Max(transactionsPerPage, 1);
                Service.Config.RecordsPerPage = transactionsPerPage;
                Service.Config.Save();
            }
        }
    }

    private static void ColumnDisplayCheckbox(string boolName)
    {
        var isShowColumn = Service.Config.ColumnsVisibility[boolName];
        if (ImGui.Checkbox($"{Service.Lang.GetText(boolName)}##Display{boolName}Column", ref isShowColumn))
        {
            Service.Config.ColumnsVisibility[boolName] = isShowColumn;
            Service.Config.Save();

            var tempList = new List<string>();
            foreach (var column in Service.Config.ColumnsVisibility)
                if (column.Value)
                    tempList.Add(column.Key);

            visibleColumns = [.. tempList];
        }
    }

    private static void TransactionTableInfoBarUI()
    {
        var selectedTransactions = currentTypeTransactions.Where(x => x.Selected).ToList();

        if (selectedTransactions.Count != 0)
        {
            var count = selectedTransactions.Count;
            var sum = selectedTransactions.Sum(x => x.Transaction.Change);
            var avg = Math.Round((double)sum / count, 2);
            var max = selectedTransactions.Max(x => x.Transaction.Change);
            var min = selectedTransactions.Min(x => x.Transaction.Change);

            ImGui.Spacing();

            ImGui.SameLine();
            ImGui.TextDisabled(Service.Lang.GetText("SelectedTransactionsInfo", count, sum, avg, max, min));
        }
    }
}
