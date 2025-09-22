
using UnityEngine;
using UnityEngine.UI;

public class CleaningProgressBar : MonoBehaviour
{
    [Header("Progress Bar Setup")]
    public GameObject progressBarPrefab; // Prefab con Image como Fill
    public Vector3 offset = Vector3.down * 0.5f;

    private GameObject progressBarInstance;
    private Image fillImage;
    private bool isActive = false;

    void Start()
    {
        CreateProgressBar();
        HideProgressBar();
    }


    void CreateProgressBar()
    {
        if (progressBarPrefab != null)
        {
            progressBarInstance = Instantiate(progressBarPrefab, transform.position + offset, Quaternion.identity);
            progressBarInstance.transform.SetParent(transform, worldPositionStays: true);
            // Buscar la Image marcada como fill (primera que tenga Image y tipo Filled)
            fillImage = progressBarInstance.GetComponentInChildren<Image>();
            if (fillImage != null && fillImage.type != Image.Type.Filled)
                fillImage.type = Image.Type.Filled; // forzar tipo Filled en caso de error
        }
        else
        {
            CreateDefaultProgressBar();
        }
    }
    void CreateDefaultProgressBar()
    {
        // Canvas en World Space como hijo del objeto
        GameObject canvasGO = new GameObject("ProgressCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = offset;
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.01f; // escala pequeña para que no sea gigante en worldspace (ajusta si lo prefieres)

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        // Fondo
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(80, 10);
        bgRect.anchoredPosition = Vector2.zero;

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = Color.gray;

        // Fill (hijo del background), anclas completas para que ocupe el rect inicial
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        RectTransform fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fillImage = fillGO.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 0f;

        progressBarInstance = canvasGO;
    }
    public void ShowProgressBar()
    {
        if (progressBarInstance != null)
        {
            progressBarInstance.SetActive(true);
            isActive = true;
            if (fillImage != null)
                fillImage.fillAmount = 0f;
        }
    }

    public void HideProgressBar()
    {
        if (progressBarInstance != null)
        {
            progressBarInstance.SetActive(false);
            isActive = false;
        }
    }
    public void UpdateProgress(float progress)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(progress);
            // Si la barra no está activa, forzamos activarla para evitar casos donde UpdateProgress se llame sin Show
            if (!isActive && progress > 0f)
            {
                ShowProgressBar();
            }
        }
    }

    void LateUpdate()
    {
        if (progressBarInstance == null) return;

        // Mantener posicion relativa aunque esté inactivo (por si se activa luego)
        Vector3 worldPos = transform.position + offset;
        progressBarInstance.transform.position = worldPos;

        // Mirar a cámara (opcional)
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            progressBarInstance.transform.rotation = Quaternion.LookRotation(progressBarInstance.transform.position - mainCamera.transform.position);
        }
    }
}
