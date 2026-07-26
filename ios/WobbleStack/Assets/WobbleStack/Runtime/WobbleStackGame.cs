using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WobbleStack.Domain;

namespace WobbleStack.Runtime
{
    internal sealed class WobbleStackGame : MonoBehaviour
    {
        private const float GroundSurfaceY = -7.15f;
        private const float WheelRadius = 1.08f;
        private const float WheelSpriteFillRatio = 268f / 310f;
        private const float WheelCenterY = GroundSurfaceY + WheelRadius;
        private const float PlatformY = GroundSurfaceY + (WheelRadius * 2f) + 0.37f;
        private const float PlatformWidth = 8.5f;
        private const float PlatformHeight = 0.78f;
        private const float StaticStackContactInset = 0.06f;
        private const float DynamicStackContactInset = 0.05f;
        private const float RoadLength = 400f;
        private const float RoadCenterX = 0f;
        private const float PointerTravelFraction = 0.28f;
        private const float WheelMotorSpeed = 42f;
        private const float WheelCatchBoostSpeed = 30f;
        private const float WheelCruiseMotorSpeed = 120f;
        private const float WheelCruiseMotorTorque = 14f;
        private const float WheelRecoveryMotorSpeed = 120f;
        private const float WheelRecoveryMotorTorque = 40f;
        private const float RouteForwardCruiseAmount = 0.18f;
        private const float WheelDriveTorque = 40f;
        private const float WheelBrakeTorque = 12f;
        private const float PlatformSpringTorquePerDegree = 20f;
        private const float PlatformSpringDamping = 8f;
        private const float PlatformSpringMaximumTorque = 1000f;
        private const float PlatformAxleStopDegrees = 20f;
        private const float PlatformAxleStopTorquePerDegree = 70f;
        private const float PlatformAxleStopDamping = 12f;
        private const float WindBalancePreviewSeconds = 1.25f;
        private const float PostGustRecoverySeconds = 4f;
        private const float UnityAccelerationScale = 9342.857f;
        private const float ImpactSlowMotionScale = 0.18f;
        private const float ImpactSlowMotionSeconds = 0.36f;
        private const float FailureResultHoldSeconds = 1.15f;
        private const float FailureHardTimeoutSeconds = 2.6f;
        private const float FinishCelebrationSeconds = 1.45f;
        private const string ReducedMotionPreference = "wobble.ios.reduced-motion";
        private const string RoutePreference = "wobble.ios.route";
        private const string UnlockedRoutePreference = "wobble.ios.route-unlocked";
        private const string OnboardingPreference = "wobble.ios.onboarding-seen";

        private readonly List<CreatureBody> _creatures = new List<CreatureBody>();
        private Camera _camera;
        private Vector3 _cameraHome;
        private float _cameraFollowVelocity;
        private TravellingWorld _travellingWorld;
        private Rigidbody2D _platformBody;
        private Rigidbody2D _wheelBody;
        private WheelJoint2D _wheelJoint;
        private Transform _worldRoot;
        private WindStreaks _windStreaks;
        private GameAudio _audio;
        private PhysicsMaterial2D _creatureMaterial;
        private PhysicsMaterial2D _roadMaterial;
        private Canvas _canvas;
        private GameObject _hudRoot;
        private GameObject _startOverlay;
        private GameObject _pauseOverlay;
        private GameObject _resultsOverlay;
        private Text _scoreText;
        private Text _motionText;
        private Text _routeText;
        private Text _routeSubtitleText;
        private Text _resultTitleText;
        private Text _resultTimeText;
        private Text _resultBestText;
        private Text _resultActionText;
        private Text _saveText;
        private Text _hintText;
        private GameObject _gestureTrack;
        private RectTransform _gestureCue;
        private GamePhase _phase = GamePhase.Ready;
        private int _creatureCount = 5;
        private int _currentRouteIndex;
        private int _unlockedRouteIndex;
        private int _collectedBadges;
        private int _nextJoinIndex;
        private float _routeProgress;
        private float _friendStopUntil;
        private RouteDefinition _route;
        private bool _reducedMotion;
        private bool _runSucceeded;
        private bool _gestureUsed;
        private bool _gameplayProbeActive;
        private int _runCount;
        private float _runSeconds;
        private float _postGustRecoveryUntil;
        private int _postGustRecoveryDirection;
        private float _platformSpringBlend = 1f;
        private float _finishStartedAt;
        private float _nextWheelDustAt;
        private bool _pointerActive;
        private float _pointerOriginX;
        private float _controlAmount;
        private GustScheduler _gustScheduler;
        private GustSample _gust;
        private bool _hasGust;
        private int _gustIndex;
        private int _routeGustSequenceIndex;
        private float _failureStartedAt;
        private float _firstImpactAt = -1f;
        private float _slowMotionEndsAt = -1f;
        private float _failureSuspendedAt = -1f;
        private bool _dangerWasHigh;
        private string _lastFailureReason = string.Empty;
        private float _saveMessageEndsAt;
        private float _cameraShake;
        private GameObject _crownObject;
        private Font _font;

        private void Awake()
        {
            ConfigureRuntime();
            LoadSettings();
            BuildScene();
            BuildInterface();
            ShowReady();
        }

        private IEnumerator Start()
        {
            string capturePath = FindArgumentValue("--wobble-capture=");
            if (string.IsNullOrEmpty(capturePath))
            {
                yield break;
            }

            if (HasArgument("--wobble-capture-results"))
            {
                PrepareResultsCapture();
            }
            else if (HasArgument("--wobble-capture-finish"))
            {
                PrepareFinishCapture();
            }
            else if (HasArgument("--wobble-capture-impact"))
            {
                PrepareImpactCapture();
            }
            else if (HasArgument("--wobble-capture-playing"))
            {
                PreparePlayingCapture();
            }

            UpdateCameraRig();
            UpdateHud();
            PortraitCapture.Write(_camera, _canvas, capturePath);
            Application.Quit();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_phase == GamePhase.Playing)
                {
                    PauseRun();
                }
                else if (_phase == GamePhase.Paused)
                {
                    ResumeRun();
                }
            }

            if (_phase == GamePhase.Playing)
            {
                ReadInput();
            }
            else if (_phase == GamePhase.Failing && _failureSuspendedAt < 0f)
            {
                UpdateFailure();
            }
            else if (_phase == GamePhase.Finishing)
            {
                UpdateFinish();
            }

            UpdateCameraRig();
            UpdateHud();
        }

        private void FixedUpdate()
        {
            if (_phase != GamePhase.Playing)
            {
                return;
            }

            _runSeconds += Time.fixedDeltaTime;
            UpdateGust();
            UpdatePlatformSuspension();
            UpdateFriendStopBraking();
            UpdateWheelDrive();
            UpdateBalanceTransitionDamping();
            UpdateRecoveryGrips();
            UpdateWheelDust();

            if (_hasGust && _runSeconds >= _gust.StartsAtSeconds && _runSeconds < _gust.EndsAtSeconds)
            {
                float progress = (_runSeconds - _gust.StartsAtSeconds) / _gust.DurationSeconds;
                float envelope = WobbleStackRules.GetGustEnvelope(progress);
                float acceleration = WobbleStackRules.GetEffectiveGustAcceleration(
                    _gust.Force,
                    _gust.Direction,
                    envelope) * UnityAccelerationScale;
                float liftAcceleration = 0.000006f * UnityAccelerationScale * envelope;

                for (int index = 0; index < _creatures.Count; index += 1)
                {
                    CreatureBody creature = _creatures[index];
                    Rigidbody2D body = creature.Body;
                    float exposure = 0.9f + (index * 0.22f);
                    float horizontalAcceleration = acceleration * exposure;
                    Vector2 horizontalForce = new Vector2(horizontalAcceleration * body.mass, 0f);
                    body.AddForceAtPosition(horizontalForce, body.worldCenterOfMass + new Vector2(0f, 0.18f));
                    body.AddForce(new Vector2(0f, liftAcceleration * body.mass));
                }
            }

            CheckDangerAndFailure();
            UpdateRouteProgress();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SuspendForInterruption();
            }
            else
            {
                ResumeFailureAfterInterruption();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SuspendForInterruption();
            }
            else
            {
                ResumeFailureAfterInterruption();
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Time.fixedDeltaTime = 1f / 60f;
            if (_creatureMaterial != null)
            {
                Destroy(_creatureMaterial);
            }

            if (_roadMaterial != null)
            {
                Destroy(_roadMaterial);
            }

            GeneratedArt.Release();
        }

        public void RegisterImpact(CreatureBody creature, Vector2 point)
        {
            if (_phase == GamePhase.Playing)
            {
                _lastFailureReason = $"{creature.Kind} touched the road";
                BeginFailure();
            }

            if (_phase != GamePhase.Failing)
            {
                return;
            }

            SpawnImpact(point, creature.Kind);
            if (_firstImpactAt >= 0f)
            {
                return;
            }

            _firstImpactAt = Time.unscaledTime;
            _cameraShake = _reducedMotion ? 0f : 0.18f;
            _audio.PlayImpact();
            TriggerImpactHaptic();
            if (!_reducedMotion)
            {
                Time.timeScale = ImpactSlowMotionScale;
                _slowMotionEndsAt = Time.unscaledTime + ImpactSlowMotionSeconds;
            }
        }

        public void CollectRouteBadge(int badgeId, Vector2 point)
        {
            if (_phase != GamePhase.Playing ||
                badgeId < 0 ||
                badgeId >= _route.Badges.Length)
            {
                return;
            }

            _collectedBadges = Mathf.Min(_collectedBadges + 1, _route.Badges.Length);
            _cameraShake = _reducedMotion ? 0f : Mathf.Max(_cameraShake, 0.045f);
            _audio.PlaySave();
            CreateTransientSprite(
                "Collected Badge",
                GeneratedArt.ImpactStars(),
                point,
                0.9f,
                new Vector2(0f, 0.85f),
                0.72f,
                new Color(1f, 0.84f, 0.28f, 1f),
                0.9f,
                75f,
                0.72f);
        }

        internal void ConfigureGameplayProbe(
            float force,
            int direction,
            float controlAmount,
            int creatureCount,
            float durationSeconds = 5.4f)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            _gameplayProbeActive = true;
            _creatureCount = WobbleStackRules.ClampCreatureCount(creatureCount);
            _phase = GamePhase.Playing;
            _runSeconds = 0f;
            _postGustRecoveryUntil = 0f;
            _postGustRecoveryDirection = 0;
            _platformSpringBlend = 1f;
            _collectedBadges = 0;
            _routeProgress = 0f;
            _friendStopUntil = 0f;
            _runSucceeded = false;
            _firstImpactAt = -1f;
            _slowMotionEndsAt = -1f;
            _failureSuspendedAt = -1f;
            _dangerWasHigh = false;
            _lastFailureReason = string.Empty;
            _cameraShake = 0f;
            _nextWheelDustAt = 0f;
            _saveMessageEndsAt = 0f;
            _controlAmount = Mathf.Clamp(controlAmount, -1f, 1f);
            _pointerActive = true;
            _gustScheduler = new GustScheduler(1u);
            _gust = new GustSample(0.7f, durationSeconds, force, direction, 0.7f);
            _hasGust = true;
            _gustIndex = 0;
            ResetVehicle(true);
            BuildStack(true);
            _startOverlay.SetActive(false);
            _pauseOverlay.SetActive(false);
            _resultsOverlay.SetActive(false);
            _hudRoot.SetActive(true);
        }

        internal GamePhase GetGameplayProbePhase()
        {
            return _phase;
        }

        internal void SetGameplayProbeControlAmount(float controlAmount)
        {
            _controlAmount = Mathf.Clamp(controlAmount, -1f, 1f);
            _pointerActive = true;
        }

        internal void ReleaseGameplayProbeControl()
        {
            _controlAmount = 0f;
            _pointerActive = false;
        }

        internal float GetGameplayProbeMaxDrift()
        {
            float maxDrift = 0f;
            foreach (CreatureBody creature in _creatures)
            {
                maxDrift = Mathf.Max(maxDrift, Mathf.Abs(creature.Body.position.x - _platformBody.position.x));
            }

            return maxDrift;
        }

        internal float GetGameplayProbeMeanDrift()
        {
            if (_creatures.Count == 0)
            {
                return 0f;
            }

            float sum = 0f;
            foreach (CreatureBody creature in _creatures)
            {
                sum += creature.Body.position.x - _wheelBody.position.x;
            }

            return sum / _creatures.Count;
        }

        internal float GetGameplayProbeMeanCreatureVelocity()
        {
            if (_creatures.Count == 0)
            {
                return 0f;
            }

            float sum = 0f;
            foreach (CreatureBody creature in _creatures)
            {
                sum += creature.Body.linearVelocity.x;
            }

            return sum / _creatures.Count;
        }

        internal Vector2 GetGameplayProbeWheelPosition()
        {
            return _wheelBody.position;
        }

        internal float GetGameplayProbeWheelRotation()
        {
            return _wheelBody.rotation;
        }

        internal float GetGameplayProbePlatformRotation()
        {
            return _platformBody.rotation;
        }

        internal float GetGameplayProbePlatformAngularVelocity()
        {
            return _platformBody.angularVelocity;
        }

        internal float GetGameplayProbePlatformVelocity()
        {
            return _platformBody.linearVelocity.x;
        }

        internal float GetGameplayProbeWheelVelocity()
        {
            return _wheelBody.linearVelocity.x;
        }

        internal string GetGameplayProbeFailureReason()
        {
            return _lastFailureReason;
        }

        internal bool GetGameplayProbeWindBalanceWindow()
        {
            return IsWindBalanceWindow();
        }

        internal int GetGameplayProbeWindDirection()
        {
            if (_runSeconds < _postGustRecoveryUntil)
            {
                return _postGustRecoveryDirection;
            }

            return _hasGust ? _gust.Direction : 0;
        }

        internal float GetGameplayProbeWindForce()
        {
            return _hasGust ? _gust.Force : 0f;
        }

        internal bool GetGameplayProbeGustActive()
        {
            return IsGustActive();
        }

        internal bool GetGameplayProbePostGustRecovery()
        {
            return _runSeconds < _postGustRecoveryUntil;
        }

        internal float GetGameplayProbeCameraX()
        {
            return _camera.transform.position.x;
        }

        internal int GetRouteProbeBadgeCount()
        {
            return _collectedBadges;
        }

        internal int GetRouteProbeIndex()
        {
            return _route.Index;
        }

        internal float GetRouteProbeFinishX()
        {
            return _route.FinishX;
        }

        internal float GetRouteProbeProgress()
        {
            return _routeProgress;
        }

        internal void SetRouteProbeProgress(float progress)
        {
            _routeProgress = Mathf.Max(0f, progress);
            _friendStopUntil = 0f;
            _travellingWorld.SetRouteView(_cameraHome.x, _routeProgress);
        }

        internal TravellingWorld GetTravellingWorldProbe()
        {
            return _travellingWorld;
        }

        private void ConfigureRuntime()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Input.multiTouchEnabled = false;
            Time.fixedDeltaTime = 1f / 60f;
            Physics2D.gravity = new Vector2(0f, -9.81f);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _creatureMaterial = new PhysicsMaterial2D("Creature Grip")
            {
                friction = 0.28f,
                bounciness = 0.035f
            };
            _roadMaterial = new PhysicsMaterial2D("Road Grip")
            {
                friction = 1.1f,
                bounciness = 0f
            };
        }

        private void LoadSettings()
        {
            _reducedMotion = PlayerPrefs.GetInt(ReducedMotionPreference, 0) == 1;
            _unlockedRouteIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(UnlockedRoutePreference, 0),
                0,
                RouteDefinition.Count - 1);
            _currentRouteIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(RoutePreference, _unlockedRouteIndex),
                0,
                _unlockedRouteIndex);
            _route = RouteDefinition.Get(_currentRouteIndex);
            _gestureUsed = PlayerPrefs.GetInt(OnboardingPreference, 0) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(ReducedMotionPreference, _reducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(RoutePreference, _currentRouteIndex);
            PlayerPrefs.SetInt(UnlockedRoutePreference, _unlockedRouteIndex);
            PlayerPrefs.SetInt(OnboardingPreference, _gestureUsed ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void BuildScene()
        {
            _worldRoot = new GameObject("World").transform;
            _worldRoot.SetParent(transform, false);
            BuildCamera();
            BuildBackground();
            BuildStage();
            GameObject windObject = new GameObject("Wind");
            windObject.transform.SetParent(_worldRoot, false);
            _windStreaks = windObject.AddComponent<WindStreaks>();
            _windStreaks.Build();

            GameObject audioObject = new GameObject("Game Audio");
            audioObject.transform.SetParent(transform, false);
            _audio = audioObject.AddComponent<GameAudio>();
            _audio.Build();
        }

        private void BuildCamera()
        {
            GameObject cameraObject = new GameObject("Game Camera");
            cameraObject.transform.SetParent(transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 10f;
            _camera.backgroundColor = new Color(0.97f, 0.59f, 0.48f, 1f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            _cameraHome = new Vector3(0f, 0f, -10f);
            cameraObject.transform.position = _cameraHome;
            cameraObject.tag = "MainCamera";
        }

        private void BuildBackground()
        {
            GameObject travellingWorld = new GameObject("Travelling World");
            travellingWorld.transform.SetParent(_worldRoot, false);
            _travellingWorld = travellingWorld.AddComponent<TravellingWorld>();
            _travellingWorld.Build();
            _travellingWorld.ConfigureRoute(_route, this);
        }

        private void BuildStage()
        {
            GameObject road = new GameObject("Road");
            road.transform.SetParent(_worldRoot, false);
            road.transform.position = new Vector3(RoadCenterX, GroundSurfaceY - 1.2f, 0f);
            GameObject roadVisual = new GameObject("Road Visual");
            roadVisual.transform.SetParent(road.transform, false);
            SpriteRenderer roadRenderer = roadVisual.AddComponent<SpriteRenderer>();
            roadRenderer.sprite = GeneratedArt.Road();
            roadRenderer.sortingOrder = -4;
            roadRenderer.drawMode = SpriteDrawMode.Tiled;
            roadRenderer.tileMode = SpriteTileMode.Continuous;
            roadRenderer.size = new Vector2(RoadLength, roadRenderer.sprite.bounds.size.y);
            roadVisual.transform.localScale = new Vector3(
                1f,
                2.4f / roadRenderer.sprite.bounds.size.y,
                1f);
            BoxCollider2D roadCollider = road.AddComponent<BoxCollider2D>();
            roadCollider.size = new Vector2(RoadLength, 2.4f);
            roadCollider.sharedMaterial = _roadMaterial;

            GameObject wheel = new GameObject("Star Wheel");
            wheel.transform.SetParent(_worldRoot, false);
            wheel.transform.position = new Vector3(0f, WheelCenterY, 0f);
            GameObject wheelVisual = new GameObject("Visual");
            wheelVisual.transform.SetParent(wheel.transform, false);
            SpriteRenderer wheelRenderer = wheelVisual.AddComponent<SpriteRenderer>();
            wheelRenderer.sprite = GeneratedArt.Fulcrum();
            wheelRenderer.material = GeneratedArt.ChromaMaterial;
            wheelRenderer.sortingOrder = 12;
            FitHeight(
                wheelVisual.transform,
                wheelRenderer.sprite,
                (WheelRadius * 2f) / WheelSpriteFillRatio);
            _wheelBody = wheel.AddComponent<Rigidbody2D>();
            _wheelBody.bodyType = RigidbodyType2D.Kinematic;
            _wheelBody.mass = 6f;
            _wheelBody.gravityScale = 1.5f;
            _wheelBody.linearDamping = 0.08f;
            _wheelBody.angularDamping = 0.08f;
            _wheelBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _wheelBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CircleCollider2D wheelCollider = wheel.AddComponent<CircleCollider2D>();
            wheelCollider.radius = WheelRadius;
            wheelCollider.sharedMaterial = _roadMaterial;

            GameObject platform = new GameObject("Seesaw Beam");
            platform.transform.SetParent(_worldRoot, false);
            platform.transform.position = new Vector3(0f, PlatformY, 0f);
            SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
            renderer.sprite = GeneratedArt.Beam();
            renderer.material = GeneratedArt.ChromaMaterial;
            renderer.sortingOrder = 15;
            FitWidth(platform.transform, renderer.sprite, PlatformWidth + 0.35f);
            _platformBody = platform.AddComponent<Rigidbody2D>();
            _platformBody.bodyType = RigidbodyType2D.Kinematic;
            _platformBody.mass = 4.2f;
            _platformBody.gravityScale = 1f;
            _platformBody.linearDamping = 0.16f;
            _platformBody.angularDamping = 1.15f;
            _platformBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _platformBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D collider = platform.AddComponent<CapsuleCollider2D>();
            float platformScale = platform.transform.localScale.x;
            collider.size = new Vector2(PlatformWidth / platformScale, PlatformHeight / platformScale);
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.sharedMaterial = _creatureMaterial;

            _wheelJoint = platform.AddComponent<WheelJoint2D>();
            _wheelJoint.connectedBody = _wheelBody;
            _wheelJoint.autoConfigureConnectedAnchor = false;
            _wheelJoint.anchor = platform.transform.InverseTransformPoint(wheel.transform.position);
            _wheelJoint.connectedAnchor = Vector2.zero;
            _wheelJoint.enableCollision = false;
            JointSuspension2D suspension = _wheelJoint.suspension;
            suspension.angle = 90f;
            suspension.frequency = 8f;
            suspension.dampingRatio = 0.92f;
            _wheelJoint.suspension = suspension;
            _wheelJoint.useMotor = false;
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject("Game UI");
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1179f, 2556f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.6f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject safeRoot = new GameObject("Safe Area");
            safeRoot.transform.SetParent(canvasObject.transform, false);
            RectTransform safeRect = safeRoot.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            safeRoot.AddComponent<SafeAreaFitter>();

            BuildHud(safeRoot.transform);
            BuildStartOverlay(safeRoot.transform);
            BuildPauseOverlay(safeRoot.transform);
            BuildResultsOverlay(safeRoot.transform);
            EnsureEventSystem();
        }

        private void BuildHud(Transform parent)
        {
            _hudRoot = new GameObject("HUD");
            _hudRoot.transform.SetParent(parent, false);
            RectTransform hudRect = _hudRoot.AddComponent<RectTransform>();
            Stretch(hudRect);
            Transform hud = _hudRoot.transform;

            GameObject scorePlate = CreateImage("Badge Plate", hud, GeneratedArt.CocoaPlate(), new Vector2(0f, 1f), new Vector2(170f, -100f), new Vector2(280f, 128f));
            _scoreText = CreateText("Badge Count", scorePlate.transform, "★  0/7", 46, TextAnchor.MiddleCenter, Color.white);
            Stretch(_scoreText.rectTransform);

            CreateButton("Pause", hud, "Ⅱ", new Vector2(1f, 1f), new Vector2(-104f, -105f), new Vector2(132f, 132f), GeneratedArt.CocoaPlate(), PauseRun, 55);

            _saveText = CreateText("Save Feedback", hud, "NICE SAVE!", 54, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.58f, 1f));
            RectTransform saveRect = _saveText.rectTransform;
            saveRect.anchorMin = new Vector2(0.5f, 0.68f);
            saveRect.anchorMax = new Vector2(0.5f, 0.68f);
            saveRect.sizeDelta = new Vector2(700f, 100f);
            saveRect.anchoredPosition = Vector2.zero;
            _saveText.gameObject.SetActive(false);

            _hintText = CreateText("First Run Hint", hud, "TOUCH · SLIDE TO ROLL THE WHEEL", 32, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.8f, 0.96f));
            RectTransform hintRect = _hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0.16f);
            hintRect.anchorMax = new Vector2(0.5f, 0.16f);
            hintRect.anchoredPosition = Vector2.zero;
            hintRect.sizeDelta = new Vector2(760f, 90f);
            _hintText.gameObject.SetActive(false);

            _gestureTrack = CreateImage(
                "Thumb Track",
                hud,
                GeneratedArt.CocoaPlate(),
                new Vector2(0.5f, 0.11f),
                Vector2.zero,
                new Vector2(340f, 16f));
            _gestureTrack.GetComponent<Image>().color = new Color(1f, 0.95f, 0.82f, 0.28f);
            _gestureTrack.SetActive(false);

            GameObject cue = CreateImage(
                "Thumb Cue",
                hud,
                GeneratedArt.CoralPlate(),
                new Vector2(0.5f, 0.11f),
                Vector2.zero,
                new Vector2(70f, 62f));
            _gestureCue = cue.GetComponent<RectTransform>();
            cue.GetComponent<Image>().color = new Color(1f, 0.93f, 0.75f, 0.82f);
            cue.SetActive(false);
        }

        private void BuildStartOverlay(Transform parent)
        {
            _startOverlay = CreateOverlay("Start Overlay", parent);
            Text title = CreateText("Title", _startOverlay.transform, "WOBBLE\nSTACK", 102, TextAnchor.MiddleCenter, Color.white);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -285f);
            titleRect.sizeDelta = new Vector2(850f, 250f);

            Text subtitle = CreateText("Subtitle", _startOverlay.transform, "FIVE FRIENDS · ONE WHEEL · SUNSET AHEAD", 29, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.84f, 0.95f));
            RectTransform subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0.5f, 1f);
            subtitleRect.anchorMax = new Vector2(0.5f, 1f);
            subtitleRect.anchoredPosition = new Vector2(0f, -460f);
            subtitleRect.sizeDelta = new Vector2(960f, 80f);

            _routeText = CreateText("Route Name", _startOverlay.transform, "ORCHARD ROAD", 39, TextAnchor.MiddleCenter, Color.white);
            RectTransform routeRect = _routeText.rectTransform;
            routeRect.anchorMin = new Vector2(0.5f, 0.17f);
            routeRect.anchorMax = new Vector2(0.5f, 0.17f);
            routeRect.sizeDelta = new Vector2(760f, 90f);
            _routeText.raycastTarget = true;
            Button routeButton = _routeText.gameObject.AddComponent<Button>();
            routeButton.targetGraphic = _routeText;
            routeButton.onClick.AddListener(CycleRoute);

            _routeSubtitleText = CreateText("Route Goal", _startOverlay.transform, "FIND RABBIT AND JELLY", 24, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.74f, 0.96f));
            RectTransform routeSubtitleRect = _routeSubtitleText.rectTransform;
            routeSubtitleRect.anchorMin = new Vector2(0.5f, 0.135f);
            routeSubtitleRect.anchorMax = new Vector2(0.5f, 0.135f);
            routeSubtitleRect.sizeDelta = new Vector2(820f, 70f);

            CreateButton("Play", _startOverlay.transform, "ROLL", new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(560f, 220f), GeneratedArt.CoralPlate(), StartRun, 70);
        }

        private void BuildPauseOverlay(Transform parent)
        {
            _pauseOverlay = CreateOverlay("Pause Overlay", parent);
            Image veil = _pauseOverlay.AddComponent<Image>();
            veil.color = new Color(0.25f, 0.12f, 0.2f, 0.48f);
            Text title = CreateText("Paused", _pauseOverlay.transform, "PAUSED", 92, TextAnchor.MiddleCenter, Color.white);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.6f);
            titleRect.anchorMax = new Vector2(0.5f, 0.6f);
            titleRect.sizeDelta = new Vector2(700f, 160f);
            CreateButton("Resume", _pauseOverlay.transform, "KEEP ROLLING", new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(620f, 210f), GeneratedArt.CoralPlate(), ResumeRun, 48);
            _motionText = CreateButton(
                "Motion",
                _pauseOverlay.transform,
                "MOTION FULL",
                new Vector2(0.5f, 0.3f),
                Vector2.zero,
                new Vector2(390f, 115f),
                GeneratedArt.CocoaPlate(),
                ToggleReducedMotion,
                28).GetComponentInChildren<Text>();
            _pauseOverlay.SetActive(false);
        }

        private void BuildResultsOverlay(Transform parent)
        {
            _resultsOverlay = CreateOverlay("Results Overlay", parent);
            _resultTitleText = CreateText("Result Title", _resultsOverlay.transform, "EVERYONE\nPANICKED", 76, TextAnchor.MiddleCenter, Color.white);
            RectTransform titleRect = _resultTitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.76f);
            titleRect.anchorMax = new Vector2(0.5f, 0.76f);
            titleRect.sizeDelta = new Vector2(820f, 230f);
            _resultTimeText = CreateText("Result Badges", _resultsOverlay.transform, "0/7 BADGES", 50, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.78f, 1f));
            RectTransform timeRect = _resultTimeText.rectTransform;
            timeRect.anchorMin = new Vector2(0.5f, 0.64f);
            timeRect.anchorMax = new Vector2(0.5f, 0.64f);
            timeRect.sizeDelta = new Vector2(700f, 100f);
            _resultBestText = CreateText("Result Best", _resultsOverlay.transform, "BEST 0/7", 30, TextAnchor.MiddleCenter, Color.white);
            RectTransform resultBestRect = _resultBestText.rectTransform;
            resultBestRect.anchorMin = new Vector2(0.5f, 0.595f);
            resultBestRect.anchorMax = new Vector2(0.5f, 0.595f);
            resultBestRect.sizeDelta = new Vector2(650f, 70f);
            GameObject resultAction = CreateButton(
                "Result Action",
                _resultsOverlay.transform,
                "RETRY",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 190f),
                new Vector2(570f, 225f),
                GeneratedArt.CoralPlate(),
                StartResultAction,
                68);
            _resultActionText = resultAction.GetComponentInChildren<Text>();
            _resultsOverlay.SetActive(false);
        }

        private void BuildStack(bool dynamicBodies)
        {
            ClearStack();
            CreatureSpec[] specs = CreatureSpec.All;
            float bottom = _platformBody.position.y + (PlatformHeight * 0.5f);
            float contactInset = dynamicBodies ? DynamicStackContactInset : StaticStackContactInset;

            for (int index = 0; index < _creatureCount; index += 1)
            {
                CreatureSpec spec = specs[index];
                float y = bottom + (spec.ColliderSize.y * 0.5f);
                bottom += spec.ColliderSize.y - contactInset;
                CreatureBody creature = CreateCreature(
                    spec,
                    index,
                    new Vector2(_platformBody.position.x, y),
                    dynamicBodies);
                _creatures.Add(creature);
            }

            for (int index = 1; index < _creatures.Count; index += 1)
            {
                _creatures[index].ConfigureLowerNeighbor(_creatures[index - 1], index);
            }
        }

        private void ResetVehicle(bool dynamicBodies)
        {
            _wheelJoint.useMotor = false;
            _platformBody.bodyType = RigidbodyType2D.Kinematic;
            _wheelBody.bodyType = RigidbodyType2D.Kinematic;
            _platformBody.position = new Vector2(0f, PlatformY);
            _platformBody.rotation = 0f;
            _platformBody.linearVelocity = Vector2.zero;
            _platformBody.angularVelocity = 0f;
            _wheelBody.position = new Vector2(0f, WheelCenterY);
            _wheelBody.rotation = 0f;
            _wheelBody.linearVelocity = Vector2.zero;
            _wheelBody.angularVelocity = 0f;
            _cameraFollowVelocity = 0f;
            _cameraHome = new Vector3(0f, 0f, -10f);
            _camera.transform.position = _cameraHome;
            _travellingWorld.SetRouteView(0f, _routeProgress);
            _windStreaks.SetCenterX(0f);
            Physics2D.SyncTransforms();

            if (!dynamicBodies)
            {
                return;
            }

            _platformBody.bodyType = RigidbodyType2D.Dynamic;
            _wheelBody.bodyType = RigidbodyType2D.Dynamic;
            _wheelJoint.useMotor = true;
        }

        private CreatureBody CreateCreature(CreatureSpec spec, int index, Vector2 position, bool dynamicBody)
        {
            GameObject bodyObject = new GameObject($"Creature {index + 1} {spec.Kind}");
            bodyObject.transform.SetParent(_worldRoot, false);
            bodyObject.transform.position = position;
            Rigidbody2D body = bodyObject.AddComponent<Rigidbody2D>();
            body.bodyType = dynamicBody ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearDamping = 0.55f;
            body.angularDamping = 0.7f;
            body.freezeRotation = false;

            Collider2D collider;
            if (spec.Kind == CharacterKind.Cube)
            {
                BoxCollider2D box = bodyObject.AddComponent<BoxCollider2D>();
                box.size = spec.ColliderSize;
                collider = box;
            }
            else
            {
                float bottomRatio = spec.Kind == CharacterKind.Jelly ? 0.82f : 0.76f;
                float topRatio = spec.Kind == CharacterKind.Rabbit ? 0.7f : 0.82f;
                collider = CreateFlatRoundedCollider(bodyObject, spec.ColliderSize, bottomRatio, topRatio);
            }

            collider.sharedMaterial = _creatureMaterial;
            body.useAutoMass = true;

            CreatureRig rig = bodyObject.AddComponent<CreatureRig>();
            rig.Initialize(spec.Kind, 30 + (index * 12), index * 1.37f);
            CreatureBody creature = bodyObject.AddComponent<CreatureBody>();
            creature.Initialize(
                this,
                spec.Kind,
                rig);
            if (index == 0)
            {
                creature.ConfigureRecoveryAnchor(_platformBody);
            }

            return creature;
        }

        private static PolygonCollider2D CreateFlatRoundedCollider(
            GameObject target,
            Vector2 size,
            float bottomRatio,
            float topRatio)
        {
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;
            PolygonCollider2D collider = target.AddComponent<PolygonCollider2D>();
            collider.points = new[]
            {
                new Vector2(-halfWidth * bottomRatio, -halfHeight),
                new Vector2(halfWidth * bottomRatio, -halfHeight),
                new Vector2(halfWidth, -halfHeight * 0.46f),
                new Vector2(halfWidth * 0.94f, halfHeight * 0.42f),
                new Vector2(halfWidth * topRatio, halfHeight),
                new Vector2(-halfWidth * topRatio, halfHeight),
                new Vector2(-halfWidth * 0.94f, halfHeight * 0.42f),
                new Vector2(-halfWidth, -halfHeight * 0.46f)
            };
            return collider;
        }

        private void ClearStack()
        {
            foreach (CreatureBody creature in _creatures)
            {
                if (creature != null)
                {
                    creature.gameObject.SetActive(false);
                    Destroy(creature.gameObject);
                }
            }

            _creatures.Clear();
            if (_crownObject != null)
            {
                _crownObject.SetActive(false);
                Destroy(_crownObject);
                _crownObject = null;
            }
        }

        private void StartRun()
        {
            if (_phase != GamePhase.Ready && _phase != GamePhase.Results)
            {
                return;
            }

            Time.timeScale = 1f;
            AudioListener.pause = false;
            _gameplayProbeActive = false;
            _runCount += 1;
            _runSeconds = 0f;
            _postGustRecoveryUntil = 0f;
            _postGustRecoveryDirection = 0;
            _platformSpringBlend = 1f;
            _collectedBadges = 0;
            _routeProgress = 0f;
            _friendStopUntil = 0f;
            _nextJoinIndex = 0;
            _runSucceeded = false;
            _pointerActive = false;
            _controlAmount = 0f;
            _gustIndex = 0;
            _firstImpactAt = -1f;
            _slowMotionEndsAt = -1f;
            _failureSuspendedAt = -1f;
            _dangerWasHigh = false;
            _lastFailureReason = string.Empty;
            _cameraShake = 0f;
            _nextWheelDustAt = 0f;
            _route = RouteDefinition.Get(_currentRouteIndex);
            _creatureCount = _route.InitialCreatureCount;
            _travellingWorld.ConfigureRoute(_route, this);
            _gustScheduler = new GustScheduler(Convert.ToUInt32(
                7907 + (_runCount * 101) + (_route.Index * 1301)));
            _routeGustSequenceIndex = 0;
            _gust = NextGust(0f);
            _hasGust = true;
            ResetVehicle(true);
            BuildStack(true);
            _phase = GamePhase.Playing;
            _startOverlay.SetActive(false);
            _pauseOverlay.SetActive(false);
            _resultsOverlay.SetActive(false);
            _hudRoot.SetActive(true);
            _saveText.gameObject.SetActive(false);
            _hintText.gameObject.SetActive(!_gestureUsed);
            _gestureTrack.SetActive(!_gestureUsed);
            _gestureCue.gameObject.SetActive(!_gestureUsed);
            _audio.PlayStart();
            SaveSettings();
        }

        private void PauseRun()
        {
            if (_phase != GamePhase.Playing)
            {
                return;
            }

            _phase = GamePhase.Paused;
            _pointerActive = false;
            _controlAmount = 0f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            _pauseOverlay.SetActive(true);
            _audio.PlayClick();
        }

        private void ResumeRun()
        {
            if (_phase != GamePhase.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            AudioListener.pause = false;
            _phase = GamePhase.Playing;
            _pauseOverlay.SetActive(false);
            _audio.PlayClick();
        }

        private void BeginFailure()
        {
            if (_phase != GamePhase.Playing)
            {
                return;
            }

            _phase = GamePhase.Failing;
            _runSucceeded = false;
            _failureStartedAt = Time.unscaledTime;
            _firstImpactAt = -1f;
            _slowMotionEndsAt = -1f;
            _failureSuspendedAt = -1f;
            _pointerActive = false;
            _controlAmount = 0f;
            _wheelJoint.useMotor = false;
            _windStreaks.SetWind(1, 0f);
            _audio.SetWind(0f);
            foreach (CreatureBody creature in _creatures)
            {
                creature.BeginFallReaction();
            }

            SpawnCrown();
        }

        private void ShowResults()
        {
            if (_phase != GamePhase.Failing && _phase != GamePhase.Finishing)
            {
                return;
            }

            Time.timeScale = 1f;
            _phase = GamePhase.Results;
            int best = GetBestBadgeCount();
            if (_collectedBadges > best)
            {
                best = _collectedBadges;
                PlayerPrefs.SetInt(BestBadgeKey(), best);
                PlayerPrefs.Save();
            }

            foreach (CreatureBody creature in _creatures)
            {
                creature.Body.linearVelocity = Vector2.zero;
                creature.Body.angularVelocity = 0f;
                creature.Body.simulated = false;
            }

            _platformBody.linearVelocity = Vector2.zero;
            _platformBody.angularVelocity = 0f;
            _wheelBody.linearVelocity = Vector2.zero;
            _wheelBody.angularVelocity = 0f;
            _resultTitleText.text = _runSucceeded
                ? "SUNSET!\nEVERYONE MADE IT"
                : "EVERYONE\nPANICKED";
            _resultTimeText.text = $"{_collectedBadges}/{_route.Badges.Length} BADGES";
            _resultBestText.text = $"BEST {best}/{_route.Badges.Length}";
            _resultActionText.text = _runSucceeded
                ? _route.Index < RouteDefinition.Count - 1
                    ? "NEXT ROAD"
                    : "ROLL AGAIN"
                : "RETRY";
            _hudRoot.SetActive(false);
            _saveText.gameObject.SetActive(false);
            _hintText.gameObject.SetActive(false);
            _gestureCue.gameObject.SetActive(false);
            _resultsOverlay.SetActive(true);
        }

        private void StartResultAction()
        {
            if (_phase != GamePhase.Results)
            {
                return;
            }

            if (_runSucceeded && _route.Index < RouteDefinition.Count - 1)
            {
                _currentRouteIndex = Mathf.Min(
                    _route.Index + 1,
                    _unlockedRouteIndex);
                _route = RouteDefinition.Get(_currentRouteIndex);
            }

            StartRun();
        }

        private void ShowReady()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            _phase = GamePhase.Ready;
            _gameplayProbeActive = false;
            _runSeconds = 0f;
            _postGustRecoveryUntil = 0f;
            _postGustRecoveryDirection = 0;
            _platformSpringBlend = 1f;
            _collectedBadges = 0;
            _routeProgress = 0f;
            _friendStopUntil = 0f;
            _controlAmount = 0f;
            _hasGust = false;
            _runSucceeded = false;
            _route = RouteDefinition.Get(_currentRouteIndex);
            _creatureCount = WobbleStackRules.MaxCreatureCount;
            _windStreaks.SetWind(1, 0f);
            _audio.SetWind(0f);
            _travellingWorld.ConfigureRoute(_route, this);
            ResetVehicle(false);
            BuildStack(false);
            _startOverlay.SetActive(true);
            _pauseOverlay.SetActive(false);
            _resultsOverlay.SetActive(false);
            _hudRoot.SetActive(false);
            _hintText.gameObject.SetActive(false);
            _gestureCue.gameObject.SetActive(false);
            UpdateSetupLabels();
        }

        private void ToggleReducedMotion()
        {
            if (_phase != GamePhase.Ready && _phase != GamePhase.Paused)
            {
                return;
            }

            _reducedMotion = !_reducedMotion;
            SaveSettings();
            UpdateSetupLabels();
            _audio.PlayClick();
        }

        private void CycleRoute()
        {
            if (_phase != GamePhase.Ready || _unlockedRouteIndex <= 0)
            {
                return;
            }

            _currentRouteIndex = _currentRouteIndex >= _unlockedRouteIndex
                ? 0
                : _currentRouteIndex + 1;
            _route = RouteDefinition.Get(_currentRouteIndex);
            _travellingWorld.ConfigureRoute(_route, this);
            UpdateSetupLabels();
            SaveSettings();
            _audio.PlayClick();
        }

        private void ReadInput()
        {
            int keyboardDirection = 0;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                keyboardDirection = -1;
            }
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                keyboardDirection = 1;
            }

            if (keyboardDirection != 0)
            {
                _pointerActive = false;
                _controlAmount = keyboardDirection;
                return;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        _pointerActive = false;
                        _controlAmount = 0f;
                        return;
                    }

                    BeginPointer(touch.position.x);
                    return;
                }

                if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && _pointerActive)
                {
                    SetPointerTarget(touch.position.x);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _pointerActive = false;
                    _controlAmount = 0f;
                }

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    BeginPointer(Input.mousePosition.x);
                }
            }
            else if (Input.GetMouseButton(0) && _pointerActive)
            {
                SetPointerTarget(Input.mousePosition.x);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _pointerActive = false;
                _controlAmount = 0f;
            }
            else if (!_pointerActive)
            {
                _controlAmount = 0f;
            }
        }

        private void SetPointerTarget(float screenX)
        {
            _controlAmount = WobbleStackRules.GetRelativeDriveAmount(
                _pointerOriginX,
                screenX,
                Screen.width,
                PointerTravelFraction);
        }

        private void BeginPointer(float screenX)
        {
            _pointerActive = true;
            _pointerOriginX = screenX;
            _controlAmount = 0f;
        }

        private void UpdateWheelDrive()
        {
            if (_runSeconds < _friendStopUntil)
            {
                JointMotor2D stopMotor = _wheelJoint.motor;
                stopMotor.motorSpeed = 0f;
                stopMotor.maxMotorTorque = WheelDriveTorque;
                _wheelJoint.motor = stopMotor;
                _wheelJoint.useMotor = true;
                return;
            }

            float driveAmount =
                !_gameplayProbeActive && !_pointerActive
                    ? RouteForwardCruiseAmount
                    : _controlAmount;
            float inputMagnitude = Mathf.Abs(driveAmount);
            if (inputMagnitude <= 0f)
            {
                _wheelJoint.useMotor = false;
                return;
            }

            bool balanceWindow = IsWindBalanceWindow();
            bool recovering = _runSeconds < _postGustRecoveryUntil;
            float shapedMagnitude = Mathf.Sqrt(inputMagnitude);
            float boost = balanceWindow
                ? Mathf.InverseLerp(0.65f, 1f, inputMagnitude)
                : 0f;
            float direction = driveAmount < 0f ? -1f : 1f;
            float baseMotorSpeed = recovering
                ? WheelRecoveryMotorSpeed
                : balanceWindow
                    ? WheelMotorSpeed
                    : WheelCruiseMotorSpeed;
            JointMotor2D motor = _wheelJoint.motor;
            motor.motorSpeed = -direction *
                ((shapedMagnitude * baseMotorSpeed) +
                    (boost * boost * WheelCatchBoostSpeed));
            motor.maxMotorTorque = recovering
                ? WheelRecoveryMotorTorque
                : balanceWindow
                    ? Mathf.Lerp(WheelBrakeTorque, WheelDriveTorque, shapedMagnitude)
                    : WheelCruiseMotorTorque;
            _wheelJoint.motor = motor;
            _wheelJoint.useMotor = true;
        }

        private void UpdateFriendStopBraking()
        {
            if (_runSeconds >= _friendStopUntil)
            {
                return;
            }

            ApplyHorizontalBrake(_wheelBody, 7f);
            ApplyHorizontalBrake(_platformBody, 7f);
            foreach (CreatureBody creature in _creatures)
            {
                ApplyHorizontalBrake(creature.Body, 7f);
            }
        }

        private static void ApplyHorizontalBrake(Rigidbody2D body, float damping)
        {
            body.AddForce(new Vector2(
                -body.linearVelocity.x * body.mass * damping,
                0f));
        }

        private bool IsWindBalanceWindow()
        {
            if (_runSeconds < _postGustRecoveryUntil)
            {
                return true;
            }

            if (!_hasGust || _gust.Force <= 0f)
            {
                return false;
            }

            return IsGustActive() ||
                _gust.StartsAtSeconds - _runSeconds <= WindBalancePreviewSeconds;
        }

        private void UpdatePlatformSuspension()
        {
            bool activeGust =
                IsGustActive() &&
                !IsGustRecoveryTail() &&
                _gust.Force > 0f;
            float targetBlend = activeGust ? 0f : 1f;
            _platformSpringBlend = Mathf.MoveTowards(
                _platformSpringBlend,
                targetBlend,
                Time.fixedDeltaTime * (targetBlend < _platformSpringBlend ? 8f : 2.5f));
            if (_platformSpringBlend > 0f)
            {
                float springTorque =
                    (-_platformBody.rotation * PlatformSpringTorquePerDegree) -
                    (_platformBody.angularVelocity * PlatformSpringDamping);
                _platformBody.AddTorque(Mathf.Clamp(
                    springTorque,
                    -PlatformSpringMaximumTorque,
                    PlatformSpringMaximumTorque) * _platformSpringBlend);
            }

            float absoluteAngle = Mathf.Abs(_platformBody.rotation);
            if (absoluteAngle <= PlatformAxleStopDegrees)
            {
                return;
            }

            float stopDirection = -Mathf.Sign(_platformBody.rotation);
            float stopTorque =
                ((absoluteAngle - PlatformAxleStopDegrees) *
                    PlatformAxleStopTorquePerDegree) +
                (Mathf.Abs(_platformBody.angularVelocity) * PlatformAxleStopDamping);
            _platformBody.AddTorque(stopDirection * Mathf.Min(
                stopTorque,
                PlatformSpringMaximumTorque));
        }

        private void UpdateBalanceTransitionDamping()
        {
            if (IsGustActive() && !IsGustRecoveryTail())
            {
                return;
            }

            bool recovering = IsRecoveryWindow();
            float velocityDamping = recovering ? 8f : 2.2f;
            float positionStiffness = recovering ? 9f : 1.8f;
            float maximumAcceleration = recovering ? 15f : 4f;
            float platformVelocity = _platformBody.linearVelocity.x;
            float platformX = _platformBody.position.x;
            float reactionForce = 0f;
            foreach (CreatureBody creature in _creatures)
            {
                float relativeVelocity = creature.Body.linearVelocity.x - platformVelocity;
                float acceleration =
                    (-relativeVelocity * velocityDamping) +
                    ((platformX - creature.Body.position.x) * positionStiffness);

                float force = Mathf.Clamp(
                    acceleration,
                    -maximumAcceleration,
                    maximumAcceleration) * creature.Body.mass;
                creature.Body.AddForce(new Vector2(force, 0f));
                reactionForce -= force;
            }

            _platformBody.AddForce(new Vector2(reactionForce, 0f));
        }

        private void UpdateRecoveryGrips()
        {
            bool recovering = IsRecoveryWindow();
            foreach (CreatureBody creature in _creatures)
            {
                creature.SetRecoveryGrip(recovering);
            }
        }

        private void UpdateWheelDust()
        {
            float speed = Mathf.Abs(_wheelBody.linearVelocity.x);
            if (speed < 1.15f || Time.time < _nextWheelDustAt)
            {
                return;
            }

            RaycastHit2D hit = Physics2D.Raycast(
                _wheelBody.position,
                Vector2.down,
                WheelRadius + 0.18f);
            if (hit.collider == null || hit.collider.gameObject.name != "Road")
            {
                return;
            }

            _nextWheelDustAt = Time.time + Mathf.Lerp(0.22f, 0.11f, Mathf.InverseLerp(1.15f, 7f, speed));
            float direction = Mathf.Sign(_wheelBody.linearVelocity.x);
            CreateTransientSprite(
                "Wheel Dust",
                GeneratedArt.Dust(),
                hit.point + new Vector2(-direction * 0.18f, 0.08f),
                0.32f,
                new Vector2(-direction * 0.38f, 0.28f),
                0.38f,
                new Color(1f, 0.87f, 0.72f, 0.68f),
                0.18f,
                -direction * 18f,
                1.45f);
        }

        private void UpdateGust()
        {
            if (!_hasGust)
            {
                _gust = NextGust(_runSeconds);
                _hasGust = true;
            }

            if (_runSeconds >= _gust.EndsAtSeconds)
            {
                _postGustRecoveryUntil = _runSeconds + PostGustRecoverySeconds;
                _postGustRecoveryDirection = _gust.Direction;
                _gust = NextGust(
                    _gust.EndsAtSeconds + PostGustRecoverySeconds);
                _gustIndex += 1;
            }

            if (!IsGustActive())
            {
                float preview = WobbleStackRules.GetWindPreviewEnvelope(_gust.StartsAtSeconds - _runSeconds);
                float previewForceRatio = WobbleStackRules.GetGustIntensity(_gust.Force);
                float previewIntensity = preview <= 0f
                    ? 0f
                    : Mathf.Clamp01(
                        Mathf.Lerp(0.08f, 0.3f, preview) *
                        Mathf.Lerp(0.75f, 1.25f, previewForceRatio));
                _windStreaks.SetWind(_gust.Direction, previewIntensity);
                _audio.SetWind(previewIntensity * 0.24f);
                foreach (CreatureBody creature in _creatures)
                {
                    creature.SetWind(_gust.Direction * previewIntensity);
                }
                return;
            }

            float progress = (_runSeconds - _gust.StartsAtSeconds) / _gust.DurationSeconds;
            float envelope = WobbleStackRules.GetGustEnvelope(progress);
            float forceRatio = WobbleStackRules.GetGustIntensity(_gust.Force);
            float visibleEnvelope = Mathf.Lerp(0.34f, 1f, envelope);
            float visibleIntensity = visibleEnvelope * Mathf.Lerp(0.58f, 1f, forceRatio);
            _windStreaks.SetWind(_gust.Direction, visibleIntensity);
            _audio.SetWind(envelope * Mathf.Lerp(0.35f, 1f, forceRatio));
            foreach (CreatureBody creature in _creatures)
            {
                creature.SetWind(_gust.Direction * visibleIntensity);
            }
        }

        private bool IsGustActive()
        {
            return _hasGust && _runSeconds >= _gust.StartsAtSeconds && _runSeconds < _gust.EndsAtSeconds;
        }

        private GustSample NextGust(float previousEndsAtSeconds)
        {
            GustSample sampled = _gustScheduler.Next(previousEndsAtSeconds);
            if (_gameplayProbeActive)
            {
                return sampled;
            }

            int direction;
            if (_route.Index == 0)
            {
                direction = _routeGustSequenceIndex % 3 == 2 ? -1 : 1;
            }
            else if (_route.Index == 1)
            {
                direction = (_routeGustSequenceIndex & 1) == 0 ? 1 : -1;
            }
            else
            {
                direction = sampled.Direction;
            }

            _routeGustSequenceIndex += 1;
            return new GustSample(
                sampled.RestSeconds,
                sampled.DurationSeconds,
                sampled.Force,
                direction,
                sampled.StartsAtSeconds);
        }

        private bool IsGustRecoveryTail()
        {
            if (!IsGustActive() || _gust.DurationSeconds <= 0f)
            {
                return false;
            }

            float progress = (_runSeconds - _gust.StartsAtSeconds) / _gust.DurationSeconds;
            return progress >= 0.82f;
        }

        private bool IsRecoveryWindow()
        {
            return _runSeconds < _postGustRecoveryUntil || IsGustRecoveryTail();
        }

        private void CheckDangerAndFailure()
        {
            if (_runSeconds < 1.1f)
            {
                return;
            }

            float maxDrift = 0f;
            for (int index = 0; index < _creatures.Count; index += 1)
            {
                CreatureBody creature = _creatures[index];
                float localDrift = Mathf.Abs(creature.Body.position.x - _platformBody.position.x);
                maxDrift = Mathf.Max(maxDrift, localDrift);
                if (creature.Body.position.y < PlatformY - 1.15f)
                {
                    _lastFailureReason = $"{creature.Kind} fell below the beam";
                    BeginFailure();
                    return;
                }

                if (index > 0)
                {
                    float verticalGap =
                        creature.Body.position.y -
                        _creatures[index - 1].Body.position.y;
                    bool gripCanRecover =
                        (creature.HasActiveGrip ||
                            creature.IsRecoveryGripRequested);
                    if (verticalGap < -0.12f && !gripCanRecover)
                    {
                        _lastFailureReason =
                            $"{creature.Kind} lost vertical stack order " +
                            $"(gap={verticalGap:0.00}, grip={creature.HasActiveGrip})";
                        BeginFailure();
                        return;
                    }
                }
            }

            float danger = Mathf.Max(maxDrift / 2.2f, Mathf.Abs(_platformBody.rotation) / 20f);
            if (danger > 0.72f)
            {
                _dangerWasHigh = true;
            }
            else if (_dangerWasHigh && danger < 0.34f && IsGustActive())
            {
                _dangerWasHigh = false;
                _saveMessageEndsAt = Time.unscaledTime + 0.7f;
                foreach (CreatureBody creature in _creatures)
                {
                    creature.ShowReliefReaction();
                }

                _saveText.gameObject.SetActive(true);
                _cameraShake = _reducedMotion ? 0f : Mathf.Max(_cameraShake, 0.04f);
                _audio.PlaySave();
                TriggerSaveHaptic();
            }
        }

        private void UpdateRouteProgress()
        {
            if (_phase != GamePhase.Playing || _gameplayProbeActive)
            {
                return;
            }

            if (_runSeconds < _friendStopUntil)
            {
                return;
            }

            float wheelSurfaceSpeed =
                Mathf.Abs(_wheelBody.angularVelocity) *
                Mathf.Deg2Rad *
                WheelRadius;
            float routeSpeed = Mathf.Clamp(
                0.45f + (wheelSurfaceSpeed * 0.75f),
                0.45f,
                2.4f);
            _routeProgress += routeSpeed * Time.fixedDeltaTime;
            _travellingWorld.SetRouteView(_cameraHome.x, _routeProgress);

            while (_nextJoinIndex < _route.JoinStops.Length &&
                _routeProgress >= _route.JoinStops[_nextJoinIndex])
            {
                AddFriendAtSafeStop();
                _nextJoinIndex += 1;
            }

            if (_routeProgress >= _route.FinishX)
            {
                BeginFinish();
            }
        }

        private void AddFriendAtSafeStop()
        {
            if (_creatureCount >= WobbleStackRules.MaxCreatureCount ||
                _creatures.Count == 0)
            {
                return;
            }

            CreatureSpec spec = CreatureSpec.All[_creatureCount];
            CreatureBody lower = _creatures[_creatures.Count - 1];
            Collider2D lowerCollider = lower.GetComponent<Collider2D>();
            Vector2 position = new Vector2(
                lower.Body.position.x,
                lowerCollider.bounds.max.y + (spec.ColliderSize.y * 0.5f) - DynamicStackContactInset);
            CreatureBody friend = CreateCreature(spec, _creatureCount, position, true);
            friend.Body.linearVelocity = lower.Body.linearVelocity;
            friend.Body.angularVelocity = lower.Body.angularVelocity * 0.35f;
            friend.ConfigureLowerNeighbor(lower, _creatureCount);
            friend.HoldRecoveryGripFor(12f);
            friend.ShowReliefReaction();
            _creatures.Add(friend);
            _creatureCount += 1;

            _friendStopUntil = _runSeconds + 1.35f;
            _postGustRecoveryUntil = Mathf.Max(
                _postGustRecoveryUntil,
                _runSeconds + 3.2f);
            _postGustRecoveryDirection = _hasGust
                ? _gust.Direction
                : 1;
            _gust = NextGust(_postGustRecoveryUntil);
            _hasGust = true;
            _windStreaks.SetWind(1, 0f);
            _audio.SetWind(0f);
            foreach (CreatureBody creature in _creatures)
            {
                creature.SetWind(0f);
            }

            _saveText.text = friend.Kind == CharacterKind.Rabbit
                ? "RABBIT HOPPED ON!"
                : "JELLY MADE IT!";
            _saveMessageEndsAt = Time.unscaledTime + 1.1f;
            _saveText.gameObject.SetActive(true);
            _cameraShake = _reducedMotion ? 0f : Mathf.Max(_cameraShake, 0.07f);
            _audio.PlaySave();
            CreateTransientSprite(
                "Friend Joined",
                GeneratedArt.ImpactStars(),
                position + new Vector2(0f, 0.35f),
                1.05f,
                new Vector2(0f, 0.8f),
                0.82f,
                GetCharacterEffectColor(friend.Kind),
                0.8f,
                55f,
                0.8f);
            Physics2D.SyncTransforms();
        }

        private void BeginFinish()
        {
            if (_phase != GamePhase.Playing)
            {
                return;
            }

            _phase = GamePhase.Finishing;
            _runSucceeded = true;
            _finishStartedAt = Time.unscaledTime;
            _pointerActive = false;
            _controlAmount = 0f;
            _wheelJoint.useMotor = false;
            _windStreaks.SetWind(1, 0f);
            _audio.SetWind(0f);
            foreach (CreatureBody creature in _creatures)
            {
                creature.SetWind(0f);
                creature.ShowReliefReaction();
            }

            int best = GetBestBadgeCount();
            if (_collectedBadges > best)
            {
                PlayerPrefs.SetInt(BestBadgeKey(), _collectedBadges);
            }

            _unlockedRouteIndex = Mathf.Max(
                _unlockedRouteIndex,
                Mathf.Min(_route.Index + 1, RouteDefinition.Count - 1));
            SaveSettings();
            _cameraShake = _reducedMotion ? 0f : 0.09f;
            _audio.PlaySave();
            TriggerSaveHaptic();
            SpawnFinishCelebration();
        }

        private void UpdateFinish()
        {
            if (Time.unscaledTime - _finishStartedAt >= FinishCelebrationSeconds)
            {
                ShowResults();
            }
        }

        private void SpawnFinishCelebration()
        {
            Vector2 center = _platformBody.position + new Vector2(0f, 3.8f);
            Color[] colors =
            {
                new Color(1f, 0.83f, 0.22f, 1f),
                new Color(0.36f, 0.88f, 0.98f, 1f),
                new Color(0.94f, 0.48f, 0.72f, 1f)
            };
            for (int index = 0; index < colors.Length; index += 1)
            {
                float side = index - 1f;
                CreateTransientSprite(
                    $"Finish Confetti {index + 1}",
                    GeneratedArt.ImpactStars(),
                    center + new Vector2(side * 0.75f, index * 0.28f),
                    0.86f,
                    new Vector2(side * 0.65f, 1.25f + (index * 0.12f)),
                    1.1f,
                    colors[index],
                    1.15f,
                    side * 90f,
                    0.7f);
            }
        }

        private void UpdateFailure()
        {
            if (_slowMotionEndsAt >= 0f && Time.unscaledTime >= _slowMotionEndsAt)
            {
                Time.timeScale = 1f;
                _slowMotionEndsAt = -1f;
            }

            float elapsed = Time.unscaledTime - _failureStartedAt;
            bool impactHeld = _firstImpactAt >= 0f && Time.unscaledTime - _firstImpactAt >= FailureResultHoldSeconds;
            if (impactHeld || elapsed >= FailureHardTimeoutSeconds)
            {
                ShowResults();
            }
        }

        private void SuspendForInterruption()
        {
            if (_phase == GamePhase.Playing)
            {
                PauseRun();
                return;
            }

            if (_phase != GamePhase.Failing || _failureSuspendedAt >= 0f)
            {
                return;
            }

            _failureSuspendedAt = Time.unscaledTime;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        private void ResumeFailureAfterInterruption()
        {
            if (_phase != GamePhase.Failing || _failureSuspendedAt < 0f)
            {
                return;
            }

            float interruptionSeconds = Time.unscaledTime - _failureSuspendedAt;
            _failureStartedAt += interruptionSeconds;
            if (_firstImpactAt >= 0f)
            {
                _firstImpactAt += interruptionSeconds;
            }

            if (_slowMotionEndsAt >= 0f)
            {
                _slowMotionEndsAt += interruptionSeconds;
            }

            _failureSuspendedAt = -1f;
            bool slowMotionActive = _slowMotionEndsAt >= 0f && Time.unscaledTime < _slowMotionEndsAt;
            Time.timeScale = slowMotionActive ? ImpactSlowMotionScale : 1f;
            AudioListener.pause = false;
        }

        private void UpdateHud()
        {
            _scoreText.text = $"★  {_collectedBadges}/{_route.Badges.Length}";
            if (_saveText.gameObject.activeSelf && Time.unscaledTime >= _saveMessageEndsAt)
            {
                _saveText.gameObject.SetActive(false);
            }

            if (_phase != GamePhase.Playing)
            {
                _hintText.gameObject.SetActive(false);
                _gestureTrack.SetActive(false);
                _gestureCue.gameObject.SetActive(false);
                return;
            }

            if (!_gestureUsed && Mathf.Abs(_controlAmount) >= 0.12f)
            {
                _gestureUsed = true;
                SaveSettings();
            }

            bool showCue = !_gestureUsed && _runSeconds <= 4.8f;
            _hintText.gameObject.SetActive(showCue);
            _gestureTrack.SetActive(showCue);
            _gestureCue.gameObject.SetActive(showCue);
            _gestureCue.anchoredPosition = new Vector2(
                Mathf.Sin(Time.unscaledTime * 2.1f) * 125f,
                0f);
        }

        private void UpdateSetupLabels()
        {
            _motionText.text = _reducedMotion ? "MOTION REDUCED" : "MOTION FULL";
            _routeText.text = _route.Title;
            int best = GetBestBadgeCount();
            _routeSubtitleText.text = $"{_route.Subtitle} · BEST {best}/{_route.Badges.Length}";
        }

        private void SpawnCrown()
        {
            if (_creatures.Count == 0)
            {
                return;
            }

            CreatureBody top = _creatures[_creatures.Count - 1];
            _crownObject = new GameObject("Flying Crown");
            _crownObject.transform.SetParent(_worldRoot, false);
            _crownObject.transform.position = top.transform.position + new Vector3(0.35f, 0.65f, 0f);
            SpriteRenderer renderer = _crownObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GeneratedArt.Crown();
            renderer.material = GeneratedArt.ChromaMaterial;
            renderer.sortingOrder = 70;
            FitHeight(_crownObject.transform, renderer.sprite, 1.15f);
            Rigidbody2D body = _crownObject.AddComponent<Rigidbody2D>();
            body.mass = 0.25f;
            body.gravityScale = 0.75f;
            body.AddForce(new Vector2(1.8f, 4.8f), ForceMode2D.Impulse);
            body.AddTorque(-2.4f, ForceMode2D.Impulse);
        }

        private void SpawnImpact(Vector2 point, CharacterKind kind)
        {
            CreateTransientSprite(
                "Impact Dust",
                GeneratedArt.Dust(),
                point + new Vector2(0f, 0.12f),
                1.22f,
                new Vector2(0f, 0.32f),
                0.62f,
                Color.white,
                0.12f,
                0f,
                1.45f);
            CreateTransientSprite(
                "Impact Dust Left",
                GeneratedArt.Dust(),
                point + new Vector2(-0.18f, 0.12f),
                0.58f,
                new Vector2(-0.48f, 0.48f),
                0.48f,
                new Color(1f, 0.94f, 0.86f, 0.86f),
                0.24f,
                -24f,
                1.65f);
            CreateTransientSprite(
                "Impact Dust Right",
                GeneratedArt.Dust(),
                point + new Vector2(0.2f, 0.1f),
                0.52f,
                new Vector2(0.55f, 0.42f),
                0.46f,
                new Color(1f, 0.94f, 0.86f, 0.82f),
                0.24f,
                28f,
                1.55f);

            Color kindTint = GetCharacterEffectColor(kind);
            CreateTransientSprite(
                "Impact Stars",
                GeneratedArt.ImpactStars(),
                point + new Vector2(0.22f, 0.62f),
                0.78f,
                new Vector2(0.18f, 0.88f),
                0.78f,
                kindTint,
                1.4f,
                92f,
                0.78f);
            CreateTransientSprite(
                "Impact Toy Chips",
                GeneratedArt.ImpactStars(),
                point + new Vector2(-0.22f, 0.38f),
                0.36f,
                new Vector2(-0.5f, 1.08f),
                0.68f,
                kindTint,
                2.15f,
                -145f,
                0.5f);
        }

        private void PreparePlayingCapture()
        {
            _gameplayProbeActive = true;
            _phase = GamePhase.Playing;
            _route = RouteDefinition.Get(0);
            _routeProgress = 24f;
            _travellingWorld.ConfigureRoute(_route, this);
            _creatureCount = 4;
            _collectedBadges = 3;
            ResetVehicle(false);
            _wheelBody.position = new Vector2(24f, WheelCenterY);
            _platformBody.position = new Vector2(24f, PlatformY);
            _wheelBody.transform.position = _wheelBody.position;
            _platformBody.transform.position = _platformBody.position;
            _cameraHome = new Vector3(24.8f, 0f, -10f);
            _camera.transform.position = _cameraHome;
            _travellingWorld.SetRouteView(_cameraHome.x, _routeProgress);
            _windStreaks.SetCenterX(_cameraHome.x);
            BuildStack(false);
            _startOverlay.SetActive(false);
            _pauseOverlay.SetActive(false);
            _resultsOverlay.SetActive(false);
            _hudRoot.SetActive(true);
            _hintText.gameObject.SetActive(false);
            _gestureCue.gameObject.SetActive(false);
            _windStreaks.SetWind(-1, 0.74f);
            _windStreaks.Refresh(0.9f);
            foreach (CreatureBody creature in _creatures)
            {
                creature.SetWind(-0.74f);
            }

            Physics2D.SyncTransforms();
        }

        private void PrepareImpactCapture()
        {
            StartRun();
            _creatureCount = 5;
            BuildStack(true);
            _runSeconds = 18.7f;
            BeginFailure();
            Time.timeScale = 1f;
            Vector2[] positions =
            {
                new Vector2(-3f, -1.5f),
                new Vector2(-1.9f, 0.35f),
                new Vector2(0.1f, 1.85f),
                new Vector2(2.05f, 0.45f),
                new Vector2(3.55f, -1.2f)
            };
            float[] rotations = { -28f, 18f, -12f, 34f, -21f };

            for (int index = 0; index < _creatures.Count; index += 1)
            {
                CreatureBody creature = _creatures[index];
                creature.Body.position = positions[index];
                creature.Body.rotation = rotations[index];
                creature.transform.position = positions[index];
                creature.transform.rotation = Quaternion.Euler(0f, 0f, rotations[index]);
                creature.ShowImpactReaction();
            }

            _windStreaks.SetWind(1, 0.48f);
            _windStreaks.Refresh(0.8f);
            SpawnImpact(new Vector2(-2.8f, -2.7f), CharacterKind.Bird);
            SpawnImpact(new Vector2(2.7f, -2.5f), CharacterKind.Rabbit);
            Physics2D.SyncTransforms();
            UpdateHud();
        }

        private void PrepareResultsCapture()
        {
            StartRun();
            _creatureCount = 5;
            BuildStack(true);
            _runSeconds = 18.7f;
            BeginFailure();
            ShowResults();
            UpdateHud();
        }

        private void PrepareFinishCapture()
        {
            _currentRouteIndex = 2;
            _route = RouteDefinition.Get(_currentRouteIndex);
            _routeProgress = _route.FinishX - 1.4f;
            _travellingWorld.ConfigureRoute(_route, this);
            _gameplayProbeActive = true;
            _phase = GamePhase.Finishing;
            _runSucceeded = true;
            _creatureCount = 5;
            _collectedBadges = _route.Badges.Length;
            ResetVehicle(false);
            float vehicleX = _route.FinishX - 1.4f;
            _wheelBody.position = new Vector2(vehicleX, WheelCenterY);
            _platformBody.position = new Vector2(vehicleX, PlatformY);
            _wheelBody.transform.position = _wheelBody.position;
            _platformBody.transform.position = _platformBody.position;
            _cameraHome = new Vector3(vehicleX + 0.8f, 0f, -10f);
            _camera.transform.position = _cameraHome;
            _travellingWorld.SetRouteView(_cameraHome.x, _routeProgress);
            _windStreaks.SetCenterX(_cameraHome.x);
            BuildStack(false);
            foreach (CreatureBody creature in _creatures)
            {
                creature.ShowReliefReaction();
            }

            _startOverlay.SetActive(false);
            _pauseOverlay.SetActive(false);
            _resultsOverlay.SetActive(false);
            _hudRoot.SetActive(true);
            _hintText.gameObject.SetActive(false);
            _gestureCue.gameObject.SetActive(false);
            SpawnFinishCelebration();
            Physics2D.SyncTransforms();
        }

        private void CreateTransientSprite(string name, Sprite sprite, Vector2 position, float height, Vector2 velocity, float duration)
        {
            CreateTransientSprite(name, sprite, position, height, velocity, duration, Color.white);
        }

        private void CreateTransientSprite(string name, Sprite sprite, Vector2 position, float height, Vector2 velocity, float duration, Color color)
        {
            CreateTransientSprite(name, sprite, position, height, velocity, duration, color, 0f, 0f, 1.35f);
        }

        private void CreateTransientSprite(
            string name,
            Sprite sprite,
            Vector2 position,
            float height,
            Vector2 velocity,
            float duration,
            Color color,
            float gravity,
            float angularVelocity,
            float endScale)
        {
            GameObject effect = new GameObject(name);
            effect.transform.SetParent(_worldRoot, false);
            effect.transform.position = position;
            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.material = GeneratedArt.ChromaMaterial;
            renderer.sortingOrder = 80;
            renderer.color = color;
            FitHeight(effect.transform, sprite, height);
            TransientFx transient = effect.AddComponent<TransientFx>();
            transient.Initialize(duration, velocity, gravity, angularVelocity, endScale);
        }

        private static Color GetCharacterEffectColor(CharacterKind kind)
        {
            switch (kind)
            {
                case CharacterKind.Pear:
                    return new Color(1f, 0.88f, 0.3f, 1f);
                case CharacterKind.Cube:
                    return new Color(0.3f, 0.82f, 1f, 1f);
                case CharacterKind.Bird:
                    return new Color(1f, 0.57f, 0.28f, 1f);
                case CharacterKind.Rabbit:
                    return new Color(0.77f, 0.48f, 1f, 1f);
                default:
                    return new Color(0.42f, 0.94f, 0.95f, 1f);
            }
        }

        private void UpdateCameraRig()
        {
            float targetX = 0f;
            if (_wheelBody != null)
            {
                float lookAhead = Mathf.Clamp(_wheelBody.linearVelocity.x * 0.25f, -1f, 1.65f);
                targetX = _wheelBody.position.x + 0.8f + lookAhead;
            }

            _cameraHome.x = Mathf.SmoothDamp(
                _cameraHome.x,
                targetX,
                ref _cameraFollowVelocity,
                0.18f,
                30f,
                Time.unscaledDeltaTime);
            _travellingWorld.SetRouteView(_cameraHome.x, _routeProgress);
            _windStreaks.SetCenterX(_cameraHome.x);
            if (_cameraShake <= 0f)
            {
                _camera.transform.position = _cameraHome;
                return;
            }

            _cameraShake = Mathf.MoveTowards(_cameraShake, 0f, Time.unscaledDeltaTime * 0.45f);
            float x = Mathf.Sin(Time.unscaledTime * 47f) * _cameraShake;
            float y = Mathf.Cos(Time.unscaledTime * 39f) * _cameraShake * 0.65f;
            _camera.transform.position = _cameraHome + new Vector3(x, y, 0f);
        }

        private int GetBestBadgeCount()
        {
            return PlayerPrefs.GetInt(BestBadgeKey(), 0);
        }

        private string BestBadgeKey()
        {
            return $"wobble.ios.badges.{_route.Index}";
        }

        private static void FitHeight(Transform target, Sprite sprite, float height)
        {
            float scale = height / sprite.bounds.size.y;
            target.localScale = new Vector3(scale, scale, 1f);
        }

        private static void FitWidth(Transform target, Sprite sprite, float width)
        {
            float scale = width / sprite.bounds.size.x;
            target.localScale = new Vector3(scale, scale, 1f);
        }

        private GameObject CreateImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.material = GeneratedArt.ChromaMaterial;
            image.preserveAspect = false;
            return gameObject;
        }

        private GameObject CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Sprite sprite,
            UnityEngine.Events.UnityAction action,
            int fontSize)
        {
            GameObject gameObject = CreateImage(name, parent, sprite, anchor, position, size);
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
            colors.pressedColor = new Color(0.88f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(action);
            Text text = CreateText("Label", gameObject.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);
            return gameObject;
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            Text text = gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(16, fontSize / 2);
            text.resizeTextMaxSize = fontSize;
            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.15f, 0.17f, 0.45f);
            outline.effectDistance = new Vector2(2.5f, -3f);
            return text;
        }

        private static GameObject CreateOverlay(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            Stretch(rect);
            return gameObject;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("Event System");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static bool HasArgument(string expected)
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string FindArgumentValue(string prefix)
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private static void TriggerImpactHaptic()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private static void TriggerSaveHaptic()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private readonly struct CreatureSpec
        {
            private CreatureSpec(CharacterKind kind, Vector2 colliderSize)
            {
                Kind = kind;
                ColliderSize = colliderSize;
            }

            public CharacterKind Kind { get; }

            public Vector2 ColliderSize { get; }

            public static CreatureSpec[] All { get; } =
            {
                new CreatureSpec(CharacterKind.Pear, new Vector2(1.68f, 2.32f)),
                new CreatureSpec(CharacterKind.Cube, new Vector2(1.65f, 1.65f)),
                new CreatureSpec(CharacterKind.Bird, new Vector2(1.42f, 1.65f)),
                new CreatureSpec(CharacterKind.Rabbit, new Vector2(1.58f, 1.78f)),
                new CreatureSpec(CharacterKind.Jelly, new Vector2(1.72f, 1.2f))
            };
        }
    }
}
