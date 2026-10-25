using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static void ApplyLayered(TopologyChart chart, TopologyRenderOptions? options, bool inferRoutes = true, bool ranksPrepared = false, LayeredResponsibilityBands? responsibilityBands = null) {
        // Fully automatic grouped diagrams follow their dependency graph before shells are packed;
        // otherwise unrelated kind defaults can put a database before the service that calls it.
        var automaticGroups = chart.Groups.Count >= 2 && chart.Groups.All(group => group.X == 0 && group.Y == 0 && group.Width <= 0 && group.Height <= 0)
            && chart.Nodes.All(node => IsUnset(node.X) && IsUnset(node.Y));
        var automaticLayout = chart.Groups.All(group => group.X == 0 && group.Y == 0 && group.Width <= 0 && group.Height <= 0)
            && chart.Nodes.All(node => IsUnset(node.X) && IsUnset(node.Y));
        var horizontalAutomaticLayout = automaticLayout
            && (chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft);
        if (!ranksPrepared) ApplyEdgeRankHints(chart, force: automaticGroups || horizontalAutomaticLayout || options?.LayeredOptimizeRanksByCrossings == true,
            preserveAuthoredRanks: options?.LayeredOptimizeRanksByCrossings == true, optimizeCrossings: options?.LayeredOptimizeRanksByCrossings == true);
        var nodes = chart.Nodes;
        var layers = OrderLayeredGroups(nodes.GroupBy(node => GetLayer(node)).OrderBy(group => group.Key), chart.HierarchyPreparation?.View);
        var pad = Math.Max(24, chart.Viewport.Padding);
        var top = pad + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
        if (chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft) {
            ApplyLayeredLeftToRight(chart, layers, pad, top, options, inferRoutes, responsibilityBands);
            if (chart.LayoutDirection == TopologyLayoutDirection.RightToLeft) MirrorLayoutHorizontally(chart, pad, chart.Viewport.Width - pad);
            return;
        }

        ApplyLayeredTopToBottom(chart, layers, pad, top, options, inferRoutes, responsibilityBands);
        if (chart.LayoutDirection == TopologyLayoutDirection.BottomToTop) MirrorLayoutVertically(chart, top, chart.Viewport.Height - pad - LegendReservedHeight(chart.Legend, chart.Viewport));
    }

    private static void ApplyLayeredTopToBottom(TopologyChart chart, IReadOnlyList<LayerNodeGroup> layers, double pad, double top, TopologyRenderOptions? options, bool inferRoutes, LayeredResponsibilityBands? responsibilityBands) {
        var nodeSpacing = options?.LayeredNodeSpacing ?? 28;
        var rankSpacing = LayeredRankGap(chart, options, horizontal: false);
        var availableW = Math.Max(140, chart.Viewport.Width - pad * 2);
        var availableH = Math.Max(100, chart.Viewport.Height - top - pad - LegendReservedHeight(chart.Legend, chart.Viewport));
        var prepared = layers.Select(group => {
            var maxWidth = group.Nodes.Select(node => LayeredNodeWidth(chart, node)).DefaultIfEmpty(120).Max();
            var maxHeight = group.Nodes.Select(node => LayeredNodeHeight(chart, node)).DefaultIfEmpty(60).Max();
            var columns = responsibilityBands == null ? LayerColumns(group.Nodes.Count, availableW, maxWidth, nodeSpacing) : group.Nodes.Count;
            var rows = (int)Math.Ceiling(group.Nodes.Count / (double)columns);
            var height = rows * maxHeight + Math.Max(0, rows - 1) * (nodeSpacing + 6);
            return new LayerPlacement(group.Index, group.Layer, group.Nodes, columns, rows, maxWidth, maxHeight, height);
        }).ToList();
        var totalHeight = prepared.Sum(layer => layer.BlockSize);
        var layerGap = prepared.Count <= 1 ? 0 : options?.FitContentToViewport == true
            ? rankSpacing
            : Math.Max(rankSpacing, (availableH - totalHeight) / (prepared.Count - 1));
        var nodesById = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var lanes = LongEdgeLanePlan.Create(chart, prepared, false, availableW, nodeSpacing + 18);
        var y = top;

        for (var layerIndex = 0; layerIndex < prepared.Count; layerIndex++) {
            var layer = prepared[layerIndex];
            var gapAfter = LayerGapAfter(prepared, layerIndex, layerGap, rankSpacing);
            if (TryPlaceHierarchyBucketsTopToBottom(chart, layer, nodesById, pad, availableW, y, out var hierarchyHeight)) {
                lanes.Reject(layerIndex);
                lanes.RecordRank(layerIndex, y, y + Math.Max(layer.BlockSize, hierarchyHeight));
                y += Math.Max(layer.BlockSize, hierarchyHeight) + gapAfter;
                continue;
            }

            lanes.RecordRank(layerIndex, y, y + layer.BlockSize);
            if (responsibilityBands == null && lanes.HasLanes(layerIndex)) {
                lanes.Place(chart, layer, layerIndex, false, pad, availableW, y, nodeSpacing + 18, nodesById);
                y += layer.BlockSize + gapAfter;
                continue;
            }

            var cellW = availableW / layer.Columns;
            for (var i = 0; i < layer.Nodes.Count; i++) {
                var node = layer.Nodes[i];
                var row = i / layer.Columns;
                var col = i % layer.Columns;
                var rowCount = Math.Min(layer.Columns, layer.Nodes.Count - row * layer.Columns);
                var rowLeft = pad + (availableW - rowCount * cellW) / 2;
                if (responsibilityBands != null) { row = 0; col = responsibilityBands.Index(node); }
                ApplyLayerMetadata(node, layer, row, col);
                if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
                node.X = responsibilityBands == null ? rowLeft + col * cellW + (cellW - node.Width) / 2 : pad + responsibilityBands.Offset(node);
                node.Y = y + row * (layer.MaxHeight + nodeSpacing + 6) + (layer.MaxHeight - LayeredNodeHeight(chart, node)) / 2 + TopologyNodeFootprint.TopInset(chart, node);
            }

            y += layer.BlockSize + gapAfter;
        }

        if (inferRoutes) { lanes.ApplyRoutes(nodesById, false); ApplyHierarchyEdgeRoutesTopToBottom(chart); }
    }

    private static void ApplyLayeredLeftToRight(TopologyChart chart, IReadOnlyList<LayerNodeGroup> layers, double pad, double top, TopologyRenderOptions? options, bool inferRoutes, LayeredResponsibilityBands? responsibilityBands) {
        var nodeSpacing = options?.LayeredNodeSpacing ?? 24;
        var rankSpacing = LayeredRankGap(chart, options, horizontal: true);
        var availableW = Math.Max(120, chart.Viewport.Width - pad * 2);
        var availableH = Math.Max(100, chart.Viewport.Height - top - pad - LegendReservedHeight(chart.Legend, chart.Viewport));
        var prepared = layers.Select(group => {
            var maxWidth = group.Nodes.Select(node => LayeredNodeWidth(chart, node)).DefaultIfEmpty(120).Max();
            var maxHeight = group.Nodes.Select(node => LayeredNodeHeight(chart, node)).DefaultIfEmpty(60).Max();
            var rows = responsibilityBands == null ? LayerColumns(group.Nodes.Count, availableH, maxHeight, nodeSpacing) : group.Nodes.Count;
            var columns = (int)Math.Ceiling(group.Nodes.Count / (double)rows);
            var width = columns * maxWidth + Math.Max(0, columns - 1) * (nodeSpacing + 18);
            return new LayerPlacement(group.Index, group.Layer, group.Nodes, columns, rows, maxWidth, maxHeight, width);
        }).ToList();
        var totalWidth = prepared.Sum(layer => layer.BlockSize);
        var minimumGap = options?.LayeredRankSpacing.HasValue == true ? rankSpacing + 14 : Math.Max(56, rankSpacing);
        var layerGap = prepared.Count <= 1 ? 0 : options?.FitContentToViewport == true
            ? minimumGap
            : Math.Max(minimumGap, (availableW - totalWidth) / (prepared.Count - 1));
        var nodesById = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var lanes = LongEdgeLanePlan.Create(chart, prepared, true, availableH, nodeSpacing + 6);
        var x = pad;

        for (var layerIndex = 0; layerIndex < prepared.Count; layerIndex++) {
            var layer = prepared[layerIndex];
            var gapAfter = LayerGapAfter(prepared, layerIndex, layerGap, rankSpacing);
            if (TryPlaceHierarchyBucketsLeftToRight(chart, layer, nodesById, x, top, availableH, out var hierarchyWidth)) {
                lanes.Reject(layerIndex);
                lanes.RecordRank(layerIndex, x, x + Math.Max(layer.BlockSize, hierarchyWidth));
                x += Math.Max(layer.BlockSize, hierarchyWidth) + gapAfter;
                continue;
            }

            lanes.RecordRank(layerIndex, x, x + layer.BlockSize);
            if (responsibilityBands == null && lanes.HasLanes(layerIndex)) {
                lanes.Place(chart, layer, layerIndex, true, top, availableH, x, nodeSpacing + 6, nodesById);
                x += layer.BlockSize + gapAfter;
                continue;
            }

            var cellH = availableH / layer.Rows;
            for (var i = 0; i < layer.Nodes.Count; i++) {
                var node = layer.Nodes[i];
                var col = i / layer.Rows;
                var row = i % layer.Rows;
                var colCount = Math.Min(layer.Rows, layer.Nodes.Count - col * layer.Rows);
                var colTop = top + (availableH - colCount * cellH) / 2;
                if (responsibilityBands != null) { col = 0; row = responsibilityBands.Index(node); }
                ApplyLayerMetadata(node, layer, row, col);
                if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
                node.X = x + col * (layer.MaxWidth + nodeSpacing + 18) + (layer.MaxWidth - node.Width) / 2;
                node.Y = (responsibilityBands == null ? colTop + row * cellH + (cellH - LayeredNodeHeight(chart, node)) / 2 : top + responsibilityBands.Offset(node)) + TopologyNodeFootprint.TopInset(chart, node);
            }

            x += layer.BlockSize + gapAfter;
        }

        if (inferRoutes) { lanes.ApplyRoutes(nodesById, true); ApplyHierarchyEdgeRoutesLeftToRight(chart); }
    }

    /// <summary>Reserves the canonical Tile footprint only for authored subtitle typography; null roles retain incumbent cells.</summary>
    private static bool ReserveStyledLayeredSubtitle(TopologyChart chart, TopologyNode node) =>
        chart.RenderOptions is { ReadableDenseLayout: true, IncludeNodeLabels: true, IncludeTileSubtitles: true } options
        && node.SubtitleTextStyle?.HasOverrides == true && !string.IsNullOrWhiteSpace(node.Subtitle)
        && EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Tile;
    private static double LayeredNodeWidth(TopologyChart chart, TopologyNode node) =>
        TopologyExternalCaption.HasPolicy(node) || ReserveStyledLayeredSubtitle(chart, node) ? TopologyNodeFootprint.Width(chart, node) : node.Width;
    private static double LayeredNodeHeight(TopologyChart chart, TopologyNode node) =>
        TopologyExternalCaption.HasPolicy(node) || ReserveStyledLayeredSubtitle(chart, node) ? TopologyNodeFootprint.Height(chart, node) : node.Height;

    private static double LayerGapAfter(IReadOnlyList<LayerPlacement> layers, int index, double baseGap, double rankSpacing) {
        if (index >= layers.Count - 1) return 0;
        var preserveRankDistance = layers[index].Nodes.Any(node => node.Metadata.ContainsKey("layout.rankHintsApplied"));
        var rankDelta = preserveRankDistance ? Math.Max(1, layers[index + 1].Layer - layers[index].Layer) : 1;
        return baseGap + Math.Max(0, rankDelta - 1) * rankSpacing;
    }

    private static int LayerColumns(int count, double available, double itemSize, double gap) {
        if (count <= 0) return 1;
        var fitted = Math.Max(1, (int)Math.Floor((available + gap) / Math.Max(1, itemSize + gap)));
        return Math.Max(1, Math.Min(count, fitted));
    }

    private sealed class LayerPlacement {
        public LayerPlacement(int index, int layer, List<TopologyNode> nodes, int columns, int rows, double maxWidth, double maxHeight, double blockSize) {
            Index = index;
            Layer = layer;
            Nodes = nodes;
            Columns = Math.Max(1, columns);
            Rows = Math.Max(1, rows);
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            BlockSize = Math.Max(0, blockSize);
        }

        public int Index { get; }
        public int Layer { get; }
        public List<TopologyNode> Nodes { get; }
        public int Columns { get; }
        public int Rows { get; }
        public double MaxWidth { get; }
        public double MaxHeight { get; }
        public double BlockSize { get; }
    }

}
