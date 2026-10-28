using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartMapScaleLegendPosition _mapScaleLegendPosition = ChartMapScaleLegendPosition.Bottom;
    private ChartMapViewport _mapViewport = ChartMapViewport.World();
    /// <summary>Gets or sets optional shared collision-aware map captions and straight callout leaders. Null preserves legacy placement.</summary>
    public ChartMapCallouts? MapCallouts { get; set; }
    /// <summary>
    /// Gets or sets the longitude/latitude window rendered by dotted map charts.
    /// </summary>
    public ChartMapViewport MapViewport {
        get => _mapViewport;
        set { _mapViewport = value; HasExplicitMapViewport = true; }
    }

    internal bool HasExplicitMapViewport { get; private set; }

    /// <summary>
    /// Gets or sets optional source coordinate bounds used to frame region maps.
    /// </summary>
    public ChartRect? RegionMapBounds { get; set; }

    /// <summary>
    /// Gets the non-data geography layers rendered behind region maps.
    /// </summary>
    public List<ChartMapLayer> MapBaseLayers { get; } = new();

    /// <summary>
    /// Gets the non-data geography layers rendered above region maps.
    /// </summary>
    public List<ChartMapLayer> MapOverlayLayers { get; } = new();

    /// <summary>
    /// Gets the connector lines rendered on capable map charts.
    /// </summary>
    public List<ChartMapConnector> MapConnectors { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether region labels are rendered on map charts.
    /// </summary>
    public bool ShowMapLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets where map scale legends are rendered.
    /// </summary>
    public ChartMapScaleLegendPosition MapScaleLegendPosition {
        get => _mapScaleLegendPosition;
        set { _mapScaleLegendPosition = value; HasExplicitMapScaleLegendPosition = true; }
    }

    internal bool HasExplicitMapScaleLegendPosition { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether map charts render their own background surface.
    /// </summary>
    public bool ShowMapSurface { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional color scale used by region and tile maps.
    /// </summary>
    public ChartColorScale? MapColorScale { get; set; }

    /// <summary>
    /// Gets or sets the optional stroke color used for data regions.
    /// </summary>
    public ChartColor? MapRegionStrokeColor { get; set; }

    /// <summary>
    /// Gets or sets the stroke width used for data regions.
    /// </summary>
    public double MapRegionStrokeWidth { get; set; } = 1.1;
}
