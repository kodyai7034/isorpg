using UnityEngine;
using System.Collections.Generic;
using IsoRPG.Core;
using IsoRPG.Map;
using IsoRPG.Units;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Main MonoBehaviour for the battle scene.
    /// Bootstraps the grid, spawns units, and runs the battle state machine.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [SerializeField] private IsometricGrid grid;
        [SerializeField] private BattleMapData mapOverride;

        private BattleContext _context;
        private StateMachine<BattleContext> _stateMachine;

        private void Start()
        {
            var map = mapOverride != null ? mapOverride : MapGenerator.CreateTestMap();

            grid.LoadMap(map);

            _context = new BattleContext
            {
                Map = map,
                Grid = grid,
                Units = SpawnTestUnits(map)
            };

            _stateMachine = new StateMachine<BattleContext>(_context);

            Debug.Log($"Battle started with {_context.Units.Count} units on {map.mapName}");
            foreach (var unit in _context.Units)
                Debug.Log($"  {unit}");

            // Start with CT advance to find first active unit
            _stateMachine.ChangeState(new CTAdvanceState());
        }

        private void Update()
        {
            _stateMachine.Update();
        }

        private List<UnitInstance> SpawnTestUnits(BattleMapData map)
        {
            var units = new List<UnitInstance>();

            // Player units
            if (map.spawnZones.Length > 0)
            {
                var playerSpawns = map.spawnZones[0].tiles;
                var playerNames = new[] { "Ramza", "Agrias", "Mustadio" };
                for (int i = 0; i < Mathf.Min(playerNames.Length, playerSpawns.Length); i++)
                {
                    units.Add(new UnitInstance(playerNames[i], 0, 1, playerSpawns[i]));
                }
            }

            // Enemy units
            if (map.spawnZones.Length > 1)
            {
                var enemySpawns = map.spawnZones[1].tiles;
                var enemyNames = new[] { "Goblin A", "Goblin B", "Goblin C" };
                for (int i = 0; i < Mathf.Min(enemyNames.Length, enemySpawns.Length); i++)
                {
                    units.Add(new UnitInstance(enemyNames[i], 1, 1, enemySpawns[i]));
                }
            }

            return units;
        }
    }

    /// <summary>
    /// Advance CT until a unit is ready to act.
    /// </summary>
    public class CTAdvanceState : IState<BattleContext>
    {
        public void Enter(BattleContext ctx)
        {
            var activeUnit = CTSystem.AdvanceTick(ctx.Units);
            ctx.ActiveUnit = activeUnit;
            ctx.ActiveUnitMoved = false;
            ctx.ActiveUnitActed = false;

            Debug.Log($"[CT] {activeUnit.Name}'s turn! (CT={activeUnit.CT}, Speed={activeUnit.Stats.Speed})");
        }

        public void Execute(BattleContext ctx)
        {
            // Immediately transition — in the future this will go to
            // SelectActionState (player) or AITurnState (enemy)
            Debug.Log($"[Turn] {ctx.ActiveUnit.Name} (Team {ctx.ActiveUnit.Team}) — waiting for input...");

            // For now, auto-end turn
            CTSystem.ResolveTurn(ctx.ActiveUnit, false, false);
        }

        public void Exit(BattleContext ctx) { }
    }
}
