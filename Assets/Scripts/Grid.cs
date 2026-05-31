using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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


    private HashSet<Vector3Int> _visited = new HashSet<Vector3Int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        _visited.Clear();
    }

    public void SetupGoal()
    {
        if (goalTile != null)
            tilemap.SetTile(goalCell, goalTile);
    }

    public bool IsVisited(Vector3Int cell) => _visited.Contains(cell);

    public void MarkVisited(Vector3Int cell, bool applyVisual = true)
    {
        if (_visited.Contains(cell)) 
            return;
        
        _visited.Add(cell);

        if (cell == goalCell)
            return;
        
        if (applyVisual && visitedTile != null)
        {
            tilemap.SetTile(cell, visitedTile);
        }
    }

    public void FlashCell(Vector3Int cell)
    {
        StartCoroutine(FlashCoroutine(cell));
    }

    IEnumerator FlashCoroutine(Vector3Int cell)
    {
        Vector3 worldPos = tilemap.GetCellCenterWorld(cell);

        GameObject flashObj = new GameObject("TileFlash");
        flashObj.transform.position = new Vector3(worldPos.x, worldPos.y, -0.1f);
        flashObj.transform.localScale = new Vector3(tilemap.cellSize.x, tilemap.cellSize.y, 1f);

        SpriteRenderer sr = flashObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = tilemap.GetComponent<Renderer>().sortingLayerName;
        sr.sortingOrder = tilemap.GetComponent<Renderer>().sortingOrder + 1;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        float elapsed = 0f;
        float duration = flashDuration * 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sr.color = new Color(1f, 1f, 1f, 1f - (elapsed / duration));
            yield return null;
        }

        Destroy(tex);
        Destroy(flashObj);
    }

    public bool HasVisitedAll()
    {
        int total = 0;
        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            var cell = new Vector3Int(pos.x, pos.y, 0);
            if (tilemap.HasTile(cell) && cell != goalCell)
                total++;
        }
        return _visited.Count >= total;
    }

    public bool HasTileAt(Vector3Int cellPos)
    {
        return tilemap.HasTile(cellPos);
    }

    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        // GetCellCenterWorld gives the exact centre of the tile grid cell.
        return tilemap.GetCellCenterWorld(cellPos);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return tilemap.WorldToCell(worldPos);
    }

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
