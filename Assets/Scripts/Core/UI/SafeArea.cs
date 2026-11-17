using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Fits its RectTransform to the device safe area (notch, punch hole, gesture bar). Put it on a full-screen child of the screen root:
    /// backgrounds stay outside it (they fill the whole display), interactive content goes inside.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect _lastArea;
        Vector2Int _lastScreen;

        void OnEnable() => ApplyIfChanged(force: true);

        void Update() => ApplyIfChanged(force: false);

        void ApplyIfChanged(bool force)
        {
            var area = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (!force && area == _lastArea && size == _lastScreen) return;
            _lastArea = area;
            _lastScreen = size;
            Apply(area, size);
        }

        /// <summary>Applies an explicit safe area (pixels, origin bottom-left) for a screen of the given size. Used by previews too.</summary>
        public void Apply(Rect safeArea, Vector2 screenSize)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0) return;
            var rt = (RectTransform)transform;
            ComputeAnchors(safeArea, screenSize, out var min, out var max);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void ComputeAnchors(Rect safeArea, Vector2 screenSize, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = new Vector2(Mathf.Clamp01(safeArea.xMin / screenSize.x), Mathf.Clamp01(safeArea.yMin / screenSize.y));
            anchorMax = new Vector2(Mathf.Clamp01(safeArea.xMax / screenSize.x), Mathf.Clamp01(safeArea.yMax / screenSize.y));
        }
    }
}
