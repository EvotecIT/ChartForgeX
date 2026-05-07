using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>
    /// Gets or sets the longitude/latitude window rendered by dotted map charts.
    /// </summary>
    public ChartMapViewport MapViewport { get; set; } = ChartMapViewport.World();

    /// <summary>
    /// Gets or sets the region-map geometry used by generic region map charts.
    /// </summary>
    public ChartMapDefinition? RegionMapDefinition { get; set; }

    /// <summary>
    /// Gets or sets the tile-map geometry used by tile map charts.
    /// </summary>
    public ChartTileMapDefinition? TileMapDefinition { get; set; }

    /// <summary>
    /// Gets the connector lines rendered on capable map charts.
    /// </summary>
    public List<ChartMapConnector> MapConnectors { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether region labels are rendered on map charts.
    /// </summary>
    public bool ShowMapLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether map scale legends are rendered.
    /// </summary>
    public bool ShowMapScaleLegend { get; set; } = true;
}
