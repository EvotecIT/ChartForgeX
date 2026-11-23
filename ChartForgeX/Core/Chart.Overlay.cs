using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Configures the chart for transparent overlay rendering on an existing image or surface.
    /// </summary>
    /// <param name="showHeader">True to keep the chart title and subtitle visible; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart Overlay(bool showHeader = false) {
        Options.TransparentBackground = true;
        Options.ShowHeader = showHeader;
        Options.Legend.Visible = false;
        Options.ShowGrid = false;
        Options.ShowAxes = false;
        Options.XAxis.Visible = false;
        Options.YAxis.Visible = false;
        Options.ShowAxisLines = false;
        Options.ShowCard = false;
        Options.ShowPlotBackground = false;
        Options.Padding = showHeader ? new ChartPadding(8, 30, 8, 8) : new ChartPadding(8, 8, 8, 8);
        return this;
    }
}
