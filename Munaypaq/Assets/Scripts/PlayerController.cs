using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        // Colocar en posición inicial válida
        Vector3 startPos = GridManager.Instance.GetRandomWalkablePosition();
        transform.position = startPos;
        targetPosition = startPos;
    }

    void Update()
    {
        HandleInput();
        MoveToTarget();
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
                // Limpiar basura al llegar
                GridManager.Instance.CleanTrash(transform.position);
            }
        }
    }
}