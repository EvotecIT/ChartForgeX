using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

/// <summary>
/// Light, font-independent hinting for small text, in the spirit of FreeType's "light" autohinter:
/// the baseline of a run is moved to a whole output pixel, and each glyph is stretched vertically,
/// piecewise, so its face's x-height and cap height also land on whole pixels. Nothing moves
/// horizontally, so advances, measured widths, and wrapping are exactly those of unhinted text.
/// Full mode additionally fits narrow straight stems without changing layout coordinates.
/// It applies only at <see cref="MaximumPixelSize"/> output pixels and below, where half-covered
/// rows blur stems and the x-height most.
/// </summary>
internal sealed partial class GlyphGridFit {
    /// <summary>The largest font size, in output pixels, that is grid-fitted.</summary>
    internal const double MaximumPixelSize = 12;

    private readonly double _outputScale;
    private readonly bool _fitStems;

    private GlyphGridFit(double outputScale, double baseline, bool fitStems) {
        _outputScale = outputScale;
        Baseline = baseline;
        _fitStems = fitStems;
    }

    /// <summary>The run's baseline in canvas units, on a whole output pixel.</summary>
    internal double Baseline { get; }

    /// <summary>The fit for text of <paramref name="fontSize"/> on <paramref name="canvas"/>, or null when it is not hinted.</summary>
    internal static GlyphGridFit? Create(RgbaCanvas canvas, double fontSize, double baseline) {
        if (canvas.TextHinting == TextHinting.None) return null;
        double outputScale = canvas.OutputScale;
        var pixelSize = fontSize * outputScale;
        if (!(pixelSize > 0) || pixelSize > MaximumPixelSize + 1e-9 || double.IsNaN(baseline) || double.IsInfinity(baseline)) return null;
        return new GlyphGridFit(outputScale, Math.Round(baseline * outputScale, MidpointRounding.AwayFromZero) / outputScale, canvas.TextHinting == TextHinting.Full);
    }

    /// <summary>
    /// Fits the contours of one glyph drawn on <see cref="Baseline"/>; <paramref name="xHeight"/> and
    /// <paramref name="capHeight"/> are its face's heights in canvas units at the drawn size.
    /// </summary>
    internal void Apply(List<List<ChartPoint>> contours, double xHeight, double capHeight, bool allowStemFit = true) {
        if (_fitStems && allowStemFit) FitStems(contours);
        var xh = xHeight * _outputScale;
        var cap = capHeight * _outputScale;
        if (!(xh > 0)) return;
        // Round the x-height up from four tenths of a pixel: a taller x-height reads better at these sizes.
        var fittedXh = Math.Max(1, Math.Floor(xh + 0.6));
        var fittedCap = Math.Max(fittedXh, Math.Round(cap, MidpointRounding.AwayFromZero));
        foreach (var contour in contours) {
            for (var i = 0; i < contour.Count; i++) {
                var point = contour[i];
                var height = (Baseline - point.Y) * _outputScale;
                contour[i] = new ChartPoint(point.X, Baseline - Fit(height, xh, fittedXh, cap, fittedCap) / _outputScale);
            }
        }
    }

    // Heights above the baseline, in output pixels: 0..x-height and x-height..cap height are scaled
    // onto their fitted spans; ascenders above the cap height shift with it, descenders are unchanged.
    private static double Fit(double height, double xh, double fittedXh, double cap, double fittedCap) {
        if (height <= 0) return height;
        if (height <= xh) return height * fittedXh / xh;
        if (cap > xh + 0.5) {
            if (height <= cap) return fittedXh + (height - xh) * (fittedCap - fittedXh) / (cap - xh);
            return height + fittedCap - cap;
        }

        return height + fittedXh - xh;
    }
}
