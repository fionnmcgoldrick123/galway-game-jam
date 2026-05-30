using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Procedurally generates a connected upward-moving path on the Tilemap.
///
/// Setup:
///   1. Create an empty GameObject in the scene and attach this script.
///   2. Assign the same Tilemap used by GridManager.
///   3. Assign a TileBase (drag a Tile asset from your Art folder).
///   4. Set Start Cell to where the path should begin.
///   5. Hit Play, or right-click this component → Generate Path to preview in Editor.
///
/// Rules:
///   - Path never goes downward.
///   - After MaxHorizontalStreak consecutive horizontal steps it forces an upward move.
///   - No cell is visited twice.
///   - No row will have more than MaxTilesPerRow tiles.
/// </summary>
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

    [Header("Options")]
    [Tooltip("Clear all tiles on the Tilemap before generating.")]
    public bool clearOnGenerate = true;

    [Tooltip("Use a fixed seed for reproducible results. 0 = random each time.")]
    public int seed = 0;

    // ── internals ────────────────────────────────────────────────────────────
    private Dictionary<int, int> _rowCount = new Dictionary<int, int>();
    private HashSet<Vector3Int>  _placed   = new HashSet<Vector3Int>();

    void Start()
    {
        Generate();
    }

    /// <summary>Generate a new path. Also callable via the Inspector context menu.</summary>
    [ContextMenu("Generate Path")]
    public void Generate()
    {
        if (tilemap == null || tile == null)
        {
            Debug.LogError("[LevelGenerator] Tilemap or Tile not assigned.");
            return;
        }

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
                // Completely stuck — try forcing a jump up two rows to escape
                Vector3Int escape = current + new Vector3Int(0, 2, 0);
                if (IsValid(escape))
                {
                    // bridge the gap with an intermediate tile
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

            // Track horizontal streak
            if (next.y == current.y)
                horizontalStreak++;
            else
                horizontalStreak = 0;

            PlaceTile(next);
            current = next;
            placed++;
        }

        Debug.Log($"[LevelGenerator] Placed {placed} tiles. Seed: {seed}. Goal cell: {current}.");

        // Register the last tile as the goal cell and apply its visual.
        if (GridManager.Instance != null)
        {
            GridManager.Instance.goalCell = current;
            GridManager.Instance.SetupGoal();
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

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
            return from; // stuck

        return pool[Random.Range(0, pool.Count)];
    }

    bool IsValid(Vector3Int cell)
    {
        // Never go downward
        if (cell.y < startCell.y) return false;

        // No revisits
        if (_placed.Contains(cell)) return false;

        // Row tile limit
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
