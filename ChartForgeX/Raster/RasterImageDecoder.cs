using System;
using System.IO;

namespace ChartForgeX.Raster;

/// <summary>Decodes dependency-free raster image formats into RGBA pixels.</summary>
public static class RasterImageDecoder {
    /// <summary>Reads and decodes a raster image file.</summary>
    /// <param name="path">The source image path.</param>
    /// <returns>The decoded RGBA image.</returns>
    public static RgbaImage Read(string path) => Read(path, null);

    /// <summary>Reads a raster file with explicit encoded-input and pixel limits.</summary>
    public static RgbaImage Read(string path, RasterDecodeOptions? options) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        using var stream = File.OpenRead(path);
        return Read(stream, options);
    }

    /// <summary>Reads and decodes a raster image stream.</summary>
    /// <param name="stream">The source image stream.</param>
    /// <returns>The decoded RGBA image.</returns>
    public static RgbaImage Read(Stream stream) => Read(stream, null);

    /// <summary>Reads a raster stream with explicit limits without closing the caller's stream.</summary>
    public static RgbaImage Read(Stream stream, RasterDecodeOptions? options) {
        var limits = RasterDecodeLimits.From(options);
        return Decode(limits.Read(stream), limits);
    }

    /// <summary>Decodes raster image bytes into RGBA pixels.</summary>
    /// <param name="data">The encoded image bytes.</param>
    /// <returns>The decoded RGBA image.</returns>
    public static RgbaImage Decode(byte[] data) => Decode(data, (RasterDecodeOptions?)null);

    /// <summary>Decodes raster bytes with explicit encoded-input and pixel limits.</summary>
    public static RgbaImage Decode(byte[] data, RasterDecodeOptions? options) => Decode(data, RasterDecodeLimits.From(options));

    private static RgbaImage Decode(byte[] data, RasterDecodeLimits limits) {
        limits.ValidateInput(data);
        if (PngReader.IsPng(data)) return PngReader.Decode(data, limits);
        if (JpegReader.IsJpeg(data)) return JpegReader.Decode(data, limits);
        if (GifReader.IsGif(data)) return GifReader.Decode(data, limits);
        if (BmpReader.IsBmp(data)) return BmpReader.Decode(data, limits);
        if (PpmReader.IsPpm(data)) return PpmReader.Decode(data, limits);
        if (TiffReader.IsTiff(data)) return TiffReader.Decode(data, limits);

        throw new NotSupportedException("Unsupported raster image format. Supported dependency-free input formats are baseline JPEG, PNG, GIF, BMP, PPM, and uncompressed RGB TIFF.");
    }

    /// <summary>Attempts to read and decode a raster image file.</summary>
    /// <param name="path">The source image path.</param>
    /// <param name="image">The decoded RGBA image when successful.</param>
    /// <returns><c>true</c> when the image could be decoded.</returns>
    public static bool TryRead(string? path, out RgbaImage image) => TryRead(path, null, out image);

    /// <summary>Attempts to decode a file within explicit raster limits.</summary>
    public static bool TryRead(string? path, RasterDecodeOptions? options, out RgbaImage image) {
        image = default;
        if (path == null || path.Trim().Length == 0) return false;
        try {
            image = Read(path, options);
            return true;
        } catch (IOException) {
            return false;
        } catch (UnauthorizedAccessException) {
            return false;
        } catch (Exception ex) when (IsDecodeFailure(ex)) {
            return false;
        }
    }

    /// <summary>Attempts to read and decode a raster image stream.</summary>
    /// <param name="stream">The source image stream.</param>
    /// <param name="image">The decoded RGBA image when successful.</param>
    /// <returns><c>true</c> when the image could be decoded.</returns>
    public static bool TryRead(Stream? stream, out RgbaImage image) => TryRead(stream, null, out image);

    /// <summary>Attempts to decode a stream within explicit raster limits without closing it.</summary>
    public static bool TryRead(Stream? stream, RasterDecodeOptions? options, out RgbaImage image) {
        image = default;
        if (stream == null) return false;
        try {
            image = Read(stream, options);
            return true;
        } catch (Exception ex) when (IsDecodeFailure(ex)) {
            return false;
        }
    }

    /// <summary>Attempts to decode raster image bytes.</summary>
    /// <param name="data">The encoded image bytes.</param>
    /// <param name="image">The decoded RGBA image when successful.</param>
    /// <returns><c>true</c> when the image could be decoded.</returns>
    public static bool TryDecode(byte[]? data, out RgbaImage image) => TryDecode(data, null, out image);

    /// <summary>Attempts to decode bytes within explicit raster limits.</summary>
    public static bool TryDecode(byte[]? data, RasterDecodeOptions? options, out RgbaImage image) {
        image = default;
        if (data == null) return false;
        try {
            image = Decode(data, options);
            return true;
        } catch (Exception ex) when (IsDecodeFailure(ex)) {
            return false;
        }
    }

    private static bool IsDecodeFailure(Exception ex) =>
        ex is IOException ||
        ex is UnauthorizedAccessException ||
        ex is InvalidDataException ||
        ex is NotSupportedException ||
        ex is ArgumentException ||
        ex is ArithmeticException ||
        ex is IndexOutOfRangeException;

    internal static string MimeTypeFor(byte[] data, string? path = null) {
        if (data != null) {
            if (PngReader.IsPng(data)) return "image/png";
            if (JpegReader.IsJpeg(data)) return "image/jpeg";
            if (GifReader.IsGif(data)) return "image/gif";
            if (BmpReader.IsBmp(data)) return "image/bmp";
            if (PpmReader.IsPpm(data)) return "image/x-portable-pixmap";
            if (TiffReader.IsTiff(data)) return "image/tiff";
        }

        var extension = path == null ? string.Empty : Path.GetExtension(path).ToLowerInvariant();
        switch (extension) {
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".bmp": return "image/bmp";
            case ".ppm":
            case ".pnm": return "image/x-portable-pixmap";
            case ".tif":
            case ".tiff": return "image/tiff";
            default: return "application/octet-stream";
        }
    }
}
