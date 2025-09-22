using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Hace que la cámara siga suavemente al jugador y se mantenga dentro de los límites del Tilemap
/// (busca el Tilemap con tag "Walkable") o de un rectángulo definido manualmente.
/// Coloca este script en la Main Camera (asegúrate que es Orthographic).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;                     // arrastra el Player aquí o deja vacío para buscar por tag "Player"
    public string playerTag = "Player";

    [Header("Follow settings")]
    public float smoothTime = 0.12f;             // menor = cámara más rígida
    public Vector2 followOffset = Vector2.zero;  // offset opcional respecto al player

    [Header("Bounds")]
    public bool useTilemapBounds = true;         // si true toma bounds desde Tilemap con tag "Walkable"
    public string tilemapTag = "Walkable";
    public Vector2 manualMin = Vector2.zero;     // si useTilemapBounds = false puedes definir límites manualmente
    public Vector2 manualMax = Vector2.zero;
    public Vector2 boundsPadding = new Vector2(0.5f, 0.5f); // margen interno

    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    // Calculated limits
    private float minX, maxX, minY, maxY;
    private bool hasLimits = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }
    }

    void Start()
    {
        CalculateBounds();
        // En caso de que la cámara deba comenzar dentro del límite
        SnapCameraIntoBounds();
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPos = player.position + (Vector3)followOffset;
        targetPos.z = transform.position.z; // conservar z de la cámara

        // Smooth follow
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);

        // Clamp dentro de límites calculados
        if (hasLimits)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float clampedX = Mathf.Clamp(smoothed.x, minX + halfW + boundsPadding.x, maxX - halfW - boundsPadding.x);
            float clampedY = Mathf.Clamp(smoothed.y, minY + halfH + boundsPadding.y, maxY - halfH - boundsPadding.y);

            // Si el mapa es más pequeño que la vista de la cámara, centramos
            if (minX + halfW > maxX - halfW) clampedX = (minX + maxX) * 0.5f;
            if (minY + halfH > maxY - halfH) clampedY = (minY + maxY) * 0.5f;

            transform.position = new Vector3(clampedX, clampedY, smoothed.z);
        }
        else
        {
            transform.position = smoothed;
        }
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
                    // cellBounds devuelve un rect en coordenadas de celda (min inclusive, max exclusive).
                    var cb = tilemap.cellBounds;
                    // Convertir las esquinas a world
                    Vector3 minWorld = tilemap.GetCellCenterWorld(cb.min);
                    Vector3 maxWorld = tilemap.GetCellCenterWorld(cb.max - Vector3Int.one);

                    // Ajuste por tilemap cell size si fuera necesario
                    minX = Mathf.Min(minWorld.x, maxWorld.x);
                    maxX = Mathf.Max(minWorld.x, maxWorld.x);
                    minY = Mathf.Min(minWorld.y, maxWorld.y);
                    maxY = Mathf.Max(minWorld.y, maxWorld.y);

                    hasLimits = true;
                    return;
                }
            }
            Debug.LogWarning($"CameraFollow: no se encontró Tilemap con tag '{tilemapTag}' — usando límites manuales (si están).");
        }

        // Si llegamos aquí, usar manual bounds si son válidos
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

    void SnapCameraIntoBounds()
    {
        if (!hasLimits) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float x = Mathf.Clamp(transform.position.x, minX + halfW + boundsPadding.x, maxX - halfW - boundsPadding.x);
        float y = Mathf.Clamp(transform.position.y, minY + halfH + boundsPadding.y, maxY - halfH - boundsPadding.y);

        if (minX + halfW > maxX - halfW) x = (minX + maxX) * 0.5f;
        if (minY + halfH > maxY - halfH) y = (minY + maxY) * 0.5f;

        transform.position = new Vector3(x, y, transform.position.z);
    }

    // Método público por si quieres recalcular límites en runtime (p.ej. tras cambiar mapa)
    public void RecalculateBounds()
    {
        CalculateBounds();
        SnapCameraIntoBounds();
    }

#if UNITY_EDITOR
    // para visualizar límites en editor
    void OnDrawGizmosSelected()
    {
        if (!hasLimits)
        {
            // intentar calcular para dibujar (solo editor)
            if (useTilemapBounds)
            {
                GameObject tileObj = GameObject.FindWithTag(tilemapTag);
                if (tileObj != null)
                {
                    var tt = tileObj.GetComponent<Tilemap>();
                    if (tt != null)
                    {
                        var cb = tt.cellBounds;
                        Vector3 minWorld = tt.GetCellCenterWorld(cb.min);
                        Vector3 maxWorld = tt.GetCellCenterWorld(cb.max - Vector3Int.one);
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube((minWorld + maxWorld) / 2f, maxWorld - minWorld);
                    }
                }
            }
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f),
                new Vector3(maxX - minX, maxY - minY, 0f));
        }
    }
#endif
}
