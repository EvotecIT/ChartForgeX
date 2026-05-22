using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawMotionOverlay(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyMotionPlan? plan = null) {
        plan ??= TopologyMotionPlanner.Build(chart, options);
        if (plan == null) return;
        var sample = TopologyMotionPlanner.Sample(plan, options, theme);
        var color = ChartColor.TryParse(sample.Color, out var parsed) ? parsed : Color(theme.Accent);
        var radius = options.Motion!.MarkerRadius;
        canvas.DrawCircle(sample.Point.X, sample.Point.Y, radius + 7, WithAlpha(color, 42));
        canvas.DrawCircle(sample.Point.X, sample.Point.Y, radius + 3, WithAlpha(Color(theme.Background), 230));
        canvas.DrawCircle(sample.Point.X, sample.Point.Y, radius, color);
        canvas.DrawCircleOutline(sample.Point.X, sample.Point.Y, radius + 3, WithAlpha(color, 180), 1.5);
    }
}
