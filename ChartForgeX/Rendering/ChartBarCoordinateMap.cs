using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Assigns one stable coordinate key to corresponding vertical bar points while preserving
/// distinct histogram bins. The map is built once per chart operation so stacking, range
/// calculation, and total labels share the same identity without repeated layout scans.
/// </summary>
internal sealed class ChartBarCoordinateMap {
    private readonly ChartBarCoordinateKey[][] _keys;
    private readonly Dictionary<int, HistogramSlot> _histogramSlots;
    private readonly Dictionary<int, int[]> _seriesIndices;

    private ChartBarCoordinateMap(ChartBarCoordinateKey[][] keys, Dictionary<int, HistogramSlot> histogramSlots, Dictionary<int, int[]> seriesIndices) {
        _keys = keys;
        _histogramSlots = histogramSlots;
        _seriesIndices = seriesIndices;
    }

    internal static ChartBarCoordinateMap Create(Chart chart) {
        var keys = chart.Series.Select(series => new ChartBarCoordinateKey[series.Points.Count]).ToArray();
        var nodes = CreateNodes(chart).OrderBy(node => node.Coordinate).ThenBy(node => node.SeriesIndex).ThenBy(node => node.PointIndex).ToArray();
        var activeGroups = new List<CoordinateGroup>();
        var allGroups = new List<CoordinateGroup>();
        var nextId = 0;
        foreach (var node in nodes) {
            activeGroups.RemoveAll(group => !ChartMath.SameCoordinate(group.LastCoordinate, node.Coordinate));
            var group = FindBestGroup(activeGroups, node);
            if (group == null) {
                group = new CoordinateGroup(new ChartBarCoordinateKey(nextId++, node.Coordinate));
                activeGroups.Add(group);
                allGroups.Add(group);
            }

            group.Add(node);
            keys[node.SeriesIndex][node.PointIndex] = group.Key;
        }

        var histogramSlots = allGroups
            .Where(group => group.RepresentativeHistogramNode.HasValue)
            .ToDictionary(group => group.Key.Id, group => new HistogramSlot(group.RepresentativeHistogramNode!.Value.Layout!, group.RepresentativeHistogramNode.Value.BinIndex));
        var seriesIndices = allGroups.ToDictionary(group => group.Key.Id, group => group.SeriesIndices.OrderBy(index => index).ToArray());
        return new ChartBarCoordinateMap(keys, histogramSlots, seriesIndices);
    }

    internal ChartBarCoordinateKey Resolve(int seriesIndex, int pointIndex) => _keys[seriesIndex][pointIndex];

    internal IReadOnlyList<int> SeriesIndices(int seriesIndex, int pointIndex) => _seriesIndices[Resolve(seriesIndex, pointIndex).Id];

    internal bool TryResolveHistogramSlot(int seriesIndex, int pointIndex, out ChartHistogramBinLayout layout, out int binIndex) {
        if (_histogramSlots.TryGetValue(Resolve(seriesIndex, pointIndex).Id, out var slot)) {
            layout = slot.Layout;
            binIndex = slot.BinIndex;
            return true;
        }

        layout = null!;
        binIndex = -1;
        return false;
    }

    private static IEnumerable<CoordinateNode> CreateNodes(Chart chart) {
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.Kind != ChartSeriesKind.Bar) continue;
            for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
                var layout = series.HistogramBinLayout;
                var isHistogramBin = layout != null && layout.Minimum != layout.Maximum && pointIndex < layout.Count;
                yield return new CoordinateNode(seriesIndex, pointIndex, series.Points[pointIndex].X, isHistogramBin ? layout : null, pointIndex);
            }
        }
    }

    private static CoordinateGroup? FindBestGroup(List<CoordinateGroup> groups, CoordinateNode node) {
        CoordinateGroup? best = null;
        var bestRank = int.MaxValue;
        var bestDistance = double.PositiveInfinity;
        foreach (var group in groups) {
            if (!group.TryGetCompatibility(node, out var rank, out var distance)) continue;
            if (rank > bestRank || rank == bestRank && distance >= bestDistance) continue;
            best = group;
            bestRank = rank;
            bestDistance = distance;
        }

        return best;
    }

    private sealed class CoordinateGroup {
        private readonly HashSet<int> _seriesIndices = new HashSet<int>();
        private CoordinateNode? _lastHistogramNode;

        internal CoordinateGroup(ChartBarCoordinateKey key) {
            Key = key;
            LastCoordinate = key.Value;
        }

        internal ChartBarCoordinateKey Key { get; }

        internal double LastCoordinate { get; private set; }

        internal IEnumerable<int> SeriesIndices => _seriesIndices;

        internal CoordinateNode? RepresentativeHistogramNode { get; private set; }

        internal void Add(CoordinateNode node) {
            _seriesIndices.Add(node.SeriesIndex);
            LastCoordinate = node.Coordinate;
            if (node.Layout != null) {
                if (!RepresentativeHistogramNode.HasValue) RepresentativeHistogramNode = node;
                _lastHistogramNode = node;
            }
        }

        internal bool TryGetCompatibility(CoordinateNode node, out int rank, out double distance) {
            rank = int.MaxValue;
            distance = double.PositiveInfinity;
            if (_seriesIndices.Contains(node.SeriesIndex) || !ChartMath.SameCoordinate(LastCoordinate, node.Coordinate)) return false;

            if (node.Layout != null && _lastHistogramNode.HasValue) {
                var histogramNode = _lastHistogramNode.Value;
                if (EquivalentLayouts(node.Layout, histogramNode.Layout!)) {
                    if (node.BinIndex != histogramNode.BinIndex) return false;
                } else if (!EquivalentBounds(node, histogramNode)) {
                    return false;
                }

                rank = 0;
                distance = BoundDistance(node, histogramNode);
                return true;
            }

            rank = node.Layout == null && !_lastHistogramNode.HasValue ? 0 : 1;
            distance = Math.Abs(node.Coordinate - LastCoordinate);
            return true;
        }
    }

    private readonly struct CoordinateNode {
        internal CoordinateNode(int seriesIndex, int pointIndex, double coordinate, ChartHistogramBinLayout? layout, int binIndex) {
            SeriesIndex = seriesIndex;
            PointIndex = pointIndex;
            Coordinate = coordinate;
            Layout = layout;
            BinIndex = binIndex;
        }

        internal int SeriesIndex { get; }
        internal int PointIndex { get; }
        internal double Coordinate { get; }
        internal ChartHistogramBinLayout? Layout { get; }
        internal int BinIndex { get; }
    }

    private readonly struct HistogramSlot {
        internal HistogramSlot(ChartHistogramBinLayout layout, int binIndex) {
            Layout = layout;
            BinIndex = binIndex;
        }

        internal ChartHistogramBinLayout Layout { get; }
        internal int BinIndex { get; }
    }

    private static bool EquivalentLayouts(ChartHistogramBinLayout left, ChartHistogramBinLayout right) =>
        left.Count == right.Count && ChartMath.SameCoordinate(left.Minimum, right.Minimum) &&
        ChartMath.SameCoordinate(left.Maximum, right.Maximum) && ChartMath.SameCoordinate(left.Width, right.Width);

    private static bool EquivalentBounds(CoordinateNode left, CoordinateNode right) =>
        ChartMath.SameCoordinate(left.Layout!.GetLowerBound(left.BinIndex), right.Layout!.GetLowerBound(right.BinIndex)) &&
        ChartMath.SameCoordinate(left.Layout.GetUpperBound(left.BinIndex), right.Layout.GetUpperBound(right.BinIndex));

    private static double BoundDistance(CoordinateNode left, CoordinateNode right) =>
        Math.Abs(left.Layout!.GetLowerBound(left.BinIndex) - right.Layout!.GetLowerBound(right.BinIndex)) +
        Math.Abs(left.Layout.GetUpperBound(left.BinIndex) - right.Layout.GetUpperBound(right.BinIndex));
}

internal readonly struct ChartBarCoordinateKey : IEquatable<ChartBarCoordinateKey> {
    internal ChartBarCoordinateKey(int id, double value) {
        Id = id;
        Value = value;
    }

    internal int Id { get; }
    internal double Value { get; }

    public bool Equals(ChartBarCoordinateKey other) => Id == other.Id;
    public override bool Equals(object? obj) => obj is ChartBarCoordinateKey other && Equals(other);
    public override int GetHashCode() => Id;
}
