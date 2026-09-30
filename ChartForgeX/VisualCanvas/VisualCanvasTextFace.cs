using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Composition;

/// <summary>
/// The CSS weights visual canvas text asks for. The SVG renderer writes them as <c>font-weight</c>
/// and the PNG renderer resolves the same weights to faces, so both outputs draw the same face.
/// </summary>
internal static class VisualCanvasFontWeights {
    public const int Regular = 500;
    public const int Emphasized = 800;
    public const int HeroTitle = 850;
    public const int HeroBadge = 850;
    public const int KeyValueEmphasizedValue = 700;
    public const int TileLabel = 700;
    public const int TileValue = 650;
    public const int TileDetail = 500;
    public const int FeatureLabel = 700;

    public static string Css(int weight) => weight.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// One family and weight resolved to a face once, then used for fitting, measuring, and drawing, so
/// fitted and wrapped canvas text is measured with exactly the face that draws it.
/// </summary>
internal readonly struct VisualCanvasTextFace {
    private const string Ellipsis = "...";
    private readonly ResolvedTypeface _face;

    private VisualCanvasTextFace(ResolvedTypeface face) {
        _face = face;
    }

    public static VisualCanvasTextFace Resolve(string? family, int weight) => new(TypographyFontResolver.ResolveFace(family, weight, italic: false));

    public double Measure(string text, double fontSize) {
        if (string.IsNullOrEmpty(text)) return 0;
        // Only a family without a bold face is emboldened, and only then does the offset take room.
        return _face.SynthesizeBold
            ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, _face.Font)
            : RgbaCanvas.MeasureTextWidth(text, fontSize, _face.Font);
    }

    public double LineHeight(double fontSize) => RgbaCanvas.MeasureTextHeight(fontSize, _face.Font);

    /// <summary>Draws text whose em box starts at <paramref name="y"/>; the baseline is one font size lower, where SVG output puts it.</summary>
    public void Draw(RgbaCanvas canvas, double x, double y, string text, ChartColor color, double fontSize) {
        if (string.IsNullOrEmpty(text)) return;
        var top = y + fontSize - (_face.Font?.Ascent(fontSize) ?? fontSize);
        if (_face.SynthesizeBold) canvas.DrawTextEmphasized(x, top, text, color, fontSize, _face.Font);
        else canvas.DrawText(x, top, text, color, fontSize, _face.Font);
    }

    /// <summary>The largest size up to <paramref name="fontSize"/> at which the runs, set side by side, fit the width.</summary>
    public double FitSize(IEnumerable<string> runs, double fontSize, double maxWidth) {
        var size = fontSize;
        for (var attempt = 0; attempt < 40; attempt++) {
            var width = 0.0;
            foreach (var run in runs) width += Measure(run, size);
            if (width <= maxWidth || size <= 1) return size;
            // Widths scale almost linearly with size; step a little past the ratio so rounding cannot loop.
            size = Math.Max(1, Math.Min(size * 0.99, size * maxWidth / width));
        }

        return size;
    }

    /// <summary>Returns the text, or its longest prefix followed by an ellipsis, that fits the width.</summary>
    public string Fit(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || Measure(value, fontSize) <= maxWidth) return value;
        if (Measure(Ellipsis, fontSize) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (Measure(value.Substring(0, mid) + Ellipsis, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + Ellipsis;
    }
}
