using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class HighScoresMenu : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentParent;     // Content del ScrollView donde se instanciarán las filas
    public GameObject rowPrefab;        // Prefab simple: contiene 1 Text o 1 TextMeshProUGUI en sus hijos
    public TextMeshProUGUI titleText;   // título usando TMP opcional

    [Header("Display")]
    public int maxToShow = 10;
    void Start()
    {
        // Delay corto por si ScoreManager recién se inicializa
        StartCoroutine(DelayedPopulate());
    }

    private System.Collections.IEnumerator DelayedPopulate()
    {
        // esperar un frame para asegurar que ScoreManager esté listo
        yield return null;

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("HighScoresMenu: ScoreManager no encontrado al poblar.");
        }
        Populate();
    }

    public void Populate()
    {
        if (contentParent == null)
        {
            Debug.LogWarning("HighScoresMenu: contentParent no asignado.");
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogWarning("HighScoresMenu: rowPrefab no asignado.");
            return;
        }

        // Limpiar contenido anterior
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        var list = ScoreManager.Instance.LoadHighScoreList();
        Debug.Log($"HighScoresMenu: entries cargadas = {list.entries.Count}");
        int count = Mathf.Min(list.entries.Count, maxToShow);

        for (int i = 0; i < count; i++)
        {
            var e = list.entries[i];
            GameObject row = Instantiate(rowPrefab, contentParent);

            // Asegurarnos de que el rect transform quede bien (importante para UI)
            RectTransform rowRect = row.GetComponent<RectTransform>();
            if (rowRect != null)
            {
                rowRect.localScale = Vector3.one;
                rowRect.anchoredPosition = Vector2.zero;
                rowRect.localRotation = Quaternion.identity;
            }
            else
            {
                row.transform.localScale = Vector3.one;
                row.transform.localPosition = Vector3.zero;
            }

            // Activar por si estaba inactivo
            row.SetActive(true);

            // Intentar TMP primero
            TextMeshProUGUI tmp = row.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                string duration = FormatDuration(e.durationSeconds);
                tmp.text = $"{i + 1}. {e.playerName}  |  {e.score} pts  |  suciedad:{e.cityDirtLevel}  |  {duration}  |  {e.date}";
                Debug.Log($"HighScoresMenu: fila {i + 1} creada (TMP) -> {tmp.text}");
                continue;
            }

            // Fallback a Unity UI Text
            Text uiText = row.GetComponentInChildren<Text>();
            if (uiText != null)
            {
                string duration = FormatDuration(e.durationSeconds);
                uiText.text = $"{i + 1}. {e.playerName}  |  {e.score} pts  |  suciedad:{e.cityDirtLevel}  |  {duration}  |  {e.date}";
                Debug.Log($"HighScoresMenu: fila {i + 1} creada (UI.Text) -> {uiText.text}");
                continue;
            }

            // Si no encuentra ninguno, buscar en profundidad (por si tu Text está anidado raro)
            var tmps = row.GetComponentsInChildren<TextMeshProUGUI>(true);
            var uis = row.GetComponentsInChildren<Text>(true);
            if (tmps.Length > 0)
            {
                tmps[0].text = $"{i + 1}. {e.playerName}  |  {e.score} pts  |  suciedad:{e.cityDirtLevel}  |  {FormatDuration(e.durationSeconds)}  |  {e.date}";
                Debug.Log($"HighScoresMenu: fila {i + 1} creada (TMP found deeper)");
                continue;
            }
            if (uis.Length > 0)
            {
                uis[0].text = $"{i + 1}. {e.playerName}  |  {e.score} pts  |  suciedad:{e.cityDirtLevel}  |  {FormatDuration(e.durationSeconds)}  |  {e.date}";
                Debug.Log($"HighScoresMenu: fila {i + 1} creada (UI.Text found deeper)");
                continue;
            }

            Debug.LogWarning("HighScoresMenu: rowPrefab no contiene TextMeshProUGUI ni Text (ni en hijos). Añade uno en el prefab o cambia rowPrefab.");
        }

        if (titleText != null)
            titleText.text = $"Mejores partidas ({list.entries.Count})";
    }

    public void OnMainMenuPressed()
    {
        SceneManager.LoadScene("MainMenu");
    }

    string FormatDuration(int seconds)
    {
        if (seconds <= 0) return "0s";
        int m = seconds / 60;
        int s = seconds % 60;
        if (m > 0) return $"{m}m {s}s";
        return $"{s}s";
    }
}
