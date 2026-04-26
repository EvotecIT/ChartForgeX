using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>
/// Defines renderer-independent options for a chart.
/// </summary>
public sealed class ChartOptions {
    /// <summary>
    /// Gets or sets the rendered chart size in pixels.
    /// </summary>
    public ChartSize Size { get; set; } = new(1000, 560);

    /// <summary>
    /// Gets or sets the chart padding around the plot area.
    /// </summary>
    public ChartPadding Padding { get; set; } = new(76, 78, 36, 74);

    /// <summary>
    /// Gets or sets the visual theme used by renderers.
    /// </summary>
    public ChartTheme Theme { get; set; } = ChartTheme.Light();

    /// <summary>
    /// Gets or sets a value indicating whether the legend is rendered.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the title and subtitle are rendered.
    /// </summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the outer card surface is rendered.
    /// </summary>
    public bool ShowCard { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the plot background surface is rendered.
    /// </summary>
    public bool ShowPlotBackground { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether grid lines are rendered.
    /// </summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether axes, tick labels, and axis titles are rendered.
    /// </summary>
    public bool ShowAxes { get; set; } = true;

    /// <summary>
    /// Gets or sets the preferred number of axis ticks.
    /// </summary>
    public int TickCount { get; set; } = 6;

    /// <summary>
    /// Gets or sets the density used for explicit x-axis labels.
    /// </summary>
    public ChartLabelDensity XAxisLabelDensity { get; set; } = ChartLabelDensity.Auto;

    /// <summary>
    /// Gets or sets the x-axis label rotation angle in degrees for capable renderers.
    /// </summary>
    public double XAxisLabelAngle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the chart background should be transparent.
    /// </summary>
    public bool TransparentBackground { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether point and bar values are rendered as labels.
    /// </summary>
    public bool ShowDataLabels { get; set; }

    /// <summary>
    /// Gets or sets how multiple bar series are arranged within each category.
    /// </summary>
    public ChartBarMode BarMode { get; set; } = ChartBarMode.Grouped;

    /// <summary>
    /// Gets or sets a value indicating whether stacked bar totals are rendered above each category.
    /// </summary>
    public bool ShowStackTotals { get; set; }

    /// <summary>
    /// Gets or sets how heatmap values are converted into cell colors.
    /// </summary>
    public ChartHeatmapScale HeatmapScale { get; set; } = ChartHeatmapScale.Sequential;

    /// <summary>
    /// Gets or sets a formatter used for y-axis ticks, data labels, stack totals, and donut totals.
    /// </summary>
    public Func<double, string>? ValueFormatter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the chart is rendered as a compact sparkline.
    /// </summary>
    public bool IsSparkline { get; set; }

    /// <summary>
    /// Gets explicit labels for x-axis values.
    /// </summary>
    public List<ChartAxisLabel> XAxisLabels { get; } = new();
}
