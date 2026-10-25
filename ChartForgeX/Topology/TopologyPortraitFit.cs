namespace ChartForgeX.Topology;

/// <summary>Chooses how an embedded portrait fits the existing topology artwork slot.</summary>
public enum TopologyPortraitFit {
    /// <summary>Retains the complete image, centered within the slot, with unpainted space when its aspect differs.</summary>
    Meet,
    /// <summary>Fills the slot with a centered image crop. Native raster and SVG use the same captured pixels.</summary>
    Slice
}
