namespace ChartForgeX.Core;

using ChartForgeX.Primitives;

public sealed partial class Chart {
    /// <summary>
    /// Applies reusable dashboard cartesian chrome: dashed vertical guide lines and lightweight report-panel surfaces.
    /// </summary>
    /// <param name="includeChrome">True to also remove the default card, plot background, and axis rules for a lightweight dashboard panel.</param>
    /// <returns>The current chart.</returns>
    public Chart DashboardCartesianStyle(bool includeChrome = true) {
        Options.GridLineStyle = ChartGridLineStyle.DashboardVerticalGuides();
        Options.Lines.VisualStyle = ChartLineVisualStyle.Premium();

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
    public Chart DashboardPanelStyle(bool showPlotBackground = false) {
        Options.GridLineStyle = ChartGridLineStyle.DashboardVerticalGuides();
        Options.Lines.VisualStyle = ChartLineVisualStyle.Premium();
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
    public Chart DashboardBarStyle(bool includeChrome = true) {
        Options.Bars.VisualStyle = ChartBarVisualStyle.DashboardCapsule();
        return DashboardCartesianStyle(includeChrome);
    }

    /// <summary>
    /// Applies the reusable dashboard panel and segmented bar treatment together.
    /// </summary>
    /// <param name="showPlotBackground">True to also render the inner plot background surface.</param>
    /// <returns>The current chart.</returns>
    public Chart DashboardBarPanelStyle(bool showPlotBackground = false) {
        Options.Bars.VisualStyle = ChartBarVisualStyle.DashboardCapsule();
        return DashboardPanelStyle(showPlotBackground);
    }

    /// <summary>
    /// Applies a compact dashboard preset for stacked horizontal row charts with optional trailing totals.
    /// </summary>
    /// <param name="showTotals">True to render totals at the end of each stacked row.</param>
    /// <param name="showLegend">True to keep the series legend visible.</param>
    /// <param name="showPlotBackground">True to render the inner plot background surface.</param>
    /// <returns>The current chart.</returns>
    public Chart DashboardStackedRowStyle(bool showTotals = false, bool showLegend = true, bool showPlotBackground = false) {
        Options.Bars.VisualStyle = ChartBarVisualStyle.DashboardCapsule();
        Options.Bars.Mode = ChartBarMode.Stacked;
        Options.Bars.ShowTotals = showTotals;
        Options.Legend.Visible = showLegend;
        Options.XAxis.Visible = false;
        Options.ShowAxisLines = false;
        Options.ShowCard = true;
        Options.ShowPlotBackground = showPlotBackground;
        Options.GridLineStyle = ChartGridLineStyle.DashboardVerticalGuides().ShowHorizontalLines(false);
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
    public Chart DashboardTrendPanelStyle(bool showLegend = true, bool showYAxis = false, bool showPlotBackground = false) {
        Options.GridLineStyle = ChartGridLineStyle.DashboardVerticalGuides();
        Options.Lines.VisualStyle = ChartLineVisualStyle.Premium();
        Options.ShowCard = true;
        Options.ShowPlotBackground = showPlotBackground;
        Options.ShowAxisLines = false;
        Options.YAxis.Visible = showYAxis;
        Options.Legend.Visible = showLegend;
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
    public Chart DashboardTrendFocus(double x, double y, string label, ChartColor? color = null, ChartDataLabelPlacement placement = ChartDataLabelPlacement.Above) {
        if (label == null) throw new System.ArgumentNullException(nameof(label));
        ChartGuards.Finite(x, nameof(x));
        ChartGuards.Finite(y, nameof(y));
        var focusColor = color ?? Options.ResolvedTheme.Palette[0];
        HighlightXAxisLabel(x, focusColor);
        // The callout owns the visible caption; the crosshair retains its full label
        // for accessibility and semantic exports without painting a duplicate.
        AddAnnotation(new ChartAnnotation(ChartAnnotationKind.VerticalLine, x, null, label, focusColor, 1, showLabel: false));
        AddPointCallout(label, x, y, focusColor, placement);
        return this;
    }
}
