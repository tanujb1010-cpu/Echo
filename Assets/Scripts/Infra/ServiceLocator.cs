using System;
using System.Collections.Generic;

namespace Echo.Infra
{
    /// <summary>
    /// Tiny explicit service registry wired once at the composition root (GameBootstrap).
    /// Preferred over scattered singletons: platform services (save, cloud, achievements, analytics,
    /// audio) are registered behind interfaces and can be swapped for fakes in tests (docs/06 §5).
    /// </summary>
    public sealed class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
        }

        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object svc)) return (T)svc;
            throw new InvalidOperationException($"Service not registered: {typeof(T).Name}");
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object svc)) { service = (T)svc; return true; }
            service = null;
            return false;
        }

        public void Clear() => _services.Clear();
    }
}
