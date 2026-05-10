using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

/// <summary>
/// Provides opaque raster export helpers for topology charts.
/// </summary>
public static partial class TopologyChartExtensions {
    /// <summary>
    /// Renders the topology chart to BMP bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    /// <returns>A BMP image.</returns>
    public static byte[] ToBmp(this TopologyChart chart, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.ToRasterImage(RasterImageFormat.Bmp, options, imageOptions);

    /// <summary>
    /// Renders the topology chart to PPM bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    /// <returns>A PPM image.</returns>
    public static byte[] ToPpm(this TopologyChart chart, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.ToRasterImage(RasterImageFormat.Ppm, options, imageOptions);

    /// <summary>
    /// Renders the topology chart to TIFF bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    /// <returns>A TIFF image.</returns>
    public static byte[] ToTiff(this TopologyChart chart, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.ToRasterImage(RasterImageFormat.Tiff, options, imageOptions);

    /// <summary>
    /// Renders the topology chart to opaque raster image bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    /// <returns>Encoded raster image bytes.</returns>
    public static byte[] ToRasterImage(this TopologyChart chart, RasterImageFormat format, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) {
        RasterImageEncoder.ThrowIfUnsupported(format);
        return RasterImageEncoder.Encode(TopologyRasterRenderer.RenderImage(chart, options), format, imageOptions);
    }

    /// <summary>
    /// Writes the topology chart to an opaque raster image stream.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    public static void WriteRasterImage(this TopologyChart chart, Stream stream, RasterImageFormat format, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) {
        RasterImageEncoder.ThrowIfNull(stream);
        RasterImageEncoder.ThrowIfUnsupported(format);
        RasterImageEncoder.WriteTo(stream, TopologyRasterRenderer.RenderImage(chart, options), format, imageOptions);
    }

    /// <summary>
    /// Writes the topology chart to a BMP stream.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    public static void WriteBmp(this TopologyChart chart, Stream stream, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.WriteRasterImage(stream, RasterImageFormat.Bmp, options, imageOptions);

    /// <summary>
    /// Writes the topology chart to a PPM stream.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    public static void WritePpm(this TopologyChart chart, Stream stream, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.WriteRasterImage(stream, RasterImageFormat.Ppm, options, imageOptions);

    /// <summary>
    /// Writes the topology chart to a TIFF stream.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    public static void WriteTiff(this TopologyChart chart, Stream stream, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.WriteRasterImage(stream, RasterImageFormat.Tiff, options, imageOptions);

    /// <summary>
    /// Saves the topology chart as BMP.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    public static void SaveBmp(this TopologyChart chart, string path, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.SaveRasterImage(path, RasterImageFormat.Bmp, options, imageOptions);

    /// <summary>
    /// Saves the topology chart as PPM.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    public static void SavePpm(this TopologyChart chart, string path, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.SaveRasterImage(path, RasterImageFormat.Ppm, options, imageOptions);

    /// <summary>
    /// Saves the topology chart as TIFF.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    public static void SaveTiff(this TopologyChart chart, string path, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.SaveRasterImage(path, RasterImageFormat.Tiff, options, imageOptions);

    /// <summary>
    /// Saves the topology chart as an opaque raster image file.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    public static void SaveRasterImage(this TopologyChart chart, string path, RasterImageFormat format, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) {
        RasterImageEncoder.ThrowIfUnsupported(format);
        using var stream = File.Create(path);
        chart.WriteRasterImage(stream, format, options, imageOptions);
    }

    /// <summary>
    /// Saves the topology chart as an opaque raster image file using the output path extension to choose the format.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="imageOptions">Optional raster export options.</param>
    public static void SaveRasterImage(this TopologyChart chart, string path, TopologyRenderOptions? options = null, RasterImageOptions? imageOptions = null) => chart.SaveRasterImage(path, RasterImageFormatExtensions.FromFileExtension(path), options, imageOptions);
}
