using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Milestone 5.3 Fix Pack: simple centered popup message with fade + cooldown.
    /// IMGUI-based to avoid new UI systems.
    /// </summary>
    public sealed class HudMessagePopup : MonoBehaviour
    {
        public static HudMessagePopup Instance { get; private set; }

        [SerializeField] private float fadeDuration = 1.2f;
        [SerializeField] private float cooldown = 0.5f;

        private string _text;
        private float _shownAt;
        private float _nextAllowedAt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Show(string text)
        {
            if (Time.unscaledTime < _nextAllowedAt)
                return;

            _text = text ?? "";
            _shownAt = Time.unscaledTime;
            _nextAllowedAt = Time.unscaledTime + Mathf.Max(0.05f, cooldown);
        }

        private void OnGUI()
        {
            if (string.IsNullOrWhiteSpace(_text))
                return;

            var t = (Time.unscaledTime - _shownAt) / Mathf.Max(0.01f, fadeDuration);
            if (t >= 1f)
            {
                _text = "";
                return;
            }

            var alpha = Mathf.SmoothStep(1f, 0f, t);
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                wordWrap = false
            };

            var rect = new Rect(0, (Screen.height * 0.5f) - 40, Screen.width, 80);
            GUI.Label(rect, _text, style);

            GUI.color = prev;
        }
    }
}

