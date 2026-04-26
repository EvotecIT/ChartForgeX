using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartLayout {
    public static ChartRect PlotArea(ChartOptions options) {
        var header = options.ShowHeader ? 34 : 0;
        var x = options.Padding.Left;
        var y = options.Padding.Top + header;
        var width = Math.Max(1, options.Size.Width - options.Padding.Left - options.Padding.Right);
        var height = Math.Max(1, options.Size.Height - options.Padding.Top - options.Padding.Bottom - header);
        return new ChartRect(x, y, width, height);
    }
}
