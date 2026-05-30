using System;
using System.IO;
using ChartForgeX.Core;

namespace ChartForgeX.Raster;

internal static class RasterImageEncoder {
    internal static void ThrowIfNull(Stream stream) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
    }

    internal static void ThrowIfUnsupported(RasterImageFormat format) {
        if (!format.IsSupported()) throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported raster image format.");
    }

    internal static byte[] Encode(RgbaImage image, RasterImageFormat format, RasterImageOptions? options = null) {
        switch (format) {
            case RasterImageFormat.Png:
                return PngWriter.WriteRgba(image);
            case RasterImageFormat.Jpeg:
                return JpegWriter.WriteRgba(image, options);
            case RasterImageFormat.Bmp:
                return BmpWriter.WriteRgba(image, options);
            case RasterImageFormat.Ppm:
                return PpmWriter.WriteRgba(image, options);
            case RasterImageFormat.Tiff:
                return TiffWriter.WriteRgba(image, options);
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported raster image format.");
        }
    }

    internal static void WriteTo(Stream stream, RgbaImage image, RasterImageFormat format, RasterImageOptions? options = null) {
        ThrowIfNull(stream);
        switch (format) {
            case RasterImageFormat.Png:
                var png = PngWriter.WriteRgba(image);
                stream.Write(png, 0, png.Length);
                break;
            case RasterImageFormat.Jpeg:
                JpegWriter.WriteRgba(stream, image, options);
                break;
            case RasterImageFormat.Bmp:
                BmpWriter.WriteRgba(stream, image, options);
                break;
            case RasterImageFormat.Ppm:
                PpmWriter.WriteRgba(stream, image, options);
                break;
            case RasterImageFormat.Tiff:
                TiffWriter.WriteRgba(stream, image, options);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported raster image format.");
        }
    }
}
