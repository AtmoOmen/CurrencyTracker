namespace CurrencyTracker.Manager
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

        public static void OpenDirectory(string path)
        {
            if (path.IsNullOrEmpty())
            {
                return;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start \"\" \"{path}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = path
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = path
                    });
                }
                else
                {
                    Service.PluginLog.Error("Unsupported OS");
                }
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"Error :{ex.Message}");
            }
        }

        public static bool IconButton(FontAwesomeIcon icon, string tooltip = "None", string str_id = "None", Vector2 size = default)
        {
            ImGui.PushFont(UiBuilder.IconFont);

            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{str_id}", size);
            ImGui.PopFont();

            if (tooltip != null && tooltip != "None")
                TextTooltip(tooltip);

            return result;
        }

        public static void TextCentered(string text)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(text).X / 2);
            ImGui.TextUnformatted(text);
        }

        public static bool SelectableCentered(string text, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(text).X / 2);
            return ImGui.Selectable(text, selected, flags);
        }

        public static unsafe ImFontPtr GetFont(float size)
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

        public static unsafe bool SelectableIconButton(FontAwesomeIcon icon, string tooltip = "None", string str_id = "None", Vector2 size = default)
        {
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
            ImGui.PushStyleColor(ImGuiCol.Button, 0);
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{str_id}", size);
            ImGui.PopFont();
            ImGui.PopStyleColor(3);

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

        public static void Restart(this Timer timer)
        {
            timer.Stop();
            timer.Start();
        }
    }
}
