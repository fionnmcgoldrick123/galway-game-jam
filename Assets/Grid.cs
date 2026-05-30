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

    [Header("Debug")]
    public bool showDebugGizmos = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
