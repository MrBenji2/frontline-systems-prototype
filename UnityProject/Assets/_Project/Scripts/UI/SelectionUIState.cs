using System;

namespace Frontline.UI
{
    /// <summary>
    /// Minimal shared selection state for IMGUI HUD.
    /// Updated by build mode + dev spawn controls (Patch Edition).
    /// </summary>
    public static class SelectionUIState
    {
        public static event Action Changed;

        public static string SelectedText { get; private set; } = "";

        public static void SetSelected(string selectedText)
        {
            SelectedText = selectedText ?? "";
            Changed?.Invoke();
        }
    }
}

