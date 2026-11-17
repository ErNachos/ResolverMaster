using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// The "press me" behaviour of the UI kit buttons: while the finger is down the sprite switches to its pressed variant (smaller
    /// bottom band, the face goes down) and every child (label, icon) moves down by the same amount; on release everything goes back.
    /// <see cref="SetInteractable"/> mutes a disabled button: background towards grey, label and icon faded (restored when enabled again;
    /// a color set by the screen while disabled is replaced on enabling).
    /// Everything is applied at once (works in edit mode too, for the previews).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class PressableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Image _image;
        Button _button;
        Sprite _normal, _pressed;
        float _drop;
        float _contentAlpha = 0.45f, _desaturate = 0.7f;
        bool _down, _muted;
        Color _enabledColor;

        public bool IsPressed => _down;

        public void Init(Image image, Sprite normal, Sprite pressed, float drop, float disabledContentAlpha, float disabledDesaturate)
        {
            _image = image;
            _button = GetComponent<Button>();
            _normal = normal;
            _pressed = pressed;
            _drop = drop;
            _contentAlpha = disabledContentAlpha;
            _desaturate = disabledDesaturate;
        }

        public void SetInteractable(bool on)
        {
            if (_button == null) _button = GetComponent<Button>();
            _button.interactable = on;
            if (!on) Release();
            // muted colors: the background goes towards a light grey of the same brightness
            if (!on && !_muted && _image != null)
            {
                _enabledColor = _image.color;
                float l = 0.3f * _enabledColor.r + 0.59f * _enabledColor.g + 0.11f * _enabledColor.b;
                var grey = new Color(Mathf.Lerp(l, 1f, 0.35f), Mathf.Lerp(l, 1f, 0.35f), Mathf.Lerp(l, 1f, 0.35f), _enabledColor.a);
                _image.color = Color.Lerp(_enabledColor, grey, _desaturate);
                _muted = true;
            }
            else if (on && _muted && _image != null)
            {
                _image.color = _enabledColor;
                _muted = false;
            }
            foreach (var g in GetComponentsInChildren<Graphic>(true))
                if (g != _image) g.canvasRenderer.SetAlpha(on ? 1f : _contentAlpha);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button == null || !_button.IsInteractable() || _down) return;
            _down = true;
            Apply(-_drop);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        void OnDisable() => Release();

        void Release()
        {
            if (!_down) return;
            _down = false;
            Apply(_drop);
        }

        void Apply(float dy)
        {
            if (_image != null && _pressed != null) _image.sprite = dy < 0 ? _pressed : _normal;
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i) is RectTransform child) child.anchoredPosition += new Vector2(0, dy);
        }
    }
}
