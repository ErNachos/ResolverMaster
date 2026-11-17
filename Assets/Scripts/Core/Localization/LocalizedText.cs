using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.Localization
{
    /// <summary>Keeps a uGUI Text in sync with a key: refreshes when the language changes.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] string key;
        Localizer _localizer;
        Text _text;

        public string Key => key;

        /// <summary>Binds and fills the text at once (works in edit mode too, e.g. for previews).</summary>
        public void Bind(Localizer localizer, string textKey)
        {
            Unbind();
            _localizer = localizer;
            key = textKey;
            _text = GetComponent<Text>();
            _localizer.Changed += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            if (_localizer != null && _text != null) _text.text = _localizer.Get(key);
        }

        void Unbind()
        {
            if (_localizer != null) _localizer.Changed -= Refresh;
            _localizer = null;
        }

        void OnDestroy() => Unbind();
    }
}
