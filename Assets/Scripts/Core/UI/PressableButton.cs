using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// The "press me" behaviour of the UI kit buttons (ErNa-005, Gemini sprites):
    /// pressed = the whole button goes down by a few units (its sprite through a <see cref="VertexOffset"/>, its children by moving them)
    /// and gets a little darker; released = everything goes back.
    /// <see cref="SetInteractable"/>: disabled = every sprite of the button desaturated (shared material of the style) and the texts
    /// faded; enabled again = the original look.
    /// Everything is applied at once (works in edit mode too, for the previews).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class PressableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Image _image;
        Button _button;
        VertexOffset _offset;
        float _drop, _darken = 0.86f, _contentAlpha = 0.55f;
        Material _disabledMaterial;
        bool _down, _muted;
        Color _restColor;

        public bool IsPressed => _down;
        public bool IsMuted => _muted;

        public void Init(Image image, float drop, float darken, Material disabledMaterial, float disabledContentAlpha)
        {
            _image = image;
            _button = GetComponent<Button>();
            _drop = drop;
            _darken = darken;
            _disabledMaterial = disabledMaterial;
            _contentAlpha = disabledContentAlpha;
            _offset = image.GetComponent<VertexOffset>();
            if (_offset == null) _offset = image.gameObject.AddComponent<VertexOffset>();
        }

        public void SetInteractable(bool on)
        {
            if (_button == null) _button = GetComponent<Button>();
            _button.interactable = on;
            if (!on) Release();
            if (on == !_muted) return;
            _muted = !on;
            foreach (var g in GetComponentsInChildren<Graphic>(true))
            {
                if (g is TMP_Text) g.canvasRenderer.SetAlpha(on ? 1f : _contentAlpha);
                else if (g is Image img && img.sprite != null) img.material = on ? null : _disabledMaterial;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button == null || !_button.IsInteractable() || _down) return;
            _down = true;
            Apply(true);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        void OnDisable() => Release();

        void Release()
        {
            if (!_down) return;
            _down = false;
            Apply(false);
        }

        void Apply(bool pressed)
        {
            float dy = pressed ? -_drop : _drop;
            if (_image != null)
            {
                if (pressed)
                {
                    _restColor = _image.color;
                    _image.color = new Color(_restColor.r * _darken, _restColor.g * _darken, _restColor.b * _darken, _restColor.a);
                }
                else _image.color = _restColor;
            }
            if (_offset != null) _offset.Offset = pressed ? new Vector2(0, -_drop) : Vector2.zero;
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i) is RectTransform child) child.anchoredPosition += new Vector2(0, dy);
        }
    }
}
