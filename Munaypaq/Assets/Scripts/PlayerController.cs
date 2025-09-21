using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Auto Cleaning")]
    public float autoCleanTime = 1.5f;
    public GameObject cleaningIndicator; // UI o sprite que muestra que está limpiando

    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isAutoCleaning = false;
    private float timeStationary = 0f;
    private Coroutine autoCleanCoroutine;
    public CleaningProgressBar progressBar;
    void Start()
    {
        // Colocar en posición inicial válida
        Vector3 startPos = GridManager.Instance.GetRandomWalkablePosition();
        transform.position = startPos;
        targetPosition = startPos;

        if (cleaningIndicator)
            cleaningIndicator.SetActive(false);
    }

    void Update()
    {
        HandleInput();
        MoveToTarget();
        HandleAutoCleaning();
    }

    void HandleInput()
    {
        if (isMoving) return;

        Vector3 inputDirection = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            inputDirection = Vector3.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            inputDirection = Vector3.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            inputDirection = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            inputDirection = Vector3.right;

        if (inputDirection != Vector3.zero)
        {
            Vector3 newPosition = transform.position + inputDirection;
            Vector3 validPosition = GridManager.Instance.GetNearestWalkableTile(newPosition);

            if (GridManager.Instance.IsWalkable(validPosition))
            {
                targetPosition = validPosition;
                isMoving = true;

                // Reset auto-cleaning cuando se mueve
                StopAutoCleaning();
            }
        }
    }

    void MoveToTarget()
    {
        if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
            if (isMoving)
            {
                isMoving = false;
                timeStationary = 0f; // Reset timer cuando llega a destino
            }
        }
    }

    void HandleAutoCleaning()
    {
        if (isMoving)
        {
            StopAutoCleaning();
            return;
        }

        // Solo auto-limpiar si hay basura en la posición actual
        if (!GridManager.Instance.HasTrashAt(transform.position))
        {
            StopAutoCleaning();
            return;
        }

        // Incrementar tiempo estacionario
        timeStationary += Time.deltaTime;

        // Iniciar auto-limpieza si no está ya limpiando
        if (!isAutoCleaning && timeStationary >= 0.2f) // Small delay before starting
        {
            StartAutoCleaning();
        }
    }

    void StartAutoCleaning()
    {
        if (autoCleanCoroutine != null)
            StopCoroutine(autoCleanCoroutine);

        autoCleanCoroutine = StartCoroutine(AutoCleanProcess());
    }

    void StopAutoCleaning()
    {
        if (autoCleanCoroutine != null)
        {
            StopCoroutine(autoCleanCoroutine);
            autoCleanCoroutine = null;
        }

        isAutoCleaning = false;
        timeStationary = 0f;

        if (cleaningIndicator)
            cleaningIndicator.SetActive(false);
    }

    IEnumerator AutoCleanProcess()
    {
        isAutoCleaning = true;
        if (cleaningIndicator) cleaningIndicator.SetActive(true);

        float cleanTimer = 0f;

        while (cleanTimer < autoCleanTime)
        {
            // Si se mueve o ya no hay basura, cancelar
            if (isMoving || !GridManager.Instance.HasTrashAt(transform.position))
            {
                StopAutoCleaning();
                yield break;
            }

            cleanTimer += Time.deltaTime;

            // Opcional: actualizar barra de progreso
            float progress = cleanTimer / autoCleanTime;
            UpdateProgress(progress);

            yield return null;
        }

        // Limpieza completada
        GridManager.Instance.CleanTrash(transform.position);
        isAutoCleaning = false;

        if (cleaningIndicator)
            cleaningIndicator.SetActive(false);

        autoCleanCoroutine = null;

        // Pequeña pausa antes de poder limpiar otra basura
        yield return new WaitForSeconds(0.2f);
    }

    void UpdateProgress(float progress)
    {
        // Actualizar barra de progreso
        if (progressBar != null)
            progressBar.UpdateProgress(progress);
    }

}