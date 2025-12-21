using UnityEngine;
using Frontline.Harvesting;

namespace Frontline.Tactical
{
    public static class TacticalFactory
    {
        private const int IgnoreRaycastLayer = 2; // built-in Unity layer

        public static TacticalPlayerController CreatePlayer()
        {
            var player = new GameObject("_Player_Tactical");
            Object.DontDestroyOnLoad(player);
            player.layer = IgnoreRaycastLayer; // don't occlude LOS

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Body";
            capsule.transform.SetParent(player.transform, false);
            capsule.transform.localPosition = Vector3.zero;
            capsule.layer = IgnoreRaycastLayer;
            Object.Destroy(capsule.GetComponent<Collider>());

            player.transform.position = new Vector3(0, 1, 0);

            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1.0f, 0);
            cc.height = 2.0f;
            cc.radius = 0.4f;

            var ctrl = player.AddComponent<TacticalPlayerController>();
            player.AddComponent<TacticalHarvestInteractor>();
            return ctrl;
        }

        public static TopDownCameraController CreateTopDownCamera()
        {
            var camGo = new GameObject("_Camera_TopDown");
            Object.DontDestroyOnLoad(camGo);

            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            camGo.tag = "MainCamera";

            var controller = camGo.AddComponent<TopDownCameraController>();
            controller.Target = Object.FindFirstObjectByType<TacticalPlayerController>()?.transform;
            return controller;
        }

        public static FogOfWarSystem CreateFogOfWar()
        {
            var go = new GameObject("_FogOfWar");
            Object.DontDestroyOnLoad(go);

            var fow = go.AddComponent<FogOfWarSystem>();
            fow.Target = Object.FindFirstObjectByType<TacticalPlayerController>()?.transform;
            return fow;
        }

        public static TacticalTestMapSpawner CreateTestMapSpawner()
        {
            var go = new GameObject("_TacticalTestMapSpawner");
            Object.DontDestroyOnLoad(go);
            return go.AddComponent<TacticalTestMapSpawner>();
        }
    }
}

