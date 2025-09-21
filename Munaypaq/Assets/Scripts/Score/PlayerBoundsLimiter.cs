using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Limita la posición del Player dentro de los límites del Tilemap (o rect manual).
/// Úsalo si quieres que el Player NO pueda moverse fuera del mapa.
/// </summary>
public class PlayerBoundsLimiter : MonoBehaviour
{
    public bool useTilemapBounds = true;
    public string tilemapTag = "Walkable";
    public Vector2 manualMin = Vector2.zero;
    public Vector2 manualMax = Vector2.zero;
    public Vector2 padding = Vector2.zero; // opcional padding interno

    private float minX, maxX, minY, maxY;
    private bool hasLimits = false;

    void Start()
    {
        CalculateBounds();
    }

    void LateUpdate()
    {
        if (!hasLimits) return;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX + padding.x, maxX - padding.x);
        p.y = Mathf.Clamp(p.y, minY + padding.y, maxY - padding.y);
        transform.position = p;
    }

    void CalculateBounds()
    {
        hasLimits = false;
        if (useTilemapBounds)
        {
            GameObject tileObj = GameObject.FindWithTag(tilemapTag);
            if (tileObj != null)
            {
                Tilemap tilemap = tileObj.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    var cb = tilemap.cellBounds;
                    Vector3 minWorld = tilemap.GetCellCenterWorld(cb.min);
                    Vector3 maxWorld = tilemap.GetCellCenterWorld(cb.max - Vector3Int.one);

                    minX = Mathf.Min(minWorld.x, maxWorld.x);
                    maxX = Mathf.Max(minWorld.x, maxWorld.x);
                    minY = Mathf.Min(minWorld.y, maxWorld.y);
                    maxY = Mathf.Max(minWorld.y, maxWorld.y);

                    hasLimits = true;
                    return;
                }
            }
            Debug.LogWarning($"PlayerBoundsLimiter: no se encontró Tilemap con tag '{tilemapTag}' — usando límites manuales (si están).");
        }

        if (manualMax.x > manualMin.x && manualMax.y > manualMin.y)
        {
            minX = manualMin.x;
            minY = manualMin.y;
            maxX = manualMax.x;
            maxY = manualMax.y;
            hasLimits = true;
        }
        else
        {
            hasLimits = false;
        }
    }
}
