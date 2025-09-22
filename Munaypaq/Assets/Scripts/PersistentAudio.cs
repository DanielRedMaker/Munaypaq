using UnityEngine;

public class PersistentAudio : MonoBehaviour
{
    public static PersistentAudio Instance { get; private set; }
    public AudioSource AudioSource { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AudioSource = gameObject.AddComponent<AudioSource>();
        AudioSource.playOnAwake = false;
        AudioSource.loop = true;
        AudioSource.spatialBlend = 0f; // 2D music
    }

    // Permite revisar desde otro script si existe la instancia
    public static bool InstanceExists()
    {
        return Instance != null;
    }

    // Asignar música y reproducir (si clip es null, no hace nada)
    public void SetMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        if (AudioSource.clip == clip)
        {
            if (!AudioSource.isPlaying)
                AudioSource.Play();
            AudioSource.volume = volume;
            return;
        }

        AudioSource.clip = clip;
        AudioSource.volume = volume;
        AudioSource.Play();
    }
}
