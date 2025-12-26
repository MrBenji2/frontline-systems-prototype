using System;
using System.IO;
using Frontline.Tactical;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frontline.Vehicles
{
    /// <summary>
    /// Milestone 6: single Transport Truck v1 runtime + persistence.
    /// - Spawn via debug panel
    /// - Save/Load via debug panel (JSON in Application.persistentDataPath)
    /// </summary>
    public sealed class TransportTruckService : MonoBehaviour
    {
        public static TransportTruckService Instance { get; private set; }

        private const string ResourcesPrefabPath = "TransportTruck"; // Resources/TransportTruck.prefab
        private string SavePath => Path.Combine(Application.persistentDataPath, "transport_truck_world.json");

        private TransportTruckController _active;

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

        public TransportTruckController ActiveTruck
        {
            get
            {
                if (_active == null)
                    _active = FindFirstObjectByType<TransportTruckController>();
                return _active;
            }
        }

        public void SpawnNearPlayer()
        {
            if (SceneManager.GetActiveScene().name != "TacticalTest")
                return;

            var player = FindFirstObjectByType<TacticalPlayerController>();
            if (player == null)
                return;

            var basePos = player.transform.position + new Vector3(2f, 0f, 2f);
            var pos = SnapToGround(basePos);

            // Keep single active truck for v1.
            if (ActiveTruck != null)
            {
                ActiveTruck.transform.position = pos;
                ActiveTruck.transform.rotation = Quaternion.identity;
                return;
            }

            _active = SpawnAt(pos, Quaternion.identity);
        }

        public void SaveWorld()
        {
            try
            {
                var snap = new TransportTruckWorldSnapshot();
                if (ActiveTruck != null)
                {
                    snap.truck.exists = true;
                    snap.truck.position = ActiveTruck.transform.position;
                    snap.truck.rotation = ActiveTruck.transform.rotation;
                    snap.truck.currentHp = ActiveTruck.CurrentHp;
                    snap.truck.stored = ActiveTruck.ToSnapshot();
                }
                else
                {
                    snap.truck.exists = false;
                }

                var json = JsonUtility.ToJson(snap, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TransportTruck: failed to save '{SavePath}': {ex.Message}");
            }
        }

        public void LoadWorld()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return;

                var json = File.ReadAllText(SavePath);
                var snap = JsonUtility.FromJson<TransportTruckWorldSnapshot>(json);
                if (snap == null || snap.truck == null)
                    return;

                if (!snap.truck.exists)
                {
                    if (ActiveTruck != null)
                        Destroy(ActiveTruck.gameObject);
                    _active = null;
                    return;
                }

                if (ActiveTruck == null)
                    _active = SpawnAt(snap.truck.position, snap.truck.rotation);

                if (ActiveTruck == null)
                    return;

                ActiveTruck.transform.SetPositionAndRotation(snap.truck.position, snap.truck.rotation);
                ActiveTruck.SetCurrentHpForLoad(snap.truck.currentHp);
                ActiveTruck.LoadFromSnapshot(snap.truck.stored);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TransportTruck: failed to load '{SavePath}': {ex.Message}");
            }
        }

        private static Vector3 SnapToGround(Vector3 pos)
        {
            // Ray from above to find the ground in TacticalTest.
            var origin = pos + Vector3.up * 50f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
            {
                pos.y = hit.point.y + 0.9f;
                return pos;
            }

            // Fallback: keep incoming y.
            pos.y = Mathf.Max(pos.y, 0.9f);
            return pos;
        }

        private TransportTruckController SpawnAt(Vector3 pos, Quaternion rot)
        {
            GameObject go = null;

            var prefab = Resources.Load<GameObject>(ResourcesPrefabPath);
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, rot);
            }
            else
            {
                // Fallback if prefab hasn't been generated yet: build minimal runtime truck.
                go = new GameObject("TransportTruck");
                go.transform.SetPositionAndRotation(pos, rot);
                go.AddComponent<Rigidbody>();
                go.AddComponent<BoxCollider>();
                go.AddComponent<Frontline.World.Health>();
                go.AddComponent<TransportTruckController>();
            }

            go.name = "TransportTruck";
            return go.GetComponent<TransportTruckController>();
        }
    }
}

