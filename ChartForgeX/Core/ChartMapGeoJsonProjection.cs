namespace ChartForgeX.Core;

/// <summary>
/// Defines how longitude/latitude coordinates are projected into map definition coordinates.
/// </summary>
public enum ChartMapGeoJsonProjection {
    /// <summary>
    /// Uses longitude as X and inverted latitude as Y.
    /// </summary>
    Equirectangular,

    /// <summary>
    /// Uses a Web Mercator style latitude transform.
    /// </summary>
    WebMercator
}
