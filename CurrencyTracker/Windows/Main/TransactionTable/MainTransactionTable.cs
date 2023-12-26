namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private static readonly Dictionary<string, Action> ColumnHeaderActions = new()
    {
        {"Order", Plugin.Instance.Main.OrderColumnHeaderUI},
        {"Time", Plugin.Instance.Main.TimeColumnHeaderUI},
        {"Amount", Plugin.Instance.Main.AmountColumnHeaderUI},
        {"Change", Plugin.Instance.Main.ChangeColumnHeaderUI},
        {"Location", Plugin.Instance.Main.LocationColumnHeaderUI},
        {"Note", Plugin.Instance.Main.NoteColumnHeaderUI},
        {"Checkbox", Plugin.Instance.Main.CheckboxColumnHeaderUI}
    };
    private static readonly Dictionary<string, Action<int, bool, TransactionsConvertor>> ColumnCellActions = new()
    {
        {"Order", Plugin.Instance.Main.OrderColumnCellUI},
        {"Time", Plugin.Instance.Main.TimeColumnCellUI},
        {"Amount", Plugin.Instance.Main.AmountColumnCellUI},
        {"Change", Plugin.Instance.Main.ChangeColumnCellUI},
        {"Location", Plugin.Instance.Main.LocationColumnCellUI},
        {"Note", Plugin.Instance.Main.NoteColumnCellUI},
        {"Checkbox", Plugin.Instance.Main.CheckboxColumnCellUI}
    };

    internal ConcurrentDictionary<uint, List<bool>>? selectedStates = new();
    internal ConcurrentDictionary<uint, List<TransactionsConvertor>>? selectedTransactions = new();
    internal List<TransactionsConvertor> currentTypeTransactions = new();
    private readonly Timer searchTimer = new(100);

    private int currentPage;
    private int visibleStartIndex;
    private int visibleEndIndex;
    private TransactionFileCategory currentView = TransactionFileCategory.Inventory;
    private ulong currentViewID = 0;

    private void TransactionTableUI()
    {
        if (selectedCurrencyID == 0) return;

        var windowWidth = ImGui.GetWindowWidth() - C.ChildWidthOffset - 100;

        ImGui.SameLine();

        if (ImGui.BeginChildFrame(1, new Vector2(windowWidth, ChildframeHeightAdjust())))
        {
            TransactionTablePagingUI(windowWidth);

            var columns = C.ColumnsVisibility.Where(c => c.Value).Select(c => c.Key).ToArray();
            var columnCount = columns?.Length ?? 0;
            if (columnCount == 0) return;

            var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable;
            var tableSize = new Vector2(windowWidth - 175, 1);

            using (var table = ImRaii.Table("Transactions", columnCount, tableFlags, tableSize))
            {
                if (table)
                {
                    var transactionCount = currentTypeTransactions.Count;
                    var orderColumnWidth = ImGui.CalcTextSize((transactionCount + 1).ToString()).X + 10;

                    foreach (var column in columns)
                    {
                        var flags = column == "Order" || column == "Checkbox" ? ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize : ImGuiTableColumnFlags.None;
                        var width = column == "Order" ? orderColumnWidth : column == "Checkbox" ? 30 : 150;
                        ImGui.TableSetupColumn(column, flags, width, 0);
                    }

                    ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                    foreach (var column in columns)
                    {
                        ImGui.TableNextColumn();
                        ColumnHeaderActions[column].Invoke();
                    }

                    if (transactionCount > 0)
                    {
                        SelectedStatesWatcher(transactionCount);

                        ImGui.TableNextRow();
                        for (var i = visibleStartIndex; i < visibleEndIndex; i++)
                        {
                            foreach (var column in columns)
                            {
                                ImGui.TableNextColumn();
                                ColumnCellActions[column].Invoke(i, selectedStates[selectedCurrencyID][i], currentTypeTransactions[i]);
                            }
                            ImGui.TableNextRow();
                        }

                    }
                }
            }

            TransactionTableInfoBarUI();

            ImGui.EndChildFrame();
        }
    }

    private void SelectedStatesWatcher(int transactionCount)
    {
        var stateList = selectedStates.GetOrAdd(selectedCurrencyID, _ => new List<bool>());
        var transactionList = selectedTransactions.GetOrAdd(selectedCurrencyID, _ => new());

        var itemsToAdd = transactionCount - stateList.Count;
        if (itemsToAdd > 0) for (var i = 0; i < itemsToAdd; i++) stateList.Add(false);
    }

    private void TransactionTablePagingUI(float windowWidth)
    {
        var pageCount = (currentTypeTransactions.Any()) ? (int)Math.Ceiling((double)currentTypeTransactions.Count / C.RecordsPerPage) : 0;
        currentPage = (pageCount > 0) ? Math.Clamp(currentPage, 0, pageCount - 1) : 0;

        // 视图切换 Table View Switch
        ImGui.SetCursorPosX((windowWidth / 3));
        TableViewSwitchUI();

        // 首页 First Page
        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.Backward)) currentPage = 0;

        // 上一页 Last Page
        ImGui.SameLine();
        if (ImGui.ArrowButton("PreviousPage", ImGuiDir.Left) && currentPage > 0) currentPage--;

        // 页数显示 Pages
        ImGui.SameLine();
        ImGui.Text($"{Service.Lang.GetText("PageComponent", currentPage + 1, pageCount)}");

        // 下一页 Next Page
        ImGui.SameLine();
        if (ImGui.ArrowButton("NextPage", ImGuiDir.Right) && currentPage < pageCount - 1) currentPage++;

        // 尾页 Final Page
        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.Forward) && currentPage >= 0) currentPage = pageCount;

        // 表格外观 Table Appearence
        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.Table, Service.Lang.GetText("TableAppearance"), "TableAppearance")) ImGui.OpenPopup("TableAppearence");

        if (ImGui.BeginPopup("TableAppearence"))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ColumnsDisplayed")}:");

            ColumnDisplayCheckbox("Time");
            ImGui.SameLine();
            ColumnDisplayCheckbox("Amount");
            ImGui.SameLine();
            ColumnDisplayCheckbox("Change");

            ColumnDisplayCheckbox("Order");
            ImGui.SameLine();
            ColumnDisplayCheckbox("Location");
            ImGui.SameLine();
            ColumnDisplayCheckbox("Note");
            ImGui.SameLine();
            ColumnDisplayCheckbox("Checkbox");

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ChildframeWidthOffset")}:");

            ImGui.SetNextItemWidth(150f);
            ImGui.SameLine();
            var childWidthOffset = C.ChildWidthOffset;
            if (ImGui.InputInt("##ChildframesWidthOffset", ref childWidthOffset, 10))
            {
                childWidthOffset = Math.Max(-240, Math.Min(childWidthOffset, (int)windowWidth - 700));
                C.ChildWidthOffset = childWidthOffset;
                C.Save();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("TransactionsPerPage")}:");

            ImGui.SetNextItemWidth(150);
            ImGui.SameLine();
            var transactionsPerPage = C.RecordsPerPage;
            if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
            {
                transactionsPerPage = Math.Max(transactionsPerPage, 1);
                C.RecordsPerPage = transactionsPerPage;
                C.Save();
            }

            ImGui.EndPopup();
        }

        visibleStartIndex = currentPage * C.RecordsPerPage;
        visibleEndIndex = Math.Min(visibleStartIndex + C.RecordsPerPage, currentTypeTransactions.Count);

        // 鼠标滚轮控制 Logic controlling Mouse Wheel Filpping
        {
            if (!ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
            {
                if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel > 0 && currentPage > 0)
                    currentPage--;

                if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel < 0 && currentPage < pageCount - 1)
                    currentPage++;
            }
        }
    }

    private void TableViewSwitchUI()
    {
        if (IconButton(FontAwesomeIcon.Bars, ""))
        {
            ImGui.OpenPopup("TableViewSwitch");
        }

        if (ImGui.BeginPopup("TableViewSwitch"))
        {
            var boolUI = false;
            if (ImGui.Selectable(Service.Lang.GetText("Inventory"), boolUI, ImGuiSelectableFlags.DontClosePopups))
            {
                currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, 0, 0));
            }

            foreach(var retainer in C.CharacterRetainers[P.CurrentCharacter.ContentID])
            {
                if (ImGui.Selectable($"{retainer.Value}##{retainer.Key}", boolUI, ImGuiSelectableFlags.DontClosePopups))
                {
                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, TransactionFileCategory.Retainer, retainer.Key));
                }
            }

            if (ImGui.Selectable(Service.Lang.GetText("SaddleBag"), boolUI, ImGuiSelectableFlags.DontClosePopups))
            {
                currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, TransactionFileCategory.SaddleBag, 0));
                currentView = TransactionFileCategory.SaddleBag;
                currentViewID = 0;
            }

            if (ImGui.Selectable(Service.Lang.GetText("PSaddleBag"), boolUI, ImGuiSelectableFlags.DontClosePopups))
            {
                currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, TransactionFileCategory.PremiumSaddleBag, 0));
            }
            ImGui.EndPopup();
        }
    }

    private void ColumnDisplayCheckbox(string boolName)
    {
        var isShowColumn = C.ColumnsVisibility[boolName];
        if (ImGui.Checkbox($"{Service.Lang.GetText(boolName)}##Display{boolName}Column", ref isShowColumn))
        {
            C.ColumnsVisibility[boolName] = isShowColumn;
            C.Save();
        }
    }

    private void TransactionTableInfoBarUI()
    {
        if (selectedTransactions.TryGetValue(selectedCurrencyID, out var transactions) && transactions.Any())
        {
            var count = transactions.Count;
            var sum = transactions.Sum(x => x.Change);
            var avg = Math.Round((double)sum / count, 2);
            var max = transactions.Max(x => x.Change);
            var min = transactions.Min(x => x.Change);

            ImGui.TextDisabled(Service.Lang.GetText("SelectedTransactionsInfo", count, sum, avg, max, min));
        }
    }
}
