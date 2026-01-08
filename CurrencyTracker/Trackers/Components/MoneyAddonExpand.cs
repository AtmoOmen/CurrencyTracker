using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CurrencyTracker.Infos;
using CurrencyTracker.Trackers;
using CurrencyTracker.Windows;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.Addon.Events.EventDataTypes;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class MoneyAddonExpand : TrackerComponentBase
{

    private static IAddonEventHandle? mouseoverHandle;
    private static IAddonEventHandle? mouseoutHandle;

    private static Overlay? overlay;

    protected override void OnInit()
    {
        overlay ??= new();

        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "_Money", OnMoneyUI);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_Money", OnMoneyUI);
    }

    private static void OnMoneyUI(AddonEvent type, AddonArgs args)
    {
        if (!Throttler.Throttle("MoneyAddonExpand", 1000)) return;

        if (mouseoutHandle != null || mouseoverHandle != null) return;
        if (!TryGetAddonByName<AtkUnitBase>("_Money", out var addon)) return;

        var counterNode = addon->GetNodeById(3);
        if (counterNode == null) return;

        mouseoverHandle ??= DService.Instance().AddonEvent.AddEvent((nint)addon, (nint)counterNode, AddonEventType.MouseOver, OverlayHandler);
        mouseoutHandle  ??= DService.Instance().AddonEvent.AddEvent((nint)addon, (nint)counterNode, AddonEventType.MouseOut,  OverlayHandler);
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

    protected override void OnUninit()
    {
        DService.Instance().AddonLifecycle.UnregisterListener(OnMoneyUI);
        if (mouseoutHandle != null)
        {
            DService.Instance().AddonEvent.RemoveEvent(mouseoverHandle);
            mouseoutHandle = null;
        }
        if (mouseoverHandle != null)
        {
            DService.Instance().AddonEvent.RemoveEvent(mouseoutHandle);
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

                    if (!Service.Config.AllCurrencies.TryGetValue(currency, out var name)) continue;

                    var texture = CurrencyInfo.GetIcon(currency);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Image(texture.Handle, ImGuiHelpers.ScaledVector2(16));

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
