using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class NPCBase : MonoBehaviour
{
    [Header("NPC Type")]
    public bool isGoodNPC = true;
    public Material goodMaterial;
    public Material badMaterial;

    [Header("Movement Settings")]
    public float moveInterval = 3f;
    public float moveSpeed = 2f;

    [Header("Action Settings")]
    public float cleaningTime = 2f;
    public float trashCreationInterval = 4f;

    [Header("Conversion Settings")]
    public float corruptionCheckInterval = 5f;
    public float baseCorruptionChance = 0.1f;
    public float trashInfluenceRadius = 2f;
    public float maxCorruptionChance = 0.8f;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isPerformingAction = false;
    private SpriteRenderer spriteRenderer;

    // Variables para conversión
    private float timeInDirtyArea = 0f;
    private float timeSinceLastCorruptionCheck = 0f;
    public CleaningProgressBar progressBar;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisualAppearance();

        // Posición inicial
        Vector3 startPos = GridManager.Instance.GetRandomWalkablePosition();
        transform.position = startPos;
        targetPosition = startPos;

        StartCoroutine(NPCBehavior());
        StartCoroutine(CorruptionSystem());
    }

    void Update()
    {
        MoveToTarget();
        UpdateCorruptionTimer();
    }

    void MoveToTarget()
    {
        if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    IEnumerator NPCBehavior()
    {
        while (true)
        {
            // Esperar antes de moverse
            yield return new WaitForSeconds(moveInterval + Random.Range(-1f, 1f));

            if (!isPerformingAction)
            {
                MoveRandomly();
            }

            // NPCs buenos limpian, malos crean basura
            if (isGoodNPC)
            {
                yield return StartCoroutine(TryCleanTrash());
            }
            else
            {
                yield return new WaitForSeconds(trashCreationInterval + Random.Range(-1f, 1f));
                CreateTrash();
            }
        }
    }

    IEnumerator CorruptionSystem()
    {
        while (true)
        {
            yield return new WaitForSeconds(corruptionCheckInterval);

            if (isGoodNPC)
            {
                CheckForCorruption();
            }
        }
    }

    void MoveRandomly()
    {
        if (isMoving || isPerformingAction) return;

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        Vector3 randomDirection = directions[Random.Range(0, directions.Length)];

        Vector3 newPosition = transform.position + randomDirection;
        Vector3 validPosition = GridManager.Instance.GetNearestWalkableTile(newPosition);

        if (GridManager.Instance.IsWalkable(validPosition))
        {
            targetPosition = validPosition;
        }
    }
    IEnumerator TryCleanTrash()
    {
        if (GridManager.Instance.HasTrashAt(transform.position))
        {
            isPerformingAction = true;

            // Mostrar barra de progreso
            if (progressBar != null)
                progressBar.ShowProgressBar();

            float cleanTimer = 0f;

            // Proceso de limpieza con progreso visual
            while (cleanTimer < cleaningTime)
            {
                cleanTimer += Time.deltaTime;
                float progress = cleanTimer / cleaningTime;

                if (progressBar != null)
                    progressBar.UpdateProgress(progress);

                yield return null;
            }

            // Limpiar la basura
            GridManager.Instance.CleanTrash(transform.position);

            // Ocultar barra de progreso
            if (progressBar != null)
                progressBar.HideProgressBar();

            isPerformingAction = false;
        }
    }
    void CreateTrash()
    {
        if (!isPerformingAction)
        {
            GridManager.Instance.CreateTrash(transform.position);
        }
    }

    void UpdateCorruptionTimer()
    {
        if (!isGoodNPC) return;

        // Verificar si está en área sucia
        bool inDirtyArea = IsInDirtyArea();

        if (inDirtyArea)
        {
            timeInDirtyArea += Time.deltaTime;
        }
        else
        {
            timeInDirtyArea = Mathf.Max(0f, timeInDirtyArea - Time.deltaTime * 0.5f); // Recuperación lenta
        }

        timeSinceLastCorruptionCheck += Time.deltaTime;
    }

    bool IsInDirtyArea()
    {
        // Verificar si hay basura en el tile actual
        if (GridManager.Instance.HasTrashAt(transform.position))
            return true;

        // Verificar área alrededor
        int trashCount = GridManager.Instance.GetTrashCountInRadius(transform.position, trashInfluenceRadius);
        return trashCount > 2; // Si hay más de 2 basuras cerca
    }

    void CheckForCorruption()
    {
        // Calcular probabilidad de corrupción
        float corruptionChance = baseCorruptionChance;

        // Aumentar probabilidad por tiempo en área sucia
        float timeInfluence = timeInDirtyArea / 10f; // Cada 10 segundos aumenta la probabilidad
        corruptionChance += timeInfluence * 0.2f;

        // Aumentar por basura cercana
        int nearbyTrash = GridManager.Instance.GetTrashCountInRadius(transform.position, trashInfluenceRadius);
        float trashInfluence = nearbyTrash * 0.1f;
        corruptionChance += trashInfluence;

        // Limitar probabilidad máxima
        corruptionChance = Mathf.Min(corruptionChance, maxCorruptionChance);

        // Roll de corrupción
        if (Random.Range(0f, 1f) < corruptionChance)
        {
            BecomeEvil();
        }
    }

    void BecomeEvil()
    {
        Debug.Log("¡Un NPC bueno se ha corrompido!");
        isGoodNPC = false;
        timeInDirtyArea = 0f;
        UpdateVisualAppearance();

        // Crear basura inmediatamente como acto de corrupción
        GridManager.Instance.CreateTrash(transform.position);
    }

    void UpdateVisualAppearance()
    {
        if (spriteRenderer != null)
        {
            if (isGoodNPC && goodMaterial != null)
            {
                spriteRenderer.material = goodMaterial;
            }
            else if (!isGoodNPC && badMaterial != null)
            {
                spriteRenderer.material = badMaterial;
            }

            // También puedes cambiar el color
            spriteRenderer.color = isGoodNPC ? Color.green : Color.red;
        }
    }

    // Método público para forzar conversión (para debugging)
    [ContextMenu("Force Corruption")]
    public void ForceCorruption()
    {
        if (isGoodNPC)
        {
            BecomeEvil();
        }
    }
}