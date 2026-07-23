using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WobbleStack.Domain;

namespace WobbleStack.Runtime
{
    internal sealed class WobbleStackGame : MonoBehaviour
    {
        private const float PlatformY = -4.35f;
        private const float PlatformWidth = 8.5f;
        private const float PlatformHeight = 0.78f;
        private const float StaticStackContactInset = 0.06f;
        private const float DynamicStackContactInset = -0.012f;
        private const float CatchFloorY = -8.65f;
        private const float UnityAccelerationScale = 9342.857f;
        private const float ImpactSlowMotionScale = 0.18f;
        private const float ImpactSlowMotionSeconds = 0.36f;
        private const float FailureResultHoldSeconds = 0.9f;
        private const float FailureHardTimeoutSeconds = 2.6f;
        private const string DifficultyPreference = "wobble.ios.difficulty";
        private const string CreatureCountPreference = "wobble.ios.creature-count";
        private const string ReducedMotionPreference = "wobble.ios.reduced-motion";

        private readonly List<CreatureBody> _creatures = new List<CreatureBody>();
        private Camera _camera;
        private Vector3 _cameraHome;
        private Rigidbody2D _platformBody;
        private Transform _worldRoot;
        private WindStreaks _windStreaks;
        private GameAudio _audio;
        private PhysicsMaterial2D _creatureMaterial;
        private Canvas _canvas;
        private GameObject _hudRoot;
        private GameObject _startOverlay;
        private GameObject _pauseOverlay;
        private GameObject _resultsOverlay;
        private Text _scoreText;
        private Text _bestText;
        private Text _difficultyText;
        private Text _countText;
        private Text _motionText;
        private Text _resultTimeText;
        private Text _resultBestText;
        private Text _saveText;
        private Text _hintText;
        private GamePhase _phase = GamePhase.Ready;
        private DifficultyId _difficulty = DifficultyId.Normal;
        private int _creatureCount = 5;
        private bool _reducedMotion;
        private int _runCount;
        private float _runSeconds;
        private float _targetAngleRadians;
        private bool _pointerActive;
        private float _controlAmount;
        private GustScheduler _gustScheduler;
        private GustSample _gust;
        private bool _hasGust;
        private int _gustIndex;
        private float _failureStartedAt;
        private float _firstImpactAt = -1f;
        private float _slowMotionEndsAt = -1f;
        private float _failureSuspendedAt = -1f;
        private bool _dangerWasHigh;
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

        private void Start()
        {
            string capturePath = FindArgumentValue("--wobble-capture=");
            if (string.IsNullOrEmpty(capturePath))
            {
                return;
            }

            if (HasArgument("--wobble-capture-results"))
            {
                PrepareResultsCapture();
            }
            else if (HasArgument("--wobble-capture-impact"))
            {
                PrepareImpactCapture();
            }
            else if (HasArgument("--wobble-capture-playing"))
            {
                PreparePlayingCapture();
            }

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

            UpdateCameraShake();
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
            UpdateControlTarget();

            float currentDegrees = _platformBody.rotation;
            float targetDegrees = _targetAngleRadians * Mathf.Rad2Deg;
            float nextDegrees = Mathf.MoveTowardsAngle(currentDegrees, targetDegrees, 150f * Time.fixedDeltaTime);
            _platformBody.MoveRotation(nextDegrees);

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
                    Vector2 horizontalForce = new Vector2(acceleration * exposure * body.mass, 0f);
                    body.AddForceAtPosition(horizontalForce, body.worldCenterOfMass + new Vector2(0f, 0.18f));
                    body.AddForce(new Vector2(0f, liftAcceleration * body.mass));
                }
            }

            CheckDangerAndFailure();
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

            GeneratedArt.Release();
        }

        public void RegisterImpact(CreatureBody creature, Vector2 point)
        {
            if (_phase == GamePhase.Playing)
            {
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

        internal void ConfigureGameplayProbe(
            float force,
            int direction,
            float controlAmount,
            int creatureCount,
            float durationSeconds = 5.4f)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            _creatureCount = WobbleStackRules.ClampCreatureCount(creatureCount);
            _phase = GamePhase.Playing;
            _runSeconds = 0f;
            _targetAngleRadians = 0f;
            _controlAmount = Mathf.Clamp(controlAmount, -1f, 1f);
            _pointerActive = true;
            _gustScheduler = new GustScheduler(1u, _difficulty);
            _gust = new GustSample(0.7f, durationSeconds, force, direction, 0.7f);
            _hasGust = true;
            _gustIndex = 0;
            _platformBody.rotation = 0f;
            _platformBody.transform.rotation = Quaternion.identity;
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

        internal float GetGameplayProbeMaxDrift()
        {
            float maxDrift = 0f;
            foreach (CreatureBody creature in _creatures)
            {
                maxDrift = Mathf.Max(maxDrift, Mathf.Abs(creature.Body.position.x));
            }

            return maxDrift;
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
                friction = 0.18f,
                bounciness = 0.035f
            };
        }

        private void LoadSettings()
        {
            _difficulty = DifficultyFromStored(PlayerPrefs.GetInt(DifficultyPreference, 1));
            _creatureCount = WobbleStackRules.ClampCreatureCount(PlayerPrefs.GetInt(CreatureCountPreference, 5));
            _reducedMotion = PlayerPrefs.GetInt(ReducedMotionPreference, 0) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(DifficultyPreference, DifficultyToStored(_difficulty));
            PlayerPrefs.SetInt(CreatureCountPreference, _creatureCount);
            PlayerPrefs.SetInt(ReducedMotionPreference, _reducedMotion ? 1 : 0);
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
            GameObject background = new GameObject("Sunset Stage");
            background.transform.SetParent(_worldRoot, false);
            SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = GeneratedArt.Background();
            renderer.sortingOrder = -100;
            float targetHeight = 20.4f;
            float scale = targetHeight / renderer.sprite.bounds.size.y;
            background.transform.localScale = new Vector3(scale, scale, 1f);
            background.transform.position = new Vector3(0f, 0f, 2f);
        }

        private void BuildStage()
        {
            GameObject fulcrum = new GameObject("Fulcrum");
            fulcrum.transform.SetParent(_worldRoot, false);
            SpriteRenderer fulcrumRenderer = fulcrum.AddComponent<SpriteRenderer>();
            fulcrumRenderer.sprite = GeneratedArt.Fulcrum();
            fulcrumRenderer.material = GeneratedArt.ChromaMaterial;
            fulcrumRenderer.sortingOrder = 10;
            FitHeight(fulcrum.transform, fulcrumRenderer.sprite, 1.65f);
            fulcrum.transform.position = new Vector3(0f, PlatformY - 0.82f, 0f);

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
            _platformBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            CapsuleCollider2D collider = platform.AddComponent<CapsuleCollider2D>();
            float platformScale = platform.transform.localScale.x;
            collider.size = new Vector2(PlatformWidth / platformScale, PlatformHeight / platformScale);
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.sharedMaterial = _creatureMaterial;

            GameObject floor = new GameObject("Catch Floor");
            floor.transform.SetParent(_worldRoot, false);
            floor.transform.position = new Vector3(0f, CatchFloorY, 0f);
            BoxCollider2D floorCollider = floor.AddComponent<BoxCollider2D>();
            floorCollider.size = new Vector2(18f, 0.5f);
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

            GameObject scorePlate = CreateImage("Score Plate", hud, GeneratedArt.CocoaPlate(), new Vector2(0f, 1f), new Vector2(190f, -100f), new Vector2(320f, 135f));
            _scoreText = CreateText("Score", scorePlate.transform, "★  0.0", 50, TextAnchor.MiddleCenter, Color.white);
            Stretch(_scoreText.rectTransform);

            GameObject bestObject = new GameObject("Best Score");
            bestObject.transform.SetParent(hud, false);
            RectTransform bestRect = bestObject.AddComponent<RectTransform>();
            bestRect.anchorMin = new Vector2(0f, 1f);
            bestRect.anchorMax = new Vector2(0f, 1f);
            bestRect.pivot = new Vector2(0.5f, 0.5f);
            bestRect.anchoredPosition = new Vector2(190f, -182f);
            bestRect.sizeDelta = new Vector2(320f, 55f);
            _bestText = CreateText("Best", bestObject.transform, "BEST 0.0", 28, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.82f, 0.9f));
            Stretch(_bestText.rectTransform);

            CreateButton("Pause", hud, "Ⅱ", new Vector2(1f, 1f), new Vector2(-104f, -105f), new Vector2(132f, 132f), GeneratedArt.CocoaPlate(), PauseRun, 55);

            _saveText = CreateText("Save Feedback", hud, "NICE SAVE!", 54, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.58f, 1f));
            RectTransform saveRect = _saveText.rectTransform;
            saveRect.anchorMin = new Vector2(0.5f, 0.68f);
            saveRect.anchorMax = new Vector2(0.5f, 0.68f);
            saveRect.sizeDelta = new Vector2(700f, 100f);
            saveRect.anchoredPosition = Vector2.zero;
            _saveText.gameObject.SetActive(false);

            _hintText = CreateText("First Run Hint", hud, "DRAG AGAINST THE WIND", 35, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.8f, 0.96f));
            RectTransform hintRect = _hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0.12f);
            hintRect.anchorMax = new Vector2(0.5f, 0.12f);
            hintRect.anchoredPosition = Vector2.zero;
            hintRect.sizeDelta = new Vector2(760f, 90f);
            _hintText.gameObject.SetActive(false);
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

            Text subtitle = CreateText("Subtitle", _startOverlay.transform, "KEEP THE LITTLE ONES TOGETHER", 31, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.84f, 0.95f));
            RectTransform subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0.5f, 1f);
            subtitleRect.anchorMax = new Vector2(0.5f, 1f);
            subtitleRect.anchoredPosition = new Vector2(0f, -460f);
            subtitleRect.sizeDelta = new Vector2(850f, 80f);

            CreateButton("Play", _startOverlay.transform, "PLAY", new Vector2(0.5f, 0f), new Vector2(0f, 340f), new Vector2(560f, 230f), GeneratedArt.CoralPlate(), StartRun, 72);
            _difficultyText = CreateButton("Difficulty", _startOverlay.transform, "NORMAL", new Vector2(0.31f, 0f), new Vector2(0f, 170f), new Vector2(360f, 125f), GeneratedArt.CocoaPlate(), CycleDifficulty, 34).GetComponentInChildren<Text>();
            _countText = CreateButton("Creature Count", _startOverlay.transform, "5 FRIENDS", new Vector2(0.69f, 0f), new Vector2(0f, 170f), new Vector2(360f, 125f), GeneratedArt.CocoaPlate(), CycleCreatureCount, 34).GetComponentInChildren<Text>();
            _motionText = CreateButton("Motion", _startOverlay.transform, "MOTION FULL", new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(380f, 95f), GeneratedArt.CocoaPlate(), ToggleReducedMotion, 27).GetComponentInChildren<Text>();
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
            CreateButton("Resume", _pauseOverlay.transform, "KEEP WOBBLING", new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(620f, 210f), GeneratedArt.CoralPlate(), ResumeRun, 48);
            _pauseOverlay.SetActive(false);
        }

        private void BuildResultsOverlay(Transform parent)
        {
            _resultsOverlay = CreateOverlay("Results Overlay", parent);
            Image veil = _resultsOverlay.AddComponent<Image>();
            veil.color = new Color(0.22f, 0.1f, 0.15f, 0.22f);
            Text title = CreateText("Result Title", _resultsOverlay.transform, "EVERYONE\nPANICKED", 76, TextAnchor.MiddleCenter, Color.white);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.76f);
            titleRect.anchorMax = new Vector2(0.5f, 0.76f);
            titleRect.sizeDelta = new Vector2(820f, 230f);
            _resultTimeText = CreateText("Result Time", _resultsOverlay.transform, "0.0 SECONDS", 54, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.78f, 1f));
            RectTransform timeRect = _resultTimeText.rectTransform;
            timeRect.anchorMin = new Vector2(0.5f, 0.64f);
            timeRect.anchorMax = new Vector2(0.5f, 0.64f);
            timeRect.sizeDelta = new Vector2(700f, 100f);
            _resultBestText = CreateText("Result Best", _resultsOverlay.transform, "BEST 0.0", 30, TextAnchor.MiddleCenter, Color.white);
            RectTransform resultBestRect = _resultBestText.rectTransform;
            resultBestRect.anchorMin = new Vector2(0.5f, 0.595f);
            resultBestRect.anchorMax = new Vector2(0.5f, 0.595f);
            resultBestRect.sizeDelta = new Vector2(650f, 70f);
            CreateButton("Retry", _resultsOverlay.transform, "RETRY", new Vector2(0.5f, 0f), new Vector2(0f, 270f), new Vector2(570f, 225f), GeneratedArt.CoralPlate(), StartRun, 68);
            CreateButton("Change Setup", _resultsOverlay.transform, "CHANGE SETUP", new Vector2(0.5f, 0f), new Vector2(0f, 105f), new Vector2(450f, 120f), GeneratedArt.CocoaPlate(), ShowReady, 31);
            _resultsOverlay.SetActive(false);
        }

        private void BuildStack(bool dynamicBodies)
        {
            ClearStack();
            CreatureSpec[] specs = CreatureSpec.All;
            float bottom = PlatformY + (PlatformHeight * 0.5f);
            float contactInset = dynamicBodies ? DynamicStackContactInset : StaticStackContactInset;

            for (int index = 0; index < _creatureCount; index += 1)
            {
                CreatureSpec spec = specs[index];
                float y = bottom + (spec.ColliderSize.y * 0.5f);
                bottom += spec.ColliderSize.y - contactInset;
                CreatureBody creature = CreateCreature(spec, index, new Vector2(0f, y), dynamicBodies);
                _creatures.Add(creature);
            }

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

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(bodyObject.transform, false);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GeneratedArt.CalmCharacter(spec.Kind);
            renderer.material = GeneratedArt.ChromaMaterial;
            renderer.sortingOrder = 30 + index;
            FitHeight(visualObject.transform, renderer.sprite, spec.VisualHeight);
            visualObject.transform.localPosition = new Vector3(0f, spec.VisualOffsetY, 0f);
            CreatureBody creature = bodyObject.AddComponent<CreatureBody>();
            creature.Initialize(
                this,
                spec.Kind,
                visualObject.transform,
                index * 1.37f,
                GeneratedArt.Character(spec.Kind),
                GeneratedArt.ImpactCharacter(spec.Kind));
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
            _runCount += 1;
            _runSeconds = 0f;
            _targetAngleRadians = 0f;
            _pointerActive = false;
            _controlAmount = 0f;
            _gustIndex = 0;
            _firstImpactAt = -1f;
            _slowMotionEndsAt = -1f;
            _failureSuspendedAt = -1f;
            _dangerWasHigh = false;
            _cameraShake = 0f;
            _platformBody.rotation = 0f;
            _gustScheduler = new GustScheduler(Convert.ToUInt32(7907 + (_runCount * 101)), _difficulty);
            _gust = _gustScheduler.Next(0f);
            _hasGust = true;
            BuildStack(true);
            _phase = GamePhase.Playing;
            _startOverlay.SetActive(false);
            _pauseOverlay.SetActive(false);
            _resultsOverlay.SetActive(false);
            _hudRoot.SetActive(true);
            _saveText.gameObject.SetActive(false);
            _hintText.gameObject.SetActive(true);
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
            _targetAngleRadians = 0f;
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
            _failureStartedAt = Time.unscaledTime;
            _firstImpactAt = -1f;
            _slowMotionEndsAt = -1f;
            _failureSuspendedAt = -1f;
            _pointerActive = false;
            _controlAmount = 0f;
            _targetAngleRadians = _platformBody.rotation * Mathf.Deg2Rad;
            _windStreaks.SetWind(1, 0f);
            _audio.SetWind(0f);
            SpawnCrown();
        }

        private void ShowResults()
        {
            if (_phase != GamePhase.Failing)
            {
                return;
            }

            Time.timeScale = 1f;
            _phase = GamePhase.Results;
            float best = GetBestScore();
            if (_runSeconds > best)
            {
                best = _runSeconds;
                PlayerPrefs.SetFloat(BestScoreKey(), best);
                PlayerPrefs.Save();
            }

            foreach (CreatureBody creature in _creatures)
            {
                creature.Body.simulated = false;
                creature.gameObject.SetActive(false);
            }

            CreatureBody mascot = _creatures[_creatures.Count - 1];
            mascot.gameObject.SetActive(true);
            mascot.Body.position = new Vector2(0f, -0.25f);
            mascot.Body.rotation = -7f;
            mascot.transform.position = mascot.Body.position;
            mascot.transform.rotation = Quaternion.Euler(0f, 0f, mascot.Body.rotation);
            mascot.ShowImpactReaction();
            if (_crownObject != null)
            {
                _crownObject.SetActive(false);
            }

            _resultTimeText.text = $"{_runSeconds:0.0} SECONDS";
            _resultBestText.text = $"BEST {best:0.0}";
            _hudRoot.SetActive(false);
            _resultsOverlay.SetActive(true);
        }

        private void ShowReady()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            _phase = GamePhase.Ready;
            _runSeconds = 0f;
            _targetAngleRadians = 0f;
            _controlAmount = 0f;
            _platformBody.rotation = 0f;
            _hasGust = false;
            _windStreaks.SetWind(1, 0f);
            _audio.SetWind(0f);
            BuildStack(false);
            _startOverlay.SetActive(true);
            _pauseOverlay.SetActive(false);
            _resultsOverlay.SetActive(false);
            _hudRoot.SetActive(false);
            UpdateSetupLabels();
        }

        private void CycleDifficulty()
        {
            if (_phase != GamePhase.Ready)
            {
                return;
            }

            switch (_difficulty)
            {
                case DifficultyId.Gentle:
                    _difficulty = DifficultyId.Normal;
                    break;
                case DifficultyId.Normal:
                    _difficulty = DifficultyId.Wild;
                    break;
                default:
                    _difficulty = DifficultyId.Gentle;
                    break;
            }

            SaveSettings();
            UpdateSetupLabels();
            _audio.PlayClick();
        }

        private void CycleCreatureCount()
        {
            if (_phase != GamePhase.Ready)
            {
                return;
            }

            _creatureCount = _creatureCount >= WobbleStackRules.MaxCreatureCount
                ? WobbleStackRules.MinCreatureCount
                : _creatureCount + 1;
            SaveSettings();
            BuildStack(false);
            UpdateSetupLabels();
            _audio.PlayClick();
        }

        private void ToggleReducedMotion()
        {
            if (_phase != GamePhase.Ready)
            {
                return;
            }

            _reducedMotion = !_reducedMotion;
            SaveSettings();
            UpdateSetupLabels();
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

                    _pointerActive = true;
                    SetPointerTarget(touch.position.x);
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
                    _pointerActive = true;
                    SetPointerTarget(Input.mousePosition.x);
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
            float normalized = Mathf.Clamp01(screenX / Screen.width);
            _controlAmount = WobbleStackRules.GetControlAmount(normalized);
        }

        private void UpdateControlTarget()
        {
            float inputMagnitude = Mathf.Abs(_controlAmount);
            if (inputMagnitude <= 0f || !_hasGust)
            {
                _targetAngleRadians = 0f;
                return;
            }

            float requiredAngle = WobbleStackRules.GetRequiredCounterAngle(
                _gust.Force,
                WobbleStackRules.GravityScale);
            float authority = Mathf.Max(requiredAngle * 0.72f, 0.06f);
            float secondsUntilGust = _gust.StartsAtSeconds - _runSeconds;
            if (secondsUntilGust > 0f && secondsUntilGust <= WobbleStackRules.WindPreviewSeconds)
            {
                float preview = WobbleStackRules.GetWindPreviewEnvelope(secondsUntilGust);
                authority = Mathf.Max(
                    requiredAngle * Mathf.Lerp(0.72f, 0.88f, preview),
                    0.06f);
            }
            else if (IsGustActive())
            {
                float progress = (_runSeconds - _gust.StartsAtSeconds) / _gust.DurationSeconds;
                float envelope = WobbleStackRules.GetGustEnvelope(progress);
                authority = Mathf.Max(
                    requiredAngle * Mathf.Lerp(0.88f, 1.08f, envelope),
                    0.06f);
            }

            float shapedMagnitude = Mathf.Min(1f, Mathf.Sqrt(inputMagnitude) * 1.35f);
            float direction = _controlAmount < 0f ? -1f : 1f;
            _targetAngleRadians = direction *
                Mathf.Min(authority, WobbleStackRules.MaxPlatformAngle) *
                shapedMagnitude;
        }

        private void UpdateGust()
        {
            if (!_hasGust)
            {
                _gust = _gustScheduler.Next(_runSeconds);
                _hasGust = true;
            }

            if (_runSeconds >= _gust.EndsAtSeconds)
            {
                _gust = _gustScheduler.Next(_gust.EndsAtSeconds);
                _gustIndex += 1;
            }

            if (!IsGustActive())
            {
                float preview = WobbleStackRules.GetWindPreviewEnvelope(_gust.StartsAtSeconds - _runSeconds);
                float previewIntensity = preview <= 0f ? 0f : Mathf.Lerp(0.08f, 0.34f, preview);
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
            DifficultyProfile wild = WobbleStackRules.GetDifficultyProfile(DifficultyId.Wild);
            float forceRatio = Mathf.Clamp01(_gust.Force / wild.ForceMax);
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
                maxDrift = Mathf.Max(maxDrift, Mathf.Abs(creature.Body.position.x));
                if (creature.Body.position.y < -7.1f || Mathf.Abs(creature.Body.position.x) > 6.8f)
                {
                    BeginFailure();
                    return;
                }

                if (index > 0 && creature.Body.position.y - _creatures[index - 1].Body.position.y < 0.3f)
                {
                    BeginFailure();
                    return;
                }
            }

            float danger = Mathf.Max(maxDrift / 2.2f, Mathf.Abs(_platformBody.rotation) / (WobbleStackRules.MaxPlatformAngle * Mathf.Rad2Deg));
            if (danger > 0.72f)
            {
                _dangerWasHigh = true;
            }
            else if (_dangerWasHigh && danger < 0.34f && IsGustActive())
            {
                _dangerWasHigh = false;
                _saveMessageEndsAt = Time.unscaledTime + 0.7f;
                _saveText.gameObject.SetActive(true);
                _audio.PlaySave();
                TriggerSaveHaptic();
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
            _scoreText.text = $"★  {_runSeconds:0.0}";
            _bestText.text = $"BEST {GetBestScore():0.0}";
            if (_saveText.gameObject.activeSelf && Time.unscaledTime >= _saveMessageEndsAt)
            {
                _saveText.gameObject.SetActive(false);
            }

            if (_phase != GamePhase.Playing || _gustIndex > 0)
            {
                _hintText.gameObject.SetActive(false);
                return;
            }

            _hintText.gameObject.SetActive(true);
            float secondsUntilGust = _gust.StartsAtSeconds - _runSeconds;
            if (secondsUntilGust > WobbleStackRules.WindPreviewSeconds)
            {
                _hintText.text = "DRAG LEFT / RIGHT · THE TOUCHED END RISES";
            }
            else
            {
                _hintText.text = _gust.Direction > 0
                    ? "WIND → · RAISE THE RIGHT END"
                    : "WIND ← · RAISE THE LEFT END";
            }
        }

        private void UpdateSetupLabels()
        {
            _difficultyText.text = WobbleStackRules.GetDifficultyProfile(_difficulty).Label.ToUpperInvariant();
            _countText.text = $"{_creatureCount} FRIENDS";
            _motionText.text = _reducedMotion ? "MOTION REDUCED" : "MOTION FULL";
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
            CreateTransientSprite("Impact Dust", GeneratedArt.Dust(), point + new Vector2(0f, 0.15f), 1.2f, new Vector2(0f, 0.25f), 0.55f);
            Color kindTint = kind == CharacterKind.Bird ? new Color(1f, 0.8f, 0.65f, 1f) : Color.white;
            CreateTransientSprite("Impact Stars", GeneratedArt.ImpactStars(), point + new Vector2(0.25f, 0.65f), 0.78f, new Vector2(0.15f, 0.55f), 0.7f, kindTint);
        }

        private void PreparePlayingCapture()
        {
            StartRun();
            _runSeconds = 12.4f;
            _targetAngleRadians = -0.12f;
            _platformBody.rotation = _targetAngleRadians * Mathf.Rad2Deg;
            _platformBody.transform.rotation = Quaternion.Euler(0f, 0f, _platformBody.rotation);
            _windStreaks.SetWind(1, 0.78f);
            _windStreaks.Refresh(1.35f);

            for (int index = 0; index < _creatures.Count; index += 1)
            {
                CreatureBody creature = _creatures[index];
                Vector2 position = creature.Body.position + new Vector2(index * -0.08f, 0f);
                creature.Body.position = position;
                creature.Body.rotation = index * -1.2f;
                creature.transform.position = position;
                creature.transform.rotation = Quaternion.Euler(0f, 0f, creature.Body.rotation);
                creature.SetWind(0.78f);
            }

            Physics2D.SyncTransforms();
            UpdateHud();
        }

        private void PrepareImpactCapture()
        {
            StartRun();
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
            _runSeconds = 18.7f;
            BeginFailure();
            ShowResults();
            UpdateHud();
        }

        private void CreateTransientSprite(string name, Sprite sprite, Vector2 position, float height, Vector2 velocity, float duration)
        {
            CreateTransientSprite(name, sprite, position, height, velocity, duration, Color.white);
        }

        private void CreateTransientSprite(string name, Sprite sprite, Vector2 position, float height, Vector2 velocity, float duration, Color color)
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
            transient.Initialize(duration, velocity);
        }

        private void UpdateCameraShake()
        {
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

        private float GetBestScore()
        {
            return PlayerPrefs.GetFloat(BestScoreKey(), 0f);
        }

        private string BestScoreKey()
        {
            return $"wobble.ios.best.{DifficultyToStored(_difficulty)}.{_creatureCount}";
        }

        private static DifficultyId DifficultyFromStored(int value)
        {
            if (value == 0)
            {
                return DifficultyId.Gentle;
            }

            return value == 2 ? DifficultyId.Wild : DifficultyId.Normal;
        }

        private static int DifficultyToStored(DifficultyId difficulty)
        {
            switch (difficulty)
            {
                case DifficultyId.Gentle:
                    return 0;
                case DifficultyId.Wild:
                    return 2;
                default:
                    return 1;
            }
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
            private CreatureSpec(CharacterKind kind, Vector2 colliderSize, float visualHeight, float visualOffsetY)
            {
                Kind = kind;
                ColliderSize = colliderSize;
                VisualHeight = visualHeight;
                VisualOffsetY = visualOffsetY;
            }

            public CharacterKind Kind { get; }

            public Vector2 ColliderSize { get; }

            public float VisualHeight { get; }

            public float VisualOffsetY { get; }

            public static CreatureSpec[] All { get; } =
            {
                new CreatureSpec(CharacterKind.Pear, new Vector2(1.72f, 2.4f), 2.95f, 0f),
                new CreatureSpec(CharacterKind.Cube, new Vector2(1.78f, 1.76f), 2.35f, 0f),
                new CreatureSpec(CharacterKind.Bird, new Vector2(1.55f, 1.85f), 2.35f, 0f),
                new CreatureSpec(CharacterKind.Rabbit, new Vector2(1.7f, 2.64f), 3.15f, 0f),
                new CreatureSpec(CharacterKind.Jelly, new Vector2(1.66f, 1.64f), 2f, 0f)
            };
        }
    }
}
