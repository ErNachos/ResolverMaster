using System.Collections.Generic;

namespace ResolverMaster.Core.Localization
{
    /// <summary>
    /// One language's strings. Text format (UTF-8), one entry per line:
    /// <c>key = value</c>; empty lines and lines starting with '#' are ignored; "\n" in a value becomes a new line.
    /// Several files can be merged into one table (Core file plus one file per minigame).
    /// </summary>
    public sealed class LocalizationTable
    {
        readonly Dictionary<string, string> _entries = new Dictionary<string, string>();
        readonly List<string> _errors = new List<string>();

        public IReadOnlyDictionary<string, string> Entries => _entries;

        /// <summary>Malformed lines and duplicate keys met while parsing (reported by the validator).</summary>
        public IReadOnlyList<string> Errors => _errors;

        public bool TryGet(string key, out string value) => _entries.TryGetValue(key, out value);

        public void Parse(string text, string sourceName)
        {
            if (string.IsNullOrEmpty(text)) return;
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    _errors.Add($"{sourceName}:{i + 1}: missing '=' or key");
                    continue;
                }
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
                if (_entries.ContainsKey(key))
                    _errors.Add($"{sourceName}:{i + 1}: duplicate key '{key}'");
                _entries[key] = value;
            }
        }
    }
}
