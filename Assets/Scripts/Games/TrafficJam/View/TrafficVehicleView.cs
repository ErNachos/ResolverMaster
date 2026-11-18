using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ResolverMaster.Games.TrafficJam.View
{
    /// <summary>
    /// One pooled vehicle (TJ-001): an unrotated root sized like the vehicle on the grid (takes the touches) and, inside it, the art drawn
    /// pointing up and turned towards the vehicle's front: wheels, body (tinted), stripes (target cars), details. Touches go to the board,
    /// which owns every rule; the view only shows the vehicle at a position along its line (in cells, fractions while dragging or
    /// sliding) and runs the slide and the shake in real time.
    /// </summary>
    public sealed class TrafficVehicleView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Art;
        public Image Wheels, Body, Stripes, Details;
        public CanvasGroup Group;
        public int Index;
        public Action<TrafficVehicleView, PointerEventData> Pressed, Dragged, Released;

        /// <summary>Shown position along the line (cells), and the slide in progress.</summary>
        public float Shown;
        float _from, _to, _time = -1f, _duration;
        float _shake = -1f;
        Vector2 _base;

        public bool IsSliding => _time >= 0f;
        public bool IsShaking => _shake >= 0f;

        public void SlideTo(float to, float duration)
        {
            _from = Shown;
            _to = to;
            _duration = Mathf.Max(0.0001f, duration);
            _time = 0f;
        }

        public void StopSlide()
        {
            if (_time >= 0f) Shown = _to;
            _time = -1f;
        }

        /// <summary>Advances the slide; true when it ended in this tick.</summary>
        public bool TickSlide(float dt)
        {
            if (_time < 0f) return false;
            _time += dt;
            float u = Mathf.Clamp01(_time / _duration);
            Shown = Mathf.Lerp(_from, _to, 1f - (1f - u) * (1f - u));   // ease out
            if (u < 1f) return false;
            _time = -1f;
            Shown = _to;
            return true;
        }

        /// <summary>Where the root sits when not shaking (the board places it).</summary>
        public void Place(Vector2 anchored)
        {
            _base = anchored;
            ((RectTransform)transform).anchoredPosition = anchored;
        }

        public void StartShake() => _shake = 0f;

        public void StopShake()
        {
            _shake = -1f;
            ((RectTransform)transform).anchoredPosition = _base;
        }

        /// <summary>The Core shake (<see cref="ResolverMaster.Core.UI.ShakeMotion"/>) across the vehicle's line.</summary>
        public void TickShake(float dt, float duration, float amplitude, int cycles, bool horizontal, float cell)
        {
            if (_shake < 0f) return;
            _shake += dt;
            if (_shake >= duration)
            {
                StopShake();
                return;
            }
            float off = ResolverMaster.Core.UI.ShakeMotion.Offset(_shake, duration, amplitude * cell, cycles);
            ((RectTransform)transform).anchoredPosition = _base + (horizontal ? new Vector2(0, off) : new Vector2(off, 0));
        }

        public void OnPointerDown(PointerEventData e) => Pressed?.Invoke(this, e);
        public void OnDrag(PointerEventData e) => Dragged?.Invoke(this, e);
        public void OnPointerUp(PointerEventData e) => Released?.Invoke(this, e);
    }
}
