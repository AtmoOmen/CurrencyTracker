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

    internal static string[] visibleColumns = Array.Empty<string>();
    internal ConcurrentDictionary<uint, List<bool>>? selectedStates = new();
    internal ConcurrentDictionary<uint, List<TransactionsConvertor>>? selectedTransactions = new();
    internal List<TransactionsConvertor> currentTypeTransactions = new();
    private readonly Timer searchTimer = new(100);

    private int currentPage;
    private int visibleStartIndex;
    private int visibleEndIndex;
    internal TransactionFileCategory currentView = TransactionFileCategory.Inventory;
    internal ulong currentViewID = 0;
    private int tablePagingComponentsWidth = 300;

    private void TransactionTableUI()
    {
        if (selectedCurrencyID == 0) return;

        var windowWidth = ImGui.GetContentRegionAvail().X - C.ChildWidthOffset - (185 * ImGuiHelpers.GlobalScale);

        ImGui.SameLine();
        if (ImGui.BeginChildFrame(1, new Vector2(windowWidth, ImGui.GetContentRegionAvail().Y), ImGuiWindowFlags.NoScrollbar))
        {
            TransactionTablePagingUI(windowWidth);

            if (visibleColumns.Length == 0) return;

            using (var table = ImRaii.Table("Transactions", visibleColumns.Length, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable, new Vector2(windowWidth - 10, 1)))
            {
                if (table)
                {
                    SetupTableColumns(visibleColumns);

                    if (currentTypeTransactions.Count > 0)
                    {
                        SelectedStatesWatcher(currentTypeTransactions.Count);

                        ImGui.TableNextRow();
                        for (var i = visibleStartIndex; i < visibleEndIndex; i++)
                        {
                            foreach (var column in visibleColumns)
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

    private void SetupTableColumns(string[] columns)
    {
        var orderColumnWidth = ImGui.CalcTextSize((currentTypeTransactions.Count + 1).ToString()).X + 10;

        foreach (var column in columns)
        {
            var flags = column == "Order" || column == "Checkbox" ? ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize : ImGuiTableColumnFlags.None;
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

        CenterCursorFor(tablePagingComponentsWidth);
        ImGui.BeginGroup();

        // 视图切换 Table View Switch
        TableViewSwitchUI();

        // 首页 First Page
        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.Backward)) currentPage = 0;

        // 上一页 Last Page
        ImGui.SameLine();
        if (ImGui.ArrowButton("PreviousPage", ImGuiDir.Left))
        {
            if (currentPage > 0) currentPage--;
        }

        // 页数显示 Pages
        ImGui.SameLine();
        ImGui.Text($"{Service.Lang.GetText("PageComponent", currentPage + 1, pageCount)}");

        // 下一页 Next Page
        ImGui.SameLine();
        if (ImGui.ArrowButton("NextPage", ImGuiDir.Right))
        {
            if (currentPage < pageCount - 1) currentPage++;
        }

        // 尾页 Final Page
        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.Forward))
        {
            if (currentPage >= 0) currentPage = pageCount;
        }

        // 表格外观 Table Appearence
        ImGui.SameLine();
        TableAppearenceUI(windowWidth);

        ImGui.EndGroup();
        tablePagingComponentsWidth = (int)ImGui.GetItemRectSize().X;

        visibleStartIndex = currentPage * C.RecordsPerPage;
        visibleEndIndex = Math.Min(visibleStartIndex + C.RecordsPerPage, currentTypeTransactions.Count);

        // 鼠标滚轮控制 Logic controlling Mouse Wheel Filpping
        if (!ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
        {
            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel > 0 && currentPage > 0)
                currentPage--;

            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.GetIO().MouseWheel < 0 && currentPage < pageCount - 1)
                currentPage++;
        }
    }

    private void TableViewSwitchUI()
    {
        if (IconButton(FontAwesomeIcon.Bars, "", "TableViewSwitch")) ImGui.OpenPopup("TableViewSwitch");

        using (var popup = ImRaii.Popup("TableViewSwitch"))
        {
            if (popup)
            {
                var boolUI = false;
                if (ImGui.Selectable(Service.Lang.GetText("Inventory"), boolUI, ImGuiSelectableFlags.DontClosePopups))
                {
                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, 0, 0));
                }

                foreach (var retainer in C.CharacterRetainers[P.CurrentCharacter.ContentID])
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
            }
        }
    }

    private void TableAppearenceUI(float windowWidth)
    {
        if (IconButton(FontAwesomeIcon.Table, Service.Lang.GetText("TableAppearance"), "TableAppearance")) ImGui.OpenPopup("TableAppearence");

        using (var popup = ImRaii.Popup("TableAppearence"))
        {
            if (popup)
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ColumnsDisplayed")}:");

                ImGui.BeginGroup();
                using (var table = ImRaii.Table("##ColumnsDisplay", 4, ImGuiTableFlags.NoBordersInBody))
                {
                    if (table)
                    {
                        foreach (var column in C.ColumnsVisibility.Keys)
                        {
                            ImGui.TableNextColumn();
                            ColumnDisplayCheckbox(column);
                        }
                    }
                }
                ImGui.EndGroup();

                var tablewidth = ImGui.GetItemRectSize().X;
                var textWidthOffset = $"{Service.Lang.GetText("ChildframeWidthOffset")}:";
                var widthWidthOffset = tablewidth - ImGui.CalcTextSize(textWidthOffset).X;
                var textPerPage = $"{Service.Lang.GetText("TransactionsPerPage")}:";
                var widthPerPage = tablewidth - ImGui.CalcTextSize(textPerPage).X;

                ImGui.Separator();

                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, textWidthOffset);

                var childWidthOffset = C.ChildWidthOffset;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(widthWidthOffset);
                if (ImGui.InputInt("##ChildframeWidthOffset", ref childWidthOffset, 10))
                {
                    childWidthOffset = Math.Max(-240, Math.Min(childWidthOffset, (int)windowWidth - 700));
                    C.ChildWidthOffset = childWidthOffset;
                    C.Save();
                }

                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, textPerPage);

                var transactionsPerPage = C.RecordsPerPage;
                ImGui.SetNextItemWidth(widthPerPage);
                ImGui.SameLine();
                if (ImGui.InputInt("##TransactionsPerPage", ref transactionsPerPage))
                {
                    transactionsPerPage = Math.Max(transactionsPerPage, 1);
                    C.RecordsPerPage = transactionsPerPage;
                    C.Save();
                }
            }
        }

    }

    private void ColumnDisplayCheckbox(string boolName)
    {
        var isShowColumn = C.ColumnsVisibility[boolName];
        if (ImGui.Checkbox($"{Service.Lang.GetText(boolName)}##Display{boolName}Column", ref isShowColumn))
        {
            C.ColumnsVisibility[boolName] = isShowColumn;
            C.Save();

            var tempList = new List<string>();
            foreach (var column in C.ColumnsVisibility)
            {
                if (column.Value) tempList.Add(column.Key);
            }
            visibleColumns = tempList.ToArray();
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
