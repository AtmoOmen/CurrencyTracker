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

            for (var i = 0; i < list1.Count; i++)
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
                    Service.Log.Error("Unsupported OS");
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error :{ex.Message}");
            }
        }

        public static bool IsFileLocked(FileInfo file)
        {
            FileStream? stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }

            return false;
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

        public static void SelectableClickToCopy(string text, string? textCopy = null, int? order = null)
        {
            textCopy ??= text;

            ImGui.Selectable($"{text}##{order ?? 0}");

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
            {
                ImGui.SetClipboardText(textCopy ?? "");
                Service.Chat.Print($"{Service.Lang.GetText("CopiedToClipboard")}: {textCopy}");
            }
        }

        public static unsafe bool SelectableButton(string name, string str_id = "None", string tooltip = "", Vector2 size = default)
        {
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
            ImGui.PushStyleColor(ImGuiCol.Button, 0);
            var result = ImGui.Button($"{name}##{name}-{str_id}", size);
            ImGui.PopStyleColor(3);

            if (!tooltip.IsNullOrEmpty())
                TextTooltip(tooltip);

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
                TextTooltip(tooltip);

            return result;
        }

        public static bool ColoredCheckbox(string label, ref bool state)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, state ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite);
            var result = ImGui.Checkbox(label, ref state);
            ImGui.PopStyleColor();

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
            var columnWidth = ImGui.GetColumnWidth(0);
            var textWidth = ImGui.CalcTextSize(text).X;

            var cursorPosX = (columnWidth - textWidth) * 0.5f + offset;
            return cursorPosX;
        }

        public static void CenterCursorFor(int itemWidth)
        {
            var num = (int)ImGui.GetWindowWidth();
            ImGui.SetCursorPosX((num / 2) - (itemWidth / 2));
        }

        public static unsafe string GetWindowTitle(AddonArgs args, uint windowNodeID, uint[]? textNodeIDs = null)
        {
            textNodeIDs ??= new uint[] { 3, 4 };

            var UI = (AtkUnitBase*)args.Addon;

            if (UI == null || UI->RootNode == null || UI->RootNode->ChildNode == null || UI->UldManager.NodeList == null)
                return string.Empty;

            var windowNode = (AtkComponentBase*)UI->GetComponentNodeById(windowNodeID);
            if (windowNode == null)
                return string.Empty;

            // 国服和韩服特别处理逻辑 For CN and KR Client
            var bigTitle = windowNode->GetTextNodeById(textNodeIDs[0])->GetAsAtkTextNode()->NodeText.ToString();
            var smallTitle = windowNode->GetTextNodeById(textNodeIDs[1])->GetAsAtkTextNode()->NodeText.ToString();

            var windowTitle = !smallTitle.IsNullOrEmpty() ? smallTitle : bigTitle;

            return windowTitle;
        }

        public static unsafe string GetWindowTitle(nint addon, uint windowNodeID, uint[]? textNodeIDs = null)
        {
            textNodeIDs ??= new uint[] { 3, 4 };

            var UI = (AtkUnitBase*)addon;

            if (UI == null || UI->RootNode == null || UI->RootNode->ChildNode == null || UI->UldManager.NodeList == null)
                return string.Empty;

            var windowNode = (AtkComponentBase*)UI->GetComponentNodeById(windowNodeID);
            if (windowNode == null)
                return string.Empty;

            // 国服和韩服特别处理逻辑 For CN and KR Client
            var textNode3 = windowNode->GetTextNodeById(textNodeIDs[0])->GetAsAtkTextNode()->NodeText.ToString();
            var textNode4 = windowNode->GetTextNodeById(textNodeIDs[1])->GetAsAtkTextNode()->NodeText.ToString();

            var windowTitle = !textNode4.IsNullOrEmpty() ? textNode4 : textNode3;

            return windowTitle;
        }

        public static void Restart(this Timer timer)
        {
            timer.Stop();
            timer.Start();
        }
    }
}
