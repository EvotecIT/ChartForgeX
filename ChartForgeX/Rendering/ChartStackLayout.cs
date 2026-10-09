using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Compiles stack membership, source-independent geometry, slot identities and totals once.
/// Bounds, marks and labels consume the same snapshot rather than recalculating stack rules.
/// </summary>
internal sealed partial class ChartStackLayout {
    private readonly ChartStackPoint[][] _points;
    private readonly double[][] _sourceValues;
    private readonly StackGroup[] _seriesGroups;
    private readonly ChartStackTotal[] _totals;

    private ChartStackLayout(ChartStackPoint[][] points, double[][] sourceValues, StackGroup[] seriesGroups, ChartStackTotal[] totals, int zeroTotalCount) {
        _points = points; _sourceValues = sourceValues; _seriesGroups = seriesGroups; _totals = totals; ZeroTotalCount = zeroTotalCount;
    }

    internal IReadOnlyList<ChartStackTotal> Totals => _totals;
    internal int ZeroTotalCount { get; }
    internal bool HasBarStacks => _seriesGroups.Any(group => group != null && group.Stacked && group.Kind != ChartSeriesKind.StackedArea);
    internal ChartStackPoint Point(int seriesIndex, int pointIndex) => _points[seriesIndex].Length > 0
        ? _points[seriesIndex][pointIndex] : new ChartStackPoint(_sourceValues[seriesIndex][pointIndex]);
    internal bool IsStacked(int seriesIndex) => _seriesGroups[seriesIndex]?.Stacked == true;
    internal static bool Participates(Chart chart, ChartSeries series) => Supports(series.Kind)
        && (series.Kind == ChartSeriesKind.StackedArea || series.StackGroup != null || chart.Options.BarMode == ChartBarMode.Stacked);

    internal static ChartStackLayout Create(Chart chart, ChartBarCoordinateMap coordinates) {
        var points = chart.Series.Select(_ => Array.Empty<ChartStackPoint>()).ToArray();
        var sourceValues = chart.Series.Select(_ => Array.Empty<double>()).ToArray();
        var groups = new List<StackGroup>();
        var seriesGroups = new StackGroup[chart.Series.Count];
        var coordinateIds = CoordinateIds(chart, coordinates);
        var buckets = new Dictionary<(int Group, int Coordinate, bool Positive), StackBucket>();
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (!Supports(series.Kind)) { sourceValues[seriesIndex] = series.Points.Select(point => point.Y).ToArray(); continue; }
            var stacked = Participates(chart, series);
            if (!stacked && series.NormalizedTo.HasValue)
                throw new InvalidOperationException("Stack normalization requires stacked bars or an explicit StackGroup.");
            var group = groups.FirstOrDefault(candidate => candidate.Matches(series, stacked));
            if (group == null) {
                group = new StackGroup(groups.Count, seriesIndex, series, stacked);
                groups.Add(group);
            } else group.Validate(series);
            seriesGroups[seriesIndex] = group;
            if (stacked) points[seriesIndex] = new ChartStackPoint[series.Points.Count];
            else sourceValues[seriesIndex] = series.Points.Select(point => point.Y).ToArray();
            for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
                var point = series.Points[pointIndex];
                if (!stacked) continue;
                var key = (group.Id, coordinateIds[seriesIndex][pointIndex], point.Y >= 0);
                if (!buckets.TryGetValue(key, out var bucket)) {
                    bucket = new StackBucket(group, seriesIndex, pointIndex, point.X, point.Y >= 0);
                    buckets.Add(key, bucket);
                }
                bucket.Add(seriesIndex, pointIndex, point.Y);
            }
        }

        var totals = new List<ChartStackTotal>(); var zeroTotalCount = 0;
        foreach (var bucket in buckets.Values.OrderBy(value => value.Group.Kind).ThenBy(value => value.Group.Axis)
            .ThenBy(value => value.Coordinate).ThenBy(value => value.Group.Id).ThenByDescending(value => value.Positive)) {
            var target = bucket.Group.NormalizedTo;
            var total = target.HasValue && bucket.SourceTotal != 0 ? (bucket.Positive ? target.Value : -target.Value) : bucket.SourceTotal;
            var baseline = 0d;
            var lastNonzero = bucket.Points.FindLastIndex(point => point.Value != 0);
            for (var index = 0; index < bucket.Points.Count; index++) {
                var source = bucket.Points[index];
                var value = !target.HasValue || bucket.SourceTotal == 0 ? source.Value : source.Value / Math.Abs(bucket.SourceTotal) * target.Value;
                var end = target.HasValue && index == lastNonzero ? total : baseline + value;
                if (!ChartMath.IsFinite(end)) throw new InvalidOperationException("Stack values exceed the finite rendering range.");
                value = end - baseline;
                points[source.Series][source.Point] = new ChartStackPoint(value, baseline, total, bucket.SourceTotal, bucket.Group.Key, target);
                baseline = end;
            }
            if (target.HasValue && bucket.SourceTotal == 0) zeroTotalCount++;
            totals.Add(new ChartStackTotal(bucket.SeriesIndex, bucket.PointIndex, bucket.Group.Kind, bucket.Group.Axis,
                bucket.Group.Key, bucket.Coordinate, bucket.Positive, total, bucket.SourceTotal, target));
        }
        return new ChartStackLayout(points, sourceValues, seriesGroups, totals.ToArray(), zeroTotalCount);
    }

    /// <summary>Resolves distinct stack or standalone-series slots within the supplied physical participants.</summary>
    internal (int Count, int Position) Slot(IReadOnlyList<int> participants, int seriesIndex) {
        var identities = new List<StackGroup>();
        foreach (var participant in participants) {
            var group = _seriesGroups[participant];
            if (group != null && !identities.Contains(group)) identities.Add(group);
        }
        return (Math.Max(1, identities.Count), Math.Max(0, identities.IndexOf(_seriesGroups[seriesIndex])));
    }

    private static bool Supports(ChartSeriesKind kind) => kind == ChartSeriesKind.Bar || kind == ChartSeriesKind.HorizontalBar || kind == ChartSeriesKind.StackedArea
        || kind == ChartSeriesKind.RadialBar || kind == ChartSeriesKind.RadialColumn;

    private sealed class StackGroup {
        internal StackGroup(int id, int seriesIndex, ChartSeries series, bool stacked) {
            Id = id; SeriesIndex = seriesIndex; Kind = series.Kind; Axis = series.YAxis;
            Key = series.StackGroup; NormalizedTo = series.NormalizedTo; Stacked = stacked;
            Interpolation = series.Interpolation; StepPosition = series.StepPosition;
        }
        internal int Id { get; }
        internal int SeriesIndex { get; }
        internal ChartSeriesKind Kind { get; }
        internal ChartAxisSide Axis { get; }
        internal string? Key { get; }
        internal double? NormalizedTo { get; }
        internal bool Stacked { get; }
        private ChartInterpolation Interpolation { get; }
        private ChartStepPosition StepPosition { get; }
        internal bool Matches(ChartSeries series, bool stacked) => Stacked && stacked && Kind == series.Kind && Axis == series.YAxis
            && string.Equals(Key, series.StackGroup, StringComparison.Ordinal);
        internal void Validate(ChartSeries series) {
            if (NormalizedTo != series.NormalizedTo)
                throw new InvalidOperationException("Every series in the same stack must use the same NormalizedTo target, including null for source units.");
            if (Kind == ChartSeriesKind.StackedArea && (Interpolation != series.Interpolation || StepPosition != series.StepPosition))
                throw new InvalidOperationException("Every series in the same stacked-area group and axis must use the same Interpolation and StepPosition so shared boundaries align.");
        }
    }

    private sealed class StackBucket {
        internal StackBucket(StackGroup group, int seriesIndex, int pointIndex, double coordinate, bool positive) {
            Group = group; SeriesIndex = seriesIndex; PointIndex = pointIndex; Coordinate = coordinate; Positive = positive;
        }
        internal StackGroup Group { get; }
        internal int SeriesIndex { get; }
        internal int PointIndex { get; }
        internal double Coordinate { get; }
        internal bool Positive { get; }
        internal double SourceTotal { get; private set; }
        internal List<(int Series, int Point, double Value)> Points { get; } = new();
        internal void Add(int series, int point, double value) {
            SourceTotal += value;
            if (!ChartMath.IsFinite(SourceTotal)) throw new InvalidOperationException("Stack totals exceed the finite rendering range.");
            Points.Add((series, point, value));
        }
    }
}

/// <summary>A source observation's compiled contribution and baseline in resolved stack units.</summary>
internal readonly struct ChartStackPoint {
    internal ChartStackPoint(double value) { Value = value; Base = 0; Total = value; SourceTotal = value; Group = null; NormalizedTo = null; IsStacked = false; }
    internal ChartStackPoint(double value, double baseline, double total, double sourceTotal, string? group, double? normalizedTo) {
        Value = value; Base = baseline; Total = total; SourceTotal = sourceTotal; Group = group; NormalizedTo = normalizedTo; IsStacked = true;
    }
    internal double Value { get; }
    internal double Base { get; }
    internal double End => Base + Value;
    internal double Total { get; }
    internal double SourceTotal { get; }
    internal string? Group { get; }
    internal double? NormalizedTo { get; }
    internal bool IsStacked { get; }
}

/// <summary>A signed total of one stack at a single coordinate, with its original source total.</summary>
internal sealed class ChartStackTotal {
    internal ChartStackTotal(int seriesIndex, int pointIndex, ChartSeriesKind kind, ChartAxisSide axis, string? group,
        double coordinate, bool positive, double value, double sourceValue, double? normalizedTo) {
        SeriesIndex = seriesIndex; PointIndex = pointIndex; Kind = kind; Axis = axis; Group = group;
        Coordinate = coordinate; Positive = positive; Value = value; SourceValue = sourceValue; NormalizedTo = normalizedTo;
    }
    internal int SeriesIndex { get; }
    internal int PointIndex { get; }
    internal ChartSeriesKind Kind { get; }
    internal ChartAxisSide Axis { get; }
    internal string? Group { get; }
    internal double Coordinate { get; }
    internal bool Positive { get; }
    internal double Value { get; }
    internal double SourceValue { get; }
    internal double? NormalizedTo { get; }
}
