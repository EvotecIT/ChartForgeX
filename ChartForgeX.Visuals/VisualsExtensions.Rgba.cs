using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX;

public static partial class VisualsExtensions {
    /// <summary>Renders a visual grid directly to an in-memory RGBA image without encoding it.</summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <returns>The rendered RGBA image.</returns>
    public static RgbaImage ToRgbaImage(this VisualGrid grid) => new PngVisualGridRenderer().RenderImage(grid);

    /// <summary>Renders a visual canvas directly to an in-memory RGBA image without encoding it.</summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <returns>The rendered RGBA image.</returns>
    public static RgbaImage ToRgbaImage(this VisualCanvas canvas) => new PngVisualCanvasRenderer().RenderImage(canvas);
}
