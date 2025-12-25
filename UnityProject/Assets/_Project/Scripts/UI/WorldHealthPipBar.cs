using Frontline.World;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Patch 5.2: Simple 8-pip world health indicator that billboards to the camera.
    /// Intended for buildables/destructibles (hidden at full HP).
    /// </summary>
    public sealed class WorldHealthPipBar : MonoBehaviour
    {
        private const int Pips = 8;

        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.35f, 0f);
        [SerializeField] private float pipSize = 0.08f;
        [SerializeField] private float pipGap = 0.02f;

        private Health _health;
        private Transform _root;
        private Renderer[] _pipRenderers;
        private Collider _col;
        private Renderer _renderer;

        private static Material _pipOn;
        private static Material _pipOff;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _col = GetComponent<Collider>();
            _renderer = GetComponent<Renderer>();

            if (_health != null)
                _health.Changed += OnHealthChanged;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Changed -= OnHealthChanged;
        }

        private void Start()
        {
            EnsureBuilt();
            Refresh();
        }

        private void LateUpdate()
        {
            if (_root == null)
                return;

            // Position above object.
            var topY = transform.position.y;
            if (_col != null)
                topY = _col.bounds.max.y;
            else if (_renderer != null)
                topY = _renderer.bounds.max.y;

            _root.position = new Vector3(transform.position.x, topY, transform.position.z) + worldOffset;

            // Billboard.
            var cam = Camera.main;
            if (cam != null)
            {
                var forward = cam.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                    _root.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }

        private void OnHealthChanged(Health h)
        {
            Refresh();
        }

        private void EnsureBuilt()
        {
            if (_root != null)
                return;

            var go = new GameObject("_HealthPips");
            go.transform.SetParent(null);
            _root = go.transform;

            _pipRenderers = new Renderer[Pips];

            _pipOn ??= MakeMat(new Color(0.20f, 0.85f, 0.20f, 1f));
            _pipOff ??= MakeMat(new Color(0.10f, 0.10f, 0.10f, 0.75f));

            var totalW = Pips * pipSize + (Pips - 1) * pipGap;
            var startX = -totalW * 0.5f + pipSize * 0.5f;

            for (var i = 0; i < Pips; i++)
            {
                var pip = GameObject.CreatePrimitive(PrimitiveType.Quad);
                pip.name = $"Pip_{i}";
                pip.transform.SetParent(_root, false);
                pip.transform.localPosition = new Vector3(startX + i * (pipSize + pipGap), 0f, 0f);
                pip.transform.localRotation = Quaternion.identity;
                pip.transform.localScale = Vector3.one * pipSize;

                // No collision.
                var c = pip.GetComponent<Collider>();
                if (c != null)
                    Destroy(c);

                var r = pip.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = _pipOff;
                    _pipRenderers[i] = r;
                }
            }
        }

        private void Refresh()
        {
            if (_health == null || _root == null || _pipRenderers == null)
                return;

            var max = Mathf.Max(1, _health.MaxHp);
            var cur = Mathf.Clamp(_health.CurrentHp, 0, max);

            // Hide at full HP to reduce clutter.
            var show = cur < max && cur > 0;
            _root.gameObject.SetActive(show);
            if (!show)
                return;

            var ratio = cur / (float)max;
            var on = Mathf.Clamp(Mathf.CeilToInt(ratio * Pips), 1, Pips);
            for (var i = 0; i < Pips; i++)
            {
                var r = _pipRenderers[i];
                if (r == null)
                    continue;
                r.material = i < on ? _pipOn : _pipOff;
            }
        }

        private static Material MakeMat(Color c)
        {
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            var m = new Material(shader);
            if (m.HasProperty("_Color"))
                m.color = c;
            else
                m.color = c;
            return m;
        }
    }
}

