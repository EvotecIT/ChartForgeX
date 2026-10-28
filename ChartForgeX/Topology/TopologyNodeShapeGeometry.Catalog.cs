using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    // The append-only catalog occupies27-61; existing0-26 retain their original geometry and byte paths.
    private static bool IsCatalogSurface(TopologyNode node) => node.Shape is TopologyNodeShape shape && (int)shape >= 27 && (int)shape <= 61;

    private static string CatalogInkPath(TopologyNode node) => NumericPartsPath(CatalogParts(node).Where(part => part.Ink).ToArray());

    private static IReadOnlyList<ScenePart> CatalogParts(TopologyNode node) => node.Shape switch {
        TopologyNodeShape.DataStore => new[] { CatalogLogicalBox(node), CatalogOpen(node, 0, 0, 1, 0), CatalogOpen(node, 0, 1, 1, 1) },
        TopologyNodeShape.Folder => new[] { CatalogPolygon(node, 0, 0, .38, 0, .38, .18, 1, .18, 1, 1, 0, 1) },
        TopologyNodeShape.Bucket => BucketParts(node),
        TopologyNodeShape.Console => WindowParts(node, false),
        TopologyNodeShape.Browser => WindowParts(node, true),
        TopologyNodeShape.Person => PersonParts(node),
        TopologyNodeShape.TextBlock => new[] { CatalogLogicalBox(node) },
        TopologyNodeShape.NotchedRectangle => new[] { CatalogPolygon(node, .12, 0, 1, 0, 1, 1, 0, 1, 0, .18) },
        TopologyNodeShape.LinedRectangle => new[] { CatalogRectangle(node), CatalogOpen(node, .12, 0, .12, 1) },
        TopologyNodeShape.FilledCircle => new[] { CatalogCircle(node, true) },
        TopologyNodeShape.FramedCircle => new[] { CatalogCircle(node, false), CatalogEllipse(node, .5, .5,
            Math.Min(node.Width, node.Height) * .18 / node.Width, Math.Min(node.Width, node.Height) * .18 / node.Height, true, false) },
        TopologyNodeShape.Hourglass => new[] { CatalogPolygon(node, 0, 0, 1, 0, 0, 1, 1, 1) },
        TopologyNodeShape.BraceLeft => BraceParts(node, true, false),
        TopologyNodeShape.BraceRight => BraceParts(node, false, true),
        TopologyNodeShape.Braces => BraceParts(node, true, true),
        TopologyNodeShape.LightningBolt => new[] { CatalogPolygon(node, 1, 0, 0, .54, .62, .54, 0, 1, 1, .46, .38, .46) },
        TopologyNodeShape.Document => DocumentCatalogParts(node, false, false),
        TopologyNodeShape.HalfRoundedRectangle => HalfRoundedParts(node),
        TopologyNodeShape.LinedCylinder => LinedCylinderCatalogParts(node),
        TopologyNodeShape.CurvedTrapezoid => DisplayParts(node),
        TopologyNodeShape.DividedRectangle => new[] { CatalogRectangle(node), CatalogOpen(node, 0, .2, 1, .2) },
        TopologyNodeShape.Triangle => new[] { CatalogPolygon(node, .5, 0, 1, 1, 0, 1) },
        TopologyNodeShape.WindowPane => new[] { CatalogRectangle(node), CatalogOpen(node, 0, .2, 1, .2), CatalogOpen(node, .12, 0, .12, 1) },
        TopologyNodeShape.NotchedPentagon => new[] { CatalogPolygon(node, .1, 0, .9, 0, 1, .2, 1, 1, 0, 1, 0, .2) },
        TopologyNodeShape.TriangleDown => new[] { CatalogPolygon(node, 0, 0, 1, 0, .5, 1) },
        TopologyNodeShape.SlopedRectangle => new[] { CatalogPolygon(node, 0, .33, 1, 0, 1, 1, 0, 1) },
        TopologyNodeShape.StackedDocument => DocumentCatalogParts(node, true, false),
        TopologyNodeShape.StackedRectangle => StackRectangleParts(node),
        TopologyNodeShape.BowTieRectangle => StoredDataParts(node),
        TopologyNodeShape.CrossedCircle => CrossedCircleParts(node),
        TopologyNodeShape.TaggedDocument => DocumentCatalogParts(node, false, true),
        TopologyNodeShape.TaggedRectangle => new[] { CatalogRectangle(node), CatalogTag(node) },
        TopologyNodeShape.WavyRectangle => PaperTapeParts(node),
        TopologyNodeShape.CollapsedContainer => CollapsedContainerParts(node),
        TopologyNodeShape.ScallopedBurst => new[] { ScallopedBurstPart(node) },
        _ => throw new ArgumentException("A native catalog surface is required.", nameof(node))
    };

    private static ScenePart CatalogRectangle(TopologyNode node) => CatalogPolygon(node, 0, 0, 1, 0, 1, 1, 0, 1);
    private static ScenePart CatalogLogicalBox(TopologyNode node) {
        var part = CatalogRectangle(node);
        return new ScenePart(part.Path, true, false, false, true);
    }
    private static ScenePart CatalogPolygon(TopologyNode node, params double[] points) => CatalogLine(node, true, true, true, points);
    private static ScenePart CatalogOpen(TopologyNode node, params double[] points) => CatalogLine(node, false, false, false, points);
    private static ScenePart CatalogLine(TopologyNode node, bool closed, bool fill, bool attachment, double[] points) {
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(node.X + points[0] * node.Width, node.Y + points[1] * node.Height) };
        for (var index = 2; index < points.Length; index += 2)
            commands.Add(ChartPathCommand.LineTo(node.X + points[index] * node.Width, node.Y + points[index + 1] * node.Height));
        return new ScenePart(new ChartPath(commands), closed, fill, true, attachment);
    }
    private static ScenePart CatalogCircle(TopologyNode node, bool accent) {
        var radius = Math.Min(node.Width, node.Height) / 2;
        return CatalogEllipse(node, .5, .5, radius / node.Width, radius / node.Height, accent, true);
    }
    private static ScenePart CatalogEllipse(TopologyNode node, double cx, double cy, double rx, double ry, bool accent, bool attachment) {
        return new ScenePart(ChartPathBuilder.Ellipse(node.X + cx * node.Width, node.Y + cy * node.Height, rx * node.Width, ry * node.Height), true, true, true, attachment, accent);
    }
    private static ScenePart CatalogRounded(TopologyNode node, double radius) =>
        new(ChartPathBuilder.RoundedRectangle(new ChartRect(node.X, node.Y, node.Width, node.Height), radius), true, true);
    private static TopologyNode CatalogCell(TopologyNode node, double left, double top, double right, double bottom) => new() {
        X = node.X + left * node.Width, Y = node.Y + top * node.Height,
        Width = Math.Max(1e-6, (right - left) * node.Width), Height = Math.Max(1e-6, (bottom - top) * node.Height)
    };
}
