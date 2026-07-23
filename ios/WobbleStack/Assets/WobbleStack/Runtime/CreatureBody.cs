using UnityEngine;

namespace WobbleStack.Runtime
{
    [RequireComponent(typeof(Rigidbody2D))]
    internal sealed class CreatureBody : MonoBehaviour
    {
        private WobbleStackGame _game;
        private Transform _visual;
        private Vector3 _visualBaseScale;
        private SpriteRenderer _renderer;
        private Sprite _calmSprite;
        private Sprite _windSprite;
        private Sprite _impactSprite;
        private float _idlePhase;
        private float _windIntensity;
        private float _windLeanDegrees;
        private bool _impacted;

        public Rigidbody2D Body { get; private set; }

        public CharacterKind Kind { get; private set; }

        public void Initialize(
            WobbleStackGame game,
            CharacterKind kind,
            Transform visual,
            float idlePhase,
            Sprite windSprite,
            Sprite impactSprite)
        {
            _game = game;
            Kind = kind;
            _visual = visual;
            _visualBaseScale = visual.localScale;
            _idlePhase = idlePhase;
            Body = GetComponent<Rigidbody2D>();
            _renderer = visual.GetComponent<SpriteRenderer>();
            _calmSprite = _renderer.sprite;
            _windSprite = windSprite;
            _impactSprite = impactSprite;
        }

        public void ResetReaction()
        {
            _impacted = false;
            _windIntensity = 0f;
            _windLeanDegrees = 0f;
            _visual.localRotation = Quaternion.identity;
            _renderer.sprite = _calmSprite;
            _renderer.color = Color.white;
        }

        public void SetWind(float signedIntensity)
        {
            if (_impacted)
            {
                return;
            }

            _windIntensity = Mathf.Abs(signedIntensity);
            _windLeanDegrees = -signedIntensity * 9f;
            if (_windIntensity >= 0.16f)
            {
                _renderer.sprite = _windSprite;
            }
            else if (_windIntensity <= 0.06f)
            {
                _renderer.sprite = _calmSprite;
            }
        }

        public void ShowImpactReaction()
        {
            _impacted = true;
            _renderer.sprite = _impactSprite;
            _renderer.color = new Color(0.98f, 0.97f, 1f, 1f);
            _visual.localRotation = Quaternion.Euler(0f, 0f, Kind == CharacterKind.Rabbit ? 12f : -9f);
        }

        private void LateUpdate()
        {
            if (_visual == null)
            {
                return;
            }

            float breath = 1f + (Mathf.Sin((Time.time * 2.3f) + _idlePhase) * 0.018f);
            float windWobble = Mathf.Sin((Time.time * 6.2f) + _idlePhase) * _windIntensity * 0.025f;
            _visual.localScale = new Vector3(
                _visualBaseScale.x * (breath + windWobble),
                _visualBaseScale.y / (breath + (windWobble * 0.6f)),
                _visualBaseScale.z);
            if (!_impacted)
            {
                Quaternion target = Quaternion.Euler(0f, 0f, _windLeanDegrees);
                _visual.localRotation = Quaternion.Lerp(_visual.localRotation, target, Mathf.Clamp01(Time.deltaTime * 9f));
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_impacted || collision.collider.gameObject.name != "Catch Floor")
            {
                return;
            }

            ShowImpactReaction();
            Vector2 point = collision.contactCount > 0 ? collision.GetContact(0).point : Body.position;
            _game.RegisterImpact(this, point);
        }
    }
}
