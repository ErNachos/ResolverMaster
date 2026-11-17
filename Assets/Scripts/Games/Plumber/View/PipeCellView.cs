using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ResolverMaster.Games.Plumber.View
{
    /// <summary>
    /// One pooled cell, from the bottom: the theme slab (takes the tap); for a sink the colored halo and the water filling it; the pipe
    /// (rotates; for a source or a sink only the stub towards the opening); the water running inside the pipe (not rotated: drawn in its
    /// final position while the water runs); for a source or a sink a soft shadow and the drawing on top, lifted so that its base sits
    /// on the pipe (a source also has its mask, tinted with the water color). Rotation is animated at a constant speed towards the
    /// target, so taps made during an animation queue up (one 90-degree turn each).
    /// </summary>
    public sealed class PipeCellView : MonoBehaviour, IPointerClickHandler
    {
        public Image Tile;
        public Image Pipe;
        public WaterGraphic Water;
        /// <summary>ErNa-006, bridges only: the upper pipe (north-south) and its water, above the lower pipe and its water.</summary>
        public Image PipeTop;
        public WaterGraphic WaterTop;
        /// <summary>ErNa-006, locked pieces only: the dark plate with 4 bolts under the pipe.</summary>
        public Image Plate;
        /// <summary>ErNa-007, pieces fixed by the light bulb: a glowing outline on the tile.</summary>
        public Image Glow;
        public Image Halo;
        /// <summary>Sink only: the water filling the fountain (a disk that grows on the halo).</summary>
        public Image Fill;
        public Image Shadow;
        /// <summary>Source or fountain drawing (inactive on pipe cells).</summary>
        public Image Object;
        /// <summary>Source only: the magenta parts of the drawing, tinted with the water color.</summary>
        public Image ObjectMask;
        public int Index;
        public Action<int> Tapped;
        /// <summary>Tint of the slab (white, or the locked tint).</summary>
        public Color TileColor = Color.white;

        int _steps;
        float _angle;
        float _shake = -1f;

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

        /// <summary>A tap on a piece that cannot turn: a short shake of the whole cell (ErNa-006, <see cref="ResolverMaster.Core.UI.ShakeMotion"/>).</summary>
        public void StartShake() => _shake = 0f;

        public void StopShake()
        {
            _shake = -1f;
            ((RectTransform)transform).anchoredPosition = Vector2.zero;
        }

        public void TickShake(float dt, float duration, float amplitude, int cycles)
        {
            if (_shake < 0f) return;
            _shake += dt;
            var rt = (RectTransform)transform;
            if (_shake >= duration)
            {
                StopShake();
                return;
            }
            rt.anchoredPosition = new Vector2(ResolverMaster.Core.UI.ShakeMotion.Offset(_shake, duration, amplitude * rt.rect.width, cycles), 0f);
        }

        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke(Index);
    }
}
