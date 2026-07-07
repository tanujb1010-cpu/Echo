using System.Collections.Generic;
using UnityEngine;

namespace Echo.Unity
{
    /// <summary>
    /// Zero-asset ambient music, sibling of <see cref="AudioDirector"/>: one slowly-breathing synth pad
    /// per mood (menu, each world, ending), baked to a seamless PCM loop the first time it's requested.
    ///
    /// Seamlessness: every partial and LFO frequency is quantized to a whole number of cycles per loop,
    /// so sample[n-1] flows into sample[0] with no click and no crossfade trickery. Two AudioSources
    /// crossfade between moods over a few seconds.
    ///
    /// PRODUCTION: swap baked pads for authored tracks behind the same PlayMood call; nothing else changes.
    /// </summary>
    public sealed class MusicDirector : MonoBehaviour
    {
        public const int MoodMenu = 0;    // worlds are 1..6
        public const int MoodEnding = 7;

        private const int SampleRate = 22050;
        private const float LoopSeconds = 12f;
        private const float MasterVolume = 0.30f;
        private const float FadePerSecond = 0.4f;

        /// <summary>SettingsBlock.MusicOn — when false both sources fade to silence but keep looping,
        /// so re-enabling resumes mid-phrase instead of restarting the pad.</summary>
        public bool Enabled = true;

        private AudioSource _a, _b;
        private bool _aActive = true;
        private int _mood = -1;
        private readonly Dictionary<int, AudioClip> _pads = new Dictionary<int, AudioClip>();

        public static MusicDirector Attach(GameObject host)
        {
            var dir = host.GetComponent<MusicDirector>();
            if (dir == null) dir = host.AddComponent<MusicDirector>();
            return dir;
        }

        private void Awake()
        {
            _a = MakeSource();
            _b = MakeSource();
        }

        private AudioSource MakeSource()
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.volume = 0f;
            return src;
        }

        public void PlayMenu() => PlayMood(MoodMenu);
        public void PlayWorld(int world) => PlayMood(Mathf.Clamp(world, 1, 6));
        public void PlayEnding() => PlayMood(MoodEnding);

        public void PlayMood(int mood)
        {
            if (mood == _mood) return;
            _mood = mood;

            _aActive = !_aActive; // the incoming pad takes the idle source
            var incoming = _aActive ? _a : _b;
            incoming.clip = GetPad(mood);
            incoming.Play();
        }

        private void Update()
        {
            float targetA = Enabled && _aActive ? MasterVolume : 0f;
            float targetB = Enabled && !_aActive ? MasterVolume : 0f;
            _a.volume = Mathf.MoveTowards(_a.volume, targetA, FadePerSecond * Time.deltaTime);
            _b.volume = Mathf.MoveTowards(_b.volume, targetB, FadePerSecond * Time.deltaTime);
            if (_a.volume <= 0f && !_aActive && _a.isPlaying) _a.Stop();
            if (_b.volume <= 0f && _aActive && _b.isPlaying) _b.Stop();
        }

        // ------------------------------------------------------------------ synthesis

        /// <summary>Chord voicings, low register, chosen to track each world's narrative temperature.</summary>
        private static float[] ChordHz(int mood) => mood switch
        {
            MoodMenu => new[] { 110.00f, 164.81f, 246.94f, 261.63f },  // Am add9 — warm, waiting
            1 => new[] { 65.41f, 98.00f, 130.81f, 164.81f },           // C open — the facility wakes, hopeful
            2 => new[] { 87.31f, 130.81f, 164.81f, 246.94f },          // F lydian — space starts lying
            3 => new[] { 73.42f, 110.00f, 164.81f, 174.61f },          // Dm add9 — stranger verbs
            4 => new[] { 58.27f, 87.31f, 130.81f, 138.59f },           // Bbm add9 — the facility pushes back
            5 => new[] { 82.41f, 123.47f, 185.00f, 220.00f },          // Esus — precision, thin air
            6 => new[] { 55.00f, 82.41f, 116.54f, 130.81f },           // Am b9 — everything, at once
            MoodEnding => new[] { 65.41f, 98.00f, 146.83f, 164.81f },  // C add9 — resolution
            _ => new[] { 110f, 165f, 220f },
        };

        private AudioClip GetPad(int mood)
        {
            if (_pads.TryGetValue(mood, out var clip)) return clip;

            int n = (int)(SampleRate * LoopSeconds);
            var data = new float[n];
            float[] chord = ChordHz(mood);

            for (int k = 0; k < chord.Length; k++)
            {
                // Quantize to whole cycles per loop so the buffer tiles perfectly.
                float hz = Mathf.Round(chord[k] * LoopSeconds) / LoopSeconds;
                float lfoHz = (2 + (k * 2 + mood) % 4) / LoopSeconds; // 2..5 whole LFO cycles per loop
                float phase0 = k * 1.7f;                              // decorrelate the breathing
                float gain = 1f / chord.Length * (k == 0 ? 1.2f : 1f); // root slightly forward

                float w = 2f * Mathf.PI * hz / SampleRate;
                float lw = 2f * Mathf.PI * lfoHz / SampleRate;
                for (int i = 0; i < n; i++)
                {
                    float breathe = 0.55f + 0.45f * Mathf.Sin(lw * i + phase0);
                    data[i] += Mathf.Sin(w * i) * breathe * gain;
                }
            }

            clip = AudioClip.Create($"pad_{mood}", n, 1, SampleRate, stream: false);
            clip.SetData(data, 0);
            _pads[mood] = clip;
            return clip;
        }
    }
}
