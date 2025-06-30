using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CurrencyTracker.Infos;
using CurrencyTracker.Windows;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class MoneyAddonExpand : ITrackerComponent
{
    public bool Initialized { get; set; }

    private static IAddonEventHandle? mouseoverHandle;
    private static IAddonEventHandle? mouseoutHandle;

    private static Overlay? overlay;

    public void Init()
    {
        overlay ??= new();

        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "_Money", OnMoneyUI);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_Money", OnMoneyUI);
    }

    private static void OnMoneyUI(AddonEvent type, AddonArgs args)
    {
        if (!Throttler.Throttle("MoneyAddonExpand", 1000)) return;

        if (mouseoutHandle != null || mouseoverHandle != null) return;
        if (!TryGetAddonByName<AtkUnitBase>("_Money", out var addon)) return;

        var counterNode = addon->GetNodeById(3);
        if (counterNode == null) return;

        mouseoverHandle ??= Service.AddonEventManager.AddEvent((nint)addon, (nint)counterNode, AddonEventType.MouseOver, OverlayHandler);
        mouseoutHandle ??= Service.AddonEventManager.AddEvent((nint)addon, (nint)counterNode, AddonEventType.MouseOut, OverlayHandler);
    }

    private static void OverlayHandler(AddonEventType type, AddonEventData data)
    {
        if (overlay == null) return;
        overlay.IsOpen = type switch
        {
            AddonEventType.MouseOver => true,
            AddonEventType.MouseOut => false,
            _ => overlay.IsOpen
        };
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(OnMoneyUI);
        if (mouseoutHandle != null)
        {
            Service.AddonEventManager.RemoveEvent(mouseoverHandle);
            mouseoutHandle = null;
        }
        if (mouseoverHandle != null)
        {
            Service.AddonEventManager.RemoveEvent(mouseoutHandle);
            mouseoverHandle = null;
        }

        if (overlay != null && P.WindowSystem.Windows.Contains(overlay)) P.WindowSystem.RemoveWindow(overlay);
        overlay = null;
    }

    public class Overlay : Window
    {
        private CharacterCurrencyInfo? characterCurrencyInfo;

        public Overlay() : base("MoneyAddonExpandOverlay###CurrencyTracker", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar)
        {
            RespectCloseHotkey = false;

            if (P.WindowSystem.Windows.Any(x => x.WindowName == WindowName))
                P.WindowSystem.RemoveWindow(P.WindowSystem.Windows.FirstOrDefault(x => x.WindowName == WindowName));
            P.WindowSystem.AddWindow(this);
        }

        public override void OnOpen()
        {
            if (Main.CharacterCurrencyInfos.Count <= 0) 
                Main.LoadDataMCS();

            characterCurrencyInfo ??= 
                Main.CharacterCurrencyInfos?.FirstOrDefault(x => x.Character.Equals(P.CurrentCharacter));
        }

        public override void Draw()
        {
            if (!TryGetAddonByName<AtkUnitBase>("_Money", out var addon))
            {
                IsOpen = false;
                return;
            }

            var pos = new Vector2(addon->GetX(), addon->GetY() - ImGui.GetWindowSize().Y);
            ImGui.SetWindowPos(pos);

            if (characterCurrencyInfo == null) return;
            if (Throttler.Throttle("MoneyAddonExpand_UpdateCharacter", 1000)) 
                characterCurrencyInfo?.UpdateAllCurrencies();

            ImGui.SetWindowFontScale(1.1f);
            if (ImGui.BeginTable($"###{characterCurrencyInfo.Character.ContentID}", 2, ImGuiTableFlags.BordersInnerH))
            {
                foreach (var currency in Service.Config.OrderedOptions)
                {
                    if (currency == 0) continue;

                    var amount = characterCurrencyInfo.CurrencyAmount.GetValueOrDefault(currency, 0);
                    if (amount == 0) continue;

                    if (!Service.Config.AllCurrencyIcons.TryGetValue(currency, out var texture)) continue;
                    if (!Service.Config.AllCurrencies.TryGetValue(currency, out var name)) continue;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Image(texture.GetWrapOrEmpty().ImGuiHandle, ImGuiHelpers.ScaledVector2(16.0f));

                    ImGui.SameLine();
                    ImGui.Text($"{name}  ");

                    ImGui.SameLine();
                    ImGui.Spacing();

                    ImGui.TableNextColumn();
                    ImGui.Text($"{amount:N0}  ");
                }

                ImGui.EndTable();
            }
            ImGui.SetWindowFontScale(1f);
        }
    }

}
