using UnityEngine;
using System.Collections;

public abstract class NPCBase : MonoBehaviour
{
    [Header("NPC Type")]
    public bool isGoodNPC = true;

    [Header("Settings")]
    public float moveInterval = 3f;
    public float actionInterval = 4f;
    public float moveSpeed = 2f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        // Posición inicial
        Vector3 startPos = GridManager.Instance.GetRandomWalkablePosition();
        transform.position = startPos;
        targetPosition = startPos;

        StartCoroutine(NPCBehavior());
    }

    void Update()
    {
        // Movimiento suave
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

            // Moverse aleatoriamente
            MoveRandomly();

            // Esperar y realizar acción
            yield return new WaitForSeconds(actionInterval + Random.Range(-1f, 1f));
            PerformAction();
        }
    }

    void MoveRandomly()
    {
        if (isMoving) return;

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        Vector3 randomDirection = directions[Random.Range(0, directions.Length)];

        Vector3 newPosition = transform.position + randomDirection;
        Vector3 validPosition = GridManager.Instance.GetNearestWalkableTile(newPosition);

        if (GridManager.Instance.IsWalkable(validPosition))
        {
            targetPosition = validPosition;
        }
    }

    void PerformAction()
    {
        if (isGoodNPC)
        {
            // Persona buena: limpiar basura
            GridManager.Instance.CleanTrash(transform.position);
        }
        else
        {
            // Persona mala: crear basura
            GridManager.Instance.CreateTrash(transform.position);
        }
    }
}