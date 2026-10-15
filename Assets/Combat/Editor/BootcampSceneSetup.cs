using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > Bootcamp > Setup Collectibles + Ending
///  - Melee:  Elite that drops the Key   -> skull key sprite
///            Elite that drops a Crystal -> purple crystal
///  - Ranged: the last Ghost drops the (blue) Crystal; no other enemy does
///  - Trap:   green Crystal placed just before the exit; the exit needs it and loads CoordiScene
///  - CoordiScene: full-screen "Work in Progress" image + clickable QUIT button on the drawn one
///  - CoordiScene added to the Build Profile scene list
/// Safe to run again: it updates what is there and never duplicates objects.
/// </summary>
public static class BootcampSceneSetup
{
    private const string Collect = "Assets/Combat/Art/Collectibles/";
    private const string Ending = "Assets/Combat/Art/Ending/";
    private const string Melee = "Assets/Scenes/MelleDungeon.unity";
    private const string Ranged = "Assets/Scenes/rangeddungeon.unity";
    private const string Trap = "Assets/Scenes/TrapDungeon.unity";
    private const string Coordi = "Assets/Scenes/CoordiScene.unity";

    // QUIT button position inside the 1536x1024 screen image (normalised, origin bottom-left)
    private static readonly Vector2 QuitMin = new Vector2(446f / 1536f, 1f - 982f / 1024f);
    private static readonly Vector2 QuitMax = new Vector2(1072f / 1536f, 1f - 862f / 1024f);

    [MenuItem("Tools/Bootcamp/Setup Collectibles + Ending")]
    private static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ImportSprites(Collect, 1024);
        ImportSprites(Ending, 2048);
        Sprite key = Load(Collect + "key_skull.png");
        Sprite purple = Load(Collect + "crystal_purple.png");
        Sprite blue = Load(Collect + "crystal_blue.png");
        Sprite green = Load(Collect + "crystal_green.png");
        Sprite screen = Load(Ending + "coordi_screen.png");
        Sprite quit = Load(Ending + "quit_button.png");

        List<string> log = new List<string>();
        WithScene(Melee, sc => SetupMelee(sc, key, purple, log));
        WithScene(Ranged, sc => SetupRanged(sc, blue, log));
        WithScene(Trap, sc => SetupTrap(sc, green, log));
        WithScene(Coordi, sc => SetupCoordi(sc, screen, quit, log));
        AddToBuild(Coordi, log);

        EditorUtility.DisplayDialog("Bootcamp setup", string.Join("\n", log), "OK");
    }

    // ---------------------------------------------------------------- Melee
    private static void SetupMelee(Scene sc, Sprite key, Sprite crystal, List<string> log)
    {
        int n = 0;
        foreach (Elite e in Find<Elite>(sc))
        {
            SerializedObject so = new SerializedObject(e);
            SerializedProperty type = so.FindProperty("dropType");
            SerializedProperty sprite = so.FindProperty("dropSprite");
            if (type.intValue == (int)CollectibleType.Key) sprite.objectReferenceValue = key;
            else if (type.intValue == (int)CollectibleType.Crystal) sprite.objectReferenceValue = crystal;
            else continue;
            so.ApplyModifiedProperties();
            n++;
        }
        log.Add("Melee: set key / crystal sprite on " + n + " Elite(s).");
    }

    // ---------------------------------------------------------------- Ranged
    private static void SetupRanged(Scene sc, Sprite crystal, List<string> log)
    {
        List<MobChase> mobs = Find<MobChase>(sc);
        MobChase lastGhost = mobs.Where(m => m.name.ToLower().Contains("ghost"))
                                 .OrderByDescending(m => m.transform.position.x)
                                 .FirstOrDefault();
        foreach (MobChase m in mobs)
        {
            SerializedObject so = new SerializedObject(m);
            bool isIt = m == lastGhost;
            so.FindProperty("dropsCrystal").boolValue = isIt;
            if (isIt) so.FindProperty("crystalSprite").objectReferenceValue = crystal;
            so.ApplyModifiedProperties();
        }
        log.Add(lastGhost != null
            ? "Ranged: '" + lastGhost.name + "' (the last ghost) now drops the crystal; other enemies don't."
            : "Ranged: no Ghost found!");
    }

    // ---------------------------------------------------------------- Trap
    private static void SetupTrap(Scene sc, Sprite crystal, List<string> log)
    {
        LevelTrigger exit = Find<LevelTrigger>(sc).FirstOrDefault(t => t.levelNumber == 6);
        if (exit == null) { log.Add("Trap: exit trigger (level 6) not found!"); return; }

        SerializedObject so = new SerializedObject(exit);
        so.FindProperty("sceneToLoad").stringValue = "CoordiScene";
        so.FindProperty("requiredItem").intValue = (int)CollectibleType.Crystal;
        so.FindProperty("lockedMessage").stringValue = "You need the Crystal to escape!";
        so.ApplyModifiedProperties();

        const string pickupName = "Crystal Pickup (Exit)";
        GameObject go = sc.GetRootGameObjects().FirstOrDefault(g => g.name == pickupName);
        if (go == null)
        {
            go = new GameObject(pickupName);
            SceneManager.MoveGameObjectToScene(go, sc);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<CircleCollider2D>();
            go.AddComponent<Collectible>();
        }

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = crystal;
        sr.sortingOrder = 20;
        float h = crystal != null ? crystal.bounds.size.y : 1f;
        float scale = 1.2f / Mathf.Max(0.01f, h);          // about 1.2 units tall
        go.transform.localScale = new Vector3(scale, scale, 1f);
        CircleCollider2D cc = go.GetComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = h * 0.45f;
        Collectible c = go.GetComponent<Collectible>();
        c.type = CollectibleType.Crystal;

        // just in front of the exit, standing on the ground
        Bounds b = exit.GetComponent<Collider2D>().bounds;
        float x = b.min.x - 2f;
        float y = b.min.y + 0.9f;
        Physics2D.SyncTransforms();
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(new Vector2(x, b.max.y), Vector2.down, 30f))
        {
            if (hit.collider.isTrigger || hit.collider.gameObject == go) continue;
            if (hit.collider.gameObject.scene != sc) continue;
            y = hit.point.y + 0.9f;
            break;
        }
        go.transform.position = new Vector3(x, y, 0f);
        EditorUtility.SetDirty(go);
        log.Add("Trap: green crystal placed before the exit; exit needs it and loads CoordiScene.");
    }

    // ---------------------------------------------------------------- CoordiScene
    private static void SetupCoordi(Scene sc, Sprite screen, Sprite quit, List<string> log)
    {
        foreach (Camera cam in Find<Camera>(sc))
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        if (Find<EndScreen>(sc).Count > 0) { log.Add("CoordiScene: end screen already there."); return; }

        GameObject canvasGo = new GameObject("EndScreen", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(canvasGo, sc);
        canvasGo.layer = 5;
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        EndScreen end = canvasGo.AddComponent<EndScreen>();

        Image bg = UI<Image>("Background", canvasGo.transform);
        bg.color = Color.black;
        Stretch(bg.rectTransform);

        // the picture keeps its 3:2 shape and fits the screen
        Image pic = UI<Image>("Panel", canvasGo.transform);
        pic.sprite = screen;
        pic.raycastTarget = false;
        Stretch(pic.rectTransform);
        AspectRatioFitter fit = pic.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1536f / 1024f;

        // QUIT button sits exactly on the one drawn in the picture
        Image qi = UI<Image>("QuitButton", pic.transform);
        qi.sprite = quit;
        RectTransform qr = qi.rectTransform;
        qr.anchorMin = QuitMin; qr.anchorMax = QuitMax;
        qr.offsetMin = qr.offsetMax = Vector2.zero;
        Button btn = qi.gameObject.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.white;
        cb.selectedColor = Color.white;
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = cb;
        UnityEventTools.AddPersistentListener(btn.onClick, end.Quit);
        qi.gameObject.AddComponent<ButtonHover>();

        // clicks need an EventSystem
        if (Find<EventSystem>(sc).Count == 0)
        {
            GameObject es = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(es, sc);
            es.AddComponent<EventSystem>();
            System.Type inputSystemModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null) es.AddComponent(inputSystemModule);
            else es.AddComponent<StandaloneInputModule>();
        }
        log.Add("CoordiScene: picture panel + QUIT button added.");
    }

    // ---------------------------------------------------------------- helpers
    private static void WithScene(string path, System.Action<Scene> work)
    {
        if (!File.Exists(path)) { Debug.LogWarning("Scene not found: " + path); return; }
        Scene sc = SceneManager.GetSceneByPath(path);
        bool wasOpen = sc.IsValid() && sc.isLoaded;
        if (!wasOpen) sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        work(sc);
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
        if (!wasOpen) EditorSceneManager.CloseScene(sc, true);
    }

    private static List<T> Find<T>(Scene sc) where T : Component
    {
        List<T> list = new List<T>();
        foreach (GameObject root in sc.GetRootGameObjects())
            list.AddRange(root.GetComponentsInChildren<T>(true));
        return list;
    }

    private static void ImportSprites(string dir, int maxSize)
    {
        if (!Directory.Exists(dir)) return;
        foreach (string f in Directory.GetFiles(dir, "*.png"))
        {
            string path = f.Replace('\\', '/');
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = maxSize;
            ti.spritePixelsPerUnit = 100;
            ti.SaveAndReimport();
        }
    }

    private static Sprite Load(string path) { return AssetDatabase.LoadAssetAtPath<Sprite>(path); }

    private static T UI<T>(string name, Transform parent) where T : Component
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void AddToBuild(string path, List<string> log)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        log.Add("Added CoordiScene to the build scene list.");
    }
}
