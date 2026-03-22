using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;
using IsoRPG.Units;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Command to use an ability on a target. Handles damage, healing, hit/miss,
    /// status application, and MP cost. Fully undoable.
    ///
    /// Captures all mutable state before execution for perfect undo:
    /// - Target HP, MP
    /// - Status effect list snapshot
    /// - Caster MP
    /// - RNG seed
    /// </summary>
    public class AttackCommand : ICommand, ICommandMeta
    {
        private readonly UnitInstance _caster;
        private readonly UnitInstance _target;
        private readonly AbilityData _ability;
        private readonly BattleMapData _map;
        private readonly IGameRng _rng;
        private readonly int _rngSeedBefore;

        // Captured pre-state for undo
        private int _targetHPBefore;
        private int _casterMPBefore;
        private List<StatusSnapshot> _targetStatusBefore;
        private EntityId _appliedStatusId;

        // Result (for display)
        private bool _didHit;
        private int _damageDealt;
        private int _healingDone;
        private bool _appliedStatus;

        /// <inheritdoc/>
        public string Description
        {
            get
            {
                if (_ability.IsHealing)
                    return $"{_caster.Name} uses {_ability.AbilityName} on {_target.Name} (heals {_healingDone})";
                if (_didHit)
                    return $"{_caster.Name} uses {_ability.AbilityName} on {_target.Name} ({_damageDealt} damage)";
                return $"{_caster.Name} uses {_ability.AbilityName} on {_target.Name} (miss)";
            }
        }

        /// <inheritdoc/>
        public EntityId ActorId => _caster.Id;

        /// <inheritdoc/>
        public int RngSeedBefore => _rngSeedBefore;

        /// <summary>Whether the attack hit.</summary>
        public bool DidHit => _didHit;

        /// <summary>Damage dealt (0 if miss or healing).</summary>
        public int DamageDealt => _damageDealt;

        /// <summary>Healing done (0 if damage or miss).</summary>
        public int HealingDone => _healingDone;

        /// <summary>
        /// Create an attack command.
        /// </summary>
        /// <param name="caster">Unit using the ability.</param>
        /// <param name="target">Target unit.</param>
        /// <param name="ability">Ability data.</param>
        /// <param name="map">Map data (for elevation/height advantage).</param>
        /// <param name="rng">RNG for hit rolls and status application.</param>
        public AttackCommand(UnitInstance caster, UnitInstance target, AbilityData ability,
            BattleMapData map, IGameRng rng)
        {
            _caster = caster;
            _target = target;
            _ability = ability;
            _map = map;
            _rng = rng;
            _rngSeedBefore = rng?.Seed ?? 0;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            // Capture pre-state
            _targetHPBefore = _target.CurrentHP;
            _casterMPBefore = _caster.CurrentMP;
            _targetStatusBefore = SnapshotStatuses(_target);
            _appliedStatusId = EntityId.None;
            _didHit = false;
            _damageDealt = 0;
            _healingDone = 0;
            _appliedStatus = false;

            // Pay MP cost
            if (_ability.MPCost > 0)
                _caster.SetMP(_caster.CurrentMP - _ability.MPCost);

            // Height advantage
            int heightAdv = _map.GetElevation(_caster.GridPosition) - _map.GetElevation(_target.GridPosition);

            if (_ability.IsHealing)
            {
                // Healing always hits
                _didHit = true;
                _healingDone = DamageCalculator.CalculateHealing(
                    _ability, _caster.Stats, _caster.Faith);

                _target.ApplyHealing(_healingDone);

                GameEvents.HealingDealt.Raise(new HealingDealtArgs(
                    _caster.Id, _target.Id, _healingDone));
            }
            else
            {
                // Hit check
                int hitChance = DamageCalculator.CalculateHitChance(
                    _ability, _caster.Stats, _target.Stats, heightAdv);

                _didHit = _rng?.Check(hitChance) ?? true;

                if (_didHit)
                {
                    // Calculate and apply damage
                    _damageDealt = DamageCalculator.CalculateFinalDamage(
                        _ability, _caster.Stats, _target.Stats,
                        _caster.Brave, _caster.Faith,
                        _target.Faith, heightAdv);

                    _target.ApplyDamage(_damageDealt);

                    GameEvents.DamageDealt.Raise(new DamageDealtArgs(
                        _caster.Id, _target.Id, _damageDealt,
                        _ability.DamageType, false));

                    // Check death
                    if (!_target.IsAlive)
                    {
                        GameEvents.UnitDied.Raise(new UnitDiedArgs(_target.Id, _caster.Id));
                    }

                    // Apply status effect
                    if (_ability.AppliesStatus && _target.IsAlive)
                    {
                        bool statusHit = _rng?.Check(_ability.StatusChance) ?? true;
                        if (statusHit)
                        {
                            var statusInstance = new StatusEffectInstance(
                                _ability.AppliedStatus, _ability.StatusDuration);
                            _target.AddStatus(statusInstance);
                            _appliedStatusId = statusInstance.Id;
                            _appliedStatus = true;

                            GameEvents.StatusApplied.Raise(new StatusAppliedArgs(
                                _target.Id, _ability.AppliedStatus, _ability.StatusDuration));
                        }
                    }
                }
            }

            // Fire ability used event
            GameEvents.AbilityUsed.Raise(new AbilityUsedArgs(
                _caster.Id, _ability.AbilityName, _target.GridPosition));
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // Restore target HP
            _target.SetHP(_targetHPBefore);

            // Restore caster MP
            _caster.SetMP(_casterMPBefore);

            // Remove applied status effect
            if (_appliedStatus && _appliedStatusId.IsValid)
            {
                _target.RemoveStatus(_appliedStatusId);
            }

            // Restore RNG seed
            _rng?.SetSeed(_rngSeedBefore);
        }

        private struct StatusSnapshot
        {
            public EntityId Id;
            public StatusType Type;
            public int Duration;
        }

        private static List<StatusSnapshot> SnapshotStatuses(UnitInstance unit)
        {
            var snapshot = new List<StatusSnapshot>();
            foreach (var s in unit.StatusEffects)
            {
                snapshot.Add(new StatusSnapshot
                {
                    Id = s.Id,
                    Type = s.Type,
                    Duration = s.RemainingDuration
                });
            }
            return snapshot;
        }
    }
}
