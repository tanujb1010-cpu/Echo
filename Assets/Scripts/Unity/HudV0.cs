using System.Text;
using UnityEngine;

namespace Echo.Unity
{
    /// <summary>
    /// Minimal in-game HUD (IMGUI so it needs no scene setup): the braid size and the restart prompt —
    /// the two things a new player must always see (docs/08 §2). Replaced by the UI Toolkit HUD in polish.
    /// </summary>
    public sealed class HudV0 : MonoBehaviour
    {
        [SerializeField] private SimRunner _runner;
        [SerializeField] private int _maxEchoes = 6;
        private GUIStyle _style;

        private void Awake()
        {
            if (_runner == null) _runner = FindFirstObjectByType<SimRunner>();
        }

        private void OnGUI()
        {
            if (_runner == null) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };

            int echoes = _runner.EchoCount;
            var sb = new StringBuilder();
            for (int i = 0; i < _maxEchoes; i++) sb.Append(i < echoes ? "◉" : "○"); // ◉ / ○

            GUI.Label(new Rect(16, 12, 600, 28), $"ECHOES  {sb}  ({echoes}/{_maxEchoes})", _style);
            GUI.Label(new Rect(16, 40, 600, 24), "Hold R to restart — your run replays as an Echo.");
        }
    }
}
