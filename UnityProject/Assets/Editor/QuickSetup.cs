#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Minimal scene setup that doesn't depend on any IsoRPG assemblies.
/// Adds components by string name so it compiles independently.
///
/// Unity menu: IsoRPG → Quick Setup Battle Scene
/// </summary>
public static class QuickSetup
{
    [MenuItem("IsoRPG/Quick Setup Battle Scene")]
    public static void Setup()
    {
        // New scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Camera
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            cam.transform.position = new Vector3(0, 1, -10);

            // Add camera controller by type name
            AddComponentByName(cam.gameObject, "IsoRPG.Map.BattleCameraController");
        }

        // Tile prefab
        var tilePrefab = CreateTilePrefab();

        // Unit prefab
        var unitPrefab = CreateUnitPrefab();

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
        }

        // Save
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Battle.unity");

        Debug.Log("[IsoRPG] Battle scene created! Press Play.");
        Debug.Log("[IsoRPG] Controls: M=Move, A=Act, W=Wait, U=Undo, 1-4=Ability, Esc=Cancel, WASD=Pan, Scroll=Zoom, Q/E=Rotate");

        EditorUtility.DisplayDialog("IsoRPG - Ready!",
            "Battle scene created!\n\n" +
            "Press PLAY to start.\n\n" +
            "Controls:\n" +
            "  M = Move\n" +
            "  A = Act (then 1-4 to pick ability)\n" +
            "  W = Wait (end turn)\n" +
            "  U = Undo\n" +
            "  Esc = Cancel\n" +
            "  WASD = Pan camera\n" +
            "  Scroll = Zoom\n" +
            "  Q/E = Rotate view\n" +
            "  Left Click = Confirm\n" +
            "  Right Click = Cancel",
            "Play!");
    }

    static Component AddComponentByName(GameObject go, string fullTypeName)
    {
        var type = FindType(fullTypeName);
        if (type == null)
        {
            Debug.LogError($"[QuickSetup] Could not find type: {fullTypeName}");
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
    }

    static void SetInt(SerializedObject so, string propName, int value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null) prop.intValue = value;
    }

    static GameObject CreateTilePrefab()
    {
        var go = new GameObject("_TilePrefab_temp");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeDiamondSprite();

        AddComponentByName(go, "IsoRPG.Map.TileView");

        string dir = "Assets/Prefabs/Tiles";
        if (!AssetDatabase.IsValidFolder(dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Tiles");
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, dir + "/TilePrefab.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateUnitPrefab()
    {
        var go = new GameObject("_UnitPrefab_temp");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeUnitSprite();

        string dir = "Assets/Prefabs/Units";
        if (!AssetDatabase.IsValidFolder(dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Units");
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, dir + "/UnitPrefab.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Sprite MakeDiamondSprite()
    {
        int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color32[s * s];
        var clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        // Diamond (isometric tile shape)
        int cx = 32, cy = 48;
        for (int y = 0; y < 16; y++)
        {
            int w = y * 2;
            for (int x = -w; x <= w; x++)
            {
                Px(px, s, cx + x, cy - y, new Color32(140, 180, 130, 255));
                Px(px, s, cx + x, cy + y - 16, new Color32(140, 180, 130, 255));
            }
        }
        // Side faces
        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                int ry = 32 - y;
                if (ry >= 0 && ry < s)
                {
                    if (px[ry * s + x].a == 0 && ry < 40)
                    {
                        Px(px, s, x, ry, new Color32(90, 70, 50, 255));
                    }
                    if (px[ry * s + (s - 1 - x)].a == 0 && ry < 40)
                    {
                        Px(px, s, s - 1 - x, ry, new Color32(70, 55, 40, 255));
                    }
                }
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.75f), 64);
    }

    static Sprite MakeUnitSprite()
    {
        int s = 24;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color32[s * s];
        var clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        var body = new Color32(80, 130, 200, 255);
        var outline = new Color32(30, 30, 50, 255);
        var skin = new Color32(220, 180, 150, 255);

        // Head
        for (int y = 18; y < 23; y++)
            for (int x = 9; x < 15; x++)
            {
                float dx = x - 12f, dy = y - 20f;
                if (dx * dx + dy * dy < 7) Px(px, s, x, y, skin);
                if (dx * dx + dy * dy >= 6 && dx * dx + dy * dy < 9) Px(px, s, x, y, outline);
            }
        // Body
        for (int y = 8; y < 18; y++)
            for (int x = 8; x < 16; x++)
                Px(px, s, x, y, body);
        // Body outline
        for (int y = 8; y < 18; y++) { Px(px, s, 8, y, outline); Px(px, s, 15, y, outline); }
        for (int x = 8; x < 16; x++) Px(px, s, x, 8, outline);
        // Legs
        for (int y = 2; y < 8; y++) { Px(px, s, 9, y, body); Px(px, s, 10, y, body); Px(px, s, 13, y, body); Px(px, s, 14, y, body); }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.3f), 32);
    }

    static void Px(Color32[] px, int s, int x, int y, Color32 c)
    {
        if (x >= 0 && x < s && y >= 0 && y < s) px[y * s + x] = c;
    }
}
#endif
