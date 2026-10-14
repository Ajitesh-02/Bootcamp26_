using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a game overview panel when the hub scene starts, then fades it out.
/// Put it on the hub player. Text is editable in the Inspector.
/// Press Enter to skip it.
/// </summary>
public class IntroMessage : MonoBehaviour
{
    [Header("Text")]
    public string title = "Welcome, Knight!";
    [TextArea(5, 14)]
    public string body =
        "Three cursed dungeons stand between you and freedom.\n\n" +
        "Step into a dungeon gate to enter it. They must be cleared in order:\n" +
        "<b>Melee Dungeon</b>, then <b>Ranged Dungeon</b>, then <b>Trap Dungeon</b>.\n\n" +
        "Defeat the Elite guards: one carries the <color=#FFD23F><b>KEY</b></color> that opens the door,\n" +
        "the other carries the <color=#4DE6FF><b>CRYSTAL</b></color> you need to escape.\n\n" +
        "Hub: <b>WASD</b> move        Dungeons: <b>A / D</b> move, <b>Space</b> jump\n" +
        "<b>H / U / K</b> attack     <b>J</b> Air Slash     <b>C</b> Lightning     <b>Esc</b> pause";

    [Header("Look")]
    public Color panelColor = new Color(0.07f, 0.05f, 0.05f, 1f);   // fully opaque = crisp, no scene showing through
    public Color borderColor = new Color(0.85f, 0.65f, 0.25f, 1f);
    public float titleSize = 60f;
    public float bodySize = 32f;

    [Header("Timing")]
    public float startDelay = 0.6f;     // waits for the scene fade-in
    public float fadeInTime = 0.4f;
    public float holdTime = 7f;
    public float fadeOutTime = 0.6f;
    [Tooltip("Only show the first time the hub loads (not every time you come back from a dungeon).")]
    public bool showOnlyOnce = true;

    private static bool alreadyShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { alreadyShown = false; }

    private void Start()
    {
        if (showOnlyOnce && alreadyShown) return;
        alreadyShown = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // ---- canvas (pixel perfect, 1:1 with a 1920x1080 layout) ----
        GameObject root = new GameObject("IntroMessage (auto)");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        // ---- border + opaque panel ----
        RectTransform border = NewBox("Border", root.transform, borderColor, new Vector2(1260, 640));
        RectTransform panel = NewBox("Panel", border, panelColor, Vector2.zero);
        panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
        panel.offsetMin = new Vector2(4, 4); panel.offsetMax = new Vector2(-4, -4);

        // ---- title ----
        TextMeshProUGUI titleText = NewCrispText("Title", panel, title, titleSize);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.84f, 0.4f);
        RectTransform trt = titleText.rectTransform;
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.anchoredPosition = new Vector2(0, -30);
        trt.sizeDelta = new Vector2(-60, 80);

        // ---- body ----
        TextMeshProUGUI bodyText = NewCrispText("Body", panel, body, bodySize);
        bodyText.lineSpacing = 8f;
        RectTransform brt = bodyText.rectTransform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(50, 70);
        brt.offsetMax = new Vector2(-50, -125);

        // ---- hint ----
        TextMeshProUGUI hint = NewCrispText("Hint", panel, "Press Enter to continue", 22);
        hint.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        RectTransform hrt = hint.rectTransform;
        hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(1, 0);
        hrt.pivot = new Vector2(0.5f, 0);
        hrt.anchoredPosition = new Vector2(0, 20);
        hrt.sizeDelta = new Vector2(0, 32);

        // ---- show, hold, hide ----
        yield return new WaitForSeconds(startDelay);
        yield return Fade(group, 0f, 1f, fadeInTime);

        float end = Time.time + holdTime;
        while (Time.time < end && !SkipPressed()) yield return null;

        yield return Fade(group, 1f, 0f, fadeOutTime);
        Destroy(root);
    }

    private static RectTransform NewBox(string name, Transform parent, Color color, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>White TMP text with no auto-sizing or blur-causing settings.</summary>
    private static TextMeshProUGUI NewCrispText(string name, Transform parent, string text, float size)
    {
        TextMeshProUGUI t = GameHUD.NewText(name, parent, text, size, TextAlignmentOptions.Center);
        t.enableAutoSizing = false;
        t.richText = true;
        t.extraPadding = true;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private static bool SkipPressed()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    private static IEnumerator Fade(CanvasGroup g, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        g.alpha = to;
    }
}
