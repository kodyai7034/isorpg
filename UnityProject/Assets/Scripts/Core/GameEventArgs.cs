using UnityEngine;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Core
{
    /// <summary>Raised when a battle begins.</summary>
    public readonly struct BattleStartedArgs
    {
        public readonly string MapName;
        public readonly int PlayerUnitCount;
        public readonly int EnemyUnitCount;

        public BattleStartedArgs(string mapName, int playerUnitCount, int enemyUnitCount)
        {
            MapName = mapName;
            PlayerUnitCount = playerUnitCount;
            EnemyUnitCount = enemyUnitCount;
        }
    }

    /// <summary>Raised when a battle ends.</summary>
    public readonly struct BattleEndedArgs
    {
        public readonly BattleResult Result;
        public readonly int TurnsElapsed;

        public BattleEndedArgs(BattleResult result, int turnsElapsed)
        {
            Result = result;
            TurnsElapsed = turnsElapsed;
        }
    }

    /// <summary>Raised when a unit's turn begins.</summary>
    public readonly struct TurnStartedArgs
    {
        public readonly EntityId UnitId;
        public readonly int TurnNumber;

        public TurnStartedArgs(EntityId unitId, int turnNumber)
        {
            UnitId = unitId;
            TurnNumber = turnNumber;
        }
    }

    /// <summary>Raised when a unit's turn ends.</summary>
    public readonly struct TurnEndedArgs
    {
        public readonly EntityId UnitId;
        public readonly bool Moved;
        public readonly bool Acted;

        public TurnEndedArgs(EntityId unitId, bool moved, bool acted)
        {
            UnitId = unitId;
            Moved = moved;
            Acted = acted;
        }
    }

    /// <summary>Raised when a unit moves to a new tile.</summary>
    public readonly struct UnitMovedArgs
    {
        public readonly EntityId UnitId;
        public readonly Vector2Int From;
        public readonly Vector2Int To;
        public readonly Vector2Int[] Path;

        public UnitMovedArgs(EntityId unitId, Vector2Int from, Vector2Int to, Vector2Int[] path)
        {
            UnitId = unitId;
            From = from;
            To = to;
            Path = path;
        }
    }

    /// <summary>Raised when damage is dealt to a unit.</summary>
    public readonly struct DamageDealtArgs
    {
        public readonly EntityId AttackerId;
        public readonly EntityId TargetId;
        public readonly int Amount;
        public readonly DamageType Type;
        public readonly bool IsCritical;

        public DamageDealtArgs(EntityId attackerId, EntityId targetId, int amount, DamageType type, bool isCritical)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Amount = amount;
            Type = type;
            IsCritical = isCritical;
        }
    }

    /// <summary>Raised when a unit is healed.</summary>
    public readonly struct HealingDealtArgs
    {
        public readonly EntityId HealerId;
        public readonly EntityId TargetId;
        public readonly int Amount;

        public HealingDealtArgs(EntityId healerId, EntityId targetId, int amount)
        {
            HealerId = healerId;
            TargetId = targetId;
            Amount = amount;
        }
    }

    /// <summary>Raised when a unit dies (HP reaches 0).</summary>
    public readonly struct UnitDiedArgs
    {
        public readonly EntityId UnitId;
        public readonly EntityId KillerId;

        public UnitDiedArgs(EntityId unitId, EntityId killerId)
        {
            UnitId = unitId;
            KillerId = killerId;
        }
    }

    /// <summary>Raised when an ability is used.</summary>
    public readonly struct AbilityUsedArgs
    {
        public readonly EntityId CasterId;
        public readonly string AbilityName;
        public readonly Vector2Int TargetTile;

        public AbilityUsedArgs(EntityId casterId, string abilityName, Vector2Int targetTile)
        {
            CasterId = casterId;
            AbilityName = abilityName;
            TargetTile = targetTile;
        }
    }

    /// <summary>Raised when a status effect is applied to a unit.</summary>
    public readonly struct StatusAppliedArgs
    {
        public readonly EntityId TargetId;
        public readonly StatusType Status;
        public readonly int Duration;

        public StatusAppliedArgs(EntityId targetId, StatusType status, int duration)
        {
            TargetId = targetId;
            Status = status;
            Duration = duration;
        }
    }
}

namespace IsoRPG.Core
{
    /// <summary>Request to show the action menu with context flags.</summary>
    public readonly struct ActionMenuRequestArgs
    {
        public readonly bool CanMove;
        public readonly bool CanAct;
        public readonly bool CanUndo;

        public ActionMenuRequestArgs(bool canMove, bool canAct, bool canUndo)
        {
            CanMove = canMove;
            CanAct = canAct;
            CanUndo = canUndo;
        }
    }

    /// <summary>Request to show the ability menu with available abilities.</summary>
    public readonly struct AbilityMenuRequestArgs
    {
        public readonly AbilityData[] Abilities;
        public readonly int CurrentMP;

        public AbilityMenuRequestArgs(AbilityData[] abilities, int currentMP)
        {
            Abilities = abilities;
            CurrentMP = currentMP;
        }
    }
}
