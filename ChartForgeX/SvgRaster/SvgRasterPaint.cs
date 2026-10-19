using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.SvgRaster;

internal readonly struct SvgRasterPaint {
    private SvgRasterPaint(ChartColor? color, string? referenceId, bool none, bool currentColor = false) {
        Color = color;
        ReferenceId = referenceId;
        IsNone = none;
        IsCurrentColor = currentColor;
    }

    public ChartColor? Color { get; }
    public string? ReferenceId { get; }
    public bool IsNone { get; }
    public bool IsReference => !string.IsNullOrWhiteSpace(ReferenceId);

    private bool IsCurrentColor { get; }

    public static SvgRasterPaint None => new(null, null, true);
    public static SvgRasterPaint Solid(ChartColor color) => new(color, null, false);
    public static SvgRasterPaint Reference(string id) => new(null, id, false);

    /// <summary>Resolves used color after the element's cascade while retaining the inherited keyword.</summary>
    public SvgRasterPaint ResolveCurrentColor(ChartColor color) => IsCurrentColor ? new(color, null, false, true) : this;

    public static SvgRasterPaint Parse(string value, ChartColor currentColor, SvgRasterPaint fallback) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var trimmed = value.Trim();
        if (string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase)) return None;
        if (string.Equals(trimmed, "currentColor", StringComparison.OrdinalIgnoreCase)) return new(currentColor, null, false, true);
        var reference = TryParseReference(trimmed);
        if (reference != null) return Reference(reference);
        return SvgRasterColor.TryParse(trimmed, out var color) ? Solid(color) : fallback;
    }

    private static string? TryParseReference(string value) {
        if (!value.StartsWith("url(", StringComparison.OrdinalIgnoreCase)) return null;
        var close = value.IndexOf(')');
        if (close < 0) return null;
        var body = value.Substring(4, close - 4).Trim().Trim('\'', '"');
        return body.StartsWith("#", StringComparison.Ordinal) && body.Length > 1 ? body.Substring(1) : null;
    }
}
