using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Instruction panel shown when a level starts (e.g. the Melee Dungeon).
/// It's a normal canvas in the scene - edit the Title / Body text directly in the Editor.
/// The game pauses while it is open; press Enter (or E) to start.
/// Shown once per play session per scene, so it doesn't reappear after dying.
/// </summary>
public class LevelIntro : MonoBehaviour
{
    public CanvasGroup group;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    [Header("Behaviour")]
    public bool pauseGame = true;
    public bool showOncePerSession = true;
    public float waitBeforeShow = 0.5f;   // lets the scene fade in first
    public float fadeTime = 0.3f;

    /// <summary>True while any level intro is on screen (player input is ignored).</summary>
    public static bool IsOpen { get; private set; }

    private static readonly System.Collections.Generic.HashSet<string> shown = new System.Collections.Generic.HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { shown.Clear(); IsOpen = false; }

    private void Start()
    {
        string key = gameObject.scene.name + "/" + name;
        if (showOncePerSession && shown.Contains(key))
        {
            gameObject.SetActive(false);
            return;
        }
        shown.Add(key);
        if (group != null) group.alpha = 0f;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(waitBeforeShow);

        IsOpen = true;
        float oldScale = Time.timeScale;
        if (pauseGame) Time.timeScale = 0f;

        yield return Fade(0f, 1f);
        yield return null;
        while (!(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E)))
            yield return null;

        if (pauseGame) Time.timeScale = oldScale > 0f ? oldScale : 1f;
        yield return Fade(1f, 0f);
        IsOpen = false;
        gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (group == null) yield break;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        group.alpha = to;
    }

    private void OnDisable()
    {
        if (IsOpen)
        {
            IsOpen = false;
            if (pauseGame && Time.timeScale == 0f) Time.timeScale = 1f;
        }
    }
}
