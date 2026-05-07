using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static class TopologyRenderPrimitives {
    public const int LegendColumns = 4;
    public const double LegendMaxWidth = 560;
    public const double LegendItemColumnWidth = 132;
    public const double LegendItemRowHeight = 24;
    public const double LegendFirstItemOffsetY = 46;
    public const double LegendBottomPadding = 16;
    public const int NodeLabelMaxLength = 18;

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

    public static double LegendHeight(TopologyLegend legend) {
        var rows = Math.Max(1, (int)Math.Ceiling(legend.Items.Count / (double)LegendColumns));
        return LegendFirstItemOffsetY + (rows - 1) * LegendItemRowHeight + LegendBottomPadding;
    }

    public static double LegendReservedHeight(TopologyLegend? legend) => legend == null ? 0 : LegendHeight(legend) + 24;

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

    public static TopologyNodeDisplayMode EffectiveNodeDisplayMode(TopologyNode node, TopologyRenderOptions options) => node.DisplayMode ?? options.NodeDisplayMode;

    public static string NodeBadge(TopologyNode node) => string.IsNullOrWhiteSpace(node.Badge) ? string.Empty : TrimTo(node.Badge!.Trim(), 8);

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

    public static string StatusFill(string color, string background) => Blend(color, background, 0.10);

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
        var points = edge.Waypoints.Count == 0
            ? edge.Routing == TopologyEdgeRouting.ObstacleAvoidingOrthogonal
                ? TopologyEdgeRouter.Route(chart, edge, source, target).Points
                : EdgePoints(source, target, edge.Routing, edge.SourcePort, edge.TargetPort, edge.RouteLane)
            : EdgePoints(source, target, edge.Waypoints, edge.SourcePort, edge.TargetPort);
        var offset = EdgeRouteOffset(chart, edge);
        if (Math.Abs(offset) < 0.0001) return points;

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
        var index = related.FindIndex(candidate => string.Equals(candidate.Id, edge.Id, StringComparison.Ordinal));
        if (index < 0) return 0;
        return (index - (related.Count - 1) / 2.0) * ParallelEdgeSpacing;
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

    public static List<TopologyEdgeLabelLayout> EdgeLabelLayouts(TopologyChart chart, TopologyRenderOptions options) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var nodeBoxes = chart.Nodes.Select(node => LabelBox.FromNode(node, 8)).ToList();
        var edgeSegments = EdgeSegments(chart, nodes);
        var placed = new List<LabelBox>();
        var layouts = new List<TopologyEdgeLabelLayout>();

        foreach (var edge in chart.Edges) {
            var label = EdgeLabel(edge, options.EdgeLabelMetricKey, edge.Label);
            var secondary = EdgeLabel(edge, options.EdgeSecondaryLabelMetricKey, edge.SecondaryLabel);
            var tertiary = EdgeLabel(edge, options.EdgeTertiaryLabelMetricKey, edge.TertiaryLabel);
            if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(secondary) && string.IsNullOrWhiteSpace(tertiary)) continue;
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;

            var points = EdgePoints(chart, edge, nodes);
            var labelPoint = IsGeographicCurve(chart, edge, nodes)
                ? QuadraticPoint(points[0], GeographicCurveControlPoint(chart, edge, nodes, points), points[points.Count - 1], 0.5)
                : EdgeLabelPoint(points);
            var maxText = Math.Max(label.Length, Math.Max(secondary.Length, tertiary.Length));
            var lineCount = (string.IsNullOrWhiteSpace(label) ? 0 : 1) + (string.IsNullOrWhiteSpace(secondary) ? 0 : 1) + (string.IsNullOrWhiteSpace(tertiary) ? 0 : 1);
            var width = Math.Max(48, maxText * 7.2 + 18);
            var height = lineCount <= 1 ? 22 : lineCount == 2 ? 38 : 52;
            var center = PlaceLabel(edge, labelPoint.X, labelPoint.Y, width, height, chart.Viewport, chart.Legend, nodeBoxes, placed, edgeSegments);
            var box = LabelBox.FromCenter(center.X, center.Y, width, height);
            placed.Add(box);
            layouts.Add(new TopologyEdgeLabelLayout(edge, label, secondary, tertiary, center.X, center.Y, width, height));
        }

        return layouts;
    }

    private static ChartPoint PlaceLabel(TopologyEdge edge, double baseX, double baseY, double width, double height, TopologyViewport viewport, TopologyLegend? legend, IReadOnlyList<LabelBox> nodeBoxes, IReadOnlyList<LabelBox> placedLabels, IReadOnlyList<EdgeSegment> edgeSegments) {
        var candidates = LabelCandidates(baseX, baseY);
        ChartPoint? best = null;
        var bestScore = double.MaxValue;
        foreach (var candidate in candidates) {
            var clamped = ClampLabel(candidate, width, height, viewport, legend);
            var box = LabelBox.FromCenter(clamped.X, clamped.Y, width, height);
            var score = OverlapScore(box, nodeBoxes) * 4 + OverlapScore(box, placedLabels) * 2 + EdgeOverlapScore(edge, box, edgeSegments) * 8 + Distance(new ChartPoint(baseX, baseY), clamped) * 0.05;
            if (score <= 0.0001) return clamped;
            if (score < bestScore) {
                best = clamped;
                bestScore = score;
            }
        }

        return best ?? ClampLabel(new ChartPoint(baseX, baseY), width, height, viewport, legend);
    }

    private static List<EdgeSegment> EdgeSegments(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var segments = new List<EdgeSegment>();
        foreach (var edge in chart.Edges) {
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            var points = EdgePoints(chart, edge, nodes);
            if (IsGeographicCurve(chart, edge, nodes)) points = GeographicCurveSamplePoints(chart, edge, nodes, points, 12);
            for (var i = 0; i < points.Count - 1; i++) segments.Add(new EdgeSegment(edge, points[i], points[i + 1]));
        }

        return segments;
    }

    private static IEnumerable<ChartPoint> LabelCandidates(double x, double y) {
        yield return new ChartPoint(x, y);
        var offsets = new[] {
            new ChartPoint(0, -30),
            new ChartPoint(0, 30),
            new ChartPoint(44, 0),
            new ChartPoint(-44, 0),
            new ChartPoint(54, -30),
            new ChartPoint(-54, -30),
            new ChartPoint(54, 30),
            new ChartPoint(-54, 30),
            new ChartPoint(0, -60),
            new ChartPoint(0, 60),
            new ChartPoint(82, 0),
            new ChartPoint(-82, 0)
        };
        foreach (var offset in offsets) yield return new ChartPoint(x + offset.X, y + offset.Y);
    }

    private static ChartPoint ClampLabel(ChartPoint point, double width, double height, TopologyViewport viewport, TopologyLegend? legend) {
        var minX = viewport.Padding + width / 2;
        var maxX = Math.Max(minX, viewport.Width - viewport.Padding - width / 2);
        var minY = viewport.Padding + height / 2;
        var maxY = Math.Max(minY, viewport.Height - viewport.Padding - LegendReservedHeight(legend) - height / 2);
        return new ChartPoint(Math.Min(maxX, Math.Max(minX, point.X)), Math.Min(maxY, Math.Max(minY, point.Y)));
    }

    private static double OverlapScore(LabelBox box, IReadOnlyList<LabelBox> others) {
        var score = 0.0;
        foreach (var other in others) score += box.OverlapArea(other);
        return score;
    }

    private static double EdgeOverlapScore(TopologyEdge edge, LabelBox box, IReadOnlyList<EdgeSegment> segments) {
        var score = 0.0;
        var expanded = box.Expand(5);
        foreach (var segment in segments) {
            if (ReferenceEquals(edge, segment.Edge)) continue;
            if (expanded.Intersects(segment.Start, segment.End)) score += 120;
        }

        return score;
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
            var topLeft = new ChartPoint(_left, _top);
            var topRight = new ChartPoint(_right, _top);
            var bottomRight = new ChartPoint(_right, _bottom);
            var bottomLeft = new ChartPoint(_left, _bottom);
            return SegmentsIntersect(start, end, topLeft, topRight)
                || SegmentsIntersect(start, end, topRight, bottomRight)
                || SegmentsIntersect(start, end, bottomRight, bottomLeft)
                || SegmentsIntersect(start, end, bottomLeft, topLeft);
        }

        private bool Contains(ChartPoint point) => point.X >= _left && point.X <= _right && point.Y >= _top && point.Y <= _bottom;

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
    public TopologyEdgeLabelLayout(TopologyEdge edge, string label, string secondaryLabel, string tertiaryLabel, double centerX, double centerY, double width, double height) {
        Edge = edge;
        Label = label;
        SecondaryLabel = secondaryLabel;
        TertiaryLabel = tertiaryLabel;
        CenterX = centerX;
        CenterY = centerY;
        Width = width;
        Height = height;
    }

    public TopologyEdge Edge { get; }

    public string Label { get; }

    public string SecondaryLabel { get; }

    public string TertiaryLabel { get; }

    public double CenterX { get; }

    public double CenterY { get; }

    public double Width { get; }

    public double Height { get; }
}
