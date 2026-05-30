using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Attach to the Grid GameObject in the scene.
/// Provides helpers to check tile existence and convert between
/// grid cell positions and world positions.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Tilemap Reference")]
    [Tooltip("Drag the Tilemap child of this Grid here.")]
    public Tilemap tilemap;

    [Header("Column Limit")]
    [Tooltip("Maximum number of tiles allowed per row (enforced at placement time via HasTile checks).")]
    public int maxTilesPerRow = 3;

    [Header("Tile Visuals")]
    [Tooltip("Tile to swap in after the player walks on a cell (shows it has been visited).")]
    public TileBase visitedTile;

    [Tooltip("Tile placed on the goal cell (the final winning tile).")]
    public TileBase goalTile;

    [Header("Level Goal")]
    [Tooltip("Grid cell that acts as the level exit. Set manually or auto-set by LevelGenerator.")]
    public Vector3Int goalCell;

    [Header("Step Flash")]
    [Tooltip("Duration of the white flash when the player lands on a tile.")]
    public float flashDuration = 0.12f;

    [Header("Debug")]
    public bool showDebugGizmos = false;

    // ── visited tracking ─────────────────────────────────────────────────────
    private HashSet<Vector3Int> _visited = new HashSet<Vector3Int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Fresh start on scene reload
        _visited.Clear();
    }

    /// <summary>Places the goal tile visually. Call this after all tiles have been placed.</summary>
    public void SetupGoal()
    {
        if (goalTile != null)
            tilemap.SetTile(goalCell, goalTile);
    }

    /// <summary>Returns true if this cell has already been stepped on.</summary>
    public bool IsVisited(Vector3Int cell) => _visited.Contains(cell);

    /// <summary>Marks a cell as visited and optionally swaps its tile to the visited visual.</summary>
    public void MarkVisited(Vector3Int cell, bool applyVisual = true)
    {
        if (_visited.Contains(cell)) 
            return;
        
        _visited.Add(cell);

        // Never overwrite the goal tile with the visited visual.
        if (cell == goalCell)
            return;
        
        if (applyVisual && visitedTile != null)
        {
            tilemap.SetTile(cell, visitedTile);
        }
        else if (applyVisual && visitedTile == null)
        {
            Debug.LogWarning($"[GridManager] No visited tile assigned! Cell {cell} marked but not visually updated.");
        }
    }

    /// <summary>Briefly flashes a tile white, then restores its original colour.</summary>
    public void FlashCell(Vector3Int cell)
    {
        StartCoroutine(FlashCoroutine(cell));
    }

    IEnumerator FlashCoroutine(Vector3Int cell)
    {
        Vector3 worldPos = tilemap.GetCellCenterWorld(cell);

        // Spawn a temporary white quad over the tile, above tilemap but below player.
        GameObject flashObj = new GameObject("TileFlash");
        flashObj.transform.position = new Vector3(worldPos.x, worldPos.y, -0.1f);
        flashObj.transform.localScale = new Vector3(tilemap.cellSize.x, tilemap.cellSize.y, 1f);

        SpriteRenderer sr = flashObj.AddComponent<SpriteRenderer>();
        // Match the tilemap's sorting layer; order 1 = above tilemap (0), below player (set player to 2+).
        sr.sortingLayerName = tilemap.GetComponent<Renderer>().sortingLayerName;
        sr.sortingOrder = tilemap.GetComponent<Renderer>().sortingOrder + 1;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        // Instant white, then fade out quickly.
        float elapsed = 0f;
        float duration = flashDuration * 0.5f; // half the inspector value for a snappier flash
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sr.color = new Color(1f, 1f, 1f, 1f - (elapsed / duration));
            yield return null;
        }

        Destroy(tex);
        Destroy(flashObj);
    }

    /// <summary>Returns true when every non-goal tile has been visited.</summary>
    public bool HasVisitedAll()
    {
        int total = 0;
        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            // z is always 0 in 2D tilemaps
            var cell = new Vector3Int(pos.x, pos.y, 0);
            if (tilemap.HasTile(cell) && cell != goalCell)
                total++;
        }
        return _visited.Count >= total;
    }

    /// <summary>Returns true if a tile exists at the given cell coordinate.</summary>
    public bool HasTileAt(Vector3Int cellPos)
    {
        return tilemap.HasTile(cellPos);
    }

    /// <summary>Converts a cell position to the centre of that cell in world space.</summary>
    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        // GetCellCenterWorld gives the exact centre of the tile grid cell.
        return tilemap.GetCellCenterWorld(cellPos);
    }

    /// <summary>Converts a world position to the cell it sits in.</summary>
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return tilemap.WorldToCell(worldPos);
    }

    /// <summary>
    /// Returns how many tiles exist in the given row (y value).
    /// Iterates over the tilemap bounds so it works for any layout.
    /// </summary>
    public int TileCountInRow(int row)
    {
        int count = 0;
        BoundsInt bounds = tilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            var cell = new Vector3Int(x, row, 0);
            if (tilemap.HasTile(cell))
                count++;
        }
        return count;
    }

    /// <summary>Debug visualization of tile centers in the scene view.</summary>
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || tilemap == null) return;

        Gizmos.color = Color.green;
        BoundsInt bounds = tilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (tilemap.HasTile(cell))
                {
                    Vector3 center = tilemap.GetCellCenterWorld(cell);
                    Gizmos.DrawWireCube(center, Vector3.one * 0.9f);
                }
            }
        }
    }
}
