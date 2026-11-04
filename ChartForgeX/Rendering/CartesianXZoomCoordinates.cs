using System;
using ChartForgeX.Primitives;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Detached source windows and the native affine track that paints them.</summary>
internal sealed class CartesianXZoomCoordinates {
    private readonly double _minimum;
    private readonly double _maximum;
    private readonly double _factor;
    private readonly double _windowSpan;
    private readonly ChartRect _plot;
    private readonly bool _reversed;

    internal CartesianXZoomCoordinates(double minimum, double maximum, ChartRect plot, double factor, bool reversed) {
        var trackWidth = plot.Width * factor;
        if (!ChartMath.IsFinite(trackWidth) || !ChartMath.IsFinite(plot.Left + trackWidth))
            throw new ArgumentOutOfRangeException("levels", "An X zoom track must fit finite logical coordinates.");
        var travel = trackWidth - plot.Width;
        var magnitude = Math.Max(Math.Abs(plot.Left + trackWidth), Math.Abs(plot.Left - travel));
        // Normalization, projection and translation need rounding headroom below half an SVG 0.001-unit step.
        // Include the negative translated endpoint as well as the positive unshifted endpoint.
        if (!ChartMath.IsFinite(magnitude) || 4 * UpwardSpacing(magnitude) > .0005)
            throw new ArgumentOutOfRangeException("levels", "An X zoom track must retain four double spacings within 0.0005 logical units.");
        _minimum = minimum; _maximum = maximum; _factor = factor; _plot = plot; _reversed = reversed;
        Track = new ChartRect(plot.Left, plot.Top, trackWidth, plot.Height);
        _windowSpan = factor == 1 ? 0 : ResolveWindowSpan(minimum, maximum, factor);
    }

    internal ChartRect Track { get; }
    internal double Travel => Track.Width - _plot.Width;

    /// <summary>Rounds the source window before deriving its native translation; no model or layout participates.</summary>
    internal (ChartAxisProjection Projection, double Delta) At(double panFraction) {
        var minimum = _minimum; var maximum = _maximum; var delta = 0d;
        if (_factor != 1) {
            minimum = ChartMath.InterpolateRange(_minimum, _maximum, panFraction * (1 - 1 / _factor));
            maximum = minimum + _windowSpan;
            var atEnd = panFraction == 1 || maximum >= _maximum;
            if (atEnd) { maximum = _maximum; minimum = maximum - _windowSpan; }
            var anchor = atEnd ? maximum : minimum;
            var edge = atEnd != _reversed ? _plot.Right : _plot.Left;
            delta = edge - ChartMapper.ProjectX(ChartScaleTransform.Normalize(anchor, _minimum, _maximum, _reversed), Track);
        }
        return (new ChartAxisProjection(_minimum, _maximum, minimum, maximum, _plot, new ChartAxis { Reversed = _reversed }, ChartAxisDimension.X, nativeTrack: Track, translation: delta), delta);
    }

    /// <summary>Uses a common source-coordinate grid while retaining the original full-span floating-point precision.</summary>
    private static double ResolveWindowSpan(double minimum, double maximum, double factor) {
        var span = maximum - minimum;
        var finiteSpan = ChartMath.IsFinite(span);
        var width = finiteSpan ? span / factor : maximum / factor - minimum / factor;
        // An overflowing full span still has a finite half-span and a finite absolute rounding budget.
        var budget = finiteSpan ? UpwardSpacing(span) : UpwardSpacing(maximum / 2 - minimum / 2) * 2;
        var magnitude = Math.Max(Math.Abs(minimum), Math.Abs(maximum));
        var spacing = magnitude - BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(magnitude) - 1);
        var represented = Math.Round(width / spacing, MidpointRounding.ToEven) * spacing;
        // Pan may round on the source grid, but its width must retain the original span's arithmetic precision.
        if (!ChartMath.IsFinite(width) || !ChartMath.IsFinite(represented) || represented <= 0
            || Math.Abs(represented - width) > budget || finiteSpan && represented > span)
            throw new ArgumentOutOfRangeException("levels", "An X zoom window must retain the full source span's precision on the source-coordinate grid.");
        return represented;
    }

    private static double UpwardSpacing(double value) {
        var bits = BitConverter.DoubleToInt64Bits(value);
        var next = BitConverter.Int64BitsToDouble(bits + 1);
        return ChartMath.IsFinite(next) ? next - value : value - BitConverter.Int64BitsToDouble(bits - 1);
    }
}
