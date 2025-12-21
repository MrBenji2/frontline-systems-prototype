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
        }

        private void CreateBlock(Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Obstacle_Block";
            go.layer = DefaultLayer;
            go.transform.position = pos;
            go.transform.localScale = size;
        }
    }
}

