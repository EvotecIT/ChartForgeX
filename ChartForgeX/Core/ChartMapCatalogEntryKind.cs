namespace ChartForgeX.Core;

/// <summary>
/// Identifies how a map catalog entry is made available to callers.
/// </summary>
public enum ChartMapCatalogEntryKind {
    /// <summary>
    /// The geometry is embedded in the ChartForgeX package and can be loaded without external assets.
    /// </summary>
    Embedded,

    /// <summary>
    /// The geometry is a known external dataset that callers load from a host-provided GeoJSON asset.
    /// </summary>
    External
}
