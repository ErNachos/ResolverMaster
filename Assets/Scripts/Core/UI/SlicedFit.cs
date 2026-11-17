using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// For 9-sliced pills (Gemini buttons): scales the borders so the sprite always fills the height of its rect exactly
    /// (pixelsPerUnitMultiplier = sprite height / rect height): the round ends keep their shape and only the middle stretches
    /// sideways. Never scales up: a rect taller than the sprite keeps the sprite size (the round ends would blur).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public sealed class SlicedFit : MonoBehaviour
    {
        Image _image;

        void OnEnable() => Fit();

        void OnRectTransformDimensionsChange() => Fit();

        public void Fit()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null || _image.sprite == null) return;
            float h = ((RectTransform)transform).rect.height;
            if (h <= 0.5f) return;
            float spriteH = _image.sprite.rect.height / Mathf.Max(0.01f, _image.sprite.pixelsPerUnit / 100f);
            _image.pixelsPerUnitMultiplier = Mathf.Max(1f, spriteH / h);
        }
    }
}
