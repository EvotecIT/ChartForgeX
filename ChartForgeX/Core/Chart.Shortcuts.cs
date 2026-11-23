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
}
