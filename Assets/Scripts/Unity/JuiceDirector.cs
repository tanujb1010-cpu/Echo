using System.Collections.Generic;
using UnityEngine;

namespace Echo.Unity
{
    /// <summary>
    /// Presentation-only game feel, zero assets, zero sim impact: camera shake on death, a particle
    /// burst where the run ended, landing squash-and-stretch on the player view, and a fading
    /// afterimage trail while the player moves fast. All of it reads SimRunner events/state and
    /// touches only view transforms and throwaway sprites — nothing here can desync a replay.
    ///
    /// Honors <see cref="HazardTelegraphView.ReduceMotion"/>: shake and afterimages are suppressed,
    /// squash is kept subtle (it reads as landing feedback, not motion).
    /// </summary>
    public sealed class JuiceDirector : MonoBehaviour
    {
        private struct Particle
        {
            public Transform T;
            public SpriteRenderer Sr;
            public Vector3 Vel;
            public float Life, MaxLife;
        }

        private SimRunner _runner;
        private Transform _cam;
        private Vector3 _camBase;
        private float _shake;

        private float _squash;                 // 0..1, decays; scales the player view
        private Vector3 _viewBaseScale = new Vector3(0.8f, 0.9f, 1f);

        private Sprite _sprite;
        private readonly List<Particle> _live = new List<Particle>(64);
        private readonly Stack<(Transform, SpriteRenderer)> _pool = new Stack<(Transform, SpriteRenderer)>();
        private float _trailClock;
        private readonly List<SimView> _viewScratch = new List<SimView>(8);
        private readonly Dictionary<Transform, Vector3> _lastPos = new Dictionary<Transform, Vector3>();

        private static readonly Color PlayerTrail = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color EchoTrail = new Color(0.5f, 0.7f, 1f, 0.13f);

        public static JuiceDirector Attach(SimRunner runner)
        {
            var j = runner.gameObject.AddComponent<JuiceDirector>();
            j._runner = runner;
            j._cam = Camera.main != null ? Camera.main.transform : null;
            if (j._cam != null) j._camBase = j._cam.localPosition;

            runner.OnPlayerDied += r => j.OnDeath();
            runner.OnPlayerLanded += () => j._squash = 1f;
            runner.OnLevelComplete += r => j.Burst(r.PlayerViewTransform, 18, new Color(0.7f, 1f, 0.8f), 5f);
            return j;
        }

        private void OnDeath()
        {
            if (!HazardTelegraphView.ReduceMotion) _shake = 0.45f;
            Burst(_runner.PlayerViewTransform, 24, new Color(1f, 0.45f, 0.35f), 7f);
        }

        /// <summary>External celebration hook (e.g., "a new Echo just banked" burst at the spawn point).</summary>
        public void BurstAtPlayer(int count, Color color, float speed)
            => Burst(_runner != null ? _runner.PlayerViewTransform : null, count, color, speed);

        private void Burst(Transform at, int count, Color color, float speed)
        {
            if (at == null) return;
            for (int i = 0; i < count; i++)
            {
                var (t, sr) = _pool.Count > 0 ? _pool.Pop() : MakeQuad();
                t.gameObject.SetActive(true);
                t.position = at.position;
                float size = Random.Range(0.08f, 0.22f);
                t.localScale = new Vector3(size, size, 1f);
                sr.color = color;
                float a = Random.Range(0f, Mathf.PI * 2f);
                float v = Random.Range(0.3f, 1f) * speed;
                float life = Random.Range(0.25f, 0.6f);
                _live.Add(new Particle
                {
                    T = t, Sr = sr,
                    Vel = new Vector3(Mathf.Cos(a) * v, Mathf.Sin(a) * v + 2f, 0f),
                    Life = life, MaxLife = life,
                });
            }
        }

        private (Transform, SpriteRenderer) MakeQuad()
        {
            if (_sprite == null)
            {
                var tex = Texture2D.whiteTexture;
                _sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            var go = new GameObject("JuiceParticle");
            go.transform.SetParent(transform, worldPositionStays: true);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.sortingOrder = 10;
            return (go.transform, sr);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Particles: simple ballistic + fade, recycled through the pool.
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var p = _live[i];
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    p.T.gameObject.SetActive(false);
                    _pool.Push((p.T, p.Sr));
                    _live.RemoveAt(i);
                    continue;
                }
                p.Vel += Vector3.down * (12f * dt);
                p.T.position += p.Vel * dt;
                var c = p.Sr.color; c.a = p.Life / p.MaxLife; p.Sr.color = c;
                _live[i] = p;
            }

            // Afterimage trails: fading stamps of every fast-moving body — the player in white, each
            // Echo in ghost blue — so the whole braid reads as motion, and replays are legible.
            if (_runner != null && !HazardTelegraphView.ReduceMotion)
            {
                _trailClock -= dt;
                bool stampFrame = _trailClock <= 0f;
                if (stampFrame) _trailClock = 0.07f;

                _runner.CollectViews(_viewScratch);
                for (int i = 0; i < _viewScratch.Count; i++)
                {
                    Transform vt = _viewScratch[i].transform;
                    bool tracked = _lastPos.TryGetValue(vt, out Vector3 last);
                    if (stampFrame && tracked && (vt.position - last).magnitude > 4f * dt) // actually traveling
                    {
                        var (t, sr) = _pool.Count > 0 ? _pool.Pop() : MakeQuad();
                        t.gameObject.SetActive(true);
                        t.position = vt.position;
                        t.localScale = vt.localScale;
                        sr.color = _viewScratch[i].IsPlayer ? PlayerTrail : EchoTrail;
                        _live.Add(new Particle { T = t, Sr = sr, Vel = Vector3.zero, Life = 0.22f, MaxLife = 0.22f });
                    }
                    _lastPos[vt] = vt.position;
                }
            }

            // Landing squash: wide-and-flat snapping back to the body's base proportions.
            var view = _runner != null ? _runner.PlayerViewTransform : null;
            if (view != null && _squash > 0f)
            {
                _squash = Mathf.Max(0f, _squash - dt * 6f);
                float k = HazardTelegraphView.ReduceMotion ? 0.5f * _squash : _squash;
                float sx = Mathf.Sign(view.localScale.x); // preserve facing flip from SimView
                view.localScale = new Vector3(
                    sx * _viewBaseScale.x * (1f + 0.30f * k),
                    _viewBaseScale.y * (1f - 0.28f * k),
                    1f);
            }
        }

        private void LateUpdate()
        {
            if (_cam == null) return;
            if (_shake > 0f)
            {
                _shake = Mathf.Max(0f, _shake - Time.deltaTime * 1.6f);
                float amp = 0.35f * _shake * _shake;
                _cam.localPosition = _camBase + (Vector3)(Random.insideUnitCircle * amp);
            }
            else _cam.localPosition = _camBase;
        }

        private void OnDestroy()
        {
            if (_cam != null) _cam.localPosition = _camBase;
        }
    }
}
