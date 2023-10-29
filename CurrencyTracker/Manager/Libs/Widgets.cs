using CurrencyTracker.Manager;
using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CurrencyTracker.Windows
{
    public static class Widgets
    {
        public static bool IsTransactionEqual(TransactionsConvertor t1, TransactionsConvertor t2)
        {
            return t1.TimeStamp == t2.TimeStamp && t1.Amount == t2.Amount && t1.Change == t2.Change && t1.LocationName == t2.LocationName && t1.Note == t2.Note;
        }

        public static bool AreTransactionsEqual(List<TransactionsConvertor> list1, List<TransactionsConvertor> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }

            for (int i = 0; i < list1.Count; i++)
            {
                if (!IsTransactionEqual(list1[i], list2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // 打开链接用 Used to open URL
        public static void OpenUrl(string url)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = url;
                psi.UseShellExecute = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                psi.FileName = "xdg-open";
                psi.ArgumentList.Add(url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                psi.FileName = "open";
                psi.ArgumentList.Add(url);
            }
            else
            {
                Service.PluginLog.Error("Unsupported OS");
                return;
            }

            Process.Start(psi);
        }

        public static bool IconButton(FontAwesomeIcon icon, string tooltip = "None", string str_id = "None", int width = -1, Vector2 size = default)
        {
            ImGui.PushFont(UiBuilder.IconFont);

            if (width > 0)
                ImGui.SetNextItemWidth(32);

            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{str_id}", size);
            ImGui.PopFont();

            if (tooltip != null && tooltip != "None")
                TextTooltip(tooltip);

            return result;
        }

        public unsafe static ImFontPtr GetFont(float size)
        {
            var style = new Dalamud.Interface.GameFonts.GameFontStyle(Dalamud.Interface.GameFonts.GameFontStyle.GetRecommendedFamilyAndSize(Dalamud.Interface.GameFonts.GameFontFamily.Axis, size));
            var font = Plugin.Instance.PluginInterface.UiBuilder.GetGameFontHandle(style).ImFont;

            if ((IntPtr)font.NativePtr == IntPtr.Zero)
            {
                return ImGui.GetFont();
            }
            font.Scale = size / style.BaseSizePt;
            return font;
        }

        public static unsafe bool SelectableButton(string name)
        {
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
            ImGui.PushStyleColor(ImGuiCol.Button, 0);
            var result = ImGui.Button(name);
            ImGui.PopStyleColor(3);
            return result;
        }

        public static void TextTooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(text);
                ImGui.EndTooltip();
            }
        }

        public static float SetColumnCenterAligned(string text, int columnIndex = 0, float offset = 0)
        {
            float columnWidth = ImGui.GetColumnWidth(0);
            float textWidth = ImGui.CalcTextSize(text).X;

            float cursorPosX = (columnWidth - textWidth) * 0.5f + offset;
            return cursorPosX;
        }
    }
}
