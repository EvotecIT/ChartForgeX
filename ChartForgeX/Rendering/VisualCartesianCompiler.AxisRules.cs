using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Moves an authored rule through the existing primary mapper without moving labels or expanding the plot.</summary>
    private static void DrawAxisRule(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartRange primaryRange, ChartMapper primaryMap, ChartAxis axis, bool horizontal, double edge, VisualThemeColors colors, string role) {
        if (!axis.RuleCrossAt.HasValue) {
            // Preserve the established command and ordering for unauthored crossings.
            builder.Line(horizontal ? plot.Left : edge, horizontal ? edge : plot.Top,
                horizontal ? plot.Right : edge, horizontal ? edge : plot.Bottom, colors.Axis, context.Theme.AxisStrokeWidth,
                role: role, paint: VisualChartPaint.Stroke(colors.Axis, SvgColorRole.Axis));
            return;
        }
        var value = axis.RuleCrossAt.Value;
        var metadata = AxisCrossingMetadata(value, horizontal, false);
        if (!TryAxisCrossingCoordinate(chart, primaryRange, primaryMap, value, horizontal, out var coordinate)) {
            metadata["data-cfx-visible"] = "false";
            using (builder.PushGroup(null, "axis-rule-crossing", metadata)) { }
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.axis-rule-outside-domain", "An authored axis rule crossing is outside the opposite primary physical-axis window and was omitted."));
            return;
        }
        using (builder.PushGroup(null, "axis-rule-crossing", metadata))
        using (builder.PushClip(plot))
            builder.Line(horizontal ? plot.Left : coordinate, horizontal ? coordinate : plot.Top,
                horizontal ? plot.Right : coordinate, horizontal ? coordinate : plot.Bottom, colors.Axis, context.Theme.AxisStrokeWidth,
                role: role, paint: VisualChartPaint.Stroke(colors.Axis, SvgColorRole.Axis));
    }

    private static Dictionary<string, string> AxisCrossingMetadata(double value, bool horizontal, bool complete) => new Dictionary<string, string> {
        [complete ? "data-cfx-cross-at" : "data-cfx-rule-cross-at"] = Number(value),
        ["data-cfx-crossing-axis"] = horizontal ? "primary-y" : "primary-x", ["data-cfx-passive"] = "true"
    };

    /// <summary>One source-window and projection owner for complete and rule-only crossings.</summary>
    private static bool TryAxisCrossingCoordinate(Chart chart, ChartRange primaryRange, ChartMapper primaryMap,
        double value, bool horizontal, out double coordinate) {
        coordinate = 0;
        var opposite = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var minimum = horizontal ? primaryRange.MinY : primaryRange.MinX;
        var maximum = horizontal ? primaryRange.MaxY : primaryRange.MaxX;
        if (value < minimum || value > maximum || value == maximum && opposite.PreparedViewportWindow?.MaximumInclusive == false) return false;
        coordinate = horizontal ? primaryMap.Y(value) : primaryMap.X(value);
        return true;
    }
}
