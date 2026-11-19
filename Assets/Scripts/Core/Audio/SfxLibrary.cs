using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResolverMaster.Core.Audio
{
    /// <summary>
    /// ErNa-012: the clips of the generated effects (Assets/Audio/Sfx), by file name: Resources/Audio/SfxLibrary.asset, rebuilt by
    /// Resolver Master &gt; Audio &gt; Importa suoni. The game reads it once; a missing clip is silence.
    /// </summary>
    public sealed class SfxLibrary : ScriptableObject
    {
        public const string ResourcesPath = "Audio/SfxLibrary";

        [Serializable]
        public struct Entry
        {
            public string name;
            public AudioClip clip;
        }

        public List<Entry> entries = new List<Entry>();

        Dictionary<string, AudioClip> _map;

        public AudioClip Get(string file)
        {
            if (_map == null)
            {
                _map = new Dictionary<string, AudioClip>();
                foreach (var e in entries)
                    if (!string.IsNullOrEmpty(e.name) && e.clip != null) _map[e.name] = e.clip;
            }
            return file != null && _map.TryGetValue(file, out var c) ? c : null;
        }

        void OnValidate() => _map = null;
    }
}
