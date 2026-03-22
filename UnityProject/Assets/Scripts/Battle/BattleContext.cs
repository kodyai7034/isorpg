using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Map;
using IsoRPG.Units;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Shared context passed to all battle states.
    /// </summary>
    public class BattleContext
    {
        public BattleMapData Map { get; set; }
        public IsometricGrid Grid { get; set; }
        public List<UnitInstance> Units { get; set; } = new();
        public UnitInstance ActiveUnit { get; set; }
        public bool ActiveUnitMoved { get; set; }
        public bool ActiveUnitActed { get; set; }

        public List<UnitInstance> GetTeamUnits(int team)
        {
            return Units.FindAll(u => u.Team == team && u.IsAlive);
        }

        public UnitInstance GetUnitAt(Vector2Int pos)
        {
            return Units.Find(u => u.GridPosition == pos && u.IsAlive);
        }
    }
}
