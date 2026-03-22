using NUnit.Framework;
using IsoRPG.Core;

namespace IsoRPG.Tests
{
    public class GameRngTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var rng1 = new GameRng(42);
            var rng2 = new GameRng(42);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(rng1.Range(0, 1000), rng2.Range(0, 1000),
                    $"Diverged at iteration {i}");
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var rng1 = new GameRng(42);
            var rng2 = new GameRng(999);

            bool anyDifferent = false;
            for (int i = 0; i < 20; i++)
            {
                if (rng1.Range(0, 10000) != rng2.Range(0, 10000))
                {
                    anyDifferent = true;
                    break;
                }
            }
            Assert.IsTrue(anyDifferent, "Two different seeds should produce different sequences");
        }

        [Test]
        public void SetSeed_ResetsSequence()
        {
            var rng = new GameRng(42);
            int first = rng.Range(0, 1000);

            rng.SetSeed(42);
            int replayed = rng.Range(0, 1000);

            Assert.AreEqual(first, replayed);
        }

        [Test]
        public void Range_RespectsMinMax()
        {
            var rng = new GameRng(42);
            for (int i = 0; i < 1000; i++)
            {
                int val = rng.Range(5, 10);
                Assert.GreaterOrEqual(val, 5);
                Assert.Less(val, 10);
            }
        }

        [Test]
        public void Range_MinEqualsMax_ReturnsMin()
        {
            var rng = new GameRng(42);
            Assert.AreEqual(7, rng.Range(7, 7));
        }

        [Test]
        public void Value_ReturnsBetweenZeroAndOne()
        {
            var rng = new GameRng(42);
            for (int i = 0; i < 1000; i++)
            {
                float val = rng.Value();
                Assert.GreaterOrEqual(val, 0f);
                Assert.Less(val, 1f);
            }
        }

        [Test]
        public void Check_ZeroPercent_ClampedToMinHitChance()
        {
            // With MinHitChance = 5, a 0% check should still have 5% chance
            var rng = new GameRng(42);
            int hits = 0;
            for (int i = 0; i < 10000; i++)
            {
                rng.SetSeed(i);
                if (rng.Check(0)) hits++;
            }
            // ~5% of 10000 = ~500, allow wide margin
            Assert.Greater(hits, 100, "0% check should still hit ~5% of the time (clamped to MinHitChance)");
            Assert.Less(hits, 1500);
        }

        [Test]
        public void Check_HundredPercent_ClampedToMaxHitChance()
        {
            // With MaxHitChance = 95, a 100% check should still miss ~5% of the time
            var rng = new GameRng(42);
            int misses = 0;
            for (int i = 0; i < 10000; i++)
            {
                rng.SetSeed(i);
                if (!rng.Check(100)) misses++;
            }
            Assert.Greater(misses, 100, "100% check should still miss ~5% of the time (clamped to MaxHitChance)");
            Assert.Less(misses, 1500);
        }

        [Test]
        public void Check_FiftyPercent_RoughlyHalf()
        {
            var rng = new GameRng(42);
            int hits = 0;
            for (int i = 0; i < 10000; i++)
            {
                rng.SetSeed(i);
                if (rng.Check(50)) hits++;
            }
            // Should be roughly 50% — allow ±10% margin
            Assert.Greater(hits, 4000);
            Assert.Less(hits, 6000);
        }
    }
}
