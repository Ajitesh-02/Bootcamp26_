using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// The in-game HUD. Lives on the prefab Assets/Combat/Resources/UI/GameHUD.prefab
/// (made by Tools > Bootcamp > Build UI) - edit its look in the Editor like any canvas.
///  - health bar + "current / max" (top-left, always visible)
///  - potion counter under it, collected items (Key, Crystal) next to that
///  - coins (top-right)
///  - message line (top-centre)
/// It is spawned automatically in every scene except the main menu.
/// If a scene already contains a GameHUD, that one is used instead.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Health")]
    public HealthBar healthBar;            // drives fill + "60 / 100" text

    [Header("Coins")]
    public TextMeshProUGUI coinText;
    public RectTransform coinIcon;

    [Header("Potions")]
    public GameObject potionGroup;
    public TextMeshProUGUI potionText;

    [Header("Collected items")]
    public RectTransform inventoryRow;
    [Tooltip("Inactive child used as the template for Key / Crystal icons.")]
    public GameObject inventoryItemTemplate;

    [Header("Message")]
    public CanvasGroup messageGroup;
    public TextMeshProUGUI messageText;

    [Header("Sprites")]
    public Sprite coinSprite;              // also used for coin pickups in the world

    /// <summary>Scenes where the HUD should NOT appear.</summary>
    public static readonly string[] HiddenInScenes = { "Main Menu" };
    public const string PrefabPath = "UI/GameHUD";

    private static GameHUD instance;
    private static bool warned;
    private static Sprite whiteSprite, generatedCoin;

    private float messageHideTime;
    private float coinPunch;
    private int shownCoins = -1;

    public HealthBar HealthBar => healthBar;

    public static GameHUD Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = FindFirstObjectByType<GameHUD>();
            if (instance != null) return instance;

            GameObject prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab != null)
            {
                instance = Instantiate(prefab).GetComponent<GameHUD>();
                instance.name = "GameHUD";
            }
            else
            {
                if (!warned) Debug.LogError("GameHUD prefab missing. Run  Tools > Bootcamp > Build UI  in the Unity menu.");
                warned = true;
                instance = new GameObject("GameHUD (missing prefab)").AddComponent<GameHUD>();
            }
            return instance;
        }
    }

    // ---------- auto-spawn in gameplay scenes ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
        warned = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        if (System.Array.IndexOf(HiddenInScenes, scene.name) >= 0) return;
        Instance.Refresh();
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        if (messageGroup != null) messageGroup.alpha = 0f;
        if (inventoryItemTemplate != null) inventoryItemTemplate.SetActive(false);
        PlayerProgress.Changed += Refresh;
    }

    private void Start() { Refresh(); }

    private void OnDestroy()
    {
        PlayerProgress.Changed -= Refresh;
        if (instance == this) instance = null;
    }

    /// <summary>Re-reads health, coins and potions from PlayerProgress.</summary>
    public void Refresh()
    {
        if (this == null) return;
        if (healthBar != null) healthBar.UpdateHealthBar(PlayerProgress.Health, PlayerProgress.MaxHealth);

        if (coinText != null)
        {
            if (shownCoins >= 0 && PlayerProgress.Coins > shownCoins) coinPunch = 1f;
            shownCoins = PlayerProgress.Coins;
            coinText.text = PlayerProgress.Coins.ToString();
        }

        if (potionText != null) potionText.text = "x " + PlayerProgress.Potions;
        if (potionGroup != null) potionGroup.SetActive(PlayerProgress.Potions > 0 || PlayerProgress.GuideIntroDone);
    }

    /// <summary>Shows a message at the top of the screen for a few seconds.</summary>
    public void ShowMessage(string text, float duration = 2.5f)
    {
        if (messageText == null || messageGroup == null) { Debug.Log("[HUD] " + text); return; }
        messageText.text = text;
        messageGroup.alpha = 1f;
        messageHideTime = Time.unscaledTime + duration;
    }

    public void AddInventoryIcon(CollectibleType type, Sprite icon, Color tint)
    {
        if (inventoryItemTemplate == null || inventoryRow == null) return;
        GameObject item = Instantiate(inventoryItemTemplate, inventoryRow);
        item.name = type + "Item";
        item.SetActive(true);

        Image img = null;
        foreach (Image i in item.GetComponentsInChildren<Image>(true))
            if (i.gameObject != item) { img = i; break; }
        if (img != null)
        {
            img.sprite = icon != null ? icon : WhiteSprite;
            img.color = tint;
            img.preserveAspect = true;
        }
        TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.text = type.ToString().ToUpper();
    }

    private void Update()
    {
        if (messageGroup != null && messageGroup.alpha > 0f && Time.unscaledTime >= messageHideTime)
            messageGroup.alpha = Mathf.MoveTowards(messageGroup.alpha, 0f, Time.unscaledDeltaTime * 2f);

        if (coinIcon != null)
        {
            coinPunch = Mathf.MoveTowards(coinPunch, 0f, Time.unscaledDeltaTime * 4f);
            coinIcon.localScale = Vector3.one * (1f + 0.35f * coinPunch);
        }
    }

    // ---------- sprites used by world pickups ----------
    public static Sprite CoinSprite
    {
        get
        {
            GameHUD hud = Instance;
            if (hud != null && hud.coinSprite != null) return hud.coinSprite;
            if (generatedCoin == null) generatedCoin = MakeCircleSprite(new Color(1f, 0.8f, 0.15f));
            return generatedCoin;
        }
    }

    /// <summary>1x1-unit white square (placeholder pickups).</summary>
    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D tex = new Texture2D(4, 4);
                Color[] px = new Color[16];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            return whiteSprite;
        }
    }

    private static Sprite MakeCircleSprite(Color c)
    {
        const int S = 16;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f)) <= 7f ? c : Color.clear);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
    }

    /// <summary>Old helper kept for IntroMessage (no longer used in scenes).</summary>
    public static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = align; t.raycastTarget = false;
        return t;
    }
}
