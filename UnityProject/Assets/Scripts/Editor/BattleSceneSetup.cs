using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IsoRPG.Battle;
using IsoRPG.Map;
using IsoRPG.UI;

/// <summary>
/// Editor tool that creates a fully playable Battle scene with all required
/// GameObjects, components, prefabs, and references wired up.
///
/// Usage: Unity menu → IsoRPG → Setup Battle Scene
/// </summary>
public static class BattleSceneSetup
{
    [MenuItem("IsoRPG/Setup Battle Scene")]
    public static void SetupBattleScene()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Camera ---
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            cam.transform.position = new Vector3(0, 0, -10);
            cam.gameObject.AddComponent<BattleCameraController>();
        }

        // --- Tile Prefab ---
        var tilePrefab = CreateTilePrefab();

        // --- Unit Prefab ---
        var unitPrefab = CreateUnitPrefab();

        // --- Grid ---
        var gridObj = new GameObject("IsometricGrid");
        var grid = gridObj.AddComponent<IsometricGrid>();

        // Assign tile prefab via SerializedObject
        var gridSO = new SerializedObject(grid);
        var tilePrefabProp = gridSO.FindProperty("tilePrefab");
        tilePrefabProp.objectReferenceValue = tilePrefab;
        gridSO.ApplyModifiedProperties();

        // --- Battle Manager ---
        var battleObj = new GameObject("BattleManager");
        var battleMgr = battleObj.AddComponent<BattleManager>();

        var battleSO = new SerializedObject(battleMgr);
        battleSO.FindProperty("grid").objectReferenceValue = grid;
        battleSO.FindProperty("unitPrefab").objectReferenceValue = unitPrefab;
        battleSO.FindProperty("rngSeed").intValue = 42;
        battleSO.ApplyModifiedProperties();

        // --- UI Canvas ---
        CreateUICanvas();

        // --- Save Scene ---
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Battle.unity");

        Debug.Log("[BattleSceneSetup] Battle scene created! Press Play to start.");
        EditorUtility.DisplayDialog("IsoRPG",
            "Battle scene created!\n\n" +
            "Press Play to start the battle.\n\n" +
            "Controls:\n" +
            "  M = Move\n" +
            "  A = Act (use abilities)\n" +
            "  W = Wait\n" +
            "  U = Undo\n" +
            "  1-4 = Select ability\n" +
            "  Escape = Cancel\n" +
            "  WASD = Pan camera\n" +
            "  Scroll = Zoom\n" +
            "  Q/E = Rotate",
            "Let's Go!");
    }

    private static GameObject CreateTilePrefab()
    {
        var tileObj = new GameObject("TilePrefab");

        // Create a diamond sprite procedurally
        var sr = tileObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDiamondSprite();
        sr.sortingLayerName = "Default";

        tileObj.AddComponent<TileView>();

        // Save as prefab
        string prefabPath = "Assets/Prefabs/Tiles/TilePrefab.prefab";
        EnsureDirectoryExists(prefabPath);
        var prefab = PrefabUtility.SaveAsPrefabAsset(tileObj, prefabPath);
        Object.DestroyImmediate(tileObj);
        return prefab;
    }

    private static GameObject CreateUnitPrefab()
    {
        var unitObj = new GameObject("UnitPrefab");

        var sr = unitObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateUnitSprite();
        sr.sortingLayerName = "Default";

        // UnitView is added at runtime by BattleManager

        // Save as prefab
        string prefabPath = "Assets/Prefabs/Units/UnitPrefab.prefab";
        EnsureDirectoryExists(prefabPath);
        var prefab = PrefabUtility.SaveAsPrefabAsset(unitObj, prefabPath);
        Object.DestroyImmediate(unitObj);
        return prefab;
    }

    private static void CreateUICanvas()
    {
        // Create Canvas
        var canvasObj = new GameObject("BattleCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // UIManager
        var uiManager = canvasObj.AddComponent<UIManager>();

        // Action Menu
        var actionMenuObj = CreateActionMenu(canvasObj.transform);
        uiManager.ActionMenu = actionMenuObj.GetComponent<ActionMenuUI>();

        // Battle Result Panel (hidden by default)
        var resultObj = CreateBattleResultPanel(canvasObj.transform);
        uiManager.BattleResult = resultObj.GetComponent<BattleResultPanelUI>();
    }

    private static GameObject CreateActionMenu(Transform parent)
    {
        var menuObj = new GameObject("ActionMenu");
        menuObj.transform.SetParent(parent, false);

        var rect = menuObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-20, 20);
        rect.sizeDelta = new Vector2(160, 200);

        var bg = menuObj.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

        var layout = menuObj.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var actionMenu = menuObj.AddComponent<ActionMenuUI>();

        // Create buttons via SerializedObject
        var moveBtn = CreateButton("Move", menuObj.transform, new Color(0.2f, 0.5f, 0.8f));
        var actBtn = CreateButton("Act", menuObj.transform, new Color(0.8f, 0.3f, 0.3f));
        var waitBtn = CreateButton("Wait", menuObj.transform, new Color(0.5f, 0.5f, 0.5f));
        var undoBtn = CreateButton("Undo", menuObj.transform, new Color(0.7f, 0.6f, 0.2f));

        var so = new SerializedObject(actionMenu);
        so.FindProperty("moveButton").objectReferenceValue = moveBtn.GetComponent<UnityEngine.UI.Button>();
        so.FindProperty("actButton").objectReferenceValue = actBtn.GetComponent<UnityEngine.UI.Button>();
        so.FindProperty("waitButton").objectReferenceValue = waitBtn.GetComponent<UnityEngine.UI.Button>();
        so.FindProperty("undoButton").objectReferenceValue = undoBtn.GetComponent<UnityEngine.UI.Button>();
        so.ApplyModifiedProperties();

        return menuObj;
    }

    private static GameObject CreateBattleResultPanel(Transform parent)
    {
        var panelObj = new GameObject("BattleResultPanel");
        panelObj.transform.SetParent(parent, false);

        var rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400, 200);

        var bg = panelObj.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Result text
        var textObj = new GameObject("ResultText");
        textObj.transform.SetParent(panelObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.5f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, -10);
        var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "VICTORY";
        tmp.fontSize = 48;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(1, 0.85f, 0.2f);

        var resultPanel = panelObj.AddComponent<BattleResultPanelUI>();
        var rso = new SerializedObject(resultPanel);
        rso.FindProperty("resultText").objectReferenceValue = tmp;
        rso.ApplyModifiedProperties();

        panelObj.SetActive(false);
        return panelObj;
    }

    private static GameObject CreateButton(string label, Transform parent, Color color)
    {
        var btnObj = new GameObject(label + "Button");
        btnObj.transform.SetParent(parent, false);

        var rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140, 36);

        var img = btnObj.AddComponent<UnityEngine.UI.Image>();
        img.color = color;

        var btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
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
        tmp.fontSize = 18;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnObj;
    }

    private static Sprite CreateDiamondSprite()
    {
        int size = 64;
        int halfW = 32;
        int halfH = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // Clear to transparent
        var clear = new Color32(0, 0, 0, 0);
        var pixels = tex.GetPixels32();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        // Draw diamond shape (2:1 isometric)
        var fillColor = new Color32(180, 200, 180, 255);
        var outlineColor = new Color32(80, 80, 80, 255);

        int cx = halfW;
        int cy = size - halfH - 1; // top face center

        for (int y = 0; y < halfH; y++)
        {
            int width = (int)((float)y / halfH * halfW);
            for (int x = -width; x <= width; x++)
            {
                SetPixel(pixels, size, cx + x, cy + y, fillColor);
                SetPixel(pixels, size, cx + x, cy - y, fillColor);
            }
            // Outline edges
            SetPixel(pixels, size, cx - width, cy + y, outlineColor);
            SetPixel(pixels, size, cx + width, cy + y, outlineColor);
            SetPixel(pixels, size, cx - width, cy - y, outlineColor);
            SetPixel(pixels, size, cx + width, cy - y, outlineColor);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64);
    }

    private static Sprite CreateUnitSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        var pixels = tex.GetPixels32();
        var clear = new Color32(0, 0, 0, 0);
        var body = new Color32(100, 150, 220, 255);
        var outline = new Color32(40, 40, 60, 255);

        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        // Simple character silhouette
        // Head (circle-ish at top)
        for (int y = 22; y < 30; y++)
            for (int x = 12; x < 20; x++)
            {
                float dx = x - 16f, dy = y - 26f;
                if (dx * dx + dy * dy < 16) SetPixel(pixels, size, x, y, body);
                if (dx * dx + dy * dy >= 14 && dx * dx + dy * dy < 18) SetPixel(pixels, size, x, y, outline);
            }

        // Body
        for (int y = 10; y < 22; y++)
            for (int x = 11; x < 21; x++)
            {
                SetPixel(pixels, size, x, y, body);
                if (x == 11 || x == 20 || y == 10) SetPixel(pixels, size, x, y, outline);
            }

        // Legs
        for (int y = 4; y < 10; y++)
        {
            SetPixel(pixels, size, 12, y, body);
            SetPixel(pixels, size, 13, y, body);
            SetPixel(pixels, size, 18, y, body);
            SetPixel(pixels, size, 19, y, body);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.75f), 64);
    }

    private static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 color)
    {
        if (x >= 0 && x < size && y >= 0 && y < size)
            pixels[y * size + x] = color;
    }

    private static void EnsureDirectoryExists(string assetPath)
    {
        var dir = System.IO.Path.GetDirectoryName(
            System.IO.Path.Combine(Application.dataPath, "..", assetPath));
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
    }
}
