using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HighScoreEntry
{
    public string playerName;
    public int score;
    public int cityDirtLevel;
    public string date;
    public int durationSeconds; // NUEVO: tiempo que duró la partida en segundos
}

[Serializable]
public class HighScoreList
{
    public List<HighScoreEntry> entries = new List<HighScoreEntry>();
}

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Highscore")]
    public int maxHighScoresToKeep = 10;

    // Estado en runtime
    public int CurrentScore { get; private set; } = 0;
    public string CurrentPlayerName { get; private set; } = "Player";

    // SESSION TIMER
    private DateTime sessionStartTime;
    public bool SessionRunning { get; private set; } = false;

    const string KEY_LAST_NAME = "LastPlayerName";
    const string KEY_LAST_SCORE = "LastScore";
    const string KEY_LAST_DIRT = "LastCityDirt";
    const string KEY_HIGHSCORES = "HighScoreList_v1";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (PlayerPrefs.HasKey(KEY_LAST_NAME))
            CurrentPlayerName = PlayerPrefs.GetString(KEY_LAST_NAME);
    }

    void Start()
    {
        // No arrancamos la sesión automáticamente; llamar StartSession() al iniciar una partida
    }

    #region Session control (nuevo)
    // Llamar cuando comienza la partida (por ejemplo al cargar Gameplay)
    public void StartSession()
    {
        sessionStartTime = DateTime.UtcNow;
        SessionRunning = true;
        CurrentScore = 0;
    }

    // Llamar para detener la sesión (opcional)
    public void StopSession()
    {
        SessionRunning = false;
    }

    // Devuelve los segundos transcurridos desde el inicio de la sesión (redondeado)
    public int GetSessionElapsedSeconds()
    {
        if (!SessionRunning) return 0;
        TimeSpan span = DateTime.UtcNow - sessionStartTime;
        return Mathf.Max(0, (int)span.TotalSeconds);
    }
    #endregion

    #region Gameplay API
    public void AddScore(int amount)
    {
        CurrentScore += amount;
    }

    public void SetPlayerName(string name)
    {
        if (!string.IsNullOrEmpty(name))
            CurrentPlayerName = name;
    }

    public void ResetScore()
    {
        CurrentScore = 0;
    }
    #endregion

    #region Saving / Loading (modificado para incluir duration)
    // Guarda la puntuación actual y el nivel de suciedad de la ciudad (llamar cuando se quiera puntuar)
    public void SaveScoreAndCityState(string playerName = null)
    {
        if (!string.IsNullOrEmpty(playerName))
            CurrentPlayerName = playerName;

        int dirt = GetCityDirtLevel();
        int duration = GetSessionElapsedSeconds();

        // Guardar últimos
        PlayerPrefs.SetString(KEY_LAST_NAME, CurrentPlayerName);
        PlayerPrefs.SetInt(KEY_LAST_SCORE, CurrentScore);
        PlayerPrefs.SetInt(KEY_LAST_DIRT, dirt);
        PlayerPrefs.Save();

        // Añadir a highscore (con duración)
        AddToHighScores(CurrentPlayerName, CurrentScore, dirt, duration);
        Debug.Log($"ScoreManager: guardado -> {CurrentPlayerName} {CurrentScore} dirt:{dirt} duration:{duration}s");
    }

    void AddToHighScores(string name, int score, int dirt, int durationSeconds)
    {
        HighScoreList list = LoadHighScoreList();
        HighScoreEntry entry = new HighScoreEntry
        {
            playerName = name,
            score = score,
            cityDirtLevel = dirt,
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            durationSeconds = durationSeconds
        };

        list.entries.Add(entry);

        // Orden descendente por score (si quieres ordenar por tiempo cambia aquí)
        list.entries.Sort((a, b) => b.score.CompareTo(a.score));

        if (list.entries.Count > maxHighScoresToKeep)
            list.entries.RemoveRange(maxHighScoresToKeep, list.entries.Count - maxHighScoresToKeep);

        string json = JsonUtility.ToJson(list);
        PlayerPrefs.SetString(KEY_HIGHSCORES, json);
        PlayerPrefs.Save();
    }

    public HighScoreList LoadHighScoreList()
    {
        if (PlayerPrefs.HasKey(KEY_HIGHSCORES))
        {
            try
            {
                string json = PlayerPrefs.GetString(KEY_HIGHSCORES);
                HighScoreList list = JsonUtility.FromJson<HighScoreList>(json);
                if (list == null) return new HighScoreList();
                return list;
            }
            catch
            {
                return new HighScoreList();
            }
        }
        return new HighScoreList();
    }
    #endregion

    #region City dirt detection (intento genérico)
    int GetCityDirtLevel()
    {
        var gm = GridManager.Instance;
        if (gm == null) return -1;

        var type = gm.GetType();

        // 1) Intentar método GetTotalTrashCount()
        var mi = type.GetMethod("GetTotalTrashCount");
        if (mi != null)
        {
            try
            {
                object r = mi.Invoke(gm, null);
                if (r is int) return (int)r;
            }
            catch { }
        }

        // 2) Intentar propiedad totalTrashCount
        var prop = type.GetProperty("totalTrashCount");
        if (prop != null)
        {
            try
            {
                object r = prop.GetValue(gm);
                if (r is int) return (int)r;
            }
            catch { }
        }

        // 3) Intentar método GetGameStats(out int trashCount, out int maxTrashCount, out float percentage)
        var mi2 = type.GetMethod("GetGameStats");
        if (mi2 != null)
        {
            try
            {
                // preparar argumentos para parámetros out
                object[] args = new object[] { 0, 0, 0f };
                mi2.Invoke(gm, args);
                if (args[0] is int) return (int)args[0];
            }
            catch { }
        }

        // 4) fallback: si GridManager tiene campo allTrash accesible (no recomendado), intentar por reflexión
        var field = type.GetField("allTrash", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            try
            {
                var listObj = field.GetValue(gm) as System.Collections.ICollection;
                if (listObj != null) return listObj.Count;
            }
            catch { }
        }

        // Si nada funciona, devolver -1
        return -1;
    }

    #endregion
}
