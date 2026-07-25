using UnityEngine;

namespace WobbleStack.Runtime
{
    internal sealed class TransientFx : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Vector3 _startScale;
        private float _createdAt;
        private float _duration;
        private float _endScale;
        private float _gravity;
        private float _angularVelocity;
        private Vector2 _velocity;

        public void Initialize(float duration, Vector2 velocity)
        {
            Initialize(duration, velocity, 0f, 0f, 1.35f);
        }

        public void Initialize(
            float duration,
            Vector2 velocity,
            float gravity,
            float angularVelocity,
            float endScale)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _startScale = transform.localScale;
            _createdAt = Time.unscaledTime;
            _duration = duration;
            _velocity = velocity;
            _gravity = gravity;
            _angularVelocity = angularVelocity;
            _endScale = endScale;
        }

        private void Update()
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - _createdAt) / _duration);
            float deltaTime = Time.unscaledDeltaTime;
            _velocity.y -= _gravity * deltaTime;
            transform.position += new Vector3(_velocity.x, _velocity.y, 0f) * deltaTime;
            transform.Rotate(0f, 0f, _angularVelocity * deltaTime);
            transform.localScale = Vector3.Lerp(_startScale, _startScale * _endScale, progress);
            Color color = _renderer.color;
            color.a = 1f - progress;
            _renderer.color = color;

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
