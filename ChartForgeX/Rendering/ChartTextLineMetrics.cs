using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Resolved line extents for layouts that position text by its alphabetic baseline.</summary>
internal static class ChartTextLineMetrics {
    internal static (double Height, double Ascent) Measure(Chart chart, string text, double size, TextStyleOverride style, int weight) {
        var resolved = ChartLabelScene.ResolveTextStyle(size, style.WithDefaultFontFamily(chart.Options.Theme.FontFamily), weight);
        resolved.Font.FilePath = chart.Options.PngFontPath;
        resolved.Font.CollectionIndex = chart.Options.PngFontCollectionIndex;
        resolved.Font.FaceName = chart.Options.PngFontFaceName;
        var metrics = ChartLabelScene.MeasureText(style.TransformText(text, CultureInfo.InvariantCulture), resolved);
        var ascent = TypographyFontResolver.ResolveFace(resolved.Font).Font?.Ascent(size) ?? size * 0.82;
        return (metrics.Height, ascent);
    }
}
