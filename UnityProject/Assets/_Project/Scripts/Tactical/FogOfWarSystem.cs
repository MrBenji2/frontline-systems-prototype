using System;
using UnityEngine;

namespace Frontline.Tactical
{
    /// <summary>
    /// Grid-based fog of war:
    /// - unseen: dark
    /// - seen (memory): greyed/dim
    /// - currently visible: clear
    ///
    /// Visibility uses LOS sampling with raycasts against occluders.
    /// </summary>
    public sealed class FogOfWarSystem : MonoBehaviour
    {
        [Header("World Mapping")]
        [SerializeField] private Vector2 worldMin = new Vector2(-50, -50);
        [SerializeField] private Vector2 worldMax = new Vector2(50, 50);
        [SerializeField] private int resolution = 128;

        [Header("Vision")]
        // Patch 9: widen reveal and prep future aim mode.
        [SerializeField] private float defaultRevealRadius = 35f;
        [SerializeField] private float aimRevealRadius = 22f; // not used yet

        // Legacy field kept for backwards compatibility with existing scenes/prefabs.
        [SerializeField] private float visibleRadius = 18f;
        [SerializeField] private float eyeHeight = 1.6f;
        [SerializeField] private float sampleHeight = 0.7f;
        [SerializeField] private LayerMask occluderMask = 1 << 0; // Default layer
        [SerializeField] private float visibilityUpdateHz = 12f;

        [Header("Fog Colors")]
        [SerializeField] private Color unseen = new Color(0f, 0f, 0f, 0.90f);
        [SerializeField] private Color memory = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color visible = new Color(0f, 0f, 0f, 0.0f);

        [Header("Target")]
        [SerializeField] private Transform target;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public Texture2D FogTexture => _tex;

        private Texture2D _tex;
        private Color32[] _pixels;
        private byte[] _seen;    // 0/1
        private byte[] _visible; // 0/1 for current tick

        private float _nextUpdate;

        private void Awake()
        {
            Allocate();
            EnsureOverlay();
        }

        private void OnValidate()
        {
            resolution = Mathf.Clamp(resolution, 32, 512);
            visibleRadius = Mathf.Max(1f, visibleRadius);
            defaultRevealRadius = Mathf.Max(1f, defaultRevealRadius);
            aimRevealRadius = Mathf.Max(1f, aimRevealRadius);

            // If the new field hasn't been set yet, migrate from legacy.
            if (defaultRevealRadius <= 1.01f && visibleRadius > 1.01f)
                defaultRevealRadius = visibleRadius;
        }

        private void Update()
        {
            if (target == null)
                target = FindFirstObjectByType<TacticalPlayerController>()?.transform;
            if (target == null)
                return;

            var interval = visibilityUpdateHz <= 0f ? 0f : (1f / visibilityUpdateHz);
            if (interval > 0f && Time.unscaledTime < _nextUpdate)
                return;
            _nextUpdate = Time.unscaledTime + interval;

            TickVisibility();
            RebuildTexture();
        }

        private void Allocate()
        {
            if (resolution <= 0)
                resolution = 128;

            _tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
            _tex.wrapMode = TextureWrapMode.Clamp;
            _tex.filterMode = FilterMode.Point;

            _pixels = new Color32[resolution * resolution];
            _seen = new byte[resolution * resolution];
            _visible = new byte[resolution * resolution];

            for (var i = 0; i < _pixels.Length; i++)
                _pixels[i] = unseen;
            _tex.SetPixels32(_pixels);
            _tex.Apply(false, false);
        }

        private void EnsureOverlay()
        {
            if (FindFirstObjectByType<FogOfWarOverlay>() != null)
                return;
            var go = new GameObject("_FogOfWarOverlay");
            DontDestroyOnLoad(go);
            var overlay = go.AddComponent<FogOfWarOverlay>();
            overlay.Source = this;
        }

        private void TickVisibility()
        {
            Array.Clear(_visible, 0, _visible.Length);

            var playerPos = target.position;
            var origin = playerPos + Vector3.up * eyeHeight;

            var radius = defaultRevealRadius > 0.01f ? defaultRevealRadius : visibleRadius;
            var r2 = radius * radius;
            var min = worldMin;
            var max = worldMax;
            var size = max - min;
            if (size.x <= 0.01f || size.y <= 0.01f)
                return;

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var idx = y * resolution + x;
                    var world = CellCenterWorld(x, y);

                    var dx = world.x - playerPos.x;
                    var dz = world.z - playerPos.z;
                    if (dx * dx + dz * dz > r2)
                        continue;

                    var dest = world + Vector3.up * sampleHeight;
                    var dir = dest - origin;
                    var dist = dir.magnitude;
                    if (dist <= 0.001f)
                        continue;

                    // If ray hits an occluder before the sample point, it's not visible.
                    if (!Physics.Raycast(origin, dir / dist, dist, occluderMask, QueryTriggerInteraction.Ignore))
                    {
                        _visible[idx] = 1;
                        _seen[idx] = 1;
                    }
                }
            }
        }

        private void RebuildTexture()
        {
            for (var i = 0; i < _pixels.Length; i++)
            {
                if (_visible[i] == 1)
                    _pixels[i] = visible;
                else if (_seen[i] == 1)
                    _pixels[i] = memory;
                else
                    _pixels[i] = unseen;
            }

            _tex.SetPixels32(_pixels);
            _tex.Apply(false, false);
        }

        private Vector3 CellCenterWorld(int x, int y)
        {
            var tX = (x + 0.5f) / resolution;
            var tY = (y + 0.5f) / resolution;

            var wx = Mathf.Lerp(worldMin.x, worldMax.x, tX);
            var wz = Mathf.Lerp(worldMin.y, worldMax.y, tY);
            return new Vector3(wx, 0f, wz);
        }
    }
}

