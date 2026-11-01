using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Maps authored family observations into the shared formatting facts without exposing mutable models.</summary>
internal sealed class ChartValueContexts {
    private readonly Chart _chart;
    private readonly ChartSeries _series;
    private readonly double _partScale;
    private readonly double _partTotal;
    private readonly double[]? _waterfallValues;
    private readonly IReadOnlyDictionary<string, string> _nodeLabels;

    internal ChartValueContexts(Chart chart, ChartSeries series) {
        _chart = chart; _series = series;
        _nodeLabels = series.Kind is ChartSeriesKind.Tree or ChartSeriesKind.Sankey or ChartSeriesKind.Chord
            ? series.Nodes.ToDictionary(node => node.Id, node => node.Label, StringComparer.Ordinal) : new Dictionary<string, string>();
        if (series.IsTypedWaterfall) _waterfallValues = ChartWaterfallSteps.Create(series).Select(step => step.Value).ToArray();
        if (series.Kind is ChartSeriesKind.Pie or ChartSeriesKind.Donut or ChartSeriesKind.PolarArea or ChartSeriesKind.Pyramid) {
            _partScale = series.Points.Select(point => Math.Max(0, point.Y)).DefaultIfEmpty(0).Max();
            if (_partScale > 0) _partTotal = series.Points.Sum(point => Math.Max(0, point.Y) / _partScale);
        }
    }

    internal ChartValueContext CreateRelationship(ChartSeriesInfo info, int index, object datum) {
        string source, target; double value;
        if (datum is ChartFlowLink flow) { source = flow.SourceId; target = flow.TargetId; value = flow.Value; }
        else {
            var branch = (ChartTreeLink)datum;
            source = branch.ParentId; target = branch.ChildId; value = branch.Value;
        }
        return new ChartValueContext(info, index, datum, value, _nodeLabels[source] + " → " + _nodeLabels[target], null);
    }

    internal ChartValueContext Create(ChartSeriesInfo info, int index, object? datum) {
        double? value = null, coordinate = null, percent = null;
        string? category = null;
        switch (datum) {
            case ChartPoint point:
                value = point.Y; coordinate = point.X;
                if (_partTotal > 0) percent = Math.Max(0, point.Y) / _partScale / _partTotal;
                if (_series.Kind == ChartSeriesKind.ProgressRing) percent = point.Y / 100;
                if (_series.Kind == ChartSeriesKind.ProgressBar) percent = Fraction(point.Y, 0, _chart.Options.ProgressMaximum);
                if (_series.Kind == ChartSeriesKind.Funnel && _series.Points.Count > 0 && _series.Points[0].Y > 0)
                    percent = point.Y / _series.Points[0].Y;
                if (_series.HistogramBinLayout != null && !_series.HistogramBins[index].Value.HasValue) value = null;
                if (_series.Kind == ChartSeriesKind.Timeline) { value = null; coordinate = null; category = _series.Name; }
                break;
            case ChartBubble bubble: value = bubble.Y; coordinate = bubble.X; break;
            case ChartErrorBar error: value = error.Y; coordinate = error.X; break;
            case ChartCandlestick candle: coordinate = candle.X; break;
            case ChartRangeBand range: coordinate = range.X; break;
            case ChartInterval interval: coordinate = interval.X; break;
            case ChartDumbbell dumbbell: coordinate = dumbbell.X; break;
            case ChartDifferencePoint difference: coordinate = difference.X; break;
            case ChartBoxPlot box: coordinate = box.X; break;
            case ChartMapPoint map: value = map.Weight; category = map.Label; break;
            case ChartRegionMapItem region: value = region.Value; category = region.Region; break;
            case ChartCalendarHeatmapItem day:
                value = day.Value; category = day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); break;
            case ChartHeatmapCell cell: category = cell.Text ?? cell.State; break;
            case ChartHierarchyItem hierarchy:
                value = _chart.HierarchyPreparation?.Values[index] ?? hierarchy.Value; category = hierarchy.Label; break;
            case ChartNode node: category = node.Label; break;
            case ChartWaterfallItem waterfall: value = _waterfallValues![index]; coordinate = waterfall.X; break;
            case ChartProgressItem progress:
                value = progress.Value; category = progress.Label;
                var maximum = progress.Maximum ?? _chart.Options.ProgressMaximum;
                if (maximum > progress.Minimum) percent = Fraction(progress.Value, progress.Minimum, maximum);
                break;
            case ChartGaugeValue gauge:
                value = gauge.Value; percent = Fraction(gauge.Value, gauge.Minimum, gauge.Maximum); break;
            case ChartBulletValue bullet:
                value = bullet.Value; percent = Fraction(bullet.Value, bullet.Minimum, bullet.Maximum); break;
            case ChartRadialLayer layer:
                value = layer.Value; category = layer.Name; percent = Fraction(layer.Value, layer.Minimum, layer.Maximum); break;
            case ChartGanttItem gantt: percent = gantt.Progress; category = _series.Name; break;
            case ChartStateTimelineSegment segment: category = segment.State; break;
            case ChartGanttLaneItem item: category = item.Label ?? item.Category; break;
        }
        if (coordinate.HasValue) {
            var labels = ChartCartesianOrientation.CategoryLabels(_chart);
            var axis = ChartCartesianOrientation.CategoryAxis(_chart);
            category = ChartAxisValueFormatter.FindExplicitLabel(labels, coordinate.Value)
                ?? ChartAxisValueFormatter.Format(axis, coordinate.Value);
        }
        return new ChartValueContext(info, index, datum, value, category, percent);
    }

    private static double? Fraction(double value, double minimum, double maximum) {
        var scale = Math.Max(Math.Abs(minimum), Math.Max(Math.Abs(maximum), Math.Abs(value)));
        if (!(scale > 0) || maximum <= minimum) return null;
        var fraction = (value / scale - minimum / scale) / (maximum / scale - minimum / scale);
        return ChartMath.IsFinite(fraction) ? fraction : null;
    }
}
