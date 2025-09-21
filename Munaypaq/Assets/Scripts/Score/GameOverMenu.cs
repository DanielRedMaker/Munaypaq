using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class GameOverMenu : MonoBehaviour
{
    public GameObject gameOverUI;   // Panel Game Over
    public TextMeshProUGUI scoreText;          // Texto para mostrar la puntuación final
    public TMP_InputField nameInput;    // Input para nombre del jugador
    public Button restartButton;
    public Button mainMenuButton;

    public string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnReturnToMainMenu);
    }

    // Llamar desde tu controlador de juego cuando el jugador pierde:
    // GameOverMenu.Instance.ShowGameOver();
    public void ShowGameOver()
    {
        // Pausar juego
        Time.timeScale = 0f;

        if (gameOverUI != null) gameOverUI.SetActive(true);

        // Mostrar score actual
        if (scoreText != null)
            scoreText.text = $"Puntuación: {ScoreManager.Instance.CurrentScore}";

        // Autollenar input con nombre anterior si existe
        if (nameInput != null)
            nameInput.text = ScoreManager.Instance.CurrentPlayerName;

        // Guardar automática al perder (con el nombre actual si lo hay)
        string playerName = (nameInput != null && !string.IsNullOrEmpty(nameInput.text)) ? nameInput.text : ScoreManager.Instance.CurrentPlayerName;
        ScoreManager.Instance.SetPlayerName(playerName);
        ScoreManager.Instance.SaveScoreAndCityState(playerName);
    }

    public void OnRestart()
    {
        // Restaurar timeScale antes de recargar
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
