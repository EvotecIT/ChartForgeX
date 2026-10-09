using System;
using ChartForgeX.Core;
using ChartForgeX.Composition;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX;

public static partial class VisualsExtensions {
    /// <summary>
    /// Saves a visual grid using the output path extension to choose SVG, HTML, PNG, or another raster image format.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="rasterOptions">Optional raster export options.</param>
    public static void Save(this VisualGrid grid, string path, RasterImageOptions? rasterOptions = null) {
        if (ChartExtensions.TrySaveSvgOrHtmlOutput(path, () => grid.SaveSvg(path), () => grid.SaveHtml(path))) return;
        grid.SaveRasterImage(path, rasterOptions);
    }

    /// <summary>
    /// Saves a visual canvas using the output path extension to choose SVG, HTML, PNG, or another raster image format.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="rasterOptions">Optional raster export options.</param>
    public static void Save(this VisualCanvas canvas, string path, RasterImageOptions? rasterOptions = null) {
        if (ChartExtensions.TrySaveSvgOrHtmlOutput(path, () => canvas.SaveSvg(path), () => canvas.SaveHtml(path))) return;
        canvas.SaveRasterImage(path, rasterOptions);
    }

}
