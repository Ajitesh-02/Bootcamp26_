using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The hub Guide ("Aldric").
///  - Walk up to him: he says a short voice line and a "[E] Talk" prompt appears.
///  - First talk: explains the game, then shows his shop.
///  - Later talks: opens the shop (Lightning, Air Slash, Health Booster) with a context line.
///  - After the first talk an arrow around the player points to the next gate
///    (or back to the Guide when you have enough coins to buy something).
/// Sprite and voice lines are loaded from Assets/Combat/Resources (Guide/, GuideVoice/)
/// unless you assign your own in the Inspector.
/// </summary>
public class GuideNPC : MonoBehaviour
{
    [Header("Look")]
    public Sprite idleSprite;          // empty = Resources/Guide/guide_idle
    public Sprite castSprite;          // empty = Resources/Guide/guide_cast
    public float worldHeight = 1.6f;
    public string guideName = "Aldric, the Guide";

    [Header("Interaction")]
    public float talkRadius = 2.5f;
    public KeyCode talkKey = KeyCode.E;

    [Header("Shop prices (coins)")]
    public int lightningPrice = 30;
    public int airSlashPrice = 50;
    public int healPrice = 20;
    public int healAmount = 50;
    public int potionPrice = 15;
    [Tooltip("HP restored when the player drinks a potion with Q (set on the Player too).")]
    public int potionHealAmount = 40;

    [Header("UI prefab (Tools > Bootcamp > Build UI)")]
    public GuideUIView uiPrefab;   // empty = Resources/UI/GuideUI

    [Header("Voice")]
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public float approachVoiceCooldown = 10f;

    // ---------- lines (text shown on screen; voice file = Resources/GuideVoice/<key>) ----------
    public static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
    {
        { "approach_first",  "Ah, a new knight! Come closer. Press E, and I will tell you what lies ahead." },
        { "approach_return", "Welcome back, knight. Press E if you wish to talk." },
        { "approach_coins",  "I see coins in your pouch! Press E, and I will teach you new powers." },
        { "intro_1", "Welcome, knight. I am Aldric, guide of these lands. Three cursed dungeons stand between you and freedom." },
        { "intro_2", "Clear them in order: first the Melee Dungeon, then the Ranged Dungeon, and last, the Trap Dungeon." },
        { "intro_3", "For now, you fight with your blade alone. Strike with H, U and K. Move with A and D, and jump with Space." },
        { "intro_4", "The guards of the Melee Dungeon carry coins. Defeat them, collect the coins, and bring them back to me." },
        { "intro_5", "With coins, I can teach you Lightning and Air Slash, and sell you potions. Press Q in a dungeon to drink one. Your health does not return by itself." },
        { "intro_6", "This is my shop. When you have enough coins, press one, two, three or four to buy. Now follow the arrow to the first gate." },
        { "shop_greet",   "What will it be, knight? Choose a power, and pay the price." },
        { "no_coins",     "Your pouch is empty. Return to the Melee Dungeon and defeat its guards to earn coins." },
        { "not_enough",   "You lack the coins, knight. Return to the Melee Dungeon and defeat its guards again." },
        { "buy_lightning","Lightning is yours. Press C to strike your foes from afar." },
        { "buy_airslash", "Air Slash is yours. Press J to send your blade flying." },
        { "heal",         "Drink this. Your wounds will close." },
        { "buy_potion",   "A potion for the road. Press Q when your wounds grow deep." },
        { "potions_full", "You cannot carry more than three potions." },
        { "full_health",  "You are already at full strength." },
        { "owned",        "You have already learned that power." },
        { "all_learned",  "I have taught you all I know. Go, and clear the dungeons." },
        { "died",         "Back so soon? Rest, knight. Your coins and skills are safe. Spend them wisely, and try again." },
        { "farewell",     "Go well, knight." },
    };

    private static readonly string[] IntroKeys = { "intro_1", "intro_2", "intro_3", "intro_4", "intro_5", "intro_6" };

    // ---------- runtime ----------
    private SpriteRenderer sr;
    private AudioSource voice;
    private Transform player;
    private Collider2D playerCol;
    private bool wasInRange;
    private float nextApproachVoice;
    private float baseScale;

    private bool panelOpen;
    private bool inShop;
    private Queue<string> pendingLines = new Queue<string>();
    private bool typing;
    private string fullText = "";
    private float typeStart;

    private readonly List<Behaviour> frozen = new List<Behaviour>();

    // UI (instance of the GuideUI prefab)
    private GuideUIView ui;
    private RectTransform prompt;
    private GameObject dialogBox, shopBox;
    private TextMeshProUGUI dialogText, dialogHint, shopSay, shopCoins;

    // ============================================================
    private void Start()
    {
        BuildSprite();
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0f;
        BuildUI();

        GuideArrow arrow = gameObject.AddComponent<GuideArrow>();
        arrow.guide = this;

        if (PlayerProgress.JustDied)
        {
            PlayerProgress.JustDied = false;
            StartCoroutine(SayAfter("died", 1.0f));
        }
    }

    private IEnumerator SayAfter(string key, float delay)
    {
        yield return new WaitForSeconds(delay);
        Say(key, true);
    }

    private void BuildSprite()
    {
        if (idleSprite == null) idleSprite = LoadPixelSprite("Guide/guide_idle");
        if (castSprite == null) castSprite = LoadPixelSprite("Guide/guide_cast");

        GameObject s = new GameObject("GuideSprite");
        s.transform.SetParent(transform, false);
        sr = s.AddComponent<SpriteRenderer>();
        sr.sprite = idleSprite;
        sr.sortingOrder = 1;

        // copy the material the hub player uses (keeps lighting consistent)
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            SpriteRenderer psr = p.GetComponentInChildren<SpriteRenderer>();
            if (psr != null) sr.sharedMaterial = psr.sharedMaterial;
        }

        if (idleSprite != null)
        {
            float h = idleSprite.bounds.size.y;
            baseScale = h > 0.0001f ? worldHeight / h : 1f;
            s.transform.localScale = Vector3.one * baseScale;
        }

        // small solid body so the player can't walk through him
        BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(worldHeight * 0.45f, worldHeight * 0.35f);
        box.offset = new Vector2(0f, -worldHeight * 0.3f);
    }

    /// <summary>Loads a texture from Resources and makes a crisp pixel-art sprite (16 px = 1 unit).</summary>
    private static Sprite LoadPixelSprite(string path)
    {
        Sprite asSprite = Resources.Load<Sprite>(path);
        Texture2D tex = asSprite != null ? asSprite.texture : Resources.Load<Texture2D>(path);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
    }

    // ============================================================
    private void Update()
    {
        // idle "breathing"
        if (sr != null)
        {
            float b = 1f + Mathf.Sin(Time.time * 2.2f) * 0.025f;
            sr.transform.localScale = new Vector3(baseScale, baseScale * b, 1f);
        }

        if (!FindPlayer()) return;

        float dist = Vector2.Distance(PlayerCenter(), transform.position);
        bool inRange = dist <= talkRadius;

        // walked up to the guide: short voice hint
        if (inRange && !wasInRange && !panelOpen && Time.time >= nextApproachVoice)
        {
            nextApproachVoice = Time.time + approachVoiceCooldown;
            string key = !PlayerProgress.GuideIntroDone ? "approach_first"
                       : CanAffordSomething() ? "approach_coins"
                       : "approach_return";
            Say(key, true);
        }
        wasInRange = inRange;

        // prompt above his head
        bool showPrompt = inRange && !panelOpen;
        prompt.gameObject.SetActive(showPrompt);
        if (showPrompt && Camera.main != null)
        {
            Vector3 head = transform.position + Vector3.up * (worldHeight * 0.75f);
            prompt.position = Camera.main.WorldToScreenPoint(head);
        }

        if (!panelOpen)
        {
            if (inRange && Input.GetKeyDown(talkKey)) Open();
            return;
        }

        // ---- panel input ----
        if (!inShop)
        {
            if (Input.GetKeyDown(talkKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (typing) FinishTyping();
                else NextLine();
            }
            UpdateTyping(dialogText);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Buy(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Buy(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) Buy(2);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) Buy(3);
            if (Input.GetKeyDown(talkKey)) Close();
            UpdateTyping(shopSay);
        }
    }

    private bool FindPlayer()
    {
        if (player != null) return true;
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) return false;
        player = p.transform;
        playerCol = p.GetComponent<Collider2D>();
        return true;
    }

    public Vector3 PlayerCenter()
    {
        if (player == null) return transform.position;
        return playerCol != null ? playerCol.bounds.center : player.position;
    }

    public Transform PlayerTransform => player;
    public bool PanelOpen => panelOpen;

    // ============================================================
    // Talking
    // ============================================================
    private void Open()
    {
        panelOpen = true;
        FreezePlayer(true);

        if (!PlayerProgress.GuideIntroDone)
        {
            pendingLines.Clear();
            foreach (string k in IntroKeys) pendingLines.Enqueue(k);
            ShowDialog();
            NextLine();
        }
        else
        {
            string key = AllLearned() && PlayerProgress.Health >= PlayerProgress.MaxHealth ? "all_learned"
                       : PlayerProgress.Coins == 0 ? "no_coins"
                       : !CanAffordSomething() ? "not_enough"
                       : "shop_greet";
            ShowShop(key);
        }
    }

    private void NextLine()
    {
        if (pendingLines.Count == 0)
        {
            // intro finished -> show the shop so the player sees how buying works
            PlayerProgress.GuideIntroDone = true;
            ShowShop(null);
            return;
        }

        string key = pendingLines.Dequeue();
        // the last intro line is said while the shop is visible
        if (key == "intro_6")
        {
            PlayerProgress.GuideIntroDone = true;
            ShowShop("intro_6");
            return;
        }

        StartTyping(dialogText, Lines[key]);
        PlayVoice(key);
        if (dialogHint != null) dialogHint.text = "Next";
    }

    private void Close()
    {
        panelOpen = false;
        inShop = false;
        dialogBox.SetActive(false);
        shopBox.SetActive(false);
        FreezePlayer(false);
        if (voice.isPlaying) voice.Stop();
        Say("farewell", false);
    }

    private void ShowDialog()
    {
        inShop = false;
        dialogBox.SetActive(true);
        shopBox.SetActive(false);
    }

    private void ShowShop(string sayKey)
    {
        inShop = true;
        dialogBox.SetActive(false);
        shopBox.SetActive(true);
        RefreshShop();
        if (sayKey != null)
        {
            StartTyping(shopSay, Lines[sayKey]);
            PlayVoice(sayKey);
        }
        else
        {
            shopSay.text = "";
        }
    }

    /// <summary>Voice line + subtitle on the HUD (used when the panel is closed).</summary>
    private void Say(string key, bool subtitle)
    {
        PlayVoice(key);
        if (subtitle) GameHUD.Instance.ShowMessage(Lines[key], 4f);
    }

    private void PlayVoice(string key)
    {
        AudioClip clip = Resources.Load<AudioClip>("GuideVoice/" + key);
        if (clip == null) return;               // no voice file yet: text only
        voice.Stop();
        voice.PlayOneShot(clip, voiceVolume);
    }

    // ---- typewriter ----
    private TextMeshProUGUI typingTarget;
    private void StartTyping(TextMeshProUGUI target, string text)
    {
        typingTarget = target;
        fullText = text;
        target.text = text;
        target.maxVisibleCharacters = 0;
        typeStart = Time.unscaledTime;
        typing = true;
    }

    private void UpdateTyping(TextMeshProUGUI target)
    {
        if (!typing || typingTarget != target) return;
        int n = Mathf.FloorToInt((Time.unscaledTime - typeStart) * 55f);
        target.maxVisibleCharacters = n;
        if (n >= fullText.Length) FinishTyping();
    }

    private void FinishTyping()
    {
        typing = false;
        if (typingTarget != null) typingTarget.maxVisibleCharacters = 99999;
    }

    // ============================================================
    // Shop
    // ============================================================
    private bool AllLearned() { return PlayerProgress.Has(Skill.LightningBolt) && PlayerProgress.Has(Skill.AirSlash); }

    public bool CanAffordSomething()
    {
        int c = PlayerProgress.Coins;
        if (!PlayerProgress.Has(Skill.LightningBolt) && c >= lightningPrice) return true;
        if (!PlayerProgress.Has(Skill.AirSlash) && c >= airSlashPrice) return true;
        if (PlayerProgress.Health < PlayerProgress.MaxHealth && c >= healPrice) return true;
        if (PlayerProgress.Potions < PlayerProgress.MaxPotions && c >= potionPrice && PlayerProgress.Health < PlayerProgress.MaxHealth) return true;
        return false;
    }

    private void Buy(int index)
    {
        FinishTyping();
        switch (index)
        {
            case 0: BuySkill(Skill.LightningBolt, lightningPrice, "buy_lightning"); break;
            case 1: BuySkill(Skill.AirSlash, airSlashPrice, "buy_airslash"); break;
            case 2:
                if (PlayerProgress.Potions >= PlayerProgress.MaxPotions) { Respond("potions_full"); break; }
                if (!PlayerProgress.TrySpend(potionPrice)) { Respond("not_enough"); break; }
                PlayerProgress.AddPotion();
                StartCoroutine(CastFlash(new Color(1f, 0.5f, 0.5f)));
                Respond("buy_potion");
                break;
            case 3:
                if (PlayerProgress.Health >= PlayerProgress.MaxHealth) { Respond("full_health"); break; }
                if (!PlayerProgress.TrySpend(healPrice)) { Respond("not_enough"); break; }
                PlayerProgress.Heal(healAmount);
                StartCoroutine(CastFlash(new Color(0.4f, 1f, 0.4f)));
                Respond("heal");
                break;
        }
        RefreshShop();
    }

    private void BuySkill(Skill skill, int price, string thanksKey)
    {
        if (PlayerProgress.Has(skill)) { Respond("owned"); return; }
        if (!PlayerProgress.TrySpend(price)) { Respond("not_enough"); return; }
        PlayerProgress.Unlock(skill);
        StartCoroutine(CastFlash(new Color(0.5f, 0.85f, 1f)));
        Respond(thanksKey);
    }

    private void Respond(string key)
    {
        StartTyping(shopSay, Lines[key]);
        PlayVoice(key);
    }

    private IEnumerator CastFlash(Color tint)
    {
        if (castSprite != null) sr.sprite = castSprite;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            sr.color = Color.Lerp(tint, Color.white, t / 0.6f);
            yield return null;
        }
        sr.color = Color.white;
        sr.sprite = idleSprite;
    }

    private void RefreshShop()
    {
        if (shopCoins != null)
            shopCoins.text = "Coins  <color=#FFD54A>" + PlayerProgress.Coins + "</color>      HP  " +
                             PlayerProgress.Health + " / " + PlayerProgress.MaxHealth +
                             "      Potions  " + PlayerProgress.Potions + " / " + PlayerProgress.MaxPotions;
        if (ui == null || ui.rows == null) return;

        SetRow(0, "Lightning Bolt", "Key C. A bolt that flies through walls and bursts on the first enemy.",
               lightningPrice, PlayerProgress.Has(Skill.LightningBolt), null);
        SetRow(1, "Air Slash", "Key J. A flying blade that hits the first thing in its path. 3s cooldown.",
               airSlashPrice, PlayerProgress.Has(Skill.AirSlash), null);
        SetRow(2, "Health Potion", "Key Q in a dungeon: +" + potionHealAmount + " HP. Carry up to " + PlayerProgress.MaxPotions + ".",
               potionPrice, false, PlayerProgress.Potions >= PlayerProgress.MaxPotions ? "BAG FULL" : null);
        SetRow(3, "Healing Draught", "Drink it here: +" + healAmount + " HP right now.",
               healPrice, false, PlayerProgress.Health >= PlayerProgress.MaxHealth ? "FULL HP" : null);
    }

    private void SetRow(int i, string title, string desc, int price, bool owned, string blockedText)
    {
        if (i >= ui.rows.Length || ui.rows[i] == null) return;
        bool afford = PlayerProgress.Coins >= price;
        string status;
        bool highlight = false;
        if (owned) status = "<color=#7CFC7C>LEARNED</color>";
        else if (blockedText != null) status = "<color=#AAAAAA>" + blockedText + "</color>";
        else if (afford) { status = "<color=#FFD54A>BUY</color>"; highlight = true; }
        else status = "<color=#FF6B6B>NEED " + (price - PlayerProgress.Coins) + "</color>";
        ui.rows[i].Set(title, desc, price, status, highlight);
    }

    // ============================================================
    // Freeze the hub player while talking
    // ============================================================
    private void FreezePlayer(bool freeze)
    {
        if (!FindPlayer()) return;
        if (freeze)
        {
            frozen.Clear();
            foreach (MonoBehaviour mb in player.GetComponents<MonoBehaviour>())
            {
                if (mb == null || !mb.enabled) continue;
                if (mb is BasicMovement || mb is PlayerMovement)
                {
                    mb.enabled = false;
                    frozen.Add(mb);
                }
            }
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            Animator a = player.GetComponent<Animator>();
            if (a != null)
            {
                foreach (AnimatorControllerParameter prm in a.parameters)
                    if (prm.type == AnimatorControllerParameterType.Bool) a.SetBool(prm.name, false);
            }
        }
        else
        {
            foreach (Behaviour b in frozen) if (b != null) b.enabled = true;
            frozen.Clear();
        }
    }

    // ============================================================
    // UI: instance of the GuideUI prefab (edit it in the Editor)
    // ============================================================
    private void BuildUI()
    {
        GuideUIView prefab = uiPrefab;
        if (prefab == null)
        {
            GameObject go = Resources.Load<GameObject>("UI/GuideUI");
            if (go != null) prefab = go.GetComponent<GuideUIView>();
        }
        if (prefab == null)
        {
            Debug.LogError("GuideUI prefab missing. Run  Tools > Bootcamp > Build UI  in the Unity menu.");
            enabled = false;
            return;
        }

        ui = Instantiate(prefab);
        ui.name = "GuideUI";
        prompt = ui.prompt;
        dialogBox = ui.dialogBox;
        shopBox = ui.shopBox;
        dialogText = ui.dialogText;
        dialogHint = ui.dialogHint;
        shopSay = ui.shopSay;
        shopCoins = ui.shopCoins;

        if (ui.dialogName != null) ui.dialogName.text = guideName;
        if (ui.shopName != null) ui.shopName.text = guideName;
        if (ui.dialogPortrait != null && idleSprite != null) ui.dialogPortrait.sprite = idleSprite;
        if (ui.shopPortrait != null && idleSprite != null) ui.shopPortrait.sprite = idleSprite;

        prompt.gameObject.SetActive(false);
        dialogBox.SetActive(false);
        shopBox.SetActive(false);
    }

    private void OnDisable()
    {
        if (panelOpen) FreezePlayer(false);
    }

    private void OnDestroy()
    {
        if (ui != null) Destroy(ui.gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position, new Vector3(worldHeight * 0.6f, worldHeight, 0f));
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, talkRadius);
    }
}

/// <summary>
/// Arrow that circles the player and points to where to go next:
/// back to the Guide when you can afford something, otherwise the next locked-in gate.
/// Created automatically by GuideNPC.
/// </summary>
public class GuideArrow : MonoBehaviour
{
    public GuideNPC guide;
    public float orbitRadius = 1.8f;
    public float hideWithin = 3f;
    public Color gateColor = new Color(1f, 0.82f, 0.25f);
    public Color guideColor = new Color(0.45f, 0.85f, 1f);

    private SpriteRenderer arrow;
    private readonly List<LevelTrigger> gates = new List<LevelTrigger>();

    private void Start()
    {
        GameObject go = new GameObject("GuideArrow (auto)");
        arrow = go.AddComponent<SpriteRenderer>();
        arrow.sprite = MakeArrowSprite();
        arrow.sortingOrder = 50;
        go.transform.localScale = Vector3.one * 0.9f;
        arrow.enabled = false;

        foreach (LevelTrigger t in FindObjectsByType<LevelTrigger>(FindObjectsSortMode.None))
            if (t.levelNumber >= 1 && t.levelNumber <= 3) gates.Add(t);
    }

    private void LateUpdate()
    {
        if (arrow == null) return;
        Transform target = PickTarget(out Color color);
        Transform p = guide != null ? guide.PlayerTransform : null;

        if (target == null || p == null || guide.PanelOpen || !PlayerProgress.GuideIntroDone)
        {
            arrow.enabled = false;
            return;
        }

        Vector3 from = guide.PlayerCenter();
        Vector3 to = target.position;
        Vector2 dir = to - from;
        if (dir.magnitude < hideWithin) { arrow.enabled = false; return; }
        dir.Normalize();

        arrow.enabled = true;
        arrow.color = color;
        float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.12f;
        arrow.transform.position = from + (Vector3)(dir * orbitRadius * pulse);
        arrow.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private Transform PickTarget(out Color color)
    {
        color = gateColor;
        if (guide != null && guide.CanAffordSomething())
        {
            color = guideColor;
            return guide.transform;
        }

        int next = 1;
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            if (gm.isLevel1Complete) next = 2;
            if (gm.isLevel1Complete && gm.isLevel2Complete) next = 3;
            if (gm.isLevel1Complete && gm.isLevel2Complete && gm.isLevel3Complete) return null;
        }
        foreach (LevelTrigger t in gates) if (t != null && t.levelNumber == next) return t.transform;
        return null;
    }

    private void OnDestroy()
    {
        if (arrow != null) Destroy(arrow.gameObject);
    }

    /// <summary>Simple pixel arrow pointing right (1 unit long).</summary>
    private static Sprite MakeArrowSprite()
    {
        const int W = 16, H = 16;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0), white = Color.white, edge = new Color(0.1f, 0.1f, 0.1f, 1f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int dy = Mathf.Abs(y - 7);
                bool head = x >= 8 && dy <= (15 - x);           // triangle tip
                bool shaft = x < 9 && x >= 1 && dy <= 2;          // tail
                bool inside = head || shaft;
                bool headIn = x >= 9 && dy <= (14 - x);
                bool shaftIn = x < 9 && x >= 2 && dy <= 1;
                tex.SetPixel(x, y, inside ? ((headIn || shaftIn) ? white : edge) : clear);
            }
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16f);
    }
}
