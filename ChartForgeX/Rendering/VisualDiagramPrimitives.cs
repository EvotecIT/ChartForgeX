using System;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Uses the shared prepared text and arrow geometry for the bounded diagram proof.</summary>
internal static class VisualDiagramPrimitives {
    internal static VisualRenderContext WithFrame(VisualRenderContext context, string? title, string? subtitle) {
        var frame = context.Frame;
        return new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode,
            frame.WithHeadings(frame.Title ?? title, frame.Subtitle ?? subtitle), context.Font);
    }

    internal static void Text(VisualSceneBuilder builder, string text, ChartRect bounds, double size,
        ChartColor color, int weight, string role, string? id = null) {
        if (string.IsNullOrEmpty(text)) return;
        var metrics = builder.MeasureText(text, size, weight);
        if (metrics.Width > bounds.Width + 0.001 || metrics.Height > bounds.Height + 0.001)
            throw new NotSupportedException($"Prepared diagram label '{id ?? role}' requires {metrics.Width:0.##} x {metrics.Height:0.##} logical pixels; available {bounds.Width:0.##} x {bounds.Height:0.##}. Shorten the label, add explicit line breaks, or enlarge its bounds.");
        builder.Text(text, bounds.X + bounds.Width / 2, bounds.Y + (bounds.Height - metrics.Height) / 2 + builder.TextAscent(size, weight),
            size, color, weight, role, id, TextAlignment.Center);
    }

    internal static void RequireInside(ChartRect bounds, ChartRect viewport, string id) {
        if (bounds.Left < viewport.Left - 0.001 || bounds.Top < viewport.Top - 0.001 ||
            bounds.Right > viewport.Right + 0.001 || bounds.Bottom > viewport.Bottom + 0.001)
            throw new NotSupportedException($"Prepared diagram content '{id}' exceeds its fixed viewport. Required bounds ({bounds.X:0.##}, {bounds.Y:0.##}, {bounds.Width:0.##}, {bounds.Height:0.##}); available ({viewport.X:0.##}, {viewport.Y:0.##}, {viewport.Width:0.##}, {viewport.Height:0.##}). Enlarge the common layout size or simplify the diagram.");
    }

    internal static void Arrow(VisualSceneBuilder builder, ChartPoint from, ChartPoint to, ChartColor color, string role) {
        if (Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y) < 0.001) return;
        var options = new TopologyRenderOptions { ArrowMarkerStyle = TopologyArrowMarkerStyle.Triangle };
        var a = TopologyArrowGeometry.Project(new ChartPoint(0, 0), from, to, options);
        var b = TopologyArrowGeometry.Project(new ChartPoint(10, 5), from, to, options);
        var c = TopologyArrowGeometry.Project(new ChartPoint(0, 10), from, to, options);
        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(a.X, a.Y), ChartPathCommand.LineTo(b.X, b.Y), ChartPathCommand.LineTo(c.X, c.Y) }), color, role: role, close: true);
    }
}
