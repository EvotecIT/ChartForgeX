using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    private static IReadOnlyList<ScenePart> WindowParts(TopologyNode node, bool browser) {
        var parts = new List<ScenePart> { CatalogRounded(node, 10) };
        if (browser) {
            parts.Add(CatalogOpen(node, 0, .26, 1, .26));
            for (var index = 0; index < 3; index++)
                parts.Add(CatalogEllipse(node, .08 + index * .065, .13, Math.Min(2.5, node.Width * .02) / node.Width,
                    Math.Min(2.5, node.Height * .05) / node.Height, true, false));
            var address = CatalogRounded(CatalogCell(node, .36, .07, .92, .2), 3);
            parts.Add(new ScenePart(address.Path, true, false, true, false));
        } else {
            // A native path prompt avoids introducing another font or authored HTML surface.
            parts.Add(CatalogOpen(node, .08, .08, .14, .15, .08, .22));
            parts.Add(CatalogOpen(node, .16, .22, .24, .22));
        }
        return parts;
    }
    private static IReadOnlyList<ScenePart> PersonParts(TopologyNode node) {
        var torso = CatalogRounded(CatalogCell(node, 0, PersonBodyTop(node), 1, 1), Math.Min(node.Width * .16, node.Height * .22));
        var radius = PersonHeadRadius(node);
        return new[] { torso, CatalogEllipse(node, .5, radius / node.Height, radius / node.Width, radius / node.Height, false, true) };
    }
    private static double PersonHeadRadius(TopologyNode node) => Math.Min(node.Width * .23, node.Height * .18);
    private static double PersonBodyTop(TopologyNode node) => PersonHeadRadius(node) * 1.73 / node.Height;
    private static IReadOnlyList<ScenePart> BucketParts(TopologyNode node) {
        var x = node.X; var y = node.Y; var w = node.Width; var h = node.Height;
        var rim = Math.Min(12, h * .12); var bottom = y + h - rim;
        var body = new ScenePart(new ChartPath(new[] {
            ChartPathCommand.MoveTo(x, y + rim),
            ChartPathCommand.LineTo(x + w * .14, bottom),
            ChartPathCommand.CubicTo(x + w * .28, y + h + rim / 3, x + w * .72, y + h + rim / 3, x + w * .86, bottom),
            ChartPathCommand.LineTo(x + w, y + rim),
            ChartPathCommand.CubicTo(x + w, y - rim / 3, x, y - rim / 3, x, y + rim)
        }), true, true);
        var lip = new ScenePart(new ChartPath(new[] {
            ChartPathCommand.MoveTo(x, y + rim),
            ChartPathCommand.CubicTo(x, y + rim * 2.3, x + w, y + rim * 2.3, x + w, y + rim)
        }), false, false, true, false);
        return new[] { body, lip };
    }
    private static IReadOnlyList<ScenePart> HalfRoundedParts(TopologyNode node) {
        var x = node.X; var y = node.Y; var w = node.Width; var h = node.Height; var r = Math.Min(w * .35, h / 2);
        const double k = .5522847498307936;
        return new[] { new ScenePart(new ChartPath(new[] {
            ChartPathCommand.MoveTo(x, y), ChartPathCommand.LineTo(x + w - r, y),
            ChartPathCommand.CubicTo(x + w - r + k * r, y, x + w, y + h * .5 - k * h * .5, x + w, y + h * .5),
            ChartPathCommand.CubicTo(x + w, y + h * .5 + k * h * .5, x + w - r + k * r, y + h, x + w - r, y + h),
            ChartPathCommand.LineTo(x, y + h)
        }), true, true) };
    }
    private static IReadOnlyList<ScenePart> DisplayParts(TopologyNode node) {
        var parts = HalfRoundedParts(node); var path = parts[0].Path.Commands;
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(node.X + node.Width * .2, node.Y) };
        for (var index = 1; index < path.Count - 1; index++) commands.Add(path[index]);
        commands.Add(ChartPathCommand.LineTo(node.X + node.Width * .2, node.Y + node.Height));
        commands.Add(ChartPathCommand.LineTo(node.X, node.Y + node.Height / 2));
        return new[] { new ScenePart(new ChartPath(commands), true, true) };
    }
    private static IReadOnlyList<ScenePart> StoredDataParts(TopologyNode node) {
        var x = node.X; var y = node.Y; var w = node.Width; var h = node.Height;
        return new[] { new ScenePart(new ChartPath(new[] {
            ChartPathCommand.MoveTo(x, y), ChartPathCommand.LineTo(x + w * .86, y),
            ChartPathCommand.CubicTo(x + w * 1.04, y + h * .28, x + w * 1.04, y + h * .72, x + w * .86, y + h),
            ChartPathCommand.LineTo(x, y + h),
            ChartPathCommand.CubicTo(x + w * .18, y + h * .72, x + w * .18, y + h * .28, x, y)
        }), true, true) };
    }
    private static IReadOnlyList<ScenePart> CollapsedContainerParts(TopologyNode node) {
        var parts = new List<ScenePart> { CatalogRounded(node, 10) };
        var left = node.X + Math.Min(8, node.Width * .08); var right = node.X + node.Width - Math.Min(8, node.Width * .08);
        var y = node.Y + node.Height * .64;
        var segments = (int)Math.Min(256, Math.Max(1, Math.Ceiling((right - left) / 6)));
        var step = (right - left) / segments;
        for (var index = 0; index < segments; index++) {
            var x = left + index * step;
            parts.Add(new ScenePart(new ChartPath(new[] { ChartPathCommand.MoveTo(x, y), ChartPathCommand.LineTo(Math.Min(right, x + step / 2), y) }), false, false, true, false));
        }
        var radius = Math.Min(2.5, Math.Min(node.Width, node.Height) * .04);
        for (var index = -1; index <= 1; index++)
            parts.Add(CatalogEllipse(node, .5 + index * Math.Min(10, node.Width * .1) / node.Width, .82,
                radius / node.Width, radius / node.Height, true, false));
        return parts;
    }
}
