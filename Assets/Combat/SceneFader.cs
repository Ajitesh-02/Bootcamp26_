using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Fade to black when leaving a scene and fade back in when the next one loads.
/// Creates itself automatically when the game starts - nothing to set up in any scene.
///
/// To change scene WITH a fade, call:  SceneFader.LoadScene("SceneName");
/// (Plain SceneManager.LoadScene still works and will still fade IN on the new scene.)
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static float fadeOutTime = 0.45f;
    public static float fadeInTime = 0.45f;
    public static Color fadeColor = Color.black;

    private static SceneFader instance;
    private CanvasGroup group;
    private Image overlay;
    private bool isLoading;
    private bool isFading;

    // Reset leftovers from the previous Play session (needed when "Enter Play Mode Options"
    // skips the domain reload - otherwise a destroyed fader is still subscribed).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("SceneFader (auto)");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneFader>();
        instance.Build();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance == null) return; // destroyed (e.g. leaving Play mode) - nothing to do
        instance.OnSceneLoaded(scene, mode);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000; // above everything, including the pause menu
        gameObject.AddComponent<CanvasScaler>();

        GameObject img = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        img.transform.SetParent(transform, false);
        overlay = img.GetComponent<Image>();
        overlay.color = fadeColor;
        overlay.raycastTarget = true; // blocks clicks while fading
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f; // start black, the first scene fades in
    }

    // Safety net: never leave the screen black if no load is in progress
    private void Start()
    {
        if (!isLoading) StartCoroutine(FadeTo(0f, fadeInTime));
    }

    private void Update()
    {
        if (!isLoading && !isFading && group.alpha > 0f)
        {
            StartCoroutine(FadeTo(0f, fadeInTime));
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        isLoading = false;
        StopAllCoroutines();
        isFading = false;
        StartCoroutine(FadeTo(0f, fadeInTime));
    }

    /// <summary>Fade out, load the scene by name, fade back in.</summary>
    public static void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("SceneFader: scene '" + sceneName + "' can't be loaded. Is it added to File > Build Profiles (Scene List)?");
            return;
        }
        if (instance == null) Bootstrap();
        instance.StartLoad(() => SceneManager.LoadScene(sceneName));
    }

    /// <summary>Fade out, load the scene by build index, fade back in.</summary>
    public static void LoadScene(int buildIndex)
    {
        if (instance == null) Bootstrap();
        instance.StartLoad(() => SceneManager.LoadScene(buildIndex));
    }

    private void StartLoad(System.Action load)
    {
        if (isLoading) return;
        isLoading = true;
        StopAllCoroutines();
        isFading = false;
        StartCoroutine(FadeOutThenLoad(load));
    }

    private IEnumerator FadeOutThenLoad(System.Action load)
    {
        Time.timeScale = 1f; // in case we leave from the pause menu
        yield return FadeTo(1f, fadeOutTime);
        load();

        // If the load failed for any reason, don't stay stuck on black
        yield return new WaitForSecondsRealtime(3f);
        isLoading = false;
    }

    private IEnumerator FadeTo(float target, float time)
    {
        isFading = true;
        group.blocksRaycasts = true;
        float start = group.alpha;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime; // works even while paused
            group.alpha = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        group.alpha = target;
        group.blocksRaycasts = target > 0.01f;
        isFading = false;
    }
}
