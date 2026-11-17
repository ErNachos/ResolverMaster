using TMPro;
using UnityEngine;

namespace ResolverMaster.Core.Localization
{
    /// <summary>Keeps a TextMeshPro text in sync with a key: refreshes when the language changes.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] string key;
        Localizer _localizer;
        TMP_Text _text;

        public string Key => key;

        /// <summary>Binds and fills the text at once (works in edit mode too, e.g. for previews).</summary>
        public void Bind(Localizer localizer, string textKey)
        {
            Unbind();
            _localizer = localizer;
            key = textKey;
            _text = GetComponent<TMP_Text>();
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
