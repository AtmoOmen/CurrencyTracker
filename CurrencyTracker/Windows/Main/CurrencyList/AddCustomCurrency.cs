using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OmenTools.ImGuiOm;
using TinyPinyin;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static Dictionary<string, uint>? ItemNames;
    private static string[]? itemNamesACC;
    private static uint[]? currenciesACC;
    private static string searchFilterCCT = string.Empty;
    private static uint currencyIDACC = uint.MaxValue;
    private static string currencyNameAAC = string.Empty;
    private static int currentPageACC;

    private static void AddCustomCurrencyUI()
    {
        if (ImGuiOm.ButtonIcon("AddCustomCurrency", FontAwesomeIcon.Plus))
        {
            if (ItemNames == null) LoadDataACC();
            ImGui.OpenPopup("AddCustomCurrency");
        }

        using var popup = ImRaii.Popup("AddCustomCurrency", ImGuiWindowFlags.AlwaysAutoResize);
        if (popup.Success)
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("AddCustomCurrency"));
            ImGuiOm.HelpMarker(Service.Lang.GetText("CustomCurrencyHelp"));

            ImGui.Separator();

            ImGui.BeginGroup();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{Service.Lang.GetText("Now")}:");

            ImGui.SetNextItemWidth(210f);
            ImGui.SameLine();
            using var combo =
                ImRaii.Combo(
                    "",
                    !string.IsNullOrEmpty(currencyNameAAC) ? currencyNameAAC : Service.Lang.GetText("PleaseSelect"),
                    ImGuiComboFlags.HeightLarge);
            if (combo)
            {
                var startIndex = currentPageACC * 10;
                var endIndex = Math.Min(startIndex + 10, itemNamesACC.Length);

                ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputTextWithHint("##SearchFilterACC", Service.Lang.GetText("PleaseSearch"),
                                            ref searchFilterCCT, 100))
                    RefreshCustomCurrencyResultView();

                ImGui.SameLine();
                ImGui.PushID("AddCustomCurrencyPagingComponent");
                PagingComponent(
                    () => currentPageACC = 0, 
                    () => { if (currentPageACC > 0) currentPageACC--; }, 
                    () => { if (itemNamesACC.Any() && currentPageACC < (itemNamesACC.Length / 10) - 1) currentPageACC++; }, 
                    () => { if (itemNamesACC.Any()) currentPageACC = (itemNamesACC.Length / 10) - 1; });
                ImGui.PopID();

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

            if (ImGui.IsItemClicked() && !currenciesACC.SequenceEqual(Service.Config.AllCurrencyID))
                LoadDataACC();
            ImGui.EndGroup();

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("AddCustomCurrency", FontAwesomeIcon.Plus))
            {
                if (string.IsNullOrEmpty(currencyNameAAC))
                {
                    Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                    return;
                }

                if (Service.Config.AllCurrencies.ContainsValue(currencyNameAAC) || Service.Config.AllCurrencyID.Contains(currencyIDACC))
                {
                    Service.Chat.PrintError(Service.Lang.GetText("CustomCurrencyHelp1"));
                    return;
                }

                Service.Config.CustomCurrencies.Add(currencyIDACC, currencyNameAAC);
                Service.Config.Save();

                ReloadOrderedOptions();

                Service.Tracker.CheckCurrency(currencyIDACC, "", "", RecordChangeType.All, 1);
                currentTypeTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(selectedCurrencyID));

                searchFilterCCT = string.Empty;
                currencyIDACC = 0;
                currencyNameAAC = string.Empty;

                ImGui.CloseCurrentPopup();
            }
        }
    }

    private static void LoadDataACC()
    {
        var currencyNames = Service.Config.AllCurrencyID.Select(CurrencyInfo.GetCurrencyLocalName).ToHashSet();
        currenciesACC = Service.Config.AllCurrencyID;

        ItemNames = ItemHandler.ItemNames
                               .Where(x => !currencyNames.Contains(x.Key))
                               .ToDictionary(x => x.Key, x => x.Value);

        itemNamesACC = ItemNames.Keys.ToArray();
    }

    private static string[] LoadSearchResultACC(string filter = "")
    {
        if (!string.IsNullOrEmpty(filter))
        {
            var isCS = Service.Config.SelectedLanguage == "ChineseSimplified";
            return ItemNames
                   .Keys
                   .Where(itemName => itemName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                                      || 
                                      (isCS && PinyinHelper.GetPinyin(itemName, "")
                                                           .Contains(filter, StringComparison.OrdinalIgnoreCase)))
                   .ToArray();
        }

        return ItemNames.Keys.ToArray();
    }

    private static void RefreshCustomCurrencyResultView()
    {
        TaskManager.Abort();

        TaskManager.DelayNext(250);
        TaskManager.Enqueue(() =>
        {
            itemNamesACC = LoadSearchResultACC(searchFilterCCT);
            currentPageACC = 0;
        });
    }
}
