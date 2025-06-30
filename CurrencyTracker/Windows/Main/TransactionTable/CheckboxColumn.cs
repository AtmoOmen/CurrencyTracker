using System.Collections;
using System.Linq;
using System.Text;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public class CheckboxColumn : TableColumn
{
    public override ImGuiTableColumnFlags ColumnFlags { get; protected set; } =
        ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize;

    public override float ColumnWidthOrWeight { get; protected set; }
    public static   float CheckboxWidth       { get; private set; } = 22f;

    private static bool isOnMergingTT;
    private static bool isOnEdit;

    private static string editedNoteContent = string.Empty;
    private static string editedLocationContent = string.Empty;

    public override void Header()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || CurrentTransactions.Count <= 0);
        if (ImGuiOm.ButtonIcon("CheckboxTools", FontAwesomeIcon.EllipsisH, "", true)) ImGui.OpenPopup("TableTools");
        ImGui.EndDisabled();

        if (ImGui.BeginPopup("TableTools"))
        {
            CheckboxColumnToolUI();
            ImGui.EndPopup();
        }
    }

    public override void Cell(int i, DisplayTransaction transaction)
    {
        if (i < 0) return;
        var selected = transaction.Selected;
        if (ImGui.Checkbox($"##select_{i}", ref selected)) transaction.Selected = selected;

        ColumnWidthOrWeight = CheckboxWidth = (int)ImGui.GetItemRectSize().X;
    }

    internal static void CheckboxColumnToolUI()
    {
        var selectedCount = CurrentTransactions.Count(x => x.Selected);
        ImGui.Text($"{Service.Lang.GetText("Now")}: {selectedCount} {Service.Lang.GetText("Transactions")}");
        ImGui.Separator();

        UnselectCBCTUI();
        SelectAllCBCTUI();
        InverseSelectCBCTUI();
        CopyCBCTUI(selectedCount);
        DeleteCBCTUI(selectedCount);
        ExportCBCTUI(selectedCount);
        MergeCBCTUI();
        EditCBCTUI();
    }

    // 取消选择 Unselect
    private static void UnselectCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("Unselect")))
            Main.currentTransactions.ForEach(x => x.Selected = false);
    }

    // 全选 Select All
    private static void SelectAllCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("SelectAll")))
            Main.currentTransactions.ForEach(x => x.Selected = true);
    }

    // 反选 Inverse Select
    private static void InverseSelectCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("InverseSelect")))
            Main.currentTransactions.ForEach(x => x.Selected ^= true);
    }

    // 复制 Copy
    private static void CopyCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Copy")))
        {
            if (count <= 0)
            {
                DService.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var isCSV = Service.Config.ExportDataFileType == 0;
            var header = isCSV
                             ? Service.Lang.GetText("ExportFileCSVHeader")
                             : Service.Lang.GetText("ExportFileMDHeader1");
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(header);

            var selectedTransactions = CurrentTransactions.Where(x => x.Selected).ToList();

            foreach (var record in selectedTransactions)
            {
                var change = $"{record.Transaction.Change:+ #,##0;- #,##0;0}";
                var line = isCSV
                               ? $"{record.Transaction.TimeStamp},{record.Transaction.Amount},{change},{record.Transaction.LocationName},{record.Transaction.Note}"
                               : $"| {record.Transaction.TimeStamp} | {record.Transaction.Amount} | {change} | {record.Transaction.LocationName} | {record.Transaction.Note}";
                stringBuilder.AppendLine(line);
            }

            ImGui.SetClipboardText(stringBuilder.ToString());
            DService.Chat.Print($"{Service.Lang.GetText("CopyTransactionsHelp", count)}");
        }
    }

    // 删除 Delete
    private static void DeleteCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Delete")))
        {
            if (count <= 0)
            {
                DService.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var filePath = TransactionsHandler.GetTransactionFilePath(SelectedCurrencyID, CurrentView, CurrentViewID);
            var editedTransactions =
                TransactionsHandler.LoadAllTransactions(SelectedCurrencyID, CurrentView, CurrentViewID);
            var selectedSet = CurrentTransactions.Where(x => x.Selected).Select(x => x.Transaction).ToHashSet();
            editedTransactions.RemoveAll(selectedSet.Contains);

            Transaction.WriteTransactionsToFile(filePath, editedTransactions);
            Main.UpdateTransactions(SelectedCurrencyID, CurrentView, CurrentViewID);
        }
    }

    // 导出 Export
    private static void ExportCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Export")))
        {
            if (count <= 0)
            {
                DService.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var selectedTransactions = CurrentTransactions.Where(x => x.Selected).Select(x => x.Transaction).ToList();
            var filePath = TransactionsHandler.ExportData(selectedTransactions, "",
                                                          SelectedCurrencyID, Service.Config.ExportDataFileType,
                                                          CurrentView,
                                                          CurrentViewID);
            DService.Chat.Print($"{Service.Lang.GetText("ExportFileMessage")} {filePath}");
        }
    }

    // 合并 Merge
    private static void MergeCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("Merge"), ref isOnMergingTT, ImGuiSelectableFlags.DontClosePopups))
        {
            if (isOnMergingTT)
            {
                var selectedTransactions =
                    CurrentTransactions.Where(x => x.Selected).Select(x => x.Transaction).ToList();

                var t1 = selectedTransactions.FirstOrDefault(t => !string.IsNullOrEmpty(t.LocationName));
                editedLocationContent = t1?.LocationName;

                var t2 = selectedTransactions.FirstOrDefault(t => !string.IsNullOrEmpty(t.Note));
                editedNoteContent = t2?.Note;

                if (isOnEdit) isOnEdit = false;
            }
        }

        if (!isOnMergingTT) return;

        ImGui.Separator();

        ImGui.Text($"{Service.Lang.GetText("Location")}:");

        ImGui.SetNextItemWidth(210);
        ImGui.InputText("##MergeLocationName", ref editedLocationContent, 80);

        ImGui.Text($"{Service.Lang.GetText("Note")}:");

        ImGui.SetNextItemWidth(210);
        ImGui.InputText("##MergeNoteContent", ref editedNoteContent, 150);

        if (ImGui.SmallButton(Service.Lang.GetText("Confirm")))
        {
            var selectedTransactions = CurrentTransactions.Where(x => x.Selected).Select(x => x.Transaction).ToList();
            if (selectedTransactions.Count < 2 || editedLocationContent.IsNullOrWhitespace()) return;

            var mergeCount = TransactionsHandler.MergeSpecificTransactions(
                SelectedCurrencyID, editedLocationContent, selectedTransactions,
                string.IsNullOrEmpty(editedNoteContent) ? "-1" : editedNoteContent, CurrentView, CurrentViewID);
            DService.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");

            Main.UpdateTransactions(SelectedCurrencyID, CurrentView, CurrentViewID);
            isOnMergingTT = false;
        }
    }

    // 编辑 Edit
    private static void EditCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("Edit"), ref isOnEdit, ImGuiSelectableFlags.DontClosePopups))
        {
            var selectedTransactions = CurrentTransactions.Where(x => x.Selected).Select(x => x.Transaction).ToList();
            if (selectedTransactions.Count > 0)
            {
                if (isOnEdit)
                {
                    var t1 = selectedTransactions.FirstOrDefault(t => !string.IsNullOrEmpty(t.LocationName));
                    editedLocationContent = t1?.LocationName;

                    var t2 = selectedTransactions.FirstOrDefault(t => !string.IsNullOrEmpty(t.Note));
                    editedNoteContent = t2?.Note;

                    if (isOnMergingTT) isOnMergingTT = !isOnMergingTT;
                }
            }
            else
            {
                isOnEdit = false;
                DService.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }
        }

        if (!isOnEdit) return;

        ImGui.Separator();

        ImGui.Text($"{Service.Lang.GetText("Location")}:");

        ImGui.SetNextItemWidth(210);
        if (ImGui.InputTextWithHint("##EditLocationName", Service.Lang.GetText("PressEnterToConfirm"),
                                    ref editedLocationContent, 80, ImGuiInputTextFlags.EnterReturnsTrue))
            EditLocationName();

        ImGui.Text($"{Service.Lang.GetText("Note")}:");

        ImGui.SetNextItemWidth(210);
        if (ImGui.InputTextWithHint("##EditNoteContent", Service.Lang.GetText("PressEnterToConfirm"),
                                    ref editedNoteContent, 80, ImGuiInputTextFlags.EnterReturnsTrue))
            EditNoteContent();

        if (!string.IsNullOrEmpty(editedNoteContent)) ImGui.TextWrapped(editedNoteContent);
    }

    // 编辑地名 Edit Location Name
    private static void EditLocationName()
    {
        if (editedLocationContent.IsNullOrWhitespace()) return;

        var selectedTransactions = CurrentTransactions.Where(x => x.Selected).Select(x => x.Transaction).ToList();
        var failCount = TransactionsHandler.EditSpecificTransactions(SelectedCurrencyID,
                                                                     selectedTransactions,
                                                                     editedLocationContent, "None",
                                                                     CurrentView, CurrentViewID);

        EditResultHandler(selectedTransactions, failCount, editedLocationContent);
    }

    // 编辑备注 Edit Note Content
    private static void EditNoteContent()
    {
        if (editedNoteContent.IsNullOrWhitespace()) return;

        var selectedTransactions = CurrentTransactions.Where(x => x.Selected).Select(x => x.Transaction).ToList();
        var failCount = TransactionsHandler.EditSpecificTransactions(SelectedCurrencyID,
                                                                     selectedTransactions,
                                                                     "None", editedNoteContent,
                                                                     CurrentView, CurrentViewID);

        EditResultHandler(selectedTransactions, failCount, "", editedNoteContent);
    }

    // 编辑结果处理 Handle Edit Result
    private static void EditResultHandler(
        ICollection selectedTransactions, int failCount, string locationName = "", string noteContent = "")
    {
        switch (failCount)
        {
            case 0:
                DService.Chat.Print(
                    Service.Lang.GetText("EditHelp1", selectedTransactions.Count,
                                         string.IsNullOrEmpty(locationName)
                                             ? Service.Lang.GetText("Note")
                                             : Service.Lang.GetText("Location")) + " " +
                    (string.IsNullOrEmpty(locationName) ? noteContent : locationName));

                Main.UpdateTransactions(SelectedCurrencyID, CurrentView, CurrentViewID);
                break;
            case > 0 when failCount < selectedTransactions.Count:
                DService.Chat.Print(
                    Service.Lang.GetText("EditHelp1", selectedTransactions.Count - failCount,
                                         string.IsNullOrEmpty(locationName)
                                             ? Service.Lang.GetText("Note")
                                             : Service.Lang.GetText("Location")) + " " +
                    (string.IsNullOrEmpty(locationName) ? noteContent : locationName));
                DService.Chat.PrintError($"({Service.Lang.GetText("EditFailed")}: {failCount})");

                Main.UpdateTransactions(SelectedCurrencyID, CurrentView, CurrentViewID);
                break;
            default:
                DService.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                break;
        }

        isOnEdit = false;
    }
}
