using UnityEngine;

namespace Echo.Unity
{
    /// <summary>
    /// The first-launch "what is this game" vignette (~40 s, skippable any time): a choreographed
    /// screen-space demo of the one core loop — walk to the plate, the door opens, walk away, it
    /// closes; restart; your Echo repeats the run and holds the door for you. Show, don't tell.
    ///
    /// Pure IMGUI theater over Time — no sim, no scene objects, nothing persisted except the
    /// "seen it" flag the flow layer sets. Captions carry six short beats; squares do the teaching.
    /// </summary>
    public static class IntroCinematic
    {
        private static GUIStyle _caption, _small, _titleStyle;

        private static readonly Color Floor = new Color(0.32f, 0.40f, 0.52f);
        private static readonly Color Plate = new Color(0.30f, 0.85f, 0.75f, 0.85f);
        private static readonly Color Door = new Color(0.75f, 0.78f, 0.85f, 0.95f);
        private static readonly Color ExitC = new Color(0.20f, 0.90f, 0.35f, 0.75f);
        private static readonly Color EchoC = new Color(0.50f, 0.70f, 1.00f, 0.65f);

        private const float End = 40f;

        /// <summary>Draw one frame at elapsed time <paramref name="t"/>. True = finished or skipped.</summary>
        public static bool Draw(float t)
        {
            EnsureStyles();
            float W = UnityEngine.Screen.width, H = UnityEngine.Screen.height;

            // Backdrop: W1's cold slate, so the real game feels continuous with the intro.
            Tint(new Rect(0, 0, W, H), new Color(0.05f, 0.07f, 0.13f));

            float floorY = H * 0.62f;
            float unit = H * 0.045f;                       // body size
            float X(float pct) => W * pct / 100f;          // stage position in percent

            // Stage: floor, plate at 30%, door at 62%, exit glow at 88%.
            Tint(new Rect(0, floorY, W, H * 0.06f), Floor);
            Tint(new Rect(X(27f), floorY - unit * 0.28f, X(6f), unit * 0.28f), Plate);
            bool doorOpen = (t >= 4f && t < 8.5f) || t >= 26f;
            if (!doorOpen) Tint(new Rect(X(61f), floorY - unit * 3.2f, X(2f), unit * 3.2f), Door);
            else Tint(new Rect(X(61f), floorY - unit * 3.2f, X(2f), unit * 3.2f), new Color(Door.r, Door.g, Door.b, 0.14f));
            Tint(new Rect(X(87f), floorY - unit * 3.2f, X(3f), unit * 3.2f), ExitC);

            // The player square's journey.
            float px =
                t < 4f ? Move(t, 0f, 4f, 10f, 30f) :      // walk to the plate
                t < 8f ? 30f :                             // stand — door opens
                t < 12f ? Move(t, 8f, 12f, 30f, 59f) :    // walk away — door slams
                t < 16f ? 59f :                            // stuck at the closed door
                t < 18f ? Move(t, 16f, 18f, 59f, 10f) :   // REWIND
                t < 26f ? 10f :                            // wait while the Echo works
                Move(t, 26f, 32f, 10f, 94f);               // through the held-open door
            bool rewinding = t >= 16f && t < 18f;
            if (rewinding) // rewind afterimages
                for (int i = 1; i <= 3; i++)
                    Tint(BodyRect(X(px + i * 4f), floorY, unit), new Color(0.5f, 0.7f, 1f, 0.18f / i * 3f));
            if (t < 32.5f)
                Tint(BodyRect(X(px), floorY, unit), Color.white);

            // The Echo: appears after the rewind and repeats the exact first walk.
            if (t >= 18f)
            {
                float ex = t < 26f ? Move(t, 18f, 26f, 10f, 30f) : 30f;
                Tint(BodyRect(X(ex), floorY, unit), EchoC);
            }

            // Six caption beats, bottom-center.
            Caption(t, 0.5f, 7.5f, "This is you. The plate holds the door open — while something stands on it.");
            Caption(t, 8.5f, 15.5f, "Walk away and it closes. You cannot be in two places at once.");
            Caption(t, 16f, 25.5f, "So you restart. The run you just made is kept — as an Echo.");
            Caption(t, 26f, 32.5f, "Your Echo repeats your exact run. Past you holds the door for future you.");
            Caption(t, 33f, End - 0.5f, "Echoes are finite. Spend them with intention.");
            if (t >= 33f)
            {
                GUI.Label(new Rect(W / 2f - 200, H * 0.30f, 400, 60), "ECHO", _titleStyle);
                GUI.Label(new Rect(W / 2f - 200, H * 0.30f + 64, 400, 30), "◉ ◉ ◉", _caption);
            }

            GUI.Label(new Rect(W - 260, H - 44, 240, 30), "any key — skip", _small);

            return t > End || (t > 0.75f && Input.anyKeyDown);
        }

        private static Rect BodyRect(float x, float floorY, float unit)
            => new Rect(x - unit * 0.4f, floorY - unit, unit * 0.8f, unit);

        private static float Move(float t, float t0, float t1, float from, float to)
            => Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(t0, t1, t)));

        private static void Caption(float t, float t0, float t1, string text)
        {
            if (t < t0 || t > t1) return;
            float a = Mathf.Min(1f, (t - t0) / 0.4f, (t1 - t) / 0.4f);
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(UnityEngine.Screen.width / 2f - 380, UnityEngine.Screen.height * 0.76f, 760, 60), text, _caption);
            GUI.color = Color.white;
        }

        private static void Tint(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void EnsureStyles()
        {
            if (_caption != null) return;
            _caption = new GUIStyle(GUI.skin.label)
            { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _caption.normal.textColor = new Color(0.92f, 0.94f, 1f);
            _small = new GUIStyle(_caption) { fontSize = 12, alignment = TextAnchor.MiddleRight };
            _small.normal.textColor = new Color(1f, 1f, 1f, 0.45f);
            _titleStyle = new GUIStyle(_caption) { fontSize = 44, fontStyle = FontStyle.Bold };
        }
    }
}
