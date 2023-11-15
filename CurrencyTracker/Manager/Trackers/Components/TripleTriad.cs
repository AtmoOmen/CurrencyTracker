using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private bool isTTOn = false;
        private string ttResultText = string.Empty;
        private string ttRivalName = string.Empty;

        public void InitTripleTriad()
        {
            var TTGui = Service.GameGui.GetAddonByName("TripleTriad");
            if (TTGui != nint.Zero)
            {
                StartTripleTriad();
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "TripleTriad", TripleTriadCheck);
        }

        private void TripleTriadCheck(AddonEvent type, AddonArgs args)
        {
            StartTripleTriad();
        }

        private unsafe void StartTripleTriad()
        {
            isTTOn = true;
            var TTGui = (AtkUnitBase*)Service.GameGui.GetAddonByName("TripleTriad");
            if (TTGui != null)
            {
                ttRivalName = TTGui->GetTextNodeById(187)->NodeText.ToString();
            }
            DebindChatEvent();
            Service.PluginLog.Debug("Triple Triad Starts");
        }

        private void TripleTriad()
        {
            if (!C.RecordTripleTriad) return;

            var TTGui = Service.GameGui.GetAddonByName("TripleTriad");
            var TTRGui = Service.GameGui.GetAddonByName("TripleTriadResult");

            if (TTRGui != nint.Zero)
            {
                unsafe
                {
                    var TTR = (AtkUnitBase*)TTRGui;
                    if (TTR != null && TTR->RootNode != null && TTR->RootNode->ChildNode != null && TTR->UldManager.NodeList != null && ttResultText.IsNullOrEmpty())
                    {
                        var draw = (TTR->GetTextNodeById(5))->AtkResNode.NodeFlags.HasFlag(NodeFlags.Visible);
                        var lose = (TTR->GetTextNodeById(4))->AtkResNode.IsVisible;
                        var win = (TTR->GetTextNodeById(3))->AtkResNode.IsVisible;

                        ttResultText = draw ? TTR->GetTextNodeById(5)->NodeText.ToString() :
                                     lose ? TTR->GetTextNodeById(4)->NodeText.ToString() :
                                     win ? TTR->GetTextNodeById(3)->NodeText.ToString() : "";
                        Service.PluginLog.Debug(ttResultText);
                    }
                }
            }

            if ((TTRGui == nint.Zero && TTGui == nint.Zero && isTTOn) || (TTRGui != nint.Zero && TTGui == nint.Zero && isTTOn))
            {
                EndTripleTriad();
            }
        }

        private void EndTripleTriad()
        {
            isTTOn = false;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({(!ttResultText.IsNullOrEmpty() ? $"[{ttResultText}]" : "")}{Service.Lang.GetText("TripleTriadWith", ttRivalName)})");
            }

            ttRivalName = string.Empty;
            ttResultText = string.Empty;

            Service.Chat.ChatMessage += OnChatMessage;
            Service.PluginLog.Debug("Triple Triad Ends");
        }

        public void UninitTripleTriad()
        {
            isTTOn = false;
            ttResultText = string.Empty;
            ttRivalName = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "TripleTriad", TripleTriadCheck);
        }
    }
}
