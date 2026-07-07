using System;
using System.Collections.Generic;
using System.IO;

namespace Echo.Services
{
    /// <summary>Pluggable persistence backend (memory in tests, files on device, cloud blob upstream).</summary>
    public interface ISaveBackend
    {
        bool Exists(int slot);
        byte[] Read(int slot);
        void Write(int slot, byte[] data);
        void Delete(int slot);
    }

    /// <summary>In-memory backend for tests; counts writes so the autosave debounce can be asserted.</summary>
    public sealed class InMemorySaveBackend : ISaveBackend
    {
        private readonly Dictionary<int, byte[]> _slots = new Dictionary<int, byte[]>();
        public int WriteCount { get; private set; }
        public bool Exists(int slot) => _slots.ContainsKey(slot);
        public byte[] Read(int slot) => _slots.TryGetValue(slot, out var b) ? b : null;
        public void Write(int slot, byte[] data) { _slots[slot] = data; WriteCount++; }
        public void Delete(int slot) => _slots.Remove(slot);
    }

    /// <summary>
    /// File backend with crash-safe atomic writes (temp → replace), per docs/05 §6. Pure System.IO, so it
    /// works in Unity (point it at Application.persistentDataPath) and in headless tests alike.
    /// </summary>
    public sealed class FileSaveBackend : ISaveBackend
    {
        private readonly string _dir;
        public FileSaveBackend(string dir) { _dir = dir; Directory.CreateDirectory(_dir); }
        private string Path(int slot) => System.IO.Path.Combine(_dir, $"save_{slot}.ekv");

        public bool Exists(int slot) => File.Exists(Path(slot));
        public byte[] Read(int slot) => File.Exists(Path(slot)) ? File.ReadAllBytes(Path(slot)) : null;
        public void Delete(int slot) { if (File.Exists(Path(slot))) File.Delete(Path(slot)); }

        public void Write(int slot, byte[] data)
        {
            string final = Path(slot);
            string tmp = final + ".tmp";
            File.WriteAllBytes(tmp, data);          // write to temp
            if (File.Exists(final)) File.Replace(tmp, final, null); // atomic swap
            else File.Move(tmp, final);
        }
    }

    /// <summary>
    /// Local save service with debounced autosave (docs/05 §6, §8). Autosave requests coalesce: many calls
    /// within the interval result in a single write. On a corrupt/missing slot, load degrades to a fresh
    /// profile rather than hard-failing.
    /// </summary>
    public sealed class SaveService : ISaveService
    {
        private readonly ISaveBackend _backend;
        private readonly float _autosaveInterval;
        private bool _dirty;
        private float _sinceRequest;
        private int _slot;

        public SaveProfile Current { get; private set; }
        public event Action<SaveProfile> OnLoaded;

        public SaveService(ISaveBackend backend, float autosaveIntervalSeconds = 30f)
        {
            _backend = backend;
            _autosaveInterval = autosaveIntervalSeconds;
            Current = new SaveProfile();
        }

        public bool HasSave(int slot) => _backend.Exists(slot);
        public void DeleteSlot(int slot) => _backend.Delete(slot);

        public void Load(int slot)
        {
            _slot = slot;
            byte[] blob = _backend.Read(slot);
            Current = (blob != null && SaveProfileCodec.TryDecode(blob, out var p))
                ? p
                : NewProfile();
            _dirty = false;
            OnLoaded?.Invoke(Current);
        }

        public void Save()
        {
            _backend.Write(_slot, SaveProfileCodec.Encode(Current));
            _dirty = false;
            _sinceRequest = 0f;
        }

        public void RequestAutosave() => _dirty = true;

        /// <summary>Pump from the game loop. Coalesced writes happen at most once per interval while dirty.</summary>
        public void Pump(float deltaSeconds)
        {
            if (!_dirty) return;
            _sinceRequest += deltaSeconds;
            if (_sinceRequest >= _autosaveInterval) Save();
        }

        private static SaveProfile NewProfile()
        {
            var rng = new Random();
            return new SaveProfile { SaveSeed = ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next() };
        }
    }
}
