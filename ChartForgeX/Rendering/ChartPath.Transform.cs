using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartPath {
    /// <summary>Projects numeric endpoints and controls once while retaining subpaths and command order.</summary>
    internal ChartPath Transform(Func<ChartPoint, ChartPoint> project) {
        if (project == null) throw new ArgumentNullException(nameof(project));
        var commands = new List<ChartPathCommand>(Commands.Count);
        foreach (var command in Commands) {
            var end = project(new ChartPoint(command.X, command.Y));
            if (command.Kind == ChartPathCommandKind.MoveTo) commands.Add(ChartPathCommand.MoveTo(end.X, end.Y));
            else if (command.Kind == ChartPathCommandKind.LineTo) commands.Add(ChartPathCommand.LineTo(end.X, end.Y));
            else {
                var first = project(new ChartPoint(command.Control1X, command.Control1Y));
                var second = project(new ChartPoint(command.Control2X, command.Control2Y));
                commands.Add(ChartPathCommand.CubicTo(first.X, first.Y, second.X, second.Y, end.X, end.Y));
            }
        }
        return new ChartPath(commands);
    }
}
