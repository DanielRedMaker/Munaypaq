using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerupGround : MonoBehaviour
{
    public PowerupType powerupType;
    public Sprite iconSprite; // icono para el inventario (pequeño)

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"PowerupGround: Player recogió powerup {powerupType} en {transform.position}");

        if (InventoryUI.Instance == null)
        {
            Debug.LogError("PowerupGround: InventoryUI.Instance es null. Asegúrate de que exista un objeto InventoryUI en la escena y tenga el script InventoryUI.");
            return;
        }

        bool added = InventoryUI.Instance.AddPowerup(powerupType, iconSprite);
        if (!added)
        {
            Debug.Log("PowerupGround: No se pudo añadir al inventario (posiblemente lleno en tipos distintos).");
            // Si no se pudo añadir, podemos dejarlo en el suelo o destruirlo; por ahora lo dejamos.
            return;
        }

        Destroy(gameObject);
    }
}
