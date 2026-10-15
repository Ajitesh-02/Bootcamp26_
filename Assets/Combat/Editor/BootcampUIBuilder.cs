using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tools > Bootcamp > Build UI
/// Creates the game's UI as real, editable assets:
///   Assets/Combat/Resources/UI/GameHUD.prefab     (health, potions, items, coins, messages)
///   Assets/Combat/Resources/UI/GuideUI.prefab     ([E] prompt, dialogue box, shop)
///   Assets/Combat/Resources/UI/LevelIntro.prefab  (level instruction panel)
/// and puts a LevelIntro panel with the Melee instructions into MelleDungeon.
/// After that, edit the prefabs/canvas like any other UI - the build only creates what is missing
/// (use "Rebuild UI" to overwrite the prefabs with fresh copies).
/// </summary>
public static class BootcampUIBuilder
{
    private const string ArtDir = "Assets/Combat/UI/Art/";
    private const string FontDir = "Assets/Combat/UI/Fonts/";
    private const string PrefabDir = "Assets/Combat/Resources/UI";
    private const string MeleeScene = "Assets/Scenes/MelleDungeon.unity";

    private const float PixelScale = 1.5f;   // Image.pixelsPerUnitMultiplier for the 16px-per-unit pixel art

    private static readonly Color Gold = new Color32(0xF2, 0xC9, 0x5C, 0xFF);
    private static readonly Color Cream = new Color32(0xE9, 0xDF, 0xC9, 0xFF);
    private static readonly Color Muted = new Color32(0xB9, 0xAC, 0x96, 0xFF);
    private static readonly Color KeyInk = new Color32(0x2B, 0x1D, 0x14, 0xFF);

    private static Sprite panel, slot, slotGold, keycap, barFill, coin, potion, lightning, airslash, heart;
    private static TMP_FontAsset bodyFont, titleFont;
    private static Material bodyOutline, titleOutline;

    [MenuItem("Tools/Bootcamp/Build UI")]
    private static void BuildMissing() { Build(false); }

    [MenuItem("Tools/Bootcamp/Rebuild UI (overwrite prefabs)")]
    private static void Rebuild()
    {
        if (EditorUtility.DisplayDialog("Rebuild UI",
            "This replaces GameHUD, GuideUI and LevelIntro prefabs with fresh copies.\nAny edits you made to those prefabs will be lost.",
            "Rebuild", "Cancel"))
            Build(true);
    }

    private static void Build(bool overwrite)
    {
        ImportArt();
        bodyFont = MakeFont("PixelifySans", out bodyOutline);
        titleFont = MakeFont("Silkscreen-Regular", out titleOutline);
        if (bodyFont == null || titleFont == null)
        {
            EditorUtility.DisplayDialog("Bootcamp UI", "Could not create the font assets. Check the Console.", "OK");
            return;
        }

        Directory.CreateDirectory(PrefabDir);
        List<string> made = new List<string>();
        if (MakePrefab("GameHUD", BuildHUD, overwrite)) made.Add("GameHUD");
        if (MakePrefab("GuideUI", BuildGuideUI, overwrite)) made.Add("GuideUI");
        if (MakePrefab("LevelIntro", BuildLevelIntro, overwrite)) made.Add("LevelIntro");
        AssetDatabase.SaveAssets();

        string scene = PlaceIntroInMelee() ? "\nAdded the Melee instructions panel to MelleDungeon." : "";
        EditorUtility.DisplayDialog("Bootcamp UI",
            (made.Count > 0 ? "Created: " + string.Join(", ", made) : "Prefabs already existed (nothing overwritten).") + scene +
            "\n\nPrefabs are in Assets/Combat/Resources/UI. Open them to change the look.", "OK");
    }

    // =====================================================================
    // Art + fonts
    // =====================================================================
    private static void ImportArt()
    {
        var borders = new Dictionary<string, Vector4>
        {
            { "ui_panel", new Vector4(7, 7, 7, 7) },
            { "ui_slot", new Vector4(3, 3, 3, 3) },
            { "ui_slot_gold", new Vector4(3, 3, 3, 3) },
            { "ui_keycap", new Vector4(4, 5, 4, 4) },
        };
        foreach (string file in Directory.GetFiles(ArtDir, "*.png"))
        {
            string path = file.Replace('\\', '/');
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            string n = Path.GetFileNameWithoutExtension(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.spriteBorder = borders.TryGetValue(n, out Vector4 b) ? b : Vector4.zero;
            ti.SaveAndReimport();
        }
        panel = LoadSprite("ui_panel"); slot = LoadSprite("ui_slot"); slotGold = LoadSprite("ui_slot_gold");
        keycap = LoadSprite("ui_keycap"); barFill = LoadSprite("ui_bar_fill"); coin = LoadSprite("icon_coin");
        potion = LoadSprite("icon_potion"); lightning = LoadSprite("icon_lightning"); airslash = LoadSprite("icon_airslash");
        heart = LoadSprite("icon_heart");
    }

    private static Sprite LoadSprite(string n) { return AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + n + ".png"); }

    private static TMP_FontAsset MakeFont(string fileName, out Material outlineMat)
    {
        string assetPath = FontDir + fileName + " SDF.asset";
        string outlinePath = FontDir + fileName + " SDF Outline.mat";
        TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

        if (fa == null)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontDir + fileName + ".ttf");
            if (font == null) { Debug.LogError("Font not found: " + FontDir + fileName + ".ttf"); outlineMat = null; return null; }

            fa = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = fileName + " SDF";
            AssetDatabase.CreateAsset(fa, assetPath);
            fa.atlasTextures[0].name = fileName + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = fileName + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            if (TMP_Settings.defaultFontAsset != null)
                fa.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
        }

        outlineMat = AssetDatabase.LoadAssetAtPath<Material>(outlinePath);
        if (outlineMat == null)
        {
            outlineMat = new Material(fa.material) { name = fileName + " SDF Outline" };
            outlineMat.EnableKeyword("OUTLINE_ON");
            outlineMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            outlineMat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            outlineMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            AssetDatabase.CreateAsset(outlineMat, outlinePath);
        }
        return fa;
    }

    // =====================================================================
    // Prefabs
    // =====================================================================
    private static bool MakePrefab(string name, System.Func<GameObject> build, bool overwrite)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return false;
        GameObject go = build();
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return true;
    }

    // ---------------- HUD ----------------
    private static GameObject BuildHUD()
    {
        GameObject root = NewCanvas("GameHUD", -10);
        Transform r = root.transform;

        // Health panel (top-left)
        Image hp = Img("HealthPanel", r, panel, Color.white, true);
        Place(hp.rectTransform, new Vector2(0, 1), new Vector2(24, -24), new Vector2(480, 96));
        TextMeshProUGUI hpLabel = Txt("HPLabel", hp.transform, "HP", titleFont, 34, Gold, TextAlignmentOptions.MidlineLeft);
        PlaceMid(hpLabel.rectTransform, 28, new Vector2(80, 50));
        Image barBg = Img("BarBackground", hp.transform, slot, Color.white, true);
        PlaceMid(barBg.rectTransform, 100, new Vector2(352, 48));
        Image fill = Img("Fill", barBg.transform, barFill, Color.white, false);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        Stretch(fill.rectTransform, 6);
        TextMeshProUGUI hpText = Txt("HealthText", barBg.transform, "100 / 100", bodyFont, 30, Color.white, TextAlignmentOptions.Center, true);
        Stretch(hpText.rectTransform, 0);
        HealthBar bar = barBg.gameObject.AddComponent<HealthBar>();
        bar.healthFillImage = fill;
        bar.healthText = hpText;

        // Potions (under health)
        Image pot = Img("PotionPanel", r, slot, Color.white, true);
        Place(pot.rectTransform, new Vector2(0, 1), new Vector2(24, -128), new Vector2(170, 70));
        Image potIcon = Img("PotionIcon", pot.transform, potion, Color.white, false);
        potIcon.preserveAspect = true;
        PlaceMid(potIcon.rectTransform, 14, new Vector2(48, 48));
        TextMeshProUGUI potText = Txt("PotionText", pot.transform, "x 0", bodyFont, 32, Color.white, TextAlignmentOptions.MidlineLeft, true);
        PlaceMid(potText.rectTransform, 70, new Vector2(90, 50));
        TextMeshProUGUI qHint = Txt("KeyHint", pot.transform, "Q", titleFont, 16, Muted, TextAlignmentOptions.TopRight);
        Place(qHint.rectTransform, new Vector2(1, 1), new Vector2(-10, -6), new Vector2(30, 22));

        // Collected items row (Key, Crystal)
        RectTransform row = UI("CollectedItems", r).GetComponent<RectTransform>();
        Place(row, new Vector2(0, 1), new Vector2(206, -128), new Vector2(600, 90));
        HorizontalLayoutGroup hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 12; hl.childAlignment = TextAnchor.UpperLeft;
        hl.childControlWidth = hl.childControlHeight = false;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        Image item = Img("ItemTemplate", row, slotGold, Color.white, true);
        item.rectTransform.sizeDelta = new Vector2(70, 70);
        Image itemIcon = Img("Icon", item.transform, null, Color.white, false);
        itemIcon.preserveAspect = true;
        Stretch(itemIcon.rectTransform, 12);
        TextMeshProUGUI itemLabel = Txt("Label", item.transform, "KEY", titleFont, 16, Gold, TextAlignmentOptions.Top, true);
        itemLabel.rectTransform.anchorMin = new Vector2(0, 0); itemLabel.rectTransform.anchorMax = new Vector2(1, 0);
        itemLabel.rectTransform.pivot = new Vector2(0.5f, 1); itemLabel.rectTransform.anchoredPosition = new Vector2(0, -2);
        itemLabel.rectTransform.sizeDelta = new Vector2(30, 22);
        item.gameObject.SetActive(false);

        // Coins (top-right)
        Image cp = Img("CoinPanel", r, panel, Color.white, true);
        Place(cp.rectTransform, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(250, 96));
        Image ci = Img("CoinIcon", cp.transform, coin, Color.white, false);
        ci.preserveAspect = true;
        PlaceMid(ci.rectTransform, 26, new Vector2(52, 52));
        TextMeshProUGUI ct = Txt("CoinText", cp.transform, "0", bodyFont, 44, Gold, TextAlignmentOptions.MidlineRight, true);
        ct.rectTransform.anchorMin = new Vector2(0, 0); ct.rectTransform.anchorMax = new Vector2(1, 1);
        ct.rectTransform.offsetMin = new Vector2(90, 0); ct.rectTransform.offsetMax = new Vector2(-30, 0);

        // Message (top-centre, panel grows with the text)
        Image mp = Img("MessagePanel", r, panel, Color.white, true);
        mp.rectTransform.anchorMin = mp.rectTransform.anchorMax = new Vector2(0.5f, 1);
        mp.rectTransform.pivot = new Vector2(0.5f, 1);
        mp.rectTransform.anchoredPosition = new Vector2(0, -150);
        mp.rectTransform.sizeDelta = new Vector2(1100, 100);
        VerticalLayoutGroup vl = mp.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(44, 44, 26, 30);
        vl.childControlWidth = vl.childControlHeight = true;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
        ContentSizeFitter fit = mp.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        CanvasGroup mg = mp.gameObject.AddComponent<CanvasGroup>();
        mg.blocksRaycasts = false; mg.interactable = false;
        TextMeshProUGUI mt = Txt("MessageText", mp.transform, "Message", bodyFont, 34, Cream, TextAlignmentOptions.Center);
        mt.textWrappingMode = TextWrappingModes.Normal;

        GameHUD hud = root.AddComponent<GameHUD>();
        hud.healthBar = bar;
        hud.coinText = ct;
        hud.coinIcon = ci.rectTransform;
        hud.potionGroup = pot.gameObject;
        hud.potionText = potText;
        hud.inventoryRow = row;
        hud.inventoryItemTemplate = item.gameObject;
        hud.messageGroup = mg;
        hud.messageText = mt;
        hud.coinSprite = coin;
        return root;
    }

    // ---------------- Guide (prompt, dialogue, shop) ----------------
    private static GameObject BuildGuideUI()
    {
        GameObject root = NewCanvas("GuideUI", 20);
        Transform r = root.transform;
        GuideUIView view = root.AddComponent<GuideUIView>();

        // [E] Talk prompt
        Image pr = Img("TalkPrompt", r, slotGold, Color.white, true);
        pr.rectTransform.anchorMin = pr.rectTransform.anchorMax = Vector2.zero;
        pr.rectTransform.pivot = new Vector2(0.5f, 0f);
        pr.rectTransform.sizeDelta = new Vector2(196, 76);
        HorizontalLayoutGroup ph = Row(pr.gameObject, 12, TextAnchor.MiddleCenter);
        ph.padding = new RectOffset(14, 18, 10, 10);
        KeyCap(pr.transform, "E", 52);
        Label(pr.transform, "Talk", 32, Cream);
        view.prompt = pr.rectTransform;

        // Dialogue box
        Image db = Img("DialogueBox", r, panel, Color.white, true);
        Bottom(db.rectTransform, new Vector2(1500, 340));
        view.dialogPortrait = Portrait(db.transform);
        view.dialogName = NameText(db.transform);
        Divider(db.transform, -88);
        TextMeshProUGUI line = Txt("Line", db.transform, "...", bodyFont, 36, Cream, TextAlignmentOptions.TopLeft);
        line.textWrappingMode = TextWrappingModes.Normal;
        line.lineSpacing = 4;
        Place(line.rectTransform, new Vector2(0, 1), new Vector2(330, -104), new Vector2(1130, 170));
        view.dialogText = line;
        RectTransform dh = HintRow(db.transform, new[] { "E" }, "Next", out TextMeshProUGUI hintLabel);
        Place(dh, new Vector2(1, 0), new Vector2(-34, 28), new Vector2(220, 52));
        view.dialogHint = hintLabel;
        view.dialogBox = db.gameObject;

        // Shop box
        Image sb = Img("ShopBox", r, panel, Color.white, true);
        Bottom(sb.rectTransform, new Vector2(1500, 760));
        view.shopPortrait = Portrait(sb.transform);
        view.shopName = NameText(sb.transform);
        Divider(sb.transform, -88);
        TextMeshProUGUI say = Txt("Say", sb.transform, "...", bodyFont, 32, Cream, TextAlignmentOptions.TopLeft);
        say.textWrappingMode = TextWrappingModes.Normal;
        Place(say.rectTransform, new Vector2(0, 1), new Vector2(330, -104), new Vector2(1130, 100));
        view.shopSay = say;
        TextMeshProUGUI coins = Txt("Stats", sb.transform, "Coins 0", bodyFont, 26, Muted, TextAlignmentOptions.MidlineLeft);
        Place(coins.rectTransform, new Vector2(0, 1), new Vector2(330, -208), new Vector2(1130, 36));
        view.shopCoins = coins;

        RectTransform list = UI("Items", sb.transform).GetComponent<RectTransform>();
        Place(list, new Vector2(0, 1), new Vector2(330, -256), new Vector2(1130, 4 * 92 + 3 * 10));
        VerticalLayoutGroup lv = list.gameObject.AddComponent<VerticalLayoutGroup>();
        lv.spacing = 10;
        lv.childControlWidth = lv.childControlHeight = false;
        lv.childForceExpandWidth = lv.childForceExpandHeight = false;

        Sprite[] icons = { lightning, airslash, potion, heart };
        string[] titles = { "Lightning Bolt", "Air Slash", "Health Potion", "Healing Draught" };
        view.rows = new ShopRowView[4];
        for (int i = 0; i < 4; i++) view.rows[i] = ShopRow(list, i, icons[i], titles[i]);

        RectTransform sf = HintRow(sb.transform, new[] { "1", "2", "3", "4" }, "Buy", out _);
        Place(sf, new Vector2(0, 0), new Vector2(330, 28), new Vector2(420, 52));
        RectTransform sl = HintRow(sb.transform, new[] { "E" }, "Leave", out _);
        Place(sl, new Vector2(1, 0), new Vector2(-34, 28), new Vector2(220, 52));
        view.shopBox = sb.gameObject;

        pr.gameObject.SetActive(false);
        db.gameObject.SetActive(false);
        sb.gameObject.SetActive(false);
        return root;
    }

    private static ShopRowView ShopRow(Transform parent, int index, Sprite icon, string title)
    {
        Image bg = Img("Item" + (index + 1), parent, slot, Color.white, true);
        bg.rectTransform.sizeDelta = new Vector2(1130, 92);
        ShopRowView v = bg.gameObject.AddComponent<ShopRowView>();
        v.background = bg;
        v.normalBackground = slot;
        v.highlightBackground = slotGold;

        RectTransform key = KeyCap(bg.transform, (index + 1).ToString(), 56);
        PlaceMid(key, 18, new Vector2(56, 56));

        Image ic = Img("Icon", bg.transform, icon, Color.white, false);
        ic.preserveAspect = true;
        PlaceMid(ic.rectTransform, 92, new Vector2(52, 52));
        v.icon = ic;

        v.title = Txt("Title", bg.transform, title, bodyFont, 30, Cream, TextAlignmentOptions.TopLeft);
        Place(v.title.rectTransform, new Vector2(0, 1), new Vector2(162, -12), new Vector2(620, 38));
        v.description = Txt("Description", bg.transform, "", bodyFont, 21, Muted, TextAlignmentOptions.TopLeft);
        v.description.textWrappingMode = TextWrappingModes.NoWrap;
        v.description.overflowMode = TextOverflowModes.Ellipsis;
        Place(v.description.rectTransform, new Vector2(0, 1), new Vector2(162, -52), new Vector2(660, 30));

        Image c = Img("CoinIcon", bg.transform, coin, Color.white, false);
        c.preserveAspect = true;
        Place(c.rectTransform, new Vector2(1, 0.5f), new Vector2(-318, 0), new Vector2(34, 34), new Vector2(1, 0.5f));
        v.price = Txt("Price", bg.transform, "0", bodyFont, 34, Gold, TextAlignmentOptions.MidlineLeft);
        Place(v.price.rectTransform, new Vector2(1, 0.5f), new Vector2(-274, 0), new Vector2(70, 44), new Vector2(0, 0.5f));
        v.status = Txt("Status", bg.transform, "BUY", titleFont, 22, Gold, TextAlignmentOptions.MidlineRight);
        Place(v.status.rectTransform, new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(190, 40), new Vector2(1, 0.5f));
        return v;
    }

    private static Image Portrait(Transform parent)
    {
        Image frame = Img("PortraitFrame", parent, slotGold, Color.white, true);
        Place(frame.rectTransform, new Vector2(0, 1), new Vector2(36, -36), new Vector2(266, 266));
        Image bg = Img("PortraitBackground", frame.transform, null, new Color32(0x2A, 0x20, 0x35, 0xFF), false);
        Stretch(bg.rectTransform, 9);
        Image p = Img("Portrait", frame.transform, null, Color.white, false);
        p.preserveAspect = true;
        Stretch(p.rectTransform, 22);
        return p;
    }

    private static TextMeshProUGUI NameText(Transform parent)
    {
        TextMeshProUGUI n = Txt("Name", parent, "Aldric, the Guide", titleFont, 30, Gold, TextAlignmentOptions.MidlineLeft);
        Place(n.rectTransform, new Vector2(0, 1), new Vector2(330, -34), new Vector2(1000, 46));
        return n;
    }

    private static void Divider(Transform parent, float y)
    {
        Image d = Img("Divider", parent, null, new Color(Gold.r, Gold.g, Gold.b, 0.45f), false);
        Place(d.rectTransform, new Vector2(0, 1), new Vector2(330, y), new Vector2(1130, 3));
    }

    // ---------------- Level intro ----------------
    private static GameObject BuildLevelIntro()
    {
        GameObject root = NewCanvas("LevelIntro", 15);
        Transform r = root.transform;
        CanvasGroup g = root.AddComponent<CanvasGroup>();

        Image dim = Img("Dim", r, null, new Color(0, 0, 0, 0.6f), false);
        Stretch(dim.rectTransform, 0);

        Image p = Img("Panel", r, panel, Color.white, true);
        p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        p.rectTransform.sizeDelta = new Vector2(1280, 720);

        TextMeshProUGUI title = Txt("Title", p.transform, "LEVEL TITLE", titleFont, 54, Gold, TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.pivot = new Vector2(0.5f, 1);
        title.rectTransform.anchoredPosition = new Vector2(0, -44);
        title.rectTransform.sizeDelta = new Vector2(-80, 72);

        Image d = Img("Divider", p.transform, null, new Color(Gold.r, Gold.g, Gold.b, 0.45f), false);
        d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 1);
        d.rectTransform.anchoredPosition = new Vector2(0, -132);
        d.rectTransform.sizeDelta = new Vector2(1060, 3);

        TextMeshProUGUI body = Txt("Body", p.transform, "Instructions go here.", bodyFont, 32, Cream, TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.lineSpacing = 6;
        body.rectTransform.anchorMin = Vector2.zero; body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.offsetMin = new Vector2(90, 120);
        body.rectTransform.offsetMax = new Vector2(-90, -160);

        RectTransform hint = HintRow(p.transform, new[] { "ENTER" }, "to begin", out _);
        hint.anchorMin = hint.anchorMax = new Vector2(0.5f, 0);
        hint.pivot = new Vector2(0.5f, 0);
        hint.anchoredPosition = new Vector2(0, 40);
        hint.sizeDelta = new Vector2(360, 56);

        LevelIntro li = root.AddComponent<LevelIntro>();
        li.group = g;
        li.titleText = title;
        li.bodyText = body;
        return root;
    }

    private const string MeleeTitle = "THE MELEE DUNGEON";
    private const string MeleeBody =
        "Cut your way through the guards. Only your blade works for now.\n\n" +
        "<color=#F2C95C>A / D</color>  move      <color=#F2C95C>Space</color>  jump      <color=#F2C95C>H / U / K</color>  strike\n" +
        "<color=#F2C95C>Q</color>  drink a health potion  <color=#B9AC96>(buy them from the Guide)</color>\n\n" +
        "Guards drop <color=#FFD54A>coins</color>. Bring them to the Guide to learn <color=#F2C95C>Lightning</color> and <color=#F2C95C>Air Slash</color>.\n" +
        "Defeat an <color=#F2C95C>Elite</color> to win the <color=#FFD54A>KEY</color> to the door, " +
        "then the other Elite for the <color=#4DE6FF>CRYSTAL</color> that lets you escape.";

    private static bool PlaceIntroInMelee()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/LevelIntro.prefab");
        if (prefab == null || !File.Exists(MeleeScene)) return false;

        Scene sc = SceneManager.GetSceneByPath(MeleeScene);
        bool wasOpen = sc.IsValid() && sc.isLoaded;
        if (!wasOpen)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            sc = EditorSceneManager.OpenScene(MeleeScene, OpenSceneMode.Additive);
        }

        foreach (GameObject go in sc.GetRootGameObjects())
            if (go.GetComponentInChildren<LevelIntro>(true) != null)
            {
                if (!wasOpen) EditorSceneManager.CloseScene(sc, true);
                return false;   // already there
            }

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, sc);
        inst.name = "LevelIntro (Melee)";
        LevelIntro li = inst.GetComponent<LevelIntro>();
        li.titleText.text = MeleeTitle;
        li.bodyText.text = MeleeBody;
        PrefabUtility.RecordPrefabInstancePropertyModifications(li.titleText);
        PrefabUtility.RecordPrefabInstancePropertyModifications(li.bodyText);
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
        if (!wasOpen) EditorSceneManager.CloseScene(sc, true);
        return true;
    }

    // =====================================================================
    // Small builders
    // =====================================================================
    private static GameObject NewCanvas(string name, int order)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        c.pixelPerfect = true;
        CanvasScaler s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight = 0.5f;
        return go;
    }

    private static GameObject UI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Image Img(string name, Transform parent, Sprite sprite, Color color, bool sliced)
    {
        Image i = UI(name, parent).AddComponent<Image>();
        i.sprite = sprite;
        i.color = color;
        i.raycastTarget = false;
        if (sliced) { i.type = Image.Type.Sliced; i.pixelsPerUnitMultiplier = PixelScale; }
        return i;
    }

    private static TextMeshProUGUI Txt(string name, Transform parent, string text, TMP_FontAsset font, float size,
                                       Color color, TextAlignmentOptions align, bool outline = false)
    {
        TextMeshProUGUI t = UI(name, parent).AddComponent<TextMeshProUGUI>();
        t.font = font;
        if (outline) t.fontSharedMaterial = font == bodyFont ? bodyOutline : titleOutline;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.enableAutoSizing = false;
        t.richText = true;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    private static RectTransform KeyCap(Transform parent, string key, float height)
    {
        float width = Mathf.Max(height, 22 + key.Length * 20);
        Image k = Img("Key " + key, parent, keycap, Color.white, true);
        k.rectTransform.sizeDelta = new Vector2(width, height);
        TextMeshProUGUI t = Txt("Letter", k.transform, key, titleFont, key.Length > 1 ? 20 : height * 0.48f, KeyInk, TextAlignmentOptions.Center);
        Stretch(t.rectTransform, 0);
        t.rectTransform.offsetMin = new Vector2(0, 6);   // sits on the key top (the bottom 3px are its side)
        return k.rectTransform;
    }

    private static TextMeshProUGUI Label(Transform parent, string text, float size, Color color)
    {
        TextMeshProUGUI t = Txt("Label", parent, text, bodyFont, size, color, TextAlignmentOptions.MidlineLeft, true);
        ContentSizeFitter f = t.gameObject.AddComponent<ContentSizeFitter>();
        f.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        t.rectTransform.sizeDelta = new Vector2(100, 44);
        return t;
    }

    private static HorizontalLayoutGroup Row(GameObject go, float spacing, TextAnchor align)
    {
        HorizontalLayoutGroup h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.childAlignment = align;
        h.childControlWidth = h.childControlHeight = false;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        return h;
    }

    /// <summary>[key][key] label - used for "E Next", "1 2 3 4 Buy", "ENTER to begin".</summary>
    private static RectTransform HintRow(Transform parent, string[] keys, string label, out TextMeshProUGUI labelText)
    {
        RectTransform rt = UI("Hint", parent).GetComponent<RectTransform>();
        Row(rt.gameObject, 8, TextAnchor.MiddleLeft);
        foreach (string k in keys) KeyCap(rt, k, 46);
        labelText = Label(rt, label, 28, Gold);
        return rt;
    }

    private static void Bottom(RectTransform rt, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 40);
        rt.sizeDelta = size;
    }

    /// <summary>Anchor + pivot at the same corner, offset from it.</summary>
    private static void Place(RectTransform rt, Vector2 corner, Vector2 pos, Vector2 size)
    {
        Place(rt, corner, pos, size, corner);
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    /// <summary>Left edge, vertically centred.</summary>
    private static void PlaceMid(RectTransform rt, float x, Vector2 size)
    {
        Place(rt, new Vector2(0, 0.5f), new Vector2(x, 0), size, new Vector2(0, 0.5f));
    }

    private static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
