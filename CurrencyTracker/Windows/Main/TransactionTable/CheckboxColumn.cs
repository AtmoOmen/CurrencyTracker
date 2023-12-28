namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private bool isOnMergingTT = false;
    private bool isOnEdit = false;
    internal int checkboxColumnWidth = 22;

    private void CheckboxColumnHeaderUI()
    {
        if (IconButton(FontAwesomeIcon.EllipsisH))
        {
            if (!currentTypeTransactions.Any()) return;
            ImGui.OpenPopup("TableTools");
        }

        using var popup = ImRaii.Popup("TableTools");
        if (popup) CheckboxColumnToolUI();
    }

    private void CheckboxColumnToolUI()
    {
        var selectedCount = selectedTransactions[selectedCurrencyID].Count;
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
    private void UnselectCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Unselect")))
        {
            if (count <= 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            selectedStates[selectedCurrencyID].Clear();
            selectedTransactions[selectedCurrencyID].Clear();
        }
    }

    // 全选 Select All
    private void SelectAllCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("SelectAll")))
        {
            selectedTransactions[selectedCurrencyID] = new List<TransactionsConvertor>(currentTypeTransactions);
            selectedStates[selectedCurrencyID] = Enumerable.Repeat(true, selectedStates[selectedCurrencyID].Count).ToList();
        }
    }

    // 反选 Inverse Select
    private void InverseSelectCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("InverseSelect")))
        {
            selectedStates[selectedCurrencyID] = selectedStates[selectedCurrencyID].Select(state => !state).ToList();

            var selectedSet = new HashSet<TransactionsConvertor>(selectedTransactions[selectedCurrencyID], new TransactionComparer());

            selectedTransactions[selectedCurrencyID] = currentTypeTransactions
                .Where(transaction => !selectedSet.Contains(transaction))
                .ToList();
        }
    }

    // 复制 Copy
    private void CopyCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Copy")))
        {
            if (count <= 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var isCSV = C.ExportDataFileType == 0;
            var header = isCSV ? Service.Lang.GetText("ExportFileCSVHeader") : Service.Lang.GetText("ExportFileMDHeader1");
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(header);

            foreach (var record in selectedTransactions[selectedCurrencyID])
            {
                var change = $"{record.Change:+ #,##0;- #,##0;0}";
                var line = isCSV ? $"{record.TimeStamp},{record.Amount},{change},{record.LocationName},{record.Note}"
                                 : $"| {record.TimeStamp} | {record.Amount} | {change} | {record.LocationName} | {record.Note}";
                stringBuilder.AppendLine(line);
            }

            ImGui.SetClipboardText(stringBuilder.ToString());
            Service.Chat.Print($"{Service.Lang.GetText("CopyTransactionsHelp", count)}");
        }
    }

    // 删除 Delete
    private void DeleteCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Delete")))
        {
            if (count <= 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var filePath = Transactions.GetTransactionFilePath(selectedCurrencyID, currentView, currentViewID);
            var editedTransactions = Transactions.LoadAllTransactions(selectedCurrencyID, currentView, currentViewID);

            var selectedSet = new HashSet<TransactionsConvertor>(selectedTransactions[selectedCurrencyID], new TransactionComparer());
            editedTransactions.RemoveAll(selectedSet.Contains);

            TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
            UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
        }
    }

    // 导出 Export
    private void ExportCBCTUI(int count)
    {
        if (ImGui.Selectable(Service.Lang.GetText("Export")))
        {
            if (count <= 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("NoTransactionsSelected"));
                return;
            }

            var filePath = Transactions.ExportData(selectedTransactions[selectedCurrencyID], "", selectedCurrencyID, C.ExportDataFileType, currentView, currentViewID);
            Service.Chat.Print($"{Service.Lang.GetText("ExportFileMessage")}{filePath}");
        }
    }

    // 合并 Merge
    private void MergeCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("Merge"), ref isOnMergingTT, ImGuiSelectableFlags.DontClosePopups))
        {
            if (isOnMergingTT)
            {
                var t1 = selectedTransactions[selectedCurrencyID].FirstOrDefault(t => !t.LocationName.IsNullOrEmpty());
                editedLocationName = t1?.LocationName;

                var t2 = selectedTransactions[selectedCurrencyID].FirstOrDefault(t => !t.Note.IsNullOrEmpty());
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
            if (selectedTransactions[selectedCurrencyID].Count < 2 || editedLocationName.IsNullOrWhitespace())
            {
                return;
            }

            var mergeCount = Transactions.MergeSpecificTransactions(selectedCurrencyID, editedLocationName, selectedTransactions[selectedCurrencyID], editedNoteContent.IsNullOrEmpty() ? "-1" : editedNoteContent, currentView, currentViewID);
            Service.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");

            UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
            isOnMergingTT = false;
        }
    }

    // 编辑 Edit
    private void EditCBCTUI()
    {
        if (ImGui.Selectable(Service.Lang.GetText("Edit"), ref isOnEdit, ImGuiSelectableFlags.DontClosePopups))
        {
            if (selectedTransactions[selectedCurrencyID].Any())
            {
                if (isOnEdit)
                {
                    var t1 = selectedTransactions[selectedCurrencyID].FirstOrDefault(t => !t.LocationName.IsNullOrEmpty());
                    editedLocationName = t1?.LocationName;

                    var t2 = selectedTransactions[selectedCurrencyID].FirstOrDefault(t => !t.Note.IsNullOrEmpty());
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
        if (ImGui.InputTextWithHint("##EditLocationName", Service.Lang.GetText("PressEnterToConfirm"), ref editedLocationName, 80, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            EditLocationName();
        }

        ImGui.Text($"{Service.Lang.GetText("Note")}:");

        ImGui.SetNextItemWidth(210);
        if (ImGui.InputTextWithHint("##EditNoteContent", Service.Lang.GetText("PressEnterToConfirm"), ref editedNoteContent, 80, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            EditNoteContent();
        }

        if (!editedNoteContent.IsNullOrEmpty())
        {
            ImGui.TextWrapped(editedNoteContent);
        }
    }

    // 编辑地名 Edit Location Name
    private void EditLocationName()
    {
        if (editedLocationName.IsNullOrWhitespace())
        {
            return;
        }

        var failCount = Transactions.EditSpecificTransactions(selectedCurrencyID, selectedTransactions[selectedCurrencyID], editedLocationName, "None", currentView, currentViewID);

        EditResultHandler(failCount, editedLocationName, "");
    }

    // 编辑备注 Edit Note Content
    private void EditNoteContent()
    {
        if (editedNoteContent.IsNullOrWhitespace())
        {
            return;
        }

        var failCount = Transactions.EditSpecificTransactions(selectedCurrencyID, selectedTransactions[selectedCurrencyID], "None", editedNoteContent, currentView, currentViewID);

        EditResultHandler(failCount, "", editedNoteContent);
    }

    // 编辑结果处理 Handle Eidt Result
    private void EditResultHandler(int failCount, string locationName = "", string noteContent = "")
    {
        if (failCount == 0)
        {
            Service.Chat.Print(Service.Lang.GetText("EditHelp1", selectedTransactions[selectedCurrencyID].Count, locationName.IsNullOrEmpty() ? Service.Lang.GetText("Note") : Service.Lang.GetText("Location")) + " " + (locationName.IsNullOrEmpty() ? noteContent : locationName));

            UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
        }
        else if (failCount > 0 && failCount < selectedTransactions[selectedCurrencyID].Count)
        {
            Service.Chat.Print(Service.Lang.GetText("EditHelp1", selectedTransactions[selectedCurrencyID].Count - failCount, locationName.IsNullOrEmpty() ? Service.Lang.GetText("Note") : Service.Lang.GetText("Location")) + " " + (locationName.IsNullOrEmpty() ? noteContent : locationName));
            Service.Chat.PrintError($"({Service.Lang.GetText("EditFailed")}: {failCount})");

            UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
        }
        else
        {
            Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
        }

        isOnEdit = false;
    }

    private void CheckboxColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        if (ImGui.Checkbox($"##select_{i}", ref selected))
        {
            selectedStates[selectedCurrencyID][i] = selected;

            if (selected)
            {
                var exists = selectedTransactions[selectedCurrencyID].Any(t => IsTransactionEqual(t, transaction));

                if (!exists)
                {
                    selectedTransactions[selectedCurrencyID].Add(transaction);
                }
            }
            else
            {
                selectedTransactions[selectedCurrencyID].RemoveAll(t => IsTransactionEqual(t, transaction));
            }
        }

        checkboxColumnWidth = (int)ImGui.GetItemRectSize().X;
    }
}
