using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    /// <summary>Projects one retained source instant through the captured primary schedule axis.</summary>
    internal static double ScheduleX(ChartAxis axis, double value, double minimum, double maximum, ChartRect plot) =>
        plot.Left + ScheduleFraction(axis, value, minimum, maximum) * plot.Width;

    internal static double ScheduleFraction(ChartAxis axis, double value, double minimum, double maximum, bool physical = true) {
        var source = Math.Max(minimum, Math.Min(maximum, value));
        return physical ? ChartScaleTransform.Normalize(source, minimum, maximum, axis)
            : ChartScaleTransform.Normalize(ChartScaleTransform.Forward(source, axis), ChartScaleTransform.Bound(minimum, axis, false),
                ChartScaleTransform.Bound(maximum, axis, true), false);
    }

    private static double ScheduleRowCenter(Chart chart, ChartRect plot, double offset) =>
        chart.Options.YAxis.Reversed ? plot.Bottom - offset : plot.Top + offset;

    internal static double ScheduleRowTop(Chart chart, ChartRect plot, double logicalTop, double height) =>
        chart.Options.YAxis.Reversed ? plot.Top + plot.Bottom - logicalTop - height : logicalTop;

    private static ChartRect ScheduleCanonicalX(Chart chart, ChartRect plot, ChartRect rectangle) => chart.Options.XAxis.Reversed
        ? new ChartRect(plot.Left + plot.Right - rectangle.Right, rectangle.Top, rectangle.Width, rectangle.Height) : rectangle;

    private static ChartPathCommand[] SchedulePhysicalPath(Chart chart, ChartRect plot, IReadOnlyList<ChartPathCommand> commands) {
        if (!chart.Options.XAxis.Reversed) return commands.ToArray();
        double X(double value) => plot.Left + plot.Right - value;
        return commands.Select(command => command.Kind == ChartPathCommandKind.MoveTo ? ChartPathCommand.MoveTo(X(command.X), command.Y)
            : command.Kind == ChartPathCommandKind.LineTo ? ChartPathCommand.LineTo(X(command.X), command.Y)
            : ChartPathCommand.CubicTo(X(command.Control1X), command.Control1Y, X(command.Control2X), command.Control2Y, X(command.X), command.Y)).ToArray();
    }
}
