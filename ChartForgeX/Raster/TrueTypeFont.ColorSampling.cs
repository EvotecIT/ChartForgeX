using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    /// <summary>Preflights full-surface work so detailed glyphs can use bounded colour-preserving sampling.</summary>
    private static int PaintPasses(ColorGlyphPaint paint, ref int visits, int depth) {
        if (--visits < 0 || depth > 64) throw new FontLayoutException();
        if (paint.Kind == ColorPaintKind.Transform) return PaintPasses(paint.Children[0], ref visits, depth + 1);
        var count = 1;
        foreach (var child in paint.Children) count += PaintPasses(child, ref visits, depth + 1);
        if (paint.Kind == ColorPaintKind.Layers) count += paint.Children.Length;
        return count;
    }
}
