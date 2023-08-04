using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;
using CurrencyTracker;

namespace CurrencyTracker.Windows;

public class Main : Window, IDisposable
{
    public Main(Plugin plugin) : base("CurrencyTracker")
    {

    }
    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.Text("测试文本");
        ImGui.Text($"");

    }
}
