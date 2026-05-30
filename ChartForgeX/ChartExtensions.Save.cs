using System;
using ChartForgeX.Core;
using ChartForgeX.Composition;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX;

public static partial class ChartExtensions {
    /// <summary>
    /// Saves a chart using the output path extension to choose SVG, HTML, PNG, or another raster image format.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="rasterOptions">Optional raster export options.</param>
    public static void Save(this Chart chart, string path, RasterImageOptions? rasterOptions = null) {
        if (TrySaveCommonOutput(path, () => chart.SaveSvg(path), () => chart.SaveHtml(path), () => chart.SavePng(path))) return;
        chart.SaveRasterImage(path, rasterOptions);
    }

    /// <summary>
    /// Saves a chart grid using the output path extension to choose SVG, HTML, PNG, or another raster image format.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="rasterOptions">Optional raster export options.</param>
    public static void Save(this ChartGrid grid, string path, RasterImageOptions? rasterOptions = null) {
        if (TrySaveCommonOutput(path, () => grid.SaveSvg(path), () => grid.SaveHtml(path), () => grid.SavePng(path))) return;
        grid.SaveRasterImage(path, rasterOptions);
    }

    /// <summary>
    /// Saves a visual block using the output path extension to choose SVG, HTML, PNG, or another raster image format.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="rasterOptions">Optional raster export options.</param>
    public static void Save(this IVisualBlock block, string path, RasterImageOptions? rasterOptions = null) {
        if (TrySaveCommonOutput(path, () => block.SaveSvg(path), () => block.SaveHtml(path), () => block.SavePng(path))) return;
        block.SaveRasterImage(path, rasterOptions);
    }

    /// <summary>
    /// Saves a visual grid using the output path extension to choose SVG, HTML, PNG, or another raster image format.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="rasterOptions">Optional raster export options.</param>
    public static void Save(this VisualGrid grid, string path, RasterImageOptions? rasterOptions = null) {
        if (TrySaveCommonOutput(path, () => grid.SaveSvg(path), () => grid.SaveHtml(path), () => grid.SavePng(path))) return;
        grid.SaveRasterImage(path, rasterOptions);
    }

    /// <summary>
    /// Saves a visual canvas using the output path extension to choose SVG, HTML, PNG, or another raster image format.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="rasterOptions">Optional raster export options.</param>
    public static void Save(this VisualCanvas canvas, string path, RasterImageOptions? rasterOptions = null) {
        if (TrySaveCommonOutput(path, () => canvas.SaveSvg(path), () => canvas.SaveHtml(path), () => canvas.SavePng(path))) return;
        canvas.SaveRasterImage(path, rasterOptions);
    }

    private static bool TrySaveCommonOutput(string path, Action saveSvg, Action saveHtml, Action savePng) {
        var extension = GetExportExtension(path);
        switch (extension) {
            case ".svg":
                saveSvg();
                return true;
            case ".html":
            case ".htm":
                saveHtml();
                return true;
            case ".png":
                savePng();
                return true;
            default:
                return false;
        }
    }

    internal static string GetExportExtension(string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        var extension = System.IO.Path.GetExtension(path.Trim());
        if (string.IsNullOrWhiteSpace(extension)) throw new ArgumentException("Export file extension cannot be empty.", nameof(path));
        return extension.ToLowerInvariant();
    }
}
