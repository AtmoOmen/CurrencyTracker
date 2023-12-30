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

        public static bool IconButtonWithTextVertical(FontAwesomeIcon icon, string text)
        {
            ImGui.PushID(text);
            ImGui.PushFont(UiBuilder.IconFont);
            var iconSize = ImGui.CalcTextSize(icon.ToIconString());
            ImGui.PopFont();
            var textSize = ImGui.CalcTextSize(text);
            var windowDrawList = ImGui.GetWindowDrawList();
            var cursorScreenPos = ImGui.GetCursorScreenPos();
            var padding = ImGui.GetStyle().FramePadding.X;
            var spacing = 3f * ImGuiHelpers.GlobalScale;
            var buttonWidth = Math.Max(iconSize.X, textSize.X) + (padding * 2);
            var buttonHeight = iconSize.Y + textSize.Y + (padding * 2) + spacing;

            var result = ImGui.Button(string.Empty, new Vector2(buttonWidth, buttonHeight));

            var iconPos = new Vector2(
                cursorScreenPos.X + ((buttonWidth - iconSize.X) / 2),
                cursorScreenPos.Y + padding
            );

            var textPos = new Vector2(
                cursorScreenPos.X + ((buttonWidth - textSize.X) / 2),
                iconPos.Y + iconSize.Y + spacing
            );

            ImGui.PushFont(UiBuilder.IconFont);
            windowDrawList.AddText(iconPos, ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString());
            ImGui.PopFont();
            windowDrawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);

            ImGui.PopID();

            return result;
        }

        public static bool ImageSelectableWithText(string id, nint imageHandle, string text, bool isSelected, Vector2 imageSize)
        {
            ImGui.PushID(id);

            var windowDrawList = ImGui.GetWindowDrawList();
            var cursorPos = ImGui.GetCursorScreenPos();
            var textSize = ImGui.CalcTextSize(text);
            var totalHeight = Math.Max(imageSize.Y, textSize.Y);
            var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, totalHeight);

            var result = ImGui.Selectable($"##{id}", isSelected, ImGuiSelectableFlags.AllowDoubleClick, selectableSize);

            var imagePos = new Vector2(cursorPos.X, cursorPos.Y + ((totalHeight - imageSize.Y) / 2) + 3f);
            windowDrawList.AddImage(imageHandle, imagePos, new Vector2(imagePos.X + imageSize.X, imagePos.Y + imageSize.Y));

            var textPos = new Vector2(cursorPos.X + imageSize.X + ImGui.GetStyle().ItemSpacing.X, cursorPos.Y + ((totalHeight - textSize.Y) / 2) + 2f);
            windowDrawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);

            ImGui.PopID();

            return result;
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
