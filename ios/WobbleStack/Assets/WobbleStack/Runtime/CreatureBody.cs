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
        private Sprite _regularSprite;
        private Sprite _impactSprite;
        private float _idlePhase;
        private bool _impacted;

        public Rigidbody2D Body { get; private set; }

        public CharacterKind Kind { get; private set; }

        public void Initialize(WobbleStackGame game, CharacterKind kind, Transform visual, float idlePhase, Sprite impactSprite)
        {
            _game = game;
            Kind = kind;
            _visual = visual;
            _visualBaseScale = visual.localScale;
            _idlePhase = idlePhase;
            Body = GetComponent<Rigidbody2D>();
            _renderer = visual.GetComponent<SpriteRenderer>();
            _regularSprite = _renderer.sprite;
            _impactSprite = impactSprite;
        }

        public void ResetReaction()
        {
            _impacted = false;
            _visual.localRotation = Quaternion.identity;
            _renderer.sprite = _regularSprite;
            _renderer.color = Color.white;
        }

        public void SetWind(float signedIntensity)
        {
            if (_impacted)
            {
                return;
            }

            _visual.localRotation = Quaternion.Euler(0f, 0f, -signedIntensity * 7f);
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
            _visual.localScale = new Vector3(
                _visualBaseScale.x * breath,
                _visualBaseScale.y / breath,
                _visualBaseScale.z);
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
