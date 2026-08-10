using System;
using System.Collections.Generic;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Defines artifact-wide static rendering options shared by SVG, HTML, PNG, and host adapters.</summary>
public sealed class VisualArtifactRenderOptions {
    /// <summary>Gets the visual watermarks drawn over the rendered artifact in declaration order.</summary>
    public List<VisualWatermark> Watermarks { get; } = new();

    /// <summary>Gets or sets optional topology-specific rendering options when the artifact contains a topology model.</summary>
    public TopologyRenderOptions? Topology { get; set; }

    /// <summary>Gets or sets dependency-free raster encoding options.</summary>
    public RasterImageOptions? Raster { get; set; }
}

/// <summary>Identifies the content carried by a visual watermark.</summary>
public enum VisualWatermarkKind {
    /// <summary>A text watermark.</summary>
    Text,
    /// <summary>An embedded raster image watermark.</summary>
    Image
}

/// <summary>Defines a deterministic text or image watermark for static visual artifact exports.</summary>
public sealed class VisualWatermark {
    private string? _text;
    private byte[]? _imageBytes;
    private string? _imageMimeType;
    private double _opacity = 0.18;
    private double _rotationDegrees;
    private double _scale = 1;
    private double _padding = 24;
    private double _fontSize = 28;
    private double _offsetX;
    private double _offsetY;
    private double? _width;
    private double? _height;
    private double _repeatSpacingX = 180;
    private double _repeatSpacingY = 120;

    private VisualWatermark(VisualWatermarkKind kind) {
        Kind = kind;
    }

    /// <summary>Gets the watermark content kind.</summary>
    public VisualWatermarkKind Kind { get; }

    /// <summary>Gets the text content for a text watermark.</summary>
    public string? Text => _text;

    /// <summary>Gets a defensive copy of the raster image bytes for an image watermark.</summary>
    public byte[]? ImageBytes => _imageBytes == null ? null : (byte[])_imageBytes.Clone();

    /// <summary>Gets the image media type for an image watermark.</summary>
    public string? ImageMimeType => _imageMimeType;

    /// <summary>Gets or sets the watermark anchor.</summary>
    public VisualCanvasAnchor Anchor { get; set; } = VisualCanvasAnchor.BottomRight;

    /// <summary>Gets or sets the horizontal offset from the anchor.</summary>
    public double OffsetX {
        get => _offsetX;
        set { ValidateFinite(value, nameof(value)); _offsetX = value; }
    }

    /// <summary>Gets or sets the vertical offset from the anchor.</summary>
    public double OffsetY {
        get => _offsetY;
        set { ValidateFinite(value, nameof(value)); _offsetY = value; }
    }

    /// <summary>Gets or sets the inset from anchored canvas edges.</summary>
    public double Padding {
        get => _padding;
        set { ValidateNonNegative(value, nameof(value)); _padding = value; }
    }

    /// <summary>Gets or sets watermark opacity from zero to one.</summary>
    public double Opacity {
        get => _opacity;
        set {
            ValidateFinite(value, nameof(value));
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Opacity must be between zero and one.");
            _opacity = value;
        }
    }

    /// <summary>Gets or sets clockwise rotation in degrees.</summary>
    public double RotationDegrees {
        get => _rotationDegrees;
        set { ValidateFinite(value, nameof(value)); _rotationDegrees = value; }
    }

    /// <summary>Gets or sets a positive scale applied to text size or image dimensions.</summary>
    public double Scale {
        get => _scale;
        set { ValidatePositive(value, nameof(value)); _scale = value; }
    }

    /// <summary>Gets or sets the text color.</summary>
    public ChartColor Color { get; set; } = ChartColor.FromHex("#64748B");

    /// <summary>Gets or sets the base text size in pixels.</summary>
    public double FontSize {
        get => _fontSize;
        set { ValidatePositive(value, nameof(value)); _fontSize = value; }
    }

    /// <summary>Gets or sets whether text uses an emphasized weight.</summary>
    public bool Emphasized { get; set; } = true;

    /// <summary>Gets or sets an optional image width in pixels before <see cref="Scale"/> is applied.</summary>
    public double? Width {
        get => _width;
        set { if (value.HasValue) ValidatePositive(value.Value, nameof(value)); _width = value; }
    }

    /// <summary>Gets or sets an optional image height in pixels before <see cref="Scale"/> is applied.</summary>
    public double? Height {
        get => _height;
        set { if (value.HasValue) ValidatePositive(value.Value, nameof(value)); _height = value; }
    }

    /// <summary>Gets or sets whether the watermark repeats across the canvas.</summary>
    public bool Repeat { get; set; }

    /// <summary>Gets or sets horizontal spacing between repeated watermark anchors.</summary>
    public double RepeatSpacingX {
        get => _repeatSpacingX;
        set { ValidateRepeatSpacing(value, nameof(value)); _repeatSpacingX = value; }
    }

    /// <summary>Gets or sets vertical spacing between repeated watermark anchors.</summary>
    public double RepeatSpacingY {
        get => _repeatSpacingY;
        set { ValidateRepeatSpacing(value, nameof(value)); _repeatSpacingY = value; }
    }

    /// <summary>Creates a text watermark.</summary>
    public static VisualWatermark FromText(string text) {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Watermark text cannot be empty.", nameof(text));
        return new VisualWatermark(VisualWatermarkKind.Text) { _text = text.Trim() };
    }

    /// <summary>Creates an embedded raster image watermark.</summary>
    public static VisualWatermark FromImage(byte[] imageBytes, string imageMimeType) {
        if (imageBytes == null) throw new ArgumentNullException(nameof(imageBytes));
        if (imageBytes.Length == 0) throw new ArgumentException("Watermark image bytes cannot be empty.", nameof(imageBytes));
        string normalizedMimeType = NormalizeImageMimeType(imageMimeType);
        if (!RasterImageDecoder.TryDecode(imageBytes, out _)) throw new ArgumentException("Watermark image bytes must use a supported dependency-free raster format.", nameof(imageBytes));
        string detectedMimeType = RasterImageDecoder.MimeTypeFor(imageBytes);
        if (!string.Equals(normalizedMimeType, detectedMimeType, StringComparison.Ordinal)) throw new ArgumentException("Watermark image media type does not match the supplied image bytes.", nameof(imageMimeType));
        return new VisualWatermark(VisualWatermarkKind.Image) {
            _imageBytes = (byte[])imageBytes.Clone(),
            _imageMimeType = normalizedMimeType
        };
    }

    private static string NormalizeImageMimeType(string imageMimeType) {
        if (string.IsNullOrWhiteSpace(imageMimeType)) throw new ArgumentException("Watermark image media type cannot be empty.", nameof(imageMimeType));
        switch (imageMimeType.Trim().ToLowerInvariant()) {
            case "image/png": return "image/png";
            case "image/jpeg":
            case "image/jpg": return "image/jpeg";
            case "image/gif": return "image/gif";
            case "image/bmp":
            case "image/x-ms-bmp": return "image/bmp";
            case "image/x-portable-pixmap": return "image/x-portable-pixmap";
            case "image/tiff": return "image/tiff";
            default: throw new ArgumentException("Watermark image media type must identify a supported JPEG, PNG, GIF, BMP, PPM, or TIFF image.", nameof(imageMimeType));
        }
    }

    private static void ValidateFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
    }

    private static void ValidatePositive(double value, string parameterName) {
        ValidateFinite(value, parameterName);
        if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
    }

    private static void ValidateRepeatSpacing(double value, string parameterName) {
        ValidateFinite(value, parameterName);
        if (value < 1) throw new ArgumentOutOfRangeException(parameterName, value, "Repeat spacing must be at least one pixel.");
    }

    private static void ValidateNonNegative(double value, string parameterName) {
        ValidateFinite(value, parameterName);
        if (value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.");
    }
}
