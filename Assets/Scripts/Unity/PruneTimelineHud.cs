using UnityEngine;

namespace Echo.Unity
{
    /// <summary>
    /// Minimal prune/timeline panel (docs/08 §3-4): list banked runs (Echoes), let the player drop a bad
    /// recording, then restart the braid without it — the in-level Undo (docs/05 §6). IMGUI for zero setup;
    /// replaced by the UI-Toolkit scrubber in polish. Toggle with Tab.
    /// </summary>
    public sealed class PruneTimelineHud : MonoBehaviour
    {
        [SerializeField] private SimRunner _runner;
        private bool _open;

        private void Awake() { if (_runner == null) _runner = FindFirstObjectByType<SimRunner>(); }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) _open = !_open;
        }

        private void OnGUI()
        {
            if (!_open || _runner == null || _runner.Sim == null) return;
            var sim = _runner.Sim;

            const int w = 320, rowH = 28;
            int count = sim.BankedRunCount;
            var box = new Rect(Screen.width - w - 16, 90, w, 70 + rowH * Mathf.Max(1, count));
            GUI.Box(box, "YOUR ECHOES  (Tab to close)");

            float y = box.y + 28;
            for (int i = 0; i < count; i++)
            {
                GUI.Label(new Rect(box.x + 12, y, 180, 24), $"Run {i + 1}  (Echo)");
                if (GUI.Button(new Rect(box.x + 200, y, 100, 24), "Prune"))
                {
                    sim.PruneBankedRun(i);
                    _runner.DoRestart(); // rebuild the braid without the pruned run
                    return;
                }
                y += rowH;
            }
            if (count == 0) GUI.Label(new Rect(box.x + 12, y, 280, 24), "No banked runs yet.");
        }
    }
}
