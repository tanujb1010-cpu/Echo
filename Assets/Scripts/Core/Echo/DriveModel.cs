using Echo.Core.Determinism;

namespace Echo.Core.Echo
{
    /// <summary>
    /// The five-drive personality substrate of an Echo (docs/04 §3). Each drive is a Fix64 in [0,1].
    /// Drives evolve deterministically from observable signals; the dominant drive shapes which
    /// Divergence Gates can fire. Pure data + clamped mutators — no randomness lives here.
    /// </summary>
    public sealed class DriveModel
    {
        public Fix64 Curiosity;
        public Fix64 Autonomy;
        public Fix64 Attachment;
        public Fix64 SelfPreservation;
        public Fix64 Spite;

        public void Reset()
        {
            Curiosity = Autonomy = Attachment = SelfPreservation = Spite = Fix64.Zero;
        }

        public void Set(Fix64 cur, Fix64 aut, Fix64 att, Fix64 self, Fix64 spite)
        {
            Curiosity = Clamp01(cur); Autonomy = Clamp01(aut); Attachment = Clamp01(att);
            SelfPreservation = Clamp01(self); Spite = Clamp01(spite);
        }

        public static Fix64 Clamp01(Fix64 v) => Fix64.Clamp(v, Fix64.Zero, Fix64.One);

        public enum Drive { Curiosity, Autonomy, Attachment, SelfPreservation, Spite }

        /// <summary>Highest drive (ties resolved by fixed enum order → deterministic).</summary>
        public Drive Dominant()
        {
            Drive best = Drive.Curiosity; Fix64 bestVal = Curiosity;
            if (Autonomy > bestVal) { best = Drive.Autonomy; bestVal = Autonomy; }
            if (Attachment > bestVal) { best = Drive.Attachment; bestVal = Attachment; }
            if (SelfPreservation > bestVal) { best = Drive.SelfPreservation; bestVal = SelfPreservation; }
            if (Spite > bestVal) { best = Drive.Spite; bestVal = Spite; }
            return best;
        }

        public Fix64 ValueOf(Drive d) => d switch
        {
            Drive.Curiosity => Curiosity,
            Drive.Autonomy => Autonomy,
            Drive.Attachment => Attachment,
            Drive.SelfPreservation => SelfPreservation,
            _ => Spite,
        };

        public void ContributeHash(ref StateHash h)
        {
            h.Add(Curiosity); h.Add(Autonomy); h.Add(Attachment); h.Add(SelfPreservation); h.Add(Spite);
        }
    }
}
