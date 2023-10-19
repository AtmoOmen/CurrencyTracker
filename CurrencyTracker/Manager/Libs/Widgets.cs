using CurrencyTracker.Manager;
using Dalamud.Interface;
using ImGuiNET;
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

        public static bool IconButton(FontAwesomeIcon icon, string tooltip = "None", string str_id = "None", int width = -1)
        {
            bool result;
            ImGui.PushFont(UiBuilder.IconFont);

            if (width > 0)
                ImGui.SetNextItemWidth(32);

            result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{str_id}");
            ImGui.PopFont();

            if (tooltip != null && tooltip != "None")
                TextTooltip(tooltip);

            return result;
        }

        public static void AddSoftReturnsToText(ref string str, float multilineWidth)
        {
            float textSize = 0;
            string tmpStr = "";
            string finalStr = "";
            int curChr = 0;
            while (curChr < str.Length)
            {
                if (str[curChr] == '\n')
                {
                    finalStr += tmpStr + "\n";
                    tmpStr = "";
                }

                tmpStr += str[curChr];
                textSize = ImGui.CalcTextSize(tmpStr).X;

                if (textSize > multilineWidth)
                {
                    int lastSpace = tmpStr.Length - 1;
                    while (tmpStr[lastSpace] != ' ' && lastSpace > 0)
                        lastSpace--;
                    if (lastSpace == 0)
                        lastSpace = tmpStr.Length - 2;
                    finalStr += tmpStr.Substring(0, lastSpace + 1) + "\r\n";
                    if (lastSpace + 1 > tmpStr.Length)
                        tmpStr = "";
                    else
                        tmpStr = tmpStr.Substring(lastSpace + 1);
                }
                curChr++;
            }
            if (tmpStr.Length > 0)
                finalStr += tmpStr;

            str = finalStr;
        }

        public static bool ImGuiAutosizingMultilineInput(string label, ref string str, Vector2 sizeMin, Vector2 sizeMax, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        {
            ImGui.PushTextWrapPos(sizeMax.X);
            var textSize = ImGui.CalcTextSize(str);
            if (textSize.X > sizeMax.X)
            {
                float ratio = textSize.X / sizeMax.X;
                textSize.X = sizeMax.X;
                textSize.Y *= ratio;
                textSize.Y += 20;    // add space for an extra line
            }

            textSize.Y += 8;    // to compensate for inputbox margins

            if (textSize.X < sizeMin.X)
                textSize.X = sizeMin.X;
            if (textSize.Y < sizeMin.Y)
                textSize.Y = sizeMin.Y;
            if (textSize.X > sizeMax.X)
                textSize.X = sizeMax.X;
            if (textSize.Y > sizeMax.Y)
                textSize.Y = sizeMax.Y;

            bool valueChanged = ImGui.InputTextMultiline(label, ref str, 150, textSize, flags);
            ImGui.PopTextWrapPos();

            return valueChanged;
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
