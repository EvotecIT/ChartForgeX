using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Uniformly scales captured full-domain geometry into an annular crop; never clamps a connected vertex to a window edge.</summary>
internal sealed class ChartPolarRadiusView {
    private readonly RadialValueScale _full;
    internal ChartPolarRadiusView(RadialValueScale full, ChartAxisWindow window) {
        _full = full;
        var first = full.NormalizeBoundary(window.Minimum, false);
        var last = full.NormalizeBoundary(window.Maximum, true, window.MaximumInclusive);
        var inner = Math.Min(first, last); var outer = Math.Max(first, last);
        if (!ChartMath.IsFinite(inner) || !ChartMath.IsFinite(outer) || inner < 0 || outer > 1 || inner >= outer || outer <= 0)
            throw new ArgumentOutOfRangeException(nameof(window), "polar.viewport-unrepresentable: the source window has no finite annular span in its captured full scale.");
        Magnification = 1 / outer; InnerFraction = inner / outer;
        if (!ChartMath.IsFinite(Magnification) || !ChartMath.IsFinite(InnerFraction) || InnerFraction >= 1)
            throw new ArgumentOutOfRangeException(nameof(window), "polar.viewport-unrepresentable: annular magnification cannot be represented without changing source geometry.");
    }
    internal double Magnification { get; }
    internal double InnerFraction { get; }
    internal double Normalize(double source) => _full.Normalize(source) * Magnification;
    internal double BaseNormalize(double source) => _full.Normalize(source);
    internal double Guide(double fraction) => InnerFraction + (1 - InnerFraction) * fraction;
}
