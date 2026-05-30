using System.IO;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX;

public static partial class ChartExtensions {
    /// <summary>
    /// Renders a chart to BMP bytes.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A BMP image.</returns>
    public static byte[] ToBmp(this Chart chart, RasterImageOptions? options = null) => chart.ToRasterImage(RasterImageFormat.Bmp, options);

    /// <summary>
    /// Renders a chart to PPM bytes.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A PPM image.</returns>
    public static byte[] ToPpm(this Chart chart, RasterImageOptions? options = null) => chart.ToRasterImage(RasterImageFormat.Ppm, options);

    /// <summary>
    /// Renders a chart to TIFF bytes.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A TIFF image.</returns>
    public static byte[] ToTiff(this Chart chart, RasterImageOptions? options = null) => chart.ToRasterImage(RasterImageFormat.Tiff, options);

    /// <summary>
    /// Renders a chart to opaque raster image bytes.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>Encoded raster image bytes.</returns>
    public static byte[] ToRasterImage(this Chart chart, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfUnsupported(format);
        return RasterImageEncoder.Encode(RasterRenderer.RenderImage(chart), format, options);
    }

    /// <summary>
    /// Writes a chart to an opaque raster image stream.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteRasterImage(this Chart chart, Stream stream, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfNull(stream);
        RasterImageEncoder.ThrowIfUnsupported(format);
        RasterImageEncoder.WriteTo(stream, RasterRenderer.RenderImage(chart), format, options);
    }

    /// <summary>
    /// Writes a chart to a BMP stream.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteBmp(this Chart chart, Stream stream, RasterImageOptions? options = null) => chart.WriteRasterImage(stream, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Writes a chart to a PPM stream.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WritePpm(this Chart chart, Stream stream, RasterImageOptions? options = null) => chart.WriteRasterImage(stream, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Writes a chart to a TIFF stream.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteTiff(this Chart chart, Stream stream, RasterImageOptions? options = null) => chart.WriteRasterImage(stream, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a chart as a BMP file.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveBmp(this Chart chart, string path, RasterImageOptions? options = null) => chart.SaveRasterImage(path, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Saves a chart as a PPM file.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SavePpm(this Chart chart, string path, RasterImageOptions? options = null) => chart.SaveRasterImage(path, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Saves a chart as a TIFF file.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveTiff(this Chart chart, string path, RasterImageOptions? options = null) => chart.SaveRasterImage(path, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a chart as an opaque raster image file.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this Chart chart, string path, RasterImageFormat format, RasterImageOptions? options = null) {
        File.WriteAllBytes(path, chart.ToRasterImage(format, options));
    }

    /// <summary>
    /// Saves a chart as an opaque raster image file using the output path extension to choose the format.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this Chart chart, string path, RasterImageOptions? options = null) => chart.SaveRasterImage(path, RasterImageFormatExtensions.FromFileExtension(path), options);

    /// <summary>
    /// Renders a chart grid to BMP bytes.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A BMP image.</returns>
    public static byte[] ToBmp(this ChartGrid grid, RasterImageOptions? options = null) => grid.ToRasterImage(RasterImageFormat.Bmp, options);

    /// <summary>
    /// Renders a chart grid to PPM bytes.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A PPM image.</returns>
    public static byte[] ToPpm(this ChartGrid grid, RasterImageOptions? options = null) => grid.ToRasterImage(RasterImageFormat.Ppm, options);

    /// <summary>
    /// Renders a chart grid to TIFF bytes.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A TIFF image.</returns>
    public static byte[] ToTiff(this ChartGrid grid, RasterImageOptions? options = null) => grid.ToRasterImage(RasterImageFormat.Tiff, options);

    /// <summary>
    /// Renders a chart grid to opaque raster image bytes.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>Encoded raster image bytes.</returns>
    public static byte[] ToRasterImage(this ChartGrid grid, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfUnsupported(format);
        return RasterImageEncoder.Encode(RasterRenderer.RenderImage(grid), format, options);
    }

    /// <summary>
    /// Writes a chart grid to an opaque raster image stream.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteRasterImage(this ChartGrid grid, Stream stream, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfNull(stream);
        RasterImageEncoder.ThrowIfUnsupported(format);
        RasterImageEncoder.WriteTo(stream, RasterRenderer.RenderImage(grid), format, options);
    }

    /// <summary>
    /// Writes a chart grid to a BMP stream.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteBmp(this ChartGrid grid, Stream stream, RasterImageOptions? options = null) => grid.WriteRasterImage(stream, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Writes a chart grid to a PPM stream.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WritePpm(this ChartGrid grid, Stream stream, RasterImageOptions? options = null) => grid.WriteRasterImage(stream, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Writes a chart grid to a TIFF stream.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteTiff(this ChartGrid grid, Stream stream, RasterImageOptions? options = null) => grid.WriteRasterImage(stream, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a chart grid as a BMP file.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveBmp(this ChartGrid grid, string path, RasterImageOptions? options = null) => grid.SaveRasterImage(path, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Saves a chart grid as a PPM file.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SavePpm(this ChartGrid grid, string path, RasterImageOptions? options = null) => grid.SaveRasterImage(path, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Saves a chart grid as a TIFF file.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveTiff(this ChartGrid grid, string path, RasterImageOptions? options = null) => grid.SaveRasterImage(path, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a chart grid as an opaque raster image file.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this ChartGrid grid, string path, RasterImageFormat format, RasterImageOptions? options = null) {
        File.WriteAllBytes(path, grid.ToRasterImage(format, options));
    }

    /// <summary>
    /// Saves a chart grid as an opaque raster image file using the output path extension to choose the format.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this ChartGrid grid, string path, RasterImageOptions? options = null) => grid.SaveRasterImage(path, RasterImageFormatExtensions.FromFileExtension(path), options);

    /// <summary>
    /// Renders a visual block to BMP bytes.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A BMP image.</returns>
    public static byte[] ToBmp(this IVisualBlock block, RasterImageOptions? options = null) => block.ToRasterImage(RasterImageFormat.Bmp, options);

    /// <summary>
    /// Renders a visual block to PPM bytes.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A PPM image.</returns>
    public static byte[] ToPpm(this IVisualBlock block, RasterImageOptions? options = null) => block.ToRasterImage(RasterImageFormat.Ppm, options);

    /// <summary>
    /// Renders a visual block to TIFF bytes.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A TIFF image.</returns>
    public static byte[] ToTiff(this IVisualBlock block, RasterImageOptions? options = null) => block.ToRasterImage(RasterImageFormat.Tiff, options);

    /// <summary>
    /// Renders a visual block to opaque raster image bytes.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>Encoded raster image bytes.</returns>
    public static byte[] ToRasterImage(this IVisualBlock block, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfUnsupported(format);
        return RasterImageEncoder.Encode(RasterRenderer.RenderImage(block), format, options);
    }

    /// <summary>
    /// Writes a visual block to an opaque raster image stream.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteRasterImage(this IVisualBlock block, Stream stream, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfNull(stream);
        RasterImageEncoder.ThrowIfUnsupported(format);
        RasterImageEncoder.WriteTo(stream, RasterRenderer.RenderImage(block), format, options);
    }

    /// <summary>
    /// Writes a visual block to a BMP stream.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteBmp(this IVisualBlock block, Stream stream, RasterImageOptions? options = null) => block.WriteRasterImage(stream, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Writes a visual block to a PPM stream.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WritePpm(this IVisualBlock block, Stream stream, RasterImageOptions? options = null) => block.WriteRasterImage(stream, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Writes a visual block to a TIFF stream.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteTiff(this IVisualBlock block, Stream stream, RasterImageOptions? options = null) => block.WriteRasterImage(stream, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a visual block as a BMP file.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveBmp(this IVisualBlock block, string path, RasterImageOptions? options = null) => block.SaveRasterImage(path, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Saves a visual block as a PPM file.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SavePpm(this IVisualBlock block, string path, RasterImageOptions? options = null) => block.SaveRasterImage(path, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Saves a visual block as a TIFF file.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveTiff(this IVisualBlock block, string path, RasterImageOptions? options = null) => block.SaveRasterImage(path, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a visual block as an opaque raster image file.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this IVisualBlock block, string path, RasterImageFormat format, RasterImageOptions? options = null) {
        File.WriteAllBytes(path, block.ToRasterImage(format, options));
    }

    /// <summary>
    /// Saves a visual block as an opaque raster image file using the output path extension to choose the format.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this IVisualBlock block, string path, RasterImageOptions? options = null) => block.SaveRasterImage(path, RasterImageFormatExtensions.FromFileExtension(path), options);

    /// <summary>
    /// Renders a visual grid to BMP bytes.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A BMP image.</returns>
    public static byte[] ToBmp(this VisualGrid grid, RasterImageOptions? options = null) => grid.ToRasterImage(RasterImageFormat.Bmp, options);

    /// <summary>
    /// Renders a visual grid to PPM bytes.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A PPM image.</returns>
    public static byte[] ToPpm(this VisualGrid grid, RasterImageOptions? options = null) => grid.ToRasterImage(RasterImageFormat.Ppm, options);

    /// <summary>
    /// Renders a visual grid to TIFF bytes.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A TIFF image.</returns>
    public static byte[] ToTiff(this VisualGrid grid, RasterImageOptions? options = null) => grid.ToRasterImage(RasterImageFormat.Tiff, options);

    /// <summary>
    /// Renders a visual grid to opaque raster image bytes.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>Encoded raster image bytes.</returns>
    public static byte[] ToRasterImage(this VisualGrid grid, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfUnsupported(format);
        return RasterImageEncoder.Encode(RasterRenderer.RenderImage(grid), format, options);
    }

    /// <summary>
    /// Writes a visual grid to an opaque raster image stream.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteRasterImage(this VisualGrid grid, Stream stream, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfNull(stream);
        RasterImageEncoder.ThrowIfUnsupported(format);
        RasterImageEncoder.WriteTo(stream, RasterRenderer.RenderImage(grid), format, options);
    }

    /// <summary>
    /// Writes a visual grid to a BMP stream.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteBmp(this VisualGrid grid, Stream stream, RasterImageOptions? options = null) => grid.WriteRasterImage(stream, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Writes a visual grid to a PPM stream.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WritePpm(this VisualGrid grid, Stream stream, RasterImageOptions? options = null) => grid.WriteRasterImage(stream, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Writes a visual grid to a TIFF stream.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteTiff(this VisualGrid grid, Stream stream, RasterImageOptions? options = null) => grid.WriteRasterImage(stream, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a visual grid as a BMP file.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveBmp(this VisualGrid grid, string path, RasterImageOptions? options = null) => grid.SaveRasterImage(path, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Saves a visual grid as a PPM file.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SavePpm(this VisualGrid grid, string path, RasterImageOptions? options = null) => grid.SaveRasterImage(path, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Saves a visual grid as a TIFF file.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveTiff(this VisualGrid grid, string path, RasterImageOptions? options = null) => grid.SaveRasterImage(path, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a visual grid as an opaque raster image file.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this VisualGrid grid, string path, RasterImageFormat format, RasterImageOptions? options = null) {
        File.WriteAllBytes(path, grid.ToRasterImage(format, options));
    }

    /// <summary>
    /// Saves a visual grid as an opaque raster image file using the output path extension to choose the format.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this VisualGrid grid, string path, RasterImageOptions? options = null) => grid.SaveRasterImage(path, RasterImageFormatExtensions.FromFileExtension(path), options);

    /// <summary>
    /// Renders a visual canvas to BMP bytes.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A BMP image.</returns>
    public static byte[] ToBmp(this VisualCanvas canvas, RasterImageOptions? options = null) => canvas.ToRasterImage(RasterImageFormat.Bmp, options);

    /// <summary>
    /// Renders a visual canvas to PPM bytes.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A PPM image.</returns>
    public static byte[] ToPpm(this VisualCanvas canvas, RasterImageOptions? options = null) => canvas.ToRasterImage(RasterImageFormat.Ppm, options);

    /// <summary>
    /// Renders a visual canvas to TIFF bytes.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A TIFF image.</returns>
    public static byte[] ToTiff(this VisualCanvas canvas, RasterImageOptions? options = null) => canvas.ToRasterImage(RasterImageFormat.Tiff, options);

    /// <summary>
    /// Renders a visual canvas to opaque raster image bytes.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>Encoded raster image bytes.</returns>
    public static byte[] ToRasterImage(this VisualCanvas canvas, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfUnsupported(format);
        return RasterImageEncoder.Encode(new PngVisualCanvasRenderer().RenderImage(canvas), format, options);
    }

    /// <summary>
    /// Writes a visual canvas to an opaque raster image stream.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteRasterImage(this VisualCanvas canvas, Stream stream, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfNull(stream);
        RasterImageEncoder.ThrowIfUnsupported(format);
        RasterImageEncoder.WriteTo(stream, new PngVisualCanvasRenderer().RenderImage(canvas), format, options);
    }

    /// <summary>
    /// Writes a visual canvas to a BMP stream.
    /// </summary>
    public static void WriteBmp(this VisualCanvas canvas, Stream stream, RasterImageOptions? options = null) => canvas.WriteRasterImage(stream, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Writes a visual canvas to a PPM stream.
    /// </summary>
    public static void WritePpm(this VisualCanvas canvas, Stream stream, RasterImageOptions? options = null) => canvas.WriteRasterImage(stream, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Writes a visual canvas to a TIFF stream.
    /// </summary>
    public static void WriteTiff(this VisualCanvas canvas, Stream stream, RasterImageOptions? options = null) => canvas.WriteRasterImage(stream, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a visual canvas as a BMP file.
    /// </summary>
    public static void SaveBmp(this VisualCanvas canvas, string path, RasterImageOptions? options = null) => canvas.SaveRasterImage(path, RasterImageFormat.Bmp, options);

    /// <summary>
    /// Saves a visual canvas as a PPM file.
    /// </summary>
    public static void SavePpm(this VisualCanvas canvas, string path, RasterImageOptions? options = null) => canvas.SaveRasterImage(path, RasterImageFormat.Ppm, options);

    /// <summary>
    /// Saves a visual canvas as a TIFF file.
    /// </summary>
    public static void SaveTiff(this VisualCanvas canvas, string path, RasterImageOptions? options = null) => canvas.SaveRasterImage(path, RasterImageFormat.Tiff, options);

    /// <summary>
    /// Saves a visual canvas as an opaque raster image file.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this VisualCanvas canvas, string path, RasterImageFormat format, RasterImageOptions? options = null) {
        File.WriteAllBytes(path, canvas.ToRasterImage(format, options));
    }

    /// <summary>
    /// Saves a visual canvas as an opaque raster image file using the output path extension to choose the format.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this VisualCanvas canvas, string path, RasterImageOptions? options = null) => canvas.SaveRasterImage(path, RasterImageFormatExtensions.FromFileExtension(path), options);

    /// <summary>
    /// Encodes RGBA pixels to PNG bytes.
    /// </summary>
    /// <param name="image">The image to encode.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this RgbaImage image) => RasterImageEncoder.Encode(image, RasterImageFormat.Png);

    /// <summary>
    /// Encodes RGBA pixels to JPEG bytes after flattening transparent pixels against the configured background.
    /// </summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>A JPEG image.</returns>
    public static byte[] ToJpeg(this RgbaImage image, RasterImageOptions? options = null) => RasterImageEncoder.Encode(image, RasterImageFormat.Jpeg, options);

    /// <summary>
    /// Encodes RGBA pixels to a dependency-free raster format.
    /// </summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    /// <returns>Encoded raster image bytes.</returns>
    public static byte[] ToRasterImage(this RgbaImage image, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfUnsupported(format);
        return RasterImageEncoder.Encode(image, format, options);
    }

    /// <summary>
    /// Writes RGBA pixels to a dependency-free raster stream.
    /// </summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void WriteRasterImage(this RgbaImage image, Stream stream, RasterImageFormat format, RasterImageOptions? options = null) {
        RasterImageEncoder.ThrowIfNull(stream);
        RasterImageEncoder.ThrowIfUnsupported(format);
        RasterImageEncoder.WriteTo(stream, image, format, options);
    }

    /// <summary>
    /// Saves RGBA pixels to a dependency-free raster file.
    /// </summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="format">The raster image format.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this RgbaImage image, string path, RasterImageFormat format, RasterImageOptions? options = null) {
        File.WriteAllBytes(path, image.ToRasterImage(format, options));
    }

    /// <summary>
    /// Saves RGBA pixels to a raster file using the output path extension to choose the format.
    /// </summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">Optional raster export options.</param>
    public static void SaveRasterImage(this RgbaImage image, string path, RasterImageOptions? options = null) => image.SaveRasterImage(path, RasterImageFormatExtensions.FromFileExtension(path), options);
}
