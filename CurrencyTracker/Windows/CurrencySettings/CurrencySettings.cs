using System;
using System.Linq;
using System.Numerics;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tasks;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings : Window, IDisposable
{
    private bool isEditingCurrencyName;
    internal string editedCurrencyName = string.Empty;

    private static TaskManager? TaskManager;

    public CurrencySettings(Plugin plugin) : base($"Currency Settings##{Name}")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize;

        TaskManager ??= new TaskManager { AbortOnTimeout = true, TimeLimitMS = 5000, ShowDebug = false };
    }

    public override void Draw()
    {
        if (P.Main == null || Main.SelectedCurrencyID == 0)
        {
            IsOpen = false;
            return;
        }

        ImGui.BeginGroup();
        if (ImGui.BeginTabBar("CurrencySettingsCT"))
        {
            if (ImGui.BeginTabItem(Service.Lang.GetText("Info")))
            {
                CurrencyInfoGroupUI();

                ImGui.Separator();
                CurrencyAmountInfoUI();

                ImGui.Separator();
                CurrencyFilesInfoUI();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Service.Lang.GetText("Main-CS-AreaRestriction")))
            {
                TerritoryRestrictedUI();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Service.Lang.GetText("Alert")))
            {
                IntervalAlertUI();

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.EndGroup();
    }

    private static void DrawBackgroundImage()
    {
        var region = ImGui.GetContentRegionAvail();
        var minDimension = Math.Min(region.X, region.Y);

        var areaStart = ImGui.GetCursorPos();
        ImGui.SetCursorPosX((region.X / 2.0f) - (minDimension / 2.0f));
        ImGui.Image(Service.Config.AllCurrencyIcons[1].ImGuiHandle, new Vector2(minDimension), Vector2.Zero,
                    Vector2.One,
                    Vector4.One with { W = 0.10f });
        ImGui.SetCursorPos(areaStart);
    }

    private void CurrencyInfoGroupUI()
    {
        ImGui.BeginGroup();
        ImGui.Image(Service.Config.AllCurrencyIcons[Main.SelectedCurrencyID].ImGuiHandle,
                    new Vector2(48 * ImGuiHelpers.GlobalScale));

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.SetWindowFontScale(1.6f);
        var currencyName = Service.Config.AllCurrencies[Main.SelectedCurrencyID];
        if (!isEditingCurrencyName)
        {
            ImGui.Text($"{currencyName}");
            if (ImGui.IsItemClicked())
            {
                isEditingCurrencyName = true;
                editedCurrencyName = currencyName;
            }
        }
        else
        {
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(Service.Config.AllCurrencies[Main.SelectedCurrencyID]).X +
                                   (ImGui.GetStyle().FramePadding.X * 2));
            if (ImGui.InputText("##currencyName", ref editedCurrencyName, 100, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (!editedCurrencyName.IsNullOrWhitespace() &&
                    editedCurrencyName != Service.Config.AllCurrencies[Main.SelectedCurrencyID])
                {
                    CurrencyInfo.RenameCurrency(Main.SelectedCurrencyID, editedCurrencyName);
                    isEditingCurrencyName = false;
                }
            }

            if (ImGui.IsItemDeactivated()) isEditingCurrencyName = false;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) ImGui.OpenPopup("ResetCurrencyNamePopup");

            ImGui.SetWindowFontScale(1f);
            using var popup = ImRaii.Popup("ResetCurrencyNamePopup");
            if (popup.Success)
            {
                if (ImGuiOm.Selectable(Service.Lang.GetText("Reset")))
                {
                    CurrencyInfo.RenameCurrency(Main.SelectedCurrencyID,
                                                CurrencyInfo.GetCurrencyLocalName(Main.SelectedCurrencyID));
                    isEditingCurrencyName = false;
                }
            }
        }

        ImGui.SameLine();
        ImGui.Text("");

        if (Main.CharacterCurrencyInfos.Count == 0) Main.LoadDataMCS();

        ImGui.SetWindowFontScale(1);
        ImGui.Text(
            $"{Service.Lang.GetText("Total")}: {CurrencyInfo.GetCharacterCurrencyAmount(Main.SelectedCurrencyID, P.CurrentCharacter):N0}");

        ImGui.SameLine();
        ImGui.Text("");

        ImGui.EndGroup();
        ImGui.EndGroup();
    }

    public void Dispose()
    {
        TaskManager?.Abort();
    }
}
