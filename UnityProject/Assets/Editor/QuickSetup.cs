#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Minimal scene setup that creates a playable battle scene.
/// Uses reflection to add IsoRPG components (no asmdef dependency).
/// Saves textures as real assets so sprites persist in Play mode.
///
/// Unity menu: IsoRPG → Quick Setup Battle Scene
/// </summary>
public static class QuickSetup
{
    [MenuItem("IsoRPG/Quick Setup Battle Scene")]
    public static void Setup()
    {
        Debug.Log("[QuickSetup] Starting battle scene setup...");

        // Ensure directories exist
        EnsureFolder("Assets/Sprites");
        EnsureFolder("Assets/Sprites/Generated");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Tiles");
        EnsureFolder("Assets/Prefabs/Units");
        EnsureFolder("Assets/Scenes");

        // Save textures as real assets first
        var tileTex = MakeDiamondTexture();
        SaveTexture(tileTex, "Assets/Sprites/Generated/TileSprite.png");
        AssetDatabase.Refresh();

        var unitTex = MakeUnitTexture();
        SaveTexture(unitTex, "Assets/Sprites/Generated/UnitSprite.png");
        AssetDatabase.Refresh();

        // Configure texture import settings
        ConfigureSprite("Assets/Sprites/Generated/TileSprite.png", 64, new Vector2(0.5f, 0.75f));
        ConfigureSprite("Assets/Sprites/Generated/UnitSprite.png", 32, new Vector2(0.5f, 0.3f));

        var tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Generated/TileSprite.png");
        var unitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Generated/UnitSprite.png");

        Debug.Log($"[QuickSetup] Tile sprite: {tileSprite != null}, Unit sprite: {unitSprite != null}");

        // Create prefabs
        var tilePrefab = CreateTilePrefab(tileSprite);
        var unitPrefab = CreateUnitPrefab(unitSprite);

        // New scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Camera
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 4f;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            cam.transform.position = new Vector3(0, 1.5f, -10);
            AddComponentByName(cam.gameObject, "IsoRPG.Map.BattleCameraController");
            Debug.Log("[QuickSetup] Camera configured");
        }

        // Grid
        var gridObj = new GameObject("IsometricGrid");
        var gridComp = AddComponentByName(gridObj, "IsoRPG.Map.IsometricGrid");
        if (gridComp != null)
        {
            var so = new SerializedObject(gridComp);
            var prop = so.FindProperty("tilePrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = tilePrefab;
                so.ApplyModifiedProperties();
                Debug.Log("[QuickSetup] Grid created with tile prefab");
            }
            else
            {
                Debug.LogError("[QuickSetup] Could not find 'tilePrefab' property on IsometricGrid");
            }
        }

        // Battle Manager
        var battleObj = new GameObject("BattleManager");
        var battleComp = AddComponentByName(battleObj, "IsoRPG.Battle.BattleManager");
        if (battleComp != null)
        {
            var so = new SerializedObject(battleComp);
            SetRef(so, "grid", gridComp);
            SetRef(so, "unitPrefab", unitPrefab);
            SetInt(so, "rngSeed", 42);
            so.ApplyModifiedProperties();
            Debug.Log("[QuickSetup] BattleManager created");
        }

        // --- UI ---
        CreateBattleUI();

        // --- SFX Manager ---
        var sfxObj = new GameObject("SFXManager");
        sfxObj.AddComponent<AudioSource>();
        AddComponentByName(sfxObj, "IsoRPG.UI.SFXManager");

        // Save scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Battle.unity");

        Debug.Log("[QuickSetup] DONE! Press Play to start the battle.");

        EditorUtility.DisplayDialog("IsoRPG - Ready!",
            "Battle scene created!\n\n" +
            "Press PLAY to start.\n\n" +
            "All actions via mouse:\n" +
            "  Click menu buttons to Move/Act/Wait/Undo\n" +
            "  Click tiles to select destinations/targets\n" +
            "  Right-click to cancel\n\n" +
            "Camera: WASD pan, Scroll zoom, Q/E rotate",
            "Play!");
    }

    // --- UI Creation ---

    static void CreateBattleUI()
    {
        // EventSystem (required for UI interaction)
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Canvas
        var canvasObj = new GameObject("BattleCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // UIManager
        AddComponentByName(canvasObj, "IsoRPG.UI.UIManager");

        // --- Action Menu ---
        var actionMenu = CreateActionMenuUI(canvasObj.transform);

        // --- Ability Menu ---
        var abilityMenu = CreateAbilityMenuUI(canvasObj.transform);

        // --- Turn Banner ---
        CreateTurnBanner(canvasObj.transform);

        // Wire UIManager references via SerializedObject
        var uiMgr = canvasObj.GetComponent(FindType("IsoRPG.UI.UIManager"));
        if (uiMgr != null)
        {
            var so = new SerializedObject(uiMgr);
            SetRef(so, "ActionMenu", actionMenu.GetComponent(FindType("IsoRPG.UI.ActionMenuUI")));
            SetRef(so, "AbilityMenu", abilityMenu.GetComponent(FindType("IsoRPG.UI.AbilityMenuUI")));
            so.ApplyModifiedProperties();
        }

        Debug.Log("[QuickSetup] UI created: ActionMenu, AbilityMenu, TurnBanner, EventSystem");
    }

    static GameObject CreateActionMenuUI(Transform parent)
    {
        // Panel
        var panel = new GameObject("ActionMenu");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 20);
        panelRect.sizeDelta = new Vector2(180, 220);

        var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        var layout = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;

        // Buttons
        var moveBtn = MakeMenuButton("Move", panel.transform, new Color(0.2f, 0.45f, 0.75f));
        var actBtn = MakeMenuButton("Act", panel.transform, new Color(0.75f, 0.25f, 0.25f));
        var waitBtn = MakeMenuButton("Wait", panel.transform, new Color(0.45f, 0.45f, 0.45f));
        var undoBtn = MakeMenuButton("Undo", panel.transform, new Color(0.7f, 0.6f, 0.2f));

        // Wire navigation (arrow keys): Move↔Act↔Wait↔Undo vertical
        SetButtonNavigation(moveBtn, null, actBtn);
        SetButtonNavigation(actBtn, moveBtn, waitBtn);
        SetButtonNavigation(waitBtn, actBtn, undoBtn);
        SetButtonNavigation(undoBtn, waitBtn, null);

        // Add ActionMenuUI component
        var actionMenuComp = AddComponentByName(panel, "IsoRPG.UI.ActionMenuUI");
        if (actionMenuComp != null)
        {
            var so = new SerializedObject(actionMenuComp);
            SetRef(so, "moveButton", moveBtn.GetComponent<UnityEngine.UI.Button>());
            SetRef(so, "actButton", actBtn.GetComponent<UnityEngine.UI.Button>());
            SetRef(so, "waitButton", waitBtn.GetComponent<UnityEngine.UI.Button>());
            SetRef(so, "undoButton", undoBtn.GetComponent<UnityEngine.UI.Button>());
            so.ApplyModifiedProperties();
        }

        // Set first selected for EventSystem keyboard nav
        var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es != null)
            es.firstSelectedGameObject = moveBtn;

        return panel;
    }

    static GameObject CreateAbilityMenuUI(Transform parent)
    {
        var panel = new GameObject("AbilityMenu");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 20);
        panelRect.sizeDelta = new Vector2(220, 280);

        var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        var layout = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Entry container (abilities get instantiated here at runtime)
        var container = new GameObject("AbilityEntries");
        container.transform.SetParent(panel.transform, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        var containerLayout = container.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        containerLayout.spacing = 6;
        containerLayout.childForceExpandWidth = true;
        containerLayout.childForceExpandHeight = false;

        // Cancel button
        var cancelBtn = MakeMenuButton("Cancel", panel.transform, new Color(0.5f, 0.3f, 0.3f));

        // Ability entry prefab (saved as asset)
        var entryPrefab = CreateAbilityEntryPrefab();

        // Add AbilityMenuUI component
        var abilityMenuComp = AddComponentByName(panel, "IsoRPG.UI.AbilityMenuUI");
        if (abilityMenuComp != null)
        {
            var so = new SerializedObject(abilityMenuComp);
            SetRef(so, "abilityEntryPrefab", entryPrefab);
            SetRef(so, "entryContainer", container.transform);
            SetRef(so, "cancelButton", cancelBtn.GetComponent<UnityEngine.UI.Button>());
            so.ApplyModifiedProperties();
        }

        // Don't deactivate here — AbilityMenuUI.Awake() subscribes to events
        // then calls Hide() itself. If we deactivate here, Awake never runs.
        return panel;
    }

    static GameObject CreateAbilityEntryPrefab()
    {
        var entry = new GameObject("AbilityEntry");
        var rect = entry.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 40);

        var img = entry.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.15f, 0.15f, 0.22f, 0.9f);

        entry.AddComponent<UnityEngine.UI.Button>();

        // Label
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(entry.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 2);
        textRect.offsetMax = new Vector2(-8, -2);
        var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "Ability";
        tmp.fontSize = 16;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;

        string dir = "Assets/Prefabs/UI";
        EnsureFolder(dir);
        var prefab = PrefabUtility.SaveAsPrefabAsset(entry, dir + "/AbilityEntryPrefab.prefab");
        Object.DestroyImmediate(entry);
        return prefab;
    }

    static void CreateTurnBanner(Transform parent)
    {
        var bannerRoot = new GameObject("TurnBanner");
        bannerRoot.transform.SetParent(parent, false);
        AddComponentByName(bannerRoot, "IsoRPG.UI.TurnBannerUI");

        var bannerPanel = new GameObject("BannerPanel");
        bannerPanel.transform.SetParent(bannerRoot.transform, false);
        var rect = bannerPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.7f);
        rect.anchorMax = new Vector2(1, 0.78f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = bannerPanel.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.15f, 0.3f, 0.6f, 0.9f);

        var textObj = new GameObject("NameText");
        textObj.transform.SetParent(bannerPanel.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 0);
        textRect.offsetMax = new Vector2(-20, 0);
        var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 28;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;

        // Wire TurnBannerUI
        var bannerComp = bannerRoot.GetComponent(FindType("IsoRPG.UI.TurnBannerUI"));
        if (bannerComp != null)
        {
            var so = new SerializedObject(bannerComp);
            SetRef(so, "bannerRect", rect);
            SetRef(so, "nameText", tmp);
            SetRef(so, "backgroundImage", img);
            so.ApplyModifiedProperties();
        }

        bannerPanel.SetActive(false);
    }

    static GameObject MakeMenuButton(string label, Transform parent, Color color)
    {
        var btnObj = new GameObject(label + "Button");
        btnObj.transform.SetParent(parent, false);
        var rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 42);

        var img = btnObj.AddComponent<UnityEngine.UI.Image>();
        img.color = color;

        var btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.3f;
        colors.pressedColor = color * 0.7f;
        colors.selectedColor = color * 1.2f;
        colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        btn.colors = colors;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = TMPro.FontStyles.Bold;

        return btnObj;
    }

    static void SetButtonNavigation(GameObject btnObj, GameObject upObj, GameObject downObj)
    {
        var btn = btnObj.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) return;

        var nav = btn.navigation;
        nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
        if (upObj != null) nav.selectOnUp = upObj.GetComponent<UnityEngine.UI.Selectable>();
        if (downObj != null) nav.selectOnDown = downObj.GetComponent<UnityEngine.UI.Selectable>();
        btn.navigation = nav;
    }

    // --- Component helpers (reflection-based to avoid asmdef deps) ---

    static Component AddComponentByName(GameObject go, string fullTypeName)
    {
        var type = FindType(fullTypeName);
        if (type == null)
        {
            Debug.LogError($"[QuickSetup] Type not found: {fullTypeName}");
            return null;
        }
        return go.AddComponent(type);
    }

    static System.Type FindType(string fullName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType(fullName);
            if (type != null) return type;
        }
        return null;
    }

    static void SetRef(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null) prop.objectReferenceValue = value;
        else Debug.LogWarning($"[QuickSetup] Property not found: {propName}");
    }

    static void SetInt(SerializedObject so, string propName, int value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null) prop.intValue = value;
    }

    // --- Prefab creation ---

    static GameObject CreateTilePrefab(Sprite sprite)
    {
        var go = new GameObject("TilePrefab");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        AddComponentByName(go, "IsoRPG.Map.TileView");

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Tiles/TilePrefab.prefab");
        Object.DestroyImmediate(go);
        Debug.Log($"[QuickSetup] Tile prefab saved (sprite: {sprite != null})");
        return prefab;
    }

    static GameObject CreateUnitPrefab(Sprite sprite)
    {
        var go = new GameObject("UnitPrefab");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Units/UnitPrefab.prefab");
        Object.DestroyImmediate(go);
        Debug.Log($"[QuickSetup] Unit prefab saved (sprite: {sprite != null})");
        return prefab;
    }

    // --- Texture saving ---

    static void SaveTexture(Texture2D tex, string assetPath)
    {
        var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);
        Debug.Log($"[QuickSetup] Saved texture: {assetPath} ({bytes.Length} bytes)");
    }

    static void ConfigureSprite(string assetPath, int pixelsPerUnit, Vector2 pivot)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[QuickSetup] Could not get TextureImporter for {assetPath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spritePivot = pivot;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 128;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
        Debug.Log($"[QuickSetup] Configured sprite: {assetPath} (PPU={pixelsPerUnit})");
    }

    // --- Texture generation ---

    static Texture2D MakeDiamondTexture()
    {
        int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color32[s * s];
        var clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        var top = new Color32(120, 170, 100, 255);    // green grass top
        var frontL = new Color32(100, 80, 55, 255);    // dirt left face
        var frontR = new Color32(75, 60, 42, 255);     // dirt right face
        var edge = new Color32(50, 40, 30, 255);       // dark outline

        int cx = 32;

        // Top diamond face (y from 33 to 48)
        for (int row = 0; row <= 15; row++)
        {
            int w = row * 2;
            int y1 = 48 - row;  // upper half of diamond
            int y2 = 33 + row;  // lower half of diamond
            for (int x = cx - w; x <= cx + w; x++)
            {
                Px(px, s, x, y1, top);
                if (row < 15) Px(px, s, x, y2, top);
            }
            // Edge pixels
            Px(px, s, cx - w, y1, edge);
            Px(px, s, cx + w, y1, edge);
            if (row < 15)
            {
                Px(px, s, cx - w, y2, edge);
                Px(px, s, cx + w, y2, edge);
            }
        }

        // Left face (below diamond, left side)
        for (int row = 0; row < 20; row++)
        {
            int y = 32 - row;
            int leftEdge = cx - 30 + row * 2;
            if (leftEdge < 0) leftEdge = 0;
            for (int x = leftEdge; x < cx; x++)
            {
                if (y >= 0 && y < s && px[y * s + x].a == 0)
                    Px(px, s, x, y, frontL);
            }
            if (y >= 0) Px(px, s, leftEdge, y, edge);
        }

        // Right face (below diamond, right side)
        for (int row = 0; row < 20; row++)
        {
            int y = 32 - row;
            int rightEdge = cx + 30 - row * 2;
            if (rightEdge >= s) rightEdge = s - 1;
            for (int x = cx; x <= rightEdge; x++)
            {
                if (y >= 0 && y < s && px[y * s + x].a == 0)
                    Px(px, s, x, y, frontR);
            }
            if (y >= 0) Px(px, s, rightEdge, y, edge);
        }

        // Bottom edge
        Px(px, s, cx, 12, edge);
        for (int i = 1; i < 3; i++) { Px(px, s, cx - i, 12 + i, edge); Px(px, s, cx + i, 12 + i, edge); }

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    static Texture2D MakeUnitTexture()
    {
        int s = 24;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color32[s * s];
        var clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        var body = new Color32(80, 130, 210, 255);
        var outline = new Color32(30, 30, 50, 255);
        var skin = new Color32(225, 185, 155, 255);

        // Head (circle)
        for (int y = 17; y < 23; y++)
            for (int x = 9; x < 15; x++)
            {
                float dx = x - 12f, dy = y - 19.5f;
                if (dx * dx + dy * dy < 8) Px(px, s, x, y, skin);
                if (dx * dx + dy * dy >= 7 && dx * dx + dy * dy < 10) Px(px, s, x, y, outline);
            }

        // Body
        for (int y = 7; y < 17; y++)
            for (int x = 8; x < 16; x++)
                Px(px, s, x, y, body);
        for (int y = 7; y < 17; y++) { Px(px, s, 8, y, outline); Px(px, s, 15, y, outline); }
        for (int x = 8; x < 16; x++) { Px(px, s, x, 7, outline); Px(px, s, x, 17, outline); }

        // Legs
        for (int y = 1; y < 7; y++)
        {
            Px(px, s, 9, y, body); Px(px, s, 10, y, body);
            Px(px, s, 13, y, body); Px(px, s, 14, y, body);
            Px(px, s, 9, y, outline); Px(px, s, 14, y, outline);
        }
        // Feet
        Px(px, s, 9, 1, outline); Px(px, s, 10, 1, outline);
        Px(px, s, 13, 1, outline); Px(px, s, 14, 1, outline);

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    static void Px(Color32[] px, int s, int x, int y, Color32 c)
    {
        if (x >= 0 && x < s && y >= 0 && y < s) px[y * s + x] = c;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
