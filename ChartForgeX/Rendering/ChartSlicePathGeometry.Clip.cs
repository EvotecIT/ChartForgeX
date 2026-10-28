using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class ChartSlicePathGeometry {
    /// <summary>Builds one typed annular-sector clip for both native exporters; exterior samples are not an observation filter.</summary>
    internal static ChartPath ClipPath(double cx, double cy, double outer, double inner, double start, double sweep) {
        var commands = new List<ChartPathCommand>();
        if (outer <= inner || sweep <= 0) return new ChartPath(commands);
        sweep = Math.Min(Math.PI * 2, sweep);
        commands.Add(ChartPathCommand.MoveTo(cx + Math.Cos(start) * outer, cy + Math.Sin(start) * outer));
        ChartPathBuilder.AddCircularArc(commands, cx, cy, outer, start, sweep, Math.PI / 8);
        var end = start + sweep;
        commands.Add(ChartPathCommand.LineTo(cx + Math.Cos(end) * inner, cy + Math.Sin(end) * inner));
        if (inner > 0) ChartPathBuilder.AddCircularArc(commands, cx, cy, inner, end, -sweep, Math.PI / 8);
        return new ChartPath(commands);
    }
}
