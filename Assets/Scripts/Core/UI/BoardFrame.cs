using ResolverMaster.Core.UI.Themes;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// The window around a grid puzzle (TJ-001: moved here from the Plumber board, shared by every grid game): a cream Gemini window
    /// fitted into the puzzle area, as big as possible and centred, with the ground of the chapter theme under the cells, the cells rect
    /// and an effects rect on top. Square cells; columns and rows may differ. Big grids (cells below <see cref="compactCellBelow"/>)
    /// get a thinner window frame and may use a little of the screen margin (ErNa-006).
    /// </summary>
    public sealed class BoardFrame
    {
        public RectTransform Frame { get; private set; }
        public Image FrameImage { get; private set; }
        public Image Ground { get; private set; }
        public RectTransform Cells { get; private set; }
        public RectTransform Effects { get; private set; }
        /// <summary>Side of a cell after the last <see cref="Fit"/> (canvas units).</summary>
        public float Cell { get; private set; }
        public float Inset { get; private set; }
        /// <summary>Distance between the cells and the outer edge of the window on each side (inset + the game's extra room).</summary>
        public float Margin { get; private set; }

        /// <summary>Space between the edge of the window and the cells (canvas units): inside its inner frame line.</summary>
        public float inset = 30;
        /// <summary>Cells smaller than this (canvas units): thinner frame (<see cref="compactFrameScale"/>) and <see cref="compactOverflow"/> of the margin.</summary>
        public float compactCellBelow = 150;
        public float compactFrameScale = 0.5f;
        public float compactOverflow = 16;

        /// <summary>Builds Frame (window) / Ground / Cells / Effects under <paramref name="parent"/>.</summary>
        public static BoardFrame Create(UiFactory ui, Transform parent, float inset, float compactCellBelow, float compactFrameScale, float compactOverflow)
        {
            var b = new BoardFrame { inset = inset, compactCellBelow = compactCellBelow, compactFrameScale = compactFrameScale, compactOverflow = compactOverflow };
            b.FrameImage = ui.Panel("Frame", parent);
            b.Frame = b.FrameImage.rectTransform;
            UiFactory.Place(b.Frame, 0.5f, 0.5f, new Vector2(100, 100), Vector2.zero);
            b.Ground = ui.Image("Ground", b.Frame, null, Color.white);
            UiFactory.Stretch(b.Ground.rectTransform, inset, inset, inset, inset);
            b.Ground.gameObject.AddComponent<Decoration>();   // the terrain under the cells
            b.Cells = ui.Rect("Cells", b.Frame);
            UiFactory.Stretch(b.Cells, inset, inset, inset, inset);
            b.Effects = ui.Rect("Effects", b.Frame);
            UiFactory.Stretch(b.Effects, inset, inset, inset, inset);
            b.Inset = b.Margin = inset;
            return b;
        }

        /// <summary>The ground between the cells: the tiled terrain of the theme (hidden without a theme).</summary>
        public void SetTheme(VisualTheme theme)
        {
            Ground.sprite = theme != null ? ThemeArt.Ground(theme) : null;
            Ground.type = Image.Type.Tiled;
            Ground.gameObject.SetActive(theme != null);
        }

        /// <summary>
        /// The biggest square cells that fit <paramref name="area"/> (columns and rows may differ), the window around them, centred.
        /// <paramref name="extraCells"/>: room the game keeps between the cells and the window frame on every side, in cells (e.g. the
        /// exits of Traffic jam, drawn in the frame). False when the area is empty.
        /// </summary>
        public bool Fit(Vector2 area, int cols, int rows, float extraCells = 0)
        {
            if (cols <= 0 || rows <= 0 || area.x <= 0 || area.y <= 0) return false;
            float ins = inset, overflow = 0f, frameScale = 1f, e = Mathf.Max(0f, extraCells);
            float cell = Mathf.Floor(Mathf.Min((area.x - 2 * ins) / (cols + 2 * e), (area.y - 2 * ins) / (rows + 2 * e)));
            if (cell < compactCellBelow)
            {
                frameScale = Mathf.Clamp(compactFrameScale, 0.3f, 1f);
                ins = Mathf.Round(inset * frameScale);
                overflow = Mathf.Max(0f, compactOverflow);
                cell = Mathf.Floor(Mathf.Min((area.x + 2 * overflow - 2 * ins) / (cols + 2 * e), (area.y + 2 * overflow - 2 * ins) / (rows + 2 * e)));
            }
            if (cell <= 0) return false;
            FrameImage.pixelsPerUnitMultiplier = 1f / frameScale;   // the sliced frame drawn thinner, never bigger
            float ix = ins + Mathf.Round(e * cell), iy = ix;
            UiFactory.Stretch(Ground.rectTransform, ix, iy, ix, iy);
            UiFactory.Stretch(Cells, ix, iy, ix, iy);
            UiFactory.Stretch(Effects, ix, iy, ix, iy);
            Frame.sizeDelta = new Vector2(cell * cols + 2 * ix, cell * rows + 2 * iy);
            Cell = cell;
            Inset = ins;
            Margin = ix;
            return true;
        }
    }
}
