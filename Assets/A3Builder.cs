using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu: CS464 > Build Assignment 03 (prefabs + 5 scenes)
// Creates materials, prefabs (Player, Coin, Spikes, Guard, Crusher, BouncePad, PowerUp,
// Checkpoint, Goal, GameManager+HUD) and the scenes Level01..Level05, then fills the Build list.
public static class A3Builder
{
    const string PrefabDir = "Assets/Prefabs";
    const string SceneDir = "Assets/Scenes";
    const string MatDir = "Assets/Materials";

    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Shader baseShader;
    static Transform levelRoot;
    static GameObject pPlayer, pCoin, pSpikes, pGuard, pCrusher, pPad, pPower, pCheck, pGoal, pGM;
    static PhysicsMaterial slippery;
    static int guardCount;

    // ===================================================================== entry
    [MenuItem("CS464/Build Assignment 03 (prefabs + 5 scenes)")]
    public static void BuildAll()
    {
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("TextMeshPro needed",
                "Import TMP Essentials first:\nWindow > TextMeshPro > Import TMP Essential Resources.\nThen run this menu item again.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Folder(PrefabDir); Folder(SceneDir); Folder(MatDir);
        mats.Clear();
        MakeMaterials();
        MakePrefabs();

        var paths = new List<string>
        {
            Level1(), Level2(), Level3(), Level4(), Level5()
        };

        var list = new List<EditorBuildSettingsScene>();
        foreach (var p in paths) list.Add(new EditorBuildSettingsScene(p, true));
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(paths[0]);
        EditorUtility.DisplayDialog("Done", "Created 5 scenes (Level01-Level05) and added them to the Build list.\nPress Play in Level01.", "OK");
    }

    // ===================================================================== helpers
    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Shader BaseShader()
    {
        if (baseShader == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseShader = t.GetComponent<Renderer>().sharedMaterial.shader;
            Object.DestroyImmediate(t);
        }
        return baseShader;
    }

    static void MakeMat(string key, Color c)
    {
        string path = MatDir + "/" + key + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(BaseShader());
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetColor("_Color", c);
        EditorUtility.SetDirty(m);
        mats[key] = m;
    }

    static Material M(string key) { return mats[key]; }

    static void MakeMaterials()
    {
        MakeMat("Ground", new Color(0.30f, 0.55f, 0.35f));
        MakeMat("Dark", new Color(0.22f, 0.25f, 0.28f));
        MakeMat("Stone", new Color(0.45f, 0.47f, 0.55f));
        MakeMat("Wood", new Color(0.55f, 0.38f, 0.22f));
        MakeMat("Red", new Color(0.90f, 0.15f, 0.15f));
        MakeMat("Gold", new Color(1.00f, 0.82f, 0.10f));
        MakeMat("Blue", new Color(0.20f, 0.50f, 1.00f));
        MakeMat("White", Color.white);
        MakeMat("Gray", new Color(0.6f, 0.6f, 0.6f));
        MakeMat("Cyan", new Color(0.10f, 0.85f, 0.90f));
        MakeMat("Yellow", new Color(1.00f, 0.85f, 0.10f));
        MakeMat("Purple", new Color(0.70f, 0.30f, 0.90f));
        MakeMat("GuardBlue", new Color(0.20f, 0.25f, 0.80f));

        string pmPath = MatDir + "/Slippery.asset";
        slippery = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
        if (slippery == null)
        {
            slippery = new PhysicsMaterial("Slippery");
            AssetDatabase.CreateAsset(slippery, pmPath);
        }
        slippery.dynamicFriction = 0f;
        slippery.staticFriction = 0f;
        slippery.bounciness = 0f;
        slippery.frictionCombine = PhysicsMaterialCombine.Minimum;
        slippery.bounceCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(slippery);
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, string mat, bool keepCollider = true)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        if (parent != null) g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = M(mat);
        if (!keepCollider) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static GameObject Save(GameObject root, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    // set fields on prefab instances in a way that is recorded as overrides
    static void SetRef(Object t, string f, Object v) { var so = new SerializedObject(t); so.FindProperty(f).objectReferenceValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetInt(Object t, string f, int v) { var so = new SerializedObject(t); so.FindProperty(f).intValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetFloat(Object t, string f, float v) { var so = new SerializedObject(t); so.FindProperty(f).floatValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetBool(Object t, string f, bool v) { var so = new SerializedObject(t); so.FindProperty(f).boolValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetStr(Object t, string f, string v) { var so = new SerializedObject(t); so.FindProperty(f).stringValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetRefArray(Object t, string f, Object[] vals)
    {
        var so = new SerializedObject(t);
        var arr = so.FindProperty(f);
        arr.arraySize = vals.Length;
        for (int i = 0; i < vals.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = vals[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ===================================================================== prefabs
    static void MakePrefabs()
    {
        pPlayer = MakePlayer();
        pCoin = MakeCoin();
        pSpikes = MakeSpikes();
        pGuard = MakeGuard();
        pCrusher = MakeCrusher();
        pPad = MakePad();
        pPower = MakePower();
        pCheck = MakeCheckpoint();
        pGoal = MakeGoal();
        pGM = MakeGameManager();
    }

    static GameObject MakePlayer()
    {
        var root = new GameObject("Player");
        root.tag = "Player";                         // the built-in tag
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        var cap = root.AddComponent<CapsuleCollider>();
        cap.height = 2f; cap.radius = 0.5f; cap.center = Vector3.zero;
        cap.sharedMaterial = slippery;
        Prim(PrimitiveType.Capsule, "Body", root.transform, Vector3.zero, Vector3.one, "Blue", false);
        Prim(PrimitiveType.Cube, "Nose", root.transform, new Vector3(0f, 0.5f, 0.45f), new Vector3(0.3f, 0.3f, 0.5f), "White", false);
        root.AddComponent<Health>();
        root.AddComponent<PlayerController>();
        return Save(root, "Player");
    }

    static GameObject MakeCoin()
    {
        var root = new GameObject("Coin");
        var sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true; sc.radius = 0.5f;
        var disc = Prim(PrimitiveType.Cylinder, "Disc", root.transform, Vector3.zero, new Vector3(0.8f, 0.06f, 0.8f), "Gold", false);
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        root.AddComponent<Coin>();
        return Save(root, "Coin");
    }

    static GameObject MakeSpikes()
    {
        var root = new GameObject("Spikes");
        var bc = root.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(1.8f, 1.2f, 1.8f); bc.center = new Vector3(0f, 0.6f, 0f);
        Prim(PrimitiveType.Cube, "Base", root.transform, new Vector3(0f, 0.075f, 0f), new Vector3(1.8f, 0.15f, 1.8f), "Dark", false);
        foreach (float x in new[] { -0.45f, 0.45f })
            foreach (float z in new[] { -0.45f, 0.45f })
            {
                var tip = Prim(PrimitiveType.Cube, "Tip", root.transform, new Vector3(x, 0.4f, z), Vector3.one * 0.5f, "Red", false);
                tip.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            }
        var h = root.AddComponent<Hazard>();
        h.damage = 1; h.knockback = 9f; h.knockTime = 0.25f;
        return Save(root, "Spikes");
    }

    static GameObject MakeGuard()
    {
        var root = new GameObject("Guard");
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        var cap = root.AddComponent<CapsuleCollider>();
        cap.isTrigger = true; cap.height = 2.2f; cap.radius = 0.7f; cap.center = new Vector3(0f, 1.1f, 0f);
        var body = Prim(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 1f, 0f), Vector3.one, "GuardBlue", false);
        Prim(PrimitiveType.Cube, "Visor", root.transform, new Vector3(0f, 1.4f, 0.45f), new Vector3(0.7f, 0.25f, 0.3f), "White", false);
        var h = root.AddComponent<Hazard>();
        h.damage = 1; h.knockback = 10f; h.knockTime = 0.25f;
        var ep = root.AddComponent<EnemyPatrol>();
        ep.bodyRenderer = body.GetComponent<Renderer>();
        return Save(root, "Guard");
    }

    static GameObject MakeCrusher()
    {
        var root = new GameObject("Crusher");
        var warning = Prim(PrimitiveType.Cube, "Warning", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(4f, 0.06f, 4f), "Yellow", false);
        var slab = Prim(PrimitiveType.Cube, "Slab", root.transform, new Vector3(0f, 1f, 0f), new Vector3(4f, 2f, 4f), "Red");
        slab.GetComponent<Collider>().isTrigger = true;
        var h = slab.AddComponent<Hazard>();
        h.damage = 2; h.knockback = 12f; h.knockTime = 0.3f;
        var c = root.AddComponent<Crusher>();
        c.warning = warning; c.slab = slab;
        warning.SetActive(false); slab.SetActive(false);
        return Save(root, "Crusher");
    }

    static GameObject MakePad()
    {
        var root = new GameObject("BouncePad");
        var bc = root.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(3f, 1f, 3f); bc.center = new Vector3(0f, 0.5f, 0f);
        Prim(PrimitiveType.Cube, "Pad", root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(3f, 0.2f, 3f), "Cyan", false);
        root.AddComponent<BouncePad>();
        return Save(root, "BouncePad");
    }

    static GameObject MakePower()
    {
        var root = new GameObject("PowerUp");
        var sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true; sc.radius = 0.8f;
        var gem = Prim(PrimitiveType.Cube, "Gem", root.transform, Vector3.zero, Vector3.one * 0.7f, "Purple", false);
        gem.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        root.AddComponent<PowerUp>();
        return Save(root, "PowerUp");
    }

    static GameObject MakeCheckpoint()
    {
        var root = new GameObject("Checkpoint");
        var bc = root.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(4f, 3f, 1.5f); bc.center = new Vector3(0f, 1.5f, 0f);
        Prim(PrimitiveType.Cube, "Pole", root.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.2f), "Gray", false);
        var flag = Prim(PrimitiveType.Cube, "Flag", root.transform, new Vector3(0.5f, 2.6f, 0f), new Vector3(1f, 0.6f, 0.1f), "Red", false);
        var c = root.AddComponent<Checkpoint>();
        c.flag = flag.GetComponent<Renderer>();
        return Save(root, "Checkpoint");
    }

    static GameObject MakeGoal()
    {
        var root = new GameObject("Goal");
        var bc = root.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(3f, 3f, 3f); bc.center = new Vector3(0f, 1.5f, 0f);
        var disc = Prim(PrimitiveType.Cylinder, "Disc", root.transform, new Vector3(0f, 0.08f, 0f), new Vector3(3f, 0.08f, 3f), "Red", false);
        var beam = Prim(PrimitiveType.Cylinder, "Beam", root.transform, new Vector3(0f, 3f, 0f), new Vector3(0.4f, 3f, 0.4f), "Red", false);
        var g = root.AddComponent<GoalZone>();
        g.parts = new[] { disc.GetComponent<Renderer>(), beam.GetComponent<Renderer>() };
        return Save(root, "Goal");
    }

    // ---- GameManager + HUD ----------------------------------------------------
    static TextMeshProUGUI Txt(Transform parent, string name, string text, float size, TextAlignmentOptions al, Vector2 anchor, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = al; t.color = Color.white; t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
        return t;
    }

    static GameObject Panel(Transform parent, string name, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        return go;
    }

    static Button Btn(Transform parent, string name, string label, Vector2 pos, out TextMeshProUGUI lbl)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        var mid = new Vector2(0.5f, 0.5f);
        rt.anchorMin = mid; rt.anchorMax = mid; rt.pivot = mid;
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(380f, 90f);
        go.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.9f);
        lbl = Txt(go.transform, "Label", label, 40f, TextAlignmentOptions.Center, mid, Vector2.zero, new Vector2(380f, 90f));
        return go.GetComponent<Button>();
    }

    static GameObject MakeGameManager()
    {
        var root = new GameObject("GameManager");
        var gm = root.AddComponent<GameManager>();

        var canvasGO = new GameObject("HUD", typeof(RectTransform));
        canvasGO.transform.SetParent(root.transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        Transform c = canvasGO.transform;

        var TL = new Vector2(0f, 1f);
        var TR = new Vector2(1f, 1f);
        var TC = new Vector2(0.5f, 1f);
        var BL = new Vector2(0f, 0f);
        var MID = new Vector2(0.5f, 0.5f);

        gm.scoreText = Txt(c, "Score", "Score", 44f, TextAlignmentOptions.TopLeft, TL, new Vector2(30f, -20f), new Vector2(700f, 60f));
        gm.heartsText = Txt(c, "Hearts", "Hearts", 44f, TextAlignmentOptions.TopLeft, TL, new Vector2(30f, -80f), new Vector2(700f, 60f));
        gm.livesText = Txt(c, "Lives", "Lives", 44f, TextAlignmentOptions.TopLeft, TL, new Vector2(30f, -140f), new Vector2(700f, 60f));
        gm.titleText = Txt(c, "LevelTitle", "Level", 48f, TextAlignmentOptions.TopRight, TR, new Vector2(-30f, -20f), new Vector2(900f, 70f));
        gm.objectiveText = Txt(c, "Objective", "", 30f, TextAlignmentOptions.TopRight, TR, new Vector2(-30f, -90f), new Vector2(1000f, 90f));
        gm.powerText = Txt(c, "Power", "", 46f, TextAlignmentOptions.Top, TC, new Vector2(0f, -20f), new Vector2(900f, 70f));
        gm.messageText = Txt(c, "Message", "", 52f, TextAlignmentOptions.Center, MID, new Vector2(0f, 250f), new Vector2(1600f, 100f));
        gm.powerText.color = new Color(1f, 0.85f, 0.2f);
        gm.messageText.color = new Color(1f, 0.95f, 0.5f);
        Txt(c, "Controls", "WASD / Arrows move   SPACE jump   R restart", 28f, TextAlignmentOptions.BottomLeft, BL, new Vector2(30f, 20f), new Vector2(1200f, 50f));

        // win panel
        var win = Panel(c, "WinPanel", new Color(0f, 0f, 0f, 0.78f));
        Txt(win.transform, "Title", "LEVEL COMPLETE!", 90f, TextAlignmentOptions.Center, MID, new Vector2(0f, 190f), new Vector2(1500f, 120f)).color = new Color(0.3f, 1f, 0.4f);
        gm.winInfoText = Txt(win.transform, "Info", "", 44f, TextAlignmentOptions.Center, MID, new Vector2(0f, 80f), new Vector2(1500f, 80f));
        TextMeshProUGUI nextLbl, winRestartLbl, loseRestartLbl;
        var nextBtn = Btn(win.transform, "NextButton", "Next level", new Vector2(0f, -40f), out nextLbl);
        var winRestart = Btn(win.transform, "RestartButton", "Restart", new Vector2(0f, -150f), out winRestartLbl);
        gm.nextButtonLabel = nextLbl;
        gm.winPanel = win;
        UnityEventTools.AddPersistentListener(nextBtn.onClick, gm.NextLevel);
        UnityEventTools.AddPersistentListener(winRestart.onClick, gm.Restart);

        // lose panel
        var lose = Panel(c, "LosePanel", new Color(0.25f, 0f, 0f, 0.82f));
        Txt(lose.transform, "Title", "GAME OVER", 100f, TextAlignmentOptions.Center, MID, new Vector2(0f, 190f), new Vector2(1500f, 130f)).color = new Color(1f, 0.3f, 0.3f);
        gm.loseInfoText = Txt(lose.transform, "Info", "", 44f, TextAlignmentOptions.Center, MID, new Vector2(0f, 60f), new Vector2(1500f, 150f));
        var loseRestart = Btn(lose.transform, "RestartButton", "Restart", new Vector2(0f, -100f), out loseRestartLbl);
        gm.losePanel = lose;
        UnityEventTools.AddPersistentListener(loseRestart.onClick, gm.Restart);
        win.SetActive(false);
        lose.SetActive(false);

        // EventSystem (needed for the buttons)
        var es = new GameObject("EventSystem");
        es.transform.SetParent(root.transform, false);
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        var uiType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (uiType != null)
        {
            var mod = es.AddComponent(uiType);
            var assign = uiType.GetMethod("AssignDefaultActions");
            if (assign != null) assign.Invoke(mod, null);
        }
        else es.AddComponent<StandaloneInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
        return Save(root, "GameManager");
    }

    // ===================================================================== scene building
    static GameObject Inst(GameObject prefab, Vector3 pos)
    {
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, levelRoot);
        g.transform.position = pos;
        return g;
    }

    static void Begin()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        levelRoot = new GameObject("Level").transform;
        guardCount = 0;
    }

    static string End(int n, string title, string objective, int target, Vector3 startOnFloor)
    {
        var rp = new GameObject("RespawnPoint");
        rp.transform.SetParent(levelRoot, false);
        rp.transform.position = startOnFloor;

        var player = Inst(pPlayer, startOnFloor + Vector3.up * 1.1f);

        var gmObj = Inst(pGM, Vector3.zero);
        var gm = gmObj.GetComponent<GameManager>();
        SetRef(gm, "player", player.transform);
        SetRef(gm, "respawnPoint", rp.transform);
        SetInt(gm, "targetScore", target);
        SetStr(gm, "levelName", "Level " + n + ": " + title);
        SetStr(gm, "objective", objective);

        // kill zone: one wide trigger under the level
        var kz = Prim(PrimitiveType.Cube, "KillZone", levelRoot, new Vector3(0f, -12f, 0f), new Vector3(600f, 2f, 600f), "Dark");
        kz.GetComponent<Collider>().isTrigger = true;
        Object.DestroyImmediate(kz.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(kz.GetComponent<MeshFilter>());
        kz.AddComponent<KillZone>();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.gameObject.AddComponent<CameraFollow>();
            cam.transform.position = startOnFloor + new Vector3(0f, 10f, -8f);
            cam.transform.LookAt(startOnFloor + Vector3.up);
        }

        string path = SceneDir + "/Level0" + n + ".unity";
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
        return path;
    }

    // ---- geometry helpers ------------------------------------------------------
    static void Box(string name, float x, float y, float z, float sx, float sy, float sz, string mat)
    {
        Prim(PrimitiveType.Cube, name, levelRoot, new Vector3(x, y, z), new Vector3(sx, sy, sz), mat);
    }

    static void Wall(float x, float z, float sx, float sz, float h = 3f) { Box("Wall", x, h / 2f, z, sx, h, sz, "Stone"); }

    static void Arena(float cx, float cz, float w, float d)
    {
        Box("Floor", cx, -0.5f, cz, w, 1f, d, "Ground");
        Wall(cx, cz + d / 2f + 0.5f, w + 2f, 1f);
        Wall(cx, cz - d / 2f - 0.5f, w + 2f, 1f);
        Wall(cx + w / 2f + 0.5f, cz, 1f, d);
        Wall(cx - w / 2f - 0.5f, cz, 1f, d);
    }

    static void PlaceCoin(float x, float y, float z) { Inst(pCoin, new Vector3(x, y, z)); }
    static void Goal(float x, float z) { Inst(pGoal, new Vector3(x, 0f, z)); }
    static void Pad(float x, float z) { Inst(pPad, new Vector3(x, 0f, z)); }
    static void Power(float x, float z) { Inst(pPower, new Vector3(x, 1.2f, z)); }
    static void Check(float x, float z) { Inst(pCheck, new Vector3(x, 0f, z)); }
    static void Crush(float x, float z, float delay)
    {
        var c = Inst(pCrusher, new Vector3(x, 0f, z));
        SetFloat(c.GetComponent<Crusher>(), "startDelay", delay);
    }
    static void SpikeRow(float z, params float[] xs) { foreach (float x in xs) Inst(pSpikes, new Vector3(x, 0f, z)); }
    static void SpikeAt(float x, float z) { Inst(pSpikes, new Vector3(x, 0f, z)); }

    static void PlaceGuard(bool pingPong, float patrolSpeed, float chaseSpeed, float chaseRange, float giveUpRange, params Vector3[] pts)
    {
        guardCount++;
        var g = Inst(pGuard, pts[0]);
        g.name = "Guard" + guardCount;
        var wpRoot = new GameObject("Waypoints" + guardCount).transform;
        wpRoot.SetParent(levelRoot, false);
        var arr = new Object[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            var w = new GameObject("WP" + i).transform;
            w.SetParent(wpRoot, false);
            w.position = pts[i];
            arr[i] = w;
        }
        var ep = g.GetComponent<EnemyPatrol>();
        SetRefArray(ep, "waypoints", arr);
        SetBool(ep, "pingPong", pingPong);
        SetFloat(ep, "patrolSpeed", patrolSpeed);
        SetFloat(ep, "chaseSpeed", chaseSpeed);
        SetFloat(ep, "chaseRange", chaseRange);
        SetFloat(ep, "giveUpRange", giveUpRange);
    }

    // ===================================================================== LEVELS
    // Level 1: coins + goal + the bounce pad (nothing hurts yet)
    static string Level1()
    {
        Begin();
        Arena(0f, 0f, 30f, 30f);
        Box("Raised platform", -8f, 1f, 6f, 8f, 2f, 8f, "Wood");
        Pad(-8f, -1f);
        float[,] c = { { 8, -8 }, { -8, -8 }, { 11, -2 }, { 4, 2 }, { 11, 7 }, { 5, 9 }, { -3, -4 }, { 0, 5 } };
        for (int i = 0; i < c.GetLength(0); i++) PlaceCoin(c[i, 0], 1f, c[i, 1]);
        PlaceCoin(-9f, 3f, 6f); PlaceCoin(-7f, 3f, 8f);              // bonus coins on the platform
        Goal(0f, 12.5f);
        return End(1, "Coin Garden", "Collect 8 coins, then step on the goal. The cyan pad launches you up!", 8, new Vector3(0f, 0f, -12f));
    }

    // Level 2: spikes, one heart at a time, coins on risky detours
    static string Level2()
    {
        Begin();
        Arena(0f, 12f, 20f, 44f);
        SpikeRow(6f, -8f, -6f, -4f, 4f, 6f, 8f);
        SpikeRow(14f, -8f, -6f, -4f, -2f, 0f, 2f);
        SpikeRow(22f, 8f, 6f, 4f, 2f, 0f, -2f);
        SpikeRow(28f, -8f, -6f, -4f, 4f, 6f, 8f);
        float[,] c = { { 0, 2 }, { 2.4f, 6 }, { -7, 10 }, { 7, 10 }, { -6, 18 }, { 5.5f, 18.5f }, { -4, 25 }, { 0, 30 } };
        for (int i = 0; i < c.GetLength(0); i++) PlaceCoin(c[i, 0], 1f, c[i, 1]);
        Goal(0f, 32f);
        return End(2, "Spike Garden", "Collect 8 coins. Spikes cost a heart - after a hit you get a moment of protection.", 8, new Vector3(0f, 0f, -6f));
    }

    // Level 3: a guard that chases, spikes beside its route, a checkpoint
    static string Level3()
    {
        Begin();
        Arena(0f, 0f, 32f, 32f);
        PlaceGuard(false, 2f, 3.5f, 6f, 9f,
            new Vector3(-8f, 0f, -6f), new Vector3(8f, 0f, -6f), new Vector3(8f, 0f, 6f), new Vector3(-8f, 0f, 6f));
        SpikeAt(-11f, -2f); SpikeAt(-11f, 0f); SpikeAt(11f, 0f); SpikeAt(11f, 2f); SpikeAt(0f, 0f); SpikeAt(2f, 0f);
        float[,] c = { { -13, -12 }, { 13, -12 }, { -13, 4 }, { 13, -4 }, { -13, 12 }, { 13, 12 }, { 0, -2.5f }, { 0, 3.5f } };
        for (int i = 0; i < c.GetLength(0); i++) PlaceCoin(c[i, 0], 1f, c[i, 1]);
        Check(0f, 9f);
        Goal(0f, 13f);
        return End(3, "Chase Me", "Collect 8 coins. The guard chases you if you get close - outrun it, it gives up.", 8, new Vector3(0f, 0f, -13f));
    }

    // Level 4: the crusher (warns first) with a guard beside it
    static string Level4()
    {
        Begin();
        Arena(0f, 11f, 12f, 38f);
        Crush(-4f, 6f, 0f); Crush(0f, 6f, 1.1f); Crush(4f, 6f, 2.2f);
        Crush(-4f, 18f, 1.6f); Crush(0f, 18f, 2.7f); Crush(4f, 18f, 0.5f);
        PlaceGuard(true, 2f, 3.5f, 5f, 8f, new Vector3(-4f, 0f, 12f), new Vector3(4f, 0f, 12f));
        float[,] c = { { -4, 2 }, { 4, 2 }, { -5, 10 }, { 5, 10 }, { -5, 14 }, { 5, 14 }, { -4, 24 }, { 4, 24 } };
        for (int i = 0; i < c.GetLength(0); i++) PlaceCoin(c[i, 0], 1f, c[i, 1]);
        Check(0f, 22f);
        Goal(0f, 27f);
        return End(4, "Crusher Alley", "Collect 8 coins. Yellow = warning, red = it crushes for 2 hearts. Watch the guard too!", 8, new Vector3(0f, 0f, -5f));
    }

    // Level 5: everything + two speed pickups + a ping-pong guard
    static string Level5()
    {
        Begin();
        Arena(0f, 27f, 16f, 70f);
        SpikeRow(6f, -7f, -5f, -3f, 3f, 5f, 7f);
        Power(0f, 12f);
        PlaceGuard(false, 2f, 3.5f, 6f, 9f,
            new Vector3(-6f, 0f, 14f), new Vector3(6f, 0f, 14f), new Vector3(6f, 0f, 22f), new Vector3(-6f, 0f, 22f));
        Power(0f, 26f);
        Crush(-6f, 32f, 0f); Crush(-2f, 32f, 0.8f); Crush(2f, 32f, 1.6f); Crush(6f, 32f, 2.4f);
        Check(0f, 38f);
        SpikeRow(44f, -7f, -5f, -3f, 3f, 5f, 7f);
        SpikeRow(48f, -7f, -5f, -3f, -1f);
        PlaceGuard(true, 2.5f, 3.5f, 5f, 8f, new Vector3(-6f, 0f, 54f), new Vector3(6f, 0f, 54f));
        float[,] c = { { 0, 2 }, { -6, 10 }, { 6, 10 }, { -7, 18 }, { 7, 18 }, { 0, 29 }, { -6, 41 }, { 6, 41 }, { -3, 46 }, { 3, 50 } };
        for (int i = 0; i < c.GetLength(0); i++) PlaceCoin(c[i, 0], 1f, c[i, 1]);
        Goal(0f, 58f);
        return End(5, "The Gauntlet", "Collect 10 coins. Grab both speed gems within 5 s - then survive everything.", 10, new Vector3(0f, 0f, -5f));
    }
}
