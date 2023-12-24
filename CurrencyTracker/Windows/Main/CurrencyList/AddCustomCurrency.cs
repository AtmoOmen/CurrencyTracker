using Lumina.Excel.GeneratedSheets;

namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private Dictionary<string, uint>? ItemNames;
    private string[]? itemNamesACC;
    private uint[]? currenciesACC;
    private string searchFilterCCT = string.Empty;
    private uint currencyIDACC = uint.MaxValue;
    private string currencyNameAAC = string.Empty;
    private int currentPageACC = 0;

    private void AddCustomCurrencyUI()
    {
        if (IconButton(FontAwesomeIcon.Plus, "", "AddCustomCurrency"))
        {
            if (ItemNames == null) LoadDataACC();
            ImGui.OpenPopup("AddCustomCurrency");
        }

        using (var popup = ImRaii.Popup("AddCustomCurrency", ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (popup)
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("AddCustomCurrency"));
                HelpMaker(Service.Lang.GetText("CustomCurrencyHelp"));

                ImGui.Separator();
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"{Service.Lang.GetText("Now")}:");

                ImGui.SetNextItemWidth(210f);
                ImGui.SameLine();
                using (var combo = ImRaii.Combo("", !currencyNameAAC.IsNullOrEmpty() ? currencyNameAAC : Service.Lang.GetText("PleaseSelect"), ImGuiComboFlags.HeightLarge))
                {
                    if (combo)
                    {
                        var startIndex = currentPageACC * 10;
                        var endIndex = Math.Min(startIndex + 10, itemNamesACC.Length);

                        ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
                        if (ImGui.InputTextWithHint("##SearchFilterACC", Service.Lang.GetText("PleaseSearch"), ref searchFilterCCT, 100)) searchTimerCCT.Restart();

                        ImGui.SameLine();
                        if (IconButton(FontAwesomeIcon.Backward, "", "CCTFirstPage")) currentPageACC = 0;

                        ImGui.SameLine();
                        if (ImGui.ArrowButton("CustomPreviousPage", ImGuiDir.Left) && currentPageACC > 0) currentPageACC--;

                        ImGui.SameLine();
                        if (ImGui.ArrowButton("CustomNextPage", ImGuiDir.Right) && itemNamesACC.Any() && currentPageACC < (itemNamesACC.Length / 10) - 1) currentPageACC++;

                        ImGui.SameLine();
                        if (IconButton(FontAwesomeIcon.Forward, "", "CCTLastPage") && itemNamesACC.Any()) currentPageACC = (itemNamesACC.Length / 10) - 1;

                        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel > 0 && currentPageACC > 0) currentPageACC--;
                        if (itemNamesACC.Any() && ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel < 0 && currentPageACC < (itemNamesACC.Length / 10) - 1) currentPageACC++;

                        if (itemNamesACC.Any())
                        {
                            ImGui.Separator();
                            var items = itemNamesACC.Skip(startIndex).Take(endIndex - startIndex).ToArray();
                            foreach (var itemName in items)
                            {
                                if (ItemNames.TryGetValue(itemName, out var itemID) && ImGui.Selectable(itemName))
                                {
                                    currencyIDACC = itemID;
                                    currencyNameAAC = itemName;
                                }

                                if (ImGui.IsWindowAppearing() && currencyIDACC == itemID) ImGui.SetScrollHereY();
                            }
                        }
                    }
                }

                if (ImGui.IsItemClicked() && !currenciesACC.SequenceEqual(C.AllCurrencyID)) LoadDataACC();

                ImGui.SameLine();
                if (IconButton(FontAwesomeIcon.Plus, "", "AddCustomCurrency"))
                {
                    if (currencyNameAAC.IsNullOrEmpty())
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                        return;
                    }

                    if (C.AllCurrencies.ContainsValue(currencyNameAAC) || C.AllCurrencyID.Contains(currencyIDACC))
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("CustomCurrencyHelp1"));
                        return;
                    }

                    C.CustomCurrencies.Add(currencyIDACC, currencyNameAAC);
                    C.Save();

                    ReloadOrderedOptions();

                    Service.Tracker.CheckCurrency(currencyIDACC, "", "", RecordChangeType.All, 1);
                    currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID));

                    searchFilterCCT = string.Empty;
                    currencyIDACC = 0;
                    currencyNameAAC = string.Empty;

                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    public void LoadDataACC()
    {
        var currencyNames = C.AllCurrencyID.Select(CurrencyInfo.GetCurrencyLocalName).ToHashSet();
        currenciesACC = C.AllCurrencyID;

        ItemNames = Service.DataManager.GetExcelSheet<Item>()
            .Where(x => x.ItemSortCategory.Row != 5 && x.IsUnique == false && !x.Name.RawString.IsNullOrEmpty() && !currencyNames.Contains(x.Name.RawString))
            .ToDictionary(x => x.Name.RawString, x => x.RowId);

        itemNamesACC = ItemNames.Keys.ToArray();
    }

    private string[] LoadSearchResultACC(string searchFilterCCT = "")
    {
        if (!searchFilterCCT.IsNullOrEmpty())
        {
            var isCS = C.SelectedLanguage == "ChineseSimplified";
            return ItemNames
                .Keys
                .Where(itemName => itemName.Contains(searchFilterCCT, StringComparison.OrdinalIgnoreCase)
                    || (isCS && PinyinHelper.GetPinyin(itemName, "").Contains(searchFilterCCT, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }
        else
        {
            return ItemNames.Keys.ToArray();
        }
    }

    private void SearchTimerACCElapsed(object? sender, ElapsedEventArgs e)
    {
        itemNamesACC = LoadSearchResultACC(searchFilterCCT);
        currentPageACC = 0;
    }
}
