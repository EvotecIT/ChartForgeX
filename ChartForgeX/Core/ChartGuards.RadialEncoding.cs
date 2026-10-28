using System;
using System.Linq;

namespace ChartForgeX.Core;

internal static partial class ChartGuards {
    /// <summary>Validates the area law at the source boundary; empty charts defer family admission until refill.</summary>
    internal static void ValidateRadialValueEncoding(Chart chart) {
        if (chart.Options.RadialGeometry.ValueEncoding != ChartRadialValueEncoding.Area || chart.Series.Count == 0) return;
        if (chart.Series.Any(series => series.Kind != ChartSeriesKind.RadialColumn))
            throw new NotSupportedException("Area radial value encoding requires homogeneous RadialColumn series.");
        foreach (var series in chart.Series) {
            var axis = series.ValueAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            if (axis.Scale != ChartScaleKind.Linear)
                throw new NotSupportedException("Area radial value encoding requires linear selected value axes.");
            if (axis.Minimum.HasValue && axis.Minimum.Value < 0 || axis.Maximum.HasValue && axis.Maximum.Value < 0
                || series.RenderPoints.Any(point => point.Y < 0))
                throw new InvalidOperationException("Area radial value encoding requires nonnegative selected value bounds and observations.");
        }
    }
}
