using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public const double EndpointLabelFontSize = 9.5;

    public static (ChartRect Bounds, double FontSize) EdgeEndpointLabelLayout(TopologyChart chart, TopologyRenderOptions options, ChartPoint endpoint, ChartPoint adjacent, string text, double defaultUnits = 1) {
        var metrics = EndpointLabelMetrics(options, text, defaultUnits);
        var center = EdgeEndpointLabelPoint(chart, options, endpoint, adjacent, text, defaultUnits);
        return (new ChartRect(center.X - metrics.Width / 2, center.Y - metrics.Height / 2, metrics.Width, metrics.Height), metrics.FontSize);
    }

    internal static (double FontSize, double Width, double Height) EndpointLabelMetrics(TopologyRenderOptions options, string text, double defaultUnits) {
        var metrics = options.ResolvedEndpointLabelMetrics?.Invoke(text) ??
            (EndpointLabelFontSize, EstimateTextWidth(text, EndpointLabelFontSize, true, options.TextMeasurement) + 6, EndpointLabelFontSize + 6);
        return (metrics.Item1 * defaultUnits, metrics.Item2 * defaultUnits, metrics.Item3 * defaultUnits);
    }

    public static ChartPoint EdgeEndpointLabelPoint(TopologyChart chart, TopologyRenderOptions options, ChartPoint endpoint, ChartPoint adjacent, string text, double defaultUnits = 1) {
        var dx = adjacent.X - endpoint.X;
        var dy = adjacent.Y - endpoint.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.001) { dx = 1; dy = 0; length = 1; }
        var ux = dx / length;
        var uy = dy / length;
        var metrics = EndpointLabelMetrics(options, text, defaultUnits);
        var width = metrics.Width;
        var height = metrics.Height;
        var normalOffset = Math.Max(14 * defaultUnits, Math.Abs(uy) * width / 2 + Math.Abs(ux) * height / 2 + 4 * defaultUnits);
        var (obstacles, segments) = EndpointLabelSurroundings(chart, options, defaultUnits);
        var best = endpoint;
        var bestScore = double.PositiveInfinity;
        for (var step = 0; step < 6; step++) {
            for (var outward = 0; outward < 3; outward++) {
                foreach (var side in new[] { 1, -1 }) {
                    var along = (10 + step * 12) * defaultUnits;
                    var normal = normalOffset + outward * (height + 4 * defaultUnits);
                    var candidate = new ChartPoint(endpoint.X + ux * along - uy * normal * side,
                        endpoint.Y + uy * along + ux * normal * side);
                    var box = LabelBox.FromCenter(candidate.X, candidate.Y, width, height);
                    var clearance = box.Expand(3 * defaultUnits);
                    var crossings = segments.Count(segment => clearance.Intersects(segment.Start, segment.End));
                    var score = OverlapScore(box, obstacles) * 1000 + crossings * 1000 + step * 12 * defaultUnits + normal - normalOffset + (side == 1 ? 0 : 1);
                    if (score >= bestScore) continue;
                    best = candidate;
                    bestScore = score;
                }
            }
        }
        return best;
    }

    // Every endpoint label is placed against the same nodes, labels, headers and routes. Rebuilding them per label made
    // placement cost grow with labels times edges times routing, so one set is kept per chart geometry and options.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TopologyChart, EndpointLabelSurroundingsCache> EndpointLabelCaches = new();

    private sealed class EndpointLabelSurroundingsCache {
        internal long Key;
        internal TopologyRenderOptions? Options;
        internal List<LabelBox>? Obstacles;
        internal List<(ChartPoint Start, ChartPoint End)>? Segments;
    }

    private static (List<LabelBox> Obstacles, List<(ChartPoint Start, ChartPoint End)> Segments) EndpointLabelSurroundings(TopologyChart chart, TopologyRenderOptions options, double defaultUnits) {
        var key = EndpointLabelGeometryKey(chart, options, defaultUnits);
        var cache = EndpointLabelCaches.GetValue(chart, _ => new EndpointLabelSurroundingsCache());
        lock (cache) {
            if (cache.Obstacles != null && cache.Segments != null && cache.Key == key && ReferenceEquals(cache.Options, options)) return (cache.Obstacles, cache.Segments);
            var obstacles = chart.Nodes.Where(node => EffectiveNodeDisplayMode(node, options) != TopologyNodeDisplayMode.Hidden)
                .Select(node => EdgeLabelNodeObstacle(node, options, 4 * defaultUnits)).ToList();
            if (options.IncludeEdgeLabels) obstacles.AddRange(EdgeLabelLayouts(chart, options)
                .Select(label => LabelBox.FromCenter(label.CenterX, label.CenterY, label.Width, label.Height)));
            if (options.IncludeGroups && options.IncludeGroupLabels)
                obstacles.AddRange(chart.Groups.SelectMany(group => TopologyGroupHeader.PaintedParts(chart, group, options))
                    .Select(header => LabelBox.FromBounds(header.Left - 4 * defaultUnits, header.Top - 4 * defaultUnits, header.Right + 4 * defaultUnits, header.Bottom + 4 * defaultUnits)));
            var nodes = TopologyGroupEndpoints.Resolve(chart);
            var segments = new List<(ChartPoint Start, ChartPoint End)>();
            foreach (var edge in chart.Edges) {
                if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
                var route = EdgePoints(chart, edge, nodes);
                var painted = RenderedEdgeSamplePoints(chart, edge, nodes, route);
                for (var i = 1; i < painted.Count; i++) segments.Add((painted[i - 1], painted[i]));
                if (RenderedSourceMarker(edge, options.IncludeDirectionMarkers) != TopologyMarkerKind.None)
                    obstacles.Add(LabelBox.FromCenter(route[0].X, route[0].Y, 30, 30));
                if (RenderedTargetMarker(edge, options.IncludeDirectionMarkers) != TopologyMarkerKind.None)
                    obstacles.Add(LabelBox.FromCenter(route[route.Count - 1].X, route[route.Count - 1].Y, 30, 30));
            }
            cache.Key = key; cache.Options = options; cache.Obstacles = obstacles; cache.Segments = segments;
            return (obstacles, segments);
        }
    }

    // Everything the surroundings read: node, group and edge geometry, routing inputs, captions and the relevant switches.
    private static long EndpointLabelGeometryKey(TopologyChart chart, TopologyRenderOptions options, double defaultUnits) {
        unchecked {
            long hash = 1469598103934665603;
            void Add(double value) => hash = (hash ^ BitConverter.DoubleToInt64Bits(value)) * 1099511628211;
            void Text(string? value) => Add(value == null ? -1 : StringComparer.Ordinal.GetHashCode(value));
            Add(defaultUnits); Add(options.IncludeEdgeLabels ? 1 : 0); Add(options.IncludeGroups ? 1 : 0); Add(options.IncludeGroupLabels ? 1 : 0);
            Add(options.IncludeDirectionMarkers ? 1 : 0); Add((int)options.NodeDisplayMode);
            foreach (var node in chart.Nodes) { Text(node.Id); Add(node.X); Add(node.Y); Add(node.Width); Add(node.Height); Add((int)(node.DisplayMode ?? options.NodeDisplayMode)); Text(node.Label); Text(node.Subtitle); Add(node.Shape.HasValue ? (int)node.Shape.Value : -1); Add(node.ShowLabel ? 1 : 0); }
            foreach (var group in chart.Groups) { Text(group.Id); Add(group.X); Add(group.Y); Add(group.Width); Add(group.Height); Text(group.Label); }
            foreach (var edge in chart.Edges) {
                Text(edge.Id); Text(edge.SourceNodeId); Text(edge.TargetNodeId); Add((int)edge.Routing); Add((int)edge.SourcePort); Add((int)edge.TargetPort);
                Add(edge.RouteLane); Text(edge.Label); Text(edge.SecondaryLabel); Text(edge.TertiaryLabel); Add((int)edge.Direction);
                Add(edge.SourceMarker.HasValue ? (int)edge.SourceMarker.Value : -1); Add(edge.TargetMarker.HasValue ? (int)edge.TargetMarker.Value : -1);
                Text(edge.SourcePortId); Text(edge.TargetPortId); Add(edge.Interpolation.HasValue ? (int)edge.Interpolation.Value : -1);
                Add(edge.PathGeometry == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(edge.PathGeometry));
                foreach (var point in edge.Waypoints) { Add(point.X); Add(point.Y); }
            }
            return hash;
        }
    }

    public static string EdgeDash(TopologyEdge edge) {
        if (edge.DashPattern.Count > 0) return string.Join(" ", edge.DashPattern.Select(ChartStrokeDashes.FormatNumber));
        return edge.LineStyle switch {
            TopologyEdgeLineStyle.Solid => "none",
            TopologyEdgeLineStyle.Dashed => "8 5",
            TopologyEdgeLineStyle.Dotted => "2 5",
            _ => AssumedDashApplies(edge) ? "4 4" : EdgeDash(edge.Status)
        };
    }

    public static double[]? EdgePngDashArray(TopologyEdge edge) {
        if (edge.DashPattern.Count > 0) return edge.DashPattern.ToArray();
        var dash = EdgePngDash(edge);
        return dash.Dashed ? new[] { dash.Dash, dash.Gap } : null;
    }

    public static string EffectiveEdgeDash(TopologyEdge edge) {
        return edge.IsMuted && edge.Evidence != TopologyEdgeEvidence.Assumed && edge.DashPattern.Count == 0 && edge.LineStyle == TopologyEdgeLineStyle.Auto
            ? "none"
            : EdgeDash(edge);
    }

    public static double[]? EffectiveEdgePngDashArray(TopologyEdge edge) {
        return edge.IsMuted && edge.Evidence != TopologyEdgeEvidence.Assumed && edge.DashPattern.Count == 0 && edge.LineStyle == TopologyEdgeLineStyle.Auto
            ? null
            : EdgePngDashArray(edge);
    }

    public static TopologyMarkerKind EffectiveSourceMarker(TopologyEdge edge) {
        if (edge.SourceMarker.HasValue) return edge.SourceMarker.Value;
        return edge.Direction is VisualLinkDirection.Backward or VisualLinkDirection.Bidirectional ? TopologyMarkerKind.Arrow : TopologyMarkerKind.None;
    }

    public static TopologyMarkerKind EffectiveTargetMarker(TopologyEdge edge) {
        if (edge.TargetMarker.HasValue) return edge.TargetMarker.Value;
        return edge.Direction is VisualLinkDirection.Forward or VisualLinkDirection.Bidirectional ? TopologyMarkerKind.Arrow : TopologyMarkerKind.None;
    }

    public static TopologyMarkerKind RenderedSourceMarker(TopologyEdge edge, bool includeDirectionMarkers) {
        return edge.SourceMarker ?? (includeDirectionMarkers ? EffectiveSourceMarker(edge) : TopologyMarkerKind.None);
    }

    public static TopologyMarkerKind RenderedTargetMarker(TopologyEdge edge, bool includeDirectionMarkers) {
        return edge.TargetMarker ?? (includeDirectionMarkers ? EffectiveTargetMarker(edge) : TopologyMarkerKind.None);
    }

    private static void ApplyNamedEndpoint(TopologyChart chart, TopologyNode node, string portId, TopologyEdge edge, List<ChartPoint> points, int endpointIndex, int adjacentIndex) {
        var port = node.Ports.FirstOrDefault(candidate => string.Equals(candidate.Id, portId, StringComparison.Ordinal));
        if (port == null) return;
        if (TopologyLayoutEngine.UsesReadableDenseLayout(chart) &&
            edge.Routing == TopologyEdgeRouting.ObstacleAvoidingOrthogonal && edge.Waypoints.Count == 0 &&
            TopologyNodeFootprint.CaptionOnSide(chart, node, port.Side)) {
            if (!TopologyExternalCaption.HasPolicy(node))
                throw new InvalidOperationException("Readable dense tile '" + node.Id + "' cannot use named Bottom port '" +
                    port.Id + "' beneath its visible caption. Use a top or side port, or hide the node label.");
            throw new InvalidOperationException("Readable dense node '" + node.Id + "' cannot use named " + port.Side + " port '" +
                port.Id + "' through its visible caption. Use another side or hide the node label.");
        }
        var original = points[endpointIndex];
        var gap = EdgeEndpointGap * TopologyPreparedNodeBasis.Units(node);
        var point = port.Side switch {
            TopologyEdgePort.Top => new ChartPoint(node.X + node.Width * port.Offset, node.Y - gap),
            TopologyEdgePort.Right => new ChartPoint(node.X + node.Width + gap, node.Y + node.Height * port.Offset),
            TopologyEdgePort.Bottom => new ChartPoint(node.X + node.Width * port.Offset, node.Y + node.Height + gap),
            TopologyEdgePort.Left => new ChartPoint(node.X - gap, node.Y + node.Height * port.Offset),
            _ => original
        };
        points[endpointIndex] = point;
        PreserveOrthogonalEndpointLeg(edge, points, adjacentIndex, port.Side, original, point);
    }
}
