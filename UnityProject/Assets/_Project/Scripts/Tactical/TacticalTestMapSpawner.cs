using UnityEngine;

namespace Frontline.Tactical
{
    /// <summary>
    /// Creates a simple map for verifying LOS-based fog:
    /// - a ground plane that does NOT occlude raycasts
    /// - obstacles that DO occlude raycasts
    /// </summary>
    public sealed class TacticalTestMapSpawner : MonoBehaviour
    {
        private const int DefaultLayer = 0;
        private const int IgnoreRaycastLayer = 2;

        [SerializeField] private bool spawnIfNoGround = true;
        [SerializeField] private Vector2 groundSize = new Vector2(100, 100);
        [SerializeField] private float obstacleHeight = 3.0f;

        private bool _spawned;

        // Minimal "modern destroyed EU warzone" material pass (procedural, lightweight).
        private static Material _asphalt;
        private static Material _crackedConcrete;
        private static Material _damagedBrickPlaster;
        private static Material _rubbleGrime;
        private static Material _industrialMetal;

        private void Start()
        {
            if (_spawned)
                return;

            if (spawnIfNoGround && FindExistingGround() != null)
                return;

            Spawn();
            _spawned = true;
        }

        private static GameObject FindExistingGround()
        {
            // heuristic: any object named Ground
            return GameObject.Find("Ground");
        }

        private void Spawn()
        {
            SpawnGround();
            SpawnObstacles();
        }

        private void SpawnGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.layer = IgnoreRaycastLayer; // do not block LOS
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(groundSize.x / 10f, 1f, groundSize.y / 10f); // Unity plane is 10x10

            var r = ground.GetComponent<Renderer>();
            if (r != null)
                ApplyMaterial(r, GetAsphaltMaterial(), new Vector2(10f, 10f));
        }

        private void SpawnObstacles()
        {
            // Create a few walls and blocks to test LOS blocking.
            CreateWall(new Vector3(0, obstacleHeight / 2f, 10), new Vector3(22, obstacleHeight, 1));
            CreateWall(new Vector3(-8, obstacleHeight / 2f, 0), new Vector3(1, obstacleHeight, 18));
            CreateWall(new Vector3(12, obstacleHeight / 2f, -6), new Vector3(16, obstacleHeight, 1));

            CreateBlock(new Vector3(-16, obstacleHeight / 2f, -14), new Vector3(4, obstacleHeight, 4));
            CreateBlock(new Vector3(16, obstacleHeight / 2f, 16), new Vector3(6, obstacleHeight, 6));
            CreateBlock(new Vector3(0, obstacleHeight / 2f, -18), new Vector3(8, obstacleHeight, 3));
        }

        private void CreateWall(Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Obstacle_Wall";
            go.layer = DefaultLayer; // occluder mask defaults to Default
            go.transform.position = pos;
            go.transform.localScale = size;

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                // Walls read best as damaged plaster/brick with grime.
                var mat = Random.value < 0.75f ? GetDamagedBrickPlasterMaterial() : GetRubbleGrimeMaterial();
                ApplyMaterial(r, mat, new Vector2(2.0f, 1.0f));
            }
        }

        private void CreateBlock(Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Obstacle_Block";
            go.layer = DefaultLayer;
            go.transform.position = pos;
            go.transform.localScale = size;

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                // Blocks read as cracked concrete / rubble / occasional metal.
                var roll = Random.value;
                var mat = roll < 0.60f ? GetCrackedConcreteMaterial()
                    : roll < 0.90f ? GetRubbleGrimeMaterial()
                    : GetIndustrialMetalMaterial();
                ApplyMaterial(r, mat, new Vector2(1.5f, 1.5f));
            }
        }

        private static void ApplyMaterial(Renderer r, Material mat, Vector2 tiling)
        {
            if (r == null || mat == null)
                return;
            // Use a unique instance per renderer so per-object texture tiling doesn't fight shared state.
            var inst = new Material(mat);
            if (inst.mainTexture != null)
                inst.mainTextureScale = tiling;
            r.material = inst;
        }

        private static Material GetAsphaltMaterial()
        {
            if (_asphalt != null)
                return _asphalt;

            _asphalt = MakeWarzoneMaterial(
                name: "M_Asphalt_Road",
                baseColor: new Color(0.16f, 0.17f, 0.18f),
                metallic: 0.05f,
                smoothness: 0.12f,
                noiseScale: 7.0f,
                crackStrength: 0.55f);
            return _asphalt;
        }

        private static Material GetCrackedConcreteMaterial()
        {
            if (_crackedConcrete != null)
                return _crackedConcrete;

            _crackedConcrete = MakeWarzoneMaterial(
                name: "M_Cracked_Concrete",
                baseColor: new Color(0.58f, 0.58f, 0.58f),
                metallic: 0.02f,
                smoothness: 0.18f,
                noiseScale: 5.0f,
                crackStrength: 0.75f);
            return _crackedConcrete;
        }

        private static Material GetDamagedBrickPlasterMaterial()
        {
            if (_damagedBrickPlaster != null)
                return _damagedBrickPlaster;

            _damagedBrickPlaster = MakeWarzoneMaterial(
                name: "M_Damaged_BrickPlaster",
                baseColor: new Color(0.52f, 0.34f, 0.28f),
                metallic: 0.02f,
                smoothness: 0.15f,
                noiseScale: 4.0f,
                crackStrength: 0.60f);
            return _damagedBrickPlaster;
        }

        private static Material GetRubbleGrimeMaterial()
        {
            if (_rubbleGrime != null)
                return _rubbleGrime;

            _rubbleGrime = MakeWarzoneMaterial(
                name: "M_Rubble_Grime",
                baseColor: new Color(0.25f, 0.24f, 0.22f),
                metallic: 0.03f,
                smoothness: 0.08f,
                noiseScale: 6.5f,
                crackStrength: 0.40f);
            return _rubbleGrime;
        }

        private static Material GetIndustrialMetalMaterial()
        {
            if (_industrialMetal != null)
                return _industrialMetal;

            _industrialMetal = MakeWarzoneMaterial(
                name: "M_Industrial_Metal",
                baseColor: new Color(0.38f, 0.43f, 0.48f),
                metallic: 0.70f,
                smoothness: 0.25f,
                noiseScale: 3.0f,
                crackStrength: 0.20f);
            return _industrialMetal;
        }

        private static Material MakeWarzoneMaterial(
            string name,
            Color baseColor,
            float metallic,
            float smoothness,
            float noiseScale,
            float crackStrength)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard"); // last attempt (should exist in Unity)

            var mat = new Material(shader)
            {
                name = name
            };

            mat.color = baseColor;
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));

            // Small procedural texture to break up "primitive" look.
            mat.mainTexture = GenerateWarzoneAlbedo(128, 128, noiseScale, crackStrength);
            return mat;
        }

        private static Texture2D GenerateWarzoneAlbedo(int w, int h, float noiseScale, float crackStrength)
        {
            w = Mathf.Clamp(w, 16, 256);
            h = Mathf.Clamp(h, 16, 256);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            var seedX = Random.Range(0f, 1000f);
            var seedY = Random.Range(0f, 1000f);

            // Create a noisy grayscale mask with "crack" lines from fBM-ish noise + thresholding.
            var pixels = new Color[w * h];
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var u = x / (float)w;
                    var v = y / (float)h;

                    var n1 = Mathf.PerlinNoise(seedX + u * noiseScale, seedY + v * noiseScale);
                    var n2 = Mathf.PerlinNoise(seedX + u * noiseScale * 2.1f, seedY + v * noiseScale * 2.1f) * 0.5f;
                    var n3 = Mathf.PerlinNoise(seedX + u * noiseScale * 4.2f, seedY + v * noiseScale * 4.2f) * 0.25f;
                    var n = Mathf.Clamp01(n1 * 0.65f + n2 * 0.25f + n3 * 0.10f);

                    // Crack mask: very dark thin ridges.
                    var crack = Mathf.SmoothStep(0.0f, 1.0f, (n - 0.72f) * 3.0f);
                    crack = Mathf.Clamp01(crackStrength * crack);

                    var value = Mathf.Clamp01(0.70f + (n - 0.5f) * 0.30f - crack * 0.55f);
                    pixels[y * w + x] = new Color(value, value, value, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }
    }
}

