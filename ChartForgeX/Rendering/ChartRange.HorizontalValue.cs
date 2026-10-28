using System;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartRange {
    /// <summary>Resolves the horizontal secondary value domain while sharing the completed category domain.</summary>
    private static ChartRange FromSecondaryHorizontalAxis(Chart chart, ChartRange primary, ChartStackLayout stacks, bool applyBounds) {
        var range = new ChartRange(); var axis = chart.Options.SecondaryXAxis;
        var baseline = false; var line = false;
        var observed = chart.Series.Any(series => series.ValueAxis == ChartAxisSide.Secondary && series.RenderPoints.Count > 0);
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.ValueAxis != ChartAxisSide.Secondary || observed && series.RenderPoints.Count == 0) continue;
            if (series.Kind == ChartSeriesKind.HorizontalBar) {
                for (var item = 0; item < series.RenderPoints.Count; item++) range.IncludeX(stacks.Point(index, item).End);
                baseline = true;
            } else if (series.Kind == ChartSeriesKind.Waterfall) {
                foreach (var step in ChartWaterfallSteps.Create(series)) {
                    if (step.Start != 0 || UsesZeroBaseline(axis)) range.IncludeX(step.Start);
                    if (step.End != 0 || UsesZeroBaseline(axis)) range.IncludeX(step.End);
                }
                baseline = true;
            } else {
                foreach (var point in series.RenderPoints) range.IncludeX(point.Y);
                foreach (var distribution in series.BoxPlotDistributions) {
                    range.IncludeX(distribution.SourceMinimum); range.IncludeX(distribution.SourceMaximum);
                }
                baseline |= series.Kind == ChartSeriesKind.Lollipop;
                line |= ChartCartesianOrientation.IsHorizontalLine(chart, series) || ChartCartesianOrientation.IsHorizontalComparison(chart, series);
            }
        }
        if (baseline && UsesZeroBaseline(axis)) range.IncludeX(0);
        ValidateLogarithmicObservations(range.MinX, range.MaxX, axis);
        range.InitializeEmptyX(axis);
        if (range.MinX == range.MaxX) (range.MinX, range.MaxX) = ExpandedSingleton(range.MinX, axis);
        if (baseline) range.ApplyLogarithmicXBaseline(axis);
        else if (line) (range.MinX, range.MaxX) = PaddedValueBounds(range.MinX, range.MaxX, axis);
        if (applyBounds) range.ApplyValueBounds(axis, horizontal: true);
        range.ApplyTimeSpacingBounds(axis, horizontal: true, applyBounds);
        range.SetYBounds(primary.MinY, primary.MaxY);
        return range;
    }

    /// <summary>Applies explicit physical value bounds after automatic source fitting.</summary>
    private void ApplyValueBounds(ChartAxis axis, bool horizontal) {
        var min = horizontal ? MinX : MinY; var max = horizontal ? MaxX : MaxY;
        if (axis.Minimum.HasValue) min = axis.Minimum.Value;
        if (axis.Maximum.HasValue) max = axis.Maximum.Value;
        if (max <= min) (min, max) = ResolveClippedBounds(min, max, axis);
        if (horizontal) SetXBounds(min, max); else SetYBounds(min, max);
    }
}
