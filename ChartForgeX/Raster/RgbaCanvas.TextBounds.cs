using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>Extends a temporary text surface to the actual positioned ink, retaining its logical origin.</summary>
    private ChartRect TextBufferBounds(string text, double size, TrueTypeFont? font, bool italic, bool emphasized, double width, double height, double padding = 0, double baselineOffset = 0) {
        // Full fitting can move outer ink by less than one output pixel, including negative bearings.
        var fittingMargin = TextHinting == TextHinting.Full ? 1.0 / OutputScale : 0;
        padding += fittingMargin;
        var left = -padding; var top = -padding; var right = width + padding; var bottom = height + padding;
        var inkFace = emphasized ? EmphasisFace(font) ?? font : font;
        var ink = inkFace?.MeasureGlyphInk(TextShaper.Shape(inkFace, text, size), size, italic && !inkFace.IsItalic,
            TextHinting == TextHinting.Full ? FontStrikeScale : 0);
        if (ink.HasValue) {
            var bounds = ink.Value;
            left = Math.Min(left, Math.Floor(bounds.X) - fittingMargin); top = Math.Min(top, Math.Floor(bounds.Y + baselineOffset) - fittingMargin);
            right = Math.Max(right, Math.Ceiling(bounds.X + bounds.Width + (emphasized ? EmphasisOffset(size) : 0)) + fittingMargin);
            bottom = Math.Max(bottom, Math.Ceiling(bounds.Y + bounds.Height + baselineOffset) + fittingMargin);
        }
        return new ChartRect(left, top, right - left, bottom - top);
    }
}
