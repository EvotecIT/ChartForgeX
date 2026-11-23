using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>State timeline categories and summary. Configure it through <see cref="ChartOptions.StateTimeline"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartStateTimelineOptions {
    internal ChartStateTimelineOptions Clone() {
        var copy = (ChartStateTimelineOptions)MemberwiseClone();
        copy.Categories = new List<ChartStateCategory>(Categories);
        return copy;
    }
    /// <summary>
    /// Gets or sets whether SVG output keeps state marks in their colours in forced-colours mode (Windows high contrast):
    /// marks with a status (<c>data-cfx-status</c>: state timeline segments, Gantt lane items, categorical heatmap cells,
    /// legend swatches, and also semantic heatmap cells and the calendar no-data swatch) and the hatch and outline drawn
    /// over them get <c>forced-color-adjust:none</c>, as report views
    /// do for their status chips. Off by default, so forced colours apply as usual. A host that also remaps its colour
    /// variables in forced-colours mode must keep the values of the variables these marks use, because custom
    /// properties are not affected by <c>forced-color-adjust</c>. PNG output is not affected.
    /// </summary>
    public bool PinColorsInForcedColors { get; set; }

    /// <summary>
    /// Gets the categorical state map shared by state timelines and categorical heatmaps, in legend order. Keys that
    /// are not registered render in the theme's muted colour and show the raw key. The category legend is drawn at the
    /// bottom when <see cref="ChartLegendOptions.Visible"/> is enabled (heatmaps also require <see cref="ChartHeatmapOptions.ShowCategoryLegend"/>);
    /// <see cref="ChartLegendOptions.Position"/> does not apply to it.
    /// </summary>
    public List<ChartStateCategory> Categories { get; private set; } = new();


    /// <summary>
    /// Gets or sets the header for the right-hand summary column of state timelines.
    /// This remains an alias of <see cref="ChartStateTimelineOptions.SummaryHeader"/> for existing callers.
    /// </summary>
    public string? SummaryHeader { get; set; }
}
