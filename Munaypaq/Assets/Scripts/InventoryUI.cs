using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("UI")]
    public RectTransform slotsContainer; // padre donde se instancian slots (Content)
    public GameObject slotPrefab;        // prefab UI con InventorySlot component
    public Canvas canvas;                // canvas principal (necesario para drag)

    [Header("Settings")]
    public int maxDistinctSlots = 3;     // cuantos tipos distintos puedes tener simultáneamente

    // Diccionario: PowerupType -> InventorySlot (el slot que contiene la pila)
    private Dictionary<PowerupType, InventorySlot> slotsByType = new Dictionary<PowerupType, InventorySlot>();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                Debug.LogWarning("InventoryUI: No Canvas asignado ni encontrado en padres. Asigna el Canvas en el inspector.");
        }
    }

    /// <summary>
    /// Añade un powerup al inventario. Si ya existe el tipo, incrementa su contador.
    /// Retorna true si se pudo añadir, false si no (por límite de tipos distintos).
    /// </summary>
    public bool AddPowerup(PowerupType type, Sprite icon)
    {
        if (slotsByType.ContainsKey(type))
        {
            // incrementar contador
            slotsByType[type].IncrementCount(1);
            return true;
        }

        // si no existe y estamos al límite de tipos distintos, denegar
        if (slotsByType.Count >= maxDistinctSlots)
        {
            Debug.Log("InventoryUI: No hay espacio para más tipos distintos de powerups.");
            return false;
        }

        // crear nuevo slot
        if (slotPrefab == null || slotsContainer == null)
        {
            Debug.LogError("InventoryUI: slotPrefab o slotsContainer no asignado en inspector.");
            return false;
        }

        GameObject go = Instantiate(slotPrefab, slotsContainer);
        go.transform.localScale = Vector3.one;
        InventorySlot slot = go.GetComponent<InventorySlot>();
        if (slot == null)
        {
            Debug.LogError("InventoryUI: slotPrefab no contiene InventorySlot componente.");
            Destroy(go);
            return false;
        }

        slot.Initialize(type, icon, 1, canvas);
        slotsByType[type] = slot;
        return true;
    }

    /// <summary>
    /// Llamado por un InventorySlot cuando se suelta. screenPos es la posición de pantalla del drop.
    /// Aplica el efecto correspondiente y consume 1 unidad.
    /// </summary>
    public void HandleDrop(InventorySlot slot, Vector2 screenPos)
    {
        if (slot == null) return;

        // Convertir pantalla -> world
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

        // Aplicar efecto
        bool applied = ApplyPowerupEffect(slot.powerupType, worldPos);

        if (!applied)
        {
            Debug.Log("InventoryUI: No se pudo aplicar el powerup (no se cumplió la condición).");
            return;
        }

        // Consumir 1 unidad del slot
        slot.DecrementCount(1);

        // Si llegó a 0 borra el slot y del diccionario
        if (slot.count <= 0)
        {
            slotsByType.Remove(slot.powerupType);
            Destroy(slot.gameObject);
        }
    }

    bool ApplyPowerupEffect(PowerupType type, Vector3 worldPos)
    {
        switch (type)
        {
            case PowerupType.TrashBin:
                GridManager.Instance?.CleanArea(worldPos, 2, 2);
                return true;

            case PowerupType.Announcement:
                // Buscar NPC cercano y convertir
                Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.7f);
                if (hit != null)
                {
                    NPCBase npc = hit.GetComponent<NPCBase>();
                    if (npc != null && !npc.isGoodNPC)
                    {
                        npc.BecomeGood(); // public
                        return true;
                    }
                }
                // si no hay NPC, no aplicar
                return false;

            case PowerupType.SpeedBoost:
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    PlayerController pc = playerGO.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        // valores por defecto: 8s, multiplier 0.5 (50% del tiempo)
                        pc.ApplySpeedBoost(8f, 0.8f);
                        return true;
                    }
                }
                return false;

            default:
                return false;
        }
    }
}
