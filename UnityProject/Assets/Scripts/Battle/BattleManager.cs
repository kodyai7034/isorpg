using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;
using IsoRPG.Units;
using IsoRPG.Battle.States;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Main MonoBehaviour for the battle scene. Bootstraps all subsystems,
    /// spawns units, and runs the battle state machine.
    ///
    /// This is the ONLY MonoBehaviour that owns game logic references.
    /// All other MonoBehaviours (UnitView, TileView, etc.) are view-only.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private IsometricGrid grid;
        [SerializeField] private GameObject unitPrefab;

        [Header("Config")]
        [SerializeField] private BattleMapData mapOverride;
        [SerializeField] private int rngSeed = 42;

        [Header("Abilities (assign in editor)")]
        [SerializeField] private AbilityData[] defaultAbilities;

        private BattleContext _context;
        private StateMachine<BattleContext> _stateMachine;

        private void Start()
        {
            // Clear any stale event listeners from previous scene
            GameEvents.ClearAll();

            // Load map
            var map = mapOverride != null ? mapOverride : MapGenerator.CreateTestMap();
            grid.LoadMap(map);

            // Create default abilities at runtime if none assigned in editor
            if (defaultAbilities == null || defaultAbilities.Length == 0)
                defaultAbilities = CreateDefaultAbilities();

            // Create context
            _context = new BattleContext
            {
                Map = map,
                Grid = grid,
                Registry = new UnitRegistry(),
                AllUnits = new List<UnitInstance>(),
                UnitViews = new Dictionary<EntityId, UnitView>(),
                CommandHistory = new CommandHistory(),
                MovementController = new MovementController(),
                Rng = new GameRng(rngSeed),
                RewindSystem = new RewindSystem(),
                DefaultAbilities = defaultAbilities,
                TurnNumber = 0
            };

            // Spawn units
            SpawnUnits(map);

            // Initialize turn order bar via event
            GameEvents.TurnStarted.Subscribe(_ => { }); // ensure channel exists

            // Start battle
            _stateMachine = new StateMachine<BattleContext>(_context);
            _stateMachine.ChangeState(new DeploymentState());
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        private void OnDestroy()
        {
            GameEvents.ClearAll();
        }

        private void SpawnUnits(BattleMapData map)
        {
            if (map.SpawnZones == null) return;

            // Player units (team 0)
            if (map.SpawnZones.Length > 0)
            {
                var spawns = map.SpawnZones[0].Tiles;
                var names = new[] { "Ramza", "Agrias", "Mustadio" };
                for (int i = 0; i < Mathf.Min(names.Length, spawns.Length); i++)
                {
                    SpawnUnit(names[i], 0, 3, spawns[i]);
                }
            }

            // Enemy units (team 1)
            if (map.SpawnZones.Length > 1)
            {
                var spawns = map.SpawnZones[1].Tiles;
                var names = new[] { "Goblin A", "Goblin B", "Goblin C" };
                for (int i = 0; i < Mathf.Min(names.Length, spawns.Length); i++)
                {
                    SpawnUnit(names[i], 1, 2, spawns[i]);
                }
            }

            Debug.Log($"[BattleManager] Spawned {_context.AllUnits.Count} units with {defaultAbilities.Length} abilities");
        }

        private static Sprite _unitFallbackSprite;

        private void SpawnUnit(string name, int team, int level, Vector2Int position)
        {
            var unit = new UnitInstance(name, team, level, position);
            _context.AllUnits.Add(unit);
            _context.Registry.Register(unit);

            // Create view — works with or without prefab
            var worldPos = IsoMath.GridToWorld(position, _context.Map.GetElevation(position));
            worldPos.y += IsoMath.TileHeightHalf * 0.5f; // offset above tile

            GameObject go;
            if (unitPrefab != null)
            {
                go = Instantiate(unitPrefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                go = new GameObject(name);
                go.AddComponent<SpriteRenderer>();
                go.transform.SetParent(transform);
                go.transform.position = worldPos;
            }

            // Ensure sprite exists
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                if (_unitFallbackSprite == null)
                    _unitFallbackSprite = CreateUnitFallbackSprite();
                sr.sprite = _unitFallbackSprite;
            }

            var view = go.GetComponent<UnitView>();
            if (view == null)
                view = go.AddComponent<UnitView>();

            view.Initialize(unit, _context.Map);
            _context.UnitViews[unit.Id] = view;
        }

        private static Sprite CreateUnitFallbackSprite()
        {
            int s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

            var body = new Color32(200, 200, 220, 255);
            var outline = new Color32(40, 40, 60, 255);

            // Simple character shape
            // Head
            for (int y = 12; y < 15; y++)
                for (int x = 6; x < 10; x++)
                    px[y * s + x] = body;
            // Body
            for (int y = 5; y < 12; y++)
                for (int x = 5; x < 11; x++)
                    px[y * s + x] = body;
            // Legs
            for (int y = 1; y < 5; y++) { px[y * s + 6] = body; px[y * s + 7] = body; px[y * s + 8] = body; px[y * s + 9] = body; }
            // Outline
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    if (px[y * s + x].a == 0) continue;
                    // Check if any neighbor is transparent → outline
                    bool isEdge = false;
                    if (x > 0 && px[y * s + x - 1].a == 0) isEdge = true;
                    if (x < s - 1 && px[y * s + x + 1].a == 0) isEdge = true;
                    if (y > 0 && px[(y - 1) * s + x].a == 0) isEdge = true;
                    if (y < s - 1 && px[(y + 1) * s + x].a == 0) isEdge = true;
                    if (isEdge) px[y * s + x] = outline;
                }

            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.3f), 32);
        }

        /// <summary>
        /// Create the 4 MVP abilities at runtime when no ScriptableObjects are assigned.
        /// These match the spec: Attack, Fire, Cure, Poison Strike.
        /// </summary>
        private static AbilityData[] CreateDefaultAbilities()
        {
            var attack = ScriptableObject.CreateInstance<AbilityData>();
            attack.AbilityName = "Attack";
            attack.Description = "Basic melee attack.";
            attack.SlotType = AbilitySlotType.Action;
            attack.Targeting = AbilityTargetType.Single;
            attack.Range = 1;
            attack.DamageType = DamageType.Physical;
            attack.Power = 10;
            attack.Accuracy = 90;
            attack.RequiresLineOfSight = true;
            attack.hideFlags = HideFlags.HideAndDontSave;

            var fire = ScriptableObject.CreateInstance<AbilityData>();
            fire.AbilityName = "Fire";
            fire.Description = "Ranged fire magic.";
            fire.SlotType = AbilitySlotType.Action;
            fire.Targeting = AbilityTargetType.Single;
            fire.Range = 4;
            fire.DamageType = DamageType.Magical;
            fire.Power = 12;
            fire.Accuracy = 85;
            fire.MPCost = 8;
            fire.RequiresLineOfSight = true;
            fire.hideFlags = HideFlags.HideAndDontSave;

            var cure = ScriptableObject.CreateInstance<AbilityData>();
            cure.AbilityName = "Cure";
            cure.Description = "Heal a single ally.";
            cure.SlotType = AbilitySlotType.Action;
            cure.Targeting = AbilityTargetType.Single;
            cure.Range = 4;
            cure.DamageType = DamageType.Magical;
            cure.Power = 10;
            cure.Accuracy = 100;
            cure.MPCost = 6;
            cure.IsHealing = true;
            cure.RequiresLineOfSight = false;
            cure.hideFlags = HideFlags.HideAndDontSave;

            var poisonStrike = ScriptableObject.CreateInstance<AbilityData>();
            poisonStrike.AbilityName = "Poison Strike";
            poisonStrike.Description = "Melee attack that may poison the target.";
            poisonStrike.SlotType = AbilitySlotType.Action;
            poisonStrike.Targeting = AbilityTargetType.Single;
            poisonStrike.Range = 1;
            poisonStrike.DamageType = DamageType.Physical;
            poisonStrike.Power = 8;
            poisonStrike.Accuracy = 85;
            poisonStrike.MPCost = 4;
            poisonStrike.AppliesStatus = true;
            poisonStrike.AppliedStatus = StatusType.Poison;
            poisonStrike.StatusDuration = 3;
            poisonStrike.StatusChance = 60;
            poisonStrike.RequiresLineOfSight = true;
            poisonStrike.hideFlags = HideFlags.HideAndDontSave;

            return new[] { attack, fire, cure, poisonStrike };
        }
    }
}
