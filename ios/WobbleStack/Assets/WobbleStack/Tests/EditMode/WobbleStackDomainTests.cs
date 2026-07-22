using NUnit.Framework;

namespace WobbleStack.Domain.Tests
{
    public sealed class WobbleStackDomainTests
    {
        [Test]
        public void DifficultyForceRangesDoNotOverlap()
        {
            DifficultyProfile gentle = WobbleStackRules.GetDifficultyProfile(DifficultyId.Gentle);
            DifficultyProfile normal = WobbleStackRules.GetDifficultyProfile(DifficultyId.Normal);
            DifficultyProfile wild = WobbleStackRules.GetDifficultyProfile(DifficultyId.Wild);

            Assert.That(gentle.ForceMax, Is.LessThan(normal.ForceMin));
            Assert.That(normal.ForceMax, Is.LessThan(wild.ForceMin));
        }

        [Test]
        public void SeededSchedulerRepeatsDeterministicSequence()
        {
            GustScheduler first = new GustScheduler(42u, DifficultyId.Normal);
            GustScheduler second = new GustScheduler(42u, DifficultyId.Normal);

            GustSample firstA = first.Next(0f);
            GustSample secondA = second.Next(0f);
            GustSample firstB = first.Next(firstA.EndsAtSeconds);
            GustSample secondB = second.Next(secondA.EndsAtSeconds);

            Assert.That(firstA.RestSeconds, Is.EqualTo(secondA.RestSeconds).Within(0.000001f));
            Assert.That(firstA.DurationSeconds, Is.EqualTo(secondA.DurationSeconds).Within(0.000001f));
            Assert.That(firstA.Force, Is.EqualTo(secondA.Force).Within(0.000001f));
            Assert.That(firstA.Direction, Is.EqualTo(secondA.Direction));
            Assert.That(firstA.StartsAtSeconds, Is.EqualTo(secondA.StartsAtSeconds).Within(0.000001f));
            Assert.That(firstB.RestSeconds, Is.EqualTo(secondB.RestSeconds).Within(0.000001f));
            Assert.That(firstB.DurationSeconds, Is.EqualTo(secondB.DurationSeconds).Within(0.000001f));
            Assert.That(firstB.Force, Is.EqualTo(secondB.Force).Within(0.000001f));
            Assert.That(firstB.Direction, Is.EqualTo(secondB.Direction));
            Assert.That(firstB.StartsAtSeconds, Is.EqualTo(secondB.StartsAtSeconds).Within(0.000001f));
        }

        [Test]
        public void SeededSchedulerKeepsSamplesInsideDifficultyBounds()
        {
            DifficultyProfile profile = WobbleStackRules.GetDifficultyProfile(DifficultyId.Wild);
            GustScheduler scheduler = new GustScheduler(7907u, DifficultyId.Wild);

            GustSample gust = scheduler.Next(11.5f);

            Assert.That(gust.RestSeconds, Is.InRange(profile.RestMin, profile.RestMax));
            Assert.That(gust.DurationSeconds, Is.InRange(profile.DurationMin, profile.DurationMax));
            Assert.That(gust.Force, Is.InRange(profile.ForceMin, profile.ForceMax));
            Assert.That(gust.Direction == -1 || gust.Direction == 1, Is.True);
            Assert.That(gust.StartsAtSeconds, Is.EqualTo(11.5f + gust.RestSeconds).Within(0.000001f));
            Assert.That(gust.EndsAtSeconds, Is.EqualTo(gust.StartsAtSeconds + gust.DurationSeconds).Within(0.000001f));
        }

        [Test]
        public void GustEnvelopeEasesInHoldsAndEasesOut()
        {
            float start = WobbleStackRules.GetGustEnvelope(0f);
            float early = WobbleStackRules.GetGustEnvelope(0.1f);
            float mid = WobbleStackRules.GetGustEnvelope(0.4f);
            float hold = WobbleStackRules.GetGustEnvelope(0.5f);
            float late = WobbleStackRules.GetGustEnvelope(0.9f);
            float end = WobbleStackRules.GetGustEnvelope(1f);

            Assert.That(start, Is.EqualTo(0f));
            Assert.That(early, Is.LessThan(mid));
            Assert.That(mid, Is.EqualTo(1f).Within(0.000001f));
            Assert.That(hold, Is.EqualTo(1f).Within(0.000001f));
            Assert.That(late, Is.LessThan(hold));
            Assert.That(end, Is.EqualTo(0f));
        }

        [Test]
        public void CounterTiltImprovesLeftwardAndRightwardGusts()
        {
            float force = 0.00009f;
            float counterAngle = WobbleStackRules.GetRequiredCounterAngle(force, WobbleStackRules.GravityScale);

            float leftNeutral = WobbleStackRules.GetEffectiveGustAcceleration(
                force,
                -1,
                1f,
                0f,
                WobbleStackRules.GravityScale,
                WobbleStackRules.CounterAuthority);
            float leftCorrect = WobbleStackRules.GetEffectiveGustAcceleration(
                force,
                -1,
                1f,
                counterAngle,
                WobbleStackRules.GravityScale,
                WobbleStackRules.CounterAuthority);
            float leftWrong = WobbleStackRules.GetEffectiveGustAcceleration(
                force,
                -1,
                1f,
                -counterAngle,
                WobbleStackRules.GravityScale,
                WobbleStackRules.CounterAuthority);
            float rightNeutral = WobbleStackRules.GetEffectiveGustAcceleration(
                force,
                1,
                1f,
                0f,
                WobbleStackRules.GravityScale,
                WobbleStackRules.CounterAuthority);
            float rightCorrect = WobbleStackRules.GetEffectiveGustAcceleration(
                force,
                1,
                1f,
                -counterAngle,
                WobbleStackRules.GravityScale,
                WobbleStackRules.CounterAuthority);
            float rightWrong = WobbleStackRules.GetEffectiveGustAcceleration(
                force,
                1,
                1f,
                counterAngle,
                WobbleStackRules.GravityScale,
                WobbleStackRules.CounterAuthority);

            Assert.That(leftNeutral, Is.EqualTo(-force).Within(0.0000001f));
            Assert.That(rightNeutral, Is.EqualTo(force).Within(0.0000001f));
            Assert.That(System.Math.Abs(leftCorrect), Is.LessThan(System.Math.Abs(leftNeutral)));
            Assert.That(System.Math.Abs(rightCorrect), Is.LessThan(System.Math.Abs(rightNeutral)));
            Assert.That(System.Math.Abs(leftWrong), Is.GreaterThan(System.Math.Abs(leftNeutral)));
            Assert.That(System.Math.Abs(rightWrong), Is.GreaterThan(System.Math.Abs(rightNeutral)));
            Assert.That(System.Math.Abs(leftCorrect), Is.LessThan(System.Math.Abs(leftWrong)));
            Assert.That(System.Math.Abs(rightCorrect), Is.LessThan(System.Math.Abs(rightWrong)));
            Assert.That(
                WobbleStackRules.GetEffectiveGustAcceleration(
                    force,
                    0,
                    1f,
                    0f,
                    WobbleStackRules.GravityScale,
                    WobbleStackRules.CounterAuthority),
                Is.EqualTo(0f));
        }

        [Test]
        public void CreatureSetupStaysInsideThreeToFive()
        {
            Assert.That(WobbleStackRules.ClampCreatureCount(1), Is.EqualTo(3));
            Assert.That(WobbleStackRules.ClampCreatureCount(3), Is.EqualTo(3));
            Assert.That(WobbleStackRules.ClampCreatureCount(4), Is.EqualTo(4));
            Assert.That(WobbleStackRules.ClampCreatureCount(7), Is.EqualTo(5));
        }

        [Test]
        public void GamePhaseTransitionsStayWithinPlayableFlow()
        {
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Ready, GamePhase.Playing), Is.True);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Playing, GamePhase.Paused), Is.True);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Playing, GamePhase.Failing), Is.True);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Paused, GamePhase.Playing), Is.True);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Paused, GamePhase.Ready), Is.True);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Failing, GamePhase.Results), Is.True);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Results, GamePhase.Ready), Is.True);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Results, GamePhase.Playing), Is.True);

            Assert.That(WobbleStackRules.CanTransition(GamePhase.Ready, GamePhase.Results), Is.False);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Playing, GamePhase.Results), Is.False);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Failing, GamePhase.Playing), Is.False);
            Assert.That(WobbleStackRules.CanTransition(GamePhase.Paused, GamePhase.Results), Is.False);
        }
    }
}
