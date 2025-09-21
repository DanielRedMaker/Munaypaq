
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
            progressBarInstance.transform.SetParent(transform);
            fillImage = progressBarInstance.GetComponentInChildren<Image>();
        }
        else
        {
            // Crear automáticamente si no hay prefab
            CreateDefaultProgressBar();
        }
    }

    void CreateDefaultProgressBar()
    {
        // Canvas World Space
        GameObject canvasGO = new GameObject("ProgressCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = offset;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 0.01f;

        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform);
        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(80, 8);
        bgRect.anchoredPosition = Vector2.zero;

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = Color.gray;

        // Fill Image
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgGO.transform);
        RectTransform fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0, 0);

        fillImage = fillGO.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;

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
            fillImage.fillAmount = progress;
        }
    }

    void LateUpdate()
    {
        // Mantener posición relativa
        if (isActive && progressBarInstance != null)
        {
            Vector3 worldPos = transform.position + offset;
            progressBarInstance.transform.position = worldPos;

            // Opcional: Hacer que mire a la cámara
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                progressBarInstance.transform.LookAt(progressBarInstance.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                                   mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}
