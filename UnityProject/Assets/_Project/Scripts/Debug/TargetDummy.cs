using Frontline.UI;
using Frontline.World;
using UnityEngine;

namespace Frontline.DebugTools
{
    [RequireComponent(typeof(Health))]
    public sealed class TargetDummy : MonoBehaviour
    {
        [SerializeField] private int maxHp = 120;

        private Health _health;

        public int MaxHp => maxHp;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _health.Configure(Mathf.Max(1, maxHp), destroyOnDeath: false);

            // Optional feedback helpers.
            if (GetComponent<WorldHealthPipBar>() == null)
                gameObject.AddComponent<WorldHealthPipBar>();
        }

        public void ResetToFull()
        {
            if (_health == null)
                _health = GetComponent<Health>();
            if (_health == null)
                return;

            // Simplest: reconfigure to full without destroying the object.
            _health.Configure(Mathf.Max(1, maxHp), destroyOnDeath: false);
        }
    }
}

