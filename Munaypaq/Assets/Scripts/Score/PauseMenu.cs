using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class PauseMenu : MonoBehaviour
{
    public GameObject pauseUI;         // Panel con el menú de pausa
    public TMP_InputField nameInput;       // Input donde el jugador escribe su nombre (opcional)
    public Button resumeButton;
    public Button saveAndExitButton;

    public string mainMenuSceneName = "MainMenu";

    bool isPaused = false;

    void Start()
    {
        if (pauseUI != null)
            pauseUI.SetActive(false);

        // Si no asignaron botones, no hacer nada
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
        if (saveAndExitButton != null) saveAndExitButton.onClick.AddListener(OnSaveAndExit);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused) OnResume();
        else OnPause();
    }

    void OnPause()
    {
        isPaused = true;
        if (pauseUI != null) pauseUI.SetActive(true);
        Time.timeScale = 0f;
        // Opcional: pausar audios si quieres
    }

    public void OnResume()
    {
        isPaused = false;
        if (pauseUI != null) pauseUI.SetActive(false);
        Time.timeScale = 1f;
    }

    // Puntuar y volver al menú principal
    public void OnSaveAndExit()
    {
        string playerName = (nameInput != null && !string.IsNullOrEmpty(nameInput.text)) ? nameInput.text : ScoreManager.Instance.CurrentPlayerName;
        ScoreManager.Instance.SetPlayerName(playerName);
        ScoreManager.Instance.SaveScoreAndCityState(playerName);

        // Restaurar time scale por si acaso
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
