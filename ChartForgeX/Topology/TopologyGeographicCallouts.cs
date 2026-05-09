using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed class TopologyGeographicCallout {
    public TopologyGeographicCallout(TopologyGroup group, string label, string subtitle, string accentColor, double x, double y, double width, double height, double anchorX, double anchorY, string placement, int healthyCount, int warningCount, int criticalCount, int unknownCount, int disabledCount) {
        Group = group;
        Label = label;
        Subtitle = subtitle;
        AccentColor = accentColor;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        AnchorX = anchorX;
        AnchorY = anchorY;
        Placement = placement;
        HealthyCount = healthyCount;
        WarningCount = warningCount;
        CriticalCount = criticalCount;
        UnknownCount = unknownCount;
        DisabledCount = disabledCount;
    }

    public TopologyGroup Group { get; }

    public string Label { get; }

    public string Subtitle { get; }

    public string AccentColor { get; }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    public double AnchorX { get; }

    public double AnchorY { get; }

    public string Placement { get; }

    public int HealthyCount { get; }

    public int WarningCount { get; }

    public int CriticalCount { get; }

    public int UnknownCount { get; }

    public int DisabledCount { get; }

    public int NodeCount => HealthyCount + WarningCount + CriticalCount + UnknownCount + DisabledCount;
}

internal static class TopologyGeographicCallouts {
    public static List<TopologyGeographicCallout> Build(TopologyChart chart, TopologyRenderOptions options, TopologyTheme theme) {
        if (!options.IncludeGeographicCallouts || chart.LayoutMode != TopologyLayoutMode.Geographic || options.GeographicCalloutMaxItems <= 0) return new List<TopologyGeographicCallout>();

        var map = TopologyMapProjection.MapRect(chart);
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var renderedNodes = chart.Nodes
            .Where(node => EffectiveNodeDisplayMode(node, options) != TopologyNodeDisplayMode.Hidden)
            .ToList();
        var nodeBoxes = renderedNodes
            .Select(node => CalloutBox.FromNodeVisual(node, options))
            .ToList();
        var obstacleBoxes = new List<CalloutBox>(nodeBoxes);
        if (options.IncludeEdgeLabels) {
            obstacleBoxes.AddRange(EdgeLabelLayouts(chart, options).Select(layout => CalloutBox.FromRect(layout.CenterX - layout.Width / 2 - 8, layout.CenterY - layout.Height / 2 - 8, layout.Width + 16, layout.Height + 16)));
        }

        obstacleBoxes.AddRange(RouteObstacleBoxes(chart, nodes));
        var placed = new List<CalloutBox>();
        var nodesByGroup = renderedNodes
            .Where(node => !string.IsNullOrWhiteSpace(node.GroupId))
            .GroupBy(node => node.GroupId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        var eligibleGroups = chart.Groups
                      .Where(group => group.Longitude.HasValue && group.Latitude.HasValue)
                      .OrderBy(group => options.SelectedGroupIds.Contains(group.Id) ? 0 : 1)
                      .ThenBy(group => group.Longitude!.Value)
                      .ThenBy(group => group.Id, StringComparer.Ordinal)
                      .Take(options.GeographicCalloutMaxItems)
                      .ToList();
        var longitudeRank = eligibleGroups
            .OrderBy(group => group.Longitude!.Value)
            .Select((group, index) => new { group.Id, Index = index })
            .ToDictionary(item => item.Id, item => item.Index, StringComparer.Ordinal);

        var callouts = new List<TopologyGeographicCallout>();
        foreach (var group in eligibleGroups) {
            var members = nodesByGroup.TryGetValue(group.Id, out var groupedNodes) ? groupedNodes : new List<TopologyNode>();
            var anchor = TopologyMapProjection.Project(map, chart.MapViewport, group.Longitude!.Value, group.Latitude!.Value);
            var label = group.Metadata.TryGetValue("calloutLabel", out var metadataLabel) && !string.IsNullOrWhiteSpace(metadataLabel) ? metadataLabel : group.Label;
            var subtitle = group.Metadata.TryGetValue("calloutSubtitle", out var metadataSubtitle) && !string.IsNullOrWhiteSpace(metadataSubtitle)
                ? metadataSubtitle
                : !string.IsNullOrWhiteSpace(group.Subtitle)
                    ? group.Subtitle!
                    : members.Count.ToString(CultureInfo.InvariantCulture) + " nodes";
            var width = 186.0;
            var height = 92.0;
            var preferredPlacement = PreferredPlacement(group, longitudeRank, options.PreferGeographicCalloutMapEdges);
            var placement = Place(anchor.X, anchor.Y, width, height, map, chart, placed, obstacleBoxes, options.PreferGeographicCalloutMapEdges, preferredPlacement);
            var box = CalloutBox.FromRect(placement.X, placement.Y, width, height);
            placed.Add(box);
            callouts.Add(new TopologyGeographicCallout(
                group,
                label,
                subtitle,
                string.IsNullOrWhiteSpace(group.Color) ? theme.StatusColor(group.Status) : group.Color!.Trim(),
                placement.X,
                placement.Y,
                width,
                height,
                anchor.X,
                anchor.Y,
                placement.Name,
                members.Count(node => node.Status == TopologyHealthStatus.Healthy),
                members.Count(node => node.Status == TopologyHealthStatus.Warning),
                members.Count(node => node.Status == TopologyHealthStatus.Critical),
                members.Count(node => node.Status == TopologyHealthStatus.Unknown),
                members.Count(node => node.Status == TopologyHealthStatus.Disabled)));
        }

        return callouts;
    }

    private static string? PreferredPlacement(TopologyGroup group, IReadOnlyDictionary<string, int> longitudeRank, bool preferMapEdges) {
        if (group.Metadata.TryGetValue("calloutPlacement", out var metadataPlacement) && !string.IsNullOrWhiteSpace(metadataPlacement)) return metadataPlacement.Trim();
        if (!preferMapEdges || longitudeRank.Count == 0 || !longitudeRank.TryGetValue(group.Id, out var rank)) return null;
        if (rank == 0) return "left-corner";
        if (rank == longitudeRank.Count - 1) return "right-corner";
        return "top";
    }

    private static (double X, double Y, string Name) Place(double anchorX, double anchorY, double width, double height, ChartRect map, TopologyChart chart, IReadOnlyList<CalloutBox> placed, IReadOnlyList<CalloutBox> nodeBoxes, bool preferMapEdges, string? preferredPlacement) {
        var candidates = CandidatePositions(anchorX, anchorY, width, height, map, preferMapEdges, preferredPlacement);
        var best = candidates[0];
        var bestScore = double.MaxValue;
        foreach (var candidate in candidates) {
            var adjusted = AdjustCandidate(candidate, width, height, map, nodeBoxes);
            var box = CalloutBox.FromRect(adjusted.X, adjusted.Y, width, height);
            var score = OverlapScore(box, placed) * 12 + OverlapScore(box, nodeBoxes) * 3 + Distance(anchorX, anchorY, adjusted.X + width / 2, adjusted.Y + height / 2) * 0.04;
            if (!string.IsNullOrWhiteSpace(preferredPlacement) && string.Equals(adjusted.Name, preferredPlacement, StringComparison.OrdinalIgnoreCase)) score -= 12000;
            if (adjusted.Y < chart.Viewport.Padding + 54) score += 900;
            if (score < bestScore) {
                best = adjusted;
                bestScore = score;
            }
        }

        return best;
    }

    private static IEnumerable<CalloutBox> RouteObstacleBoxes(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        foreach (var edge in chart.Edges) {
            if (!ShouldReserveGeographicCalloutRouteObstacle(edge)) continue;
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            var points = EdgePoints(chart, edge, nodes);
            if (points.Count < 2) continue;
            var samples = IsGeographicCurve(chart, edge, nodes)
                ? GeographicCurveSamplePoints(chart, edge, nodes, points, 16)
                : points;
            for (var i = 0; i < samples.Count; i += 3) {
                var point = samples[i];
                yield return CalloutBox.FromRect(point.X - 17, point.Y - 13, 34, 26);
            }
        }
    }

    private static (double X, double Y, string Name) AdjustCandidate((double X, double Y, string Name) candidate, double width, double height, ChartRect map, IReadOnlyList<CalloutBox> nodeBoxes) {
        var top = map.Top + 18;
        var bottom = map.Bottom - height - 18;
        var best = candidate;
        var bestScore = OverlapScore(CalloutBox.FromRect(candidate.X, candidate.Y, width, height), nodeBoxes);
        if (bestScore <= 0.0001) return best;

        var shifts = new[] { -30.0, -60.0, 30.0, 60.0, -90.0, 90.0 };
        foreach (var shift in shifts) {
            var y = Clamp(candidate.Y + shift, top, bottom);
            if (Math.Abs(y - candidate.Y) < 0.0001) continue;
            var score = OverlapScore(CalloutBox.FromRect(candidate.X, y, width, height), nodeBoxes) + Math.Abs(shift) * 0.9;
            if (score < bestScore) {
                best = (candidate.X, y, candidate.Name);
                bestScore = score;
            }
        }

        return best;
    }

    private static List<(double X, double Y, string Name)> CandidatePositions(double anchorX, double anchorY, double width, double height, ChartRect map, bool preferMapEdges, string? preferredPlacement) {
        var left = map.Left + 14;
        var right = map.Right - width - 14;
        var top = map.Top + 18;
        var bottom = map.Bottom - height - 18;
        var y = Clamp(anchorY + 62, top, bottom);
        var preferredSide = anchorX < map.Left + map.Width / 2 ? "left" : "right";
        var primaryX = preferredSide == "left" ? left : right;
        var secondaryX = preferredSide == "left" ? right : left;
        var positions = new List<(double X, double Y, string Name)>();
        if (preferMapEdges || IsExplicitMapEdgePlacement(preferredPlacement)) {
            positions.Add((primaryX, bottom, preferredSide + "-corner"));
            positions.Add((secondaryX, bottom, preferredSide == "left" ? "right-corner" : "left-corner"));
            positions.Add((primaryX, top, preferredSide + "-top-corner"));
            positions.Add((secondaryX, top, preferredSide == "left" ? "right-top-corner" : "left-top-corner"));
        }

        positions.AddRange(new[] {
            (primaryX, y, preferredSide),
            (primaryX, Clamp(anchorY - height - 34, top, bottom), preferredSide + "-above"),
            (secondaryX, y, preferredSide == "left" ? "right" : "left"),
            (Clamp(anchorX - width / 2, left, right), top, "top"),
            (Clamp(anchorX - width / 2, left, right), bottom, "bottom"),
            (secondaryX, Clamp(anchorY - height - 34, top, bottom), preferredSide == "left" ? "right-above" : "left-above")
        });
        return positions;
    }

    private static bool IsExplicitMapEdgePlacement(string? placement) {
        var normalized = placement == null ? string.Empty : placement.Trim();
        if (normalized.Length == 0) return false;
        return normalized.EndsWith("-corner", StringComparison.OrdinalIgnoreCase);
    }

    private static double OverlapScore(CalloutBox box, IReadOnlyList<CalloutBox> others) {
        var score = 0.0;
        foreach (var other in others) score += box.OverlapArea(other);
        return score;
    }

    private static double Distance(double x1, double y1, double x2, double y2) {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

    private readonly struct CalloutBox {
        private CalloutBox(double left, double top, double right, double bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        private double Left { get; }

        private double Top { get; }

        private double Right { get; }

        private double Bottom { get; }

        public static CalloutBox FromRect(double x, double y, double width, double height) => new(x, y, x + width, y + height);

        public static CalloutBox FromNodeVisual(TopologyNode node, TopologyRenderOptions options) {
            var mode = EffectiveNodeDisplayMode(node, options);
            if (mode == TopologyNodeDisplayMode.Dot) {
                var badgeWidth = string.IsNullOrWhiteSpace(node.Badge) ? 0 : Math.Max(18, node.Badge!.Length * 6.5 + 12);
                var visualWidth = Math.Max(node.Width + 20, badgeWidth + 20);
                var centerX = node.X + node.Width / 2;
                return FromRect(centerX - visualWidth / 2, node.Y - 22, visualWidth, node.Height + 44);
            }

            if (mode == TopologyNodeDisplayMode.Icon) {
                var labelWidth = options.IncludeIconLabels ? Math.Max(node.Width + 46, 72) : node.Width;
                var centerX = node.X + node.Width / 2;
                return FromRect(centerX - labelWidth / 2 - 8, node.Y - 8, labelWidth + 16, node.Height + (options.IncludeIconLabels ? 34 : 16));
            }

            return FromRect(node.X - 10, node.Y - 10, node.Width + 20, node.Height + 20);
        }

        public double OverlapArea(CalloutBox other) {
            var width = Math.Max(0, Math.Min(Right, other.Right) - Math.Max(Left, other.Left));
            var height = Math.Max(0, Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top));
            return width * height;
        }
    }
}
