namespace ChartForgeX.Rendering;

/// <summary>A static producer that can describe its native raster allocation before rendering pixels.</summary>
/// <remarks>This optional capability preserves producer ownership without inferring dimensions from SVG or an
/// artifact's authored viewport. Measurement must use the same captured state and output scale as
/// <see cref="IStaticVisualSource.RenderRgba"/>, return positive whole pixel dimensions, and avoid allocating a raster canvas.</remarks>
public interface IMeasuredStaticVisualSource : IStaticVisualSource {
    /// <summary>Measures the exact destination pixel dimensions of the next native raster export.</summary>
    VisualSize MeasureRasterSize();
}
