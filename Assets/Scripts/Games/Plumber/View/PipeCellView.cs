using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ResolverMaster.Games.Plumber.View
{
    /// <summary>
    /// One pooled cell: tile background (takes the tap), for a sink the colored halo under it, the pipe image (rotates) and, for a sink,
    /// the water disk that fills it. Rotation is animated at a constant speed towards the target, so taps made during an
    /// animation queue up (one 90-degree turn each).
    /// </summary>
    public sealed class PipeCellView : MonoBehaviour, IPointerClickHandler
    {
        public Image Tile;
        public Image Pipe;
        public Image SinkFill;
        public Image Halo;
        public int Index;
        public Action<int> Tapped;
        /// <summary>Pipe color while dry (grey, or the water color for sources and sinks).</summary>
        public Color DryColor;

        int _steps;
        float _angle;

        float TargetAngle => -90f * _steps;   // clockwise on screen
        public bool IsRotating => _angle != TargetAngle;

        public void SnapRotation(int steps)
        {
            _steps = steps;
            _angle = TargetAngle;
            Apply();
        }

        public void AddSteps(int delta) => _steps += delta;

        /// <returns>True while still rotating.</returns>
        public bool TickRotation(float dt, float quarterTurnSeconds)
        {
            if (!IsRotating) return false;
            float speed = 90f / Mathf.Max(0.01f, quarterTurnSeconds);
            _angle = Mathf.MoveTowards(_angle, TargetAngle, speed * dt);
            if (!IsRotating && (_steps >= 4 || _steps < 0))
            {
                _steps = ((_steps % 4) + 4) % 4;   // same look, small numbers
                _angle = TargetAngle;
            }
            Apply();
            return IsRotating;
        }

        void Apply() => Pipe.rectTransform.localRotation = Quaternion.Euler(0, 0, _angle);

        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke(Index);
    }
}
