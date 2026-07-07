using System.Collections.Generic;
using UnityEngine;

namespace Echo.Unity
{
    public enum Sfx { Jump, Death, Restart, LevelComplete, Click, EchoSpawn }

    /// <summary>
    /// Zero-asset audio: every clip is synthesized to PCM at startup (short chip-style envelopes), so the
    /// project ships sound with no imported files and no package dependencies. Presentation-only — driven
    /// by SimRunner/GameFlow events, never read by the simulation.
    ///
    /// PRODUCTION: swap clips for authored ones behind the same <see cref="Play"/> call; nothing else changes.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        private const int SampleRate = 22050;

        private AudioSource _source;
        private readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();

        public static AudioDirector Attach(GameObject host)
        {
            var dir = host.GetComponent<AudioDirector>();
            if (dir == null) dir = host.AddComponent<AudioDirector>();
            return dir;
        }

        private void Awake()
        {
            _source = gameObject.GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            _clips[Sfx.Jump] = Sweep("sfx_jump", 220f, 660f, 0.12f, 0.5f);
            _clips[Sfx.Death] = Noise("sfx_death", 0.35f, 0.6f);
            _clips[Sfx.Restart] = Sweep("sfx_restart", 880f, 110f, 0.30f, 0.4f);          // falling: the rewind
            _clips[Sfx.EchoSpawn] = Sweep("sfx_echo", 440f, 550f, 0.15f, 0.25f);          // soft rise: someone joins
            _clips[Sfx.Click] = Sweep("sfx_click", 1200f, 900f, 0.04f, 0.3f);
            _clips[Sfx.LevelComplete] = Arpeggio("sfx_complete", new[] { 392f, 494f, 587f, 784f }, 0.09f, 0.5f);
        }

        public void Play(Sfx id)
        {
            if (_clips.TryGetValue(id, out var clip) && clip != null) _source.PlayOneShot(clip);
        }

        // ------------------------------------------------------------------ synthesis

        /// <summary>Linear pitch sweep with an exponential decay envelope (the classic chip blip).</summary>
        private static AudioClip Sweep(string name, float fromHz, float toHz, float seconds, float gain)
        {
            int n = (int)(SampleRate * seconds);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float hz = Mathf.Lerp(fromHz, toHz, t);
                phase += 2f * Mathf.PI * hz / SampleRate;
                float env = Mathf.Exp(-4f * t);
                data[i] = Mathf.Sign(Mathf.Sin(phase)) * env * gain * 0.6f   // square body
                        + Mathf.Sin(phase) * env * gain * 0.4f;              // sine rounding
            }
            return Bake(name, data);
        }

        /// <summary>Decaying white noise burst (impacts, deaths).</summary>
        private static AudioClip Noise(string name, float seconds, float gain)
        {
            int n = (int)(SampleRate * seconds);
            var data = new float[n];
            var rng = new System.Random(1234);
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                low = Mathf.Lerp(low, white, 0.25f); // cheap low-pass so it thuds instead of hissing
                data[i] = low * Mathf.Exp(-5f * t) * gain;
            }
            return Bake(name, data);
        }

        /// <summary>Rising note ladder (fanfares).</summary>
        private static AudioClip Arpeggio(string name, float[] notesHz, float perNoteSeconds, float gain)
        {
            int per = (int)(SampleRate * perNoteSeconds);
            var data = new float[per * notesHz.Length + SampleRate / 8];
            for (int k = 0; k < notesHz.Length; k++)
            {
                float phase = 0f;
                int tail = k == notesHz.Length - 1 ? SampleRate / 8 : 0; // let the last note ring
                for (int i = 0; i < per + tail; i++)
                {
                    float t = i / (float)(per + tail);
                    phase += 2f * Mathf.PI * notesHz[k] / SampleRate;
                    data[k * per + i] += Mathf.Sin(phase) * Mathf.Exp(-3f * t) * gain;
                }
            }
            return Bake(name, data);
        }

        private static AudioClip Bake(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, stream: false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
