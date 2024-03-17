using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CurrencyTracker.Manager;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings : Window, IDisposable
{
    private readonly Main? M = P.Main;

    private uint selectedCurrencyID;
    private bool isEditingCurrencyName;
    private int currencyTextWidth = 200;

    public CurrencySettings(Plugin plugin) : base($"Currency Settings##{Name}")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        Initialize(plugin);
    }

    public void Initialize(Plugin plugin)
    {
        searchTimerTR.Elapsed += SearchTimerTRElapsed;
        searchTimerTR.AutoReset = false;
    }

    public override void Draw()
    {
        if (M == null || Main.SelectedCurrencyID == 0)
        {
            IsOpen = false;
            return;
        }

        selectedCurrencyID = Main.SelectedCurrencyID;
        ImGui.BeginGroup();
        using (var tab0 = ImRaii.TabBar("CurrencySettingsCT"))
        {
            if (tab0)
            {
                using (var item0 = ImRaii.TabItem(Service.Lang.GetText("Info")))
                {
                    if (item0)
                    {
                        CurrencyInfoGroupUI();

                        ImGui.Separator();
                        CurrencyAmountInfoUI();

                        ImGui.Separator();
                        CurrencyFilesInfoUI();
                    }
                }

                using (var item0 = ImRaii.TabItem(Service.Lang.GetText("Main-CS-AreaRestriction")))
                {
                    if (item0) TerrioryRestrictedUI();
                }

                using (var item0 = ImRaii.TabItem(Service.Lang.GetText("Alert")))
                {
                    if (item0) IntervalAlertUI();
                }
            }
        }

        ImGui.EndGroup();
    }

    private void DrawBackgroundImage()
    {
        var region = ImGui.GetContentRegionAvail();
        var minDimension = Math.Min(region.X, region.Y);

        var areaStart = ImGui.GetCursorPos();
        ImGui.SetCursorPosX((region.X / 2.0f) - (minDimension / 2.0f));
        ImGui.Image(Service.Config.AllCurrencyIcons[1].ImGuiHandle, new Vector2(minDimension), Vector2.Zero, Vector2.One,
                    Vector4.One with { W = 0.10f });
        ImGui.SetCursorPos(areaStart);
    }

    private void CurrencyInfoGroupUI()
    {
        ImGui.BeginGroup();
        ImGui.Image(Service.Config.AllCurrencyIcons[selectedCurrencyID].ImGuiHandle, new Vector2(48 * ImGuiHelpers.GlobalScale));

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.SetWindowFontScale(1.6f);
        var currencyName = Service.Config.AllCurrencies[selectedCurrencyID];
        currencyTextWidth = (int)ImGui.CalcTextSize(currencyName).X;
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
            RenameCurrencyUI();

        ImGui.SameLine();
        ImGui.Text("");

        if (!Main.CharacterCurrencyInfos.Any()) Main.LoadDataMCS();
        ImGui.SetWindowFontScale(1);
        ImGui.Text(
            $"{Service.Lang.GetText("Total")}: {(Main.CharacterCurrencyInfos[P.CurrentCharacter].CurrencyAmount.GetValueOrDefault(selectedCurrencyID, 0)):N0}");

        ImGui.SameLine();
        ImGui.Text("");

        ImGui.EndGroup();
        ImGui.EndGroup();
    }

    public void Dispose()
    {
        searchTimerTR.Elapsed -= SearchTimerTRElapsed;
        searchTimerTR.Stop();
        searchTimerTR.Dispose();
    }
}
