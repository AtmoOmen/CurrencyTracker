using CurrencyTracker.Manager;
using Dalamud.Interface;
using ImGuiNET;

namespace CurrencyTracker.Windows
{
    public static class Widgets
    {
        public static bool AreTransactionsEqual(TransactionsConvertor t1, TransactionsConvertor t2)
        {
            return t1.TimeStamp == t2.TimeStamp && t1.Amount == t2.Amount && t1.Change == t2.Change && t1.LocationName == t2.LocationName;
        }

        public static bool IconButton(FontAwesomeIcon icon, string tooltip = "None", int width = -1)
        {
            bool result;
            ImGui.PushFont(UiBuilder.IconFont);

            if (width > 0)
                ImGui.SetNextItemWidth(32);

            result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{tooltip}");
            ImGui.PopFont();

            if (tooltip != null && tooltip != "None")
                TextTooltip(tooltip);

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