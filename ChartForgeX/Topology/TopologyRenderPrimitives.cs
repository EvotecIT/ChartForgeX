using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public const int NodeLabelMaxLength = 18;

    public static bool IsMonitoringDashboardStyle(TopologyRenderOptions options) => options.VisualStyle == TopologyVisualStyle.MonitoringDashboard;

    public static bool UseSoftMapBackground(TopologyRenderOptions options) =>
        options.MapBackgroundStyle == TopologyMapBackgroundStyle.SoftSilhouette ||
        options.MapBackgroundStyle == TopologyMapBackgroundStyle.Auto && IsMonitoringDashboardStyle(options);

    public static bool UseNeutralGroupSurface(TopologyRenderOptions options) => options.GroupSurfaceStyle == TopologyGroupSurfaceStyle.Neutral;

    public static string GroupFill(string accent, TopologyTheme theme, TopologyRenderOptions options) =>
        UseNeutralGroupSurface(options) ? theme.Card : StatusFill(accent, theme.Background, IsMonitoringDashboardStyle(options) ? 0.055 : 0.10);

    public static double EdgeStrokeWidth(TopologyEdge edge, bool selected, TopologyRenderOptions options) {
        if (options.UseForceGraphPresentation) return selected ? 2.1 : edge.Emphasis == TopologyEdgeEmphasis.Strong ? 1.25 : edge.IsMuted || edge.Emphasis == TopologyEdgeEmphasis.Subtle ? 0.65 : 0.82;
        if (!IsMonitoringDashboardStyle(options)) return selected ? 3.4 : edge.IsMuted ? 1.45 : 2.2;
        if (edge.Emphasis == TopologyEdgeEmphasis.Subtle && !selected) return 1.05;
        if (edge.Emphasis == TopologyEdgeEmphasis.Strong && !selected) return 2.15;
        return selected ? 2.35 : edge.IsMuted ? 1.05 : 1.65;
    }

    public static double EdgeOpacity(TopologyEdge edge, TopologyRenderOptions options) {
        if (options.UseForceGraphPresentation) return edge.Emphasis == TopologyEdgeEmphasis.Strong ? 0.66 : edge.IsMuted || edge.Emphasis == TopologyEdgeEmphasis.Subtle ? 0.14 : 0.26;
        if (!IsMonitoringDashboardStyle(options)) return edge.IsMuted ? 0.72 : 0.94;
        if (edge.Emphasis == TopologyEdgeEmphasis.Subtle) return edge.IsMuted ? 0.42 : 0.48;
        if (edge.Emphasis == TopologyEdgeEmphasis.Strong) return 0.98;
        return edge.IsMuted ? 0.78 : 0.96;
    }

    public static ChartLineVisualStyle EdgeVisualStyle(TopologyEdge edge, bool selected, TopologyRenderOptions options) =>
        options.EdgeVisualStyle ?? ChartRouteVisualStyles.TopologyEdge(IsMonitoringDashboardStyle(options), edge.IsMuted, selected);

    public static string EdgeColor(TopologyEdge edge, TopologyTheme theme, TopologyRenderOptions options) {
        if (edge.IsMuted) return IsMonitoringDashboardStyle(options) ? "#CBD5E1" : theme.Border;
        if (!string.IsNullOrWhiteSpace(edge.Color)) return edge.Color!.Trim();
        return theme.StatusColor(edge.Status);
    }

    public static bool ShouldRenderGeographicRouteHalo(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyRenderOptions options) {
        if (!IsMonitoringDashboardStyle(options)) return false;
        if (!IsGeographicCurve(chart, edge, nodes)) return false;
        return ShouldReserveGeographicCalloutRouteObstacle(edge);
    }

    public static bool ShouldRenderMonitoringRouteHalo(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyRenderOptions options) {
        if (!IsMonitoringDashboardStyle(options)) return false;
        if (ShouldRenderGeographicRouteHalo(chart, edge, nodes, options)) return true;
        if (edge.IsMuted || edge.Emphasis == TopologyEdgeEmphasis.Subtle) return false;
        return edge.Kind is TopologyEdgeKind.Link or TopologyEdgeKind.Connectivity or TopologyEdgeKind.Replication or TopologyEdgeKind.Mapping;
    }

    public static bool ShouldReserveGeographicCalloutRouteObstacle(TopologyEdge edge) =>
        edge.Kind is TopologyEdgeKind.Connectivity or TopologyEdgeKind.Link || !string.IsNullOrWhiteSpace(edge.Label);

    public static byte HighlightAlpha(byte alpha, bool isHighlighted, TopologyHighlightState highlight) {
        if (!highlight.IsActive || isHighlighted) return alpha;
        return (byte)Math.Round(alpha * Clamp(highlight.DimmedOpacity, 0, 1));
    }

    public static string StatusFill(string color, string background, double amount) => Blend(color, background, amount);

    public static string NormalizeCssClassPrefix(string? value, string fallback) {
        value = string.IsNullOrWhiteSpace(value) ? fallback : value!.Trim();
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value) {
            sb.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '-');
        }

        if (sb.Length == 0) return fallback;
        if (char.IsDigit(sb[0])) sb.Insert(0, "cfx-");
        return sb.ToString();
    }

    public static string LegendKindToken(TopologyLegendItemKind kind) {
        return kind == TopologyLegendItemKind.Edge ? "edge" : kind == TopologyLegendItemKind.Node ? "node" : "status";
    }

    public static string EdgeDash(TopologyHealthStatus status) {
        return status switch {
            TopologyHealthStatus.Warning => "8 5",
            TopologyHealthStatus.Critical => "8 5",
            TopologyHealthStatus.Unknown => "5 6",
            TopologyHealthStatus.Disabled => "3 6",
            _ => "none"
        };
    }

    public static string EdgeDash(TopologyEdge edge) {
        return edge.LineStyle switch {
            TopologyEdgeLineStyle.Solid => "none",
            TopologyEdgeLineStyle.Dashed => "8 5",
            TopologyEdgeLineStyle.Dotted => "2 5",
            _ => EdgeDash(edge.Status)
        };
    }

    public static (bool Dashed, double Dash, double Gap) EdgePngDash(TopologyEdge edge) {
        return edge.LineStyle switch {
            TopologyEdgeLineStyle.Solid => (false, 0, 0),
            TopologyEdgeLineStyle.Dashed => (true, 8, 5),
            TopologyEdgeLineStyle.Dotted => (true, 2, 5),
            _ => edge.Status switch {
                TopologyHealthStatus.Warning => (true, 8, 5),
                TopologyHealthStatus.Critical => (true, 8, 5),
                TopologyHealthStatus.Unknown => (true, 5, 6),
                TopologyHealthStatus.Disabled => (true, 3, 6),
                _ => (false, 0, 0)
            }
        };
    }

    public static string KindGlyph(TopologyNodeKind kind) {
        return kind switch {
            TopologyNodeKind.Group => "G",
            TopologyNodeKind.Location => "L",
            TopologyNodeKind.Hub => "H",
            TopologyNodeKind.Branch => "B",
            TopologyNodeKind.Server => "S",
            TopologyNodeKind.Gateway => "GW",
            TopologyNodeKind.Service => "SV",
            TopologyNodeKind.Endpoint => "EP",
            TopologyNodeKind.Certificate => "CA",
            TopologyNodeKind.Namespace => "NS",
            TopologyNodeKind.Cloud => "C",
            TopologyNodeKind.Database => "DB",
            TopologyNodeKind.Storage => "ST",
            TopologyNodeKind.Network => "NW",
            TopologyNodeKind.NetworkSegment => "SEG",
            TopologyNodeKind.Application => "APP",
            TopologyNodeKind.Process => "P",
            TopologyNodeKind.Queue => "Q",
            TopologyNodeKind.Person => "U",
            TopologyNodeKind.Team => "T",
            _ => "N"
        };
    }

    public static string NodeGlyph(TopologyNode node) => string.IsNullOrWhiteSpace(node.Symbol) ? KindGlyph(node.Kind) : TrimTo(node.Symbol!.Trim(), 4);

    public static bool ShouldRenderNodeStatusBadge(TopologyNode node, TopologyRenderOptions options) {
        var displayMode = EffectiveNodeDisplayMode(node, options);
        if (displayMode == TopologyNodeDisplayMode.Hidden) return false;
        if (displayMode == TopologyNodeDisplayMode.Dot) return false;
        if (displayMode == TopologyNodeDisplayMode.Artwork) return false;
        if (IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon && node.Kind == TopologyNodeKind.Cloud) return false;
        return true;
    }

    public static string NodeBadge(TopologyNode node) => string.IsNullOrWhiteSpace(node.Badge) ? string.Empty : TrimTo(node.Badge!.Trim(), 8);

    public static int NodeTitleMaxLength(TopologyNodeDisplayMode displayMode) {
        return displayMode switch {
            TopologyNodeDisplayMode.CompactCard => 11,
            TopologyNodeDisplayMode.Pill => 14,
            _ => NodeLabelMaxLength
        };
    }

    public static double EstimateTextWidth(string value, double fontSize, bool bold) {
        var weightFactor = bold ? 0.62 : 0.56;
        return value.Length * fontSize * weightFactor;
    }

    public static string TrimToEstimatedWidth(string value, double maxWidth, double fontSize, bool bold) {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        maxWidth = Math.Max(fontSize * 2.4, maxWidth);
        if (EstimateTextWidth(value, fontSize, bold) <= maxWidth) return value;
        if (maxWidth <= EstimateTextWidth("...", fontSize, bold)) return "...";

        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            var candidate = value.Substring(0, Math.Max(0, mid)) + "...";
            if (EstimateTextWidth(candidate, fontSize, bold) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, Math.Max(0, low)) + "...";
    }

    public static double FitFontSize(string value, double maxWidth, double preferredFontSize, double minimumFontSize, bool bold) {
        if (string.IsNullOrWhiteSpace(value)) return preferredFontSize;
        var fontSize = preferredFontSize;
        while (fontSize > minimumFontSize && EstimateTextWidth(value, fontSize, bold) > maxWidth) {
            fontSize -= 0.5;
        }

        return Math.Max(minimumFontSize, fontSize);
    }

    public static string StatusGlyph(TopologyHealthStatus status) {
        return status switch {
            TopologyHealthStatus.Healthy => "OK",
            TopologyHealthStatus.Warning => "!",
            TopologyHealthStatus.Critical => "!",
            TopologyHealthStatus.Disabled => "-",
            _ => "?"
        };
    }

    public static string StatusMarkerToken(TopologyHealthStatus status) => status.ToString().ToLowerInvariant();

    public static string StatusFill(string color, string background) => StatusFill(color, background, 0.10);

    public static List<ChartPoint> EdgePoints(TopologyNode source, TopologyNode target, TopologyEdgeRouting routing) {
        return EdgePoints(source, target, routing, TopologyEdgePort.Auto, TopologyEdgePort.Auto, 0);
    }

    public static List<ChartPoint> EdgePoints(TopologyNode source, TopologyNode target, TopologyEdgeRouting routing, TopologyEdgePort sourcePort, TopologyEdgePort targetPort, double routeLane) {
        var sourceCenterX = CenterX(source);
        var sourceCenterY = CenterY(source);
        var targetCenterX = CenterX(target);
        var targetCenterY = CenterY(target);
        if (routing != TopologyEdgeRouting.Orthogonal) {
            return new List<ChartPoint> {
                BoundaryPoint(source, targetCenterX, targetCenterY, sourcePort),
                BoundaryPoint(target, sourceCenterX, sourceCenterY, targetPort)
            };
        }

        var horizontal = ShouldRouteHorizontally(sourcePort, targetPort, Math.Abs(targetCenterX - sourceCenterX) >= Math.Abs(targetCenterY - sourceCenterY));
        var sourcePoint = BoundaryPoint(source, targetCenterX, targetCenterY, sourcePort);
        var targetPoint = BoundaryPoint(target, sourceCenterX, sourceCenterY, targetPort);
        if (horizontal) {
            var middleX = (sourcePoint.X + targetPoint.X) / 2 + routeLane;
            return new List<ChartPoint> {
                sourcePoint,
                new(middleX, sourcePoint.Y),
                new(middleX, targetPoint.Y),
                targetPoint
            };
        }

        var middleY = (sourcePoint.Y + targetPoint.Y) / 2 + routeLane;
        return new List<ChartPoint> {
            sourcePoint,
            new(sourcePoint.X, middleY),
            new(targetPoint.X, middleY),
            targetPoint
        };
    }

    public static List<ChartPoint> EdgePoints(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var source = nodes[edge.SourceNodeId];
        var target = nodes[edge.TargetNodeId];
        var offset = EdgeRouteOffset(chart, edge);
        var routeLane = EdgeRouteLane(chart, edge, offset);
        var points = edge.Waypoints.Count == 0
            ? edge.Routing == TopologyEdgeRouting.ObstacleAvoidingOrthogonal
                ? TopologyEdgeRouter.Route(chart, edge, source, target, routeLane).Points
                : EdgePoints(source, target, edge.Routing, edge.SourcePort, edge.TargetPort, routeLane)
            : EdgePoints(source, target, edge.Waypoints, edge.SourcePort, edge.TargetPort);
        ApplyEndpointPortSpreading(chart, edge, nodes, source, target, points);
        if (Math.Abs(offset) < 0.0001 || UsesOrthogonalRoute(edge)) return points;

        var vectorSource = string.Compare(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal) <= 0 ? source : target;
        var vectorTarget = ReferenceEquals(vectorSource, source) ? target : source;
        var dx = CenterX(vectorTarget) - CenterX(vectorSource);
        var dy = CenterY(vectorTarget) - CenterY(vectorSource);
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.0001) return points;

        var ox = -dy / length * offset;
        var oy = dx / length * offset;
        return points.Select(point => new ChartPoint(point.X + ox, point.Y + oy)).ToList();
    }

    private static bool UsesOrthogonalRoute(TopologyEdge edge) =>
        edge.Routing is TopologyEdgeRouting.Orthogonal or TopologyEdgeRouting.ObstacleAvoidingOrthogonal ||
        edge.Waypoints.Count > 0;

    private static void ApplyEndpointPortSpreading(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyNode source, TopologyNode target, List<ChartPoint> points) {
        if (points.Count < 2) return;
        if (edge.SourcePort != TopologyEdgePort.Auto) {
            var original = points[0];
            var spread = SpreadEndpoint(chart, edge, nodes, source, edge.SourcePort, original);
            points[0] = spread;
            PreserveOrthogonalEndpointLeg(edge, points, 1, edge.SourcePort, original, spread);
        }

        if (edge.TargetPort != TopologyEdgePort.Auto) {
            var targetIndex = points.Count - 1;
            var original = points[targetIndex];
            var spread = SpreadEndpoint(chart, edge, nodes, target, edge.TargetPort, original);
            points[targetIndex] = spread;
            PreserveOrthogonalEndpointLeg(edge, points, targetIndex - 1, edge.TargetPort, original, spread);
        }
    }

    private static ChartPoint SpreadEndpoint(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyNode node, TopologyEdgePort port, ChartPoint point) {
        var related = EndpointPortPeers(chart, nodes, node.Id, port);
        if (related.Count < 2) return point;
        var index = related.FindIndex(candidate => ReferenceEquals(candidate, edge));
        if (index < 0) return point;
        var offset = (index - (related.Count - 1) / 2.0) * EdgePortFanSpacing;
        var maximum = port is TopologyEdgePort.Top or TopologyEdgePort.Bottom
            ? Math.Max(0, node.Width / 2 - EdgeEndpointSidePadding)
            : Math.Max(0, node.Height / 2 - EdgeEndpointSidePadding);
        offset = Clamp(offset, -maximum, maximum);
        var spread = port switch {
            TopologyEdgePort.Top or TopologyEdgePort.Bottom => new ChartPoint(point.X + offset, point.Y),
            TopologyEdgePort.Left or TopologyEdgePort.Right => new ChartPoint(point.X, point.Y + offset),
            _ => point
        };
        return spread;
    }

    private static void PreserveOrthogonalEndpointLeg(TopologyEdge edge, List<ChartPoint> points, int adjacentIndex, TopologyEdgePort port, ChartPoint original, ChartPoint spread) {
        if (!UsesOrthogonalRoute(edge) || adjacentIndex < 0 || adjacentIndex >= points.Count) return;
        var adjacent = points[adjacentIndex];
        if (port is TopologyEdgePort.Top or TopologyEdgePort.Bottom) {
            var deltaX = spread.X - original.X;
            if (Math.Abs(deltaX) > 0.0001 && Math.Abs(adjacent.X - original.X) < 0.0001) {
                points[adjacentIndex] = new ChartPoint(adjacent.X + deltaX, adjacent.Y);
            }

            return;
        }

        if (port is TopologyEdgePort.Left or TopologyEdgePort.Right) {
            var deltaY = spread.Y - original.Y;
            if (Math.Abs(deltaY) > 0.0001 && Math.Abs(adjacent.Y - original.Y) < 0.0001) {
                points[adjacentIndex] = new ChartPoint(adjacent.X, adjacent.Y + deltaY);
            }
        }
    }

    private static List<TopologyEdge> EndpointPortPeers(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes, string nodeId, TopologyEdgePort port) {
        return chart.Edges
            .Where(candidate => EndpointUsesPort(candidate, nodeId, port))
            .OrderBy(candidate => EndpointPeerSortKey(candidate, nodeId, nodes))
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static bool EndpointUsesPort(TopologyEdge edge, string nodeId, TopologyEdgePort port) {
        return (string.Equals(edge.SourceNodeId, nodeId, StringComparison.Ordinal) && edge.SourcePort == port)
            || (string.Equals(edge.TargetNodeId, nodeId, StringComparison.Ordinal) && edge.TargetPort == port);
    }

    private static double EndpointPeerSortKey(TopologyEdge edge, string nodeId, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var otherId = string.Equals(edge.SourceNodeId, nodeId, StringComparison.Ordinal) ? edge.TargetNodeId : edge.SourceNodeId;
        if (!nodes.TryGetValue(nodeId, out var node) || !nodes.TryGetValue(otherId, out var other)) return 0;
        var dx = CenterX(other) - CenterX(node);
        var dy = CenterY(other) - CenterY(node);
        return Math.Atan2(dy, dx);
    }

    public static TopologyRouteDiagnostics EdgeRouteDiagnostics(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes) {
        return TopologyEdgeRouter.Diagnose(chart, edge, nodes);
    }

    public static List<ChartPoint> EdgePoints(TopologyNode source, TopologyNode target, IReadOnlyList<ChartPoint> waypoints) {
        return EdgePoints(source, target, waypoints, TopologyEdgePort.Auto, TopologyEdgePort.Auto);
    }

    public static List<ChartPoint> EdgePoints(TopologyNode source, TopologyNode target, IReadOnlyList<ChartPoint> waypoints, TopologyEdgePort sourcePort, TopologyEdgePort targetPort) {
        if (waypoints.Count == 0) return EdgePoints(source, target, TopologyEdgeRouting.Orthogonal, sourcePort, targetPort, 0);
        var first = waypoints[0];
        var last = waypoints[waypoints.Count - 1];
        var points = new List<ChartPoint> { BoundaryPoint(source, first.X, first.Y, sourcePort) };
        points.AddRange(waypoints);
        points.Add(BoundaryPoint(target, last.X, last.Y, targetPort));
        return points;
    }

    public static double EdgeRouteOffset(TopologyChart chart, TopologyEdge edge) {
        var related = chart.Edges
            .Where(candidate => SameEdgePair(candidate, edge))
            .OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
            .ToList();
        if (related.Count < 2) return 0;
        var index = related.FindIndex(candidate => ReferenceEquals(candidate, edge));
        if (index < 0) return 0;
        return (index - (related.Count - 1) / 2.0) * ParallelEdgeSpacing;
    }

    public static double EdgeRouteLane(TopologyChart chart, TopologyEdge edge) {
        return EdgeRouteLane(chart, edge, EdgeRouteOffset(chart, edge));
    }

    private static double EdgeRouteLane(TopologyChart chart, TopologyEdge edge, double offset) {
        if (edge.Waypoints.Count > 0) return edge.RouteLane;
        if (edge.Routing is not (TopologyEdgeRouting.Orthogonal or TopologyEdgeRouting.ObstacleAvoidingOrthogonal)) return edge.RouteLane;
        if (edge.HasRouteLaneOverride || Math.Abs(edge.RouteLane) >= 0.0001) return edge.RouteLane;
        return offset;
    }

    public static ChartPoint EdgeLabelPoint(TopologyNode source, TopologyNode target, TopologyEdgeRouting routing) {
        var points = EdgePoints(source, target, routing);
        return EdgeLabelPoint(points);
    }

    public static ChartPoint EdgeLabelPoint(IReadOnlyList<ChartPoint> points) {
        if (points.Count == 0) return new ChartPoint(0, 0);

        var total = 0.0;
        for (var i = 0; i < points.Count - 1; i++) total += Distance(points[i], points[i + 1]);
        if (total <= 0) return points[0];

        var midpoint = total / 2;
        var walked = 0.0;
        for (var i = 0; i < points.Count - 1; i++) {
            var segment = Distance(points[i], points[i + 1]);
            if (walked + segment >= midpoint) {
                var t = (midpoint - walked) / Math.Max(segment, 0.000001);
                return new ChartPoint(points[i].X + (points[i + 1].X - points[i].X) * t, points[i].Y + (points[i + 1].Y - points[i].Y) * t);
            }

            walked += segment;
        }

        return points[points.Count - 1];
    }

    public static bool IsGeographicCurve(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes) {
        if (chart.LayoutMode != TopologyLayoutMode.Geographic) return false;
        if (edge.Routing != TopologyEdgeRouting.Curved || edge.Waypoints.Count != 0) return false;
        if (!nodes.TryGetValue(edge.SourceNodeId, out var source) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) return false;
        return source.Longitude.HasValue && source.Latitude.HasValue && target.Longitude.HasValue && target.Latitude.HasValue;
    }

    public static ChartPoint GeographicCurveControlPoint(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyList<ChartPoint> points) {
        if (!IsGeographicCurve(chart, edge, nodes) || points.Count < 2) return EdgeLabelPoint(points);

        var start = points[0];
        var end = points[points.Count - 1];
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.0001) return start;

        var map = TopologyMapProjection.MapRect(chart);
        var midX = (start.X + end.X) / 2;
        var midY = (start.Y + end.Y) / 2;
        var perpX = dy / length;
        var perpY = -dx / length;

        if (Math.Abs(dx) >= Math.Abs(dy)) {
            if (perpY > 0) {
                perpX = -perpX;
                perpY = -perpY;
            }
        } else {
            var mapCenterX = map.Left + map.Width / 2;
            var awayFromCenter = midX < mapCenterX ? -1 : 1;
            if (perpX * awayFromCenter < 0) {
                perpX = -perpX;
                perpY = -perpY;
            }
        }

        var maximumCurve = Math.Min(88, Math.Min(map.Width, map.Height) * 0.24);
        var curve = Clamp(length * 0.18 + Math.Abs(edge.RouteLane) * 0.4, 24, Math.Max(24, maximumCurve));
        return new ChartPoint(
            Clamp(midX + perpX * curve, map.Left + 4, map.Right - 4),
            Clamp(midY + perpY * curve, map.Top + 4, map.Bottom - 4));
    }

    public static ChartPoint QuadraticPoint(ChartPoint start, ChartPoint control, ChartPoint end, double t) {
        var clamped = Clamp(t, 0, 1);
        var inverse = 1 - clamped;
        return new ChartPoint(
            inverse * inverse * start.X + 2 * inverse * clamped * control.X + clamped * clamped * end.X,
            inverse * inverse * start.Y + 2 * inverse * clamped * control.Y + clamped * clamped * end.Y);
    }

    public static List<ChartPoint> GeographicCurveSamplePoints(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyList<ChartPoint> points, int segments = 24) {
        if (!IsGeographicCurve(chart, edge, nodes) || points.Count < 2) return points.ToList();

        var start = points[0];
        var end = points[points.Count - 1];
        var control = GeographicCurveControlPoint(chart, edge, nodes, points);
        var count = Math.Max(2, segments);
        var samples = new List<ChartPoint>(count + 1);
        for (var i = 0; i <= count; i++) samples.Add(QuadraticPoint(start, control, end, i / (double)count));
        return samples;
    }

    public static string EdgeLabel(TopologyEdge edge, string? metricKey, string? fallback) {
        if (!string.IsNullOrWhiteSpace(metricKey) && edge.Metrics.TryGetValue(metricKey!, out var value)) return value;
        return fallback ?? string.Empty;
    }

    public static double CenterX(TopologyNode node) => node.X + node.Width / 2;

    public static double CenterY(TopologyNode node) => node.Y + node.Height / 2;

    public static string TrimTo(string value, int max) {
        if (value.Length <= max) return value;
        return value.Substring(0, Math.Max(0, max - 3)) + "...";
    }

    public static string SafeElementId(string? chartId, string kind, string id) => SanitizeId((chartId ?? "topology") + "-" + kind + "-" + id);

    public static string SanitizeId(string value) {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_') sb.Append(ch);
            else sb.Append('-');
        }

        return sb.Length == 0 ? "topology" : sb.ToString();
    }

    public static string? SafeHref(string? href) {
        if (href == null) return null;
        if (string.IsNullOrWhiteSpace(href)) return null;
        var value = href.Trim();
        var lower = value.ToLowerInvariant();
        if (lower.StartsWith("javascript:", StringComparison.Ordinal) || lower.StartsWith("data:", StringComparison.Ordinal) || lower.StartsWith("vbscript:", StringComparison.Ordinal)) return null;
        return value;
    }

    public static string CustomCssClasses(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var tokens = value!.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizeCssToken)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.Ordinal);
        var text = string.Join(" ", tokens);
        return string.IsNullOrWhiteSpace(text) ? string.Empty : " " + text;
    }

    public static string MetadataAttributes(IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, string>? metrics, bool include) {
        if (!include) return string.Empty;
        var sb = new StringBuilder();
        AppendDataAttributes(sb, "data-cfx-meta-", metadata);
        if (metrics != null) AppendDataAttributes(sb, "data-cfx-metric-", metrics);
        return sb.ToString();
    }

    public static string CssFontFamily(string value) => value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ");

    public static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public static string EscapeAttr(string value) => Escape(value).Replace("\"", "&quot;");

    public static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

    private const double EdgeEndpointGap = 7;
    private const double ParallelEdgeSpacing = 26;
    private const double EdgePortFanSpacing = 10;
    private const double EdgeEndpointSidePadding = 8;

    private static void AppendDataAttributes(StringBuilder sb, string prefix, IReadOnlyDictionary<string, string> values) {
        foreach (var item in values.OrderBy(item => item.Key, StringComparer.Ordinal)) {
            var key = SanitizeDataAttributeKey(item.Key);
            if (string.IsNullOrWhiteSpace(key)) continue;
            sb.Append(" ");
            sb.Append(prefix);
            sb.Append(key);
            sb.Append("=\"");
            sb.Append(EscapeAttr(item.Value ?? string.Empty));
            sb.Append("\"");
        }
    }

    private static bool SameEdgePair(TopologyEdge left, TopologyEdge right) {
        return (string.Equals(left.SourceNodeId, right.SourceNodeId, StringComparison.Ordinal) && string.Equals(left.TargetNodeId, right.TargetNodeId, StringComparison.Ordinal))
            || (string.Equals(left.SourceNodeId, right.TargetNodeId, StringComparison.Ordinal) && string.Equals(left.TargetNodeId, right.SourceNodeId, StringComparison.Ordinal));
    }

    private static string SanitizeCssToken(string value) => SanitizeToken(value, allowColon: true);

    private static string SanitizeDataAttributeKey(string value) => SanitizeToken(value, allowColon: false).Trim('-').ToLowerInvariant();

    private static string SanitizeToken(string value, bool allowColon) {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || (allowColon && ch == ':')) sb.Append(ch);
            else sb.Append('-');
        }

        return sb.ToString().Trim('-');
    }

    private static ChartPoint BoundaryPoint(TopologyNode node, double towardX, double towardY) {
        return BoundaryPoint(node, towardX, towardY, TopologyEdgePort.Auto);
    }

    private static ChartPoint BoundaryPoint(TopologyNode node, double towardX, double towardY, TopologyEdgePort port) {
        var centerX = CenterX(node);
        var centerY = CenterY(node);
        if (port != TopologyEdgePort.Auto) {
            return port switch {
                TopologyEdgePort.Top => new ChartPoint(centerX, node.Y - EdgeEndpointGap),
                TopologyEdgePort.Right => new ChartPoint(node.X + node.Width + EdgeEndpointGap, centerY),
                TopologyEdgePort.Bottom => new ChartPoint(centerX, node.Y + node.Height + EdgeEndpointGap),
                TopologyEdgePort.Left => new ChartPoint(node.X - EdgeEndpointGap, centerY),
                _ => new ChartPoint(centerX, centerY)
            };
        }

        var dx = towardX - centerX;
        var dy = towardY - centerY;
        if (Math.Abs(dx) < 0.000001 && Math.Abs(dy) < 0.000001) return new ChartPoint(centerX, centerY);

        var scaleX = Math.Abs(dx) < 0.000001 ? double.PositiveInfinity : (node.Width / 2 + EdgeEndpointGap) / Math.Abs(dx);
        var scaleY = Math.Abs(dy) < 0.000001 ? double.PositiveInfinity : (node.Height / 2 + EdgeEndpointGap) / Math.Abs(dy);
        var scale = Math.Min(scaleX, scaleY);
        return new ChartPoint(centerX + dx * scale, centerY + dy * scale);
    }

    private static bool ShouldRouteHorizontally(TopologyEdgePort sourcePort, TopologyEdgePort targetPort, bool fallback) {
        var hasHorizontalPort = sourcePort is TopologyEdgePort.Left or TopologyEdgePort.Right || targetPort is TopologyEdgePort.Left or TopologyEdgePort.Right;
        var hasVerticalPort = sourcePort is TopologyEdgePort.Top or TopologyEdgePort.Bottom || targetPort is TopologyEdgePort.Top or TopologyEdgePort.Bottom;
        if (hasHorizontalPort && !hasVerticalPort) return true;
        if (hasVerticalPort && !hasHorizontalPort) return false;
        return fallback;
    }

    private static double Distance(ChartPoint a, ChartPoint b) {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private readonly struct LabelBox {
        private readonly double _left;
        private readonly double _top;
        private readonly double _right;
        private readonly double _bottom;

        private LabelBox(double left, double top, double right, double bottom) {
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
        }

        public static LabelBox FromCenter(double centerX, double centerY, double width, double height) {
            return new LabelBox(centerX - width / 2, centerY - height / 2, centerX + width / 2, centerY + height / 2);
        }

        public static LabelBox FromNode(TopologyNode node, double padding) {
            return new LabelBox(node.X - padding, node.Y - padding, node.X + node.Width + padding, node.Y + node.Height + padding);
        }

        public static LabelBox FromBounds(double left, double top, double right, double bottom) => new(left, top, right, bottom);

        public static LabelBox FromGroup(TopologyGroup group, double padding) {
            return new LabelBox(group.X - padding, group.Y - padding, group.X + group.Width + padding, group.Y + group.Height + padding);
        }

        public static LabelBox FromGroupHeader(TopologyGroup group, double padding) {
            var headerHeight = Math.Min(68, Math.Max(44, group.Height * 0.32));
            return new LabelBox(group.X - padding, group.Y - padding, group.X + group.Width + padding, group.Y + headerHeight + padding);
        }

        public LabelBox Expand(double padding) {
            return new LabelBox(_left - padding, _top - padding, _right + padding, _bottom + padding);
        }

        public double OverlapArea(LabelBox other) {
            var width = Math.Min(_right, other._right) - Math.Max(_left, other._left);
            var height = Math.Min(_bottom, other._bottom) - Math.Max(_top, other._top);
            return width <= 0 || height <= 0 ? 0 : width * height;
        }

        public bool Intersects(ChartPoint start, ChartPoint end) {
            if (Contains(start) || Contains(end)) return true;
            if (SegmentBoundsOverlap(start, end)) {
                if (Math.Abs(start.Y - end.Y) < 0.000001 && start.Y >= _top && start.Y <= _bottom) return true;
                if (Math.Abs(start.X - end.X) < 0.000001 && start.X >= _left && start.X <= _right) return true;
            }

            var topLeft = new ChartPoint(_left, _top);
            var topRight = new ChartPoint(_right, _top);
            var bottomRight = new ChartPoint(_right, _bottom);
            var bottomLeft = new ChartPoint(_left, _bottom);
            return SegmentsIntersect(start, end, topLeft, topRight)
                || SegmentsIntersect(start, end, topRight, bottomRight)
                || SegmentsIntersect(start, end, bottomRight, bottomLeft)
                || SegmentsIntersect(start, end, bottomLeft, topLeft);
        }

        public bool Contains(ChartPoint point) => point.X >= _left && point.X <= _right && point.Y >= _top && point.Y <= _bottom;

        private bool SegmentBoundsOverlap(ChartPoint start, ChartPoint end) =>
            Math.Max(start.X, end.X) >= _left
            && Math.Min(start.X, end.X) <= _right
            && Math.Max(start.Y, end.Y) >= _top
            && Math.Min(start.Y, end.Y) <= _bottom;

        private static bool SegmentsIntersect(ChartPoint a, ChartPoint b, ChartPoint c, ChartPoint d) {
            var d1 = Direction(c, d, a);
            var d2 = Direction(c, d, b);
            var d3 = Direction(a, b, c);
            var d4 = Direction(a, b, d);
            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return true;
            return Math.Abs(d1) < 0.000001 && OnSegment(c, d, a)
                || Math.Abs(d2) < 0.000001 && OnSegment(c, d, b)
                || Math.Abs(d3) < 0.000001 && OnSegment(a, b, c)
                || Math.Abs(d4) < 0.000001 && OnSegment(a, b, d);
        }

        private static double Direction(ChartPoint a, ChartPoint b, ChartPoint c) => (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);

        private static bool OnSegment(ChartPoint a, ChartPoint b, ChartPoint c) {
            return c.X >= Math.Min(a.X, b.X) - 0.000001
                && c.X <= Math.Max(a.X, b.X) + 0.000001
                && c.Y >= Math.Min(a.Y, b.Y) - 0.000001
                && c.Y <= Math.Max(a.Y, b.Y) + 0.000001;
        }
    }

    private readonly struct EdgeSegment {
        public EdgeSegment(TopologyEdge edge, ChartPoint start, ChartPoint end) {
            Edge = edge;
            Start = start;
            End = end;
        }

        public TopologyEdge Edge { get; }

        public ChartPoint Start { get; }

        public ChartPoint End { get; }
    }

    private static string Blend(string foreground, string background, double alpha) {
        if (!TryParseHex(foreground, out var fr, out var fg, out var fb) || !TryParseHex(background, out var br, out var bg, out var bb)) return background;
        var r = (int)Math.Round(fr * alpha + br * (1 - alpha));
        var g = (int)Math.Round(fg * alpha + bg * (1 - alpha));
        var b = (int)Math.Round(fb * alpha + bb * (1 - alpha));
        return "#" + r.ToString("X2", CultureInfo.InvariantCulture) + g.ToString("X2", CultureInfo.InvariantCulture) + b.ToString("X2", CultureInfo.InvariantCulture);
    }

    private static bool TryParseHex(string value, out int r, out int g, out int b) {
        r = 0;
        g = 0;
        b = 0;
        if (string.IsNullOrWhiteSpace(value) || value[0] != '#') return false;
        var hex = value.Substring(1);
        if (hex.Length == 3) {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        }

        if (hex.Length != 6 && hex.Length != 8) return false;
        return int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
            && int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
            && int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }
}

internal sealed class TopologyEdgeLabelLayout {
    public TopologyEdgeLabelLayout(TopologyEdge edge, string label, string secondaryLabel, string tertiaryLabel, double centerX, double centerY, double width, double height, double anchorX, double anchorY) {
        Edge = edge;
        Label = label;
        SecondaryLabel = secondaryLabel;
        TertiaryLabel = tertiaryLabel;
        CenterX = centerX;
        CenterY = centerY;
        Width = width;
        Height = height;
        AnchorX = anchorX;
        AnchorY = anchorY;
    }

    public TopologyEdge Edge { get; }

    public string Label { get; }

    public string SecondaryLabel { get; }

    public string TertiaryLabel { get; }

    public double CenterX { get; }

    public double CenterY { get; }

    public double Width { get; }

    public double Height { get; }

    public double AnchorX { get; }

    public double AnchorY { get; }
}
