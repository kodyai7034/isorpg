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

        // --- Combat Menu (Attack / Skills / Skip) ---
        CreateCombatMenuUI(canvasObj.transform);

        // --- Skills Menu ---
        var abilityMenu = CreateAbilityMenuUI(canvasObj.transform);

        // --- Selection Context (shown during tile selection) ---
        CreateSelectionContextUI(canvasObj.transform);

        // --- Turn Banner ---
        CreateTurnBanner(canvasObj.transform);

        // Wire UIManager references
        var uiMgr = canvasObj.GetComponent(FindType("IsoRPG.UI.UIManager"));
        if (uiMgr != null)
        {
            var so = new SerializedObject(uiMgr);
            SetRef(so, "ActionMenu", actionMenu.GetComponent(FindType("IsoRPG.UI.ActionMenuUI")));
            SetRef(so, "AbilityMenu", abilityMenu.GetComponent(FindType("IsoRPG.UI.AbilityMenuUI")));
            so.ApplyModifiedProperties();
        }

        Debug.Log("[QuickSetup] UI created: ActionMenu, CombatMenu, SkillsMenu, TurnBanner");
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
        panelRect.sizeDelta = new Vector2(220, 260);

        var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        SetPanelSprite(panelImg, "Panels/action_menu", new Color(0.08f, 0.08f, 0.12f, 0.92f));

        var layout = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 14, 20, 14);
        layout.spacing = 8;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;

        // Buttons with sprite art
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

    static void CreateCombatMenuUI(Transform parent)
    {
        var panel = new GameObject("CombatMenu");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 20);
        panelRect.sizeDelta = new Vector2(220, 290);

        var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        SetPanelSprite(panelImg, "Panels/combat_menu", new Color(0.1f, 0.06f, 0.06f, 0.92f));

        var layout = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 14, 20, 14);
        layout.spacing = 8;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var attackBtn = MakeMenuButton("Attack", panel.transform, new Color(0.8f, 0.3f, 0.2f));
        var skillsBtn = MakeMenuButton("Skills", panel.transform, new Color(0.3f, 0.3f, 0.75f));
        var skipBtn = MakeMenuButton("Skip", panel.transform, new Color(0.45f, 0.45f, 0.45f));
        var cancelBtn = MakeMenuButton("Cancel", panel.transform, new Color(0.5f, 0.35f, 0.2f));

        SetButtonNavigation(attackBtn, null, skillsBtn);
        SetButtonNavigation(skillsBtn, attackBtn, skipBtn);
        SetButtonNavigation(skipBtn, skillsBtn, cancelBtn);
        SetButtonNavigation(cancelBtn, skipBtn, null);

        var combatComp = AddComponentByName(panel, "IsoRPG.UI.CombatMenuUI");
        if (combatComp != null)
        {
            var so = new SerializedObject(combatComp);
            SetRef(so, "attackButton", attackBtn.GetComponent<UnityEngine.UI.Button>());
            SetRef(so, "skillsButton", skillsBtn.GetComponent<UnityEngine.UI.Button>());
            SetRef(so, "skipButton", skipBtn.GetComponent<UnityEngine.UI.Button>());
            SetRef(so, "cancelButton", cancelBtn.GetComponent<UnityEngine.UI.Button>());
            so.ApplyModifiedProperties();
        }
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
        panelRect.sizeDelta = new Vector2(250, 320);

        var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        SetPanelSprite(panelImg, "Panels/ability_menu", new Color(0.08f, 0.08f, 0.12f, 0.92f));

        var layout = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 14, 20, 14);
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
        rect.sizeDelta = new Vector2(210, 40);

        var img = entry.AddComponent<UnityEngine.UI.Image>();
        var abilityNormal = LoadUISprite("Buttons/btn_ability_normal");
        if (abilityNormal != null)
        {
            img.sprite = abilityNormal;
            img.type = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.15f, 0.15f, 0.22f, 0.9f);
        }

        var btn = entry.AddComponent<UnityEngine.UI.Button>();
        var abilityHover = LoadUISprite("Buttons/btn_ability_hover");
        var abilityPressed = LoadUISprite("Buttons/btn_ability_pressed");
        var abilityDisabled = LoadUISprite("Buttons/btn_ability_disabled");
        if (abilityNormal != null)
        {
            btn.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            var states = new UnityEngine.UI.SpriteState();
            states.highlightedSprite = abilityHover;
            states.pressedSprite = abilityPressed;
            states.disabledSprite = abilityDisabled;
            states.selectedSprite = abilityHover;
            btn.spriteState = states;
        }

        // Label — offset for gauntlet space
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(entry.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(36, 2);
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

    static void CreateSelectionContextUI(Transform parent)
    {
        var panel = new GameObject("SelectionContext");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 20);
        panelRect.sizeDelta = new Vector2(260, 140);

        var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        SetPanelSprite(panelImg, "Panels/selection_context", new Color(0.12f, 0.2f, 0.4f, 0.92f));

        var layout = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 14, 16, 14);
        layout.spacing = 8;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;

        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(panel.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(216, 30);
        var labelLayout = labelObj.AddComponent<UnityEngine.UI.LayoutElement>();
        labelLayout.preferredHeight = 30;
        var labelTmp = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        labelTmp.text = "Select target";
        labelTmp.fontSize = 20;
        labelTmp.fontStyle = TMPro.FontStyles.Bold;
        labelTmp.alignment = TMPro.TextAlignmentOptions.Center;
        labelTmp.color = Color.white;

        // Sublabel
        var subObj = new GameObject("Sublabel");
        subObj.transform.SetParent(panel.transform, false);
        var subRect = subObj.AddComponent<RectTransform>();
        subRect.sizeDelta = new Vector2(216, 22);
        var subLayout = subObj.AddComponent<UnityEngine.UI.LayoutElement>();
        subLayout.preferredHeight = 22;
        var subTmp = subObj.AddComponent<TMPro.TextMeshProUGUI>();
        subTmp.text = "Right-click or Cancel to go back";
        subTmp.fontSize = 14;
        subTmp.alignment = TMPro.TextAlignmentOptions.Center;
        subTmp.color = new Color(0.7f, 0.7f, 0.7f);

        // Cancel button
        var cancelBtn = MakeMenuButton("Cancel", panel.transform, new Color(0.5f, 0.3f, 0.25f));

        // Add component
        var comp = AddComponentByName(panel, "IsoRPG.UI.SelectionContextUI");
        if (comp != null)
        {
            var so = new SerializedObject(comp);
            SetRef(so, "labelText", labelTmp);
            SetRef(so, "sublabelText", subTmp);
            SetRef(so, "cancelButton", cancelBtn.GetComponent<UnityEngine.UI.Button>());
            SetRef(so, "panelBackground", panelImg);
            so.ApplyModifiedProperties();
        }
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
        // Banner sprite — default to blue, TurnBannerUI swaps to red for enemies at runtime
        SetPanelSprite(img, "Panels/turn_banner_blue", new Color(0.15f, 0.3f, 0.6f, 0.9f));

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

    static GameObject MakeMenuButton(string label, Transform parent, Color fallbackColor,
        string btnType = "standard")
    {
        var btnObj = new GameObject(label + "Button");
        btnObj.transform.SetParent(parent, false);
        var rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = btnType == "ability" ? new Vector2(200, 40) :
                         btnType == "wide" ? new Vector2(240, 50) :
                         new Vector2(180, 42);

        var img = btnObj.AddComponent<UnityEngine.UI.Image>();
        img.preserveAspect = false;

        // Try to load sprite art — fall back to solid color if not found
        var normalSprite = LoadUISprite($"Buttons/btn_{btnType}_normal");
        var hoverSprite = LoadUISprite($"Buttons/btn_{btnType}_hover");
        var pressedSprite = LoadUISprite($"Buttons/btn_{btnType}_pressed");
        var disabledSprite = LoadUISprite($"Buttons/btn_{btnType}_disabled");

        var btn = btnObj.AddComponent<UnityEngine.UI.Button>();

        if (normalSprite != null)
        {
            // Use sprite art with SpriteSwap
            img.sprite = normalSprite;
            img.type = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;

            btn.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            var states = new UnityEngine.UI.SpriteState();
            states.highlightedSprite = hoverSprite;
            states.pressedSprite = pressedSprite;
            states.disabledSprite = disabledSprite;
            states.selectedSprite = hoverSprite;
            btn.spriteState = states;
        }
        else
        {
            // Fallback to solid color
            img.color = fallbackColor;
            var colors = btn.colors;
            colors.normalColor = fallbackColor;
            colors.highlightedColor = fallbackColor * 1.3f;
            colors.pressedColor = fallbackColor * 0.7f;
            colors.selectedColor = fallbackColor * 1.2f;
            colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
            btn.colors = colors;
        }

        // Text — offset right to leave room for gauntlet selector
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(36, 0); // 36px left margin for gauntlet
        textRect.offsetMax = new Vector2(-8, 0);
        var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        tmp.color = new Color(0.96f, 0.91f, 0.79f); // cream #f5e6ca
        tmp.fontStyle = TMPro.FontStyles.Bold;

        return btnObj;
    }

    /// <summary>Load a sprite from Assets/Sprites/UI/. Returns null if not found.</summary>
    static Sprite LoadUISprite(string subpath)
    {
        string path = $"Assets/Sprites/UI/{subpath}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[QuickSetup] Sprite not found: {path}");
        return sprite;
    }

    /// <summary>Set an Image component to use a panel sprite, or fallback color.</summary>
    static void SetPanelSprite(UnityEngine.UI.Image img, string spritePath, Color fallbackColor)
    {
        var sprite = LoadUISprite(spritePath);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
        }
        else
        {
            img.color = fallbackColor;
        }
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
