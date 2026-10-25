using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    private static IReadOnlyList<ScenePart> BraceParts(TopologyNode node, bool left, bool right) {
        var parts = new List<ScenePart> { CatalogLogicalBox(node) };
        if (left) parts.Add(BracePart(node, false));
        if (right) parts.Add(BracePart(node, true));
        return parts;
    }
    private static ScenePart BracePart(TopologyNode node, bool mirror) {
        double X(double fraction) => node.X + node.Width * (mirror ? 1 - fraction : fraction);
        double Y(double fraction) => node.Y + node.Height * fraction;
        return new ScenePart(new ChartPath(new[] {
            ChartPathCommand.MoveTo(X(.18), Y(0)),
            ChartPathCommand.CubicTo(X(.08), Y(0), X(.08), Y(.12), X(.08), Y(.25)),
            ChartPathCommand.CubicTo(X(.08), Y(.4), X(.08), Y(.45), X(.02), Y(.5)),
            ChartPathCommand.CubicTo(X(.08), Y(.55), X(.08), Y(.6), X(.08), Y(.75)),
            ChartPathCommand.CubicTo(X(.08), Y(.88), X(.08), Y(1), X(.18), Y(1))
        }), false, false, true, false);
    }
    private static IReadOnlyList<ScenePart> CrossedCircleParts(TopologyNode node) {
        var radius = Math.Min(node.Width, node.Height) / 2;
        var insetX = radius / Math.Sqrt(2) / node.Width; var insetY = radius / Math.Sqrt(2) / node.Height;
        return new[] { CatalogCircle(node, false),
            CatalogOpen(node, .5 - insetX, .5 - insetY, .5 + insetX, .5 + insetY),
            CatalogOpen(node, .5 - insetX, .5 + insetY, .5 + insetX, .5 - insetY) };
    }
    private static ScenePart ScallopedBurstPart(TopologyNode node) {
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(node.X, node.Y) };
        var depth = Math.Min(node.Width, node.Height) * .07;
        for (var side = 0; side < 4; side++) for (var index = 0; index < 4; index++) {
            var a = index / 4d; var b = (index + 1) / 4d;
            var x1 = side switch { 0 => node.X + node.Width * a, 1 => node.X + node.Width, 2 => node.X + node.Width * (1 - a), _ => node.X };
            var y1 = side switch { 0 => node.Y, 1 => node.Y + node.Height * a, 2 => node.Y + node.Height, _ => node.Y + node.Height * (1 - a) };
            var x2 = side switch { 0 => node.X + node.Width * b, 1 => node.X + node.Width, 2 => node.X + node.Width * (1 - b), _ => node.X };
            var y2 = side switch { 0 => node.Y, 1 => node.Y + node.Height * b, 2 => node.Y + node.Height, _ => node.Y + node.Height * (1 - b) };
            var nx = side == 1 ? -1 : side == 3 ? 1 : 0;
            var ny = side == 0 ? 1 : side == 2 ? -1 : 0;
            AppendCircularScallop(commands, x1, y1, x2, y2, nx, ny, depth);
        }
        return new ScenePart(new ChartPath(commands), true, true);
    }
    /// <summary>Approximates one concave circular arc by its tangent-matched cubic, independent from any SVG asset.</summary>
    private static void AppendCircularScallop(List<ChartPathCommand> commands, double x1, double y1, double x2, double y2, double nx, double ny, double requestedDepth) {
        var dx = x2 - x1; var dy = y2 - y1; var chord = Math.Sqrt(dx * dx + dy * dy);
        var depth = Math.Min(requestedDepth, chord * .35);
        if (chord <= 1e-9 || depth <= 1e-9) { commands.Add(ChartPathCommand.LineTo(x2, y2)); return; }
        var radius = chord * chord / (8 * depth) + depth / 2;
        var angle = 2 * Math.Asin(Math.Min(1, chord / (2 * radius)));
        var tangent = radius - depth;
        var cx = (x1 + x2) / 2 - nx * tangent; var cy = (y1 + y2) / 2 - ny * tangent;
        ChartPathBuilder.AddCircularArc(commands, cx, cy, radius, Math.Atan2(y1 - cy, x1 - cx), -angle);
    }
}
