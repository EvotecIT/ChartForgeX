using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Map viewport, layers, labels and colour scales. Configure it through <see cref="ChartOptions.Map"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartMapOptions {
    internal ChartMapOptions Clone() {
        var copy = (ChartMapOptions)MemberwiseClone();
        copy.BaseLayers = new List<ChartMapLayer>(BaseLayers);
        copy.OverlayLayers = new List<ChartMapLayer>(OverlayLayers);
        copy.Connectors = new List<ChartMapConnector>(Connectors);
        copy._mapColorDimension = _mapColorDimension.Clone();
        return copy;
    }
    /// <summary>Gets the name and number display policy for numeric region and tile-map measurements.</summary>
    public ChartColorDimensionOptions ColorDimension {
        get => _mapColorDimension;
        set => _mapColorDimension = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartColorDimensionOptions _mapColorDimension = new();

    private ChartMapScaleLegendPosition _mapScaleLegendPosition = ChartMapScaleLegendPosition.Bottom;

    private ChartMapViewport _mapViewport = ChartMapViewport.World();

    /// <summary>Gets or sets optional shared collision-aware map captions and straight callout leaders. Null preserves legacy placement.</summary>
    public ChartMapCallouts? Callouts { get; set; }

    /// <summary>
    /// Gets or sets the longitude/latitude window rendered by dotted map charts.
    /// </summary>
    public ChartMapViewport Viewport {
        get => _mapViewport;
        set { _mapViewport = value; HasExplicitViewport = true; }
    }


    internal bool HasExplicitViewport { get; private set; }


    /// <summary>
    /// Gets or sets optional source coordinate bounds used to frame region maps.
    /// </summary>
    public ChartRect? RegionBounds { get; set; }


    /// <summary>
    /// Gets the non-data geography layers rendered behind region maps.
    /// </summary>
    public List<ChartMapLayer> BaseLayers { get; private set; } = new();


    /// <summary>
    /// Gets the non-data geography layers rendered above region maps.
    /// </summary>
    public List<ChartMapLayer> OverlayLayers { get; private set; } = new();


    /// <summary>
    /// Gets the connector lines rendered on capable map charts.
    /// </summary>
    public List<ChartMapConnector> Connectors { get; private set; } = new();


    /// <summary>
    /// Gets or sets a value indicating whether region labels are rendered on map charts.
    /// </summary>
    public bool ShowLabels { get; set; } = true;


    /// <summary>
    /// Gets or sets where map scale legends are rendered.
    /// </summary>
    public ChartMapScaleLegendPosition ScaleLegendPosition {
        get => _mapScaleLegendPosition;
        set { _mapScaleLegendPosition = value; HasExplicitScaleLegendPosition = true; }
    }


    internal bool HasExplicitScaleLegendPosition { get; private set; }


    /// <summary>
    /// Gets or sets a value indicating whether map charts render their own background surface.
    /// </summary>
    public bool ShowSurface { get; set; } = true;


    /// <summary>
    /// Gets or sets the optional color scale used by region and tile maps.
    /// </summary>
    public ChartColorScale? ColorScale { get; set; }


    /// <summary>
    /// Gets or sets the optional stroke color used for data regions.
    /// </summary>
    public ChartColor? RegionStrokeColor { get; set; }


    /// <summary>
    /// Gets or sets the stroke width used for data regions.
    /// </summary>
    public double RegionStrokeWidth { get; set; } = 1.1;
}
