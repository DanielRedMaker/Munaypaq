using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI refs")]
    public Image iconImage;
    public TextMeshProUGUI countText;

    [HideInInspector] public PowerupType powerupType;
    [HideInInspector] public int count = 0;

    // Drag visuals
    private GameObject dragIcon;
    private RectTransform dragIconRect;
    private Canvas canvas;

    public void Initialize(PowerupType type, Sprite icon, int startCount, Canvas parentCanvas)
    {
        powerupType = type;
        count = startCount;
        canvas = parentCanvas;

        if (iconImage != null) iconImage.sprite = icon;
        UpdateCountText();
    }

    public void IncrementCount(int amount)
    {
        count += amount;
        UpdateCountText();
    }

    public void DecrementCount(int amount)
    {
        count -= amount;
        if (count < 0) count = 0;
        UpdateCountText();
    }

    void UpdateCountText()
    {
        if (countText != null)
            countText.text = count.ToString();
    }

    // ---- Drag handlers ----
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (iconImage == null || iconImage.sprite == null) return;

        // Crear icono que sigue cursor
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIconRect = dragIcon.AddComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(48, 48);

        Image img = dragIcon.AddComponent<Image>();
        img.raycastTarget = false;
        img.sprite = iconImage.sprite;
        img.preserveAspect = true;

        CanvasGroup cg = dragIcon.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIconRect == null || canvas == null) return;
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out pos);
        dragIconRect.localPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Llamar a inventory handler
        InventoryUI.Instance?.HandleDrop(this, eventData.position);

        if (dragIcon != null) Destroy(dragIcon);
    }
}
