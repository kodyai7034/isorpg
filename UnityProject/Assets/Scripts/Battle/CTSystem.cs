using System.Collections.Generic;
using System.Linq;
using IsoRPG.Units;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Final Fantasy Tactics-style Charge Time system.
    /// Pure logic — no MonoBehaviour dependency.
    /// </summary>
    public static class CTSystem
    {
        public const int TurnThreshold = GameConstants.CTThreshold;
        public const int MaxCTAfterAction = GameConstants.MaxCTAfterAction;

        /// <summary>
        /// Advance CT for all living units until someone reaches the threshold.
        /// Returns the unit that gets the active turn.
        /// </summary>
        public static UnitInstance AdvanceTick(List<UnitInstance> units)
        {
            var living = units.Where(u => u.IsAlive).ToList();
            if (living.Count == 0) return null;

            for (int tick = 0; tick < GameConstants.CTTickSafetyLimit; tick++)
            {
                foreach (var unit in living)
                {
                    int speed = unit.Stats.Speed;

                    // Apply Haste/Slow status modifiers
                    foreach (var status in unit.StatusEffects)
                    {
                        float mod = status.GetModifier(Units.StatusModifierType.Speed);
                        if (mod != 1.0f)
                            speed = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.FloorToInt(speed * mod));
                    }

                    unit.CT += speed;
                }

                var ready = living
                    .Where(u => u.CT >= GameConstants.CTThreshold)
                    .OrderByDescending(u => u.CT)
                    .ThenByDescending(u => u.Stats.Speed)
                    .FirstOrDefault();

                if (ready != null)
                    return ready;
            }

            UnityEngine.Debug.LogError("[CTSystem] Safety limit reached — no unit gained a turn. Check Speed values.");
            return living.FirstOrDefault();
        }

        /// <summary>
        /// Reduce CT after a unit takes their turn.
        /// </summary>
        public static void ResolveTurn(UnitInstance unit, bool moved, bool acted)
        {
            int ctCost;
            if (moved && acted)
                ctCost = 100;
            else if (moved || acted)
                ctCost = 80;
            else
                ctCost = 60; // waited

            unit.CT -= ctCost;

            if (unit.CT > MaxCTAfterAction)
                unit.CT = MaxCTAfterAction;

            if (unit.CT < 0)
                unit.CT = 0;
        }

        /// <summary>
        /// Preview the next N turns without modifying state.
        /// Returns ordered list of units who will act.
        /// </summary>
        public static List<UnitInstance> PreviewTurnOrder(List<UnitInstance> units, int count = 10)
        {
            var living = units.Where(u => u.IsAlive).ToList();
            if (living.Count == 0) return new List<UnitInstance>();

            // Snapshot CT values
            var ctSnapshot = living.ToDictionary(u => u, u => u.CT);
            var result = new List<UnitInstance>();

            for (int i = 0; i < count; i++)
            {
                UnitInstance next = null;
                while (next == null)
                {
                    foreach (var unit in living)
                        ctSnapshot[unit] += unit.Stats.Speed;

                    next = living
                        .Where(u => ctSnapshot[u] >= TurnThreshold)
                        .OrderByDescending(u => ctSnapshot[u])
                        .ThenByDescending(u => u.Stats.Speed)
                        .FirstOrDefault();
                }

                result.Add(next);
                ctSnapshot[next] -= 100;
                if (ctSnapshot[next] > MaxCTAfterAction)
                    ctSnapshot[next] = MaxCTAfterAction;
            }

            return result;
        }
    }
}
