namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>
    /// Gets or sets whether SVG output keeps state marks in their colours in forced-colours mode (Windows high contrast):
    /// marks with a status (<c>data-cfx-status</c>: state timeline segments, Gantt lane items, categorical heatmap cells,
    /// legend swatches, and also semantic heatmap cells and the calendar no-data swatch) and the hatch and outline drawn
    /// over them get <c>forced-color-adjust:none</c>, as report views
    /// do for their status chips. Off by default, so forced colours apply as usual. A host that also remaps its colour
    /// variables in forced-colours mode must keep the values of the variables these marks use, because custom
    /// properties are not affected by <c>forced-color-adjust</c>. PNG output is not affected.
    /// </summary>
    public bool PinStateColorsInForcedColors { get; set; }
}
