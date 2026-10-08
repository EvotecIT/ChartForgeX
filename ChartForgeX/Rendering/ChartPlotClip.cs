using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Shared plot membership for mark clipping; markers are allowed only when their centers are in the plot.</summary>
internal static class ChartPlotClip {
    internal static bool Contains(ChartRect plot, double x, double y) =>
        x >= plot.Left - 1e-7 && x <= plot.Right + 1e-7 && y >= plot.Top - 1e-7 && y <= plot.Bottom + 1e-7;

    internal static ChartRect Expand(ChartRect plot, double radius) =>
        new(plot.X - radius, plot.Y - radius, plot.Width + radius * 2, plot.Height + radius * 2);
}
