using System.Collections.Generic;
using UnityEngine;

namespace WobbleStack.Runtime
{
    internal enum CreatureEmotion
    {
        Calm,
        Alert,
        Effort,
        Panic,
        Relief,
        Impact
    }

    [RequireComponent(typeof(Rigidbody2D))]
    internal sealed class CreatureRig : MonoBehaviour
    {
        private const float MaximumFrameStep = 0.05f;
        private readonly List<SecondaryPart> _secondaryParts = new List<SecondaryPart>();
        private readonly List<SpriteRenderer> _articulatedRenderers =
            new List<SpriteRenderer>();
        private readonly List<bool> _impactRendererStates = new List<bool>();
        private Rigidbody2D _body;
        private Transform _visualRoot;
        private Transform _bodyVisual;
        private Transform _leftEye;
        private Transform _rightEye;
        private Transform _leftPupil;
        private Transform _rightPupil;
        private Transform _leftBrow;
        private Transform _rightBrow;
        private Transform _leftClosedEye;
        private Transform _rightClosedEye;
        private Transform _mouth;
        private Transform _leftBlush;
        private Transform _rightBlush;
        private SpriteRenderer _leftBrowRenderer;
        private SpriteRenderer _rightBrowRenderer;
        private SpriteRenderer _mouthRenderer;
        private SpriteRenderer _leftClosedEyeRenderer;
        private SpriteRenderer _rightClosedEyeRenderer;
        private SpriteRenderer _impactPoseRenderer;
        private Transform _impactPose;
        private SecondaryPart _leftArm;
        private SecondaryPart _rightArm;
        private Vector3 _leftEyeBaseScale;
        private Vector3 _rightEyeBaseScale;
        private Vector3 _leftPupilBaseScale;
        private Vector3 _rightPupilBaseScale;
        private Vector3 _leftBrowBaseScale;
        private Vector3 _rightBrowBaseScale;
        private Vector3 _mouthBaseScale;
        private Vector2 _leftEyeRest;
        private Vector2 _rightEyeRest;
        private Vector2 _gaze;
        private Vector2 _gazeVelocity;
        private Vector2 _gazeTarget;
        private Vector2 _previousVelocity;
        private CharacterKind _kind;
        private CreatureEmotion _emotion;
        private float _idlePhase;
        private float _signedWind;
        private float _windIntensity;
        private float _previousWindIntensity;
        private float _reliefUntil;
        private float _nextBlinkAt;
        private float _blinkStartedAt = -1f;
        private float _blinkDuration = 0.13f;
        private float _nextGazeAt;
        private float _impactKick;
        private float _impactPoseEndsAt = -1f;
        private float _fallBlend;
        private float _mouthHeight;
        private float _emotionEyeScale = 1f;
        private bool _falling;
        private uint _randomState;
        private Transform _gripTarget;
        private Vector2 _gripLocalAnchor;
        private SecondaryPart _gripArm;

        public CreatureEmotion Emotion => _emotion;

        public int SecondaryPartCount => _secondaryParts.Count;

        public float BlinkAmount { get; private set; }

        public void Initialize(CharacterKind kind, int sortingOrder, float idlePhase)
        {
            _kind = kind;
            _idlePhase = idlePhase;
            _body = GetComponent<Rigidbody2D>();
            _randomState = 0x9E3779B9u ^ (ConvertKindToSeed(kind) * 0x85EBCA6Bu);
            _visualRoot = new GameObject("Articulated Rig").transform;
            _visualRoot.SetParent(transform, false);

            BuildCharacter(sortingOrder);
            BuildFace(sortingOrder + 6);
            BuildImpactPose(sortingOrder + 18);
            _previousVelocity = _body.linearVelocity;
            _nextBlinkAt = Time.time + Mathf.Lerp(1.2f, 3.1f, NextRandom01());
            _nextGazeAt = Time.time + Mathf.Lerp(0.3f, 1.2f, NextRandom01());
            _emotion = CreatureEmotion.Impact;
            SetEmotion(CreatureEmotion.Calm);
        }

        public void ResetReaction()
        {
            _signedWind = 0f;
            _windIntensity = 0f;
            _previousWindIntensity = 0f;
            _reliefUntil = 0f;
            _impactKick = 0f;
            _impactPoseEndsAt = -1f;
            RestoreArticulatedRenderers();
            _impactPoseRenderer.enabled = false;
            _fallBlend = 0f;
            _falling = false;
            _blinkStartedAt = -1f;
            BlinkAmount = 0f;
            ClearGripTarget();
            SetEmotion(CreatureEmotion.Calm);
        }

        public void SetWind(float signedIntensity)
        {
            if (_emotion == CreatureEmotion.Impact)
            {
                return;
            }

            _signedWind = signedIntensity;
            _previousWindIntensity = _windIntensity;
            _windIntensity = Mathf.Abs(signedIntensity);
            if (_previousWindIntensity >= 0.28f && _windIntensity <= 0.04f)
            {
                _reliefUntil = Time.time + GetReliefDuration();
            }

            if (Time.time < _reliefUntil)
            {
                SetEmotion(CreatureEmotion.Relief);
                return;
            }

            if (_windIntensity <= 0.04f)
            {
                SetEmotion(CreatureEmotion.Calm);
                return;
            }

            GetEmotionThresholds(out float effortThreshold, out float panicThreshold);
            if (_windIntensity < effortThreshold)
            {
                SetEmotion(CreatureEmotion.Alert);
            }
            else if (_windIntensity < panicThreshold)
            {
                SetEmotion(CreatureEmotion.Effort);
            }
            else
            {
                SetEmotion(CreatureEmotion.Panic);
            }
        }

        public void ShowImpactReaction()
        {
            _impactKick = 1f;
            _impactPoseEndsAt = Time.unscaledTime + 0.72f;
            HideArticulatedRenderers();
            _impactPoseRenderer.enabled = true;
            _impactPoseRenderer.color = Color.white;
            _falling = false;
            ClearGripTarget();
            SetEmotion(CreatureEmotion.Impact);
        }

        public void ShowFallReaction()
        {
            if (_emotion == CreatureEmotion.Impact)
            {
                return;
            }

            _falling = true;
            _impactKick = Mathf.Max(_impactKick, 0.24f);
            ClearGripTarget();
            SetEmotion(CreatureEmotion.Panic);
        }

        public void ShowReliefReaction()
        {
            if (_emotion == CreatureEmotion.Impact)
            {
                return;
            }

            _falling = false;
            _reliefUntil = Time.time + GetReliefDuration();
            ClearGripTarget();
            SetEmotion(CreatureEmotion.Relief);
        }

        public void SetGripTarget(Transform target, Vector2 localAnchor, bool useLeftArm)
        {
            _gripTarget = target;
            _gripLocalAnchor = localAnchor;
            _gripArm = useLeftArm ? _leftArm : _rightArm;
        }

        public void ClearGripTarget()
        {
            _gripTarget = null;
            _gripArm = null;
        }

        internal float GetSecondaryMotionProbe()
        {
            if (_secondaryParts.Count == 0)
            {
                return 0f;
            }

            return Mathf.DeltaAngle(
                _secondaryParts[0].RestRotation,
                _secondaryParts[0].Transform.localEulerAngles.z);
        }

        internal string GetMouthSpriteName()
        {
            return _mouthRenderer.sprite.name;
        }

        internal string GetImpactPoseSpriteNameProbe()
        {
            return _impactPoseRenderer.sprite.name;
        }

        internal bool IsImpactPoseVisibleProbe()
        {
            return _impactPoseRenderer.enabled;
        }

        internal float GetNextBlinkAtProbe()
        {
            return _nextBlinkAt;
        }

        private void LateUpdate()
        {
            if (_visualRoot == null)
            {
                return;
            }

            float deltaTime = Mathf.Min(Time.deltaTime, MaximumFrameStep);
            if (deltaTime <= 0f)
            {
                return;
            }

            UpdateBlink();
            UpdateGaze(deltaTime);
            UpdateSecondaryMotion(deltaTime);
            UpdateBodyLife(deltaTime);
            UpdateImpactPose();
        }

        private void BuildCharacter(int sortingOrder)
        {
            switch (_kind)
            {
                case CharacterKind.Pear:
                    BuildPear(sortingOrder);
                    return;
                case CharacterKind.Cube:
                    BuildCube(sortingOrder);
                    return;
                case CharacterKind.Bird:
                    BuildBird(sortingOrder);
                    return;
                case CharacterKind.Rabbit:
                    BuildRabbit(sortingOrder);
                    return;
                default:
                    BuildJelly(sortingOrder);
                    return;
            }
        }

        private void BuildPear(int order)
        {
            _bodyVisual = CreateSprite(
                "Pear Body",
                GeneratedArt.RigPart(_kind, CreatureRigPart.Body),
                Vector2.zero,
                2.35f,
                0f,
                order);
            _leftArm = CreateSecondary(
                "Pear Left Arm",
                CreatureRigPart.LeftArm,
                new Vector2(-0.72f, 0.26f),
                0.58f,
                120f,
                18f,
                0.22f,
                2.5f,
                0.16f,
                140f,
                order + 3);
            _rightArm = CreateSecondary(
                "Pear Right Arm",
                CreatureRigPart.RightArm,
                new Vector2(0.72f, 0.26f),
                0.58f,
                -120f,
                18f,
                0.22f,
                2.5f,
                0.16f,
                40f,
                order + 3);
            CreateSecondary("Pear Left Foot", CreatureRigPart.LeftFoot, new Vector2(-0.47f, -1.08f), 0.31f, 0f, 4f, 0.04f, 1.4f, 0.1f, -90f, order + 2);
            CreateSecondary("Pear Right Foot", CreatureRigPart.RightFoot, new Vector2(0.47f, -1.08f), 0.31f, 0f, 4f, 0.04f, 1.4f, 0.1f, -90f, order + 2);
            CreateSecondary("Pear Leaf 1", CreatureRigPart.Accent1, new Vector2(0.4f, 0.93f), 0.55f, 14f, 38f, 0.35f, 3f, 0.2f, 90f, order - 2);
            CreateSecondary("Pear Leaf 2", CreatureRigPart.Accent2, new Vector2(0.55f, 0.91f), 0.58f, 33f, 34f, 0.31f, 2.4f, 0.22f, 90f, order - 2);
            CreateSecondary("Pear Leaf 3", CreatureRigPart.Accent3, new Vector2(0.7f, 0.88f), 0.58f, 50f, 34f, 0.29f, 2.6f, 0.22f, 90f, order - 2);
            CreateSecondary("Pear Leaf 4", CreatureRigPart.Accent4, new Vector2(0.83f, 0.84f), 0.53f, 66f, 40f, 0.38f, 3.2f, 0.2f, 90f, order - 2);
            ConfigureFace(new Vector2(0f, -0.08f), 0.43f, 0.31f, 0.22f);
        }

        private void BuildCube(int order)
        {
            _bodyVisual = CreateSprite(
                "Cube Body",
                GeneratedArt.RigPart(_kind, CreatureRigPart.Body),
                Vector2.zero,
                1.72f,
                0f,
                order);
            _leftArm = CreateSecondary("Cube Left Arm", CreatureRigPart.LeftArm, new Vector2(-0.76f, 0.24f), 0.5f, 8f, 20f, 0.18f, 3.5f, 0.12f, -90f, order + 3);
            _rightArm = CreateSecondary("Cube Right Arm", CreatureRigPart.RightArm, new Vector2(0.76f, 0.24f), 0.5f, -8f, 20f, 0.18f, 3.2f, 0.12f, -90f, order + 3);
            CreateSecondary("Cube Left Foot", CreatureRigPart.LeftFoot, new Vector2(-0.45f, -0.78f), 0.27f, 0f, 8f, 0.06f, 2.8f, 0.09f, -90f, order + 2);
            CreateSecondary("Cube Right Foot", CreatureRigPart.RightFoot, new Vector2(0.45f, -0.78f), 0.27f, 0f, 8f, 0.06f, 3.1f, 0.09f, -90f, order + 2);
            ConfigureFace(new Vector2(0f, 0.08f), 0.44f, 0.31f, 0.22f);
        }

        private void BuildBird(int order)
        {
            _bodyVisual = CreateSprite(
                "Bird Body",
                GeneratedArt.RigPart(_kind, CreatureRigPart.Body),
                Vector2.zero,
                1.78f,
                0f,
                order);
            _leftArm = CreateSecondary("Bird Left Wing", CreatureRigPart.LeftArm, new Vector2(-0.64f, 0.18f), 0.58f, 12f, 34f, 0.29f, 5f, 0.11f, -90f, order + 3);
            _rightArm = CreateSecondary("Bird Right Wing", CreatureRigPart.RightArm, new Vector2(0.64f, 0.18f), 0.58f, -12f, 34f, 0.29f, 5.4f, 0.11f, -90f, order + 3);
            CreateSecondary("Bird Left Foot", CreatureRigPart.LeftFoot, new Vector2(-0.35f, -0.83f), 0.25f, 0f, 8f, 0.06f, 3.3f, 0.09f, -90f, order + 2);
            CreateSecondary("Bird Right Foot", CreatureRigPart.RightFoot, new Vector2(0.35f, -0.83f), 0.25f, 0f, 8f, 0.06f, 3.7f, 0.09f, -90f, order + 2);
            CreateSecondary("Bird Crest 1", CreatureRigPart.Accent1, new Vector2(0.5f, 0.68f), 0.27f, 5f, 42f, 0.35f, 4.5f, 0.13f, 90f, order + 1);
            CreateSecondary("Bird Crest 2", CreatureRigPart.Accent2, new Vector2(0.63f, 0.66f), 0.29f, 20f, 39f, 0.32f, 4.9f, 0.13f, 90f, order + 1);
            CreateSecondary("Bird Crest 3", CreatureRigPart.Accent3, new Vector2(0.75f, 0.63f), 0.28f, 35f, 40f, 0.33f, 5.1f, 0.13f, 90f, order + 1);
            CreateSecondary("Bird Crest 4", CreatureRigPart.Accent4, new Vector2(0.86f, 0.59f), 0.26f, 50f, 44f, 0.37f, 5.3f, 0.13f, 90f, order + 1);
            ConfigureFace(new Vector2(0f, 0.1f), 0.45f, 0.31f, 0.24f);
        }

        private void BuildRabbit(int order)
        {
            CreateSecondary("Rabbit Left Ear", CreatureRigPart.Accent1, new Vector2(0.2f, 0.72f), 1.3f, -54f, 48f, 0.52f, 2.1f, 0.25f, 90f, order - 2);
            CreateSecondary("Rabbit Right Ear", CreatureRigPart.Accent2, new Vector2(0.44f, 0.7f), 1.3f, -74f, 48f, 0.5f, 2.3f, 0.25f, 90f, order - 2);
            _bodyVisual = CreateSprite(
                "Rabbit Body",
                GeneratedArt.RigPart(_kind, CreatureRigPart.Body),
                Vector2.zero,
                1.9f,
                0f,
                order);
            _leftArm = CreateSecondary("Rabbit Left Arm", CreatureRigPart.LeftArm, new Vector2(-0.72f, 0.2f), 0.53f, 9f, 24f, 0.22f, 2.2f, 0.14f, -90f, order + 3);
            _rightArm = CreateSecondary("Rabbit Right Arm", CreatureRigPart.RightArm, new Vector2(0.72f, 0.2f), 0.53f, -9f, 24f, 0.22f, 2.5f, 0.14f, -90f, order + 3);
            CreateSecondary("Rabbit Left Foot", CreatureRigPart.LeftFoot, new Vector2(-0.43f, -0.88f), 0.32f, 0f, 7f, 0.07f, 2.2f, 0.1f, -90f, order + 2);
            CreateSecondary("Rabbit Right Foot", CreatureRigPart.RightFoot, new Vector2(0.43f, -0.88f), 0.32f, 0f, 7f, 0.07f, 2.5f, 0.1f, -90f, order + 2);
            ConfigureFace(new Vector2(0f, 0.08f), 0.43f, 0.31f, 0.22f);
        }

        private void BuildJelly(int order)
        {
            _bodyVisual = CreateSprite(
                "Jelly Body",
                GeneratedArt.RigPart(_kind, CreatureRigPart.Body),
                Vector2.zero,
                1.34f,
                0f,
                order);
            _leftArm = CreateSecondary("Jelly Left Arm", CreatureRigPart.LeftArm, new Vector2(-0.78f, 0.08f), 0.46f, 8f, 30f, 0.42f, 3f, 0.18f, -90f, order + 3);
            _rightArm = CreateSecondary("Jelly Right Arm", CreatureRigPart.RightArm, new Vector2(0.78f, 0.08f), 0.46f, -8f, 30f, 0.42f, 3.3f, 0.18f, -90f, order + 3);
            CreateSecondary("Jelly Left Foot", CreatureRigPart.LeftFoot, new Vector2(-0.38f, -0.6f), 0.28f, 0f, 12f, 0.16f, 3.8f, 0.13f, -90f, order - 1);
            CreateSecondary("Jelly Right Foot", CreatureRigPart.RightFoot, new Vector2(0.38f, -0.6f), 0.28f, 0f, 12f, 0.16f, 4.1f, 0.13f, -90f, order - 1);
            CreateSecondary("Jelly Crown", CreatureRigPart.Accent1, new Vector2(0f, 0.62f), 0.54f, 0f, 44f, 0.55f, 2.7f, 0.22f, 90f, order + 4);
            ConfigureFace(new Vector2(0f, 0.02f), 0.39f, 0.29f, 0.2f);
        }

        private void ConfigureFace(
            Vector2 faceCenter,
            float eyeHeight,
            float eyeSeparation,
            float mouthHeight)
        {
            _leftEyeRest = faceCenter + new Vector2(-eyeSeparation, 0.1f);
            _rightEyeRest = faceCenter + new Vector2(eyeSeparation, 0.1f);
            _mouthHeight = mouthHeight;
        }

        private void BuildFace(int order)
        {
            _leftEye = CreateSprite("Left Eye", GeneratedArt.Face(FacePart.LeftEye), _leftEyeRest, GetEyeHeight(), -4f, order);
            _rightEye = CreateSprite("Right Eye", GeneratedArt.Face(FacePart.RightEye), _rightEyeRest, GetEyeHeight(), 4f, order);
            _leftPupil = CreateSprite("Left Pupil", GeneratedArt.Face(FacePart.LeftPupil), _leftEyeRest, GetEyeHeight() * 0.43f, 0f, order + 1);
            _rightPupil = CreateSprite("Right Pupil", GeneratedArt.Face(FacePart.RightPupil), _rightEyeRest, GetEyeHeight() * 0.43f, 0f, order + 1);

            Vector2 leftBrowPosition = _leftEyeRest + new Vector2(0f, GetEyeHeight() * 0.52f);
            Vector2 rightBrowPosition = _rightEyeRest + new Vector2(0f, GetEyeHeight() * 0.52f);
            _leftBrow = CreateSprite("Left Brow", GeneratedArt.Face(FacePart.RelaxedBrow), leftBrowPosition, GetEyeHeight() * 0.16f, -4f, order + 2);
            _rightBrow = CreateSprite("Right Brow", GeneratedArt.Face(FacePart.RelaxedBrow), rightBrowPosition, GetEyeHeight() * 0.16f, 4f, order + 2);
            _leftBrowRenderer = _leftBrow.GetComponent<SpriteRenderer>();
            _rightBrowRenderer = _rightBrow.GetComponent<SpriteRenderer>();

            _leftClosedEye = CreateSprite("Left Closed Eye", GeneratedArt.Face(FacePart.LeftClosedEye), _leftEyeRest, GetEyeHeight() * 0.1f, 0f, order + 3);
            _rightClosedEye = CreateSprite("Right Closed Eye", GeneratedArt.Face(FacePart.RightClosedEye), _rightEyeRest, GetEyeHeight() * 0.1f, 0f, order + 3);
            _leftClosedEyeRenderer = _leftClosedEye.GetComponent<SpriteRenderer>();
            _rightClosedEyeRenderer = _rightClosedEye.GetComponent<SpriteRenderer>();
            _leftClosedEyeRenderer.enabled = false;
            _rightClosedEyeRenderer.enabled = false;

            Vector2 mouthPosition = Vector2.Lerp(_leftEyeRest, _rightEyeRest, 0.5f) + new Vector2(0f, -GetEyeHeight() * 0.72f);
            _mouth = CreateSprite("Mouth", GeneratedArt.Face(FacePart.CalmMouth), mouthPosition, _mouthHeight, 0f, order + 2);
            _mouthRenderer = _mouth.GetComponent<SpriteRenderer>();

            _leftBlush = CreateSprite("Left Blush", GeneratedArt.Face(FacePart.LeftBlush), mouthPosition + new Vector2(-GetEyeHeight() * 0.95f, 0.03f), GetEyeHeight() * 0.16f, -7f, order + 1);
            _rightBlush = CreateSprite("Right Blush", GeneratedArt.Face(FacePart.RightBlush), mouthPosition + new Vector2(GetEyeHeight() * 0.95f, 0.03f), GetEyeHeight() * 0.16f, 7f, order + 1);
            bool showBlush = _kind == CharacterKind.Jelly || _kind == CharacterKind.Bird;
            _leftBlush.gameObject.SetActive(showBlush);
            _rightBlush.gameObject.SetActive(showBlush);

            _leftEyeBaseScale = _leftEye.localScale;
            _rightEyeBaseScale = _rightEye.localScale;
            _leftPupilBaseScale = _leftPupil.localScale;
            _rightPupilBaseScale = _rightPupil.localScale;
            _leftBrowBaseScale = _leftBrow.localScale;
            _rightBrowBaseScale = _rightBrow.localScale;
            _mouthBaseScale = _mouth.localScale;
        }

        private void BuildImpactPose(int order)
        {
            foreach (SpriteRenderer renderer in
                GetComponentsInChildren<SpriteRenderer>())
            {
                _articulatedRenderers.Add(renderer);
            }

            GetImpactPoseGeometry(out Vector2 position, out float height);
            _impactPose = CreateSprite(
                "Character Impact Pose",
                GeneratedArt.ImpactCharacter(_kind),
                position,
                height,
                0f,
                order);
            _impactPoseRenderer = _impactPose.GetComponent<SpriteRenderer>();
            _impactPoseRenderer.enabled = false;
        }

        private void UpdateImpactPose()
        {
            if (!_impactPoseRenderer.enabled)
            {
                return;
            }

            float remaining = _impactPoseEndsAt - Time.unscaledTime;
            if (remaining <= 0f)
            {
                _impactPoseRenderer.enabled = false;
                RestoreArticulatedRenderers();
                return;
            }

            float alpha = Mathf.InverseLerp(0f, 0.16f, remaining);
            _impactPoseRenderer.color = new Color(1f, 1f, 1f, alpha);
            float wobble = Mathf.Sin((Time.unscaledTime * 31f) + _idlePhase) *
                Mathf.Min(remaining * 8f, 1f) *
                3.5f;
            _impactPose.localRotation = Quaternion.Euler(0f, 0f, wobble);
        }

        private void HideArticulatedRenderers()
        {
            if (_impactPoseRenderer.enabled)
            {
                return;
            }

            _impactRendererStates.Clear();
            foreach (SpriteRenderer renderer in _articulatedRenderers)
            {
                _impactRendererStates.Add(renderer.enabled);
                renderer.enabled = false;
            }
        }

        private void RestoreArticulatedRenderers()
        {
            int count = Mathf.Min(
                _articulatedRenderers.Count,
                _impactRendererStates.Count);
            for (int index = 0; index < count; index += 1)
            {
                _articulatedRenderers[index].enabled =
                    _impactRendererStates[index];
            }

            _impactRendererStates.Clear();
        }

        private void GetImpactPoseGeometry(out Vector2 position, out float height)
        {
            position = Vector2.zero;
            switch (_kind)
            {
                case CharacterKind.Pear:
                    height = 2.7f;
                    position = new Vector2(0f, 0.08f);
                    return;
                case CharacterKind.Cube:
                    height = 1.84f;
                    return;
                case CharacterKind.Bird:
                    height = 1.9f;
                    return;
                case CharacterKind.Rabbit:
                    height = 2.72f;
                    position = new Vector2(0f, 0.26f);
                    return;
                default:
                    height = 1.48f;
                    return;
            }
        }

        private SecondaryPart CreateSecondary(
            string name,
            CreatureRigPart part,
            Vector2 position,
            float height,
            float rotation,
            float windDegrees,
            float inertiaDegrees,
            float idleDegrees,
            float smoothTime,
            float aimAxisDegrees,
            int sortingOrder)
        {
            Transform partTransform = CreateSprite(
                name,
                GeneratedArt.RigPart(_kind, part),
                position,
                height,
                rotation,
                sortingOrder);
            SecondaryPart secondary = new SecondaryPart(
                partTransform,
                position,
                rotation,
                windDegrees,
                inertiaDegrees,
                idleDegrees,
                smoothTime,
                aimAxisDegrees,
                _idlePhase + (_secondaryParts.Count * 0.73f));
            _secondaryParts.Add(secondary);
            return secondary;
        }

        private Transform CreateSprite(
            string name,
            Sprite sprite,
            Vector2 position,
            float height,
            float rotation,
            int sortingOrder)
        {
            GameObject spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(_visualRoot, false);
            spriteObject.transform.localPosition = new Vector3(position.x, position.y, 0f);
            spriteObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.material = GeneratedArt.ChromaMaterial;
            renderer.sortingOrder = sortingOrder;
            float scale = height / sprite.bounds.size.y;
            spriteObject.transform.localScale = new Vector3(scale, scale, 1f);
            return spriteObject.transform;
        }

        private void UpdateBlink()
        {
            if (_blinkStartedAt < 0f && Time.time >= _nextBlinkAt)
            {
                _blinkStartedAt = Time.time;
                _blinkDuration = Mathf.Lerp(0.095f, 0.165f, NextRandom01());
            }

            if (_blinkStartedAt < 0f)
            {
                BlinkAmount = 0f;
                ApplyBlink(0f);
                return;
            }

            float progress = (Time.time - _blinkStartedAt) / _blinkDuration;
            if (progress >= 1f)
            {
                _blinkStartedAt = -1f;
                _nextBlinkAt = Time.time + GetBlinkInterval();
                BlinkAmount = 0f;
                ApplyBlink(0f);
                return;
            }

            BlinkAmount = Mathf.Sin(progress * Mathf.PI);
            ApplyBlink(BlinkAmount);
        }

        private void ApplyBlink(float amount)
        {
            float openScale = Mathf.Lerp(1f, 0.06f, amount);
            SetBlinkScale(_leftEye, _leftEyeBaseScale, _emotionEyeScale, openScale);
            SetBlinkScale(_rightEye, _rightEyeBaseScale, _emotionEyeScale, openScale);
            SetBlinkScale(_leftPupil, _leftPupilBaseScale, _emotionEyeScale, openScale);
            SetBlinkScale(_rightPupil, _rightPupilBaseScale, _emotionEyeScale, openScale);
            bool showClosed = amount > 0.64f;
            _leftClosedEyeRenderer.enabled = showClosed;
            _rightClosedEyeRenderer.enabled = showClosed;
        }

        private void UpdateGaze(float deltaTime)
        {
            if (Time.time >= _nextGazeAt)
            {
                float windLook = Mathf.Sign(_signedWind) * GetEyeHeight() * 0.1f;
                float personality = GetGazeAmplitude();
                _gazeTarget = new Vector2(
                    windLook + ((NextRandom01() - 0.5f) * personality),
                    (NextRandom01() - 0.5f) * personality * 0.55f);
                _nextGazeAt = Time.time + GetGazeInterval();
            }

            _gaze = Vector2.SmoothDamp(_gaze, _gazeTarget, ref _gazeVelocity, 0.12f, 1f, deltaTime);
            _leftPupil.localPosition = new Vector3(_leftEyeRest.x + _gaze.x, _leftEyeRest.y + _gaze.y, 0f);
            _rightPupil.localPosition = new Vector3(_rightEyeRest.x + _gaze.x, _rightEyeRest.y + _gaze.y, 0f);
        }

        private void UpdateSecondaryMotion(float deltaTime)
        {
            Vector2 velocity = _body.linearVelocity;
            float accelerationX = (velocity.x - _previousVelocity.x) / deltaTime;
            _previousVelocity = velocity;
            float inertia = Mathf.Clamp(-accelerationX, -16f, 16f);
            float angularVelocity = Mathf.Clamp(_body.angularVelocity, -180f, 180f);
            float time = Time.time;
            float fallingSpeed = _falling ? Mathf.Clamp01((-velocity.y + 0.5f) / 5f) : 0f;
            _fallBlend = Mathf.MoveTowards(
                _fallBlend,
                fallingSpeed,
                deltaTime * (_falling ? 4.5f : 8f));

            foreach (SecondaryPart part in _secondaryParts)
            {
                float idle = Mathf.Sin((time * (1.7f + (part.IdlePhase * 0.07f))) + part.IdlePhase) *
                    part.IdleDegrees;
                float targetRotation = part.RestRotation +
                    (_signedWind * part.WindDegrees) +
                    (inertia * part.InertiaDegrees * 0.055f) -
                    (angularVelocity * part.InertiaDegrees * 0.008f) +
                    idle +
                    (_fallBlend *
                        Mathf.Sin((time * (7.5f + (part.IdlePhase * 0.21f))) + part.IdlePhase) *
                        (10f + (part.InertiaDegrees * 28f))) +
                    (_impactKick * Mathf.Sin((time * 24f) + part.IdlePhase) * 24f);

                bool gripping = part == _gripArm && _gripTarget != null;
                if (gripping)
                {
                    Vector3 worldTarget = _gripTarget.TransformPoint(_gripLocalAnchor);
                    Vector3 localTarget = _visualRoot.InverseTransformPoint(worldTarget);
                    Vector2 direction = new Vector2(
                        localTarget.x - part.RestPosition.x,
                        localTarget.y - part.RestPosition.y);
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        targetRotation = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) -
                            part.AimAxisDegrees;
                        float lengthScale = Mathf.Clamp(
                            direction.magnitude / Mathf.Max(part.SpriteLength, 0.01f),
                            0.82f,
                            1.34f);
                        part.Transform.localScale = new Vector3(
                            part.BaseScale.x,
                            part.BaseScale.y * lengthScale,
                            part.BaseScale.z);
                    }
                }
                else
                {
                    part.Transform.localScale = Vector3.Lerp(
                        part.Transform.localScale,
                        part.BaseScale,
                        Mathf.Clamp01(deltaTime * 12f));
                }

                part.CurrentRotation = Mathf.SmoothDampAngle(
                    part.CurrentRotation,
                    targetRotation,
                    ref part.RotationVelocity,
                    part.SmoothTime,
                    720f,
                    deltaTime);
                part.Transform.localRotation = Quaternion.Euler(0f, 0f, part.CurrentRotation);

                float footLift = part.AimAxisDegrees < -80f && part.AimAxisDegrees > -100f
                    ? Mathf.Abs(Mathf.Sin((time * 4.2f) + part.IdlePhase)) * 0.025f
                    : 0f;
                part.Transform.localPosition = new Vector3(
                    part.RestPosition.x,
                    part.RestPosition.y + footLift,
                    0f);
            }

            _impactKick = Mathf.MoveTowards(_impactKick, 0f, deltaTime * 1.9f);
        }

        private void UpdateBodyLife(float deltaTime)
        {
            float breathSpeed = _kind == CharacterKind.Jelly ? 2f : 2.35f;
            float breathAmount = _kind == CharacterKind.Cube ? 0.012f : 0.019f;
            float breath = Mathf.Sin((Time.time * breathSpeed) + _idlePhase) * breathAmount;
            float motionSquash = Mathf.Clamp01(Mathf.Abs(_body.angularVelocity) / 170f) * 0.025f;
            float impactSquash = _impactKick * 0.12f;
            float fallingStretch = _fallBlend * 0.055f;
            float targetX = 1f + breath + motionSquash + impactSquash - fallingStretch;
            float targetY = 1f - (breath * 0.7f) - motionSquash - impactSquash + fallingStretch;
            Vector3 targetScale = new Vector3(targetX, targetY, 1f);
            _visualRoot.localScale = Vector3.Lerp(
                _visualRoot.localScale,
                targetScale,
                Mathf.Clamp01(deltaTime * 8f));

            float browTremble = _emotion == CreatureEmotion.Panic
                ? Mathf.Sin((Time.time * 17f) + _idlePhase) * 3.5f
                : 0f;
            _leftBrow.localRotation = Quaternion.Euler(0f, 0f, -4f + browTremble);
            _rightBrow.localRotation = Quaternion.Euler(0f, 0f, 4f - browTremble);
        }

        private void SetEmotion(CreatureEmotion emotion)
        {
            if (_emotion == emotion && _mouthRenderer != null)
            {
                return;
            }

            _emotion = emotion;
            GetFaceForEmotion(emotion, out FacePart mouth, out FacePart brow, out float mouthScale, out float eyeScale);
            _mouthRenderer.sprite = GeneratedArt.Face(mouth);
            float mouthHeight = _mouthHeight * mouthScale;
            float mouthSpriteScale = mouthHeight / _mouthRenderer.sprite.bounds.size.y;
            _mouthBaseScale = new Vector3(mouthSpriteScale, mouthSpriteScale, 1f);
            _mouth.localScale = _mouthBaseScale;
            _leftBrowRenderer.sprite = GeneratedArt.Face(brow);
            _rightBrowRenderer.sprite = GeneratedArt.Face(brow);

            _emotionEyeScale = eyeScale;
            ApplyBlink(BlinkAmount);
            _leftBrow.localScale = _leftBrowBaseScale;
            _rightBrow.localScale = new Vector3(
                -Mathf.Abs(_rightBrowBaseScale.x),
                _rightBrowBaseScale.y,
                _rightBrowBaseScale.z);
        }

        private void GetFaceForEmotion(
            CreatureEmotion emotion,
            out FacePart mouth,
            out FacePart brow,
            out float mouthScale,
            out float eyeScale)
        {
            mouthScale = 1f;
            eyeScale = 1f;
            if (emotion == CreatureEmotion.Impact)
            {
                switch (_kind)
                {
                    case CharacterKind.Pear:
                    case CharacterKind.Rabbit:
                        mouth = FacePart.UncertainMouth;
                        break;
                    case CharacterKind.Bird:
                        mouth = FacePart.PanicMouth;
                        break;
                    default:
                        mouth = FacePart.EffortMouth;
                        break;
                }

                brow = FacePart.DizzyBrow;
                mouthScale = _kind == CharacterKind.Bird ? 0.94f : 0.72f;
                eyeScale = _kind == CharacterKind.Bird ? 1.15f : 0.94f;
                return;
            }

            if (emotion == CreatureEmotion.Relief)
            {
                mouth = FacePart.JoyMouth;
                brow = FacePart.RelaxedBrow;
                mouthScale = _kind == CharacterKind.Jelly ? 1.08f : 0.9f;
                eyeScale = 0.94f;
                return;
            }

            if (emotion == CreatureEmotion.Panic)
            {
                mouth = _kind == CharacterKind.Pear
                    ? FacePart.EffortMouth
                    : _kind == CharacterKind.Rabbit
                        ? FacePart.UncertainMouth
                        : FacePart.PanicMouth;
                brow = FacePart.WorriedBrow;
                mouthScale = _kind == CharacterKind.Bird
                    ? 1.25f
                    : _kind == CharacterKind.Pear || _kind == CharacterKind.Rabbit
                        ? 0.78f
                        : 1.05f;
                eyeScale = _kind == CharacterKind.Cube || _kind == CharacterKind.Bird ? 1.14f : 1.07f;
                return;
            }

            if (emotion == CreatureEmotion.Effort)
            {
                mouth = _kind == CharacterKind.Rabbit
                    ? FacePart.UncertainMouth
                    : FacePart.EffortMouth;
                brow = _kind == CharacterKind.Rabbit || _kind == CharacterKind.Pear
                    ? FacePart.DeterminedBrow
                    : FacePart.WorriedBrow;
                mouthScale = _kind == CharacterKind.Pear || _kind == CharacterKind.Rabbit
                    ? 0.72f
                    : 0.86f;
                eyeScale = 1.03f;
                return;
            }

            if (emotion == CreatureEmotion.Alert)
            {
                mouth = _kind == CharacterKind.Bird
                    ? FacePart.EffortMouth
                    : _kind == CharacterKind.Jelly
                        ? FacePart.JoyMouth
                        : FacePart.UncertainMouth;
                brow = _kind == CharacterKind.Rabbit
                    ? FacePart.DeterminedBrow
                    : FacePart.WorriedBrow;
                mouthScale = 0.78f;
                eyeScale = _kind == CharacterKind.Cube ? 1.08f : 1.02f;
                return;
            }

            if (_kind == CharacterKind.Jelly)
            {
                mouth = FacePart.JoyMouth;
                brow = FacePart.RelaxedBrow;
                mouthScale = 0.72f;
            }
            else if (_kind == CharacterKind.Cube)
            {
                mouth = FacePart.UncertainMouth;
                brow = FacePart.WorriedBrow;
                mouthScale = 0.58f;
            }
            else
            {
                mouth = FacePart.CalmMouth;
                brow = FacePart.RelaxedBrow;
                mouthScale = 0.82f;
            }

            eyeScale = 1f;
        }

        private void GetEmotionThresholds(out float effortThreshold, out float panicThreshold)
        {
            switch (_kind)
            {
                case CharacterKind.Pear:
                    effortThreshold = 0.24f;
                    panicThreshold = 0.78f;
                    return;
                case CharacterKind.Cube:
                    effortThreshold = 0.1f;
                    panicThreshold = 0.43f;
                    return;
                case CharacterKind.Bird:
                    effortThreshold = 0.11f;
                    panicThreshold = 0.34f;
                    return;
                case CharacterKind.Rabbit:
                    effortThreshold = 0.2f;
                    panicThreshold = 0.62f;
                    return;
                default:
                    effortThreshold = 0.16f;
                    panicThreshold = 0.54f;
                    return;
            }
        }

        private float GetEyeHeight()
        {
            switch (_kind)
            {
                case CharacterKind.Bird:
                    return 0.45f;
                case CharacterKind.Cube:
                    return 0.44f;
                case CharacterKind.Pear:
                case CharacterKind.Rabbit:
                    return 0.43f;
                default:
                    return 0.39f;
            }
        }

        private float GetBlinkInterval()
        {
            float minimum;
            float maximum;
            switch (_kind)
            {
                case CharacterKind.Cube:
                    minimum = 1.2f;
                    maximum = 2.6f;
                    break;
                case CharacterKind.Bird:
                    minimum = 1.5f;
                    maximum = 3f;
                    break;
                case CharacterKind.Rabbit:
                    minimum = 2f;
                    maximum = 4.3f;
                    break;
                case CharacterKind.Jelly:
                    minimum = 1.7f;
                    maximum = 3.8f;
                    break;
                default:
                    minimum = 2.6f;
                    maximum = 5f;
                    break;
            }

            return Mathf.Lerp(minimum, maximum, NextRandom01());
        }

        private float GetGazeInterval()
        {
            switch (_kind)
            {
                case CharacterKind.Cube:
                    return Mathf.Lerp(0.35f, 0.8f, NextRandom01());
                case CharacterKind.Bird:
                    return Mathf.Lerp(0.45f, 1f, NextRandom01());
                case CharacterKind.Rabbit:
                    return Mathf.Lerp(0.75f, 1.65f, NextRandom01());
                case CharacterKind.Jelly:
                    return Mathf.Lerp(0.55f, 1.3f, NextRandom01());
                default:
                    return Mathf.Lerp(1.05f, 2.2f, NextRandom01());
            }
        }

        private float GetGazeAmplitude()
        {
            switch (_kind)
            {
                case CharacterKind.Cube:
                    return 0.09f;
                case CharacterKind.Bird:
                    return 0.08f;
                case CharacterKind.Jelly:
                    return 0.07f;
                default:
                    return 0.05f;
            }
        }

        private float GetReliefDuration()
        {
            switch (_kind)
            {
                case CharacterKind.Bird:
                    return 0.75f;
                case CharacterKind.Jelly:
                    return 1.05f;
                case CharacterKind.Cube:
                    return 0.55f;
                default:
                    return 0.85f;
            }
        }

        private float NextRandom01()
        {
            _randomState ^= _randomState << 13;
            _randomState ^= _randomState >> 17;
            _randomState ^= _randomState << 5;
            return (_randomState & 0x00FFFFFFu) / 16777215f;
        }

        private static uint ConvertKindToSeed(CharacterKind kind)
        {
            switch (kind)
            {
                case CharacterKind.Pear:
                    return 1u;
                case CharacterKind.Cube:
                    return 2u;
                case CharacterKind.Bird:
                    return 3u;
                case CharacterKind.Rabbit:
                    return 4u;
                default:
                    return 5u;
            }
        }

        private static void SetBlinkScale(
            Transform target,
            Vector3 baseScale,
            float emotionScale,
            float blinkScale)
        {
            target.localScale = new Vector3(
                baseScale.x * emotionScale,
                baseScale.y * emotionScale * blinkScale,
                baseScale.z);
        }

        private sealed class SecondaryPart
        {
            public SecondaryPart(
                Transform transform,
                Vector2 restPosition,
                float restRotation,
                float windDegrees,
                float inertiaDegrees,
                float idleDegrees,
                float smoothTime,
                float aimAxisDegrees,
                float idlePhase)
            {
                Transform = transform;
                RestPosition = restPosition;
                RestRotation = restRotation;
                WindDegrees = windDegrees;
                InertiaDegrees = inertiaDegrees;
                IdleDegrees = idleDegrees;
                SmoothTime = smoothTime;
                AimAxisDegrees = aimAxisDegrees;
                IdlePhase = idlePhase;
                CurrentRotation = restRotation;
                BaseScale = transform.localScale;
                SpriteLength = transform.GetComponent<SpriteRenderer>().bounds.size.y;
            }

            public Transform Transform { get; }

            public Vector2 RestPosition { get; }

            public float RestRotation { get; }

            public float WindDegrees { get; }

            public float InertiaDegrees { get; }

            public float IdleDegrees { get; }

            public float SmoothTime { get; }

            public float AimAxisDegrees { get; }

            public float IdlePhase { get; }

            public Vector3 BaseScale { get; }

            public float SpriteLength { get; }

            public float CurrentRotation;

            public float RotationVelocity;
        }
    }
}
