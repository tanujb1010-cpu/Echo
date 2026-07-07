using Echo.Core.Determinism;
using Echo.Core.Sim;

namespace Echo.Gameplay.Components
{
    /// <summary>
    /// A gate driven by linked plates. A normal door is solid when closed and passable when its plate is
    /// pressed. An <see cref="Invert"/>ed door is a PHASE PLATFORM (mechanic #13): solid only WHILE a plate
    /// is held — so a past self on a plate materializes a bridge for the live self. Backed by a
    /// <see cref="SimEntity"/> so it uses the same deterministic collision as everything else.
    /// </summary>
    public sealed class Door
    {
        public readonly int LinkId;
        public readonly SimEntity Body;
        public readonly bool Invert;
        public bool Active { get; private set; } // door "open" (non-invert) OR platform "present" (invert)

        public Door(int linkId, SimEntity body, bool invert = false)
        {
            LinkId = linkId;
            Body = body;
            Invert = invert;
            SetActivated(false);
        }

        /// <summary>True when the linked plate(s) are pressed. Non-invert → open/non-solid; invert → solid platform.</summary>
        public void SetActivated(bool activated)
        {
            Active = activated;
            Body.SolidToEntities = Invert ? activated : !activated;
        }

        /// <summary>Convenience: is this gate currently passable? (Door open, or platform absent.)</summary>
        public bool Open => Invert ? !Active : Active;

        public void ContributeHash(ref StateHash h)
        {
            h.Add(Active);
            Body.ContributeHash(ref h);
        }
    }
}
