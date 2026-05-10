namespace ChartForgeX.Core;

using ChartForgeX.Primitives;

public sealed partial class Chart {
    /// <summary>
    /// Applies reusable dashboard cartesian chrome: dashed vertical guide lines and lightweight report-panel surfaces.
    /// </summary>
    /// <param name="includeChrome">True to also remove the default card, plot background, and axis rules for a lightweight dashboard panel.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDashboardCartesianStyle(bool includeChrome = true) {
        Options.GridLineStyle = ChartGridLineStyle.DashboardVerticalGuides();
        Options.LineVisualStyle = ChartLineVisualStyle.Premium();

        if (includeChrome) {
            Options.ShowCard = false;
            Options.ShowPlotBackground = false;
            Options.ShowAxisLines = false;
        }

        return this;
    }

    /// <summary>
    /// Applies reusable dashboard panel chrome: a card surface, dashed vertical guide lines, premium line strokes, and hidden axis rules.
    /// </summary>
    /// <param name="showPlotBackground">True to also render the inner plot background surface.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDashboardPanelStyle(bool showPlotBackground = false) {
        Options.GridLineStyle = ChartGridLineStyle.DashboardVerticalGuides();
        Options.LineVisualStyle = ChartLineVisualStyle.Premium();
        Options.ShowCard = true;
        Options.ShowPlotBackground = showPlotBackground;
        Options.ShowAxisLines = false;
        return this;
    }

    /// <summary>
    /// Applies the reusable dashboard bar treatment: translucent bar segments, rounded value caps, soft cap shadows, and dashed vertical guide lines.
    /// </summary>
    /// <param name="includeChrome">True to also remove the default card, plot background, and axis rules for a lightweight dashboard panel.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDashboardBarStyle(bool includeChrome = true) {
        Options.BarVisualStyle = ChartBarVisualStyle.DashboardCapsule();
        return WithDashboardCartesianStyle(includeChrome);
    }

    /// <summary>
    /// Applies the reusable dashboard panel and segmented bar treatment together.
    /// </summary>
    /// <param name="showPlotBackground">True to also render the inner plot background surface.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDashboardBarPanelStyle(bool showPlotBackground = false) {
        Options.BarVisualStyle = ChartBarVisualStyle.DashboardCapsule();
        return WithDashboardPanelStyle(showPlotBackground);
    }

    /// <summary>
    /// Applies a compact dashboard preset for stacked horizontal row charts with optional trailing totals.
    /// </summary>
    /// <param name="showTotals">True to render totals at the end of each stacked row.</param>
    /// <param name="showLegend">True to keep the series legend visible.</param>
    /// <param name="showPlotBackground">True to render the inner plot background surface.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDashboardStackedRowStyle(bool showTotals = false, bool showLegend = true, bool showPlotBackground = false) {
        Options.BarVisualStyle = ChartBarVisualStyle.DashboardCapsule();
        Options.BarMode = ChartBarMode.Stacked;
        Options.ShowStackTotals = showTotals;
        Options.ShowLegend = showLegend;
        Options.ShowXAxis = false;
        Options.ShowAxisLines = false;
        Options.ShowCard = true;
        Options.ShowPlotBackground = showPlotBackground;
        Options.GridLineStyle = ChartGridLineStyle.DashboardVerticalGuides().WithHorizontalLines(false);
        Options.Padding = new ChartPadding(154, 42, showTotals ? 58 : 28, showLegend ? 58 : 34);
        return this;
    }

    /// <summary>
    /// Applies a compact dashboard preset for multi-line trend cards with premium strokes and lightweight axes.
    /// </summary>
    /// <param name="showLegend">True to keep the series legend visible.</param>
    /// <param name="showYAxis">True to keep y-axis labels visible.</param>
    /// <param name="showPlotBackground">True to render the inner plot background surface.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDashboardTrendPanelStyle(bool showLegend = true, bool showYAxis = false, bool showPlotBackground = false) {
        Options.GridLineStyle = ChartGridLineStyle.DashboardVerticalGuides();
        Options.LineVisualStyle = ChartLineVisualStyle.Premium();
        Options.ShowCard = true;
        Options.ShowPlotBackground = showPlotBackground;
        Options.ShowAxisLines = false;
        Options.ShowYAxis = showYAxis;
        Options.ShowLegend = showLegend;
        Options.Padding = new ChartPadding(56, 58, 34, showLegend ? 58 : 36);
        return this;
    }

    /// <summary>
    /// Adds a reusable dashboard focus marker to a trend chart.
    /// </summary>
    /// <param name="x">The focused x-axis value.</param>
    /// <param name="y">The focused y-axis value.</param>
    /// <param name="label">The focus label.</param>
    /// <param name="color">The focus color. When omitted, the first theme palette color is used.</param>
    /// <param name="placement">The callout placement around the focused point.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDashboardTrendFocus(double x, double y, string label, ChartColor? color = null, ChartDataLabelPlacement placement = ChartDataLabelPlacement.Above) {
        if (label == null) throw new System.ArgumentNullException(nameof(label));
        ChartGuards.Finite(x, nameof(x));
        ChartGuards.Finite(y, nameof(y));
        var focusColor = color ?? Options.Theme.Palette[0];
        WithHighlightedXAxisLabel(x, focusColor);
        AddVerticalLine(x, label, focusColor);
        AddPointCallout(label, x, y, focusColor, placement);
        return this;
    }
}
