using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Tilemap to paint tiles onto.")]
    public Tilemap tilemap;

    [Tooltip("The tile asset to paint with.")]
    public TileBase tile;

    [Header("Path Settings")]
    [Tooltip("Cell the path starts from.")]
    public Vector3Int startCell = Vector3Int.zero;

    [Tooltip("Total number of tiles to place (including the start tile).")]
    public int pathLength = 40;

    [Tooltip("Maximum tiles placed in any single row.")]
    public int maxTilesPerRow = 5;

    [Tooltip("How many horizontal steps in a row before the generator is forced upward.")]
    public int maxHorizontalStreak = 3;

    [Header("Randomness")]
    [Tooltip("Chance weight for moving upward vs. sideways.")]
    [Range(1, 10)] public int upWeight    = 5;
    [Tooltip("Chance weight for moving left.")]
    [Range(1, 10)] public int leftWeight  = 2;
    [Tooltip("Chance weight for moving right.")]
    [Range(1, 10)] public int rightWeight = 2;

    [Header("Win Collectible")]
    [Tooltip("Prefab with WinCollectible component to spawn on the final tile.")]
    public GameObject winCollectiblePrefab;

    [Header("Options")]
    [Tooltip("Clear all tiles on the Tilemap before generating.")]
    public bool clearOnGenerate = true;

    [Tooltip("Use a fixed seed for reproducible results. 0 = random each time.")]
    public int seed = 0;

    private Dictionary<int, int> _rowCount = new Dictionary<int, int>();
    private HashSet<Vector3Int>  _placed   = new HashSet<Vector3Int>();
    private Vector3Int           _goalCell;

    void Awake()
    {
        Generate();
    }

    void Start()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.SetupGoal();

        if (winCollectiblePrefab != null)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(_goalCell);
            GameObject go = Instantiate(winCollectiblePrefab, worldPos, Quaternion.identity);
            WinCollectible wc = go.GetComponent<WinCollectible>();
            if (wc != null && CollectibleManager.Instance != null)
            {
                CollectibleItemData item = CollectibleManager.Instance.GetNext();
                if (item != null)
                    wc.Apply(item);
            }
        }
    }

    [ContextMenu("Generate Path")]
    public void Generate()
    {
        if (tilemap == null || tile == null)
            return;

        if (seed != 0)
            Random.InitState(seed);

        if (clearOnGenerate)
            tilemap.ClearAllTiles();

        _rowCount.Clear();
        _placed.Clear();

        Vector3Int current = startCell;
        PlaceTile(current);

        int horizontalStreak = 0;
        int maxAttempts      = pathLength * 20;
        int attempts         = 0;
        int placed           = 1;

        while (placed < pathLength && attempts < maxAttempts)
        {
            attempts++;

            bool forceUp = horizontalStreak >= maxHorizontalStreak;
            Vector3Int next = PickNext(current, forceUp);

            if (next == current)
            {
                Vector3Int escape = current + new Vector3Int(0, 2, 0);
                if (IsValid(escape))
                {
                    Vector3Int bridge = current + Vector3Int.up;
                    if (IsValid(bridge))
                    {
                        PlaceTile(bridge);
                        placed++;
                    }
                    PlaceTile(escape);
                    placed++;
                    current = escape;
                    horizontalStreak = 0;
                }
                continue;
            }

            if (next.y == current.y)
                horizontalStreak++;
            else
                horizontalStreak = 0;

            PlaceTile(next);
            current = next;
            placed++;
        }

        if (current == startCell)
        {
            current = startCell + Vector3Int.up;
            PlaceTile(current);
        }

        _goalCell = current;
        if (GridManager.Instance != null)
        {
            GridManager.Instance.goalCell = current;
            GridManager.Instance.SetupGoal();
        }
    }


    Vector3Int PickNext(Vector3Int from, bool forceUp)
    {
        List<Vector3Int> pool = new List<Vector3Int>();

        Vector3Int up    = from + Vector3Int.up;
        Vector3Int left  = from + Vector3Int.left;
        Vector3Int right = from + Vector3Int.right;

        if (IsValid(up))
        {
            for (int i = 0; i < upWeight; i++)
                pool.Add(up);
        }

        if (!forceUp)
        {
            if (IsValid(left))
                for (int i = 0; i < leftWeight; i++)
                    pool.Add(left);

            if (IsValid(right))
                for (int i = 0; i < rightWeight; i++)
                    pool.Add(right);
        }

        if (pool.Count == 0)
            return from;

        return pool[Random.Range(0, pool.Count)];
    }

    bool IsValid(Vector3Int cell)
    {
        if (cell.y < startCell.y) return false;
        if (_placed.Contains(cell)) return false;
        int count = _rowCount.ContainsKey(cell.y) ? _rowCount[cell.y] : 0;
        if (count >= maxTilesPerRow) return false;
        return true;
    }

    void PlaceTile(Vector3Int cell)
    {
        tilemap.SetTile(cell, tile);
        _placed.Add(cell);

        if (!_rowCount.ContainsKey(cell.y))
            _rowCount[cell.y] = 0;
        _rowCount[cell.y]++;
    }
}
