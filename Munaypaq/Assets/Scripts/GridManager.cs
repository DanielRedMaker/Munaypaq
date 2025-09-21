using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("References")]
    public GameObject trashPrefab;
    public Transform trashParent;

    [Header("Game Settings")]
    public int maxTrash = 20;
    [Range(0f, 1f)]
    public float loseThreshold = 0.8f;

    private List<GameObject> allTrash = new List<GameObject>();
    private Tilemap floorTilemap;

    public static GridManager Instance;

    void Awake()
    {
        Instance = this;
        floorTilemap = GameObject.FindWithTag("Walkable").GetComponent<Tilemap>();
    }

    public bool IsWalkable(Vector3 worldPosition)
    {
        // Convertir a posición de grid
        Vector3Int gridPos = floorTilemap.WorldToCell(worldPosition);

        // Verificar si hay tile caminable
        TileBase tile = floorTilemap.GetTile(gridPos);
        if (tile == null) return false;

        // Verificar que no haya obstáculos
        Collider2D obstacle = Physics2D.OverlapPoint(worldPosition);
        return obstacle == null || obstacle.CompareTag("Walkable");
    }

    public Vector3 GetNearestWalkableTile(Vector3 worldPosition)
    {
        Vector3Int gridPos = floorTilemap.WorldToCell(worldPosition);
        Vector3 centerPos = floorTilemap.GetCellCenterWorld(gridPos);

        if (IsWalkable(centerPos))
            return centerPos;

        // Buscar en tiles adyacentes
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector3Int newGridPos = gridPos + new Vector3Int(x, y, 0);
                Vector3 newWorldPos = floorTilemap.GetCellCenterWorld(newGridPos);

                if (IsWalkable(newWorldPos))
                    return newWorldPos;
            }
        }

        return centerPos; // Fallback
    }

    public void CreateTrash(Vector3 position)
    {
        if (allTrash.Count >= maxTrash) return;

        Vector3 tileCenter = GetNearestWalkableTile(position);

        // Verificar que no haya basura ya ahí
        foreach (GameObject trash in allTrash)
        {
            if (trash != null && Vector3.Distance(trash.transform.position, tileCenter) < 0.5f)
                return;
        }

        GameObject newTrash = Instantiate(trashPrefab, tileCenter, Quaternion.identity, trashParent);
        allTrash.Add(newTrash);

        CheckLoseCondition();
    }

    public void CleanTrash(Vector3 position)
    {
        for (int i = allTrash.Count - 1; i >= 0; i--)
        {
            if (allTrash[i] == null)
            {
                allTrash.RemoveAt(i);
                continue;
            }

            if (Vector3.Distance(allTrash[i].transform.position, position) < 0.7f)
            {
                Destroy(allTrash[i]);
                allTrash.RemoveAt(i);
                break;
            }
        }
    }

    public Vector3 GetRandomWalkablePosition()
    {
        BoundsInt bounds = floorTilemap.cellBounds;

        for (int attempts = 0; attempts < 50; attempts++)
        {
            int randomX = Random.Range(bounds.xMin, bounds.xMax);
            int randomY = Random.Range(bounds.yMin, bounds.yMax);
            Vector3Int gridPos = new Vector3Int(randomX, randomY, 0);
            Vector3 worldPos = floorTilemap.GetCellCenterWorld(gridPos);

            if (IsWalkable(worldPos))
                return worldPos;
        }

        return Vector3.zero;
    }

    void CheckLoseCondition()
    {
        float trashPercentage = (float)allTrash.Count / maxTrash;
        if (trashPercentage >= loseThreshold)
        {
            Debug.Log("¡GAME OVER! Demasiada basura!");
            // Aquí puedes pausar el juego o mostrar pantalla de Game Over
        }
    }

    void Update()
    {
        // UI Debug
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log($"Basura: {allTrash.Count}/{maxTrash} ({(float)allTrash.Count / maxTrash * 100:F1}%)");
        }
    }
}