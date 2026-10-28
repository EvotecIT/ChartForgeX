namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>Actual device grid; unlike typography strike metadata this cannot change independently of the surface.</summary>
    internal int EffectPixelsPerUnit => _scale;
    /// <summary>Exposes the current device samples only as a borrowed input to explicit bounded effect capture.</summary>
    /// <remarks>This is not an implicit scene-group/backdrop capture. The producer must freeze the surface and identify
    /// the complete atomic mark/text unit and its device bounds. Clipping, transforms and source inclusion are producer
    /// decisions; the effect operation then copies only its declared source region before filtering.</remarks>
    internal RgbaImage BorrowDeviceSurfaceForEffects() => new(_pixelWidth, _pixelHeight, Pixels);

}
