using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    private static ChartRect CatalogCaptionBounds(TopologyNode node, double measuredHeight, double spacingScale) {
        if (node.Shape is TopologyNodeShape.FilledCircle or TopologyNodeShape.FramedCircle or TopologyNodeShape.CrossedCircle) {
            var radius = Math.Min(node.Width, node.Height) / 2;
            var circleHeight = Math.Min(Math.Max(0, measuredHeight), Math.Max(0, radius * 2 - 12 * spacingScale));
            var vertical = radius > 0 ? Math.Min(1, (circleHeight / 2 + 4 * spacingScale) / radius) : 1;
            var circleWidth = Math.Max(0, 2 * radius * Math.Sqrt(1 - vertical * vertical) - 8 * spacingScale);
            return new ChartRect(node.X + (node.Width - circleWidth) / 2, node.Y + (node.Height - circleHeight) / 2, circleWidth, circleHeight);
        }
        var left = .08; var top = .08; var right = .92; var bottom = .92;
        switch (node.Shape) {
            case TopologyNodeShape.Folder: top = .25; break;
            case TopologyNodeShape.Bucket: left = .2; right = .8; top = .25; bottom = .82; break;
            case TopologyNodeShape.Console: case TopologyNodeShape.Browser: top = .34; break;
            case TopologyNodeShape.Person: left = .14; right = .86; top = PersonBodyTop(node) + .12 * (1 - PersonBodyTop(node)); bottom = .9; break;
            case TopologyNodeShape.NotchedRectangle: left = .18; break;
            case TopologyNodeShape.LinedRectangle: left = .2; break;
            case TopologyNodeShape.Hourglass: left = .3; right = .7; top = .1; bottom = .28; break;
            case TopologyNodeShape.BraceLeft: left = .22; break;
            case TopologyNodeShape.BraceRight: right = .78; break;
            case TopologyNodeShape.Braces: left = .22; right = .78; break;
            case TopologyNodeShape.LightningBolt: left = .4; right = .6; top = .4; bottom = .6; break;
            case TopologyNodeShape.Document: case TopologyNodeShape.TaggedDocument: bottom = .66; break;
            case TopologyNodeShape.StackedDocument: left = .13; right = .8; top = .2; bottom = .67; break;
            case TopologyNodeShape.HalfRoundedRectangle: right = .72; break;
            case TopologyNodeShape.LinedCylinder: top = .4; bottom = .72; break;
            case TopologyNodeShape.CurvedTrapezoid: left = .3; right = .72; break;
            case TopologyNodeShape.DividedRectangle: top = .3; break;
            case TopologyNodeShape.Triangle: left = .32; right = .68; top = .6; bottom = .84; break;
            case TopologyNodeShape.WindowPane: left = .22; top = .3; break;
            case TopologyNodeShape.NotchedPentagon: top = .28; break;
            case TopologyNodeShape.TriangleDown: left = .32; right = .68; top = .16; bottom = .4; break;
            case TopologyNodeShape.SlopedRectangle: top = .45; break;
            case TopologyNodeShape.StackedRectangle: left = .12; right = .8; top = .2; break;
            case TopologyNodeShape.BowTieRectangle: left = .23; right = .77; break;
            case TopologyNodeShape.TaggedRectangle: right = .76; break;
            case TopologyNodeShape.WavyRectangle: top = .24; bottom = .76; break;
            case TopologyNodeShape.CollapsedContainer: bottom = .52; break;
            case TopologyNodeShape.ScallopedBurst: left = .2; right = .8; top = .2; bottom = .8; break;
        }
        var inset = Math.Min(6 * spacingScale, Math.Min(node.Width, node.Height) * .03);
        var x = node.X + node.Width * left + inset;
        var y = node.Y + node.Height * top + inset;
        var width = Math.Max(0, node.Width * (right - left) - 2 * inset);
        var available = Math.Max(0, node.Height * (bottom - top) - 2 * inset);
        var height = Math.Min(available, Math.Max(0, measuredHeight));
        return new ChartRect(x, y + (available - height) / 2, width, height);
    }
}
