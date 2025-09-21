using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("References")]
    public List<GameObject> trashPrefabs;
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

        // Verificar que no haya obstáculos físicos (ignoramos triggers como powerups)
        Collider2D obstacle = Physics2D.OverlapPoint(worldPosition);
        if (obstacle == null) return true;

        // Si el collider es trigger (powerups, triggers visuales), lo ignoramos y consideramos walkable
        if (obstacle.isTrigger) return true;

        // Si el collider está marcado explícitamente como "Walkable" (p.ej. zonas con colliders), permitirlo
        if (obstacle.CompareTag("Walkable")) return true;

        // Si es un powerup y por algún motivo no es trigger, también lo permitimos (por seguridad)
        if (obstacle.CompareTag("Powerup")) return true;

        // Si llegamos aquí, hay un obstáculo sólido -> no es walkable
        return false;
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
    // Nuevo: crear basura eligiendo un prefab al azar
    public void CreateTrashRandom(Vector3 position)
    {
        if (allTrash.Count >= maxTrash) return;
        if (trashPrefabs == null || trashPrefabs.Count == 0) return;

        Vector3 tileCenter = GetNearestWalkableTile(position);

        // Verificar que no haya basura ya ahí
        foreach (GameObject trash in allTrash)
        {
            if (trash != null && Vector3.Distance(trash.transform.position, tileCenter) < 0.5f)
                return;
        }

        GameObject chosen = trashPrefabs[Random.Range(0, trashPrefabs.Count)];
        GameObject newTrash = Instantiate(chosen, tileCenter, Quaternion.identity, trashParent);
        allTrash.Add(newTrash);

        CheckLoseCondition();
    }

    // Overload de CreateTrash para compatibilidad con código anterior
    public void CreateTrash(Vector3 position)
    {
        CreateTrashRandom(position);
    }
    // Limpia un área rectangular centrada en center (width x height en tiles)
    public void CleanArea(Vector3 center, int widthTiles = 2, int heightTiles = 2)
    {
        // Obtener el centro tile
        Vector3Int centerCell = floorTilemap.WorldToCell(center);

        int halfW = widthTiles / 2;
        int halfH = heightTiles / 2;

        // Recorremos las celdas del rectángulo y destruimos basura en ellas
        for (int dx = -halfW; dx <= halfW; dx++)
        {
            for (int dy = -halfH; dy <= halfH; dy++)
            {
                Vector3Int cell = centerCell + new Vector3Int(dx, dy, 0);
                Vector3 cellWorld = floorTilemap.GetCellCenterWorld(cell);

                // Eliminar basura cercana a esta center
                for (int i = allTrash.Count - 1; i >= 0; i--)
                {
                    var t = allTrash[i];
                    if (t == null)
                    {
                        allTrash.RemoveAt(i);
                        continue;
                    }

                    if (Vector3.Distance(t.transform.position, cellWorld) < 0.7f)
                    {
                        Destroy(t);
                        allTrash.RemoveAt(i);
                    }
                }
            }
        }

        // Re-evaluar condición de pérdida por si cambió
        CheckLoseCondition();
    }
    public bool CleanTrash(Vector3 position)
    {
        // Intentamos eliminar la basura más cercana al position y devolvemos true si lo hicimos
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
                // Después de eliminar una basura, reevaluar condición de pérdida
                CheckLoseCondition();
                return true;
            }
        }

        // No se encontró basura que limpiar en la posición
        return false;
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