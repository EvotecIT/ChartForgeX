using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Saves the topology chart using the output path extension to choose SVG, HTML, PNG, animated GIF, animated PNG, or an opaque raster image format.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options for opaque raster formats.</param>
    public static void Save(this TopologyChart chart, string path, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) {
        var extension = ChartExtensions.GetExportExtension(path);
        if (AnimatedRasterFormatExtensions.TryFromFileExtension(extension, out var animatedFormat)) {
            SaveAnimatedRaster(chart, path, options, animatedFormat);
            return;
        }

        switch (extension) {
            case ".svg":
                chart.SaveSvg(path, options);
                return;
            case ".html":
            case ".htm":
                chart.SaveHtml(path, options);
                return;
            case ".png":
                chart.SavePng(path, options);
                return;
            default:
                chart.SaveRasterImage(path, options, imageOptions);
                return;
        }
    }
}
