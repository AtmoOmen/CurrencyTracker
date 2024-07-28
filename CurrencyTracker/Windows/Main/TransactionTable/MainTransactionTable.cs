using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
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
    private static readonly Dictionary<Type, TableColumn?> TableColumns = new()
    {
        { typeof(OrderColumn),    null },
        { typeof(TimeColumn),     null },
        { typeof(AmountColumn),   null },
        { typeof(ChangeColumn),   null },
        { typeof(LocationColumn), null },
        { typeof(NoteColumn),     null },
        { typeof(CheckboxColumn), null },
    };

    internal static List<DisplayTransaction> currentTransactions = [];

    private static int currentPage;
    private static int visibleStartIndex;
    private static int visibleEndIndex;
    internal static TransactionFileCategory currentView = TransactionFileCategory.Inventory;
    internal static ulong currentViewID;
    private static int tablePagingComponentsWidth = 300;

    private static void TransactionTableUI()
    {
        if (SelectedCurrencyID == 0) return;

        var windowWidth = ImGui.GetContentRegionAvail().X - Service.Config.ChildWidthOffset -
                          (185 * ImGuiHelpers.GlobalScale);

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);
        if (ImGui.BeginChild("TransactionsTable", new(windowWidth, ImGui.GetContentRegionAvail().Y), false,
                             ImGuiWindowFlags.NoScrollbar))
        {
            TransactionTablePagingUI(windowWidth);


            ImGui.SetCursorPosX(5);
            CreateTableColumnsInstance();
            if (ImGui.BeginTable("TransactionTable", TableColumns.Values.Count(x => x.IsVisible),
                                 ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable,
                                 new(windowWidth - 10, 1)))
            {
                SetupTableColumns();

                DrawTableHeaders();

                DrawTableCells();

                ImGui.EndTable();
            }

            TransactionTableInfoBarUI();

            ImGui.EndChild();
        }

        ImGui.PopStyleColor();
    }

    private static void CreateTableColumnsInstance()
    {
        foreach (var (type, column) in TableColumns)
            if (column == null)
                TableColumns[type] = (TableColumn?)Activator.CreateInstance(type);
    }

    private static void SetupTableColumns()
    {
        foreach (var (_, column) in TableColumns)
            ImGui.TableSetupColumn(column.ToString(), column.ColumnFlags, column.ColumnWidthOrWeight);
    }

    private static void DrawTableHeaders()
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        foreach (var column in TableColumns.Values)
        {
            ImGui.TableNextColumn();
            column.Header();
        }
    }

    private static void DrawTableCells()
    {
        if (currentTransactions.Count <= 0) return;

        for (var i = visibleStartIndex; i < visibleEndIndex; i++)
        {
            ImGui.TableNextRow();
            foreach (var column in TableColumns.Values)
            {
                if (!column.IsVisible) continue;
                ImGui.TableNextColumn();

                column.Cell(i, currentTransactions[i]);
            }
        }
    }

    private static void TransactionTablePagingUI(float windowWidth)
    {
        var pageCount = currentTransactions.Count != 0
                            ? (int)Math.Ceiling((double)currentTransactions.Count / Service.Config.RecordsPerPage)
                            : 0;
        currentPage = pageCount > 0 ? Math.Clamp(currentPage, 0, pageCount - 1) : 0;

        ImGuiOm.CenterAlignFor(tablePagingComponentsWidth);
        ImGui.BeginGroup();

        TableViewSwitchUI();

        ImGui.SameLine();
        ImGui.BeginDisabled(pageCount <= 0);
        if (ImGuiOm.ButtonIcon("FirstPageTransactionTable", FontAwesomeIcon.Backward, "", true)) currentPage = 0;
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
        if (ImGuiOm.ButtonIcon("LastPageTransactionPage", FontAwesomeIcon.Forward, "", true)) currentPage = pageCount;
        ImGui.EndDisabled();

        ImGui.SameLine();
        TableAppearanceUI(windowWidth);

        ImGui.EndGroup();
        tablePagingComponentsWidth = (int)ImGui.GetItemRectSize().X;

        visibleStartIndex = currentPage * Service.Config.RecordsPerPage;
        visibleEndIndex = Math.Min(visibleStartIndex + Service.Config.RecordsPerPage, currentTransactions.Count);

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
        if (ImGuiOm.ButtonIcon("TableViewSwitch", FontAwesomeIcon.Bars, "", true)) ImGui.OpenPopup("TableViewSwitch");

        using var popup = ImRaii.Popup("TableViewSwitch");
        if (!popup.Success) return;

        const bool boolUI = false;
        if (ImGui.Selectable(Service.Lang.GetText("Inventory"), boolUI, ImGuiSelectableFlags.DontClosePopups))
            currentTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(SelectedCurrencyID))
                .ToDisplayTransaction();

        foreach (var retainer in Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID])
            if (ImGui.Selectable($"{retainer.Value}##{retainer.Key}", boolUI,
                                 ImGuiSelectableFlags.DontClosePopups))
            {
                currentTransactions =
                    ApplyFilters(TransactionsHandler.LoadAllTransactions(
                                     SelectedCurrencyID, TransactionFileCategory.Retainer, retainer.Key))
                        .Select(transaction => new DisplayTransaction
                        {
                            Transaction = transaction,
                            Selected = false
                        }).ToList();

                currentView = TransactionFileCategory.Retainer;
                currentViewID = retainer.Key;
            }

        if (ImGui.Selectable(Service.Lang.GetText("SaddleBag"), boolUI, ImGuiSelectableFlags.DontClosePopups))
        {
            currentTransactions =
                ApplyFilters(
                        TransactionsHandler.LoadAllTransactions(SelectedCurrencyID, TransactionFileCategory.SaddleBag))
                    .ToDisplayTransaction();
            currentView = TransactionFileCategory.SaddleBag;
            currentViewID = 0;
        }

        if (ImGui.Selectable(Service.Lang.GetText("PSaddleBag"), boolUI, ImGuiSelectableFlags.DontClosePopups))
        {
            currentTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(SelectedCurrencyID,
                                                       TransactionFileCategory.PremiumSaddleBag))
                .ToDisplayTransaction();
        }
    }

    private static void TableAppearanceUI(float windowWidth)
    {
        if (ImGuiOm.ButtonIcon("TableAppearance", FontAwesomeIcon.Table, Service.Lang.GetText("TableAppearance"), true))
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
        }
    }

    private static void TransactionTableInfoBarUI()
    {
        var selectedTransactions = currentTransactions.Where(x => x.Selected).ToList();

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
