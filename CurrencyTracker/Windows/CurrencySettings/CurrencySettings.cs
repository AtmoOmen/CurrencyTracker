using System;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Dalamud.Bindings.ImGui;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings : Window, IDisposable
{
    private bool isEditingCurrencyName;
    internal string editedCurrencyName = string.Empty;
    
    public CurrencySettings() : base($"Currency Settings##{Name}")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize;
    }

    public override void Draw()
    {
        if (P.Main == null || Main.SelectedCurrencyID == 0)
        {
            IsOpen = false;
            return;
        }

        using (ImRaii.Group())
        {
            using (var tabBar = ImRaii.TabBar("CurrencySettingsCT"))
            {
                if (tabBar)
                {
                    using (var item = ImRaii.TabItem(Service.Lang.GetText("Info")))
                    {
                        if (item)
                        {
                            CurrencyInfoGroupUI();

                            ImGui.Separator();
                            CurrencyAmountInfoUI();

                            ImGui.Separator();
                            CurrencyFilesInfoUI();
                        }
                    }
                    
                    using (var item = ImRaii.TabItem(Service.Lang.GetText("Main-CS-AreaRestriction")))
                    {
                        if (item)
                        {
                            TerritoryRestrictedUI();
                        }
                    }
                    
                    using (var item = ImRaii.TabItem(Service.Lang.GetText("Alert")))
                    {
                        if (item)
                        {
                            IntervalAlertUI();
                        }
                    }
                }
            }
        }
    }

    private void CurrencyInfoGroupUI()
    {
        using var group = ImRaii.Group();

        var imageTexture = CurrencyInfo.GetIcon(Main.SelectedCurrencyID);
        ImGui.Image(imageTexture.Handle, ImGuiHelpers.ScaledVector2(48f));

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            ImGui.SetWindowFontScale(1.6f);
            var currencyName = Service.Config.AllCurrencies[Main.SelectedCurrencyID];
            if (!isEditingCurrencyName)
            {
                ImGui.Text($"{currencyName}");
                if (ImGui.IsItemClicked())
                {
                    isEditingCurrencyName = true;
                    editedCurrencyName    = currencyName;
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
                                                    CurrencyInfo.GetLocalName(Main.SelectedCurrencyID));
                        isEditingCurrencyName = false;
                    }
                }
            }

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(8f * ImGuiHelpers.GlobalScale, 1f);

            if (Main.CharacterCurrencyInfos.Count == 0) Main.LoadDataMCS();

            ImGui.SetWindowFontScale(1);
            ImGui.Text(
                $"{Service.Lang.GetText("Total")}: {CurrencyInfo.GetCharacterCurrencyAmount(Main.SelectedCurrencyID, P.CurrentCharacter):N0}");

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(8f * ImGuiHelpers.GlobalScale, 1f);
        }
    }

    public void Dispose() { }
}
