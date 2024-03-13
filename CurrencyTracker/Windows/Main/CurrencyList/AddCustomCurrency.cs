using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static Dictionary<string, uint>? _currencyDicACC;
    private static string _searchFilterACC = string.Empty;
    private static uint _currencyIDACC;
    private static int _currentPageACC;

    private static void AddCustomCurrencyUI(float buttonWidth)
    {
        if (ButtonIconSelectable("AddCurrency", buttonWidth, FontAwesomeIcon.Plus))
        {
            ImGui.OpenPopup("AddCustomCurrency");
            _currencyDicACC ??= ItemHandler.ItemNames;
        }

        if (ImGui.BeginPopup("AddCustomCurrency", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("AddCustomCurrency"));
            ImGuiOm.HelpMarker(Service.Lang.GetText("CustomCurrencyHelp"));

            ImGui.Separator();

            ImGui.BeginGroup();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{Service.Lang.GetText("Now")}:");

            ImGui.SetNextItemWidth(210f);
            ImGui.SameLine();
            if (ImGui.BeginCombo("###AddCustomCurrency",
                                 _currencyIDACC != 0 ? CurrencyInfo.GetCurrencyLocalName(_currencyIDACC) : Service.Lang.GetText("PleaseSelect"),
                                 ImGuiComboFlags.HeightLarge))
            {
                var itemCount = _currencyDicACC.Count;

                var startIndex = _currentPageACC * 10;
                var endIndex = Math.Min(startIndex + 10, itemCount);

                ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputTextWithHint("##SearchFilterACC", Service.Lang.GetText("PleaseSearch"),
                                            ref _searchFilterACC, 100))
                {
                    LoadSearchResultForACC();
                }

                ImGui.SameLine();
                ImGui.PushID("AddCustomCurrencyPagingComponent");
                PagingComponent(
                    () => _currentPageACC = 0, 
                    () => { if (_currentPageACC > 0) _currentPageACC--; }, 
                    () => { if (_currentPageACC < (itemCount / 10) - 1) _currentPageACC++; }, 
                    () => { _currentPageACC = (itemCount / 10) - 1; });
                ImGui.PopID();

                ImGui.Separator();

                var items = _currencyDicACC.Skip(startIndex).Take(endIndex - startIndex);
                foreach (var item in items)
                {
                    ImGui.BeginDisabled(Service.Config.AllCurrencies.ContainsKey(item.Value));
                    if (ImGui.Selectable($"{item.Key} ({item.Value})", item.Value == _currencyIDACC))
                        _currencyIDACC = item.Value;
                    ImGui.EndDisabled();

                    if (ImGui.IsWindowAppearing() && _currencyIDACC == item.Value) ImGui.SetScrollHereY();
                }

                ImGui.EndCombo();
            }

            ImGui.EndGroup();

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("AddCustomCurrency", FontAwesomeIcon.Plus))
            {
                if (_currencyIDACC == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                    return;
                }

                if (Service.Config.AllCurrencyID.Contains(_currencyIDACC))
                {
                    Service.Chat.PrintError(Service.Lang.GetText("CustomCurrencyHelp1"));
                    return;
                }

                Service.Config.CustomCurrencies.Add(_currencyIDACC, CurrencyInfo.GetCurrencyLocalName(_currencyIDACC));
                Service.Config.Save();

                ReloadOrderedOptions();

                Service.Tracker.CheckCurrency(_currencyIDACC, "", "", RecordChangeType.All, 1);
                currentTypeTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(SelectedCurrencyID));

                _searchFilterACC = string.Empty;
                _currencyIDACC = 0;

                ImGui.CloseCurrentPopup();
                ImGui.EndCombo();
            }

            ImGui.EndPopup();
        }
    }

    private static void LoadSearchResultForACC()
    {
        TaskManager.Abort();

        TaskManager.DelayNext(500);
        TaskManager.Enqueue(() =>
        {
            _currentPageACC = 0;
            _currencyDicACC = string.IsNullOrWhiteSpace(_searchFilterACC) 
                                  ? ItemHandler.ItemNames 
                                  : ItemHandler.ItemNames
                                               .Where(x => x.Key.Contains(_searchFilterACC))
                                               .ToDictionary(x => x.Key, x => x.Value);
        });
    }
}
