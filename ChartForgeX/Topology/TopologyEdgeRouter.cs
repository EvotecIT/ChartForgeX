using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static class TopologyEdgeRouter {
    public static TopologyRoutePlan Route(TopologyChart chart, TopologyEdge edge, TopologyNode source, TopologyNode target) {
        if (edge.Waypoints.Count > 0) {
            var points = EdgePoints(source, target, edge.Waypoints, edge.SourcePort, edge.TargetPort);
            return BuildPlan("ManualWaypoints", "manual-waypoints", points, RouteObstacles(chart, source.Id, target.Id, edge.Id), RouteSegments(chart, edge), edge, 1);
        }

        if (edge.Routing != TopologyEdgeRouting.ObstacleAvoidingOrthogonal) {
            var points = EdgePoints(source, target, edge.Routing, edge.SourcePort, edge.TargetPort, edge.RouteLane);
            return BuildPlan(edge.Routing.ToString(), "default", points, RouteObstacles(chart, source.Id, target.Id, edge.Id), RouteSegments(chart, edge), edge, 1);
        }

        var sourcePoint = BoundaryPoint(source, CenterX(target), CenterY(target), edge.SourcePort);
        var targetPoint = BoundaryPoint(target, CenterX(source), CenterY(source), edge.TargetPort);
        var obstacles = RouteObstacles(chart, source.Id, target.Id, edge.Id);
        var existingSegments = RouteSegments(chart, edge);
        var candidates = new List<RouteCandidate> {
            new("orthogonal-default", EdgePoints(source, target, TopologyEdgeRouting.Orthogonal, edge.SourcePort, edge.TargetPort, edge.RouteLane))
        };

        foreach (var corridor in RouteXCandidates(chart, source, target, obstacles, edge.RouteLane)) {
            candidates.Add(new RouteCandidate(corridor.Name, new List<ChartPoint> {
                sourcePoint,
                new(corridor.Value, sourcePoint.Y),
                new(corridor.Value, targetPoint.Y),
                targetPoint
            }));
        }

        foreach (var corridor in RouteYCandidates(chart, source, target, obstacles, edge.RouteLane)) {
            candidates.Add(new RouteCandidate(corridor.Name, new List<ChartPoint> {
                sourcePoint,
                new(sourcePoint.X, corridor.Value),
                new(targetPoint.X, corridor.Value),
                targetPoint
            }));
        }

        var best = candidates
            .GroupBy(candidate => RouteKey(candidate.Points), StringComparer.Ordinal)
            .Select(group => group.OrderBy(candidate => candidate.Corridor, StringComparer.Ordinal).First())
            .Select(candidate => BuildPlan("ObstacleAvoidingOrthogonal", candidate.Corridor, candidate.Points, obstacles, existingSegments, edge, candidates.Count))
            .OrderBy(plan => RouteScore(plan))
            .ThenBy(plan => RouteLength(plan.Points))
            .ThenBy(plan => RouteKey(plan.Points), StringComparer.Ordinal)
            .First();

        return best;
    }

    public static TopologyRouteDiagnostics Diagnose(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes) {
        if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) return new TopologyRouteDiagnostics(edge.Routing.ToString(), "missing-node", 0, 0, 0, 0, 0, "missing-node");
        var plan = Route(chart, edge, nodes[edge.SourceNodeId], nodes[edge.TargetNodeId]);
        return plan.Diagnostics;
    }

    private static TopologyRoutePlan BuildPlan(string strategy, string corridor, List<ChartPoint> points, IReadOnlyList<RouteBox> obstacles, IReadOnlyList<RouteSegment> existingSegments, TopologyEdge edge, int candidateCount) {
        var obstacleHits = RouteObstacleHits(points, obstacles);
        var routeOverlap = RouteOverlapScore(points, existingSegments);
        var labelHits = LabelObstacleHits(points, edge, obstacles);
        return new TopologyRoutePlan(points, new TopologyRouteDiagnostics(strategy, corridor, Math.Max(0, points.Count - 1), obstacleHits, labelHits, routeOverlap, candidateCount, FallbackReason(strategy, obstacleHits, labelHits, routeOverlap)));
    }

    private static double RouteScore(TopologyRoutePlan plan) {
        return plan.Diagnostics.ObstacleHits * 100000 +
            plan.Diagnostics.LabelObstacleHits * 30000 +
            plan.Diagnostics.RouteOverlapScore * 220 +
            Math.Max(0, plan.Points.Count - 2) * 80 +
            RouteLength(plan.Points);
    }

    private static string FallbackReason(string strategy, int obstacleHits, int labelHits, double routeOverlap) {
        if (string.Equals(strategy, "ManualWaypoints", StringComparison.Ordinal)) return "manual-waypoints";
        if (!string.Equals(strategy, "ObstacleAvoidingOrthogonal", StringComparison.Ordinal)) return "not-obstacle-aware";
        if (obstacleHits <= 0 && labelHits <= 0 && routeOverlap <= 0.0001) return "none";
        if (obstacleHits > 0) return "best-effort-obstacle";
        if (labelHits > 0) return "best-effort-label";
        return "best-effort-overlap";
    }

    private static IEnumerable<RouteCorridor> RouteXCandidates(TopologyChart chart, TopologyNode source, TopologyNode target, IReadOnlyList<RouteBox> obstacles, double routeLane) {
        const double margin = 18;
        var min = chart.Viewport.Padding;
        var max = chart.Viewport.Width - chart.Viewport.Padding;
        yield return new RouteCorridor("vertical-mid", Clamp((CenterX(source) + CenterX(target)) / 2 + routeLane, min, max));
        yield return new RouteCorridor("vertical-viewport-left", Clamp(min + margin, min, max));
        yield return new RouteCorridor("vertical-viewport-right", Clamp(max - margin, min, max));
        yield return new RouteCorridor("vertical-source-left", Clamp(source.X - margin, min, max));
        yield return new RouteCorridor("vertical-source-right", Clamp(source.X + source.Width + margin, min, max));
        yield return new RouteCorridor("vertical-target-left", Clamp(target.X - margin, min, max));
        yield return new RouteCorridor("vertical-target-right", Clamp(target.X + target.Width + margin, min, max));
        foreach (var obstacle in obstacles) {
            yield return new RouteCorridor("vertical-obstacle-left", Clamp(obstacle.Left - margin, min, max));
            yield return new RouteCorridor("vertical-obstacle-right", Clamp(obstacle.Right + margin, min, max));
        }
    }

    private static IEnumerable<RouteCorridor> RouteYCandidates(TopologyChart chart, TopologyNode source, TopologyNode target, IReadOnlyList<RouteBox> obstacles, double routeLane) {
        const double margin = 18;
        var min = chart.Viewport.Padding + (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle) ? 0 : 72);
        var max = chart.Viewport.Height - chart.Viewport.Padding - LegendReservedHeight(chart.Legend);
        yield return new RouteCorridor("horizontal-mid", Clamp((CenterY(source) + CenterY(target)) / 2 + routeLane, min, max));
        yield return new RouteCorridor("horizontal-viewport-top", Clamp(min + margin, min, max));
        yield return new RouteCorridor("horizontal-viewport-bottom", Clamp(max - margin, min, max));
        yield return new RouteCorridor("horizontal-source-top", Clamp(source.Y - margin, min, max));
        yield return new RouteCorridor("horizontal-source-bottom", Clamp(source.Y + source.Height + margin, min, max));
        yield return new RouteCorridor("horizontal-target-top", Clamp(target.Y - margin, min, max));
        yield return new RouteCorridor("horizontal-target-bottom", Clamp(target.Y + target.Height + margin, min, max));
        foreach (var obstacle in obstacles) {
            yield return new RouteCorridor("horizontal-obstacle-top", Clamp(obstacle.Top - margin, min, max));
            yield return new RouteCorridor("horizontal-obstacle-bottom", Clamp(obstacle.Bottom + margin, min, max));
        }
    }

    private static List<RouteBox> RouteObstacles(TopologyChart chart, string sourceNodeId, string targetNodeId, string? routedEdgeId) {
        const double nodePadding = 10;
        var obstacles = chart.Nodes
            .Where(node => !string.Equals(node.Id, sourceNodeId, StringComparison.Ordinal) && !string.Equals(node.Id, targetNodeId, StringComparison.Ordinal))
            .Select(node => new RouteBox(node.X - nodePadding, node.Y - nodePadding, node.X + node.Width + nodePadding, node.Y + node.Height + nodePadding))
            .ToList();

        const double groupPadding = 10;
        foreach (var group in chart.Groups) {
            var header = GroupHeaderBox(group).Expand(groupPadding);
            if (header.Width > 0 && header.Height > 0) obstacles.Add(header);
        }

        foreach (var box in EstimatedLabelBoxes(chart, routedEdgeId)) obstacles.Add(box.Expand(4));
        return obstacles;
    }

    private static IEnumerable<RouteBox> EstimatedLabelBoxes(TopologyChart chart, string? routedEdgeId) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (!string.IsNullOrWhiteSpace(routedEdgeId) && string.Equals(edge.Id, routedEdgeId, StringComparison.Ordinal)) continue;
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            var label = edge.Label ?? string.Empty;
            var secondary = edge.SecondaryLabel ?? string.Empty;
            var tertiary = edge.TertiaryLabel ?? string.Empty;
            if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(secondary) && string.IsNullOrWhiteSpace(tertiary)) continue;

            var points = BasicEdgePoints(nodes[edge.SourceNodeId], nodes[edge.TargetNodeId], edge);
            var center = EdgeLabelPoint(points);
            var maxText = Math.Max(label.Length, Math.Max(secondary.Length, tertiary.Length));
            var lineCount = (string.IsNullOrWhiteSpace(label) ? 0 : 1) + (string.IsNullOrWhiteSpace(secondary) ? 0 : 1) + (string.IsNullOrWhiteSpace(tertiary) ? 0 : 1);
            var width = Math.Max(48, maxText * 7.2 + 18);
            var height = lineCount <= 1 ? 22 : lineCount == 2 ? 38 : 52;
            yield return RouteBox.FromCenter(center.X, center.Y, width, height);
        }
    }

    private static List<RouteSegment> RouteSegments(TopologyChart chart, TopologyEdge routedEdge) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var segments = new List<RouteSegment>();
        foreach (var edge in chart.Edges) {
            if (ReferenceEquals(edge, routedEdge) || string.Equals(edge.Id, routedEdge.Id, StringComparison.Ordinal)) continue;
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            var points = BasicEdgePoints(nodes[edge.SourceNodeId], nodes[edge.TargetNodeId], edge);
            for (var i = 0; i < points.Count - 1; i++) segments.Add(new RouteSegment(points[i], points[i + 1]));
        }

        return segments;
    }

    private static List<ChartPoint> BasicEdgePoints(TopologyNode source, TopologyNode target, TopologyEdge edge) {
        if (edge.Waypoints.Count > 0) return EdgePoints(source, target, edge.Waypoints, edge.SourcePort, edge.TargetPort);
        var routing = edge.Routing == TopologyEdgeRouting.ObstacleAvoidingOrthogonal ? TopologyEdgeRouting.Orthogonal : edge.Routing;
        return EdgePoints(source, target, routing, edge.SourcePort, edge.TargetPort, edge.RouteLane);
    }

    private static int RouteObstacleHits(IReadOnlyList<ChartPoint> points, IReadOnlyList<RouteBox> obstacles) {
        var hits = 0;
        for (var i = 0; i < points.Count - 1; i++) {
            foreach (var obstacle in obstacles) {
                if (obstacle.Intersects(points[i], points[i + 1])) hits++;
            }
        }

        return hits;
    }

    private static int LabelObstacleHits(IReadOnlyList<ChartPoint> points, TopologyEdge edge, IReadOnlyList<RouteBox> obstacles) {
        if (string.IsNullOrWhiteSpace(edge.Label) && string.IsNullOrWhiteSpace(edge.SecondaryLabel) && string.IsNullOrWhiteSpace(edge.TertiaryLabel)) return 0;
        var center = EdgeLabelPoint(points);
        var maxText = Math.Max((edge.Label ?? string.Empty).Length, Math.Max((edge.SecondaryLabel ?? string.Empty).Length, (edge.TertiaryLabel ?? string.Empty).Length));
        var lineCount = (string.IsNullOrWhiteSpace(edge.Label) ? 0 : 1) + (string.IsNullOrWhiteSpace(edge.SecondaryLabel) ? 0 : 1) + (string.IsNullOrWhiteSpace(edge.TertiaryLabel) ? 0 : 1);
        var label = RouteBox.FromCenter(center.X, center.Y, Math.Max(48, maxText * 7.2 + 18), lineCount <= 1 ? 22 : lineCount == 2 ? 38 : 52);
        return obstacles.Count(obstacle => label.OverlapArea(obstacle) > 0);
    }

    private static double RouteOverlapScore(IReadOnlyList<ChartPoint> points, IReadOnlyList<RouteSegment> existingSegments) {
        var score = 0.0;
        for (var i = 0; i < points.Count - 1; i++) {
            var segment = new RouteSegment(points[i], points[i + 1]);
            foreach (var existing in existingSegments) score += segment.OverlapScore(existing);
        }

        return score;
    }

    private static double RouteLength(IReadOnlyList<ChartPoint> points) {
        var length = 0.0;
        for (var i = 0; i < points.Count - 1; i++) length += Distance(points[i], points[i + 1]);
        return length;
    }

    private static string RouteKey(IReadOnlyList<ChartPoint> points) => string.Join(";", points.Select(point => F(point.X) + "," + F(point.Y)));

    private static RouteBox GroupHeaderBox(TopologyGroup group) {
        const double groupPadding = 24;
        const double topPadding = 14;
        var labelWidth = EstimateTextWidth(group.Label, 16, true);
        var subtitleWidth = string.IsNullOrWhiteSpace(group.Subtitle) ? 0 : EstimateTextWidth(group.Subtitle!, 12, false);
        var width = Math.Min(Math.Max(96, Math.Max(labelWidth, subtitleWidth) + 12), Math.Max(96, group.Width - groupPadding * 2));
        var height = string.IsNullOrWhiteSpace(group.Subtitle) ? 40 : 60;
        return new RouteBox(group.X + groupPadding, group.Y + topPadding, group.X + groupPadding + width, group.Y + topPadding + height);
    }

    private static double EstimateTextWidth(string value, double fontSize, bool bold) {
        var weightFactor = bold ? 0.62 : 0.56;
        return value.Length * fontSize * weightFactor;
    }

    private static ChartPoint BoundaryPoint(TopologyNode node, double towardX, double towardY, TopologyEdgePort port) {
        var centerX = CenterX(node);
        var centerY = CenterY(node);
        const double edgeEndpointGap = 7;
        if (port != TopologyEdgePort.Auto) {
            return port switch {
                TopologyEdgePort.Top => new ChartPoint(centerX, node.Y - edgeEndpointGap),
                TopologyEdgePort.Right => new ChartPoint(node.X + node.Width + edgeEndpointGap, centerY),
                TopologyEdgePort.Bottom => new ChartPoint(centerX, node.Y + node.Height + edgeEndpointGap),
                TopologyEdgePort.Left => new ChartPoint(node.X - edgeEndpointGap, centerY),
                _ => new ChartPoint(centerX, centerY)
            };
        }

        var dx = towardX - centerX;
        var dy = towardY - centerY;
        if (Math.Abs(dx) < 0.000001 && Math.Abs(dy) < 0.000001) return new ChartPoint(centerX, centerY);

        var scaleX = Math.Abs(dx) < 0.000001 ? double.PositiveInfinity : (node.Width / 2 + edgeEndpointGap) / Math.Abs(dx);
        var scaleY = Math.Abs(dy) < 0.000001 ? double.PositiveInfinity : (node.Height / 2 + edgeEndpointGap) / Math.Abs(dy);
        var scale = Math.Min(scaleX, scaleY);
        return new ChartPoint(centerX + dx * scale, centerY + dy * scale);
    }

    private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

    private static double Distance(ChartPoint a, ChartPoint b) {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private readonly struct RouteCandidate {
        public RouteCandidate(string corridor, List<ChartPoint> points) {
            Corridor = corridor;
            Points = points;
        }

        public string Corridor { get; }

        public List<ChartPoint> Points { get; }
    }

    private readonly struct RouteCorridor {
        public RouteCorridor(string name, double value) {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public double Value { get; }
    }

    private readonly struct RouteBox {
        public RouteBox(double left, double top, double right, double bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public double Left { get; }

        public double Top { get; }

        public double Right { get; }

        public double Bottom { get; }

        public double Width => Right - Left;

        public double Height => Bottom - Top;

        public static RouteBox FromCenter(double centerX, double centerY, double width, double height) =>
            new(centerX - width / 2, centerY - height / 2, centerX + width / 2, centerY + height / 2);

        public RouteBox Expand(double padding) => new(Left - padding, Top - padding, Right + padding, Bottom + padding);

        public double OverlapArea(RouteBox other) {
            var width = Math.Max(0, Math.Min(Right, other.Right) - Math.Max(Left, other.Left));
            var height = Math.Max(0, Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top));
            return width * height;
        }

        public bool Intersects(ChartPoint start, ChartPoint end) {
            if (Math.Abs(start.Y - end.Y) < 0.0001) {
                var left = Math.Min(start.X, end.X);
                var right = Math.Max(start.X, end.X);
                return start.Y >= Top && start.Y <= Bottom && right >= Left && left <= Right;
            }

            if (Math.Abs(start.X - end.X) < 0.0001) {
                var top = Math.Min(start.Y, end.Y);
                var bottom = Math.Max(start.Y, end.Y);
                return start.X >= Left && start.X <= Right && bottom >= Top && top <= Bottom;
            }

            return SegmentIntersectsBounds(start, end);
        }

        private bool SegmentIntersectsBounds(ChartPoint start, ChartPoint end) {
            if (PointInside(start) || PointInside(end)) return true;
            return SegmentsIntersect(start, end, new ChartPoint(Left, Top), new ChartPoint(Right, Top)) ||
                SegmentsIntersect(start, end, new ChartPoint(Right, Top), new ChartPoint(Right, Bottom)) ||
                SegmentsIntersect(start, end, new ChartPoint(Right, Bottom), new ChartPoint(Left, Bottom)) ||
                SegmentsIntersect(start, end, new ChartPoint(Left, Bottom), new ChartPoint(Left, Top));
        }

        private bool PointInside(ChartPoint point) => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

        private static bool SegmentsIntersect(ChartPoint a, ChartPoint b, ChartPoint c, ChartPoint d) {
            var denominator = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);
            if (Math.Abs(denominator) < 0.000001) return false;
            var ua = ((d.X - c.X) * (a.Y - c.Y) - (d.Y - c.Y) * (a.X - c.X)) / denominator;
            var ub = ((b.X - a.X) * (a.Y - c.Y) - (b.Y - a.Y) * (a.X - c.X)) / denominator;
            return ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1;
        }
    }

    private readonly struct RouteSegment {
        public RouteSegment(ChartPoint start, ChartPoint end) {
            Start = start;
            End = end;
        }

        public ChartPoint Start { get; }

        public ChartPoint End { get; }

        public double OverlapScore(RouteSegment other) {
            if (Math.Abs(Start.Y - End.Y) < 0.0001 && Math.Abs(other.Start.Y - other.End.Y) < 0.0001 && Math.Abs(Start.Y - other.Start.Y) < 3) {
                return AxisOverlap(Start.X, End.X, other.Start.X, other.End.X);
            }

            if (Math.Abs(Start.X - End.X) < 0.0001 && Math.Abs(other.Start.X - other.End.X) < 0.0001 && Math.Abs(Start.X - other.Start.X) < 3) {
                return AxisOverlap(Start.Y, End.Y, other.Start.Y, other.End.Y);
            }

            return 0;
        }

        private static double AxisOverlap(double firstStart, double firstEnd, double secondStart, double secondEnd) {
            var firstMin = Math.Min(firstStart, firstEnd);
            var firstMax = Math.Max(firstStart, firstEnd);
            var secondMin = Math.Min(secondStart, secondEnd);
            var secondMax = Math.Max(secondStart, secondEnd);
            return Math.Max(0, Math.Min(firstMax, secondMax) - Math.Max(firstMin, secondMin));
        }
    }
}

internal readonly struct TopologyRoutePlan {
    public TopologyRoutePlan(List<ChartPoint> points, TopologyRouteDiagnostics diagnostics) {
        Points = points;
        Diagnostics = diagnostics;
    }

    public List<ChartPoint> Points { get; }

    public TopologyRouteDiagnostics Diagnostics { get; }
}

internal readonly struct TopologyRouteDiagnostics {
    public TopologyRouteDiagnostics(string strategy, string corridor, int segmentCount, int obstacleHits, int labelObstacleHits, double routeOverlapScore, int candidateCount, string fallbackReason) {
        Strategy = strategy;
        Corridor = corridor;
        SegmentCount = segmentCount;
        ObstacleHits = obstacleHits;
        LabelObstacleHits = labelObstacleHits;
        RouteOverlapScore = routeOverlapScore;
        CandidateCount = candidateCount;
        FallbackReason = fallbackReason;
    }

    public string Strategy { get; }

    public string Corridor { get; }

    public int SegmentCount { get; }

    public int ObstacleHits { get; }

    public int LabelObstacleHits { get; }

    public double RouteOverlapScore { get; }

    public int CandidateCount { get; }

    public string FallbackReason { get; }
}
