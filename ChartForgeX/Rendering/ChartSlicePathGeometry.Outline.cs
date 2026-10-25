using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class ChartSlicePathGeometry {
    /// <summary>Uses the existing circular-arc path owner for styled separator strokes; ordinary fills retain their exact slice geometry.</summary>
    internal static ChartPath OutlinePath(double cx, double cy, double outer, double inner, double start, double sweep) {
        var commands = new List<ChartPathCommand>();
        var full = sweep >= Math.PI * 2 - .000001;
        Move(outer, start);
        ChartPathBuilder.AddCircularArc(commands, cx, cy, outer, start, full ? Math.PI * 2 : sweep, Math.PI / 8);
        if (inner > 0) {
            if (full) Move(inner, start); else Line(inner, start + sweep);
            ChartPathBuilder.AddCircularArc(commands, cx, cy, inner, full ? start : start + sweep, full ? -Math.PI * 2 : -sweep, Math.PI / 8);
            if (!full) Line(outer, start);
        } else if (!full) { commands.Add(ChartPathCommand.LineTo(cx, cy)); Line(outer, start); }
        return new ChartPath(commands);

        void Move(double radius, double angle) => commands.Add(ChartPathCommand.MoveTo(cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius));
        void Line(double radius, double angle) => commands.Add(ChartPathCommand.LineTo(cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius));
    }
}
