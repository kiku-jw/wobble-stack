using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WobbleStack.Domain;

namespace WobbleStack.Runtime.Tests
{
    internal static class PlayModeBootstrapOverrides
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PrepareBootstrapPreferences()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt("wobble.ios.difficulty", 0);
            PlayerPrefs.SetInt("wobble.ios.creature-count", 99);
            PlayerPrefs.Save();
        }
    }

    public sealed class WobbleStackRuntimePlayModeTests
    {
        [UnityTest]
        [Order(1)]
        public IEnumerator BootstrapBuildsPortraitRuntimeScene()
        {
            yield return WaitForBootstrap();

            Assert.That(Screen.orientation, Is.EqualTo(ScreenOrientation.Portrait));
            Assert.That(GameObject.Find("Wobble Stack Game"), Is.Not.Null);
            Assert.That(GameObject.Find("Game Camera"), Is.Not.Null);
            Assert.That(GameObject.Find("World"), Is.Not.Null);
            Assert.That(GameObject.Find("Game UI"), Is.Not.Null);
            Assert.That(GameObject.Find("Start Overlay"), Is.Not.Null);
            Assert.That(GameObject.Find("Start Overlay").activeSelf, Is.True);
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            Assert.That(listeners.Length, Is.EqualTo(1));
            Assert.That(listeners[0].enabled, Is.True);
            Assert.That(SceneManager.GetActiveScene().isLoaded, Is.True);
        }

        [UnityTest]
        [Order(2)]
        public IEnumerator SetupLoadsMaximumCreatureCountWhenSavedValueExceedsRange()
        {
            yield return WaitForBootstrap();

            Assert.That(GetCreatureBodies().Count, Is.EqualTo(WobbleStackRules.MaxCreatureCount));
            Assert.That(GetButtonLabel("Creature Count"), Is.EqualTo("5 FRIENDS"));
        }

        [UnityTest]
        [Order(3)]
        public IEnumerator SetupCreatureCountButtonStaysInsideThreeToFive()
        {
            yield return WaitForBootstrap();

            Button countButton = FindRequiredComponent<Button>("Creature Count");

            yield return Click(countButton);
            Assert.That(GetCreatureBodies().Count, Is.EqualTo(3));
            Assert.That(GetButtonLabel("Creature Count"), Is.EqualTo("3 FRIENDS"));

            yield return Click(countButton);
            Assert.That(GetCreatureBodies().Count, Is.EqualTo(4));
            Assert.That(GetButtonLabel("Creature Count"), Is.EqualTo("4 FRIENDS"));

            yield return Click(countButton);
            Assert.That(GetCreatureBodies().Count, Is.EqualTo(5));
            Assert.That(GetButtonLabel("Creature Count"), Is.EqualTo("5 FRIENDS"));
        }

        [UnityTest]
        [Order(4)]
        public IEnumerator ReadyStackUsesRoundedBeamAndCompactColliderContacts()
        {
            yield return WaitForBootstrap();

            GameObject beam = GameObject.Find("Seesaw Beam");
            Assert.That(beam, Is.Not.Null);
            CapsuleCollider2D beamCollider = beam.GetComponent<CapsuleCollider2D>();
            SpriteRenderer beamRenderer = beam.GetComponent<SpriteRenderer>();
            Assert.That(beamCollider, Is.Not.Null);
            Assert.That(beamCollider.direction, Is.EqualTo(CapsuleDirection2D.Horizontal));
            Assert.That(beamCollider.bounds.size.x, Is.GreaterThan(beamRenderer.bounds.size.x * 0.94f));
            Assert.That(beamCollider.bounds.size.y, Is.InRange(
                beamRenderer.bounds.size.y * 0.64f,
                beamRenderer.bounds.size.y * 0.78f));

            List<Collider2D> colliders = GetCreatureColliders();
            Assert.That(colliders.Count, Is.EqualTo(WobbleStackRules.MaxCreatureCount));
            for (int index = 1; index < colliders.Count; index += 1)
            {
                float gap = colliders[index].bounds.min.y - colliders[index - 1].bounds.max.y;
                Assert.That(gap, Is.InRange(-0.08f, 0.02f), $"Creature contact {index} had a visible collider gap.");
            }
        }

        [UnityTest]
        [Order(5)]
        public IEnumerator FacesProgressFromCalmToWindToImpact()
        {
            yield return WaitForBootstrap();

            CreatureBody creature = Object.FindFirstObjectByType<CreatureBody>();
            Assert.That(creature, Is.Not.Null);
            SpriteRenderer renderer = creature.GetComponentInChildren<SpriteRenderer>();
            Assert.That(renderer.sprite.name, Does.StartWith("character-calm-"));

            creature.SetWind(0.32f);
            yield return null;
            Assert.That(renderer.sprite.name, Does.StartWith("character-"));
            Assert.That(renderer.sprite.name, Does.Not.Contain("calm"));
            Assert.That(renderer.sprite.name, Does.Not.Contain("impact"));

            creature.ShowImpactReaction();
            yield return null;
            Assert.That(renderer.sprite.name, Does.StartWith("character-impact-"));
        }

        [UnityTest]
        [Order(6)]
        public IEnumerator WindStreaksAreCoolBlueAndShowTheirTravelDirection()
        {
            yield return WaitForBootstrap();

            WindStreaks wind = Object.FindFirstObjectByType<WindStreaks>();
            Assert.That(wind, Is.Not.Null);
            wind.SetWind(1, 0.25f);
            wind.Refresh(1f);

            LineRenderer line = FindEnabledWindLine(wind);
            Assert.That(line.startColor.b, Is.GreaterThan(line.startColor.r));
            Assert.That(line.startColor.a, Is.GreaterThan(0.2f));
            Assert.That(line.GetPosition(0).x, Is.GreaterThan(line.GetPosition(1).x));

            wind.SetWind(-1, 0.25f);
            wind.Refresh(1f);
            line = FindEnabledWindLine(wind);
            Assert.That(line.GetPosition(0).x, Is.LessThan(line.GetPosition(1).x));
        }

        [UnityTest]
        [Order(7)]
        public IEnumerator DynamicTowerHasNoHiddenConstraintsAndReachesTheFirstGust()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);

            game.ConfigureGameplayProbe(0f, 1, 0f, 5, 1.4f);
            Rigidbody2D[] bodies = GetCreatureBodies().ToArray();
            Assert.That(bodies.Length, Is.EqualTo(5));
            Assert.That(Object.FindObjectsByType<Joint2D>(FindObjectsSortMode.None), Is.Empty);
            foreach (Rigidbody2D body in bodies)
            {
                Assert.That(body.freezeRotation, Is.False, $"{body.name} had hidden rotation locking.");
            }

            Time.timeScale = 6f;
            for (int step = 0; step < 210; step += 1)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(
                    game.GetGameplayProbePhase(),
                    Is.EqualTo(GamePhase.Playing),
                    $"Free tower fell before a gust could teach the control at step {step}.");
            }

            Time.timeScale = 1f;
        }

        [UnityTest]
        [Order(8)]
        public IEnumerator HumanSideHoldMovesTheBeamBeforeWindStarts()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);
            float touchAmount = WobbleStackRules.GetControlAmount(0.82f);
            DifficultyProfile gentle = WobbleStackRules.GetDifficultyProfile(DifficultyId.Gentle);
            game.ConfigureGameplayProbe(gentle.ForceMin, 1, touchAmount, 3, 1.4f);

            for (int step = 0; step < 5; step += 1)
            {
                yield return new WaitForFixedUpdate();
            }

            Transform beam = GameObject.Find("Seesaw Beam").transform;
            Vector3 leftEnd = beam.TransformPoint(Vector3.left);
            Vector3 rightEnd = beam.TransformPoint(Vector3.right);
            Assert.That(beam.eulerAngles.z, Is.GreaterThan(3f));
            Assert.That(rightEnd.y, Is.GreaterThan(leftEnd.y), "Touching right must visibly raise the right beam end.");
            Assert.That(game.GetGameplayProbePhase(), Is.EqualTo(GamePhase.Playing));
        }

        [UnityTest]
        [Order(9)]
        public IEnumerator DelayedImpreciseCorrectionSurvivesFirstGustMatrix()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);

            foreach (DifficultyId difficulty in new[] { DifficultyId.Gentle, DifficultyId.Normal, DifficultyId.Wild })
            {
                DifficultyProfile profile = WobbleStackRules.GetDifficultyProfile(difficulty);
                foreach (int creatureCount in new[] { 3, 5 })
                {
                    foreach (int direction in new[] { -1, 1 })
                    {
                        TowerMeasurement measurement = default;
                        yield return MeasureDelayedTower(
                            game,
                            profile.ForceMax,
                            direction,
                            creatureCount,
                            profile.DurationMax,
                            value => measurement = value);

                        Assert.That(
                            measurement.Completed,
                            Is.True,
                            $"{difficulty}, {creatureCount} creatures, direction {direction} did not survive a delayed imprecise correction: {measurement}.");
                    }
                }
            }
        }

        [UnityTest]
        [Order(10)]
        public IEnumerator CorrectCounterTiltSurvivesWorstFirstGustAcrossDifficultiesAndTowerSizes()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);

            foreach (DifficultyId difficulty in new[] { DifficultyId.Gentle, DifficultyId.Normal, DifficultyId.Wild })
            {
                DifficultyProfile profile = WobbleStackRules.GetDifficultyProfile(difficulty);

                foreach (int creatureCount in new[] { 3, 5 })
                {
                    foreach (int direction in new[] { -1, 1 })
                    {
                        float humanHold = GetHumanHoldAmount(direction);
                        TowerMeasurement measurement = default;
                        yield return MeasureTower(
                            game,
                            profile.ForceMax,
                            direction,
                            humanHold,
                            creatureCount,
                            profile.DurationMax,
                            value => measurement = value);

                        Assert.That(
                            measurement.Completed,
                            Is.True,
                            $"{difficulty}, {creatureCount} creatures, direction {direction} failed the complete first gust: {measurement}.");
                    }
                }
            }
        }

        [UnityTest]
        [Order(11)]
        public IEnumerator CorrectCounterTiltOutperformsNeutralAndWrongTiltOnTheSameGust()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);
            DifficultyProfile profile = WobbleStackRules.GetDifficultyProfile(DifficultyId.Normal);

            foreach (int direction in new[] { -1, 1 })
            {
                float humanHold = GetHumanHoldAmount(direction);
                TowerMeasurement neutral = default;
                yield return MeasureTower(
                    game,
                    profile.ForceMax,
                    direction,
                    0f,
                    5,
                    profile.DurationMax,
                    value => neutral = value);
                TowerMeasurement correct = default;
                yield return MeasureTower(
                    game,
                    profile.ForceMax,
                    direction,
                    humanHold,
                    5,
                    profile.DurationMax,
                    value => correct = value);
                TowerMeasurement wrong = default;
                yield return MeasureTower(
                    game,
                    profile.ForceMax,
                    direction,
                    -humanHold,
                    5,
                    profile.DurationMax,
                    value => wrong = value);

                AssertTowerOrdering(neutral, correct, wrong, direction);
            }
        }

        [UnityTest]
        [Order(12)]
        public IEnumerator NeutralAndWrongInputCollapseUnderTheStrongestWildGust()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);
            DifficultyProfile profile = WobbleStackRules.GetDifficultyProfile(DifficultyId.Wild);
            foreach (int direction in new[] { -1, 1 })
            {
                float humanHold = GetHumanHoldAmount(direction);
                TowerMeasurement neutral = default;
                yield return MeasureTower(
                    game,
                    profile.ForceMax,
                    direction,
                    0f,
                    5,
                    profile.DurationMax,
                    value => neutral = value);
                TowerMeasurement correct = default;
                yield return MeasureTower(
                    game,
                    profile.ForceMax,
                    direction,
                    humanHold,
                    5,
                    profile.DurationMax,
                    value => correct = value);
                TowerMeasurement wrong = default;
                yield return MeasureTower(
                    game,
                    profile.ForceMax,
                    direction,
                    -humanHold,
                    5,
                    profile.DurationMax,
                    value => wrong = value);

                Assert.That(neutral.Completed, Is.False, $"Neutral input was immortal under {direction} Wild wind: {neutral}.");
                Assert.That(wrong.Completed, Is.False, $"Wrong input was immortal under {direction} Wild wind: {wrong}.");
                Assert.That(correct.Completed, Is.True, $"Correct input did not survive {direction} Wild wind: {correct}.");
                Assert.That(correct.SurvivedSteps, Is.GreaterThan(neutral.SurvivedSteps));
                Assert.That(correct.SurvivedSteps, Is.GreaterThan(wrong.SurvivedSteps));
            }
        }

        [UnityTest]
        [Order(13)]
        public IEnumerator FailureBeatFreezesDuringApplicationInterruption()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);

            game.ConfigureGameplayProbe(0.00009f, 1, 0f, 5);
            CreatureBody creature = Object.FindFirstObjectByType<CreatureBody>();
            Assert.That(creature, Is.Not.Null);
            game.RegisterImpact(creature, creature.Body.position);
            Assert.That(game.GetGameplayProbePhase(), Is.EqualTo(GamePhase.Failing));

            game.SendMessage("OnApplicationPause", true);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(AudioListener.pause, Is.True);

            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(game.GetGameplayProbePhase(), Is.EqualTo(GamePhase.Failing));

            game.SendMessage("OnApplicationPause", false);
            Assert.That(game.GetGameplayProbePhase(), Is.EqualTo(GamePhase.Failing));
            Assert.That(Time.timeScale, Is.GreaterThan(0f));
            Assert.That(AudioListener.pause, Is.False);

            Time.timeScale = 1f;
        }

        private static IEnumerator WaitForBootstrap()
        {
            int frames = 0;
            while (GameObject.Find("Wobble Stack Game") == null && frames < 120)
            {
                frames += 1;
                yield return null;
            }

            Assert.That(GameObject.Find("Wobble Stack Game"), Is.Not.Null, "Runtime bootstrap did not create the game object.");
        }

        private static IEnumerator Click(Button button)
        {
            button.onClick.Invoke();
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        private static float GetHumanHoldAmount(int direction)
        {
            float normalizedScreenX = direction < 0 ? 0.22f : 0.78f;
            return WobbleStackRules.GetControlAmount(normalizedScreenX);
        }

        private static IEnumerator MeasureTower(
            WobbleStackGame game,
            float force,
            int direction,
            float controlAmount,
            int creatureCount,
            float durationSeconds,
            System.Action<TowerMeasurement> onComplete)
        {
            int totalSteps = Mathf.CeilToInt((0.9f + durationSeconds) / Time.fixedDeltaTime);
            game.ConfigureGameplayProbe(force, direction, controlAmount, creatureCount, durationSeconds);
            Time.timeScale = 6f;
            float maxDrift = 0f;
            int survivedSteps = 0;
            for (int step = 0; step < totalSteps; step += 1)
            {
                yield return new WaitForFixedUpdate();
                maxDrift = Mathf.Max(maxDrift, game.GetGameplayProbeMaxDrift());
                if (game.GetGameplayProbePhase() != GamePhase.Playing)
                {
                    break;
                }

                survivedSteps += 1;
            }

            Time.timeScale = 1f;
            List<Rigidbody2D> bodies = GetCreatureBodies();
            float finalMeanX = 0f;
            foreach (Rigidbody2D body in bodies)
            {
                finalMeanX += body.position.x;
            }

            finalMeanX = bodies.Count == 0 ? 0f : finalMeanX / bodies.Count;
            TowerMeasurement measurement = new TowerMeasurement(
                maxDrift,
                finalMeanX,
                survivedSteps,
                totalSteps,
                game.GetGameplayProbePhase());
            onComplete(measurement);
        }

        private static IEnumerator MeasureDelayedTower(
            WobbleStackGame game,
            float force,
            int direction,
            int creatureCount,
            float durationSeconds,
            System.Action<TowerMeasurement> onComplete)
        {
            int totalSteps = Mathf.CeilToInt((0.9f + durationSeconds) / Time.fixedDeltaTime);
            int reactionSteps = Mathf.CeilToInt((0.7f + 0.35f) / Time.fixedDeltaTime);
            float normalizedScreenX = direction < 0 ? 0.32f : 0.68f;
            float impreciseHold = WobbleStackRules.GetControlAmount(normalizedScreenX);
            game.ConfigureGameplayProbe(force, direction, 0f, creatureCount, durationSeconds);
            Time.timeScale = 6f;
            float maxDrift = 0f;
            int survivedSteps = 0;
            for (int step = 0; step < totalSteps; step += 1)
            {
                if (step == reactionSteps)
                {
                    game.SetGameplayProbeControlAmount(impreciseHold);
                }

                yield return new WaitForFixedUpdate();
                maxDrift = Mathf.Max(maxDrift, game.GetGameplayProbeMaxDrift());
                if (game.GetGameplayProbePhase() != GamePhase.Playing)
                {
                    break;
                }

                survivedSteps += 1;
            }

            Time.timeScale = 1f;
            List<Rigidbody2D> bodies = GetCreatureBodies();
            float finalMeanX = 0f;
            foreach (Rigidbody2D body in bodies)
            {
                finalMeanX += body.position.x;
            }

            finalMeanX = bodies.Count == 0 ? 0f : finalMeanX / bodies.Count;
            onComplete(new TowerMeasurement(
                maxDrift,
                finalMeanX,
                survivedSteps,
                totalSteps,
                game.GetGameplayProbePhase()));
        }

        private static void AssertTowerOrdering(
            TowerMeasurement neutral,
            TowerMeasurement correct,
            TowerMeasurement wrong,
            int direction)
        {
            string directionName = direction < 0 ? "left" : "right";
            Assert.That(
                correct.DownwindDisplacement(direction),
                Is.LessThan(neutral.DownwindDisplacement(direction)),
                $"Expected counter-tilt to reduce downwind displacement for {directionName} wind. " +
                $"Neutral: {neutral}; correct: {correct}; wrong: {wrong}.");
            Assert.That(
                correct.DownwindDisplacement(direction),
                Is.LessThan(wrong.DownwindDisplacement(direction)),
                $"Expected counter-tilt to beat wrong tilt on downwind displacement for {directionName} wind. " +
                $"Neutral: {neutral}; correct: {correct}; wrong: {wrong}.");
            Assert.That(
                correct.SurvivedSteps,
                Is.GreaterThanOrEqualTo(neutral.SurvivedSteps),
                $"Expected counter-tilt to survive at least as long as neutral for {directionName} wind. " +
                $"Neutral: {neutral}; correct: {correct}; wrong: {wrong}.");
            Assert.That(
                correct.SurvivedSteps,
                Is.GreaterThanOrEqualTo(wrong.SurvivedSteps),
                $"Expected counter-tilt to survive at least as long as wrong tilt for {directionName} wind. " +
                $"Neutral: {neutral}; correct: {correct}; wrong: {wrong}.");
            Assert.That(
                correct.Completed,
                Is.True,
                $"Expected counter-tilt to complete the {directionName} gust. " +
                $"Neutral: {neutral}; correct: {correct}; wrong: {wrong}.");
        }

        private static List<Rigidbody2D> GetCreatureBodies()
        {
            Rigidbody2D[] bodies = Object.FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
            List<Rigidbody2D> creatures = new List<Rigidbody2D>();
            foreach (Rigidbody2D body in bodies)
            {
                if (body.gameObject.name.StartsWith("Creature "))
                {
                    creatures.Add(body);
                }
            }

            creatures.Sort((left, right) => string.CompareOrdinal(left.gameObject.name, right.gameObject.name));
            return creatures;
        }

        private static List<Collider2D> GetCreatureColliders()
        {
            Collider2D[] all = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            List<Collider2D> creatures = new List<Collider2D>();
            foreach (Collider2D collider in all)
            {
                if (collider.gameObject.name.StartsWith("Creature "))
                {
                    creatures.Add(collider);
                }
            }

            creatures.Sort((left, right) => string.CompareOrdinal(left.gameObject.name, right.gameObject.name));
            return creatures;
        }

        private static LineRenderer FindEnabledWindLine(WindStreaks wind)
        {
            LineRenderer[] lines = wind.GetComponentsInChildren<LineRenderer>();
            foreach (LineRenderer line in lines)
            {
                if (line.enabled)
                {
                    return line;
                }
            }

            Assert.Fail("Expected at least one visible wind streak.");
            return null;
        }

        private static string GetButtonLabel(string gameObjectName)
        {
            GameObject gameObject = GameObject.Find(gameObjectName);
            Assert.That(gameObject, Is.Not.Null, $"Could not find GameObject '{gameObjectName}'.");

            Text label = gameObject.GetComponentInChildren<Text>();
            Assert.That(label, Is.Not.Null, $"GameObject '{gameObjectName}' is missing a Text label.");
            return label.text;
        }

        private static T FindRequiredComponent<T>(string gameObjectName) where T : Component
        {
            GameObject gameObject = GameObject.Find(gameObjectName);
            Assert.That(gameObject, Is.Not.Null, $"Could not find GameObject '{gameObjectName}'.");

            T component = gameObject.GetComponent<T>();
            Assert.That(component, Is.Not.Null, $"GameObject '{gameObjectName}' is missing component {typeof(T).Name}.");
            return component;
        }

        private readonly struct TowerMeasurement
        {
            public TowerMeasurement(
                float maxDrift,
                float finalMeanX,
                int survivedSteps,
                int totalSteps,
                GamePhase phase)
            {
                MaxDrift = maxDrift;
                FinalMeanX = finalMeanX;
                SurvivedSteps = survivedSteps;
                TotalSteps = totalSteps;
                Phase = phase;
            }

            public float MaxDrift { get; }

            public float FinalMeanX { get; }

            public int SurvivedSteps { get; }

            public int TotalSteps { get; }

            public GamePhase Phase { get; }

            public bool Completed => SurvivedSteps == TotalSteps;

            public float DownwindDisplacement(int direction)
            {
                return FinalMeanX * direction;
            }

            public override string ToString()
            {
                return $"drift={MaxDrift:0.000}, meanX={FinalMeanX:0.000}, " +
                    $"steps={SurvivedSteps}/{TotalSteps}, phase={Phase}";
            }
        }
    }
}
