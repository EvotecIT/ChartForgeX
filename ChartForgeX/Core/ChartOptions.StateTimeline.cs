using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>
    /// Gets the categorical state map shared by state timelines and categorical heatmaps, in legend order. Keys that
    /// are not registered render in the theme's muted colour and show the raw key. The category legend is drawn at the
    /// bottom when <see cref="ShowLegend"/> is enabled (heatmaps also require <see cref="ShowHeatmapScale"/>);
    /// <see cref="LegendPosition"/> does not apply to it.
    /// </summary>
    public List<ChartStateCategory> StateCategories { get; } = new();

    /// <summary>
    /// Gets or sets the header for the right-hand lane summary column of state timelines and Gantt lanes, for example
    /// <c>Available</c>. The column is shown when any lane has a summary or this header is set.
    /// </summary>
    public string? LaneSummaryHeader { get; set; }
}
