using System;
using System.Text;

namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// Text form of a plumber grid, readable in the level files, the docs and the tests.
    /// Rows from top to bottom separated by '/', cells separated by spaces. A cell is <c>&lt;kind&gt;&lt;rotation&gt;[#][@color]</c>:
    /// kind <c>I</c> straight, <c>C</c> curve, <c>T</c> tee, <c>X</c> cross, <c>B</c> bridge (ErNa-006), <c>S</c> source, <c>E</c> end (sink), <c>.</c> empty;
    /// rotation 0..3 = clockwise taps from the base shape (I0 vertical, C0 up+right, T0 up+right+down, S0/E0 opening up);
    /// <c>#</c> = locked (ErNa-006: never turns); <c>@n</c> = water color n (default 0). Example: <c>"S1 I1 E3"</c>.
    /// </summary>
    public static class PlumberLevelCodec
    {
        public static PipeGrid Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("empty grid");
            string[] rows = text.Trim().Split('/');
            int height = rows.Length;
            int width = -1;
            PipeGrid grid = null;
            for (int y = 0; y < height; y++)
            {
                string[] tokens = rows[y].Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (width < 0)
                {
                    width = tokens.Length;
                    if (width == 0) throw new FormatException("empty row 1");
                    grid = new PipeGrid(width, height);
                }
                else if (tokens.Length != width) throw new FormatException($"row {y + 1} has {tokens.Length} cells, expected {width}");
                for (int x = 0; x < width; x++)
                    grid.Pieces[y * width + x] = ParseCell(tokens[x], x, y);
            }
            return grid;
        }

        static Piece ParseCell(string token, int x, int y)
        {
            var piece = new Piece();
            switch (token[0])
            {
                case '.': piece.Kind = PieceKind.Empty; break;
                case 'I': piece.Kind = PieceKind.Straight; break;
                case 'C': piece.Kind = PieceKind.Curve; break;
                case 'T': piece.Kind = PieceKind.Tee; break;
                case 'X': piece.Kind = PieceKind.Cross; break;
                case 'S': piece.Kind = PieceKind.Source; break;
                case 'E': piece.Kind = PieceKind.Sink; break;
                case 'B': piece.Kind = PieceKind.Bridge; break;
                default: throw new FormatException($"cell ({x},{y}) '{token}': unknown kind '{token[0]}'");
            }
            int i = 1;
            if (i < token.Length && char.IsDigit(token[i]))
            {
                piece.Rotation = token[i] - '0';
                if (piece.Rotation > 3) throw new FormatException($"cell ({x},{y}) '{token}': rotation must be 0..3");
                i++;
            }
            while (i < token.Length)
            {
                if (token[i] == '#')
                {
                    piece.Locked = true;
                    i++;
                }
                else if (token[i] == '@')
                {
                    int start = ++i;
                    while (i < token.Length && char.IsDigit(token[i])) i++;
                    if (i == start) throw new FormatException($"cell ({x},{y}) '{token}': color number missing after '@'");
                    piece.Color = int.Parse(token.Substring(start, i - start));
                }
                else throw new FormatException($"cell ({x},{y}) '{token}': unexpected '{token[i]}'");
            }
            return piece;
        }

        public static string Format(PipeGrid grid)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < grid.Height; y++)
            {
                if (y > 0) sb.Append('/');
                for (int x = 0; x < grid.Width; x++)
                {
                    if (x > 0) sb.Append(' ');
                    var p = grid.Pieces[y * grid.Width + x];
                    sb.Append(KindChar(p.Kind));
                    if (p.Kind != PieceKind.Empty) sb.Append((char)('0' + p.Rotation));
                    if (p.Locked) sb.Append('#');
                    if (p.Color != 0) sb.Append('@').Append(p.Color);
                }
            }
            return sb.ToString();
        }

        public static char KindChar(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Straight: return 'I';
                case PieceKind.Curve: return 'C';
                case PieceKind.Tee: return 'T';
                case PieceKind.Cross: return 'X';
                case PieceKind.Source: return 'S';
                case PieceKind.Sink: return 'E';
                case PieceKind.Bridge: return 'B';
                default: return '.';
            }
        }
    }
}
