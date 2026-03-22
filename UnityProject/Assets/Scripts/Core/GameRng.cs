using System;

namespace IsoRPG.Core
{
    /// <summary>
    /// Deterministic RNG using xorshift128 algorithm.
    /// Full internal state is capturable and restorable for rewind/replay.
    ///
    /// Unlike System.Random, the internal state is fully exposed via Seed,
    /// and SetSeed guarantees identical sequences when replayed.
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
        private uint _state;

        /// <inheritdoc/>
        public int Seed => (int)_state;

        /// <summary>
        /// Create a new RNG with the given seed.
        /// </summary>
        /// <param name="seed">Initial seed. Must not be 0 (will be coerced to 1).</param>
        public GameRng(int seed)
        {
            SetSeed(seed);
        }

        /// <summary>
        /// Create a new RNG with a time-based seed (non-deterministic).
        /// </summary>
        public GameRng() : this(Environment.TickCount)
        {
        }

        /// <inheritdoc/>
        public void SetSeed(int seed)
        {
            // Xorshift requires non-zero state
            _state = seed == 0 ? 1u : (uint)seed;
        }

        /// <inheritdoc/>
        public int Range(int min, int max)
        {
            if (min >= max)
                return min;

            uint raw = NextUint();
            return min + (int)(raw % (uint)(max - min));
        }

        /// <inheritdoc/>
        public float Value()
        {
            uint raw = NextUint();
            return (raw & 0x7FFFFFu) / (float)0x800000u; // 23-bit mantissa → [0, 1)
        }

        /// <inheritdoc/>
        public bool Check(int chancePercent)
        {
            int clamped = Math.Clamp(chancePercent, GameConstants.MinHitChance, GameConstants.MaxHitChance);
            int roll = Range(0, 100);
            return roll < clamped;
        }

        /// <summary>
        /// Xorshift32 — fast, minimal-state PRNG with full state extractability.
        /// Period: 2^32 - 1.
        /// </summary>
        private uint NextUint()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }
    }
}
