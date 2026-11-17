using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Name -> sprite for every PNG of one art folder. The same class serves every sprite library (no copies, GUIDELINES 1):
    /// icons (Gemini drawings icon_* in Assets/Art/UI, placeholder white masks in Assets/Art/Icons) in Resources/ResolverMaster/IconLibrary.asset, Home card illustrations
    /// (Assets/Art/Cards) in Resources/ResolverMaster/CardLibrary.asset, and the art of a minigame (e.g. Plumber fountains).
    /// Rebuilt by Resolver Master > UI Kit > Rebuild Sprite Libraries.
    /// </summary>
    public sealed class IconLibrary : ScriptableObject
    {
        public const string ResourcesPath = "ResolverMaster/IconLibrary";
        public const string CardsResourcesPath = "ResolverMaster/CardLibrary";

        /// <summary>
        /// Icons the Core UI uses; the validator checks they all exist. icon_* are the Gemini drawings (Assets/Art/UI, full color);
        /// "story" is a placeholder mask (Assets/Art/Icons) until its drawing arrives.
        /// </summary>
        public static readonly string[] CoreIcons =
        {
            "icon_back", "icon_settings", "icon_undo", "icon_restart", "icon_help", "icon_lock", "icon_sound_on", "icon_sound_off",
            "icon_vibration", "icon_language", "icon_bolt", "story",
            // ErNa-008: placeholder masks (rewarded ad, change level, daily streak) and the Gemini calendar of the daily challenge
            "ad_video", "change_level", "streak", "icon_calendar", "icon_trophy",
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
