using UnityEngine;

namespace WobbleStack.Runtime
{
    internal sealed class RoutePickup : MonoBehaviour
    {
        private WobbleStackGame _game;
        private Vector3 _restPosition;
        private float _phase;
        private bool _collected;

        public int BadgeId { get; private set; }

        public bool IsCollected => _collected;

        public void Initialize(WobbleStackGame game, int badgeId)
        {
            _game = game;
            BadgeId = badgeId;
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
            transform.localPosition = _restPosition + new Vector3(
                0f,
                Mathf.Sin((time * 2.4f) + _phase) * 0.12f,
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
