namespace ChartForgeX.Core;

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
}
