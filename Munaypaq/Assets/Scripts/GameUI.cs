using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider trashProgressBar;
    public TextMeshProUGUI trashCountText;
    public TextMeshProUGUI cleanTilesText;
    public TextMeshProUGUI npcCountText;
    public TextMeshProUGUI alertText;

    [Header("Alert System")]
    public float alertBlinkSpeed = 1f;
    public AudioSource alertSound;

    [Header("Colors")]
    public Color safeColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;
    public Color criticalColor = Color.magenta;

    private bool isBlinking = false;
    private Coroutine blinkCoroutine;
    private float lastPercentage = 0f;

    void Update()
    {
        UpdateTrashUI();
        UpdateTileCount();
        UpdateNPCCount();
        UpdateAlertSystem();
    }

    void UpdateTrashUI()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.GetGameStats(out int count, out int max, out float percentage);

            if (trashProgressBar != null)
            {
                trashProgressBar.value = percentage / 100f;

                // Cambiar color según peligro
                Image fillImage = trashProgressBar.fillRect.GetComponent<Image>();
                Color barColor = GetColorByPercentage(percentage);
                fillImage.color = barColor;
            }

            if (trashCountText != null)
            {
                Color textColor = GetColorByPercentage(percentage);
                trashCountText.color = textColor;
                trashCountText.text = $"Basura: {count}/{max} ({percentage:F1}%)";
            }

            lastPercentage = percentage;
        }
    }

    void UpdateTileCount()
    {
        if (GridManager.Instance != null && cleanTilesText != null)
        {
            GridManager.Instance.GetGameStats(out int dirtyCount, out int maxTrash, out float percentage);

            int totalWalkableTiles = GridManager.Instance.GetEstimatedWalkableTiles();
            int cleanTiles = totalWalkableTiles - dirtyCount;

            Color textColor = GetColorByPercentage(percentage);
            cleanTilesText.color = textColor;
            cleanTilesText.text = $"Limpios: {cleanTiles} | Sucios: {dirtyCount}";
        }
    }

    void UpdateNPCCount()
    {
        if (npcCountText != null)
        {
            NPCBase[] npcs = FindObjectsOfType<NPCBase>();
            int goodNPCs = 0, badNPCs = 0;

            foreach (NPCBase npc in npcs)
            {
                if (npc.isGoodNPC) goodNPCs++;
                else badNPCs++;
            }

            Color npcColor = goodNPCs > badNPCs ? safeColor : (goodNPCs == badNPCs ? warningColor : dangerColor);
            npcCountText.color = npcColor;
            npcCountText.text = $"NPCs - Buenos: {goodNPCs} | Malos: {badNPCs}";
        }
    }

    void UpdateAlertSystem()
    {
        if (alertText == null) return;

        string alertMessage = "";
        Color alertColor = safeColor;
        bool shouldBlink = false;
        bool playSound = false;

        if (lastPercentage >= 90f)
        {
            alertMessage = "¡CRÍTICO! ¡Ciudad perdida en segundos!";
            alertColor = criticalColor;
            shouldBlink = true;
            playSound = true;
        }
        else if (lastPercentage >= 75f)
        {
            alertMessage = "¡PELIGRO EXTREMO! ¡Limpia rápido!";
            alertColor = dangerColor;
            shouldBlink = true;
            playSound = true;
        }
        else if (lastPercentage >= 60f)
        {
            alertMessage = "¡ADVERTENCIA! Demasiada basura";
            alertColor = warningColor;
            shouldBlink = false;
        }
        else if (lastPercentage >= 40f)
        {
            alertMessage = "Cuidado: Ciudad ensuciándose";
            alertColor = warningColor;
            shouldBlink = false;
        }
        else
        {
            alertMessage = "Ciudad en buen estado";
            alertColor = safeColor;
            shouldBlink = false;
        }

        alertText.text = alertMessage;

        if (shouldBlink && !isBlinking)
        {
            StartBlinking(alertColor);
        }
        else if (!shouldBlink && isBlinking)
        {
            StopBlinking();
            alertText.color = alertColor;
        }

        if (playSound && alertSound != null && !alertSound.isPlaying)
        {
            alertSound.Play();
        }
    }

    void StartBlinking(Color blinkColor)
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        isBlinking = true;
        blinkCoroutine = StartCoroutine(BlinkText(blinkColor));
    }

    void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        isBlinking = false;
    }

    IEnumerator BlinkText(Color blinkColor)
    {
        while (isBlinking)
        {
            alertText.color = blinkColor;
            yield return new WaitForSeconds(alertBlinkSpeed * 0.5f);

            alertText.color = Color.white;
            yield return new WaitForSeconds(alertBlinkSpeed * 0.5f);
        }
    }

    Color GetColorByPercentage(float percentage)
    {
        if (percentage >= 90f)
            return criticalColor;
        else if (percentage >= 75f)
            return dangerColor;
        else if (percentage >= 50f)
            return warningColor;
        else
            return safeColor;
    }

    public void ShowTemporaryMessage(string message, float duration = 3f)
    {
        StartCoroutine(DisplayTemporaryMessage(message, duration));
    }

    IEnumerator DisplayTemporaryMessage(string message, float duration)
    {
        string originalMessage = alertText.text;
        Color originalColor = alertText.color;

        alertText.text = message;
        alertText.color = Color.cyan;

        yield return new WaitForSeconds(duration);

        alertText.text = originalMessage;
        alertText.color = originalColor;
    }
}