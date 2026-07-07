using UnityEngine;
using Echo.Infra;
using Echo.Services;

namespace Echo.Unity
{
    /// <summary>
    /// Composition root (docs/06 §5). Wires platform services behind interfaces once, at startup, so
    /// gameplay never references a vendor SDK directly and tests can swap fakes. Analytics defaults to a
    /// privacy-safe no-op until the player opts in.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static ServiceLocator Services { get; private set; }

        private void Awake()
        {
            if (Services != null) { Destroy(gameObject); return; }
            Services = new ServiceLocator();

            // Privacy-first default. A real sink is attached only on explicit opt-in (docs/05 §12).
            Services.Register<IAnalyticsSink>(new NoOpAnalytics());

            // TODO (per-platform, behind interfaces — no gameplay changes required):
            //   Services.Register<ISaveService>(new SaveService(...));            // LZ4 + atomic write
            //   Services.Register<ICloudSaveProvider>(SteamCloud / PlayGames / iCloud);
            //   Services.Register<IAchievementProvider>(Steam / PlayGames / GameCenter);
            //   Services.Register<IAudioService>(new FmodAudioService(...));

            DontDestroyOnLoad(gameObject);
        }
    }
}
