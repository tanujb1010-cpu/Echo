using Echo.Core.Determinism;
using Echo.Core.Sim;

namespace Echo.Gameplay.Components
{
    /// <summary>
    /// Marks a crate that a character can grab and carry (Content Bible #3 "Carry Hand-off", #4 mass, etc.).
    /// While carried, the crate follows its carrier and is non-solid; while free, it falls and is solid
    /// (you can stack/stand on it). Deterministic state → reproducible in replays.
    /// </summary>
    public sealed class Carriable : ISimComponent
    {
        public SimEntity Owner { get; set; }   // the crate body
        public int CarrierId = -1;             // -1 = free
        public Fix64Vec2 Home;                 // reset position

        public bool IsCarried => CarrierId != -1;

        public void ContributeHash(ref StateHash h)
        {
            h.Add(CarrierId);
            h.Add(Owner.Position);
            h.Add(Owner.Velocity);
        }
    }
}
