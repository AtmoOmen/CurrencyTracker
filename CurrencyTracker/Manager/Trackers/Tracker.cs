using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        public enum RecordChangeType
        {
            All,
            Positive,
            Negative
        }

        private static readonly ushort[] TriggerChatTypes = new ushort[]
        {
            57, 0, 2110, 2105, 62, 3006, 3001, 2238, 2622
        };

        private static Dictionary<uint, string> _territoryNames = new();
        private static Dictionary<uint, string> _itemNames = new();
        private static HashSet<string> _itemNamesSet = new();

        public delegate void CurrencyChangedHandler(object sender, EventArgs e);
        public event CurrencyChangedHandler? OnCurrencyChanged;



        public ChatHandler ChatHandler = null!;
        public TerrioryHandler TerrioryHandler = null!;
        public ComponentManager ComponentManager = null!;



        private Configuration? C = Plugin.Instance.Configuration;
        private Plugin? P = Plugin.Instance;

        public Tracker()
        {
            LoadConstantNames();
            InitCurrencies();

            InitializeTracking();
            Service.PluginLog.Debug("Currency Tracker Activated");
        }

        public void InitializeTracking()
        {
            ChatHandler = new();
            TerrioryHandler = new();

            ComponentManager = new ComponentManager();
            ComponentManager.Init();

            UpdateCurrencies();
        }

        // (人为触发)发现货币发生改变时触发的事件
        public virtual void OnTransactionsUpdate(EventArgs e)
        {
            OnCurrencyChanged?.Invoke(this, e);
        }

        public void UpdateCurrencies()
        {
            if (!Service.ClientState.IsLoggedIn || Flags.BetweenAreas()) return;

            Parallel.ForEach(C.AllCurrencies, currency =>
            {
                CheckCurrency(currency.Key);
            });
        }

        // 检查货币情况 Check the currency
        public bool CheckCurrency(uint currencyID, string locationName = "", string noteContent = "", RecordChangeType recordChangeType = RecordChangeType.All)
        {
            if (!C.AllCurrencies.TryGetValue(currencyID, out var currencyName)) return false;

            var currencyAmount = CurrencyInfo.GetCurrencyAmount(currencyID);
            uint locationKey = Service.ClientState.TerritoryType;
            locationName = locationName.IsNullOrEmpty()
                ? TerritoryNames.TryGetValue(locationKey, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation")
                : locationName;

            var latestTransaction = Transactions.LoadLatestSingleTransaction(currencyName);

            if (latestTransaction != null)
            {
                var currencyChange = currencyAmount - latestTransaction.Amount;

                if (currencyChange != 0 && (recordChangeType == RecordChangeType.All || (recordChangeType == RecordChangeType.Positive && currencyChange > 0) || (recordChangeType == RecordChangeType.Negative && currencyChange < 0)))
                {
                    Transactions.AppendTransaction(DateTime.Now, currencyName, currencyAmount, currencyChange, locationName, noteContent);
                    OnTransactionsUpdate(EventArgs.Empty);
                    Service.PluginLog.Debug($"{currencyName}({currencyID}) Changed: Update Transactions Data");
                    return true;
                }
            }
            else if (currencyAmount > 0 && (recordChangeType == RecordChangeType.All || recordChangeType == RecordChangeType.Positive))
            {
                Transactions.AddTransaction(DateTime.Now, currencyName, currencyAmount, currencyAmount, locationName, noteContent);
                OnTransactionsUpdate(EventArgs.Empty);
                Service.PluginLog.Debug($"{currencyName}({currencyID}) Changed: Update Transactions Data");
                return true;
            }
            return false;
        }

        private void InitCurrencies()
        {
            foreach (var currency in CurrencyInfo.PresetCurrencies)
            {
                if (!C.PresetCurrencies.ContainsKey(currency.Key))
                {
                    var currencyName = CurrencyInfo.CurrencyLocalName(currency.Key);
                    if (!currencyName.IsNullOrEmpty())
                    {
                        C.PresetCurrencies.Add(currency.Key, currencyName);
                    }
                }
            }

            C.PresetCurrencies = C.PresetCurrencies.Where(kv => CurrencyInfo.PresetCurrencies.ContainsKey(kv.Key))
                                       .ToDictionary(kv => kv.Key, kv => kv.Value);
            C.Save();

            if (C.FisrtOpen)
            {
                foreach (var currencyID in CurrencyInfo.defaultCurrenciesToAdd)
                {
                    var currencyName = CurrencyInfo.CurrencyLocalName(currencyID);

                    if (currencyName.IsNullOrEmpty()) continue;

                    if (!C.CustomCurrencies.ContainsKey(currencyID))
                    {
                        C.CustomCurrencies.Add(currencyID, currencyName);
                    }
                }

                C.FisrtOpen = false;
                C.Save();
            }
        }

        private static void LoadConstantNames()
        {
            _territoryNames = Service.DataManager.GetExcelSheet<TerritoryType>()
                .Where(x => !string.IsNullOrEmpty(x.PlaceName?.Value?.Name?.ToString()))
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.PlaceName?.Value?.Name}");

            _itemNames = Service.DataManager.GetExcelSheet<Item>()
                .Where(x => !string.IsNullOrEmpty(x.Name?.ToString()))
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.Name}");

            _itemNamesSet = new HashSet<string>(_itemNames.Values);
        }

        public unsafe string GetWindowTitle(AddonArgs args, uint windowNodeID, uint[]? textNodeIDs = null)
        {
            textNodeIDs ??= new uint[] { 3, 4 };

            var UI = (AtkUnitBase*)args.Addon;

            if (UI == null || UI->RootNode == null || UI->RootNode->ChildNode == null || UI->UldManager.NodeList == null)
                return string.Empty;

            var windowNode = (AtkComponentBase*)UI->GetComponentNodeById(windowNodeID);
            if (windowNode == null)
                return string.Empty;

            // 国服和韩服特别处理逻辑 For CN and KR Client
            var textNode3 = windowNode->GetTextNodeById(textNodeIDs[0])->GetAsAtkTextNode()->NodeText.ToString();
            var textNode4 = windowNode->GetTextNodeById(textNodeIDs[1])->GetAsAtkTextNode()->NodeText.ToString();

            var windowTitle = !textNode4.IsNullOrEmpty() ? textNode4 : textNode3;

            return windowTitle;
        }

        public unsafe string GetWindowTitle(nint addon, uint windowNodeID, uint[]? textNodeIDs = null)
        {
            textNodeIDs ??= new uint[] { 3, 4 };

            var UI = (AtkUnitBase*)addon;

            if (UI == null || UI->RootNode == null || UI->RootNode->ChildNode == null || UI->UldManager.NodeList == null)
                return string.Empty;

            var windowNode = (AtkComponentBase*)UI->GetComponentNodeById(windowNodeID);
            if (windowNode == null)
                return string.Empty;

            // 国服和韩服特别处理逻辑 For CN and KR Client
            var textNode3 = windowNode->GetTextNodeById(textNodeIDs[0])->GetAsAtkTextNode()->NodeText.ToString();
            var textNode4 = windowNode->GetTextNodeById(textNodeIDs[1])->GetAsAtkTextNode()->NodeText.ToString();

            var windowTitle = !textNode4.IsNullOrEmpty() ? textNode4 : textNode3;

            return windowTitle;
        }

        public static HashSet<string> ItemNamesSet
        {
            get
            {
                return _itemNamesSet;
            }
        }

        public static Dictionary<uint, string> ItemNames
        {
            get
            {
                return _itemNames;
            }
        }

        public static Dictionary<uint, string> TerritoryNames
        {
            get
            {
                return _territoryNames;
            }
        }

        public void Dispose()
        {
            ComponentManager.Uninit();

        }
    }
}
