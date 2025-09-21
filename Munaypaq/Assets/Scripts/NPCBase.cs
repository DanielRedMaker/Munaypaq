using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NPCBase : MonoBehaviour
{
    [Header("NPC Type")]
    public bool isGoodNPC = true;

    [Header("Movement Settings")]
    public float moveInterval = 3f;
    public float moveSpeed = 2f;

    [Header("Action Settings")]
    public float cleaningTime = 2f;
    public float trashCreationInterval = 4f;

    [Header("Conversion Settings")]
    public float corruptionCheckInterval = 5f;
    [Header("Powerup Drop")]
    [Range(0f, 1f)] public float powerupDropChance = 0.5f; // 25% de soltar algo
    public List<PowerupType> possiblePowerups; // asignar en inspector (ej: TrashBin,Announcement,SpeedBoost)

    // Probabilidades directas pedidas por el usuario:
    [Range(0f, 1f)] public float badToGoodChance = 0.5f;   // 50% si el área está limpia
    [Range(0f, 1f)] public float goodToBadChance = 0.6f;   // 70% si el área está sucia

    public float trashInfluenceRadius = 2f;
    public float maxCorruptionChance = 0.8f; // lo dejamos pero no es usado para la conversión simple

    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isPerformingAction = false;
    private SpriteRenderer spriteRenderer;

    // Barra de progreso (opcional)
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
        StartCoroutine(ConversionCheckRoutine());
    }

    void Update()
    {
        MoveToTarget();
        // si quieres tiempo acumulado para otra mecánica, lo puedes mantener aquí
    }
    void TryDropPowerupAfterClean()
    {
        if (possiblePowerups == null || possiblePowerups.Count == 0) return;
        if (Random.value > powerupDropChance) return;

        // Elegir aleatorio
        PowerupType chosen = possiblePowerups[Random.Range(0, possiblePowerups.Count)];

        // Pedimos al GridManager que instancie un prefab de powerup en esta posición
        GridManagerPowerupSpawner.Instance?.SpawnPowerupAt(transform.position, chosen);
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

    // Rutina que chequea conversiones (tanto corrupción como rehabilitación)
    IEnumerator ConversionCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(corruptionCheckInterval);

            CheckForConversion();
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

            bool cleaned = GridManager.Instance.CleanTrash(transform.position);
            TryDropPowerupAfterClean();

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

    // --- Conversion logic (nuevo) ---
    void CheckForConversion()
    {
        if (isGoodNPC)
        {
            // Si el NPC bueno está en un área sucia -> puede corromperse con probabilidad goodToBadChance (70%)
            if (IsInDirtyArea())
            {
                float roll = Random.value; // 0..1
                if (roll < goodToBadChance)
                {
                    BecomeEvil();
                }
                // si no pasa el roll, permanece bueno por ahora
            }
        }
        else
        {
            // Si el NPC malo está en un área limpia -> puede volverse bueno con probabilidad badToGoodChance (50%)
            if (IsInCleanArea())
            {
                float roll = Random.value;
                if (roll < badToGoodChance)
                {
                    BecomeGood();
                }
            }
        }
    }

    bool IsInDirtyArea()
    {
        // Verificar si hay basura en el tile actual
        if (GridManager.Instance.HasTrashAt(transform.position))
            return true;

        // Verificar área alrededor
        int trashCount = GridManager.Instance.GetTrashCountInRadius(transform.position, trashInfluenceRadius);
        return trashCount > 2; // tu lógica original: si hay más de 2 basuras cerca -> sucio
    }

    bool IsInCleanArea()
    {
        // Consideramos área limpia si no hay basura en el radio (y tampoco en la tile actual)
        if (GridManager.Instance.HasTrashAt(transform.position))
            return false;

        int trashCount = GridManager.Instance.GetTrashCountInRadius(transform.position, trashInfluenceRadius);
        return trashCount == 0;
    }

    void BecomeEvil()
    {
        Debug.Log("¡Un NPC bueno se ha corrompido!");
        isGoodNPC = false;
        UpdateVisualAppearance();

        // Crear basura inmediatamente como acto de corrupción (mantengo tu comportamiento)
        GridManager.Instance.CreateTrash(transform.position);
    }

    public void BecomeGood()
    {
        Debug.Log("Un NPC malo se ha reformado y ahora es bueno.");
        isGoodNPC = true;
        UpdateVisualAppearance();

        // (Opcional) puedes limpiar la casilla al convertirse en bueno:
        // GridManager.Instance.CleanTrash(transform.position);
    }

    void UpdateVisualAppearance()
    {
      // También puedes cambiar el color
       spriteRenderer.color = isGoodNPC ? Color.blue : Color.red;
      
    }

    // Método público para forzar conversión (para debugging)
    [ContextMenu("Force Corruption")]
    public void ForceCorruption()
    {
        if (isGoodNPC)
        {
            BecomeEvil();
        }
        else
        {
            BecomeGood();
        }
    }
}
