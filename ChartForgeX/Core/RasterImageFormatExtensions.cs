using System;
using System.IO;

namespace ChartForgeX.Core;

/// <summary>
/// Provides metadata helpers for opaque raster image formats.
/// </summary>
public static class RasterImageFormatExtensions {
    /// <summary>
    /// Gets the supported opaque raster image formats in a stable display order.
    /// </summary>
    /// <returns>A new array containing the supported raster image formats.</returns>
    public static RasterImageFormat[] GetSupportedFormats() {
        return new[] {
            RasterImageFormat.Png,
            RasterImageFormat.Jpeg,
            RasterImageFormat.Bmp,
            RasterImageFormat.Ppm,
            RasterImageFormat.Tiff
        };
    }

    /// <summary>
    /// Determines whether the raster image format is currently supported.
    /// </summary>
    /// <param name="format">The raster image format.</param>
    /// <returns>True when the format can be encoded; otherwise, false.</returns>
    public static bool IsSupported(this RasterImageFormat format) {
        switch (format) {
            case RasterImageFormat.Png:
            case RasterImageFormat.Jpeg:
            case RasterImageFormat.Bmp:
            case RasterImageFormat.Ppm:
            case RasterImageFormat.Tiff:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Attempts to resolve a raster image format from a file extension or file path.
    /// </summary>
    /// <param name="pathOrExtension">A file extension such as ".bmp" or "bmp", or a file path ending in a supported extension.</param>
    /// <param name="format">The resolved raster image format when the method returns true.</param>
    /// <returns>True when the extension is supported; otherwise, false.</returns>
    public static bool TryFromFileExtension(string? pathOrExtension, out RasterImageFormat format) {
        format = default;
        if (pathOrExtension == null) return false;

        string extension;
        try {
            extension = NormalizeExtension(pathOrExtension);
        } catch (ArgumentException) {
            return false;
        }

        switch (extension) {
            case ".png":
                format = RasterImageFormat.Png;
                return true;
            case ".jpg":
            case ".jpeg":
                format = RasterImageFormat.Jpeg;
                return true;
            case ".bmp":
                format = RasterImageFormat.Bmp;
                return true;
            case ".ppm":
                format = RasterImageFormat.Ppm;
                return true;
            case ".tif":
            case ".tiff":
                format = RasterImageFormat.Tiff;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Resolves a raster image format from a file extension or file path.
    /// </summary>
    /// <param name="pathOrExtension">A file extension such as ".bmp" or "bmp", or a file path ending in a supported extension.</param>
    /// <returns>The resolved raster image format.</returns>
    public static RasterImageFormat FromFileExtension(string pathOrExtension) {
        if (pathOrExtension == null) throw new ArgumentNullException(nameof(pathOrExtension));
        if (TryFromFileExtension(pathOrExtension, out var format)) return format;
        var extension = NormalizeExtension(pathOrExtension);
        throw new ArgumentException("Unsupported raster image file extension '" + extension + "'.", nameof(pathOrExtension));
    }

    /// <summary>
    /// Gets the conventional file extension for the raster image format.
    /// </summary>
    /// <param name="format">The raster image format.</param>
    /// <returns>The lowercase file extension, including the leading dot.</returns>
    public static string GetFileExtension(this RasterImageFormat format) {
        switch (format) {
            case RasterImageFormat.Png:
                return ".png";
            case RasterImageFormat.Jpeg:
                return ".jpg";
            case RasterImageFormat.Bmp:
                return ".bmp";
            case RasterImageFormat.Ppm:
                return ".ppm";
            case RasterImageFormat.Tiff:
                return ".tiff";
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported raster image format.");
        }
    }

    /// <summary>
    /// Gets all recognized file extensions for the raster image format.
    /// </summary>
    /// <param name="format">The raster image format.</param>
    /// <returns>Lowercase file extensions, including leading dots, with the conventional extension first.</returns>
    public static string[] GetFileExtensions(this RasterImageFormat format) {
        switch (format) {
            case RasterImageFormat.Png:
                return new[] { ".png" };
            case RasterImageFormat.Jpeg:
                return new[] { ".jpg", ".jpeg" };
            case RasterImageFormat.Bmp:
                return new[] { ".bmp" };
            case RasterImageFormat.Ppm:
                return new[] { ".ppm" };
            case RasterImageFormat.Tiff:
                return new[] { ".tiff", ".tif" };
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported raster image format.");
        }
    }

    /// <summary>
    /// Gets the MIME type for the raster image format.
    /// </summary>
    /// <param name="format">The raster image format.</param>
    /// <returns>The MIME content type.</returns>
    public static string GetMimeType(this RasterImageFormat format) {
        switch (format) {
            case RasterImageFormat.Png:
                return "image/png";
            case RasterImageFormat.Jpeg:
                return "image/jpeg";
            case RasterImageFormat.Bmp:
                return "image/bmp";
            case RasterImageFormat.Ppm:
                return "image/x-portable-pixmap";
            case RasterImageFormat.Tiff:
                return "image/tiff";
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported raster image format.");
        }
    }

    private static string NormalizeExtension(string pathOrExtension) {
        var value = pathOrExtension.Trim();
        if (value.Length == 0) throw new ArgumentException("Raster image file extension cannot be empty.", nameof(pathOrExtension));
        var hasDirectorySeparator = value.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0;
        string extension;
        if (!hasDirectorySeparator && value.IndexOf('.') < 0) {
            extension = "." + value;
        } else if (!hasDirectorySeparator && value[0] == '.' && value.IndexOf('.', 1) < 0) {
            extension = value;
        } else {
            extension = Path.GetExtension(value);
        }

        if (string.IsNullOrWhiteSpace(extension)) throw new ArgumentException("Raster image file extension cannot be empty.", nameof(pathOrExtension));
        return extension.ToLowerInvariant();
    }
}
