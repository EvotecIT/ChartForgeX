using System;
using System.Globalization;

namespace ChartForgeX.Topology;

/// <summary>Builds diagram surface paths once for SVG and raster path consumers.</summary>
internal static partial class TopologyNodeShapeGeometry {
    internal static string Path(TopologyNode node) {
        var x = node.X; var y = node.Y; var w = node.Width; var h = node.Height;
        var inset = Math.Min(w * 0.18, h * 0.3);
        string P(double px, double py) => px.ToString("0.###", CultureInfo.InvariantCulture) + " " + py.ToString("0.###", CultureInfo.InvariantCulture);
        string Polygon(params double[] coordinates) {
            var path = "M " + P(coordinates[0], coordinates[1]);
            for (var i = 2; i < coordinates.Length; i += 2) path += " L " + P(coordinates[i], coordinates[i + 1]);
            return path + " Z";
        }
        var rect = Polygon(x, y, x + w, y, x + w, y + h, x, y + h);
        switch (node.Shape) {
            case TopologyNodeShape.Actor:
                var cx = x + w / 2;
                return "M " + P(cx, y + 6) + " A 9 9 0 1 1 " + P(cx - .01, y + 6) + " Z M " + P(cx, y + 24) + " L " + P(cx, y + 50) + " M " + P(cx - 17, y + 34) + " L " + P(cx + 17, y + 34) + " M " + P(cx - 15, y + 68) + " L " + P(cx, y + 50) + " L " + P(cx + 15, y + 68);
            case TopologyNodeShape.Ellipse:
            case TopologyNodeShape.Circle:
            case TopologyNodeShape.DoubleCircle:
                var rx = node.Shape == TopologyNodeShape.Ellipse ? w / 2 : Math.Min(w, h) / 2;
                var ry = node.Shape == TopologyNodeShape.Ellipse ? h / 2 : rx;
                string Ellipse(double pad) => "M " + P(x + w / 2, y + h / 2 - ry + pad) + " A " + P(rx - pad, ry - pad) + " 0 1 1 " + P(x + w / 2 - 0.01, y + h / 2 - ry + pad) + " Z";
                return Ellipse(0) + (node.Shape == TopologyNodeShape.DoubleCircle ? " " + Ellipse(5) : "");
            case TopologyNodeShape.Diamond: return Polygon(x + w / 2, y, x + w, y + h / 2, x + w / 2, y + h, x, y + h / 2);
            case TopologyNodeShape.Hexagon: return Polygon(x + inset, y, x + w - inset, y, x + w, y + h / 2, x + w - inset, y + h, x + inset, y + h, x, y + h / 2);
            case TopologyNodeShape.Parallelogram: return Polygon(x + inset, y, x + w, y, x + w - inset, y + h, x, y + h);
            case TopologyNodeShape.ParallelogramAlt: return Polygon(x, y, x + w - inset, y, x + w, y + h, x + inset, y + h);
            case TopologyNodeShape.Trapezoid: return Polygon(x + inset, y, x + w - inset, y, x + w, y + h, x, y + h);
            case TopologyNodeShape.TrapezoidAlt: return Polygon(x, y, x + w, y, x + w - inset, y + h, x + inset, y + h);
            case TopologyNodeShape.Asymmetric: return Polygon(x, y, x + w - inset, y, x + w, y + h / 2, x + w - inset, y + h, x, y + h, x + inset, y + h / 2);
            case TopologyNodeShape.Subroutine: return rect + " M " + P(x + 9, y) + " L " + P(x + 9, y + h) + " M " + P(x + w - 9, y) + " L " + P(x + w - 9, y + h);
            case TopologyNodeShape.Cylinder:
                var cap = Math.Min(14, h * 0.2);
                return "M " + P(x, y + cap) + " C " + P(x, y - cap / 3) + " " + P(x + w, y - cap / 3) + " " + P(x + w, y + cap) + " L " + P(x + w, y + h - cap) + " C " + P(x + w, y + h + cap / 3) + " " + P(x, y + h + cap / 3) + " " + P(x, y + h - cap) + " Z M " + P(x, y + cap) + " C " + P(x, y + cap * 2.3) + " " + P(x + w, y + cap * 2.3) + " " + P(x + w, y + cap);
            case TopologyNodeShape.Cloud:
                return "M " + P(x + w * .16, y + h * .8) + " C " + P(x - w * .08, y + h * .8) + " " + P(x - w * .02, y + h * .25) + " " + P(x + w * .2, y + h * .28) + " C " + P(x + w * .17, y - h * .06) + " " + P(x + w * .56, y - h * .06) + " " + P(x + w * .61, y + h * .18) + " C " + P(x + w * .86, y - h * .02) + " " + P(x + w * 1.04, y + h * .25) + " " + P(x + w * .9, y + h * .45) + " C " + P(x + w * 1.1, y + h * .67) + " " + P(x + w * .92, y + h * 1.04) + " " + P(x + w * .7, y + h * .85) + " C " + P(x + w * .57, y + h * 1.08) + " " + P(x + w * .29, y + h * 1.02) + " " + P(x + w * .16, y + h * .8) + " Z";
            default:
                var r = node.Shape == TopologyNodeShape.Stadium ? h / 2 : node.Shape == TopologyNodeShape.Rounded ? 10 : 0;
                if (r == 0) return rect;
                return "M " + P(x + r, y) + " L " + P(x + w - r, y) + " Q " + P(x + w, y) + " " + P(x + w, y + r) + " L " + P(x + w, y + h - r) + " Q " + P(x + w, y + h) + " " + P(x + w - r, y + h) + " L " + P(x + r, y + h) + " Q " + P(x, y + h) + " " + P(x, y + h - r) + " L " + P(x, y + r) + " Q " + P(x, y) + " " + P(x + r, y) + " Z";
        }
    }
}
