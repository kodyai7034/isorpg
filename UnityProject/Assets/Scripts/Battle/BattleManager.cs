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

        private BattleContext _context;
        private StateMachine<BattleContext> _stateMachine;

        private void Start()
        {
            // Clear any stale event listeners from previous scene
            GameEvents.ClearAll();

            // Load map
            var map = mapOverride != null ? mapOverride : MapGenerator.CreateTestMap();
            grid.LoadMap(map);

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
                TurnNumber = 0
            };

            // Spawn units
            SpawnUnits(map);

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

            Debug.Log($"[BattleManager] Spawned {_context.AllUnits.Count} units");
        }

        private void SpawnUnit(string name, int team, int level, Vector2Int position)
        {
            var unit = new UnitInstance(name, team, level, position);
            _context.AllUnits.Add(unit);
            _context.Registry.Register(unit);

            // Create view
            if (unitPrefab != null)
            {
                var worldPos = IsoMath.GridToWorld(position, _context.Map.GetElevation(position));
                var go = Instantiate(unitPrefab, worldPos, Quaternion.identity, transform);

                var view = go.GetComponent<UnitView>();
                if (view == null)
                    view = go.AddComponent<UnitView>();

                view.Initialize(unit, _context.Map);
                _context.UnitViews[unit.Id] = view;
            }
        }
    }
}
