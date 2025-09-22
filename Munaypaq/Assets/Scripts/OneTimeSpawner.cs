using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneTimeSpawner : MonoBehaviour
{
    [Header("Prefabs & References")]
    public GameObject npcPrefab;                    // Prefab que contiene NPCBase
    public CleaningProgressBar npcProgressBarPrefab; // opcional: prefab de progress bar para asignar a NPCs
    public GameObject dirtyTileVisualizerPrefab;    // opcional: visual para basura si usas uno aparte
    [Header("NPC Sprites")]
    public List<Sprite> goodNPCSprites;    // Asigna en el Inspector
    public List<Sprite> badNPCSprites;
    [Header("Counts (one-time spawn)")]
    public int dirtyTilesCount = 5;
    public int badNPCCount = 5;
    public int goodNPCCount = 3;

    [Header("Spawn Settings")]
    public int maxAttemptsPerItem = 50; // intentos para encontrar una casilla válida antes de saltar
    public float tileSnapTolerance = 0.1f; // tolerancia al usar GetNearestWalkableTile

    // Usamos un conjunto de coordenadas discretas (Vector2Int) para evitar solapamientos por precisión
    private HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    void Start()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("OneTimeSpawner: GridManager.Instance es null. Asegúrate de tener un GridManager en la escena.");
            return;
        }

        occupied.Clear();

        SpawnDirtyTiles(dirtyTilesCount);
        SpawnNPCs(badNPCCount, asGood: false);
        SpawnNPCs(goodNPCCount, asGood: true);

        Debug.Log($"OneTimeSpawner: spawn completado -> Dirty:{dirtyTilesCount} BadNPC:{badNPCCount} GoodNPC:{goodNPCCount}");
    }

    void SpawnDirtyTiles(int count)
    {
        int spawned = 0;
        int attempts = 0;

        while (spawned < count && attempts < count * maxAttemptsPerItem)
        {
            attempts++;
            Vector3 randomPos = GridManager.Instance.GetRandomWalkablePosition();
            Vector3 tilePos = GridManager.Instance.GetNearestWalkableTile(randomPos);

            Vector2Int key = TileKey(tilePos);
            if (occupied.Contains(key)) continue;

            // Si ya hay basura en esa tile, consideramos no volver a crearla (evita duplicados)
            if (GridManager.Instance.HasTrashAt(tilePos))
            {
                occupied.Add(key);
                continue;
            }

            GridManager.Instance.CreateTrash(tilePos);

            if (dirtyTileVisualizerPrefab != null)
            {
                GameObject v = Instantiate(dirtyTileVisualizerPrefab, tilePos, Quaternion.identity, transform);
            }

            occupied.Add(key);
            spawned++;
        }

        if (spawned < count)
            Debug.LogWarning($"OneTimeSpawner: sólo pude spawnear {spawned}/{count} tiles sucias (intentos agotados).");
    }

    void SpawnNPCs(int count, bool asGood)
    {
        int spawned = 0;
        int attempts = 0;

        while (spawned < count && attempts < count * maxAttemptsPerItem)
        {
            attempts++;
            Vector3 randomPos = GridManager.Instance.GetRandomWalkablePosition();
            Vector3 tilePos = GridManager.Instance.GetNearestWalkableTile(randomPos);

            Vector2Int key = TileKey(tilePos);
            if (occupied.Contains(key)) continue;

            if (!GridManager.Instance.IsWalkable(tilePos))
            {
                occupied.Add(key); // evitar reintentos infinitos en una casilla no caminable
                continue;
            }

            GameObject go = Instantiate(npcPrefab, tilePos, Quaternion.identity, transform);

            NPCBase npc = go.GetComponent<NPCBase>();
            if (npc != null)
            {
                npc.isGoodNPC = asGood;
                // Asignar las listas de sprites
                npc.goodNPCSprites = goodNPCSprites;
                npc.badNPCSprites = badNPCSprites;

                // Forzar la asignación del sprite inmediatamente
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    List<Sprite> spriteList = asGood ? goodNPCSprites : badNPCSprites;
                    if (spriteList != null && spriteList.Count > 0)
                    {
                        int randomIndex = Random.Range(0, spriteList.Count);
                        sr.sprite = spriteList[randomIndex];
                    }
                }
                if (npcProgressBarPrefab != null)
                {
                    CleaningProgressBar barInstance = Instantiate(npcProgressBarPrefab, go.transform);
                    npc.progressBar = barInstance;
                }
            }
            else
            {
                Debug.LogWarning("OneTimeSpawner: npcPrefab no tiene NPCBase. Revisa el prefab.");
            }

            occupied.Add(key);
            spawned++;
        }

        if (spawned < count)
            Debug.LogWarning($"OneTimeSpawner: sólo pude spawnear {spawned}/{count} NPCs (asGood={asGood}).");
    }

    // Convierte una posición world a clave discreta para evitar colisiones por precisión
    Vector2Int TileKey(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.y);
        return new Vector2Int(x, y);
    }
}
