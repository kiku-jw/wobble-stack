using UnityEngine;

namespace WobbleStack.Runtime
{
    internal sealed class RoutePickup : MonoBehaviour
    {
        private WobbleStackGame _game;
        private Vector3 _restPosition;
        private float _phase;
        private bool _collected;
        private int _routeIndex;

        public int BadgeId { get; private set; }

        public bool IsCollected => _collected;

        public float HorizontalMotionAmplitude => _routeIndex == 1
            ? 0.48f
            : _routeIndex == 2
                ? 0.72f
                : 0f;

        public float VerticalMotionAmplitude => _routeIndex == 2
            ? 0.34f
            : _routeIndex == 1
                ? 0.18f
                : 0.12f;

        public void Initialize(WobbleStackGame game, int badgeId, int routeIndex)
        {
            _game = game;
            BadgeId = badgeId;
            _routeIndex = routeIndex;
            _restPosition = transform.localPosition;
            _phase = badgeId * 0.91f;
        }

        private void Update()
        {
            if (_collected)
            {
                return;
            }

            float time = Time.unscaledTime;
            float horizontalOffset = Mathf.Sin(
                (time * (_routeIndex == 2 ? 1.28f : 1.05f)) +
                _phase) * HorizontalMotionAmplitude;
            float verticalOffset = Mathf.Sin(
                (time * (_routeIndex == 2 ? 2.56f : 2.4f)) +
                (_phase * 1.37f)) * VerticalMotionAmplitude;
            transform.localPosition = _restPosition + new Vector3(
                horizontalOffset,
                verticalOffset,
                0f);
            transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Sin((time * 1.65f) + _phase) * 7f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || other.GetComponent<CreatureBody>() == null)
            {
                return;
            }

            Collect();
        }

        private void Collect()
        {
            if (_collected)
            {
                return;
            }

            _collected = true;
            _game.CollectRouteBadge(BadgeId, transform.position);
            gameObject.SetActive(false);
        }
    }
}
