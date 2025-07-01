using System;
using Dalamud.Interface;

namespace CurrencyTracker.Utilities;

public static class UIHelper
{
    public static void PagingComponent(Action firstPageAction, Action previousPageAction, Action nextPageAction, Action lastPageAction)
    {
        if (ImGuiOm.ButtonIcon("FirstPage", FontAwesomeIcon.Backward, string.Empty, true))
            firstPageAction.Invoke();

        ImGui.SameLine();
        if (ImGui.ArrowButton("PreviousPage", ImGuiDir.Left))
            previousPageAction.Invoke();

        ImGui.SameLine();
        if (ImGui.ArrowButton("NextPage", ImGuiDir.Right))
            nextPageAction.Invoke();

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("LastPage", FontAwesomeIcon.Forward, string.Empty, true))
            lastPageAction.Invoke();

        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel > 0)
            previousPageAction.Invoke();
        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel < 0)
            nextPageAction.Invoke();
    }
}
