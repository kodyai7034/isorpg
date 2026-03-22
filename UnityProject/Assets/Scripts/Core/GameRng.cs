using UnityEngine;

namespace IsoRPG.Core
{
    /// <summary>
    /// Deterministic RNG backed by <see cref="System.Random"/>.
    /// Supports seed capture and restoration for rewind/replay.
    ///
    /// Usage:
    /// <code>
    /// var rng = new GameRng(42);
    /// int saved = rng.Seed;       // capture before command
    /// int roll = rng.Range(1, 7); // use during command
    /// rng.SetSeed(saved);         // restore on rewind
    /// int same = rng.Range(1, 7); // produces identical result
    /// </code>
    /// </summary>
    public class GameRng : IGameRng
    {
        private System.Random _random;

        /// <inheritdoc/>
        public int Seed { get; private set; }

        /// <summary>
        /// Create a new RNG with the given seed.
        /// </summary>
        /// <param name="seed">Initial seed. Use <see cref="System.Environment.TickCount"/> for non-deterministic.</param>
        public GameRng(int seed)
        {
            SetSeed(seed);
        }

        /// <summary>
        /// Create a new RNG with a time-based seed (non-deterministic).
        /// </summary>
        public GameRng() : this(System.Environment.TickCount)
        {
        }

        /// <inheritdoc/>
        public void SetSeed(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }

        /// <inheritdoc/>
        public int Range(int min, int max)
        {
            if (min >= max)
                return min;

            int result = _random.Next(min, max);
            // Advance seed tracking: we can't read System.Random's internal state,
            // so we track by generating a new seed from the current sequence.
            Seed = _random.Next();
            return result;
        }

        /// <inheritdoc/>
        public float Value()
        {
            float result = (float)_random.NextDouble();
            Seed = _random.Next();
            return result;
        }

        /// <inheritdoc/>
        public bool Check(int chancePercent)
        {
            int clamped = Mathf.Clamp(chancePercent, GameConstants.MinHitChance, GameConstants.MaxHitChance);
            int roll = Range(0, 100);
            return roll < clamped;
        }
    }
}
