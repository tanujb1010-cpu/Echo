using UnityEngine;
using Echo.Core.Echo;
using Echo.Infra;

namespace Echo.Unity
{
    /// <summary>
    /// Presentation-only view of one simulated body (player or Echo). Holds the previous and current
    /// fixed-tick positions and interpolates between them each rendered frame, so a 60 Hz deterministic
    /// sim looks smooth at any refresh rate (docs/05 §3 — render/sim decoupling). Pooled (IPoolable).
    ///
    /// This object NEVER feeds back into the simulation; it only reads BodyRender snapshots.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SimView : MonoBehaviour, IPoolable
    {
        private SpriteRenderer _sr;
        private Vector3 _prev, _curr;
        private int _facing = 1;
        private bool _isPlayer;
        private Trait _trait;
        private float _salience;
        private GateType _lastGate;
        private float _gateFlash; // brief telegraph flash on defiant gates

        /// <summary>True when this view renders the live player (read by JuiceDirector for trail tint).</summary>
        public bool IsPlayer => _isPlayer;

        private void Awake() => _sr = GetComponent<SpriteRenderer>();

        public void PushSnapshot(Vector3 pos, int facing, bool isPlayer, Trait trait, float salience, GateType gate, bool firstFrame)
        {
            _prev = firstFrame ? pos : _curr;
            _curr = pos;
            _facing = facing;
            _isPlayer = isPlayer;
            _trait = trait;
            _salience = salience;
            if (gate == GateType.Refuse || gate == GateType.Sabotage) _gateFlash = 0.25f;
            _lastGate = gate;
        }

        /// <summary>Interpolate toward the current tick (alpha = fraction into the current sim step).</summary>
        public void Render(float alpha, float dt)
        {
            transform.position = Vector3.Lerp(_prev, _curr, alpha);

            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (_facing < 0 ? -1f : 1f);
            transform.localScale = s;

            if (_gateFlash > 0f) _gateFlash = Mathf.Max(0f, _gateFlash - dt);
            _sr.color = ColorFor();
        }

        private Color ColorFor()
        {
            if (_isPlayer) return Color.white;
            // Echoes: cool by default, brightening with salience; trait adds an accent; defiance flashes red.
            Color baseCol = Color.Lerp(new Color(0.45f, 0.65f, 0.95f), new Color(0.65f, 0.9f, 1f), _salience);
            Color accent = _trait switch
            {
                Trait.Stubborn => new Color(0.95f, 0.8f, 0.4f),
                Trait.Trickster => new Color(0.9f, 0.45f, 0.9f),
                Trait.Curious => new Color(0.5f, 0.95f, 0.7f),
                Trait.Skittish => new Color(0.7f, 0.7f, 0.95f),
                _ => baseCol,
            };
            Color c = Color.Lerp(baseCol, accent, 0.5f * _salience);
            c = Color.Lerp(c, Color.red, _gateFlash * 2f);
            c.a = Mathf.Lerp(0.55f, 0.95f, _salience); // higher salience = more "present"
            return c;
        }

        public void OnSpawned() => gameObject.SetActive(true);
        public void OnDespawned() { _gateFlash = 0f; gameObject.SetActive(false); }
    }
}
