using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>Deterministic region geometry, independent of SVG, browser and theme.</summary>
internal sealed class TopologyRegionLayout {
    internal readonly Dictionary<TopologyEdge, List<ChartPoint>> Routes = new();
    internal readonly Dictionary<string, List<TopologyEdge>> ExternalLinks = new(StringComparer.Ordinal);
    internal readonly bool Narrow;
    internal double Bottom { get; private set; }
    internal TopologyRegionLayout(TopologyChart chart, double width, double top, Func<string, double>? subtitleWidth = null) {
        Narrow = width < 640;
        var padding = 20d;
        var available = Math.Max(120, width - padding * 2);
        if (chart.Nodes.Any(node => node.GroupId == null)) {
            var id = "ungrouped";
            while (chart.Groups.Any(group => group.Id == id)) id += "-";
            chart.Groups.Add(new TopologyGroup { Id = id, Label = "Other" });
            foreach (var node in chart.Nodes.Where(node => node.GroupId == null)) node.GroupId = id;
        }
        var x = padding; var y = top; var rowHeight = 0d;
        foreach (var group in chart.Groups) {
            var nodes = chart.Nodes.Where(node => node.GroupId == group.Id)
                .OrderBy(node => node.Weight).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray();
            var columns = Math.Max(1, Math.Min(2, (int)Math.Floor((Narrow ? available : Math.Min(390, available)) / 130)));
            var groupWidth = Narrow ? available : Math.Min(available, 390);
            columns = Math.Min(columns, Math.Max(1, nodes.Length));
            var rows = Math.Max(1, (int)Math.Ceiling(nodes.Length / (double)columns));
            var nodeAreaHeight = Narrow ? rows * 120d + 75 : Math.Max(300, rows * 120d + 75);
            var links = chart.Edges.Where(edge => !edge.IsOverlay && chart.Nodes.Any(node => node.Id == edge.SourceNodeId && node.GroupId == group.Id)
                != chart.Nodes.Any(node => node.Id == edge.TargetNodeId && node.GroupId == group.Id))
                .Where(edge => chart.Nodes.Any(node => (node.Id == edge.SourceNodeId || node.Id == edge.TargetNodeId) && node.GroupId == group.Id)).ToList();
            ExternalLinks[group.Id] = Narrow ? links : new List<TopologyEdge>();
            var groupHeight = nodeAreaHeight + (Narrow && links.Count > 0 ? 34 + links.Count * 30 : 0);
            if (!Narrow && x > padding && x + groupWidth > width - padding) { x = padding; y += rowHeight + 40; rowHeight = 0; }
            if (!group.HasPositionOverride) { group.X = x; group.Y = y; group.HasPositionOverride = false; }
            group.Width = Math.Max(group.Width, groupWidth); group.Height = Math.Max(group.Height, groupHeight);
            var occupied = nodes.Where(node => node.HasPositionOverride).ToList();
            foreach (var node in nodes) {
                var radius = Radius(node.Weight, Narrow);
                node.Width = node.Height = radius * 2;
                node.Shape = TopologyNodeShape.Circle; node.PreserveDisplayModeSize = true;
                if (node.HasPositionOverride) continue;
                var slot = 0;
                while (true) {
                    var cx = group.X + (slot % columns + .5) * group.Width / columns;
                    if (nodes.Length % columns == 1 && slot == nodes.Length - 1) cx = group.X + group.Width / 2;
                    var cy = group.Y + 100 + slot / columns * 120 + (!Narrow && slot % columns == 1 ? 80 : 0);
                    if (!occupied.Any(other => Distance(cx, cy, other.X + other.Width / 2, other.Y + other.Height / 2) < 2.6 * Math.Max(radius, other.Width / 2))) {
                        node.X = cx - radius; node.Y = cy - radius; node.HasPositionOverride = false; break;
                    }
                    slot++;
                }
                occupied.Add(node);
                group.Height = Math.Max(group.Height, node.Y + node.Height + 42 - group.Y + (Narrow && links.Count > 0 ? 34 + links.Count * 30 : 0));
            }
            for (var i = 0; i < occupied.Count; i++) for (var j = i + 1; j < occupied.Count; j++) {
                var a = occupied[i]; var b = occupied[j];
                if (Distance(a.X + a.Width / 2, a.Y + a.Height / 2, b.X + b.Width / 2, b.Y + b.Height / 2) < (a.Width + b.Width) / 2 + 4)
                    throw new InvalidOperationException("Manual region node positions overlap: " + a.Id + " and " + b.Id + ".");
            }
            Bottom = Math.Max(Bottom, group.Y + group.Height);
            if (Narrow) y = group.Y + group.Height + 24;
            else { x = group.X + group.Width + 40; rowHeight = Math.Max(rowHeight, group.Height); }
        }
        var overlayIndex = 0;
        foreach (var edge in chart.Edges) {
            var source = chart.Nodes.FirstOrDefault(node => node.Id == edge.SourceNodeId);
            var target = chart.Nodes.FirstOrDefault(node => node.Id == edge.TargetNodeId);
            if (source == null || target == null) throw new InvalidOperationException("Region edges require node endpoints: " + edge.Id);
            if (edge.IsOverlay) {
                var yOverlay = Bottom + 30 + overlayIndex++ * 44;
                var right = chart.Groups.Count > 0 ? Math.Min(width - padding, chart.Groups.Max(group => group.X + group.Width)) : width - padding;
                Routes[edge] = SampleCurve(new(padding + 30, yOverlay), new((padding + right) / 2, yOverlay + 40), new(right - 30, yOverlay));
            } else if (Narrow && source.GroupId != target.GroupId) Routes[edge] = new List<ChartPoint>();
            else Routes[edge] = Route(chart, edge, source, target, subtitleWidth);
        }
        Bottom += overlayIndex * 44;
        chart.PreparedRegionRoutes = new System.Collections.ObjectModel.ReadOnlyDictionary<string, IReadOnlyList<ChartPoint>>(
            Routes.ToDictionary(pair => pair.Key.Id, pair => (IReadOnlyList<ChartPoint>)Array.AsReadOnly(pair.Value.ToArray()), StringComparer.Ordinal));
    }

    internal static double Radius(TopologyNodeWeight weight, bool narrow) => weight switch {
        TopologyNodeWeight.Primary => narrow ? 26 : 30,
        TopologyNodeWeight.Minor => narrow ? 18 : 20,
        _ => narrow ? 22 : 26
    };
    internal static double Distance(double x1, double y1, double x2, double y2) => Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
    internal static ChartPoint Along(ChartPoint a, ChartPoint b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    private static List<ChartPoint> Route(TopologyChart chart, TopologyEdge edge, TopologyNode source, TopologyNode target, Func<string, double>? subtitleWidth) {
        var a = new ChartPoint(source.X + source.Width / 2, source.Y + source.Height / 2);
        var b = new ChartPoint(target.X + target.Width / 2, target.Y + target.Height / 2);
        var middle = Along(a, b, .5); var length = Math.Max(1, Distance(a.X, a.Y, b.X, b.Y));
        var pair = chart.Edges.Where(candidate => !candidate.IsOverlay && (candidate.SourceNodeId == edge.SourceNodeId && candidate.TargetNodeId == edge.TargetNodeId
            || candidate.SourceNodeId == edge.TargetNodeId && candidate.TargetNodeId == edge.SourceNodeId)).ToArray();
        var bow = pair.Length > 1 ? (Array.IndexOf(pair, edge) - (pair.Length - 1) / 2d) * 16 : 0;
        if (pair.Length > 1 && string.CompareOrdinal(edge.SourceNodeId, edge.TargetNodeId) > 0) bow = -bow;
        var obstacles = chart.Nodes.Where(node => node != source && node != target).ToArray();
        List<ChartPoint>? coreSafeFallback = null;
        foreach (var displacement in new[] { bow, 25d, -25d, 50d, -50d, 75d, -75d, 100d, -100d, 140d, -140d, 210d, -210d }) {
            var bend = new ChartPoint(middle.X - (b.Y - a.Y) / length * displacement, middle.Y + (b.X - a.X) / length * displacement);
            if (bend.X < 4 || bend.X > chart.Viewport.Width - 4 || bend.Y < 4
                || obstacles.Any(node => Intersects(a, bend, node) || Intersects(bend, b, node))) continue;
            var firstDirection = displacement == 0 ? b : bend;
            var lastDirection = displacement == 0 ? a : bend;
            var start = Along(a, firstDirection, source.Width / 2 / Math.Max(1, Distance(a.X, a.Y, firstDirection.X, firstDirection.Y)));
            var end = Along(b, lastDirection, target.Width / 2 / Math.Max(1, Distance(b.X, b.Y, lastDirection.X, lastDirection.Y)));
            coreSafeFallback ??= displacement == 0 ? new() { start, end } : new() { start, bend, end };
            if (chart.Nodes.Any(node => CaptionIntersects(a, bend, node, subtitleWidth) || CaptionIntersects(bend, b, node, subtitleWidth))) continue;
            if (displacement == 0) return new() { start, end };
            if (pair.Length > 1 && displacement == bow) {
                var control = new ChartPoint(middle.X + (bend.X - middle.X) * 2, middle.Y + (bend.Y - middle.Y) * 2);
                var curve = SampleCurve(start, control, end);
                if (!Enumerable.Range(1, curve.Count - 1).Any(index => obstacles.Any(node => Intersects(curve[index - 1], curve[index], node)))) return curve;
            }
            return new() { start, bend, end };
        }
        return coreSafeFallback ?? throw new InvalidOperationException("No single-bend route clears the region node cores inside the viewport: " + edge.Id);
    }
    private static List<ChartPoint> SampleCurve(ChartPoint a, ChartPoint control, ChartPoint b) {
        var points = new List<ChartPoint>();
        for (var step = 0; step <= 32; step++) {
            var t = step / 32d; var u = 1 - t;
            points.Add(new ChartPoint(u * u * a.X + 2 * u * t * control.X + t * t * b.X, u * u * a.Y + 2 * u * t * control.Y + t * t * b.Y));
        }
        return points;
    }
    private static bool Intersects(ChartPoint a, ChartPoint b, TopologyNode node) {
        var x = node.X + node.Width / 2; var y = node.Y + node.Height / 2;
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var t = Math.Max(0, Math.Min(1, ((x - a.X) * dx + (y - a.Y) * dy) / Math.Max(1, dx * dx + dy * dy)));
        return Distance(x, y, a.X + t * dx, a.Y + t * dy) < node.Width / 2 + 5;
    }
    private static bool CaptionIntersects(ChartPoint a, ChartPoint b, TopologyNode node, Func<string, double>? subtitleWidth) {
        if (string.IsNullOrWhiteSpace(node.Subtitle)) return false;
        var width = Math.Min(140, subtitleWidth?.Invoke(node.Subtitle!) ?? 140) + 4;
        var left = node.X + node.Width / 2 - width / 2; var top = node.Y + node.Height + 4;
        var t0 = 0d; var t1 = 1d;
        bool Slab(double start, double delta, double min, double max) {
            if (Math.Abs(delta) < .000001) return start < min || start > max;
            var near = (min - start) / delta; var far = (max - start) / delta;
            if (near > far) (near, far) = (far, near);
            t0 = Math.Max(t0, near); t1 = Math.Min(t1, far); return t0 > t1;
        }
        return !Slab(a.X, b.X - a.X, left, left + width) && !Slab(a.Y, b.Y - a.Y, top, top + 16);
    }
}
