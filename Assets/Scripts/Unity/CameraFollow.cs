using UnityEngine;

namespace Echo.Unity
{
    /// <summary>
    /// Keeps the camera centered on the live player so a level's far side (a ledge, a distant plate) stays
    /// reachable in view without manual scrolling. Presentation-only — reads <see cref="SimRunner"/>, never
    /// influences the sim.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private SimRunner _runner;
        [SerializeField] private float _smoothTime = 0.15f;
        [SerializeField] private float _fixedZ = -10f;

        private Vector3 _velocity;

        private void Awake()
        {
            if (_runner == null) _runner = FindFirstObjectByType<SimRunner>();
        }

        private void LateUpdate()
        {
            if (_runner == null || _runner.Sim == null) return;
            var p = _runner.Sim.PlayerPosition;
            var target = new Vector3(p.X.ToFloat(), p.Y.ToFloat(), _fixedZ);
            transform.position = Vector3.SmoothDamp(transform.position, target, ref _velocity, _smoothTime);
        }
    }
}
