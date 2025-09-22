using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI")]
    public Image logoImage;             // Image del logo (puede ser sprite)
    public Image progressFillImage;     // Image tipo Filled (Fill Method = Horizontal)
    public Image blackOverlay;          // Image negra que cubre la pantalla (alpha 1 = negro)

    [Header("Animation")]
    public float logoScaleMin = 0.6f;
    public float logoScaleMax = 1.15f;
    public float logoBounceSpeed = 3f;
    public Color colorA = new Color(0.0f, 0.4f, 1f); // azul
    public Color colorB = new Color(0.6f, 0.0f, 1f); // morado

    [Header("Load")]
    public string sceneToLoad = "MainMenu"; // escena destino (MainMenu)
    public float fakeProgressSpeed = 0.6f;  // suavizado visual si la carga va muy r�pido

    [Header("Fade & timing")]
    public float fadeDuration = 1.5f;    // tiempo que tarda el fondo negro en aclararse
    public float minLoadTime = 3.5f;     // tiempo m�nimo que permanecer� la pantalla de carga
    public bool useFixedLoadTime = false; // si true, ignoramos AsyncOperation.progress y usamos minLoadTime como duraci�n

    void Start()
    {
        // Ensure fill starts empty
        if (progressFillImage != null)
            progressFillImage.fillAmount = 0f;

        // Ensure overlay exists and starts black
        if (blackOverlay != null)
        {
            SetOverlayAlpha(1f);
            blackOverlay.gameObject.SetActive(true);
        }

        StartCoroutine(AnimateAndLoad());
    }

    IEnumerator AnimateAndLoad()
    {
        float startTime = Time.time;
        float displayedProgress = 0f;

        // Lanzar la carga as�ncrona pero no permitir activaci�n inmediata (si no usamos fixed time, necesitamos op)
        AsyncOperation op = null;
        if (!useFixedLoadTime)
        {
            op = SceneManager.LoadSceneAsync(sceneToLoad);
            op.allowSceneActivation = false;
        }

        while (true)
        {
            float elapsed = Time.time - startTime;

            // Determinar progress objetivo: si usamos tiempo fijo, lo basamos en minLoadTime; si no, usamos op.progress.
            float targetProgress;
            if (useFixedLoadTime)
            {
                // Si minLoadTime es 0, evitar divisi�n por cero
                if (minLoadTime <= 0.0001f) targetProgress = 1f;
                else targetProgress = Mathf.Clamp01(elapsed / minLoadTime);
            }
            else
            {
                // op.progress va de 0 a 0.9. 0.9 indica que la carga est� lista para activarse.
                if (op == null)
                    targetProgress = 0f;
                else
                    targetProgress = Mathf.Clamp01(op.progress / 0.9f);
            }

            // Suavizar el valor mostrado
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.deltaTime * fakeProgressSpeed);

            if (progressFillImage != null)
                progressFillImage.fillAmount = displayedProgress;

            // Animaci�n del logo: escala y color
            AnimateLogo();

            // Fade overlay alpha: queremos que pase de 1 -> 0 en fadeDuration segundos desde el inicio
            if (blackOverlay != null)
            {
                float fadeT = Mathf.Clamp01(elapsed / fadeDuration);
                SetOverlayAlpha(1f - fadeT);
            }

            // Condici�n para terminar:
            // Si usamos fixed time: cuando displayedProgress >= 0.99 y elapsed >= minLoadTime -> activamos carga final (si op existe) o terminamos
            // Si usamos real op: cuando op.progress >= 0.9 && displayedProgress >= 0.99 && elapsed >= minLoadTime
            if (useFixedLoadTime)
            {
                if (displayedProgress >= 0.99f && elapsed >= minLoadTime)
                {
                    // Si op existe (por seguridad), permitir activaci�n; sino simplemente salimos y cargamos de forma sincr�nica
                    if (op != null)
                    {
                        op.allowSceneActivation = true;
                        yield break;
                    }
                    else
                    {
                        // carga sincr�nica final (fallback)
                        SceneManager.LoadScene(sceneToLoad);
                        yield break;
                    }
                }
            }
            else
            {
                if (op != null && op.progress >= 0.9f && displayedProgress >= 0.99f && elapsed >= minLoadTime)
                {
                    // peque�a pausa para que el usuario vea 100% y el fade haya terminado
                    yield return new WaitForSeconds(0.15f);

                    op.allowSceneActivation = true;
                    yield break;
                }
            }

            yield return null;
        }
    }

    void AnimateLogo()
    {
        if (logoImage == null) return;

        float t = (Mathf.Sin(Time.time * logoBounceSpeed) + 1f) / 2f; // 0..1
        float scale = Mathf.Lerp(logoScaleMin, logoScaleMax, t);
        logoImage.rectTransform.localScale = Vector3.one * scale;

        // Interpolar color entre azul y morado
        Color c = Color.Lerp(colorA, colorB, t);
        logoImage.color = c;
    }

    void SetOverlayAlpha(float a)
    {
        if (blackOverlay == null) return;
        Color col = blackOverlay.color;
        col.a = Mathf.Clamp01(a);
        blackOverlay.color = col;
    }

    // M�todo p�blico para hacer que la carga use tiempo fijo (�til para testing)
    public void SetUseFixedLoadTime(bool useFixed)
    {
        useFixedLoadTime = useFixed;
    }
}
