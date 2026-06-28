using System.Collections.Generic;
using UnityEngine;

namespace DontDiePlease.Systems
{
    public enum SeededMapRoomType
    {
        Entrance,
        Storage,
        Maintenance,
        RobotCheckpoint,
        ToxicPocket
    }

    public sealed class SeededMapGenerator : MonoBehaviour
    {
        [SerializeField] private GameSeedManager seedManager;
        [SerializeField] private RandomEventManager randomEventManager;
        [SerializeField] private Transform generatedRoot;
        [SerializeField] private Transform mapEntrancePoint;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private bool movePlayerSpawnToEntrance;
        [SerializeField] private bool generateOnStart;
        [SerializeField] private bool clearBeforeGenerate = true;
        [SerializeField] private int width = 20;
        [SerializeField] private int height = 20;
        [SerializeField] private int mainCorridorLength = 14;
        [SerializeField] private int minRooms = 4;
        [SerializeField] private int maxRooms = 8;
        [SerializeField] private float cellSize = 4f;
        [SerializeField] private float floorY;
        [SerializeField] private float ceilingY = 3.2f;
        [SerializeField] private int propChance = 35;
        [SerializeField] private int resourceChance = 18;
        [SerializeField] private int ceilingChance = 45;
        [SerializeField] private int storageRoomChance = 34;
        [SerializeField] private int robotCheckpointChance = 24;
        [SerializeField] private int toxicPocketChance = 22;
        [SerializeField] private int robotSpawnCount = 3;
        [SerializeField] private int resourceSpawnCount = 3;
        [SerializeField] private GameObject[] floorPrefabs;
        [SerializeField] private GameObject[] wallPrefabs;
        [SerializeField] private GameObject[] doorPrefabs;
        [SerializeField] private GameObject[] cornerPrefabs;
        [SerializeField] private GameObject[] ceilingPrefabs;
        [SerializeField] private GameObject[] propPrefabs;
        [SerializeField] private GameObject[] resourcePrefabs;
        [SerializeField] private GameObject[] entrancePrefabs;
        [SerializeField] private GameObject[] storagePrefabs;
        [SerializeField] private GameObject[] maintenancePrefabs;
        [SerializeField] private GameObject[] robotCheckpointPrefabs;
        [SerializeField] private GameObject[] toxicPocketPrefabs;
        [SerializeField] private GameObject[] toxicVisualPrefabs;
        [SerializeField] private GameObject robotSpawnPointPrefab;
        [SerializeField] private GameObject resourceSpawnPointPrefab;

        private readonly HashSet<Vector2Int> floorCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> doorCells = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, RoomInfo> roomsByCell = new Dictionary<Vector2Int, RoomInfo>();
        private readonly List<RoomInfo> rooms = new List<RoomInfo>();
        private readonly List<RandomEventSpawnPoint> generatedSpawnPoints = new List<RandomEventSpawnPoint>();

        private static readonly Color FloorColor = new Color(0.17f, 0.19f, 0.18f, 1f);
        private static readonly Color WallColor = new Color(0.25f, 0.31f, 0.29f, 1f);
        private static readonly Color DoorColor = new Color(0.48f, 0.42f, 0.31f, 1f);
        private static readonly Color PropColor = new Color(0.38f, 0.42f, 0.39f, 1f);
        private static readonly Color ResourceColor = new Color(0.43f, 0.72f, 0.58f, 1f);
        private static readonly Color RobotColor = new Color(0.55f, 0.16f, 0.14f, 1f);
        private static readonly Color ToxicColor = new Color(0.43f, 0.45f, 0.42f, 1f);

        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        private sealed class RoomInfo
        {
            public RectInt rect;
            public Vector2Int center;
            public SeededMapRoomType type;
        }

        private void Start()
        {
            if (generateOnStart)
            {
                Generate();
            }
        }

        public void Generate()
        {
            ResolveRefs();

            if (clearBeforeGenerate)
            {
                ClearGeneratedMap();
            }

            floorCells.Clear();
            doorCells.Clear();
            roomsByCell.Clear();
            rooms.Clear();
            generatedSpawnPoints.Clear();

            var random = seedManager.CreateRandomStream("map-gen-v1");
            BuildLayout(random);
            SpawnFloors(random);
            SpawnWalls(random);
            SpawnCorners(random);
            SpawnDoors(random);
            SpawnCeilings(random);
            SpawnRoomContent(random);
            SpawnEventPoints(random);
            MovePlayerSpawn();
            ConnectEventManager();
        }

        public void ClearGeneratedMap()
        {
            var root = generatedRoot != null ? generatedRoot : transform;
            var pieces = root.GetComponentsInChildren<GeneratedMapPiece>(true);

            foreach (var piece in pieces)
            {
                if (piece == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(piece.gameObject);
                }
                else
                {
                    DestroyImmediate(piece.gameObject);
                }
            }

            generatedSpawnPoints.Clear();

            if (randomEventManager == null)
            {
                randomEventManager = FindObjectOfType<RandomEventManager>();
            }

            if (randomEventManager != null)
            {
                randomEventManager.SetSpawnPoints(new RandomEventSpawnPoint[0]);
            }
        }

        private void BuildLayout(System.Random random)
        {
            AddRoom(new RectInt(0, -1, 3, 3), SeededMapRoomType.Entrance);

            var corridorLength = Mathf.Clamp(mainCorridorLength, 6, Mathf.Max(6, width));

            for (var x = 0; x < corridorLength; x++)
            {
                floorCells.Add(new Vector2Int(x, 0));
            }

            var targetRooms = random.Next(Mathf.Max(1, minRooms), Mathf.Max(minRooms + 1, maxRooms + 1));
            var attempts = targetRooms * 12;

            for (var idx = 0; idx < attempts && rooms.Count < targetRooms + 1; idx++)
            {
                var roomW = Pick(random, 3, 4, 5);
                var roomH = Pick(random, 3, 4);
                var side = random.Next(0, 2) == 0 ? -1 : 1;
                var anchorX = random.Next(2, Mathf.Max(3, corridorLength - 2));
                var roomX = anchorX - roomW / 2;
                var roomY = side > 0 ? 2 : -roomH - 1;
                var rect = new RectInt(roomX, roomY, roomW, roomH);

                if (!IsInsideBounds(rect) || TouchesExistingFloor(rect))
                {
                    continue;
                }

                AddRoom(rect, PickRoomType(anchorX, corridorLength, random));
                var door = new Vector2Int(anchorX, side);
                floorCells.Add(door);
                doorCells.Add(door);
            }

            EnsureKeyRooms(random);
        }

        private void AddRoom(RectInt rect, SeededMapRoomType type)
        {
            var room = new RoomInfo
            {
                rect = rect,
                center = new Vector2Int(rect.xMin + rect.width / 2, rect.yMin + rect.height / 2),
                type = type
            };

            rooms.Add(room);

            for (var x = rect.xMin; x < rect.xMax; x++)
            {
                for (var y = rect.yMin; y < rect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    floorCells.Add(cell);
                    roomsByCell[cell] = room;
                }
            }
        }

        private SeededMapRoomType PickRoomType(int anchorX, int corridorLength, System.Random random)
        {
            var depth = corridorLength <= 0 ? 0f : anchorX / (float)corridorLength;
            var roll = random.Next(0, 100);

            if (depth > 0.58f && roll < robotCheckpointChance)
            {
                return SeededMapRoomType.RobotCheckpoint;
            }

            if (depth > 0.42f && roll < robotCheckpointChance + toxicPocketChance)
            {
                return SeededMapRoomType.ToxicPocket;
            }

            if (roll < robotCheckpointChance + toxicPocketChance + storageRoomChance)
            {
                return SeededMapRoomType.Storage;
            }

            return SeededMapRoomType.Maintenance;
        }

        private void EnsureKeyRooms(System.Random random)
        {
            EnsureRoomType(SeededMapRoomType.Storage, random);
            EnsureRoomType(SeededMapRoomType.ToxicPocket, random);
            EnsureRoomType(SeededMapRoomType.RobotCheckpoint, random);
        }

        private void EnsureRoomType(SeededMapRoomType type, System.Random random)
        {
            foreach (var room in rooms)
            {
                if (room.type == type)
                {
                    return;
                }
            }

            var candidates = new List<RoomInfo>();

            foreach (var room in rooms)
            {
                if (room.type == SeededMapRoomType.Maintenance)
                {
                    candidates.Add(room);
                }
            }

            if (candidates.Count == 0)
            {
                foreach (var room in rooms)
                {
                    if (room.type != SeededMapRoomType.Entrance)
                    {
                        candidates.Add(room);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                candidates[random.Next(0, candidates.Count)].type = type;
            }
        }

        private bool IsInsideBounds(RectInt rect)
        {
            var halfW = Mathf.Max(4, width / 2);
            var halfH = Mathf.Max(4, height / 2);
            return rect.xMin >= -halfW && rect.xMax <= halfW && rect.yMin >= -halfH && rect.yMax <= halfH;
        }

        private bool TouchesExistingFloor(RectInt rect)
        {
            for (var x = rect.xMin - 1; x <= rect.xMax; x++)
            {
                for (var y = rect.yMin - 1; y <= rect.yMax; y++)
                {
                    if (floorCells.Contains(new Vector2Int(x, y)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void SpawnFloors(System.Random random)
        {
            foreach (var cell in floorCells)
            {
                SpawnPiece(floorPrefabs, CellPos(cell), GridRot(0f), random, PrimitiveType.Cube, new Vector3(cellSize, 0.18f, cellSize), FloorColor);
            }
        }

        private void SpawnWalls(System.Random random)
        {
            foreach (var cell in floorCells)
            {
                for (var idx = 0; idx < Dirs.Length; idx++)
                {
                    var dir = Dirs[idx];

                    if (floorCells.Contains(cell + dir))
                    {
                        continue;
                    }

                    var pos = CellPos(cell) + GridOffset(dir, cellSize * 0.5f);
                    SpawnPiece(wallPrefabs, pos, GridRot(idx * 90f), random, PrimitiveType.Cube, WallScale(dir), WallColor);
                }
            }
        }

        private void SpawnCorners(System.Random random)
        {
            if (cornerPrefabs == null || cornerPrefabs.Length == 0)
            {
                return;
            }

            foreach (var cell in floorCells)
            {
                TrySpawnCorner(cell, new Vector2Int(1, 0), new Vector2Int(0, 1), 0f, random);
                TrySpawnCorner(cell, new Vector2Int(0, 1), new Vector2Int(-1, 0), 270f, random);
                TrySpawnCorner(cell, new Vector2Int(-1, 0), new Vector2Int(0, -1), 180f, random);
                TrySpawnCorner(cell, new Vector2Int(0, -1), new Vector2Int(1, 0), 90f, random);
            }
        }

        private void TrySpawnCorner(Vector2Int cell, Vector2Int a, Vector2Int b, float yRot, System.Random random)
        {
            if (floorCells.Contains(cell + a) || floorCells.Contains(cell + b))
            {
                return;
            }

            var pos = CellPos(cell) + GridOffset(a + b, cellSize * 0.5f);
            SpawnPiece(cornerPrefabs, pos, GridRot(yRot), random, PrimitiveType.Cube, Vector3.one, WallColor);
        }

        private void SpawnDoors(System.Random random)
        {
            foreach (var cell in doorCells)
            {
                SpawnPiece(doorPrefabs, CellPos(cell), GridRot(0f), random, PrimitiveType.Cube, new Vector3(cellSize * 0.75f, 2.6f, 0.35f), DoorColor);
            }
        }

        private void SpawnCeilings(System.Random random)
        {
            if (ceilingPrefabs == null || ceilingPrefabs.Length == 0)
            {
                return;
            }

            foreach (var cell in floorCells)
            {
                if (random.Next(0, 100) >= ceilingChance)
                {
                    continue;
                }

                SpawnPiece(ceilingPrefabs, CellPos(cell) + Vector3.up * ceilingY, GridRot(0f), random, PrimitiveType.Cube, new Vector3(cellSize, 0.18f, cellSize), WallColor);
            }
        }

        private void SpawnRoomContent(System.Random random)
        {
            foreach (var room in rooms)
            {
                SpawnRoomAnchor(room, random);

                for (var x = room.rect.xMin; x < room.rect.xMax; x++)
                {
                    for (var y = room.rect.yMin; y < room.rect.yMax; y++)
                    {
                        var cell = new Vector2Int(x, y);

                        if (doorCells.Contains(cell) || cell == Vector2Int.zero)
                        {
                            continue;
                        }

                        if (random.Next(0, 100) < ResourceChance(room.type))
                        {
                            SpawnPiece(resourcePrefabs, RandomCellPos(cell, random), RandomRot(random), random, PrimitiveType.Capsule, Vector3.one, ResourceColor);
                            continue;
                        }

                        if (random.Next(0, 100) < PropChance(room.type))
                        {
                            var obj = SpawnPiece(PropsFor(room.type), RandomCellPos(cell, random), RandomRot(random), random, PrimitiveType.Cube, Vector3.one, ColorFor(room.type));

                            if (obj != null && obj.GetComponent<SeededDecorationVariant>() == null)
                            {
                                obj.AddComponent<SeededDecorationVariant>();
                            }
                        }
                    }
                }
            }
        }

        private void SpawnRoomAnchor(RoomInfo room, System.Random random)
        {
            var cell = PickRoomCell(room, random);
            var pos = RandomCellPos(cell, random);

            switch (room.type)
            {
                case SeededMapRoomType.Entrance:
                    SpawnPiece(UseFallback(entrancePrefabs, maintenancePrefabs, propPrefabs), pos, RandomRot(random), random, PrimitiveType.Cube, new Vector3(1.4f, 1.2f, 1.4f), DoorColor);
                    break;
                case SeededMapRoomType.Storage:
                    SpawnPiece(UseFallback(storagePrefabs, propPrefabs), pos, RandomRot(random), random, PrimitiveType.Cube, new Vector3(1.3f, 1.1f, 1.3f), PropColor);
                    break;
                case SeededMapRoomType.Maintenance:
                    SpawnPiece(UseFallback(maintenancePrefabs, propPrefabs), pos, RandomRot(random), random, PrimitiveType.Cylinder, new Vector3(0.9f, 1.5f, 0.9f), PropColor);
                    break;
                case SeededMapRoomType.RobotCheckpoint:
                    SpawnPiece(UseFallback(robotCheckpointPrefabs, propPrefabs), pos, RandomRot(random), random, PrimitiveType.Cube, new Vector3(1.4f, 1.4f, 1.4f), RobotColor);
                    break;
                case SeededMapRoomType.ToxicPocket:
                    SpawnPiece(UseFallback(toxicVisualPrefabs, toxicPocketPrefabs, propPrefabs), pos + Vector3.up * 0.25f, RandomRot(random), random, PrimitiveType.Sphere, new Vector3(1.8f, 0.6f, 1.8f), ToxicColor);
                    break;
            }
        }

        private void SpawnEventPoints(System.Random random)
        {
            var used = new HashSet<Vector2Int>();
            var robotCandidates = CellsForType(SeededMapRoomType.RobotCheckpoint);
            var resourceCandidates = CellsForType(SeededMapRoomType.Storage);

            if (robotCandidates.Count == 0)
            {
                robotCandidates = DeepCells();
            }

            if (resourceCandidates.Count == 0)
            {
                resourceCandidates = DeepCells();
            }

            SpawnEventPoints(robotCandidates, robotSpawnCount, RandomEventType.RobotPatrol, robotSpawnPointPrefab, random, used);
            SpawnEventPoints(resourceCandidates, resourceSpawnCount, RandomEventType.ResourceDrop, resourceSpawnPointPrefab, random, used);
        }

        private void SpawnEventPoints(List<Vector2Int> candidates, int count, RandomEventType eventType, GameObject prefab, System.Random random, HashSet<Vector2Int> used)
        {
            for (var idx = 0; idx < count && candidates.Count > 0; idx++)
            {
                var pick = random.Next(0, candidates.Count);
                var cell = candidates[pick];
                candidates.RemoveAt(pick);

                if (used.Contains(cell))
                {
                    idx--;
                    continue;
                }

                used.Add(cell);

                var obj = SpawnPrefabOrEmpty(prefab, CellPos(cell) + Vector3.up * 0.2f, RandomRot(random), $"Generated {eventType} Spawn");
                var marker = obj.GetComponent<GeneratedMapPiece>();

                if (marker == null)
                {
                    obj.AddComponent<GeneratedMapPiece>();
                }

                var spawnPoint = obj.GetComponent<RandomEventSpawnPoint>();

                if (spawnPoint == null)
                {
                    spawnPoint = obj.AddComponent<RandomEventSpawnPoint>();
                }

                spawnPoint.SetEventType(eventType);
                spawnPoint.SetRadius(cellSize * 0.35f);
                generatedSpawnPoints.Add(spawnPoint);
            }
        }

        private List<Vector2Int> CellsForType(SeededMapRoomType type)
        {
            var cells = new List<Vector2Int>();

            foreach (var pair in roomsByCell)
            {
                if (pair.Value.type == type && !doorCells.Contains(pair.Key) && pair.Key != Vector2Int.zero)
                {
                    cells.Add(pair.Key);
                }
            }

            return cells;
        }

        private List<Vector2Int> DeepCells()
        {
            var cells = new List<Vector2Int>();

            foreach (var cell in floorCells)
            {
                if (Vector2Int.Distance(Vector2Int.zero, cell) >= 5f && !doorCells.Contains(cell))
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        private Vector2Int PickRoomCell(RoomInfo room, System.Random random)
        {
            for (var idx = 0; idx < 20; idx++)
            {
                var cell = new Vector2Int(random.Next(room.rect.xMin, room.rect.xMax), random.Next(room.rect.yMin, room.rect.yMax));

                if (!doorCells.Contains(cell) && cell != Vector2Int.zero)
                {
                    return cell;
                }
            }

            return room.center == Vector2Int.zero ? new Vector2Int(1, 0) : room.center;
        }

        private int ResourceChance(SeededMapRoomType type)
        {
            switch (type)
            {
                case SeededMapRoomType.Storage:
                    return Mathf.Max(resourceChance, 52);
                case SeededMapRoomType.Entrance:
                    return Mathf.Min(resourceChance, 12);
                case SeededMapRoomType.RobotCheckpoint:
                    return Mathf.Min(resourceChance, 8);
                case SeededMapRoomType.ToxicPocket:
                    return Mathf.Max(resourceChance, 28);
                default:
                    return resourceChance;
            }
        }

        private int PropChance(SeededMapRoomType type)
        {
            switch (type)
            {
                case SeededMapRoomType.Entrance:
                    return Mathf.Max(propChance, 35);
                case SeededMapRoomType.Storage:
                    return Mathf.Max(propChance, 58);
                case SeededMapRoomType.Maintenance:
                    return Mathf.Max(propChance, 55);
                case SeededMapRoomType.RobotCheckpoint:
                    return Mathf.Max(propChance, 42);
                case SeededMapRoomType.ToxicPocket:
                    return Mathf.Max(propChance, 48);
                default:
                    return propChance;
            }
        }

        private GameObject[] PropsFor(SeededMapRoomType type)
        {
            switch (type)
            {
                case SeededMapRoomType.Entrance:
                    return UseFallback(entrancePrefabs, maintenancePrefabs, propPrefabs);
                case SeededMapRoomType.Storage:
                    return UseFallback(storagePrefabs, propPrefabs);
                case SeededMapRoomType.Maintenance:
                    return UseFallback(maintenancePrefabs, propPrefabs);
                case SeededMapRoomType.RobotCheckpoint:
                    return UseFallback(robotCheckpointPrefabs, propPrefabs);
                case SeededMapRoomType.ToxicPocket:
                    return UseFallback(toxicPocketPrefabs, toxicVisualPrefabs, propPrefabs);
                default:
                    return propPrefabs;
            }
        }

        private Color ColorFor(SeededMapRoomType type)
        {
            switch (type)
            {
                case SeededMapRoomType.RobotCheckpoint:
                    return RobotColor;
                case SeededMapRoomType.ToxicPocket:
                    return ToxicColor;
                case SeededMapRoomType.Storage:
                    return ResourceColor;
                default:
                    return PropColor;
            }
        }

        private GameObject[] UseFallback(params GameObject[][] groups)
        {
            foreach (var group in groups)
            {
                if (group != null && group.Length > 0)
                {
                    return group;
                }
            }

            return null;
        }

        private void MovePlayerSpawn()
        {
            if (!movePlayerSpawnToEntrance || playerSpawnPoint == null)
            {
                return;
            }

            playerSpawnPoint.position = CellPos(Vector2Int.zero) + Vector3.up * 1.2f;
            playerSpawnPoint.rotation = GridRot(0f);
        }

        private void ConnectEventManager()
        {
            if (randomEventManager == null)
            {
                randomEventManager = FindObjectOfType<RandomEventManager>();
            }

            if (randomEventManager != null)
            {
                randomEventManager.SetSpawnPoints(generatedSpawnPoints.ToArray());
            }
        }

        private GameObject SpawnPiece(GameObject[] prefabs, Vector3 pos, Quaternion rot, System.Random random, PrimitiveType fallbackType, Vector3 fallbackScale, Color fallbackColor)
        {
            var prefab = PickPrefab(prefabs, random);
            var obj = prefab != null ? SpawnPrefabOrEmpty(prefab, pos, rot, prefab.name) : GameObject.CreatePrimitive(fallbackType);

            obj.name = prefab != null ? $"Generated {prefab.name}" : $"Generated {fallbackType}";
            obj.transform.SetParent(ResolveRoot(), true);
            obj.transform.SetPositionAndRotation(pos, rot);

            if (prefab == null)
            {
                obj.transform.localScale = fallbackScale;
                PaintFallback(obj, fallbackColor);
            }

            if (obj.GetComponent<GeneratedMapPiece>() == null)
            {
                obj.AddComponent<GeneratedMapPiece>();
            }

            return obj;
        }

        private GameObject SpawnPrefabOrEmpty(GameObject prefab, Vector3 pos, Quaternion rot, string fallbackName)
        {
            var obj = prefab != null ? Instantiate(prefab, pos, rot) : new GameObject(fallbackName);
            obj.transform.SetParent(ResolveRoot(), true);
            obj.transform.SetPositionAndRotation(pos, rot);
            return obj;
        }

        private Transform ResolveRoot()
        {
            return generatedRoot != null ? generatedRoot : transform;
        }

        private void ResolveRefs()
        {
            if (seedManager == null)
            {
                seedManager = GameSeedManager.Instance != null ? GameSeedManager.Instance : FindObjectOfType<GameSeedManager>();
            }

            if (seedManager == null)
            {
                seedManager = gameObject.AddComponent<GameSeedManager>();
            }

            if (!seedManager.HasSeed || !Application.isPlaying)
            {
                seedManager.InitialiseRun();
            }
        }

        private Vector3 CellPos(Vector2Int cell)
        {
            var origin = mapEntrancePoint != null ? mapEntrancePoint.position : transform.position;
            return origin + MapRot() * new Vector3(cell.y * cellSize, floorY, cell.x * cellSize);
        }

        private Vector3 RandomCellPos(Vector2Int cell, System.Random random)
        {
            var x = ((float)random.NextDouble() - 0.5f) * cellSize * 0.45f;
            var z = ((float)random.NextDouble() - 0.5f) * cellSize * 0.45f;
            return CellPos(cell) + MapRot() * new Vector3(x, 0.2f, z);
        }

        private Quaternion RandomRot(System.Random random)
        {
            return GridRot(random.Next(0, 4) * 90f);
        }

        private Quaternion GridRot(float yRot)
        {
            return MapRot() * Quaternion.Euler(0f, yRot, 0f);
        }

        private Quaternion MapRot()
        {
            var euler = mapEntrancePoint != null ? mapEntrancePoint.eulerAngles : transform.eulerAngles;
            return Quaternion.Euler(0f, euler.y, 0f);
        }

        private Vector3 GridOffset(Vector2Int dir, float distance)
        {
            return MapRot() * new Vector3(dir.y * distance, 0f, dir.x * distance);
        }

        private Vector3 WallScale(Vector2Int dir)
        {
            return dir.x != 0 ? new Vector3(0.2f, 2.8f, cellSize) : new Vector3(cellSize, 2.8f, 0.2f);
        }

        private static GameObject PickPrefab(GameObject[] prefabs, System.Random random)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return null;
            }

            return prefabs[random.Next(0, prefabs.Length)];
        }

        private static int Pick(System.Random random, params int[] values)
        {
            return values[random.Next(0, values.Length)];
        }

        private static void PaintFallback(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
    }
}
