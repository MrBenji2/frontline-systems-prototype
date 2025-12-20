using UnityEngine;

namespace Frontline.Tactical
{
    /// <summary>
    /// Simple fullscreen fog overlay using IMGUI (no shaders/UI assets needed).
    /// </summary>
    public sealed class FogOfWarOverlay : MonoBehaviour
    {
        [SerializeField] private FogOfWarSystem source;
        [SerializeField] private bool enabledOverlay = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F2;

        public FogOfWarSystem Source
        {
            get => source;
            set => source = value;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                enabledOverlay = !enabledOverlay;

            if (source == null)
                source = FindFirstObjectByType<FogOfWarSystem>();
        }

        private void OnGUI()
        {
            if (!enabledOverlay || source == null || source.FogTexture == null)
                return;

            GUI.depth = -1000;
            var rect = new Rect(0, 0, Screen.width, Screen.height);
            GUI.DrawTexture(rect, source.FogTexture, ScaleMode.StretchToFill, true);
        }
    }
}

