using System;
using ResolverMaster.Core.Puzzle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// PG-001: an invisible area that turns finger movements into swipes (Core: any game moved by swipes). One swipe per touch, as soon
    /// as the finger has moved farther than <see cref="Threshold"/> (canvas units); shorter movements are taps and do nothing. The
    /// direction comes from the pure <see cref="SwipeGesture"/>. Draws nothing (it only takes the touches of its rect).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SwipeArea : Graphic, IPointerDownHandler, IDragHandler, IPointerUpHandler, IBeginDragHandler
    {
        /// <summary>A swipe: direction 0 up, 1 right, 2 down, 3 left.</summary>
        public event Action<int> Swiped;

        /// <summary>Distance (canvas units) from which a movement is a swipe.</summary>
        public Func<float> Threshold = () => 60f;

        int _pointer = int.MinValue;
        Vector2 _start;
        bool _fired;

        /// <summary>Adds an invisible swipe area stretched over <paramref name="parent"/> (on top of its other children).</summary>
        public static SwipeArea Create(Transform parent, string name = "SwipeArea")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)) { layer = UiFactory.UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            UiFactory.Stretch(rt);
            var area = go.AddComponent<SwipeArea>();
            area.color = new Color(0, 0, 0, 0);
            area.raycastTarget = true;
            return area;
        }

        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointer != int.MinValue) return;
            _pointer = e.pointerId;
            _fired = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out _start);
        }

        public void OnBeginDrag(PointerEventData e) { }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointer || _fired) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out var now);
            int dir = SwipeGesture.Direction(now.x - _start.x, now.y - _start.y, Mathf.Max(1f, Threshold()));
            if (dir < 0) return;
            _fired = true;
            Swiped?.Invoke(dir);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            if (!_fired)
            {
                // a quick flick may end before a drag event: check the release point too
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out var now);
                int dir = SwipeGesture.Direction(now.x - _start.x, now.y - _start.y, Mathf.Max(1f, Threshold()));
                if (dir >= 0) Swiped?.Invoke(dir);
            }
            _pointer = int.MinValue;
            _fired = false;
        }
    }
}
