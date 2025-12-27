using UnityEngine;

namespace Frontline.Missions
{
    /// <summary>
    /// Spawns a training range for the basic rifle training mission.
    /// Creates a marked area with training targets.
    /// </summary>
    public sealed class TrainingRangeSpawner : MonoBehaviour
    {
        [SerializeField] private Vector3 rangePosition = new Vector3(25, 0, 25);
        [SerializeField] private int targetCount = 5;
        [SerializeField] private float targetSpacing = 3f;
        [SerializeField] private float targetDistance = 10f;

        private bool _spawned;

        private void Start()
        {
            if (_spawned)
                return;

            SpawnTrainingRange();
            _spawned = true;
        }

        private void SpawnTrainingRange()
        {
            // Create the training range parent
            var rangeRoot = new GameObject("TrainingRange");
            rangeRoot.transform.position = rangePosition;

            // Create the entry trigger zone
            CreateEntryTrigger(rangeRoot.transform);

            // Create the firing line marker
            CreateFiringLine(rangeRoot.transform);

            // Create training targets
            CreateTargets(rangeRoot.transform);

            // Create signage/visual marker
            CreateRangeSign(rangeRoot.transform);

            Debug.Log($"TrainingRangeSpawner: Created training range at {rangePosition}");
        }

        private void CreateEntryTrigger(Transform parent)
        {
            var trigger = new GameObject("TrainingRange_EntryTrigger");
            trigger.transform.SetParent(parent);
            trigger.transform.localPosition = Vector3.zero;

            // Add box collider as trigger
            var box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(15f, 5f, 20f);
            box.center = new Vector3(0, 2.5f, targetDistance / 2f);

            // Add mission trigger component
            var missionTrigger = trigger.AddComponent<MissionLocationTrigger>();
            // Set via reflection since we can't set serialized fields directly
            SetPrivateField(missionTrigger, "locationId", "training_range");
            SetPrivateField(missionTrigger, "displayName", "Training Range");
        }

        private void CreateFiringLine(Transform parent)
        {
            // Create a visual marker for where to stand
            var firingLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            firingLine.name = "FiringLine";
            firingLine.transform.SetParent(parent);
            firingLine.transform.localPosition = new Vector3(0, 0.05f, 0);
            firingLine.transform.localScale = new Vector3(targetCount * targetSpacing + 2f, 0.1f, 0.5f);

            // Yellow color for visibility
            var renderer = firingLine.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"))
                {
                    color = new Color(1f, 0.9f, 0.2f)
                };
            }

            // Remove collider so player can walk through
            var col = firingLine.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
        }

        private void CreateTargets(Transform parent)
        {
            var targetsRoot = new GameObject("Targets");
            targetsRoot.transform.SetParent(parent);
            targetsRoot.transform.localPosition = new Vector3(0, 0, targetDistance);

            var startX = -((targetCount - 1) * targetSpacing) / 2f;

            for (var i = 0; i < targetCount; i++)
            {
                var targetGo = CreateTargetObject();
                targetGo.name = $"TrainingTarget_{i + 1}";
                targetGo.transform.SetParent(targetsRoot.transform);
                targetGo.transform.localPosition = new Vector3(startX + i * targetSpacing, 1f, 0);
            }
        }

        private GameObject CreateTargetObject()
        {
            // Create a simple target: a cylinder (post) with a cube (target face)
            var root = new GameObject();

            // Post
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(root.transform);
            post.transform.localPosition = new Vector3(0, -0.5f, 0);
            post.transform.localScale = new Vector3(0.2f, 1f, 0.2f);

            var postRenderer = post.GetComponent<Renderer>();
            if (postRenderer != null)
            {
                postRenderer.material = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"))
                {
                    color = new Color(0.4f, 0.3f, 0.2f) // Brown
                };
            }

            // Remove post collider
            var postCol = post.GetComponent<Collider>();
            if (postCol != null)
                Destroy(postCol);

            // Target face
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "TargetFace";
            target.transform.SetParent(root.transform);
            target.transform.localPosition = Vector3.zero;
            target.transform.localScale = new Vector3(1f, 1.5f, 0.2f);
            target.layer = 0; // Default layer for raycasting

            var targetRenderer = target.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                // Create a simple bullseye-ish texture
                targetRenderer.material = CreateTargetMaterial();
            }

            // Add Health component
            var health = target.AddComponent<World.Health>();

            // Add TrainingTarget component
            var trainingTarget = target.AddComponent<TrainingTarget>();
            SetPrivateField(trainingTarget, "maxHp", 30);
            SetPrivateField(trainingTarget, "targetId", "training_target");
            SetPrivateField(trainingTarget, "respawnDelay", 2f);
            SetPrivateField(trainingTarget, "autoRespawn", true);

            return root;
        }

        private Material CreateTargetMaterial()
        {
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));

            // Create a simple bullseye texture
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var center = new Vector2(32, 32);

            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 64; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), center);
                    Color color;

                    if (dist < 8)
                        color = Color.red; // Center - red
                    else if (dist < 16)
                        color = Color.white; // Ring 1
                    else if (dist < 24)
                        color = Color.red; // Ring 2
                    else if (dist < 32)
                        color = Color.white; // Ring 3
                    else
                        color = new Color(0.9f, 0.85f, 0.7f); // Background - cream

                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            mat.mainTexture = tex;
            mat.color = Color.white;

            return mat;
        }

        private void CreateRangeSign(Transform parent)
        {
            // Create a sign post
            var signRoot = new GameObject("RangeSign");
            signRoot.transform.SetParent(parent);
            signRoot.transform.localPosition = new Vector3(-8f, 0, -2f);

            // Post
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "SignPost";
            post.transform.SetParent(signRoot.transform);
            post.transform.localPosition = new Vector3(0, 1f, 0);
            post.transform.localScale = new Vector3(0.15f, 1f, 0.15f);

            var postRenderer = post.GetComponent<Renderer>();
            if (postRenderer != null)
            {
                postRenderer.material = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"))
                {
                    color = new Color(0.3f, 0.3f, 0.3f)
                };
            }

            // Remove collider
            var postCol = post.GetComponent<Collider>();
            if (postCol != null)
                Destroy(postCol);

            // Sign board
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "SignBoard";
            sign.transform.SetParent(signRoot.transform);
            sign.transform.localPosition = new Vector3(0, 2.2f, 0);
            sign.transform.localScale = new Vector3(2f, 0.8f, 0.1f);

            var signRenderer = sign.GetComponent<Renderer>();
            if (signRenderer != null)
            {
                signRenderer.material = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"))
                {
                    color = new Color(0.15f, 0.35f, 0.15f) // Military green
                };
            }

            // Remove collider
            var signCol = sign.GetComponent<Collider>();
            if (signCol != null)
                Destroy(signCol);
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}
