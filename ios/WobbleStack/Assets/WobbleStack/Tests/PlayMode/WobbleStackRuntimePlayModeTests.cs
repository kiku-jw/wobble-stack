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
        public IEnumerator CounterTiltReducesLateralGustVelocityForLeftAndRightGusts()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);

            GustDirectionMeasurement left = default;
            yield return MeasureDirection(game, -1, value => left = value);
            GustDirectionMeasurement right = default;
            yield return MeasureDirection(game, 1, value => right = value);

            AssertDirectionMeasurement(left, "left");
            AssertDirectionMeasurement(right, "right");
        }

        [UnityTest]
        [Order(5)]
        public IEnumerator CounterTiltReducesDriftInTheSupportedTowerForBothDirections()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);
            DifficultyProfile profile = WobbleStackRules.GetDifficultyProfile(DifficultyId.Normal);
            float force = (profile.ForceMin + profile.ForceMax) * 0.5f;
            float counterAngle = WobbleStackRules.GetRequiredCounterAngle(force, WobbleStackRules.GravityScale);

            TowerMeasurement leftNeutral = default;
            yield return MeasureTower(game, force, -1, 0f, value => leftNeutral = value);
            TowerMeasurement leftCorrect = default;
            yield return MeasureTower(game, force, -1, counterAngle, value => leftCorrect = value);
            TowerMeasurement leftWrong = default;
            yield return MeasureTower(game, force, -1, -counterAngle, value => leftWrong = value);
            AssertTowerOrdering(leftNeutral, leftCorrect, leftWrong, "left");

            TowerMeasurement rightNeutral = default;
            yield return MeasureTower(game, force, 1, 0f, value => rightNeutral = value);
            TowerMeasurement rightCorrect = default;
            yield return MeasureTower(game, force, 1, -counterAngle, value => rightCorrect = value);
            TowerMeasurement rightWrong = default;
            yield return MeasureTower(game, force, 1, counterAngle, value => rightWrong = value);
            AssertTowerOrdering(rightNeutral, rightCorrect, rightWrong, "right");
        }

        [UnityTest]
        [Order(6)]
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

        private static IEnumerator MeasureDirection(WobbleStackGame game, int direction, System.Action<GustDirectionMeasurement> onComplete)
        {
            DifficultyProfile profile = WobbleStackRules.GetDifficultyProfile(DifficultyId.Normal);
            float force = (profile.ForceMin + profile.ForceMax) * 0.5f;
            float counterAngle = WobbleStackRules.GetRequiredCounterAngle(force, WobbleStackRules.GravityScale);
            float neutralVelocity = 0f;
            yield return MeasureAverageVelocityForAngle(game, force, direction, 0f, value => neutralVelocity = value);
            float correctVelocity = 0f;
            yield return MeasureAverageVelocityForAngle(game, force, direction, -direction * counterAngle, value => correctVelocity = value);
            float wrongVelocity = 0f;
            yield return MeasureAverageVelocityForAngle(game, force, direction, direction * counterAngle, value => wrongVelocity = value);

            onComplete(new GustDirectionMeasurement(direction, neutralVelocity, correctVelocity, wrongVelocity));
        }

        private static void AssertDirectionMeasurement(GustDirectionMeasurement measurement, string directionName)
        {
            Assert.That(Mathf.Abs(measurement.NeutralVelocityX), Is.GreaterThan(0.001f), $"Expected a measurable {directionName} gust.");
            Assert.That(
                Mathf.Abs(measurement.CounterTiltVelocityX),
                Is.LessThan(Mathf.Abs(measurement.NeutralVelocityX)),
                $"Expected counter-tilt to reduce {directionName} gust drift.");
            Assert.That(
                Mathf.Abs(measurement.CounterTiltVelocityX),
                Is.LessThan(Mathf.Abs(measurement.WrongTiltVelocityX)),
                $"Expected counter-tilt to beat wrong-way tilt during the {directionName} gust.");
        }

        private static IEnumerator MeasureAverageVelocityForAngle(
            WobbleStackGame game,
            float force,
            int direction,
            float platformAngleRadians,
            System.Action<float> onComplete)
        {
            game.ConfigurePhysicsProbe(force, direction, platformAngleRadians);
            yield return new WaitForFixedUpdate();

            List<Rigidbody2D> creatures = game.GetPhysicsProbeBodies();
            float totalVelocity = 0f;
            foreach (Rigidbody2D creature in creatures)
            {
                totalVelocity += creature.linearVelocity.x;
            }

            onComplete(totalVelocity / creatures.Count);
        }

        private static IEnumerator MeasureTower(
            WobbleStackGame game,
            float force,
            int direction,
            float targetAngleRadians,
            System.Action<TowerMeasurement> onComplete)
        {
            const int totalSteps = 150;
            game.ConfigureGameplayProbe(force, direction, targetAngleRadians, 5);
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

            onComplete(new TowerMeasurement(maxDrift, survivedSteps));
        }

        private static void AssertTowerOrdering(
            TowerMeasurement neutral,
            TowerMeasurement correct,
            TowerMeasurement wrong,
            string directionName)
        {
            Assert.That(
                correct.MaxDrift,
                Is.LessThan(neutral.MaxDrift),
                $"Expected counter-tilt to reduce supported-tower drift for {directionName} wind.");
            Assert.That(
                correct.MaxDrift,
                Is.LessThan(wrong.MaxDrift),
                $"Expected counter-tilt to beat wrong tilt for {directionName} wind.");
            Assert.That(
                correct.SurvivedSteps,
                Is.GreaterThanOrEqualTo(neutral.SurvivedSteps),
                $"Expected counter-tilt to survive at least as long as neutral for {directionName} wind.");
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

        private readonly struct GustDirectionMeasurement
        {
            public GustDirectionMeasurement(int direction, float neutralVelocityX, float counterTiltVelocityX, float wrongTiltVelocityX)
            {
                Direction = direction;
                NeutralVelocityX = neutralVelocityX;
                CounterTiltVelocityX = counterTiltVelocityX;
                WrongTiltVelocityX = wrongTiltVelocityX;
            }

            public int Direction { get; }

            public float NeutralVelocityX { get; }

            public float CounterTiltVelocityX { get; }

            public float WrongTiltVelocityX { get; }
        }

        private readonly struct TowerMeasurement
        {
            public TowerMeasurement(float maxDrift, int survivedSteps)
            {
                MaxDrift = maxDrift;
                SurvivedSteps = survivedSteps;
            }

            public float MaxDrift { get; }

            public int SurvivedSteps { get; }
        }
    }
}
