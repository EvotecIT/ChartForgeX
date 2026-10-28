using System;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    // One painted contour, inner caption and attachment boundary for native block arrows.
    internal static string DirectionalArrow(ChartRect bounds, bool left, bool right, bool up, bool down) {
        var l = left ? .25 : 0; var r = right ? .75 : 1;
        var t = up ? .25 : 0; var b = down ? .75 : 1;
        if (left || right) { t = Math.Max(t, .25); b = Math.Min(b, .75); }
        if (up || down) { l = Math.Max(l, .25); r = Math.Min(r, .75); }
        var path = new SvgPathDataBuilder().MoveTo(X(l), Y(t));
        if (up) path.LineTo(X(0), Y(t)).LineTo(X(.5), Y(0)).LineTo(X(1), Y(t));
        path.LineTo(X(r), Y(t));
        if (right) path.LineTo(X(r), Y(0)).LineTo(X(1), Y(.5)).LineTo(X(r), Y(1));
        path.LineTo(X(r), Y(b));
        if (down) path.LineTo(X(1), Y(b)).LineTo(X(.5), Y(1)).LineTo(X(0), Y(b));
        path.LineTo(X(l), Y(b));
        if (left) path.LineTo(X(l), Y(1)).LineTo(X(0), Y(.5)).LineTo(X(l), Y(0));
        return path.Close().Build();
        double X(double value) => bounds.Left + value * bounds.Width;
        double Y(double value) => bounds.Top + value * bounds.Height;
    }

    internal static ChartRect DirectionalArrowCaption(ChartRect bounds, bool horizontal, bool vertical) {
        var xInset = vertical ? .25 : 0; var yInset = horizontal ? .25 : 0;
        return new ChartRect(bounds.Left + bounds.Width * xInset + 8, bounds.Top + bounds.Height * yInset + 6,
            Math.Max(0, bounds.Width * (1 - 2 * xInset) - 16), Math.Max(0, bounds.Height * (1 - 2 * yInset) - 12));
    }
}
