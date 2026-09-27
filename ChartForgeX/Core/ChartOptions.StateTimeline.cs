using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>
    /// Gets the categorical state map for state timelines, in legend order. Segments whose state key is not
    /// registered render in the theme's muted colour and show the raw key. The state legend is drawn below the axis
    /// when <see cref="ShowLegend"/> is enabled; <see cref="LegendPosition"/> does not apply to state timelines.
    /// </summary>
    public List<ChartStateCategory> StateCategories { get; } = new();

    /// <summary>
    /// Gets or sets the header for the right-hand lane summary column, for example <c>Available</c>.
    /// The column is shown when any lane has a summary or this header is set.
    /// </summary>
    public string? StateTimelineSummaryHeader { get; set; }
}
