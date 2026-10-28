using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private readonly List<double?> _pointSliceRadiusRatios = new();
    private IReadOnlyList<double?>? _pointSliceRadiusRatiosView;
    /// <summary>Gets source-ordinal radius overrides. Null entries and unlisted indices retain the full resolved band thickness.</summary>
    /// <remarks>Ratios are finite in [0,1]. Radius is inner+(outer-inner)*ratio, independent of the Y-derived angle and total.
    /// Zero retains source/legend facts but paints no area. Structural edits follow their observations; complete replacement clears overrides.</remarks>
    public IReadOnlyList<double?> PointSliceRadiusRatios => _pointSliceRadiusRatiosView ??= _pointSliceRadiusRatios.AsReadOnly();

    /// <summary>Sets a normalized Pie/Donut observation radius within its resolved band.</summary>
    /// <param name="pointIndex">An existing zero-based source observation.</param>
    /// <param name="ratio">The finite fraction of band thickness, from zero to one.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithPointSliceRadiusRatio(int pointIndex, double ratio) {
        RequireObservationKind(ChartSeriesKind.Pie, ChartSeriesKind.Donut);
        ValidatePointIndex(pointIndex); ChartGuards.UnitInterval(ratio, nameof(ratio));
        while (_pointSliceRadiusRatios.Count <= pointIndex) _pointSliceRadiusRatios.Add(null);
        _pointSliceRadiusRatios[pointIndex] = ratio; return this;
    }
    /// <summary>Clears a radius override and restores the full resolved band thickness for this observation.</summary>
    public ChartSeries UseDefaultSliceRadius(int pointIndex) {
        RequireObservationKind(ChartSeriesKind.Pie, ChartSeriesKind.Donut); ValidatePointIndex(pointIndex);
        if (pointIndex < _pointSliceRadiusRatios.Count) _pointSliceRadiusRatios[pointIndex] = null;
        return this;
    }
    internal double? SliceRadiusRatio(int pointIndex) => pointIndex >= 0 && pointIndex < _pointSliceRadiusRatios.Count ? _pointSliceRadiusRatios[pointIndex] : null;
    internal bool HasSliceRadiusOverrides => _pointSliceRadiusRatios.Exists(value => value.HasValue);
}
