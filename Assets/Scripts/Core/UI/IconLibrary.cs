using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Name -> sprite for every icon in Assets/Art/Icons (white masks, tinted by the UI).
    /// Asset: Assets/Resources/ResolverMaster/IconLibrary.asset, rebuilt by Resolver Master > UI Kit > Rebuild Icon Library.
    /// </summary>
    public sealed class IconLibrary : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/IconLibrary";

        /// <summary>Icons the Core UI uses; the validator checks they all exist.</summary>
        public static readonly string[] CoreIcons =
        {
            "back", "settings", "undo", "restart", "help", "star", "lock", "sound", "vibration", "language", "story", "challenge",
        };

        [Serializable]
        public struct Entry
        {
            public string name;
            public Sprite sprite;
        }

        public List<Entry> entries = new List<Entry>();

        Dictionary<string, Sprite> _map;

        public Sprite Get(string iconName)
        {
            if (_map == null)
            {
                _map = new Dictionary<string, Sprite>();
                foreach (var e in entries)
                    if (!string.IsNullOrEmpty(e.name) && e.sprite != null) _map[e.name] = e.sprite;
            }
            return iconName != null && _map.TryGetValue(iconName, out var s) ? s : null;
        }

        public bool Has(string iconName) => Get(iconName) != null;

        /// <summary>Call after editing <see cref="entries"/> from code.</summary>
        public void InvalidateCache() => _map = null;
    }
}
