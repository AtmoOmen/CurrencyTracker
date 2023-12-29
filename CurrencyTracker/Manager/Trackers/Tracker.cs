using IntervalUtility;

namespace CurrencyTracker.Manager.Trackers
{
    public class Tracker : IDisposable
    {
        public delegate void CurrencyChangedDelegate(uint currencyID, TransactionFileCategory category, ulong ID);
        public event CurrencyChangedDelegate? CurrencyChanged;

        public HandlerManager HandlerManager = null!;
        public ComponentManager ComponentManager = null!;

        private Configuration? C = Plugin.Configuration;
        private Plugin? P = Plugin.Instance;

        public Tracker()
        {
            Init();
        }

        private void Init()
        {
            InitCurrencies();

            HandlerManager ??= new HandlerManager();
            ComponentManager ??= new ComponentManager();

            if (Service.ClientState.IsLoggedIn)
            {
                InitializeTracking();
            }
        }

        private void InitCurrencies()
        {
            foreach (var currency in CurrencyInfo.PresetCurrencies)
            {
                if (!C.PresetCurrencies.ContainsKey(currency))
                {
                    var currencyName = CurrencyInfo.GetCurrencyLocalName(currency);
                    if (!currencyName.IsNullOrEmpty())
                    {
                        C.PresetCurrencies.Add(currency, currencyName);
                    }
                }
            }

            C.PresetCurrencies = C.PresetCurrencies.Where(kv => CurrencyInfo.PresetCurrencies.Contains(kv.Key))
                                       .ToUpdateDictionary(kv => kv.Key, kv => kv.Value);
            C.Save();

            if (C.FisrtOpen)
            {
                foreach (var currencyID in CurrencyInfo.DefaultCustomCurrencies)
                {
                    var currencyName = CurrencyInfo.GetCurrencyLocalName(currencyID);

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

        public void InitializeTracking()
        {
            HandlerManager.Init();
            ComponentManager.Init();

            CheckAllCurrencies("", "", RecordChangeType.All, 0);
            Service.Log.Debug("Currency Tracker Activated");
        }

        public bool CheckCurrency(uint currencyID, string locationName = "", string noteContent = "", RecordChangeType recordChangeType = 0, uint source = 0, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!CheckCurrencyRules1(currencyID)) return false;

            var currencyAmount = CurrencyInfo.GetCurrencyAmount(currencyID, category, ID);
            var previousAmount = CurrencyInfo.GetCurrencyAmountFromFile(currencyID, P.CurrentCharacter, category, ID);

            if (previousAmount == null && currencyAmount <= 0) return false;

            var currencyChange = currencyAmount - (previousAmount ?? 0);
            if (currencyChange == 0 || !CheckCurrencyRules2(currencyID, (int)currencyAmount, (int)currencyChange, category, ID)) return false;

            locationName = locationName.IsNullOrEmpty() ? CurrentLocationName : locationName;

            if (recordChangeType == RecordChangeType.All || (recordChangeType == RecordChangeType.Positive && currencyChange > 0) || (recordChangeType == RecordChangeType.Negative && currencyChange < 0))
            {
                if (previousAmount != null)
                {
                    Transactions.AppendTransaction(currencyID, DateTime.Now, currencyAmount, currencyChange, locationName, noteContent, category, ID);
                }
                else
                {
                    Transactions.AddTransaction(currencyID, DateTime.Now, currencyAmount, currencyAmount, locationName, noteContent, category, ID);
                }
                PostTransactionUpdate(currencyID, currencyChange, source, category, ID);
                return true;
            }
            return false;
        }

        public bool CheckCurrencyRules1(uint currencyID)
        {
            if (!ItemHandler.ItemIDs.Contains(currencyID)) return false;

            if (!C.CurrencyRules.TryGetValue(currencyID, out var rule))
            {
                C.CurrencyRules.Add(currencyID, rule = new());
                C.Save();
            }
            else
            {
                // 地点限制 Location Restrictions
                if (!rule.RegionRulesMode) // 黑名单 Blacklist
                {
                    if (rule.RestrictedAreas.Contains(CurrentLocationID)) return false;
                }
                else // 白名单 Whitelist
                {
                    if (!rule.RestrictedAreas.Contains(CurrentLocationID)) return false;
                }
            }

            return true;
        }

        public bool CheckCurrencyRules2(uint currencyID, int currencyAmount, int currencyChange, TransactionFileCategory category, ulong ID)
        {
            if (!C.CurrencyRules.TryGetValue(currencyID, out var rule))
            {
                C.CurrencyRules.Add(currencyID, rule = new());
                C.Save();
                return true;
            }

            var util = new IntervalUtil();

            void CheckIntervals(List<Interval<int>> intervals, int value, string type)
            {
                foreach (var interval in intervals)
                {
                    if (util.InRange(interval, value, true) && C.AlertNotificationChat)
                    {
                        var message = Service.Lang.GetSeString("AlertIntervalMessage", type, value.ToString("N0"), SeString.CreateItemLink(1, false), GetSelectedViewName(category, ID), interval.ToIntervalString());
                        Service.Chat.PrintError(message);
                    }
                }
            }

            // 数量 Amount
            CheckIntervals(CurrencySettings.GetOrCreateIntervals(currencyID, 0, category, ID), currencyAmount, "Amount");

            // 收支 Change
            CheckIntervals(CurrencySettings.GetOrCreateIntervals(currencyID, 1, category, ID), currencyChange, "Change");

            return true;
        }

        public bool CheckCurrencies(IEnumerable<uint> currencies, string locationName = "", string noteContent = "", RecordChangeType recordChangeType = RecordChangeType.All, uint source = 0, TransactionFileCategory category = 0, ulong ID = 0)
        {
            var isChanged = false;
            foreach (var currency in C.AllCurrencyID)
            {
                if (CheckCurrency(currency, locationName, noteContent, recordChangeType, source, category, ID)) isChanged = true;
            }

            if (!currencies.Any()) return false;
            foreach (var currency in currencies)
            {
                if (CheckCurrency(currency, locationName, noteContent, recordChangeType, source, category, ID)) isChanged = true;
            };
            
            return isChanged;
        }

        public bool CheckAllCurrencies(string locationName = "", string noteContent = "", RecordChangeType recordChangeType = RecordChangeType.All, uint source = 0, TransactionFileCategory category = 0, ulong ID = 0)
        {
            var isChanged = false;
            foreach (var currency in C.AllCurrencyID)
            {
                if (CheckCurrency(currency, locationName, noteContent, recordChangeType, source, category, ID)) isChanged = true;
            };
            return isChanged;
        }

        private void PostTransactionUpdate(uint currencyID, long currencyChange, uint source, TransactionFileCategory category, ulong ID)
        {
            var currencyName = CurrencyInfo.GetCurrencyName(currencyID);

            CurrencyChanged?.Invoke(currencyID, category, ID);
            Service.Log.Debug($"{currencyName}({currencyID}) Changed ({currencyChange:+#,##0;-#,##0;0}) in {category}");
            if (P.PluginInterface.IsDev) Service.Log.Debug($"Source: {source}");
        }

        public void UninitializeTracking()
        {
            HandlerManager.Uninit();
            ComponentManager.Uninit();

            Service.Log.Debug("Currency Tracker Deactivated");
        }

        public void Dispose()
        {
            UninitializeTracking();
        }
    }
}
