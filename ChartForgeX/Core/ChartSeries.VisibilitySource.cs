using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private bool _visibilityPreparation;
    internal bool IsVisibilityPreparation => _visibilityPreparation;
    internal ChartVisibilitySource.Series CaptureVisibilitySource() => new(this);

    internal static ChartSeries FromVisibilitySource(ChartVisibilitySource.Series source, ChartVisibilityView? view, IReadOnlyList<int>? selectedIndices = null, double[]? values = null) {
        var selected = (selectedIndices ?? source.VisibleIndices(view)).ToArray();
        var selectedSet = new HashSet<int>(selected); var numeric = new List<ChartPoint>(); var breakBefore = false;
        if (source.Kind != ChartSeriesKind.BoxPlot) for (var index = 0; index < source.Points.Count; index++) {
            var point = source.Points[index]; breakBefore |= point.BreakBefore;
            if (!selectedSet.Contains(index)) continue;
            numeric.Add(new ChartPoint(point.X, values == null ? point.Y : values[index], source.Kind == ChartSeriesKind.Line ? breakBefore : point.BreakBefore)); breakBefore = false;
        }
        var model = new ChartSeries(source.Id, source.Name, source.Kind, numeric) {
            _visibilityPreparation = true, Color = source.Color, StateRole = source.State, FillPattern = source.Pattern,
            ValueAxis = source.ValueAxis, ShowInLegend = source.ShowInLegend, SemanticRole = source.SemanticRole,
            ShowDataLabels = source.Labels, DataLabelPlacement = source.Placement, DataLabelStyle = source.CopyLabelStyle(), Markers = source.CopyMarkers(),
            Interpolation = source.Interpolation, StepPosition = source.Step
        };
        if (source.IsHierarchy) model.RestoreVisibilityHierarchy(source, values);
        if (source.StrokeWidth.HasValue) model.StrokeWidth = source.StrokeWidth.Value;
        model.StackGroup = source.StackGroup; model.NormalizedTo = source.NormalizedTo; model.StackedAreaMissingData = source.StackedAreaMissingData;
        model.RadialBand = source.RadialBand;
        if (source.Kind == ChartSeriesKind.BoxPlot) {
            if (source.Raw) {
                var groups = selected.Select(index => source.RawGroups[index]).ToArray(); var statistics = source.CopyStatistics()!;
                var distributions = ChartBoxPlotStatistics.Calculate(groups, statistics);
                var originalSamples = new List<int>(); var start = 0;
                for (var group = 0; group < source.RawGroups.Count; group++) {
                    if (selectedSet.Contains(group)) originalSamples.AddRange(Enumerable.Range(start, source.RawGroups[group].Samples.Count));
                    start += source.RawGroups[group].Samples.Count;
                }
                model.InstallBoxPlotRaw(Array.AsReadOnly(groups), distributions.Select((row, index) => new ChartBoxPlotDistribution(selected[index], row.Summary,
                    row.SourceMinimum, row.SourceMaximum, row.InlierMinimum, row.InlierMaximum,
                    row.SourceIndices.Select(sample => originalSamples[sample]).ToArray(), row.OutlierSourceIndices.Select(sample => originalSamples[sample]).ToArray(), statistics)).ToArray(), statistics);
                // Calculated provenance addresses the retained original measurement array, including hidden groups.
                model.BoxPlotSourceSamples = Array.AsReadOnly(source.RawSamples.ToArray());
                foreach (var pair in source.GroupIds) model._boxPlotGroupIds.Add(pair.Key, pair.Value);
            } else model._boxPlots = Array.AsReadOnly(selected.Select(index => source.Boxes[index]).ToArray());
        }
        // Sparse display policies follow their actual source observation/group into the visible native model.
        foreach (var index in selected) {
            model.PointColors.Add(index < source.PointColors.Count ? source.PointColors[index] : null);
            model.PointFillPatterns.Add(index < source.PointPatterns.Count ? source.PointPatterns[index] : null);
            model.PointLabels.Add(index < source.PointLabels.Count ? source.PointLabels[index] : null);
            model.PointDataLabelStyles.Add(source.CopyPointStyle(index));
            if (ChartSeriesKindTraits.IsPieLikeKind(source.Kind)) {
                model.PointSliceOffsets.Add(index < source.SliceOffsets.Count ? source.SliceOffsets[index] : 0);
                model._pointSliceRadiusRatios.Add(index < source.SliceRadiusRatios.Count ? source.SliceRadiusRatios[index] : null);
            }
        }
        model.SourcePointCount = source.IsHierarchy ? 0 : source.SourceCount;
        model.SourcePointIndices = source.IsHierarchy || source.Raw ? Array.Empty<int>() : Array.AsReadOnly(selected);
        foreach (var pair in source.ObservationIds) model._observationIds.Add(pair.Key, pair.Value);
        return model;

    }
}
