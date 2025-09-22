using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]
    public string gameplaySceneName = "Gameplay";
    public string creditsSceneName = "Credits";

    [Header("Audio")]
    public AudioClip backgroundMusic; // asignar tu música aquí
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    void Start()
    {
        // Asegurar que exista el PersistentAudio en la escena (o crearlo)
        if (!PersistentAudio.InstanceExists())
        {
            GameObject audioGO = new GameObject("PersistentAudio");
            PersistentAudio pa = audioGO.AddComponent<PersistentAudio>();
            pa.SetMusic(backgroundMusic, musicVolume);
        }
        else
        {
            // Si ya existe, y no está reproduciendo nada, intentar setear clip si fue asignado
            var inst = PersistentAudio.Instance;
            if (inst != null && inst.AudioSource != null && !inst.AudioSource.isPlaying)
            {
                if (backgroundMusic != null)
                {
                    inst.SetMusic(backgroundMusic, musicVolume);
                }
            }
        }
    }

    // Métodos públicos para enlazar a botones en el Inspector
    public void OnPlayPressed()
    {
        // Asumimos que la escena de gameplay se carga directamente (o podrías usar la LoadingScene)
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnCreditsPressed()
    {
        SceneManager.LoadScene(creditsSceneName);
    }
    public void OnHighScoresPressed()
    {
       
        SceneManager.LoadScene("HighScores");

    }
    public void OnMainMenuPressed()
    {

        SceneManager.LoadScene("MainMenu");

    }
    public void OnHistoryPressed()
    {

        SceneManager.LoadScene("History");

    }
    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
