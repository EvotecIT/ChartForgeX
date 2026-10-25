using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

/// <summary>The incumbent TinyFont stroke geometry, shared by native paint and permanent rich-caption SVG resources.</summary>
internal static partial class TinyFont {
    internal static List<IReadOnlyList<ChartPoint>> Strokes(double x, double y, char ch, double scale, bool italic,
        double boldOffset = 0, double coordinateScale = 1) {
        var glyph = TinyFont.GetBitmap(ch);
        var paths = new List<IReadOnlyList<ChartPoint>>();
        for (var row = 0; row < TinyFont.Height; row++) {
            for (var col = 0; col < TinyFont.Width; col++) {
                if (!GlyphCell(glyph, row, col)) continue;
                var center = GlyphCenter(row, col);
                var cx = center.X;
                var cy = center.Y;
                var connected = false;
                if (GlyphCell(glyph, row, col + 1)) {
                    var right = GlyphCenter(row, col + 1);
                    AddStroke(center, right);
                    connected = true;
                }

                if (GlyphCell(glyph, row + 1, col)) {
                    var down = GlyphCenter(row + 1, col);
                    AddStroke(center, down);
                    connected = true;
                }

                if (GlyphCell(glyph, row + 1, col + 1) && !GlyphCell(glyph, row, col + 1) && !GlyphCell(glyph, row + 1, col)) {
                    var downRight = GlyphCenter(row + 1, col + 1);
                    AddStroke(center, downRight);
                    connected = true;
                }

                if (GlyphCell(glyph, row + 1, col - 1) && !GlyphCell(glyph, row, col - 1) && !GlyphCell(glyph, row + 1, col)) {
                    var downLeft = GlyphCenter(row + 1, col - 1);
                    AddStroke(center, downLeft);
                    connected = true;
                }

                if (!connected) paths.Add(new[] { new ChartPoint(cx / coordinateScale, cy / coordinateScale) });
            }
        }

        if (boldOffset > 0) {
            int count = paths.Count;
            for (int i = 0; i < count; i++) {
                var shifted = new List<ChartPoint>(paths[i].Count);
                foreach (ChartPoint point in paths[i]) shifted.Add(new ChartPoint(point.X + boldOffset, point.Y));
                paths.Add(shifted);
            }
        }
        return paths;
        void AddStroke(ChartPoint start, ChartPoint stop) => paths.Add(new[] {
            new ChartPoint(start.X / coordinateScale, start.Y / coordinateScale), new ChartPoint(stop.X / coordinateScale, stop.Y / coordinateScale)
        });
        ChartPoint GlyphCenter(int row, int col) {
            var italicOffset = italic ? (TinyFont.Height - 1 - row) * scale * TrueTypeFont.ObliqueShear : 0;
            return new ChartPoint(x + (col + 0.5) * scale + italicOffset, y + (row + 0.5) * scale);
        }
    }
    private static bool GlyphCell(byte[] glyph, int row, int col) {
        if (row < 0 || row >= TinyFont.Height || col < 0 || col >= TinyFont.Width) return false;
        return ((glyph[row] >> (TinyFont.Width - 1 - col)) & 1) == 1;
    }
    /// <summary>Captures the same round stroke ink once in logical coordinates; no host font is resolved.</summary>
    internal static List<List<ChartPoint>> Ink(char character, double size, bool italic, double boldOffset) {
        var scale = ScaleForFontSize(size);
        var thickness = Math.Min(scale, Math.Max(1.4 / 2, scale * .8));
        var outlines = new List<List<ChartPoint>>();
        foreach (var path in Strokes(0, 0, character, scale, italic, boldOffset))
            RasterStroker.AppendOutline(path, thickness, RasterLineCap.Round, RasterLineJoin.Round, 4, 2, outlines);
        return outlines;
    }
}
