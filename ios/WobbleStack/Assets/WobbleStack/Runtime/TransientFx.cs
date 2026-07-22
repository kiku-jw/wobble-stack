using UnityEngine;

namespace WobbleStack.Runtime
{
    internal sealed class TransientFx : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Vector3 _startScale;
        private float _createdAt;
        private float _duration;
        private Vector2 _velocity;

        public void Initialize(float duration, Vector2 velocity)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _startScale = transform.localScale;
            _createdAt = Time.unscaledTime;
            _duration = duration;
            _velocity = velocity;
        }

        private void Update()
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - _createdAt) / _duration);
            transform.position += new Vector3(_velocity.x, _velocity.y, 0f) * Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(_startScale, _startScale * 1.35f, progress);
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
