using System;
using System.Collections.Generic;

namespace Echo.Infra
{
    /// <summary>Implemented by pooled objects so the pool can reset them on release.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>
    /// Generic, allocation-free object pool (docs/05 §5, docs/08 §3). Echoes, VFX, audio one-shots,
    /// projectiles and UI toasts all come from pools so a level restart that spawns six Echoes
    /// performs ZERO runtime allocation. Tracks a high-water mark for tuning/analytics.
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Stack<T> _available;
        private int _liveCount;

        public int HighWaterMark { get; private set; }
        public int LiveCount => _liveCount;
        public int AvailableCount => _available.Count;

        public ObjectPool(Func<T> factory, int prewarm = 0, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onRelease = onRelease;
            _available = new Stack<T>(Math.Max(4, prewarm));
            Prewarm(prewarm);
        }

        /// <summary>Pre-instantiate at level load so the hot loop never allocates. Runs the same
        /// despawn hook as <see cref="Release"/> so a prewarmed item starts in its "not yet claimed"
        /// state — e.g. a pooled view starts hidden instead of rendering at its default transform.</summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T item = _factory();
                (item as IPoolable)?.OnDespawned();
                _onRelease?.Invoke(item);
                _available.Push(item);
            }
        }

        public T Get()
        {
            T item = _available.Count > 0 ? _available.Pop() : _factory();
            _liveCount++;
            if (_liveCount > HighWaterMark) HighWaterMark = _liveCount;
            _onGet?.Invoke(item);
            (item as IPoolable)?.OnSpawned();
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            (item as IPoolable)?.OnDespawned();
            _onRelease?.Invoke(item);
            _available.Push(item);
            _liveCount--;
        }

        /// <summary>Drop pooled instances on scene unload (lets GC reclaim; pools are scene-scoped).</summary>
        public void Clear()
        {
            _available.Clear();
            _liveCount = 0;
        }
    }
}
