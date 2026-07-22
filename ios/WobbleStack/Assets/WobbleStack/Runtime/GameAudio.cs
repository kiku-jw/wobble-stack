using System.Collections.Generic;
using UnityEngine;

namespace WobbleStack.Runtime
{
    internal sealed class GameAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private readonly List<AudioClip> _clips = new List<AudioClip>();
        private AudioSource _oneShot;
        private AudioSource _wind;
        private AudioClip _startClip;
        private AudioClip _clickClip;
        private AudioClip _saveClip;
        private AudioClip _impactClip;

        public void Build()
        {
            _oneShot = gameObject.AddComponent<AudioSource>();
            _oneShot.playOnAwake = false;
            _oneShot.spatialBlend = 0f;
            _oneShot.volume = 0.62f;
            _oneShot.ignoreListenerPause = true;

            _wind = gameObject.AddComponent<AudioSource>();
            _wind.playOnAwake = false;
            _wind.loop = true;
            _wind.spatialBlend = 0f;
            _wind.volume = 0f;
            _wind.clip = CreateWindLoop();
            _wind.Play();

            _startClip = CreateTone("Start Pop", 0.22f, 280f, 520f, 0.34f);
            _clickClip = CreateTone("Toy Click", 0.08f, 440f, 390f, 0.18f);
            _saveClip = CreateTone("Save Chime", 0.34f, 520f, 920f, 0.26f);
            _impactClip = CreateImpact();
        }

        public void SetWind(float intensity)
        {
            float bounded = Mathf.Clamp01(intensity);
            _wind.volume = Mathf.Lerp(0f, 0.22f, bounded);
            _wind.pitch = Mathf.Lerp(0.82f, 1.18f, bounded);
        }

        public void PlayStart()
        {
            _oneShot.PlayOneShot(_startClip, 0.85f);
        }

        public void PlayClick()
        {
            _oneShot.PlayOneShot(_clickClip, 0.7f);
        }

        public void PlaySave()
        {
            _oneShot.PlayOneShot(_saveClip, 0.8f);
        }

        public void PlayImpact()
        {
            _oneShot.PlayOneShot(_impactClip, 0.9f);
        }

        private AudioClip CreateWindLoop()
        {
            int sampleCount = SampleRate * 2;
            float[] samples = new float[sampleCount];
            uint state = 0xA341316Cu;
            float smooth = 0f;
            for (int index = 0; index < sampleCount; index += 1)
            {
                state = (state * 1664525u) + 1013904223u;
                float raw = (System.Convert.ToSingle(state & 65535u) / 32767.5f) - 1f;
                smooth = Mathf.Lerp(smooth, raw, 0.028f);
                float time = index / System.Convert.ToSingle(SampleRate);
                float air = Mathf.Sin(time * Mathf.PI * 58f) * 0.025f;
                samples[index] = (smooth * 0.19f) + air;
            }

            return StoreClip("Clay Wind", samples);
        }

        private AudioClip CreateTone(string name, float duration, float startFrequency, float endFrequency, float amplitude)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int index = 0; index < sampleCount; index += 1)
            {
                float progress = index / System.Convert.ToSingle(sampleCount - 1);
                float envelope = Mathf.Sin(progress * Mathf.PI);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += (frequency / SampleRate) * Mathf.PI * 2f;
                float body = Mathf.Sin(phase) + (Mathf.Sin(phase * 2f) * 0.18f);
                samples[index] = body * envelope * amplitude;
            }

            return StoreClip(name, samples);
        }

        private AudioClip CreateImpact()
        {
            int sampleCount = Mathf.CeilToInt(0.28f * SampleRate);
            float[] samples = new float[sampleCount];
            uint state = 0xC8013EA4u;
            for (int index = 0; index < sampleCount; index += 1)
            {
                float progress = index / System.Convert.ToSingle(sampleCount - 1);
                float envelope = (1f - progress) * (1f - progress);
                float time = index / System.Convert.ToSingle(SampleRate);
                state = (state * 1103515245u) + 12345u;
                float noise = (System.Convert.ToSingle(state & 65535u) / 32767.5f) - 1f;
                float thud = Mathf.Sin(time * Mathf.PI * Mathf.Lerp(190f, 82f, progress));
                samples[index] = ((thud * 0.7f) + (noise * 0.22f)) * envelope * 0.52f;
            }

            return StoreClip("Clay Impact", samples);
        }

        private AudioClip StoreClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            _clips.Add(clip);
            return clip;
        }

        private void OnDestroy()
        {
            foreach (AudioClip clip in _clips)
            {
                if (clip != null)
                {
                    Destroy(clip);
                }
            }

            _clips.Clear();
        }
    }
}
