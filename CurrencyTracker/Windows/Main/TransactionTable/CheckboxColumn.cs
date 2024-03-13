using System.Collections.Generic;
using System.Linq;
using System.Text;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static bool isOnMergingTT;
    private static bool isOnEdit;
    internal static int checkboxColumnWidth = 22;

    private static void CheckboxColumnHeaderUI()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        if (ImGuiOm.ButtonIcon("CheckboxTools", FontAwesomeIcon.EllipsisH)) ImGui.OpenPopup("TableTools");
        ImGui.EndDisabled();

        using var popup = ImRaii.Popup("TableTools");
        if (popup.Success) CheckboxColumnToolUI();
    }

    private static void CheckboxColumnToolUI()
    {
        var selectedCount = selectedTransactions[SelectedCurrencyID].Count;
        ImGui.Text($"{Service.Lang.GetText("Now")}: {selectedCount} {Service.Lang.GetText("Transactions")}");
        ImGui.Separator();

        UnselectCBCTUI(selectedCount);
        SelectAllCBCTUI();
        InverseSelectCBCTUI();
        CopyCBCTUI(selectedCount);
        DeleteCBCTUI(selectedCount);
        ExportCBCTUI(selectedCount);
        MergeCBCTUI();
        EditCBCTUI();
    }

    // 取消选择 Unselect
    private static void UnselectCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Unselect")))
        {
            if (count <= 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            selectedStates[SelectedCurrencyID].Clear();
            selectedTransactions[SelectedCurrencyID].Clear();
        }
    }

    // 全选 Select All
    private static void SelectAllCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("SelectAll")))
        {
            selectedTransactions[SelectedCurrencyID] = new List<TransactionsConvertor>(currentTypeTransactions);
            selectedStates[SelectedCurrencyID] =
                Enumerable.Repeat(true, selectedStates[SelectedCurrencyID].Count).ToList();
        }
    }

    // 反选 Inverse Select
    private static void InverseSelectCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("InverseSelect")))
        {
            selectedStates[SelectedCurrencyID] = selectedStates[SelectedCurrencyID].Select(state => !state).ToList();

            var selectedSet =
                new HashSet<TransactionsConvertor>(selectedTransactions[SelectedCurrencyID], new TransactionComparer());

            selectedTransactions[SelectedCurrencyID] = currentTypeTransactions
                                                       .Where(transaction => !selectedSet.Contains(transaction))
                                                       .ToList();
        }
    }

    // 复制 Copy
    private static void CopyCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Copy")))
        {
            if (count <= 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var isCSV = Service.Config.ExportDataFileType == 0;
            var header = isCSV
                             ? Service.Lang.GetText("ExportFileCSVHeader")
                             : Service.Lang.GetText("ExportFileMDHeader1");
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(header);

            foreach (var record in selectedTransactions[SelectedCurrencyID])
            {
                var change = $"{record.Change:+ #,##0;- #,##0;0}";
                var line = isCSV
                               ? $"{record.TimeStamp},{record.Amount},{change},{record.LocationName},{record.Note}"
                               : $"| {record.TimeStamp} | {record.Amount} | {change} | {record.LocationName} | {record.Note}";
                stringBuilder.AppendLine(line);
            }

            ImGui.SetClipboardText(stringBuilder.ToString());
            Service.Chat.Print($"{Service.Lang.GetText("CopyTransactionsHelp", count)}");
        }
    }

    // 删除 Delete
    private static void DeleteCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Delete")))
        {
            if (count <= 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var filePath = TransactionsHandler.GetTransactionFilePath(SelectedCurrencyID, currentView, currentViewID);
            var editedTransactions =
                TransactionsHandler.LoadAllTransactions(SelectedCurrencyID, currentView, currentViewID);

            var selectedSet =
                new HashSet<TransactionsConvertor>(selectedTransactions[SelectedCurrencyID], new TransactionComparer());
            editedTransactions.RemoveAll(selectedSet.Contains);

            TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
            UpdateTransactions(SelectedCurrencyID, currentView, currentViewID);
        }
    }

    // 导出 Export
    private static void ExportCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Export")))
        {
            if (count <= 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var filePath = TransactionsHandler.ExportData(selectedTransactions[SelectedCurrencyID], "",
                                                          SelectedCurrencyID, Service.Config.ExportDataFileType, currentView,
                                                          currentViewID);
            Service.Chat.Print($"{Service.Lang.GetText("ExportFileMessage")} {filePath}");
        }
    }

    // 合并 Merge
    private static void MergeCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("Merge"), ref isOnMergingTT, ImGuiSelectableFlags.DontClosePopups))
        {
            if (isOnMergingTT)
            {
                var t1 = selectedTransactions[SelectedCurrencyID].FirstOrDefault(t => !string.IsNullOrEmpty(t.LocationName));
                editedLocationName = t1?.LocationName;

                var t2 = selectedTransactions[SelectedCurrencyID].FirstOrDefault(t => !string.IsNullOrEmpty(t.Note));
                editedNoteContent = t2?.Note;

                if (isOnEdit) isOnEdit = false;
            }
        }

        if (!isOnMergingTT) return;

        ImGui.Separator();

        ImGui.Text($"{Service.Lang.GetText("Location")}:");

        ImGui.SetNextItemWidth(210);
        ImGui.InputText("##MergeLocationName", ref editedLocationName, 80);

        ImGui.Text($"{Service.Lang.GetText("Note")}:");

        ImGui.SetNextItemWidth(210);
        ImGui.InputText("##MergeNoteContent", ref editedNoteContent, 150);

        if (ImGui.SmallButton(Service.Lang.GetText("Confirm")))
        {
            if (selectedTransactions[SelectedCurrencyID].Count < 2 || editedLocationName.IsNullOrWhitespace()) return;

            var mergeCount = TransactionsHandler.MergeSpecificTransactions(
                SelectedCurrencyID, editedLocationName, selectedTransactions[SelectedCurrencyID],
                string.IsNullOrEmpty(editedNoteContent) ? "-1" : editedNoteContent, currentView, currentViewID);
            Service.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");

            UpdateTransactions(SelectedCurrencyID, currentView, currentViewID);
            isOnMergingTT = false;
        }
    }

    // 编辑 Edit
    private static void EditCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("Edit"), ref isOnEdit, ImGuiSelectableFlags.DontClosePopups))
        {
            if (selectedTransactions[SelectedCurrencyID].Any())
            {
                if (isOnEdit)
                {
                    var t1 = selectedTransactions[SelectedCurrencyID]
                        .FirstOrDefault(t => !string.IsNullOrEmpty(t.LocationName));
                    editedLocationName = t1?.LocationName;

                    var t2 = selectedTransactions[SelectedCurrencyID].FirstOrDefault(t => !string.IsNullOrEmpty(t.Note));
                    editedNoteContent = t2?.Note;

                    if (isOnMergingTT) isOnMergingTT = !isOnMergingTT;
                }
            }
            else
            {
                isOnEdit = false;
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }
        }

        if (!isOnEdit) return;

        ImGui.Separator();

        ImGui.Text($"{Service.Lang.GetText("Location")}:");

        ImGui.SetNextItemWidth(210);
        if (ImGui.InputTextWithHint("##EditLocationName", Service.Lang.GetText("PressEnterToConfirm"),
                                    ref editedLocationName, 80, ImGuiInputTextFlags.EnterReturnsTrue))
            EditLocationName();

        ImGui.Text($"{Service.Lang.GetText("Note")}:");

        ImGui.SetNextItemWidth(210);
        if (ImGui.InputTextWithHint("##EditNoteContent", Service.Lang.GetText("PressEnterToConfirm"),
                                    ref editedNoteContent, 80, ImGuiInputTextFlags.EnterReturnsTrue)) EditNoteContent();

        if (!string.IsNullOrEmpty(editedNoteContent)) ImGui.TextWrapped(editedNoteContent);
    }

    // 编辑地名 Edit Location Name
    private static void EditLocationName()
    {
        if (editedLocationName.IsNullOrWhitespace()) return;

        var failCount = TransactionsHandler.EditSpecificTransactions(SelectedCurrencyID,
                                                                     selectedTransactions[SelectedCurrencyID],
                                                                     editedLocationName, "None", currentView,
                                                                     currentViewID);

        EditResultHandler(failCount, editedLocationName);
    }

    // 编辑备注 Edit Note Content
    private static void EditNoteContent()
    {
        if (editedNoteContent.IsNullOrWhitespace()) return;

        var failCount = TransactionsHandler.EditSpecificTransactions(SelectedCurrencyID,
                                                                     selectedTransactions[SelectedCurrencyID], "None",
                                                                     editedNoteContent, currentView, currentViewID);

        EditResultHandler(failCount, "", editedNoteContent);
    }

    // 编辑结果处理 Handle Edit Result
    private static void EditResultHandler(int failCount, string locationName = "", string noteContent = "")
    {
        if (failCount == 0)
        {
            Service.Chat.Print(
                Service.Lang.GetText("EditHelp1", selectedTransactions[SelectedCurrencyID].Count,
                                     string.IsNullOrEmpty(locationName)
                                         ? Service.Lang.GetText("Note")
                                         : Service.Lang.GetText("Location")) + " " +
                (string.IsNullOrEmpty(locationName) ? noteContent : locationName));

            UpdateTransactions(SelectedCurrencyID, currentView, currentViewID);
        }
        else if (failCount > 0 && failCount < selectedTransactions[SelectedCurrencyID].Count)
        {
            Service.Chat.Print(
                Service.Lang.GetText("EditHelp1", selectedTransactions[SelectedCurrencyID].Count - failCount,
                                     string.IsNullOrEmpty(locationName)
                                         ? Service.Lang.GetText("Note")
                                         : Service.Lang.GetText("Location")) + " " +
                (string.IsNullOrEmpty(locationName) ? noteContent : locationName));
            Service.Chat.PrintError($"({Service.Lang.GetText("EditFailed")}: {failCount})");

            UpdateTransactions(SelectedCurrencyID, currentView, currentViewID);
        }
        else
            Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");

        isOnEdit = false;
    }

    private static void CheckboxColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        if (ImGui.Checkbox($"##select_{i}", ref selected))
        {
            selectedStates[SelectedCurrencyID][i] = selected;

            if (selected)
            {
                var comparer = new TransactionComparer();
                var exists = selectedTransactions[SelectedCurrencyID].Any(t => comparer.Equals(t, transaction));

                if (!exists) selectedTransactions[SelectedCurrencyID].Add(transaction);
            }
            else
            {
                var comparer = new TransactionComparer();

                selectedTransactions[SelectedCurrencyID].RemoveAll(t => comparer.Equals(t, transaction));
            }
        }

        checkboxColumnWidth = (int)ImGui.GetItemRectSize().X;
    }
}
