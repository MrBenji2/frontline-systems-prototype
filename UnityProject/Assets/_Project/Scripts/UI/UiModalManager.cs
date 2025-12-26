using System;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Patch 5.2: Centralized gameplay modal close rules.
    /// - Esc closes ANY open gameplay modal.
    /// - E toggles (closes) ONLY modals that were opened via interaction (E).
    /// 
    /// Milestone 7.2: Added centralized IsUIBlockingInput check.
    /// Modals register a close action when opened and clear when closed.
    /// </summary>
    public sealed class UiModalManager : MonoBehaviour
    {
        public static UiModalManager Instance { get; private set; }

        private string _currentModalId;
        private Action _currentClose;
        private bool _currentOpenedByInteract;
        private int _openedFrame = -1;

        // Milestone 7.2: Track if mouse is over IMGUI elements this frame.
        private bool _mouseOverIMGUI;
        private Rect _lastIMGUIRect;

        public bool HasOpenModal => _currentClose != null;
        public bool CurrentOpenedByInteract => HasOpenModal && _currentOpenedByInteract;

        /// <summary>
        /// Milestone 7.2: Centralized check for whether UI is blocking world input.
        /// Use this before processing mouse clicks for world actions (placing, attacking, etc.).
        /// </summary>
        public bool IsUIBlockingInput
        {
            get
            {
                // Any modal is open.
                if (HasOpenModal)
                    return true;

                // Build catalog panel open (IMGUI).
                if (BuildCatalogPanel.Instance != null && BuildCatalogPanel.Instance.IsOpen)
                    return true;

                // Storage crate panel open.
                if (StorageCratePanel.Instance != null && StorageCratePanel.Instance.IsOpen)
                    return true;

                // Truck panel open.
                if (TransportTruckPanel.Instance != null && TransportTruckPanel.Instance.IsOpen)
                    return true;

                // Dev panel (F1) - check via DestroyedPoolDebugPanel.
                if (DestroyedPoolDebugPanel.Instance != null && DestroyedPoolDebugPanel.Instance.IsVisible)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Milestone 7.2: Registers an IMGUI rect to track for mouse blocking.
        /// Call this from OnGUI() in panels that need to block world clicks.
        /// </summary>
        public void RegisterIMGUIRect(Rect rect)
        {
            _lastIMGUIRect = rect;
            if (rect.Contains(Event.current?.mousePosition ?? Input.mousePosition))
                _mouseOverIMGUI = true;
        }

        /// <summary>
        /// Milestone 7.2: Returns true if mouse is currently over registered IMGUI elements.
        /// </summary>
        public bool IsMouseOverIMGUI => _mouseOverIMGUI;

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

        private void Update()
        {
            // Milestone 7.2: Reset IMGUI mouse tracking each frame.
            _mouseOverIMGUI = false;

            if (!HasOpenModal)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseCurrent();
                return;
            }

            // Avoid immediately closing a modal that was opened earlier in the same frame.
            if (_openedFrame == Time.frameCount)
                return;

            if (_currentOpenedByInteract && Input.GetKeyDown(KeyCode.E))
            {
                CloseCurrent();
            }
        }

        public void RegisterOpen(string modalId, Action closeAction, bool openedByInteract)
        {
            _currentModalId = modalId ?? "";
            _currentClose = closeAction;
            _currentOpenedByInteract = openedByInteract;
            _openedFrame = Time.frameCount;
        }

        public void RegisterClosed(string modalId)
        {
            if (!HasOpenModal)
                return;
            if (!string.Equals(_currentModalId, modalId ?? "", StringComparison.Ordinal))
                return;
            Clear();
        }

        public bool TryToggleInteractModal(string modalId, Func<bool> tryOpen, Action closeAction)
        {
            modalId ??= "";

            // If this modal is already open via interact, toggle-close it.
            if (HasOpenModal && _currentOpenedByInteract && string.Equals(_currentModalId, modalId, StringComparison.Ordinal))
            {
                CloseCurrent();
                return true;
            }

            // Don't open a new modal while one is open.
            if (HasOpenModal)
                return false;

            if (tryOpen == null)
                return false;

            var opened = tryOpen();
            if (!opened)
                return false;

            RegisterOpen(modalId, closeAction, openedByInteract: true);
            return true;
        }

        public void CloseCurrent()
        {
            if (!HasOpenModal)
                return;

            var close = _currentClose;
            Clear();
            close?.Invoke();
        }

        private void Clear()
        {
            _currentModalId = "";
            _currentClose = null;
            _currentOpenedByInteract = false;
            _openedFrame = -1;
        }
    }
}

