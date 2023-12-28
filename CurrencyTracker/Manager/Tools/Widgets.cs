namespace CurrencyTracker.Manager.Tools
{
    public static class Widgets
    {
        public static bool IconButton(FontAwesomeIcon icon, string tooltip = "", string str_id = "", Vector2 size = default)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{str_id}", size);
            ImGui.PopFont();

            if (!tooltip.IsNullOrEmpty()) HoverTooltip(tooltip);

            return result;
        }

        public static void TextCentered(string text)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(text).X / 2);
            ImGui.TextUnformatted(text);
        }

        public static void HelpMarker(string text)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());
            ImGui.PopFont();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        public static bool SelectableCentered(string text, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(text).X / 2);
            return ImGui.Selectable(text, selected, flags);
        }

        public static bool SelectableClickToCopy(string text, string? textCopy = null, int? order = null)
        {
            textCopy ??= text;

            ImGui.Selectable($"{text}##{order ?? 0}");

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
            {
                ImGui.SetClipboardText(textCopy ?? "");
                Service.Chat.Print($"{Service.Lang.GetText("CopiedToClipboard")}: {textCopy}");
            }

            return true;
        }

        public static unsafe bool SelectableButton(string name, string str_id = "None", string tooltip = "", Vector2 size = default)
        {
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
            ImGui.PushStyleColor(ImGuiCol.Button, 0);
            var result = ImGui.Button($"{name}##{name}-{str_id}", size);
            ImGui.PopStyleColor(3);

            if (!tooltip.IsNullOrEmpty())
                HoverTooltip(tooltip);

            return result;
        }

        public static unsafe bool SelectableIconButton(FontAwesomeIcon icon, string tooltip = "", string str_id = "None", Vector2 size = default)
        {
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
            ImGui.PushStyleColor(ImGuiCol.Button, 0);
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{str_id}", size);
            ImGui.PopFont();
            ImGui.PopStyleColor(3);

            if (!tooltip.IsNullOrEmpty())
                HoverTooltip(tooltip);

            return result;
        }

        public static bool ColoredCheckbox(string label, ref bool state)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, state ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite);
            var result = ImGui.Checkbox(label, ref state);
            ImGui.PopStyleColor();

            return result;
        }

        public static void HoverTooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        public static float SetColumnCenterAligned(string text, int columnIndex = 0, float offset = 0)
        {
            var columnWidth = ImGui.GetColumnWidth(0);
            var textWidth = ImGui.CalcTextSize(text).X;

            var cursorPosX = (columnWidth - textWidth) * 0.5f + offset;
            return cursorPosX;
        }

        public static void CenterCursorFor(int itemWidth)
        {
            ImGui.SetCursorPosX((int)((ImGui.GetWindowWidth() - itemWidth) / 2f));
        }
    }
}
