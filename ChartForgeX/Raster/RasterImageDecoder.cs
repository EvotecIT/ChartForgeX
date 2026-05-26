using System;
using System.IO;

namespace ChartForgeX.Raster;

/// <summary>Decodes dependency-free raster image formats into RGBA pixels.</summary>
public static class RasterImageDecoder {
    /// <summary>Reads and decodes a raster image file.</summary>
    /// <param name="path">The source image path.</param>
    /// <returns>The decoded RGBA image.</returns>
    public static RgbaImage Read(string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        return Decode(File.ReadAllBytes(path));
    }

    /// <summary>Reads and decodes a raster image stream.</summary>
    /// <param name="stream">The source image stream.</param>
    /// <returns>The decoded RGBA image.</returns>
    public static RgbaImage Read(Stream stream) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Decode(buffer.ToArray());
    }

    /// <summary>Decodes raster image bytes into RGBA pixels.</summary>
    /// <param name="data">The encoded image bytes.</param>
    /// <returns>The decoded RGBA image.</returns>
    public static RgbaImage Decode(byte[] data) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (PngReader.IsPng(data)) return PngReader.Decode(data);
        if (JpegReader.IsJpeg(data)) return JpegReader.Decode(data);
        if (BmpReader.IsBmp(data)) return BmpReader.Decode(data);
        if (PpmReader.IsPpm(data)) return PpmReader.Decode(data);
        if (TiffReader.IsTiff(data)) return TiffReader.Decode(data);

        throw new NotSupportedException("Unsupported raster image format. Supported dependency-free input formats are baseline JPEG, PNG, BMP, PPM, and uncompressed RGB TIFF.");
    }

    /// <summary>Attempts to read and decode a raster image file.</summary>
    /// <param name="path">The source image path.</param>
    /// <param name="image">The decoded RGBA image when successful.</param>
    /// <returns><c>true</c> when the image could be decoded.</returns>
    public static bool TryRead(string? path, out RgbaImage image) {
        image = default;
        if (path == null || path.Trim().Length == 0) return false;
        try {
            image = Read(path);
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
    public static bool TryRead(Stream? stream, out RgbaImage image) {
        image = default;
        if (stream == null) return false;
        try {
            image = Read(stream);
            return true;
        } catch (Exception ex) when (IsDecodeFailure(ex)) {
            return false;
        }
    }

    /// <summary>Attempts to decode raster image bytes.</summary>
    /// <param name="data">The encoded image bytes.</param>
    /// <param name="image">The decoded RGBA image when successful.</param>
    /// <returns><c>true</c> when the image could be decoded.</returns>
    public static bool TryDecode(byte[]? data, out RgbaImage image) {
        image = default;
        if (data == null) return false;
        try {
            image = Decode(data);
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
            if (BmpReader.IsBmp(data)) return "image/bmp";
            if (PpmReader.IsPpm(data)) return "image/x-portable-pixmap";
            if (TiffReader.IsTiff(data)) return "image/tiff";
        }

        var extension = path == null ? string.Empty : Path.GetExtension(path).ToLowerInvariant();
        switch (extension) {
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".bmp": return "image/bmp";
            case ".ppm":
            case ".pnm": return "image/x-portable-pixmap";
            case ".tif":
            case ".tiff": return "image/tiff";
            default: return "application/octet-stream";
        }
    }
}
