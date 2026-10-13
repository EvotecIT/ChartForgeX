using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    internal bool UsesTypedObservations => Kind is ChartSeriesKind.Bubble or ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc
        or ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea or ChartSeriesKind.RangeBar or ChartSeriesKind.ErrorBar
        or ChartSeriesKind.BoxPlot or ChartSeriesKind.Dumbbell or ChartSeriesKind.DifferenceArea
        or ChartSeriesKind.Gantt or ChartSeriesKind.Gauge or ChartSeriesKind.Circle or ChartSeriesKind.Bullet or ChartSeriesKind.LayeredRadial;

    private IReadOnlyList<ChartPoint>? _renderPoints;

    // The projection is disposable derived data. Every replacement/update invalidates it; callers cannot mutate it.
    internal IReadOnlyList<ChartPoint> RenderPoints => UsesTypedObservations ? _renderPoints ??= ProjectObservations() : _points;

    private IReadOnlyList<ChartPoint> ProjectObservations() {
        var points = new List<ChartPoint>();
        foreach (var value in Bubbles) {
            points.Add(new ChartPoint(value.X, value.Y).WithColorValue(value.ColorValue)); points.Add(new ChartPoint(value.X, value.Size));
        }
        foreach (var value in FinancialValues) {
            points.Add(new ChartPoint(value.X, value.Open)); points.Add(new ChartPoint(value.X, value.High));
            points.Add(new ChartPoint(value.X, value.Low)); points.Add(new ChartPoint(value.X, value.Close));
        }
        foreach (var value in Ranges) {
            points.Add(new ChartPoint(value.X, value.Lower, value.BreakBefore)); points.Add(new ChartPoint(value.X, value.Upper, value.BreakBefore));
        }
        foreach (var value in Intervals) { points.Add(new ChartPoint(value.X, value.Start)); points.Add(new ChartPoint(value.X, value.End)); }
        foreach (var value in ErrorBars) {
            points.Add(new ChartPoint(value.X, value.Y)); points.Add(new ChartPoint(value.X, value.Lower)); points.Add(new ChartPoint(value.X, value.Upper));
        }
        foreach (var value in BoxPlots) {
            points.Add(new ChartPoint(value.X, value.Minimum)); points.Add(new ChartPoint(value.X, value.Q1)); points.Add(new ChartPoint(value.X, value.Median));
            points.Add(new ChartPoint(value.X, value.Q3)); points.Add(new ChartPoint(value.X, value.Maximum));
        }
        foreach (var value in Dumbbells) { points.Add(new ChartPoint(value.X, value.Start)); points.Add(new ChartPoint(value.X, value.End)); }
        foreach (var value in DifferencePoints) {
            points.Add(new ChartPoint(value.X, value.First, value.BreakBefore)); points.Add(new ChartPoint(value.X, value.Second, value.BreakBefore));
        }
        foreach (var value in GanttItems) {
            points.Add(new ChartPoint(value.Start, value.End)); points.Add(new ChartPoint(value.Progress, value.DependsOn));
            points.Add(new ChartPoint(value.IsMilestone ? 1 : 0, value.IsVerticalMarker ? 1 : 0));
        }
        foreach (var value in GaugeValues) { points.Add(new ChartPoint(value.Minimum, value.Value)); points.Add(new ChartPoint(value.Maximum, value.Value)); }
        foreach (var value in BulletValues) {
            points.Add(new ChartPoint(value.Minimum, value.Value)); points.Add(new ChartPoint(value.Maximum, value.Target));
            foreach (var boundary in value.RangeEnds) points.Add(new ChartPoint(boundary, boundary));
        }
        return points.AsReadOnly();
    }
}
