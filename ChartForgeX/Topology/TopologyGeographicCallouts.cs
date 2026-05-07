using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

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
        var nodeBoxes = chart.Nodes.Select(node => CalloutBox.FromRect(node.X - 8, node.Y - 8, node.Width + 16, node.Height + 16)).ToList();
        var placed = new List<CalloutBox>();
        var nodesByGroup = chart.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.GroupId))
            .GroupBy(node => node.GroupId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        var callouts = new List<TopologyGeographicCallout>();
        foreach (var group in chart.Groups
                     .Where(group => group.Longitude.HasValue && group.Latitude.HasValue)
                     .OrderBy(group => group.Longitude!.Value)
                     .ThenBy(group => group.Id, StringComparer.Ordinal)
                     .Take(options.GeographicCalloutMaxItems)) {
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
            var placement = Place(anchor.X, anchor.Y, width, height, map, chart, placed, nodeBoxes);
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

    private static (double X, double Y, string Name) Place(double anchorX, double anchorY, double width, double height, ChartRect map, TopologyChart chart, IReadOnlyList<CalloutBox> placed, IReadOnlyList<CalloutBox> nodeBoxes) {
        var candidates = CandidatePositions(anchorX, anchorY, width, height, map);
        var best = candidates[0];
        var bestScore = double.MaxValue;
        foreach (var candidate in candidates) {
            var box = CalloutBox.FromRect(candidate.X, candidate.Y, width, height);
            var score = OverlapScore(box, placed) * 12 + OverlapScore(box, nodeBoxes) * 3 + Distance(anchorX, anchorY, candidate.X + width / 2, candidate.Y + height / 2) * 0.04;
            if (candidate.Y < chart.Viewport.Padding + 54) score += 900;
            if (score < bestScore) {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private static List<(double X, double Y, string Name)> CandidatePositions(double anchorX, double anchorY, double width, double height, ChartRect map) {
        var left = map.Left + 14;
        var right = map.Right - width - 14;
        var top = map.Top + 18;
        var bottom = map.Bottom - height - 18;
        var y = Clamp(anchorY + 62, top, bottom);
        var preferredSide = anchorX < map.Left + map.Width / 2 ? "left" : "right";
        var primaryX = preferredSide == "left" ? left : right;
        var secondaryX = preferredSide == "left" ? right : left;
        return new List<(double X, double Y, string Name)> {
            (primaryX, y, preferredSide),
            (primaryX, Clamp(anchorY - height - 34, top, bottom), preferredSide + "-above"),
            (secondaryX, y, preferredSide == "left" ? "right" : "left"),
            (Clamp(anchorX - width / 2, left, right), top, "top"),
            (Clamp(anchorX - width / 2, left, right), bottom, "bottom"),
            (secondaryX, Clamp(anchorY - height - 34, top, bottom), preferredSide == "left" ? "right-above" : "left-above")
        };
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

        public double OverlapArea(CalloutBox other) {
            var width = Math.Max(0, Math.Min(Right, other.Right) - Math.Max(Left, other.Left));
            var height = Math.Max(0, Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top));
            return width * height;
        }
    }
}
