using CurrencyTracker.Manager.Libs;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Trackers
{
    public class TripleTriad : ITrackerComponent
    {
        private bool isTTOn = false;
        private string ttRivalName = string.Empty;
        private string ttResultText = string.Empty;

        public TripleTriad() 
        {
            Init();
        }

        public void Init()
        {
            var TTGui = Service.GameGui.GetAddonByName("TripleTriad");
            if (TTGui != nint.Zero)
            {
                StartTripleTriadHandler();
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "TripleTriad", StartTripleTriad);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "TripleTriadResult", EndTripleTriad);
        }

        private void StartTripleTriad(AddonEvent type, AddonArgs args)
        {
            StartTripleTriadHandler();
        }

        private unsafe void StartTripleTriadHandler()
        {
            isTTOn = true;
            Service.Tracker.ChatHandler.isBlocked = true;

            var TTGui = (AtkUnitBase*)Service.GameGui.GetAddonByName("TripleTriad");
            if (TTGui != null)
            {
                ttRivalName = TTGui->GetTextNodeById(187)->NodeText.ToString() ?? string.Empty;
            }
            Service.PluginLog.Debug("Triple Triad Starts");
        }

        private void EndTripleTriad(AddonEvent type, AddonArgs args)
        {
            if (!isTTOn) return;

            EndTripleTriadHandler();
        }

        private unsafe void EndTripleTriadHandler()
        {
            isTTOn = false;

            var TTRGui = (AtkUnitBase*)Service.GameGui.GetAddonByName("TripleTriadResult");

            if (TTRGui != null)
            {
                var draw = (TTRGui->GetTextNodeById(5))->AtkResNode.NodeFlags.HasFlag(NodeFlags.Visible);
                var lose = (TTRGui->GetTextNodeById(4))->AtkResNode.IsVisible;
                var win = (TTRGui->GetTextNodeById(3))->AtkResNode.IsVisible;

                ttResultText = draw ? TTRGui->GetTextNodeById(5)->NodeText.ToString() :
                             lose ? TTRGui->GetTextNodeById(4)->NodeText.ToString() :
                             win ? TTRGui->GetTextNodeById(3)->NodeText.ToString() : "";
                Service.PluginLog.Debug(ttResultText);
            }

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Value, "", $"({(!ttResultText.IsNullOrEmpty() ? $"[{ttResultText}]" : "")}{Service.Lang.GetText("TripleTriadWith", ttRivalName)})");
            });

            ttRivalName = ttResultText = string.Empty;

            Service.PluginLog.Debug("Triple Triad Ends");
        }

        public void Uninit()
        {
            isTTOn = false;
            ttRivalName = ttResultText = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "TripleTriad", StartTripleTriad);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "TripleTriadResult", EndTripleTriad);
        }
    }
}
