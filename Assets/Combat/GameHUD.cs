using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// On-screen HUD built entirely from code, so nothing has to be set up in the scene:
///  - health bar + "current / max" text in the top-left corner
///  - collected items (Key, Crystal) shown under the health bar
///  - a short message line at the top-centre ("You got the KEY!", "Door is locked", ...)
/// Created automatically the first time something uses GameHUD.Instance.
/// </summary>
public class GameHUD : MonoBehaviour
{
    private static GameHUD instance;
    private static Sprite whiteSprite;

    public static GameHUD Instance
    {
        get
        {
            if (instance == null) instance = Create();
            return instance;
        }
    }

    /// <summary>1x1-unit white square, used for the bar fill and placeholder pickups.</summary>
    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D tex = new Texture2D(4, 4);
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                whiteSprite.name = "HUD White";
            }
            return whiteSprite;
        }
    }

    public HealthBar HealthBar { get; private set; }

    private RectTransform inventoryRow;
    private int inventoryCount;
    private TextMeshProUGUI messageText;
    private CanvasGroup messageGroup;
    private float messageHideTime;

    private static GameHUD Create()
    {
        GameObject root = new GameObject("GameHUD (auto)");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10; // stays behind the pause menu

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameHUD hud = root.AddComponent<GameHUD>();
        hud.Build(root.transform);
        return hud;
    }

    private void Build(Transform root)
    {
        // ---------- Health bar (top-left) ----------
        RectTransform barBg = NewImage("HealthBar", root, new Color(0f, 0f, 0f, 0.65f));
        SetTopLeft(barBg, new Vector2(30, -30), new Vector2(380, 38));

        RectTransform fillRt = NewImage("Fill", barBg, new Color(0.85f, 0.12f, 0.12f));
        Stretch(fillRt, 4);
        Image fill = fillRt.GetComponent<Image>();
        fill.sprite = WhiteSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        TextMeshProUGUI hpText = NewText("HealthText", barBg, "100 / 100", 24, TextAlignmentOptions.Center);
        Stretch(hpText.rectTransform, 0);

        TextMeshProUGUI hpLabel = NewText("HPLabel", root, "HP", 26, TextAlignmentOptions.Left);
        SetTopLeft(hpLabel.rectTransform, new Vector2(420, -30), new Vector2(80, 38));

        HealthBar = barBg.gameObject.AddComponent<HealthBar>();
        HealthBar.healthFillImage = fill;
        HealthBar.healthText = hpText;

        // ---------- Inventory row (under the bar) ----------
        GameObject row = new GameObject("Inventory", typeof(RectTransform));
        row.transform.SetParent(root, false);
        inventoryRow = row.GetComponent<RectTransform>();
        SetTopLeft(inventoryRow, new Vector2(30, -80), new Vector2(600, 80));

        // ---------- Message line (top-centre) ----------
        messageText = NewText("Message", root, "", 36, TextAlignmentOptions.Center);
        RectTransform m = messageText.rectTransform;
        m.anchorMin = m.anchorMax = new Vector2(0.5f, 1f);
        m.pivot = new Vector2(0.5f, 1f);
        m.anchoredPosition = new Vector2(0, -140);
        m.sizeDelta = new Vector2(1400, 60);
        messageText.fontStyle = FontStyles.Bold;
        messageGroup = messageText.gameObject.AddComponent<CanvasGroup>();
        messageGroup.alpha = 0f;
    }

    /// <summary>Shows a message at the top of the screen for a few seconds.</summary>
    public void ShowMessage(string text, float duration = 2.5f)
    {
        messageText.text = text;
        messageGroup.alpha = 1f;
        messageHideTime = Time.time + duration;
    }

    public void AddInventoryIcon(CollectibleType type, Sprite icon, Color tint)
    {
        RectTransform slot = NewImage(type + "Icon", inventoryRow, tint);
        Image img = slot.GetComponent<Image>();
        img.sprite = icon != null ? icon : WhiteSprite;
        img.preserveAspect = true;
        SetTopLeft(slot, new Vector2(inventoryCount * 110, 0), new Vector2(48, 48));

        TextMeshProUGUI label = NewText(type + "Label", inventoryRow, type.ToString(), 20, TextAlignmentOptions.Center);
        SetTopLeft(label.rectTransform, new Vector2(inventoryCount * 110 - 26, -50), new Vector2(100, 26));

        inventoryCount++;
    }

    private void Update()
    {
        if (messageGroup != null && messageGroup.alpha > 0f && Time.time >= messageHideTime)
        {
            messageGroup.alpha = Mathf.MoveTowards(messageGroup.alpha, 0f, Time.deltaTime * 2f);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // ---------- small UI helpers ----------
    private static RectTransform NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return go.GetComponent<RectTransform>();
    }

    public static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static void SetTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
