using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

/// <summary>Samples the resolved motion layers into immutable native scene commands.</summary>
internal static class TopologyMotionSceneAdapter {
    internal static VisualScene Sample(VisualScene basis, ResolvedTopologyGeometry geometry,
        ResolvedTopologyMotionVisual visual, double progress) {
        var underlay = new VisualSceneBuilder(basis.Size, geometry.Context.Font);
        var commandCount = 0;
        var left = Math.Max(0, geometry.Clip.Left); var top = Math.Max(0, geometry.Clip.Top);
        var right = Math.Min(basis.Size.Width, geometry.Clip.Right); var bottom = Math.Min(basis.Size.Height, geometry.Clip.Bottom);
        var visible = new ChartRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        using (underlay.PushClip(geometry.Clip)) {
            foreach (var route in visual.Routes) underlay.Path(Dashes(route, visual, visible, progress, ref commandCount),
                stroke: route.Paint.AtOpacity(ResolvedTopologyMotionVisual.RouteOpacity), strokeWidth: visual.RouteWidth,
                role: "topology-motion-route", paint: new VisualScenePaintBinding(stroke: route.Paint.PaintAtOpacity(ResolvedTopologyMotionVisual.RouteOpacity)));
        }
        var nodes = basis.Nodes.ToList();
        var insertion = nodes.FindIndex(node => node.Role == "topology-node-host" || node.Role == "topology-node");
        nodes.InsertRange(insertion < 0 ? nodes.Count : insertion, underlay.Build().Nodes);
        var builder = new VisualSceneBuilder(basis.Size, geometry.Context.Font);
        builder.Append(new VisualScene(basis.Size, nodes, basis.Diagnostics, basis.Regions));
        using (builder.PushClip(geometry.Clip)) {
            var sample = visual.Sample(progress);
            Circle(visual.HaloRadius, sample.Paint, ResolvedTopologyMotionVisual.HaloOpacity, "topology-motion-halo");
            Circle(visual.SurfaceRadius, visual.Background, ResolvedTopologyMotionVisual.SurfaceOpacity, "topology-motion-surface");
            builder.Ellipse(sample.Point.X, sample.Point.Y, visual.Radius, visual.Radius, sample.Paint.Color,
                visual.Background.Color, visual.MarkerStrokeWidth, "topology-motion-marker",
                paint: new VisualScenePaintBinding(sample.Paint.Paint, visual.Background.Paint));
            builder.Ellipse(sample.Point.X, sample.Point.Y, visual.SurfaceRadius, visual.SurfaceRadius, null,
                sample.Paint.AtOpacity(ResolvedTopologyMotionVisual.OutlineOpacity), visual.OutlineWidth, "topology-motion-outline",
                paint: new VisualScenePaintBinding(stroke: sample.Paint.PaintAtOpacity(ResolvedTopologyMotionVisual.OutlineOpacity)));
            foreach (var node in visual.Nodes) {
                var radius = visual.PulseRadius(progress); var opacity = visual.PulseOpacity(progress);
                using (builder.PushGroup(null, "topology-motion-node-source", new Dictionary<string, string> { ["data-node-id"] = node.Id }))
                    builder.Ellipse(node.Center.X, node.Center.Y, radius, radius, null, node.Paint.AtOpacity(opacity),
                        visual.MarkerStrokeWidth, "topology-motion-node", paint: new VisualScenePaintBinding(stroke: node.Paint.PaintAtOpacity(opacity)));
            }

            void Circle(double radius, MotionPaint paint, double opacity, string role) => builder.Ellipse(
                sample.Point.X, sample.Point.Y, radius, radius, paint.AtOpacity(opacity), role: role,
                paint: new VisualScenePaintBinding(paint.PaintAtOpacity(opacity)));
        }
        return builder.Build();
    }

    // Native scenes have no animated dash offset. Resolve the exact visible intervals along the captured polyline.
    private static ChartPath Dashes(MotionRoute route, ResolvedTopologyMotionVisual visual, ChartRect clip, double progress, ref int commandCount) {
        var commands = new List<ChartPathCommand>();
        if (clip.Width <= 0 || clip.Height <= 0) return new ChartPath(commands);
        var offset = visual.DashOffset(progress) % visual.DashPeriod;
        var walked = 0d; var drawing = false;
        var padding = visual.RouteWidth / 2 + 1;
        for (var i = 1; i < route.Points.Length; i++) {
            var start = route.Points[i - 1]; var end = route.Points[i];
            var length = ResolvedTopologyMotionVisual.Distance(start, end);
            var x0 = start.X; var y0 = start.Y; var x1 = end.X; var y1 = end.Y;
            if (!ChartPatternLineGeometry.ClipLineToRect(ref x0, ref y0, ref x1, ref y1,
                clip.Left - padding, clip.Top - padding, clip.Right + padding, clip.Bottom + padding)) {
                walked += length; drawing = false; continue;
            }
            var clippedStart = new ChartPoint(x0, y0); var clippedEnd = new ChartPoint(x1, y1);
            var skipped = ResolvedTopologyMotionVisual.Distance(start, clippedStart);
            var visibleLength = ResolvedTopologyMotionVisual.Distance(clippedStart, clippedEnd);
            // Each dash needs at most a move and a line; reject dense visible geometry before producing commands.
            var maximumCommands = 2 * (Math.Ceiling(visibleLength / visual.DashPeriod) + 2);
            if (commands.Count + maximumCommands > VisualArtifactInterchangeValidation.MaximumWaypointsPerEdge ||
                commandCount + commands.Count + maximumCommands > VisualArtifactInterchangeValidation.MaximumJsonValues)
                throw new InvalidOperationException("Sampled topology motion exceeds the supported visible route geometry budget.");
            if (skipped > 0) drawing = false;
            var position = 0d;
            while (position < visibleLength) {
                var phase = (walked + skipped + position + offset) % visual.DashPeriod;
                var ink = phase < visual.DashLength;
                var remaining = (ink ? visual.DashLength : visual.DashPeriod) - phase;
                var next = Math.Min(visibleLength, position + Math.Max(.000001, remaining));
                if (ink) {
                    if (!drawing) commands.Add(ChartPathCommand.MoveTo(x0 + (x1 - x0) * position / visibleLength,
                        y0 + (y1 - y0) * position / visibleLength));
                    commands.Add(ChartPathCommand.LineTo(x0 + (x1 - x0) * next / visibleLength,
                        y0 + (y1 - y0) * next / visibleLength));
                    if (commands.Count > VisualArtifactInterchangeValidation.MaximumWaypointsPerEdge ||
                        commandCount + commands.Count > VisualArtifactInterchangeValidation.MaximumJsonValues)
                        throw new InvalidOperationException("Sampled topology motion exceeds the supported visible route geometry budget.");
                }
                drawing = ink && next == visibleLength && skipped + visibleLength >= length;
                position = next;
            }
            walked += length;
        }
        commandCount += commands.Count;
        return new ChartPath(commands);
    }
}
