using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>One detached axis transform and confinement owner used by the public X and Y projections.</summary>
internal sealed class ChartViewportAxisProjection {
    private readonly ChartAxis _axis;
    private readonly double _logicalMinimum, _logicalMaximum;
    private readonly bool _logicalReversed;
    internal ChartViewportAxisProjection(double fullMinimum, double fullMaximum, double minimum, double maximum,
        double logicalMinimum, double logicalMaximum, ChartAxis axis, bool vertical) {
        FullMinimum = fullMinimum; FullMaximum = fullMaximum; Minimum = minimum; Maximum = maximum;
        _logicalMinimum = logicalMinimum; _logicalMaximum = logicalMaximum; _logicalReversed = axis.Reversed ^ vertical;
        _axis = new ChartAxis { Scale = axis.Scale, Reversed = axis.Reversed,
            SymmetricLogarithmThreshold = axis.SymmetricLogarithmThreshold, TimeSessions = axis.TimeSessions, TimeSpacing = axis.TimeSpacing, PreparedTimeDomain = axis.PreparedTimeDomain,
            LogarithmBase = axis.LogarithmBase, LogarithmicDomain = axis.LogarithmicDomain, PreparedViewportWindow = axis.PreparedViewportWindow };
        TransformedFullMinimum = ChartScaleTransform.Bound(fullMinimum, _axis, false); TransformedFullMaximum = ChartScaleTransform.Bound(fullMaximum, _axis, true, maximumInclusive: true);
        TransformedMinimum = ChartScaleTransform.Bound(minimum, _axis, false); TransformedMaximum = ChartScaleTransform.Bound(maximum, _axis, true);
    }
    internal double FullMinimum { get; }
    internal double FullMaximum { get; }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal bool Reversed => _axis.Reversed;
    internal bool MaximumInclusive => ChartScaleTransform.MaximumInclusive(_axis, Maximum);
    internal ChartScaleKind Scale => _axis.Scale;
    internal double Threshold => _axis.SymmetricLogarithmThreshold;
    internal double LogarithmBase => _axis.LogarithmBase;
    internal ChartLogarithmicDomain LogarithmicDomain => _axis.LogarithmicDomain;
    internal ChartTimeSessionDomain? TimeSessions => _axis.TimeSessions;
    internal ChartTimeSpacingDomain? TimeDomain => _axis.PreparedTimeDomain;
    internal double TransformedMinimum { get; }
    internal double TransformedMaximum { get; }
    internal double TransformedFullMinimum { get; }
    internal double TransformedFullMaximum { get; }
    private double Forward(double value) => ChartScaleTransform.Forward(value, _axis);
    private double Inverse(double value) => ChartScaleTransform.Inverse(value, _axis);

    internal double Project(double source) {
        ChartPrimitiveGuards.Finite(source, nameof(source));
        if (_axis.Scale == ChartScaleKind.Logarithmic && !_axis.IsInLogarithmicDomain(source))
            throw new ArgumentOutOfRangeException(nameof(source), source, "A logarithmic source coordinate must be " + _axis.LogarithmicDomainName + ".");
        var result = ChartMath.InterpolateOrExtrapolateRange(_logicalMinimum, _logicalMaximum,
            ChartScaleTransform.Normalize(Forward(source), TransformedMinimum, TransformedMaximum, _logicalReversed));
        ChartPrimitiveGuards.Finite(result, nameof(source)); return result;
    }
    internal double Unproject(double logical) {
        var result = Inverse(UnprojectCoordinate(logical));
        ChartPrimitiveGuards.Finite(result, nameof(logical));
        if (_axis.Scale == ChartScaleKind.Logarithmic && !_axis.IsInLogarithmicDomain(result))
            throw new ArgumentOutOfRangeException(nameof(logical), logical, "The logical coordinate cannot map to a representable nonzero logarithmic value.");
        return result;
    }
    private double UnprojectCoordinate(double logical) {
        ChartPrimitiveGuards.Finite(logical, nameof(logical));
        var ratio = ChartMath.Normalize(logical, _logicalMinimum, _logicalMaximum);
        var result = ChartMath.InterpolateOrExtrapolateRange(TransformedMinimum, TransformedMaximum, _logicalReversed ? 1 - ratio : ratio);
        ChartPrimitiveGuards.Finite(result, nameof(logical)); return result;
    }
    internal ChartAxisWindow? ConstrainLogical(double firstLogical, double secondLogical, bool? maximumInclusive = null) {
        var first = UnprojectCoordinate(firstLogical); var second = UnprojectCoordinate(secondLogical);
        var minimum = Math.Min(first, second); var maximum = Math.Max(first, second);
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(secondLogical), secondLogical, "The gesture requires a nonempty axis span.");
        var span = maximum - minimum; var fullSpan = TransformedFullMaximum - TransformedFullMinimum;
        var spanIsFinite = ChartMath.IsFinite(span); var fullSpanIsFinite = ChartMath.IsFinite(fullSpan);
        var halfSpan = maximum / 2 - minimum / 2; var fullHalfSpan = TransformedFullMaximum / 2 - TransformedFullMinimum / 2;
        var inclusive = _axis.PreparedTimeDomain?.Unit == true ? false : maximumInclusive ?? MaximumInclusive;
        if (spanIsFinite && fullSpanIsFinite ? span >= fullSpan : halfSpan >= fullHalfSpan)
            return inclusive || _axis.PreparedTimeDomain?.Unit == true ? null : new ChartAxisWindow(FullMinimum, FullMaximum, maximumInclusive: false);
        if (minimum < TransformedFullMinimum) {
            minimum = TransformedFullMinimum;
            maximum = spanIsFinite ? Math.Min(TransformedFullMaximum, minimum + span)
                : ChartMath.InterpolateRange(TransformedFullMinimum, TransformedFullMaximum, halfSpan / fullHalfSpan);
        } else if (maximum > TransformedFullMaximum) {
            maximum = TransformedFullMaximum;
            minimum = spanIsFinite ? Math.Max(TransformedFullMinimum, maximum - span)
                : ChartMath.InterpolateRange(TransformedFullMinimum, TransformedFullMaximum, 1 - halfSpan / fullHalfSpan);
        }
        var lower = Math.Max(FullMinimum, ChartScaleTransform.InverseBound(minimum, _axis, false));
        var upper = Math.Min(FullMaximum, ChartScaleTransform.InverseBound(maximum, _axis, true));
        ChartPrimitiveGuards.Finite(lower, nameof(firstLogical)); ChartPrimitiveGuards.Finite(upper, nameof(secondLogical));
        if (upper <= lower) throw new ArgumentOutOfRangeException(nameof(secondLogical), "The confined axis window is not representable.");
        // An authored inclusive full edge may fall inside a unit. Preserve that exact
        // bound when confinement reaches it; only canonical gesture edges are exclusive.
        if (_axis.PreparedTimeDomain is { Unit: true } calendar && !calendar.IsBoundary(upper)) inclusive = true;
        return new ChartAxisWindow(lower, upper, inclusive);
    }
    internal ChartAxisWindow? ConstrainSource(ChartAxisWindow requested) {
        var minimum = requested.Minimum; var maximum = requested.Maximum;
        _axis.PreparedTimeDomain?.ValidateMaximum(requested, nameof(requested));
        if (_axis.Scale == ChartScaleKind.Logarithmic && (!_axis.IsInLogarithmicDomain(minimum) || !_axis.IsInLogarithmicDomain(maximum)))
            throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Logarithmic source coordinates must be " + _axis.LogarithmicDomainName + ".");
        if (ChartScaleTransform.Bound(maximum, _axis, true, requested.MaximumInclusive) <= ChartScaleTransform.Bound(minimum, _axis, false))
            throw new ArgumentOutOfRangeException(nameof(maximum), "The window has no projected width.");
        if (minimum >= FullMinimum && maximum <= FullMaximum)
            return minimum == FullMinimum && maximum == FullMaximum && requested.MaximumInclusive ? null : requested;
        double BoundLogical(double source, bool upper) => ChartMath.InterpolateOrExtrapolateRange(_logicalMinimum, _logicalMaximum,
            ChartScaleTransform.Normalize(ChartScaleTransform.Bound(source, _axis, upper, requested.MaximumInclusive), TransformedMinimum, TransformedMaximum, _logicalReversed));
        return ConstrainLogical(BoundLogical(minimum, false), BoundLogical(maximum, true), requested.MaximumInclusive);
    }
}
