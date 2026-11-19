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
    WebMercator,

    /// <summary>
    /// Uses the Miller cylindrical projection, a compromise between Mercator and equirectangular that keeps high latitudes readable.
    /// </summary>
    Miller,

    /// <summary>
    /// Projects the United States with an Albers equal-area conic for the lower 48 states and Alaska and Hawaii drawn as insets.
    /// It matches the layout of the built-in <c>us-states</c> geometry. Points outside the United States are projected with the lower 48 transform.
    /// </summary>
    UnitedStatesAlbers,

    /// <summary>
    /// Projects the United States with a Lambert conformal conic for the lower 48 states and Alaska and Hawaii drawn as insets.
    /// </summary>
    UnitedStatesLambert,

    /// <summary>
    /// Projects the United States with Mercator for the lower 48 states and Alaska and Hawaii drawn as insets.
    /// </summary>
    UnitedStatesMercator,

    /// <summary>
    /// Projects the United States with Miller cylindrical for the lower 48 states and Alaska and Hawaii drawn as insets.
    /// </summary>
    UnitedStatesMiller
}
