using System.Collections.Generic;
using ResolverMaster.Core.App;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// ErNa-010: the colors of a soft screen background, pure logic (tested in EditMode): a light vertical gradient from one base color,
    /// a pattern a little darker than it, and the base color following a carousel (Home cards) with a smooth blend while it scrolls.
    /// </summary>
    public static class BackgroundColors
    {
        /// <summary>Gradient top: the base a little lighter.</summary>
        public static Color Top(Color c) => Color.Lerp(c, Color.white, 0.28f);

        /// <summary>Gradient bottom: the base a little darker.</summary>
        public static Color Bottom(Color c) => new Color(c.r * 0.95f, c.g * 0.95f, c.b * 0.95f, 1f);

        /// <summary>The silhouettes of the pattern: just darker than the base.</summary>
        public static Color Pattern(Color c) => new Color(c.r * 0.9f, c.g * 0.9f, c.b * 0.92f, 1f);

        /// <summary>The base color at carousel position <paramref name="position"/> (0 = first card, fractions while scrolling).</summary>
        public static Color At(IReadOnlyList<Color> colors, float position)
        {
            if (colors == null || colors.Count == 0) return Color.white;
            float p = Mathf.Clamp(position, 0f, colors.Count - 1);
            int a = Mathf.FloorToInt(p), b = Mathf.Min(colors.Count - 1, a + 1);
            return Color.Lerp(colors[a], colors[b], p - a);
        }
    }

    /// <summary>A rectangle filled with a vertical two-color gradient (top, bottom).</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GradientGraphic : Graphic
    {
        Color _top = Color.white, _bottom = Color.white;

        public void SetColors(Color top, Color bottom)
        {
            if (_top == top && _bottom == bottom) return;
            _top = top;
            _bottom = bottom;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            vh.AddVert(new Vector3(r.xMin, r.yMin), _bottom, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), _top, Vector2.up);
            vh.AddVert(new Vector3(r.xMax, r.yMax), _top, Vector2.one);
            vh.AddVert(new Vector3(r.xMax, r.yMin), _bottom, Vector2.right);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }

    /// <summary>
    /// ErNa-010: the background of Home (and of the chapter list): a soft vertical gradient and, over it, a sparse pattern of small
    /// silhouettes from the games (drop, pipe, car, fish, snowflake; Resources/ResolverMaster/HomePattern.png, made by
    /// Tools/Art/make_home_pattern.py from the existing icons) a little darker than the background, drifting slowly on the diagonal.
    /// Drawn behind the safe area, on the whole screen; not touchable.
    /// </summary>
    public sealed class PatternBackground : MonoBehaviour
    {
        public const string PatternResource = "ResolverMaster/HomePattern";
        /// <summary>Size of one pattern tile on screen (canvas units) and drift speed (tiles per second, diagonal).</summary>
        public const float TileSize = 540f, Speed = 0.012f;

        GradientGraphic _gradient;
        RawImage _pattern;
        Vector2 _offset;

        public Color BaseColor { get; private set; } = Color.white;

        /// <summary>Adds the background as the first child of <paramref name="screenRoot"/> (behind everything else).</summary>
        public static PatternBackground Create(Transform screenRoot)
        {
            var go = new GameObject("PatternBackground", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(screenRoot, false);
            rt.SetAsFirstSibling();
            UiFactory.Stretch(rt);
            var bg = go.AddComponent<PatternBackground>();

            var g = new GameObject("Gradient", typeof(RectTransform), typeof(CanvasRenderer)) { layer = UiFactory.UiLayer };
            ((RectTransform)g.transform).SetParent(rt, false);
            UiFactory.Stretch((RectTransform)g.transform);
            bg._gradient = g.AddComponent<GradientGraphic>();
            bg._gradient.raycastTarget = false;
            g.AddComponent<Decoration>().mayLeaveScreen = true;   // the whole screen, behind the safe area on purpose

            var p = new GameObject("Pattern", typeof(RectTransform), typeof(CanvasRenderer)) { layer = UiFactory.UiLayer };
            ((RectTransform)p.transform).SetParent(rt, false);
            UiFactory.Stretch((RectTransform)p.transform);
            bg._pattern = p.AddComponent<RawImage>();
            bg._pattern.raycastTarget = false;
            bg._pattern.texture = Resources.Load<Texture2D>(PatternResource);
            bg._pattern.gameObject.SetActive(bg._pattern.texture != null);
            p.AddComponent<Decoration>().mayLeaveScreen = true;
            bg.SetColor(Color.white);
            return bg;
        }

        public void SetColor(Color c)
        {
            BaseColor = c;
            _gradient.SetColors(BackgroundColors.Top(c), BackgroundColors.Bottom(c));
            _pattern.color = BackgroundColors.Pattern(c);
            ApplyUv();   // the tile size also when nothing ticks (previews)
        }

        void Update()
        {
            _offset += new Vector2(Speed, Speed) * Time.unscaledDeltaTime;
            _offset.x = Mathf.Repeat(_offset.x, 1f);
            _offset.y = Mathf.Repeat(_offset.y, 1f);
            ApplyUv();
        }

        void ApplyUv()
        {
            var size = ((RectTransform)_pattern.transform).rect.size;
            _pattern.uvRect = new Rect(_offset.x, _offset.y, Mathf.Max(1f, size.x) / TileSize, Mathf.Max(1f, size.y) / TileSize);
        }

        void OnRectTransformDimensionsChange()
        {
            if (_pattern != null) ApplyUv();
        }
    }

    /// <summary>
    /// ErNa-010: the two round arrow buttons of a carousel (Home cards, chapter pages), in the style of the round buttons (Gemini, black
    /// outline, depth). An arrow disappears on the first and on the last item; the finger still scrolls.
    /// </summary>
    public sealed class CarouselArrows
    {
        public Button Left { get; }
        public Button Right { get; }

        /// <summary>Which arrows are shown for item <paramref name="target"/> of <paramref name="count"/> (pure logic).</summary>
        public static (bool left, bool right) Shown(int target, int count) => (count > 1 && target > 0, count > 1 && target < count - 1);

        public CarouselArrows(AppServices ctx, Transform parent, System.Action previous, System.Action next)
        {
            var s = ctx.Style;
            Left = ctx.Ui.IconButton("ArrowLeft", parent, "icon_back", s.iconButtonSize);
            Right = ctx.Ui.IconButton("ArrowRight", parent, "icon_back", s.iconButtonSize);
            var icon = Right.transform.Find("Icon");
            if (icon != null) icon.localScale = new Vector3(-1f, 1f, 1f);   // the back arrow, mirrored
            Left.onClick.AddListener(() => previous());
            Right.onClick.AddListener(() => next());
        }

        /// <summary>Places the arrows at <paramref name="leftPos"/> / <paramref name="rightPos"/> from the given anchors.</summary>
        public void Place(Vector2 anchorLeft, Vector2 leftPos, Vector2 anchorRight, Vector2 rightPos, float size)
        {
            UiFactory.Place((RectTransform)Left.transform, anchorLeft.x, anchorLeft.y, new Vector2(size, size), leftPos);
            UiFactory.Place((RectTransform)Right.transform, anchorRight.x, anchorRight.y, new Vector2(size, size), rightPos);
        }

        public void Refresh(int target, int count)
        {
            var (l, r) = Shown(target, count);
            Left.gameObject.SetActive(l);
            Right.gameObject.SetActive(r);
        }
    }
}
