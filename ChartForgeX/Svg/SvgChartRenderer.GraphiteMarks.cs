using System;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    // Only the value end is rounded. The baseline stays square for positive and negative marks.
    private static string GraphiteBarPath(double x, double y, double w, double h, double radius, bool positive, bool horizontal) {
        var r = Math.Min(radius, Math.Min(w, h) / 2);
        var right = x + w;
        var bottom = y + h;
        if (horizontal) return positive
            ? $"M {F(x)} {F(y)} H {F(right-r)} Q {F(right)} {F(y)} {F(right)} {F(y+r)} V {F(bottom-r)} Q {F(right)} {F(bottom)} {F(right-r)} {F(bottom)} H {F(x)} Z"
            : $"M {F(right)} {F(y)} H {F(x+r)} Q {F(x)} {F(y)} {F(x)} {F(y+r)} V {F(bottom-r)} Q {F(x)} {F(bottom)} {F(x+r)} {F(bottom)} H {F(right)} Z";
        return positive
            ? $"M {F(x)} {F(bottom)} V {F(y+r)} Q {F(x)} {F(y)} {F(x+r)} {F(y)} H {F(right-r)} Q {F(right)} {F(y)} {F(right)} {F(y+r)} V {F(bottom)} Z"
            : $"M {F(x)} {F(y)} V {F(bottom-r)} Q {F(x)} {F(bottom)} {F(x+r)} {F(bottom)} H {F(right-r)} Q {F(right)} {F(bottom)} {F(right)} {F(bottom-r)} V {F(y)} Z";
    }
}
