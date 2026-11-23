using System;

namespace ChartForgeX.Core;

/// <summary>Hand-written shortcuts on <see cref="Chart"/> for switches that read better than a configure callback.</summary>
public static partial class ChartFluent {
    /// <summary>Shows or hides the legend. A default legend with one entry stays hidden; an explicit true also shows a single entry.</summary>
    /// <param name="chart">The chart to configure.</param>
    /// <param name="visible">Whether the legend is shown; true when omitted.</param>
    /// <returns>The same chart.</returns>
    public static Chart Legend(this Chart chart, bool visible = true) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Options.Legend.Visible = visible;
        return chart;
    }

    /// <summary>Shows or hides data labels for every series that doesn't set its own.</summary>
    /// <param name="chart">The chart to configure.</param>
    /// <param name="visible">Whether data labels are shown; true when omitted.</param>
    /// <returns>The same chart.</returns>
    public static Chart DataLabels(this Chart chart, bool visible = true) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Options.DataLabels.Visible = visible;
        return chart;
    }

    /// <summary>Sets the grid and guide line style. An assigned style overrides the theme's grid stroke width.</summary>
    /// <param name="chart">The chart to configure.</param>
    /// <param name="style">The style; a copy is stored.</param>
    /// <returns>The same chart.</returns>
    public static Chart GridLineStyle(this Chart chart, ChartGridLineStyle style) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Options.GridLineStyle = style;
        return chart;
    }

    /// <summary>Refines the grid and guide line style on a copy, installed when the callback returns. Unlike assigning a style, it keeps the theme's grid stroke width unless the callback sets one.</summary>
    /// <param name="chart">The chart to configure.</param>
    /// <param name="configure">Edits the copy.</param>
    /// <returns>The same chart.</returns>
    public static Chart GridLineStyle(this Chart chart, Action<ChartGridLineStyle> configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        chart.Options.ConfigureGridLineStyle(configure);
        return chart;
    }
}

/// <summary>Hand-written methods of <see cref="ChartBarOptions"/>: an assigned bar style overrides theme geometry, while a callback refines it.</summary>
public static partial class ChartBarOptionsFluent {
    /// <summary>Sets the bar style. An assigned style overrides the theme's bar corner radius.</summary>
    /// <param name="bars">The bar options to configure.</param>
    /// <param name="style">The style; a copy is stored.</param>
    /// <returns>The same options.</returns>
    public static ChartBarOptions VisualStyle(this ChartBarOptions bars, ChartBarVisualStyle style) {
        if (bars == null) throw new ArgumentNullException(nameof(bars));
        bars.VisualStyle = style;
        return bars;
    }

    /// <summary>Refines the bar style on a copy, installed when the callback returns. Unlike assigning a style, it keeps the theme's corner radius unless the callback sets one.</summary>
    /// <param name="bars">The bar options to configure.</param>
    /// <param name="configure">Edits the copy.</param>
    /// <returns>The same options.</returns>
    public static ChartBarOptions VisualStyle(this ChartBarOptions bars, Action<ChartBarVisualStyle> configure) {
        if (bars == null) throw new ArgumentNullException(nameof(bars));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        bars.ConfigureVisualStyle(configure);
        return bars;
    }
}
