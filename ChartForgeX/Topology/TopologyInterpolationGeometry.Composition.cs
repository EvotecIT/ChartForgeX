using System;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyInterpolationGeometry {
    internal static ChartPath JoinBodyAndTail(ChartPath body, ChartPath tail) {
        var end = body.Commands[body.Commands.Count - 1]; var start = tail.Commands[0];
        if (start.Kind != ChartPathCommandKind.MoveTo || end.X != start.X || end.Y != start.Y)
            throw new InvalidOperationException("An interpolated body and shared owner tail must meet exactly.");
        if (body.Commands.Count + tail.Commands.Count - 1 > TopologyPathDecoration.MaximumCommands)
            throw new ArgumentException("A complete interpolated trunk route exceeds 4096 commands.");
        return new ChartPath(Array.AsReadOnly(body.Commands.Concat(tail.Commands.Skip(1)).ToArray()));
    }}
