using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

/// <summary>Captured paint, style and distance-based timing shared by animated SVG and native motion frames.</summary>
internal sealed class ResolvedTopologyMotionVisual {
    internal ResolvedTopologyMotionVisual(TopologyMotionPlan plan, TopologyMotionOptions motion, ResolvedTopologyGeometry geometry) {
        SourceId = plan.SourceId;
        Motion = motion.Clone();
        Scale = geometry.Scale;
        Radius = motion.MarkerRadius * Scale;
        Background = new MotionPaint(geometry.Background, SvgColorRole.Surface, geometry);
        var walked = 0d;
        Routes = plan.Entries.Select(entry => {
            var route = new MotionRoute(entry, Paint(entry.Edge.Color, entry.Edge.Status), walked, plan.TotalLength);
            walked += entry.Length;
            return route;
        }).ToArray();
        TotalLength = plan.TotalLength;
        Nodes = plan.NodeIds.Select(id => geometry.Chart.Nodes.FirstOrDefault(node => node.Id == id))
            .OfType<TopologyNode>().Where(node => geometry.NodeCenters.ContainsKey(node.Id))
            .Select(node => new MotionNode(node.Id, geometry.NodeCenters[node.Id], Paint(node.Color, node.Status))).ToArray();

        MotionPaint Paint(string? authored, TopologyHealthStatus status) {
            var selected = motion.MarkerColor ?? plan.Color ?? authored;
            return new MotionPaint(geometry.ResolveColor(selected, status),
                string.IsNullOrWhiteSpace(selected) ? SvgColorRole.Status : SvgColorRole.Any, geometry);
        }
    }

    internal string SourceId { get; }
    internal TopologyMotionOptions Motion { get; }
    internal MotionRoute[] Routes { get; }
    internal MotionNode[] Nodes { get; }
    internal MotionPaint Background { get; }
    internal double TotalLength { get; }
    internal double Scale { get; }
    internal double Radius { get; }
    internal double HaloRadius => Radius + 7 * Scale;
    internal double SurfaceRadius => Radius + 3 * Scale;
    internal double MarkerStrokeWidth => 2 * Scale;
    internal double OutlineWidth => 1.5 * Scale;
    internal double RouteWidth => Math.Max(2.5 * Scale, Radius * .7);
    internal double DashLength => 10 * Scale;
    internal double DashPeriod => 26 * Scale;
    internal const double RouteOpacity = .76;
    internal const double HaloOpacity = 42d / 255;
    internal const double SurfaceOpacity = 230d / 255;
    internal const double OutlineOpacity = 180d / 255;
    internal const double PulseLowOpacity = .18;
    internal const double PulseHighOpacity = .62;
    internal double PulseLowRadius => Radius + Scale;
    internal double PulseHighRadius => Radius + 8 * Scale;

    internal double Phase(double progress) => Motion.Loop && progress == 1 ? 0 : progress;
    internal double DashOffset(double progress) => DashPeriod * (1 - Phase(progress));
    internal double PulseStrength(double progress) => 1 - Math.Abs(2 * Phase(progress) - 1);
    internal double PulseRadius(double progress) => PulseLowRadius + (PulseHighRadius - PulseLowRadius) * PulseStrength(progress);
    internal double PulseOpacity(double progress) => PulseLowOpacity + (PulseHighOpacity - PulseLowOpacity) * PulseStrength(progress);

    internal (ChartPoint Point, MotionPaint Paint) Sample(double progress) {
        var phase = Phase(progress);
        var distance = TotalLength * phase;
        for (var i = Routes.Length - 1; i >= 0; i--) {
            var route = Routes[i];
            // Select directly from the same normalized stops as SVG, avoiding rounding at a route boundary.
            if (phase >= route.StartProgress)
                return (PointAtDistance(route.Points, Math.Max(0, distance - route.StartDistance)), route.Paint);
        }
        throw new InvalidOperationException("Resolved topology motion has no routes.");
    }

    internal static ChartPoint PointAtDistance(IReadOnlyList<ChartPoint> points, double distance) {
        var walked = 0d;
        for (var i = 1; i < points.Count; i++) {
            var start = points[i - 1]; var end = points[i];
            var segment = Distance(start, end);
            if (walked + segment >= distance) {
                var fraction = segment <= .0001 ? 0 : (distance - walked) / segment;
                return new ChartPoint(start.X + (end.X - start.X) * fraction, start.Y + (end.Y - start.Y) * fraction);
            }
            walked += segment;
        }
        return points[points.Count - 1];
    }

    internal static double Distance(ChartPoint start, ChartPoint end) =>
        Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
}

internal sealed class MotionPaint {
    internal MotionPaint(ChartColor color, SvgColorRole role, ResolvedTopologyGeometry geometry) {
        Color = color; Paint = SvgPaint.Of(color, role); Svg = geometry.PaintColor(color, role);
    }
    internal ChartColor Color { get; }
    internal SvgPaint Paint { get; }
    internal string Svg { get; }
    internal ChartColor AtOpacity(double opacity) => Color.WithAlpha((byte)Math.Round(Color.A * opacity));
    internal SvgPaint PaintAtOpacity(double opacity) => Paint.WithOpacity(AtOpacity(opacity), opacity);
}

internal sealed class MotionRoute {
    internal MotionRoute(TopologyMotionEntry entry, MotionPaint paint, double startDistance, double totalLength) {
        Id = entry.Edge.Id; Points = entry.Points.ToArray(); Paint = paint;
        StartDistance = startDistance; StartProgress = startDistance / totalLength;
    }
    internal string Id { get; }
    internal ChartPoint[] Points { get; }
    internal double StartDistance { get; }
    internal MotionPaint Paint { get; }
    internal double StartProgress { get; }
}

internal sealed class MotionNode {
    internal MotionNode(string id, ChartPoint center, MotionPaint paint) { Id = id; Center = center; Paint = paint; }
    internal string Id { get; }
    internal ChartPoint Center { get; }
    internal MotionPaint Paint { get; }
}
