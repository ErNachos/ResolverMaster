using System;
using System.Collections.Generic;

namespace ResolverMaster.Core.Localization
{
    /// <summary>
    /// Pure localization logic (no Unity types): holds one table per language and the current language.
    /// A missing key returns the key itself, so the problem is visible on screen, and is remembered in <see cref="MissingKeys"/>.
    /// </summary>
    public sealed class Localizer
    {
        readonly Dictionary<Language, LocalizationTable> _tables = new Dictionary<Language, LocalizationTable>();
        readonly HashSet<string> _missing = new HashSet<string>();

        public Language Current { get; private set; }

        /// <summary>Raised after <see cref="SetLanguage"/> changes the language: every visible text refreshes itself.</summary>
        public event Action Changed;

        public IReadOnlyCollection<string> MissingKeys => _missing;

        public Localizer(Language initial) { Current = initial; }

        public LocalizationTable Table(Language language)
        {
            if (!_tables.TryGetValue(language, out var table))
            {
                table = new LocalizationTable();
                _tables[language] = table;
            }
            return table;
        }

        public void SetLanguage(Language language)
        {
            if (language == Current) return;
            Current = language;
            Changed?.Invoke();
        }

        public bool Has(Language language, string key) => _tables.TryGetValue(language, out var t) && t.TryGet(key, out _);

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_tables.TryGetValue(Current, out var table) && table.TryGet(key, out var value)) return value;
            _missing.Add(key);
            return key;
        }
    }
}
