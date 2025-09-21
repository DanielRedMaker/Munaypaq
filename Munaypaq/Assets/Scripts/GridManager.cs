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
    public GameOverMenu gameOverMenu;
    public static GridManager Instance;

    void Awake()
    {
        Instance = this;
        floorTilemap = GameObject.FindWithTag("Walkable").GetComponent<Tilemap>();
    }
    void Start()
    {
        // Intentar iniciar la sesión, esperando ScoreManager si aún no existe
        StartCoroutine(TryStartScoreSession());
    }

    private System.Collections.IEnumerator TryStartScoreSession()
    {
        // Esperar 1 frame por si ScoreManager se instancia en Awake de otro GameObject
        yield return null;

        int tries = 0;
        while (ScoreManager.Instance == null && tries < 10)
        {
            tries++;
            // esperar frame(s) para que Singleton se instancie si viene de otra escena
            yield return null;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.StartSession();
            Debug.Log("GridManager: ScoreManager encontrado -> StartSession() llamado.");
        }
        else
        {
            Debug.LogWarning("GridManager: ScoreManager no encontrado después de esperar. StartSession() no fue llamado.");
        }
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

    public bool HasTrashAt(Vector3 position)
    {
        foreach (GameObject trash in allTrash)
        {
            if (trash != null && Vector3.Distance(trash.transform.position, position) < 0.7f)
            {
                return true;
            }
        }
        return false;
    }

    public int GetTrashCountInRadius(Vector3 position, float radius)
    {
        int count = 0;
        foreach (GameObject trash in allTrash)
        {
            if (trash != null && Vector3.Distance(trash.transform.position, position) <= radius)
            {
                count++;
            }
        }
        return count;
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
            gameOverMenu.ShowGameOver();
        }
    }

    // Método para obtener estadísticas del juego
    public void GetGameStats(out int trashCount, out int maxTrashCount, out float percentage)
    {
        // Limpiar nulls primero
        allTrash.RemoveAll(item => item == null);

        trashCount = allTrash.Count;
        maxTrashCount = maxTrash;
        percentage = (float)trashCount / maxTrash * 100f;
    }

    void Update()
    {
        // UI Debug mejorada
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            GetGameStats(out int count, out int max, out float percentage);
            Debug.Log($"Basura: {count}/{max} ({percentage:F1}%)");

            // Contar NPCs buenos vs malos
            NPCBase[] npcs = FindObjectsOfType<NPCBase>();
            int goodNPCs = 0, badNPCs = 0;

            foreach (NPCBase npc in npcs)
            {
                if (npc.isGoodNPC) goodNPCs++;
                else badNPCs++;
            }

            Debug.Log($"NPCs: {goodNPCs} buenos, {badNPCs} malos");
        }
    }

    public int GetEstimatedWalkableTiles()
    {
        BoundsInt bounds = floorTilemap.cellBounds;
        int totalTiles = bounds.size.x * bounds.size.y;
        return Mathf.RoundToInt(totalTiles * 0.7f); // 70% estimado como caminable
    }
}