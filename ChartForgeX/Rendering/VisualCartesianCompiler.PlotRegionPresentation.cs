using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawPlotRegionBorder(VisualRenderContext context, VisualSceneBuilder builder, ResolvedPlotRegion region, VisualThemeColors colors) {
        var border = region.Source.Border; var bounds = region.Bounds;
        if (border == null || bounds.Width <= 0 || bounds.Height <= 0) return;
        var width = VisualReferenceStroke.PaintedWidth(border, colors.Axis, context.Theme.AxisStrokeWidth);
        if (width <= 0) return;
        var paint = VisualReferenceStroke.Paint(border, colors.Axis, SvgColorRole.Axis, out var color);
        var dx = Math.Min(width / 2, bounds.Width / 2); var dy = Math.Min(width / 2, bounds.Height / 2);
        var path = new ChartPath(new[] { ChartPathCommand.MoveTo(bounds.Left + dx, bounds.Top + dy),
            ChartPathCommand.LineTo(bounds.Right - dx, bounds.Top + dy), ChartPathCommand.LineTo(bounds.Right - dx, bounds.Bottom - dy),
            ChartPathCommand.LineTo(bounds.Left + dx, bounds.Bottom - dy) });
        using (builder.PushClip(bounds))
            builder.Path(path, stroke: color, strokeWidth: width, close: true, dash: border.ResolveDash(Array.Empty<double>()),
                cap: VisualStrokeCap.Butt, join: VisualStrokeJoin.Miter, role: "plot-region-border", paint: paint);
    }

    private static LabelCandidate[] PlotRegionCaptionCandidates(ChartPlotRegionLabelPosition position, ChartRect bounds) {
        if (position == ChartPlotRegionLabelPosition.Auto)
            return new[] { new LabelCandidate(bounds.Width / 2, bounds.Height / 2, .5, .5),
                new LabelCandidate(0, 0), new LabelCandidate(bounds.Width, 0, 1),
                new LabelCandidate(0, bounds.Height, 0, 1), new LabelCandidate(bounds.Width, bounds.Height, 1, 1) };
        var x = position is ChartPlotRegionLabelPosition.TopLeft or ChartPlotRegionLabelPosition.Left or ChartPlotRegionLabelPosition.BottomLeft ? 0
            : position is ChartPlotRegionLabelPosition.TopRight or ChartPlotRegionLabelPosition.Right or ChartPlotRegionLabelPosition.BottomRight ? 1 : .5;
        var y = position is ChartPlotRegionLabelPosition.TopLeft or ChartPlotRegionLabelPosition.Top or ChartPlotRegionLabelPosition.TopRight ? 0
            : position is ChartPlotRegionLabelPosition.BottomLeft or ChartPlotRegionLabelPosition.Bottom or ChartPlotRegionLabelPosition.BottomRight ? 1 : .5;
        return new[] { new LabelCandidate(bounds.Width * x, bounds.Height * y, x, y) };
    }
}
