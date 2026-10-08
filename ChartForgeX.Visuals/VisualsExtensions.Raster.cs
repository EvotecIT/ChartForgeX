using System.IO;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX;

public static partial class VisualsExtensions {
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
        return RasterImageEncoder.Encode(grid.ToRgbaImage(), format, options);
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
        RasterImageEncoder.WriteTo(stream, grid.ToRgbaImage(), format, options);
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
        return RasterImageEncoder.Encode(canvas.ToRgbaImage(), format, options);
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
        RasterImageEncoder.WriteTo(stream, canvas.ToRgbaImage(), format, options);
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

}
