using UnityEngine;

namespace WobbleStack.Runtime
{
    [RequireComponent(typeof(Rigidbody2D))]
    internal sealed class CreatureBody : MonoBehaviour
    {
        private WobbleStackGame _game;
        private CreatureRig _rig;
        private CreatureBody _lowerNeighbor;
        private Rigidbody2D _recoveryAnchor;
        private Collider2D _collider;
        private DistanceJoint2D _gripJoint;
        private float _signedWind;
        private float _windIntensity;
        private float _gripEndsAt;
        private float _gripCooldownUntil;
        private float _recoveryGripMinimumUntil;
        private int _stackIndex;
        private bool _gripActive;
        private bool _gripUsed;
        private bool _impacted;
        private bool _recoveryGripRequested;
        private bool _platformHoldActive;

        public Rigidbody2D Body { get; private set; }

        public CharacterKind Kind { get; private set; }

        public CreatureRig Rig => _rig;

        public bool HasActiveGrip => _gripActive || _platformHoldActive;

        public bool HasGripJoint => _gripJoint != null;

        public bool GripWasUsed => _gripUsed;

        public bool IsRecoveryGripRequested => _recoveryGripRequested;

        public bool IsFalling { get; private set; }

        public void Initialize(
            WobbleStackGame game,
            CharacterKind kind,
            CreatureRig rig)
        {
            _game = game;
            Kind = kind;
            _rig = rig;
            Body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        public void ConfigureLowerNeighbor(CreatureBody lowerNeighbor, int stackIndex)
        {
            _lowerNeighbor = lowerNeighbor;
            _stackIndex = stackIndex;
            CreateGripJoint();
            _gripCooldownUntil = Time.time + (stackIndex * 0.11f);
        }

        private void CreateGripJoint()
        {
            if (_gripJoint != null || _lowerNeighbor == null)
            {
                return;
            }

            _gripJoint = gameObject.AddComponent<DistanceJoint2D>();
            _gripJoint.enabled = false;
            _gripJoint.connectedBody = _lowerNeighbor.Body;
            _gripJoint.autoConfigureConnectedAnchor = false;
            _gripJoint.maxDistanceOnly = true;
            _gripJoint.enableCollision = true;
            _gripJoint.breakForce = GetGripBreakForce();
        }

        public void ConfigureRecoveryAnchor(Rigidbody2D recoveryAnchor)
        {
            _recoveryAnchor = recoveryAnchor;
        }

        public void ResetReaction()
        {
            _impacted = false;
            _signedWind = 0f;
            _windIntensity = 0f;
            EndGrip(false);
            EndPlatformHold();
            _gripUsed = false;
            _recoveryGripRequested = false;
            _recoveryGripMinimumUntil = 0f;
            IsFalling = false;
            _rig.ResetReaction();
        }

        public void SetWind(float signedIntensity)
        {
            if (_impacted)
            {
                return;
            }

            _signedWind = signedIntensity;
            _windIntensity = Mathf.Abs(signedIntensity);
            _rig.SetWind(signedIntensity);
        }

        public void ShowImpactReaction()
        {
            _impacted = true;
            IsFalling = false;
            EndGrip(true);
            EndPlatformHold();
            _rig.ShowImpactReaction();
        }

        public void BeginFallReaction()
        {
            if (_impacted || IsFalling || Body.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            IsFalling = true;
            _recoveryGripRequested = false;
            _recoveryGripMinimumUntil = 0f;
            EndGrip(true);
            EndPlatformHold();
            _rig.ShowFallReaction();

            float alternatingDirection = (_stackIndex & 1) == 0 ? -1f : 1f;
            float windDirection = Mathf.Abs(_signedWind) > 0.01f ? Mathf.Sign(_signedWind) : 0f;
            float horizontalImpulse = (alternatingDirection * 0.08f) + (windDirection * 0.06f);
            float liftImpulse = 0.08f + (_stackIndex * 0.012f);
            Body.AddForce(new Vector2(horizontalImpulse, liftImpulse), ForceMode2D.Impulse);
            Body.AddTorque(alternatingDirection * (0.055f + (_stackIndex * 0.012f)), ForceMode2D.Impulse);
        }

        public void ShowReliefReaction()
        {
            if (_impacted)
            {
                return;
            }

            IsFalling = false;
            EndGrip(false);
            EndPlatformHold();
            _rig.ShowReliefReaction();
        }

        public void SetRecoveryGrip(bool requested)
        {
            _recoveryGripRequested =
                requested ||
                Time.time < _recoveryGripMinimumUntil;
            if (!_recoveryGripRequested)
            {
                EndPlatformHold();
            }

            if (_recoveryGripRequested)
            {
                _gripCooldownUntil = Mathf.Min(_gripCooldownUntil, Time.time);
            }
        }

        public void HoldRecoveryGripFor(float seconds)
        {
            _recoveryGripMinimumUntil = Mathf.Max(
                _recoveryGripMinimumUntil,
                Time.time + Mathf.Max(0f, seconds));
            SetRecoveryGrip(true);
        }

        private void FixedUpdate()
        {
            if (_impacted || Body.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            if (_lowerNeighbor == null)
            {
                UpdatePlatformHold();
                return;
            }

            if (_gripActive)
            {
                if (Time.time >= _gripEndsAt)
                {
                    EndGrip(true);
                }

                return;
            }

            if (_recoveryGripRequested && Time.time >= _gripCooldownUntil)
            {
                BeginGrip(true);
                return;
            }

            if (_windIntensity < GetGripWindThreshold() || Time.time < _gripCooldownUntil)
            {
                return;
            }

            float relativeSpeed = Mathf.Abs(Body.linearVelocity.x - _lowerNeighbor.Body.linearVelocity.x);
            float relativeAngle = Mathf.Abs(Mathf.DeltaAngle(Body.rotation, _lowerNeighbor.Body.rotation));
            float horizontalOffset = Mathf.Abs(Body.position.x - _lowerNeighbor.Body.position.x);
            float danger = Mathf.Max(relativeSpeed * 0.75f, Mathf.Max(relativeAngle / 16f, horizontalOffset));
            if (danger >= GetGripDangerThreshold())
            {
                BeginGrip(false);
            }
        }

        private void BeginGrip(bool recovering)
        {
            if (_lowerNeighbor == null)
            {
                return;
            }

            CreateGripJoint();
            if (_gripJoint == null)
            {
                return;
            }

            bool useLeftArm = recovering
                ? Body.position.x >= _lowerNeighbor.Body.position.x
                : _signedWind >= 0f;
            float side = useLeftArm ? -1f : 1f;
            float upperHalfHeight = _collider.bounds.extents.y;
            Collider2D lowerCollider = _lowerNeighbor.GetComponent<Collider2D>();
            float lowerHalfHeight = lowerCollider.bounds.extents.y;
            Vector2 anchor = new Vector2(side * _collider.bounds.extents.x * 0.18f, -upperHalfHeight * 0.08f);
            Vector2 connectedAnchor = new Vector2(side * lowerCollider.bounds.extents.x * 0.22f, lowerHalfHeight * 0.12f);
            Vector2 anchorWorld = Body.GetPoint(anchor);
            Vector2 connectedWorld = _lowerNeighbor.Body.GetPoint(connectedAnchor);
            float currentDistance = Vector2.Distance(anchorWorld, connectedWorld);

            _gripJoint.anchor = anchor;
            _gripJoint.connectedAnchor = connectedAnchor;
            _gripJoint.distance = recovering
                ? Mathf.Max(
                    Mathf.Abs(anchorWorld.y - connectedWorld.y) + 0.02f,
                    currentDistance - 0.18f)
                : currentDistance + 0.14f;
            _gripJoint.breakForce = recovering
                ? Mathf.Max(100f, GetGripBreakForce() * 12f)
                : GetGripBreakForce();
            _gripJoint.enabled = true;
            _gripActive = true;
            _gripUsed = true;
            _gripEndsAt = Time.time + (recovering ? 1.3f : GetGripDuration());
            Vector2 visibleAnchor = new Vector2(
                side * lowerCollider.bounds.extents.x * 0.55f,
                lowerHalfHeight * 0.42f);
            _rig.SetGripTarget(_lowerNeighbor.transform, visibleAnchor, useLeftArm);
        }

        private void UpdatePlatformHold()
        {
            if (!_recoveryGripRequested || _recoveryAnchor == null)
            {
                EndPlatformHold();
                return;
            }

            if (!_platformHoldActive)
            {
                bool useLeftArm = Body.position.x >= _recoveryAnchor.position.x;
                float side = useLeftArm ? -1f : 1f;
                Vector2 worldAnchor = new Vector2(
                    Body.position.x + (side * _collider.bounds.extents.x * 0.32f),
                    _recoveryAnchor.position.y + 0.42f);
                Vector2 localAnchor = _recoveryAnchor.transform.InverseTransformPoint(worldAnchor);
                _rig.SetGripTarget(_recoveryAnchor.transform, localAnchor, useLeftArm);
                _platformHoldActive = true;
                _gripUsed = true;
            }

        }

        private void EndPlatformHold()
        {
            if (!_platformHoldActive)
            {
                return;
            }

            _platformHoldActive = false;
            _rig.ClearGripTarget();
        }

        private void EndGrip(bool startCooldown)
        {
            if (_gripJoint != null)
            {
                _gripJoint.enabled = false;
            }

            _gripActive = false;
            _rig.ClearGripTarget();
            if (startCooldown)
            {
                _gripCooldownUntil = Time.time +
                    (_recoveryGripRequested ? 0.18f : GetGripCooldown());
            }
        }

        private void OnJointBreak2D(Joint2D brokenJoint)
        {
            if (brokenJoint != _gripJoint)
            {
                return;
            }

            _gripJoint = null;
            _gripActive = false;
            _rig.ClearGripTarget();
            _gripCooldownUntil = Time.time +
                (_recoveryGripRequested ? 0.18f : GetGripCooldown());
        }

        private float GetGripWindThreshold()
        {
            return Mathf.Min(0.46f, 0.2f + (_stackIndex * 0.055f));
        }

        private float GetGripDangerThreshold()
        {
            switch (Kind)
            {
                case CharacterKind.Rabbit:
                    return 0.18f;
                case CharacterKind.Pear:
                    return 0.24f;
                case CharacterKind.Jelly:
                    return 0.28f;
                default:
                    return 0.22f;
            }
        }

        private float GetGripDuration()
        {
            switch (Kind)
            {
                case CharacterKind.Rabbit:
                    return 0.9f;
                case CharacterKind.Jelly:
                    return 0.7f;
                case CharacterKind.Cube:
                    return 0.4f;
                default:
                    return 0.48f;
            }
        }

        private float GetGripCooldown()
        {
            return 1.35f + (_stackIndex * 0.22f);
        }

        private float GetGripBreakForce()
        {
            switch (Kind)
            {
                case CharacterKind.Rabbit:
                    return 3.2f;
                case CharacterKind.Jelly:
                    return 2.4f;
                case CharacterKind.Cube:
                    return 1.1f;
                default:
                    return 1.3f;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_impacted || collision.collider.gameObject.name != "Road")
            {
                return;
            }

            ShowImpactReaction();
            Vector2 point = collision.contactCount > 0 ? collision.GetContact(0).point : Body.position;
            _game.RegisterImpact(this, point);
        }
    }
}
