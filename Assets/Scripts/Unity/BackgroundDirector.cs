using UnityEngine;

namespace Echo.Unity
{
    /// <summary>
    /// The world behind the world, zero assets: a vertical gradient backdrop, two slow-drifting soft
    /// haze bands (depth layers — the camera is static, so drift at different speeds is what sells
    /// parallax), and a field of ambient motes tinted per world (dust in the cold worlds, embers in
    /// the hot ones). Purely presentation — driven by Time.deltaTime, never touches the sim.
    ///
    /// Honors <see cref="HazardTelegraphView.ReduceMotion"/>: fewer motes, slower drift, no wobble.
    /// </summary>
    public sealed class BackgroundDirector : MonoBehaviour
    {
        private struct Mote
        {
            public Transform T;
            public float Rise, Wobble, Phase, BaseX;
        }

        private WorldPalette.Theme _theme;
        private float _w, _h;
        private Mote[] _motes;
        private Transform[] _bands;
        private float[] _bandSpeed;
        private Camera _cam;
        private Color _prevCamColor;

        private static Sprite _softSprite; // radial-falloff blob, shared across levels

        public static BackgroundDirector Attach(Transform parent, LevelDefinition level, WorldPalette.Theme theme)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(parent, worldPositionStays: false);
            var b = go.AddComponent<BackgroundDirector>();
            b._theme = theme;
            b._w = level.Width;
            b._h = level.Height;
            b.Build();
            return b;
        }

        private void Build()
        {
            // Camera clear color matches the gradient top so the frame edges blend seamlessly.
            _cam = Camera.main;
            if (_cam != null) { _prevCamColor = _cam.backgroundColor; _cam.backgroundColor = _theme.BgTop; }

            BuildGradient();
            BuildBands();
            BuildMotes();
        }

        private void BuildGradient()
        {
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, mipChain: false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 64; y++)
                tex.SetPixel(0, y, Color.Lerp(_theme.BgBottom, _theme.BgTop, y / 63f));
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f);
            var go = new GameObject("Gradient");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position = new Vector3(_w * 0.5f, _h * 0.5f, 30f);
            go.transform.localScale = new Vector3(_w + 40f, (_h + 20f) / 64f, 1f); // overscan past level bounds
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -120;
        }

        private void BuildBands()
        {
            _bands = new Transform[2];
            _bandSpeed = new float[] { 0.25f, 0.55f }; // nearer layer drifts faster → depth
            for (int i = 0; i < _bands.Length; i++)
            {
                var sr = SoftQuad($"Haze{i}", new Color(_theme.Haze.r, _theme.Haze.g, _theme.Haze.b, 0.045f), -110 + i);
                sr.transform.position = new Vector3(_w * (0.3f + 0.4f * i), _h * (0.35f + 0.3f * i), 28f);
                sr.transform.localScale = new Vector3(_w * 0.9f, 4.5f + 3f * i, 1f);
                _bands[i] = sr.transform;
            }
        }

        private void BuildMotes()
        {
            bool calm = HazardTelegraphView.ReduceMotion;
            int count = calm ? 14 : 34;
            _motes = new Mote[count];
            for (int i = 0; i < count; i++)
            {
                float size = Random.Range(0.06f, 0.26f);
                var sr = SoftQuad("Mote", _theme.Mote, -105);
                var c = sr.color; c.a = Random.Range(0.06f, 0.18f); sr.color = c;
                sr.transform.localScale = new Vector3(size, size, 1f);
                sr.transform.position = new Vector3(Random.Range(0f, _w), Random.Range(0f, _h), 26f);
                _motes[i] = new Mote
                {
                    T = sr.transform,
                    Rise = Random.Range(0.15f, 0.6f) * (calm ? 0.4f : 1f),
                    Wobble = calm ? 0f : Random.Range(0.2f, 0.7f),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    BaseX = sr.transform.position.x,
                };
            }
        }

        private SpriteRenderer SoftQuad(string name, Color color, int order)
        {
            if (_softSprite == null)
            {
                const int S = 64;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, mipChain: false)
                { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(S / 2f, S / 2f)) / (S / 2f);
                        float a = Mathf.Clamp01(1f - d); a *= a; // soft radial falloff
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                tex.Apply();
                _softSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), pixelsPerUnit: S);
            }
            var go = new GameObject(name);
            go.transform.SetParent(transform, worldPositionStays: false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _softSprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;

            for (int i = 0; i < _bands.Length; i++)
            {
                Vector3 p = _bands[i].position;
                p.x += _bandSpeed[i] * dt * (HazardTelegraphView.ReduceMotion ? 0.4f : 1f);
                if (p.x > _w + _w * 0.5f) p.x = -_w * 0.5f; // wrap around the level
                _bands[i].position = p;
            }

            for (int i = 0; i < _motes.Length; i++)
            {
                Mote m = _motes[i];
                Vector3 p = m.T.position;
                p.y += m.Rise * dt;
                p.x = m.BaseX + Mathf.Sin(t * m.Wobble + m.Phase) * 0.8f;
                if (p.y > _h + 1f) { p.y = -1f; m.BaseX = Random.Range(0f, _w); _motes[i] = m; }
                m.T.position = p;
            }
        }

        private void OnDestroy()
        {
            if (_cam != null) _cam.backgroundColor = _prevCamColor; // menus keep their own backdrop
        }
    }
}
