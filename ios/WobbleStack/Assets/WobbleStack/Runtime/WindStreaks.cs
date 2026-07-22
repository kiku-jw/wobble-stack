using System.Collections.Generic;
using UnityEngine;

namespace WobbleStack.Runtime
{
    internal sealed class WindStreaks : MonoBehaviour
    {
        private const int StreakCount = 14;
        private readonly List<LineRenderer> _lines = new List<LineRenderer>();
        private readonly List<float> _offsets = new List<float>();
        private readonly List<float> _heights = new List<float>();
        private Material _material;
        private float _intensity;
        private int _direction = 1;

        public void Build()
        {
            _material = new Material(Shader.Find("Sprites/Default"));
            for (int index = 0; index < StreakCount; index += 1)
            {
                GameObject streak = new GameObject($"Wind Streak {index + 1}");
                streak.transform.SetParent(transform, false);
                LineRenderer line = streak.AddComponent<LineRenderer>();
                line.material = _material;
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.sortingOrder = 20;
                line.numCapVertices = 4;
                line.enabled = false;
                _lines.Add(line);
                _offsets.Add(Mathf.Repeat(index * 0.173f, 1f));
                _heights.Add(-2.4f + (Mathf.Repeat(index * 0.317f, 1f) * 9.2f));
            }
        }

        public void SetWind(int direction, float intensity)
        {
            _direction = direction < 0 ? -1 : 1;
            _intensity = Mathf.Clamp01(intensity);
        }

        private void Update()
        {
            Refresh(Time.time);
        }

        public void Refresh(float time)
        {
            float speed = Mathf.Lerp(1.8f, 9.5f, _intensity);
            float visibleThreshold = _intensity * StreakCount;

            for (int index = 0; index < _lines.Count; index += 1)
            {
                LineRenderer line = _lines[index];
                bool visible = _intensity > 0.02f && index < visibleThreshold;
                line.enabled = visible;
                if (!visible)
                {
                    continue;
                }

                float travel = Mathf.Repeat(_offsets[index] + (time * speed * 0.08f), 1f);
                float x = Mathf.Lerp(-7f, 7f, _direction > 0 ? travel : 1f - travel);
                float length = Mathf.Lerp(0.6f, 2.1f, _intensity);
                float width = Mathf.Lerp(0.025f, 0.085f, _intensity);
                float alpha = Mathf.Lerp(0.12f, 0.55f, _intensity);
                line.startWidth = width;
                line.endWidth = width * 0.3f;
                line.startColor = new Color(1f, 0.96f, 0.78f, alpha);
                line.endColor = new Color(1f, 0.96f, 0.78f, 0f);
                Vector3 start = new Vector3(x, _heights[index], -0.2f);
                Vector3 end = start - new Vector3(_direction * length, 0.14f, 0f);
                line.SetPosition(0, start);
                line.SetPosition(1, end);
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
