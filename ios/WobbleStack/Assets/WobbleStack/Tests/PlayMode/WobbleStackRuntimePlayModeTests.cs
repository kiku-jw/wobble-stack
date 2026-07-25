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
            Assert.That(GameObject.Find("Difficulty"), Is.Null);
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
        public IEnumerator ArticulatedFacesProgressFromCalmToWindToImpact()
        {
            yield return WaitForBootstrap();

            CreatureBody creature = Object.FindFirstObjectByType<CreatureBody>();
            Assert.That(creature, Is.Not.Null);
            CreatureRig rig = creature.Rig;
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.Emotion, Is.EqualTo(CreatureEmotion.Calm));
            Assert.That(rig.SecondaryPartCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(rig.GetMouthSpriteName(), Does.StartWith("face-"));

            creature.SetWind(0.32f);
            yield return null;
            Assert.That(rig.Emotion, Is.Not.EqualTo(CreatureEmotion.Calm));
            Assert.That(rig.Emotion, Is.Not.EqualTo(CreatureEmotion.Impact));

            rig.ShowFallReaction();
            yield return null;
            Assert.That(rig.Emotion, Is.EqualTo(CreatureEmotion.Panic));

            creature.ShowImpactReaction();
            yield return null;
            Assert.That(rig.Emotion, Is.EqualTo(CreatureEmotion.Impact));
            Assert.That(rig.GetMouthSpriteName(), Is.EqualTo("face-DazedMouth"));
        }

        [UnityTest]
        [Order(6)]
        public IEnumerator FiveCharactersHaveDistinctRigsAndDenseVisibleContacts()
        {
            yield return WaitForBootstrap();

            List<CreatureBody> creatures = GetCreatureComponents();
            Assert.That(creatures.Count, Is.EqualTo(5));
            Assert.That(GetCreatureByKind(creatures, CharacterKind.Pear).Rig.SecondaryPartCount, Is.EqualTo(8));
            Assert.That(GetCreatureByKind(creatures, CharacterKind.Cube).Rig.SecondaryPartCount, Is.EqualTo(4));
            Assert.That(GetCreatureByKind(creatures, CharacterKind.Bird).Rig.SecondaryPartCount, Is.EqualTo(8));
            Assert.That(GetCreatureByKind(creatures, CharacterKind.Rabbit).Rig.SecondaryPartCount, Is.EqualTo(6));
            Assert.That(GetCreatureByKind(creatures, CharacterKind.Jelly).Rig.SecondaryPartCount, Is.EqualTo(5));

            creatures.Sort((left, right) => left.Body.position.y.CompareTo(right.Body.position.y));
            for (int index = 1; index < creatures.Count; index += 1)
            {
                Bounds lower = GetVisualBounds(creatures[index - 1]);
                Bounds upper = GetVisualBounds(creatures[index]);
                float visualGap = upper.min.y - lower.max.y;
                Assert.That(
                    visualGap,
                    Is.InRange(-1.6f, 0.04f),
                    $"Articulated silhouettes {index - 1}/{index} did not read as a dense stack.");
            }
        }

        [UnityTest]
        [Order(7)]
        public IEnumerator PersonalityThresholdsAndBlinkSchedulesDiffer()
        {
            yield return WaitForBootstrap();

            List<CreatureBody> creatures = GetCreatureComponents();
            CreatureBody pear = GetCreatureByKind(creatures, CharacterKind.Pear);
            CreatureBody cube = GetCreatureByKind(creatures, CharacterKind.Cube);
            CreatureBody rabbit = GetCreatureByKind(creatures, CharacterKind.Rabbit);
            pear.SetWind(0.5f);
            cube.SetWind(0.5f);
            rabbit.SetWind(0.5f);
            Assert.That(pear.Rig.Emotion, Is.EqualTo(CreatureEmotion.Effort));
            Assert.That(cube.Rig.Emotion, Is.EqualTo(CreatureEmotion.Panic));
            Assert.That(rabbit.Rig.Emotion, Is.EqualTo(CreatureEmotion.Effort));

            HashSet<int> blinkBuckets = new HashSet<int>();
            foreach (CreatureBody creature in creatures)
            {
                blinkBuckets.Add(Mathf.RoundToInt(creature.Rig.GetNextBlinkAtProbe() * 20f));
            }

            Assert.That(blinkBuckets.Count, Is.GreaterThanOrEqualTo(3));
        }

        [UnityTest]
        [Order(8)]
        public IEnumerator EarsLeavesAndWingsLagBehindPhysicalMotion()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            game.ConfigureGameplayProbe(0f, 1, 0f, 5, 1.4f);
            CreatureBody rabbit = GetCreatureByKind(GetCreatureComponents(), CharacterKind.Rabbit);
            float before = rabbit.Rig.GetSecondaryMotionProbe();
            rabbit.Body.angularVelocity = 85f;
            rabbit.SetWind(0.82f);

            for (int step = 0; step < 14; step += 1)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            float after = rabbit.Rig.GetSecondaryMotionProbe();
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(before, after)), Is.GreaterThan(3f));
        }

        [UnityTest]
        [Order(9)]
        public IEnumerator WindStreaksAreCoolBlueAndShowTheirTravelDirection()
        {
            yield return WaitForBootstrap();

            WindStreaks wind = Object.FindFirstObjectByType<WindStreaks>();
            Assert.That(wind, Is.Not.Null);
            wind.SetWind(1, 0.25f);
            wind.Refresh(1f);

            LineRenderer line = FindEnabledWindLine(wind);
            int gentleLineCount = CountEnabledWindLines(wind);
            float gentleAlpha = line.startColor.a;
            Assert.That(line.startColor.b, Is.GreaterThan(line.startColor.r));
            Assert.That(line.startColor.a, Is.GreaterThan(0.2f));
            Assert.That(line.GetPosition(0).x, Is.GreaterThan(line.GetPosition(1).x));

            wind.SetWind(1, 0.9f);
            wind.Refresh(1f);
            LineRenderer strongLine = FindEnabledWindLine(wind);
            Assert.That(CountEnabledWindLines(wind), Is.GreaterThan(gentleLineCount));
            Assert.That(strongLine.startColor.a, Is.GreaterThan(gentleAlpha));

            wind.SetWind(-1, 0.25f);
            wind.Refresh(1f);
            line = FindEnabledWindLine(wind);
            Assert.That(line.GetPosition(0).x, Is.LessThan(line.GetPosition(1).x));
        }

        [UnityTest]
        [Order(10)]
        public IEnumerator RollingVehicleUsesOneWheelJointAndOnlyWeakCreatureGrips()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);

            game.ConfigureGameplayProbe(0f, 1, 0f, 5, 1.4f);
            Rigidbody2D[] bodies = GetCreatureBodies().ToArray();
            Assert.That(bodies.Length, Is.EqualTo(5));
            Joint2D[] joints = Object.FindObjectsByType<Joint2D>(FindObjectsSortMode.None);
            int wheelJointCount = 0;
            int weakGripCount = 0;
            foreach (Joint2D joint in joints)
            {
                if (joint is WheelJoint2D wheelJoint)
                {
                    wheelJointCount += 1;
                    Assert.That(wheelJoint.gameObject.name, Is.EqualTo("Seesaw Beam"));
                    Assert.That(wheelJoint.connectedBody.gameObject.name, Is.EqualTo("Star Wheel"));
                    continue;
                }

                Assert.That(
                    joint,
                    Is.InstanceOf<DistanceJoint2D>(),
                    "Only the wheel joint and authored weak hand grips are permitted.");
                if (!(joint is DistanceJoint2D grip))
                {
                    continue;
                }

                Assert.That(grip.maxDistanceOnly, Is.True);
                Assert.That(grip.enabled, Is.False, "A creature grip may not stabilize the calm tower.");
                weakGripCount += 1;
            }

            Assert.That(wheelJointCount, Is.EqualTo(1));
            Assert.That(weakGripCount, Is.EqualTo(4));
            foreach (Rigidbody2D body in bodies)
            {
                Assert.That(body.freezeRotation, Is.False, $"{body.name} had hidden rotation locking.");
                foreach (Joint2D joint in body.GetComponents<Joint2D>())
                {
                    Assert.That(joint, Is.InstanceOf<DistanceJoint2D>());
                    Assert.That(joint.enabled, Is.False);
                }
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
        [Order(11)]
        public IEnumerator WeakVisibleGripActivatesAndReleasesWithoutBecomingATether()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            game.ConfigureGameplayProbe(
                WobbleStackRules.GustForceMax,
                1,
                0.8f,
                5,
                WobbleStackRules.GustDurationMax);
            List<CreatureBody> creatures = GetCreatureComponents();
            CreatureBody observed = null;
            bool released = false;

            Time.timeScale = 4f;
            for (int step = 0; step < 220; step += 1)
            {
                yield return new WaitForFixedUpdate();
                foreach (CreatureBody creature in creatures)
                {
                    if (observed == null && creature.HasActiveGrip)
                    {
                        observed = creature;
                    }
                    else if (observed == creature && !creature.HasActiveGrip)
                    {
                        released = true;
                    }
                }

                if (released || game.GetGameplayProbePhase() != GamePhase.Playing)
                {
                    break;
                }
            }

            Time.timeScale = 1f;
            Assert.That(observed, Is.Not.Null, "No character visibly attempted a weak grip during danger.");
            Assert.That(observed.GripWasUsed, Is.True);
            Assert.That(released || !observed.HasGripJoint, Is.True, "The grip never released or broke.");
        }

        [UnityTest]
        [Order(12)]
        public IEnumerator RelativeDriveRollsTheGroundedWheelBeforeWindStarts()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);
            const float driveAmount = 0.72f;
            game.ConfigureGameplayProbe(0f, 1, driveAmount, 3, 1.4f);
            Vector2 initialWheelPosition = game.GetGameplayProbeWheelPosition();
            float initialWheelRotation = game.GetGameplayProbeWheelRotation();

            for (int step = 0; step < 45; step += 1)
            {
                yield return new WaitForFixedUpdate();
            }

            Vector2 wheelPosition = game.GetGameplayProbeWheelPosition();
            float wheelRotation = game.GetGameplayProbeWheelRotation();
            Rigidbody2D beam = GameObject.Find("Seesaw Beam").GetComponent<Rigidbody2D>();
            CircleCollider2D wheel = GameObject.Find("Star Wheel").GetComponent<CircleCollider2D>();
            BoxCollider2D road = GameObject.Find("Road").GetComponent<BoxCollider2D>();
            Assert.That(beam.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
            Assert.That(wheelPosition.x, Is.GreaterThan(initialWheelPosition.x + 0.12f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(initialWheelRotation, wheelRotation)), Is.GreaterThan(8f));
            Assert.That(Mathf.Abs(wheel.bounds.min.y - road.bounds.max.y), Is.LessThan(0.12f));
            Assert.That(
                Mathf.Abs(game.GetGameplayProbePlatformRotation()),
                Is.LessThan(28f),
                "Ordinary wheel travel turned the plank edge-on.");
            Assert.That(game.GetGameplayProbePhase(), Is.EqualTo(GamePhase.Playing));
        }

        [UnityTest]
        [Order(13)]
        public IEnumerator StarWheelStaysGroundedThroughAStrongFiveFriendCatch()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            game.ConfigureGameplayProbe(
                WobbleStackRules.GustForceMax,
                1,
                0.8f,
                5,
                WobbleStackRules.GustDurationMax);
            CircleCollider2D wheel = GameObject.Find("Star Wheel").GetComponent<CircleCollider2D>();
            BoxCollider2D road = GameObject.Find("Road").GetComponent<BoxCollider2D>();
            float maximumGap = 0f;

            for (int step = 0; step < 180; step += 1)
            {
                yield return new WaitForFixedUpdate();
                maximumGap = Mathf.Max(maximumGap, wheel.bounds.min.y - road.bounds.max.y);
            }

            Assert.That(maximumGap, Is.LessThan(0.18f), $"The star wheel visibly left the road by {maximumGap:0.000} world units.");
            Assert.That(game.GetGameplayProbePhase(), Is.EqualTo(GamePhase.Playing));
        }

        [UnityTest]
        [Order(14)]
        public IEnumerator CameraFollowsHorizontalWheelTravel()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            game.ConfigureGameplayProbe(0f, 1, 0.72f, 3, 2f);
            float initialCameraX = game.GetGameplayProbeCameraX();

            for (int step = 0; step < 90; step += 1)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            float wheelX = game.GetGameplayProbeWheelPosition().x;
            float cameraX = game.GetGameplayProbeCameraX();
            Assert.That(cameraX, Is.GreaterThan(initialCameraX + 0.1f));
            Assert.That(Mathf.Abs(cameraX - wheelX), Is.LessThan(2.6f));
            Assert.That(game.GetGameplayProbePhase(), Is.EqualTo(GamePhase.Playing));
        }

        [UnityTest]
        [Order(15)]
        public IEnumerator DelayedBroadCatchGestureSurvivesStrongestGustMatrix()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);

            foreach (int creatureCount in new[] { 3, 5 })
            {
                foreach (int direction in new[] { -1, 1 })
                {
                    TowerMeasurement measurement = default;
                    yield return MeasureDelayedTower(
                        game,
                        WobbleStackRules.GustForceMax,
                        direction,
                        creatureCount,
                        WobbleStackRules.GustDurationMax,
                        value => measurement = value);

                    Assert.That(
                        measurement.Completed,
                        Is.True,
                        $"{creatureCount} creatures, direction {direction} did not survive a delayed broad catch gesture: {measurement}.");
                }
            }
        }

        [UnityTest]
        [Order(16)]
        public IEnumerator SteadyWheelCatchSurvivesStrongestGustAcrossTowerSizes()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);

            foreach (int creatureCount in new[] { 3, 5 })
            {
                foreach (int direction in new[] { -1, 1 })
                {
                    float humanHold = GetSteadyCatchAmount(direction, creatureCount);
                    TowerMeasurement measurement = default;
                    yield return MeasureTower(
                        game,
                        WobbleStackRules.GustForceMax,
                        direction,
                        humanHold,
                        creatureCount,
                        WobbleStackRules.GustDurationMax,
                        value => measurement = value);

                    Assert.That(
                        measurement.Completed,
                        Is.True,
                        $"{creatureCount} creatures, direction {direction} failed the complete first gust: {measurement}.");
                }
            }
        }

        [UnityTest]
        [Order(17)]
        public IEnumerator CorrectWheelTravelOutperformsNeutralAndWrongTravelOnTheSameGust()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);
            foreach (int direction in new[] { -1, 1 })
            {
                float humanHold = GetSteadyCatchAmount(direction, 5);
                TowerMeasurement neutral = default;
                yield return MeasureTower(
                    game,
                    WobbleStackRules.GustForceMax,
                    direction,
                    0f,
                    5,
                    WobbleStackRules.GustDurationMax,
                    value => neutral = value);
                TowerMeasurement correct = default;
                yield return MeasureTower(
                    game,
                    WobbleStackRules.GustForceMax,
                    direction,
                    humanHold,
                    5,
                    WobbleStackRules.GustDurationMax,
                    value => correct = value);
                TowerMeasurement wrong = default;
                yield return MeasureTower(
                    game,
                    WobbleStackRules.GustForceMax,
                    direction,
                    -humanHold,
                    5,
                    WobbleStackRules.GustDurationMax,
                    value => wrong = value);

                AssertTowerOrdering(neutral, correct, wrong, direction);
            }
        }

        [UnityTest]
        [Order(18)]
        public IEnumerator NeutralAndWrongInputCollapseUnderTheStrongestGust()
        {
            yield return WaitForBootstrap();
            WobbleStackGame game = Object.FindFirstObjectByType<WobbleStackGame>();
            Assert.That(game, Is.Not.Null);
            foreach (int direction in new[] { -1, 1 })
            {
                float humanHold = GetSteadyCatchAmount(direction, 5);
                TowerMeasurement neutral = default;
                yield return MeasureTower(
                    game,
                    WobbleStackRules.GustForceMax,
                    direction,
                    0f,
                    5,
                    WobbleStackRules.GustDurationMax,
                    value => neutral = value);
                TowerMeasurement correct = default;
                yield return MeasureTower(
                    game,
                    WobbleStackRules.GustForceMax,
                    direction,
                    humanHold,
                    5,
                    WobbleStackRules.GustDurationMax,
                    value => correct = value);
                TowerMeasurement wrong = default;
                yield return MeasureTower(
                    game,
                    WobbleStackRules.GustForceMax,
                    direction,
                    -humanHold,
                    5,
                    WobbleStackRules.GustDurationMax,
                    value => wrong = value);

                Assert.That(neutral.Completed, Is.False, $"Neutral input was immortal under the strongest {direction} wind: {neutral}.");
                Assert.That(wrong.Completed, Is.False, $"Wrong input was immortal under the strongest {direction} wind: {wrong}.");
                Assert.That(correct.Completed, Is.True, $"Correct input did not survive the strongest {direction} wind: {correct}.");
                Assert.That(correct.SurvivedSteps, Is.GreaterThan(neutral.SurvivedSteps));
                Assert.That(correct.SurvivedSteps, Is.GreaterThan(wrong.SurvivedSteps));
            }
        }

        [UnityTest]
        [Order(19)]
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
            foreach (CreatureBody fallingCreature in GetCreatureComponents())
            {
                Assert.That(fallingCreature.IsFalling, Is.True);
                Assert.That(fallingCreature.Rig.Emotion, Is.EqualTo(CreatureEmotion.Panic));
            }

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

        private static float GetSteadyCatchAmount(int direction, int creatureCount)
        {
            float magnitude = creatureCount <= 3 ? 0.4f : 0.8f;
            return direction * magnitude;
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
            finalMeanX -= game.GetGameplayProbeWheelPosition().x;
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
            float catchSeconds = creatureCount == 3 ? 0.7f : 1.5f;
            int catchSteps = Mathf.CeilToInt(catchSeconds / Time.fixedDeltaTime);
            float settleAmount = creatureCount == 3 ? 0.4f : 1f;
            game.ConfigureGameplayProbe(force, direction, 0f, creatureCount, durationSeconds);
            Time.timeScale = 6f;
            float maxDrift = 0f;
            int survivedSteps = 0;
            for (int step = 0; step < totalSteps; step += 1)
            {
                if (step == reactionSteps)
                {
                    game.SetGameplayProbeControlAmount(direction);
                }
                else if (step == reactionSteps + catchSteps)
                {
                    game.SetGameplayProbeControlAmount(direction * settleAmount);
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
            finalMeanX -= game.GetGameplayProbeWheelPosition().x;
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
                $"Expected correct wheel travel to reduce downwind displacement for {directionName} wind. " +
                $"Neutral: {neutral}; correct: {correct}; wrong: {wrong}.");
            Assert.That(
                correct.DownwindDisplacement(direction),
                Is.LessThan(wrong.DownwindDisplacement(direction)),
                $"Expected correct wheel travel to beat wrong travel on downwind displacement for {directionName} wind. " +
                $"Neutral: {neutral}; correct: {correct}; wrong: {wrong}.");
            Assert.That(
                correct.SurvivedSteps,
                Is.GreaterThanOrEqualTo(neutral.SurvivedSteps),
                $"Expected correct wheel travel to survive at least as long as neutral for {directionName} wind. " +
                $"Neutral: {neutral}; correct: {correct}; wrong: {wrong}.");
            Assert.That(
                correct.SurvivedSteps,
                Is.GreaterThanOrEqualTo(wrong.SurvivedSteps),
                $"Expected correct wheel travel to survive at least as long as wrong travel for {directionName} wind. " +
                $"Neutral: {neutral}; correct: {correct}; wrong: {wrong}.");
            Assert.That(
                correct.Completed,
                Is.True,
                $"Expected correct wheel travel to complete the {directionName} gust. " +
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

        private static List<CreatureBody> GetCreatureComponents()
        {
            CreatureBody[] all = Object.FindObjectsByType<CreatureBody>(FindObjectsSortMode.None);
            List<CreatureBody> creatures = new List<CreatureBody>(all);
            creatures.Sort((left, right) => string.CompareOrdinal(left.gameObject.name, right.gameObject.name));
            return creatures;
        }

        private static CreatureBody GetCreatureByKind(
            List<CreatureBody> creatures,
            CharacterKind kind)
        {
            foreach (CreatureBody creature in creatures)
            {
                if (creature.Kind == kind)
                {
                    return creature;
                }
            }

            Assert.Fail($"Could not find articulated creature {kind}.");
            return null;
        }

        private static Bounds GetVisualBounds(CreatureBody creature)
        {
            SpriteRenderer[] renderers = creature.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(renderers.Length, Is.GreaterThan(0));
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index += 1)
            {
                if (renderers[index].enabled && renderers[index].gameObject.activeInHierarchy)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }
            }

            return bounds;
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

        private static int CountEnabledWindLines(WindStreaks wind)
        {
            int count = 0;
            foreach (LineRenderer line in wind.GetComponentsInChildren<LineRenderer>())
            {
                if (line.enabled)
                {
                    count += 1;
                }
            }

            return count;
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
