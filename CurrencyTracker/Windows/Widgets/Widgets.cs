using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTracker.Windows
{
    public static class Widgets
    {

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
    }
}
