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
    private Coroutine speedBoostRoutine;
    private float normalAutoCleanTime;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isAutoCleaning = false;
    private float timeStationary = 0f;
    private Coroutine autoCleanCoroutine;
    public CleaningProgressBar progressBar;
    public int pointsPerTrash = 1;
    void Start()
    {
        // Colocar en posición inicial válida
        Vector3 startPos = GridManager.Instance.GetRandomWalkablePosition();
        transform.position = startPos;
        targetPosition = startPos;
        normalAutoCleanTime = autoCleanTime;
        if (cleaningIndicator)
            cleaningIndicator.SetActive(false);
    }

    void Update()
    {
        HandleInput();
        MoveToTarget();
        HandleAutoCleaning();
    }

    public void ApplySpeedBoost(float durationSeconds, float multiplier)
    {
        if (speedBoostRoutine != null) StopCoroutine(speedBoostRoutine);
        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(durationSeconds, multiplier));
    }

    private IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        autoCleanTime *= multiplier; // reduce tiempo (ej multiplier = 0.5 -> 50% tiempo)
                                     // opcional: feedback visual (cambiar color del player, etc)
        yield return new WaitForSeconds(duration);
        autoCleanTime = normalAutoCleanTime;
        speedBoostRoutine = null;
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

        // Mostrar la barra al iniciar
        if (progressBar != null)
            progressBar.ShowProgressBar();

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

        if (progressBar != null)
            progressBar.HideProgressBar();
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

            // actualizar barra de progreso
            float progress = cleanTimer / autoCleanTime;
            UpdateProgress(progress);

            yield return null;
        }

        // Intentar limpiar la basura en la posición actual
        bool cleaned = GridManager.Instance.CleanTrash(transform.position);

        if (cleaned)
        {
            // AÑADIR PUNTOS AL JUGADOR
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(pointsPerTrash);
                Debug.Log($"PlayerController: basura limpiada, +{pointsPerTrash} pts (Total: {ScoreManager.Instance.CurrentScore})");
            }
        }

        isAutoCleaning = false;

        if (cleaningIndicator)
            cleaningIndicator.SetActive(false);

        // ocultar la barra al terminar
        if (progressBar != null)
            progressBar.HideProgressBar();

        autoCleanCoroutine = null;

        yield return new WaitForSeconds(0.2f);
    }

    void UpdateProgress(float progress)
    {
        // Actualizar barra de progreso
        if (progressBar != null)
            progressBar.UpdateProgress(progress);
    }

}