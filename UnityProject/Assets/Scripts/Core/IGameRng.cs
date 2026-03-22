namespace IsoRPG.Core
{
    /// <summary>
    /// Interface for a seedable, deterministic random number generator.
    /// Used throughout combat for hit rolls, damage variance, and AI decisions.
    ///
    /// The seed can be captured before each command and restored on rewind
    /// to ensure identical results when replaying the same action.
    /// </summary>
    public interface IGameRng
    {
        /// <summary>
        /// Current seed state. Capture this before command execution
        /// for deterministic rewind/replay.
        /// </summary>
        int Seed { get; }

        /// <summary>
        /// Set the seed for deterministic replay. After calling this,
        /// the same sequence of Range/Value/Check calls will produce
        /// identical results.
        /// </summary>
        /// <param name="seed">The seed to restore.</param>
        void SetSeed(int seed);

        /// <summary>
        /// Random integer in [min, max) range (inclusive min, exclusive max).
        /// </summary>
        /// <param name="min">Inclusive lower bound.</param>
        /// <param name="max">Exclusive upper bound.</param>
        /// <returns>Random integer.</returns>
        int Range(int min, int max);

        /// <summary>
        /// Random float in [0, 1) range.
        /// </summary>
        /// <returns>Random float.</returns>
        float Value();

        /// <summary>
        /// Hit check: returns true if a random roll (0-99) is less than the given chance.
        /// Clamps to [<see cref="GameConstants.MinHitChance"/>, <see cref="GameConstants.MaxHitChance"/>].
        /// </summary>
        /// <param name="chancePercent">Success chance as a percentage (0-100).</param>
        /// <returns>True if the roll succeeds.</returns>
        bool Check(int chancePercent);
    }
}
