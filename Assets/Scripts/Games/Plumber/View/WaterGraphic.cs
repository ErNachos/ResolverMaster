using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Games.Plumber.View
{
    /// <summary>
    /// The water inside the pipes of one cell (ErNa-005): a thick stroke along each <see cref="WaterPart"/> (straight arms from the centre,
    /// quarter arcs for the curves), drawn from where the water enters up to the current front, with a round front.
    /// The front comes from the pure logic (<see cref="WaterTiming"/>: the distance at which the water reaches each end of a part) and
    /// the current distance of the flow (<see cref="SetFront"/>), so the water moves smoothly and at the same speed in every pipe.
    /// Coordinates: the cell rect, y up, not rotated (the pipes are already in their final position while the water runs).
    /// </summary>
    [ExecuteAlways]   // not inherited from Graphic: needed to draw in edit mode (previews)
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WaterGraphic : MaskableGraphic
    {
        struct Stroke
        {
            public bool Arc;
            public Vector2 A, B, Center;
            public float AngA, AngB, Length;
            public float DistA, DistB;
            public Color ColA, ColB;
        }

        struct Junction
        {
            public Vector2 P;
            public float Dist;
            public Color Col;
        }

        const int ArcSteps = 14, CapSteps = 8;

        readonly List<Stroke> _strokes = new List<Stroke>();
        readonly List<Junction> _junctions = new List<Junction>();
        float _front = -1f;
        /// <summary>Water width as a fraction of the cell side.</summary>
        float _width = 0.2f;

        public void Clear()
        {
            _strokes.Clear();
            _junctions.Clear();
            _front = -1f;
            SetVerticesDirty();
        }

        public void SetWidth(float fractionOfCell)
        {
            _width = fractionOfCell;
            SetVerticesDirty();
        }

        static Vector2 Edge(int dir)
        {
            switch (dir)
            {
                case 0: return new Vector2(0.5f, 1f);
                case 1: return new Vector2(1f, 0.5f);
                case 2: return new Vector2(0.5f, 0f);
                default: return new Vector2(0f, 0.5f);
            }
        }

        /// <summary>Straight arm from the centre (end A) to the side <paramref name="dir"/> (end B).</summary>
        public void AddArm(int dir, float distCentre, float distEdge, Color colCentre, Color colEdge)
        {
            _strokes.Add(new Stroke
            {
                A = new Vector2(0.5f, 0.5f), B = Edge(dir), Length = WaterTiming.ArmLength,
                DistA = distCentre, DistB = distEdge, ColA = colCentre, ColB = colEdge,
            });
        }

        /// <summary>ErNa-006: one channel of a bridge, straight from side <paramref name="dirA"/> to the opposite side <paramref name="dirB"/>.</summary>
        public void AddThrough(int dirA, int dirB, float distA, float distB, Color colA, Color colB)
        {
            _strokes.Add(new Stroke
            {
                A = Edge(dirA), B = Edge(dirB), Length = WaterTiming.ThroughLength,
                DistA = distA, DistB = distB, ColA = colA, ColB = colB,
            });
        }

        /// <summary>Quarter arc of a curve from side <paramref name="dirA"/> to side <paramref name="dirB"/>, around their shared corner.</summary>
        public void AddArc(int dirA, int dirB, float distA, float distB, Color colA, Color colB)
        {
            var a = Edge(dirA);
            var b = Edge(dirB);
            // the corner shared by the two sides: x from the vertical side, y from the horizontal one
            var corner = new Vector2(Mathf.Round(a.x + b.x - 0.5f), Mathf.Round(a.y + b.y - 0.5f));
            float angA = Mathf.Atan2(a.y - corner.y, a.x - corner.x), angB = Mathf.Atan2(b.y - corner.y, b.x - corner.x);
            float delta = Mathf.DeltaAngle(angA * Mathf.Rad2Deg, angB * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            _strokes.Add(new Stroke
            {
                Arc = true, A = a, B = b, Center = corner, AngA = angA, AngB = angA + delta, Length = WaterTiming.ArcLength,
                DistA = distA, DistB = distB, ColA = colA, ColB = colB,
            });
        }

        /// <summary>A round blob at the centre of a T or a cross once the water gets there (fills the corners between the arms).</summary>
        public void AddJunction(float dist, Color col) => _junctions.Add(new Junction { P = new Vector2(0.5f, 0.5f), Dist = dist, Col = col });

        /// <summary>How far the water has gone from the sources (cells along the pipes); negative = no water.</summary>
        public void SetFront(float distance)
        {
            if (Mathf.Approximately(distance, _front)) return;
            _front = distance;
            SetVerticesDirty();
        }

        Vector2 PointAt(in Stroke s, float along)
        {
            float k = s.Length > 0 ? Mathf.Clamp01(along / s.Length) : 0f;
            if (!s.Arc) return Vector2.Lerp(s.A, s.B, k);
            float ang = Mathf.Lerp(s.AngA, s.AngB, k);
            return s.Center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.5f;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_front < 0f || (_strokes.Count == 0 && _junctions.Count == 0)) return;
            var r = rectTransform.rect;
            float hw = _width * r.width / 2f;
            Vector2 ToLocal(Vector2 u) => new Vector2(r.xMin + u.x * r.width, r.yMin + u.y * r.height);

            foreach (var j in _junctions)
                if (_front >= j.Dist) Disk(vh, ToLocal(j.P), hw, j.Col);

            foreach (var s in _strokes)
            {
                bool fromA = !float.IsInfinity(s.DistA) && _front > s.DistA;
                bool fromB = !float.IsInfinity(s.DistB) && _front > s.DistB;
                float covA = fromA ? Mathf.Min(s.Length, _front - s.DistA) : 0f;
                float covB = fromB ? Mathf.Min(s.Length, _front - s.DistB) : 0f;
                if (fromA && fromB && s.ColA != s.ColB)
                {
                    // two colors flowing into the same part meet where their fronts touch
                    float meet = Mathf.Clamp((s.Length + s.DistB - s.DistA) / 2f, 0f, s.Length);
                    covA = Mathf.Min(covA, meet);
                    covB = Mathf.Min(covB, s.Length - meet);
                }
                if (covA > 0f) Draw(vh, s, 0f, covA, s.ColA, hw, ToLocal, covA < s.Length);
                if (covB > 0f) Draw(vh, s, s.Length, s.Length - covB, s.ColB, hw, ToLocal, covB < s.Length);
            }
        }

        /// <summary>Unit direction of increasing distance along the part, at <paramref name="along"/> (cell coordinates).</summary>
        static Vector2 TangentAt(in Stroke s, float along)
        {
            if (!s.Arc) return (s.B - s.A).normalized;
            float k = s.Length > 0 ? Mathf.Clamp01(along / s.Length) : 0f;
            float ang = Mathf.Lerp(s.AngA, s.AngB, k);
            return new Vector2(-Mathf.Sin(ang), Mathf.Cos(ang)) * Mathf.Sign(s.AngB - s.AngA);
        }

        /// <summary>Stroke from <paramref name="from"/> to <paramref name="to"/> (distances along the part, either direction), round cap at <paramref name="to"/> when it is a moving front.</summary>
        void Draw(VertexHelper vh, in Stroke s, float from, float to, Color col, float hw, System.Func<Vector2, Vector2> toLocal, bool cap)
        {
            var r = rectTransform.rect;
            int steps = s.Arc ? Mathf.Max(2, Mathf.CeilToInt(ArcSteps * Mathf.Abs(to - from) / s.Length)) : 1;
            float sign = to >= from ? 1f : -1f;
            int start = vh.currentVertCount;
            Vector2 last = default, lastDir = default;
            for (int i = 0; i <= steps; i++)
            {
                float along = Mathf.Lerp(from, to, (float)i / steps);
                var p = toLocal(PointAt(s, along));
                var t = TangentAt(s, along) * sign;
                var dir = new Vector2(t.x * r.width, t.y * r.height).normalized;
                var n = new Vector2(-dir.y, dir.x) * hw;
                vh.AddVert(p + n, col, Vector4.zero);
                vh.AddVert(p - n, col, Vector4.zero);
                if (i > 0)
                {
                    vh.AddTriangle(start + 2 * i - 2, start + 2 * i - 1, start + 2 * i + 1);
                    vh.AddTriangle(start + 2 * i - 2, start + 2 * i + 1, start + 2 * i);
                }
                last = p;
                lastDir = dir;
            }
            if (!cap) return;
            // half disk in front of the end point: the water front is round
            int c0 = vh.currentVertCount;
            vh.AddVert(last, col, Vector4.zero);
            float baseAng = Mathf.Atan2(lastDir.y, lastDir.x);
            for (int k = 0; k <= CapSteps; k++)
            {
                float a = baseAng - Mathf.PI / 2f + Mathf.PI * k / CapSteps;
                vh.AddVert(last + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * hw, col, Vector4.zero);
                if (k > 0) vh.AddTriangle(c0, c0 + k, c0 + k + 1);
            }
        }

        static void Disk(VertexHelper vh, Vector2 c, float r, Color col)
        {
            const int steps = 20;
            int c0 = vh.currentVertCount;
            vh.AddVert(c, col, Vector4.zero);
            for (int k = 0; k <= steps; k++)
            {
                float a = 2f * Mathf.PI * k / steps;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col, Vector4.zero);
                if (k > 0) vh.AddTriangle(c0, c0 + k, c0 + k + 1);
            }
        }
    }
}
