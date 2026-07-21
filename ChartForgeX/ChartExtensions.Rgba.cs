using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX;

public static partial class ChartExtensions {
    /// <summary>Renders a chart directly to an in-memory RGBA image without encoding it.</summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>The rendered RGBA image.</returns>
    public static RgbaImage ToRgbaImage(this Chart chart) => RasterRenderer.RenderImage(chart);

    /// <summary>Renders a chart grid directly to an in-memory RGBA image without encoding it.</summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>The rendered RGBA image.</returns>
    public static RgbaImage ToRgbaImage(this ChartGrid grid) => RasterRenderer.RenderImage(grid);

    /// <summary>Renders a visual block directly to an in-memory RGBA image without encoding it.</summary>
    /// <param name="block">The visual block to render.</param>
    /// <returns>The rendered RGBA image.</returns>
    public static RgbaImage ToRgbaImage(this IVisualBlock block) => RasterRenderer.RenderImage(block);

    /// <summary>Renders a visual grid directly to an in-memory RGBA image without encoding it.</summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <returns>The rendered RGBA image.</returns>
    public static RgbaImage ToRgbaImage(this VisualGrid grid) => RasterRenderer.RenderImage(grid);

    /// <summary>Renders a visual canvas directly to an in-memory RGBA image without encoding it.</summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <returns>The rendered RGBA image.</returns>
    public static RgbaImage ToRgbaImage(this VisualCanvas canvas) => new PngVisualCanvasRenderer().RenderImage(canvas);
}
