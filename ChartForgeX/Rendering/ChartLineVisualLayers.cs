using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal readonly struct ChartLineVisualLayer {
    public ChartLineVisualLayer(string roleSuffix, ChartColor color, double strokeWidth, double opacity) {
        RoleSuffix = roleSuffix ?? throw new ArgumentNullException(nameof(roleSuffix));
        Color = color;
        StrokeWidth = strokeWidth;
        Opacity = opacity;
    }

    public string RoleSuffix { get; }
    public ChartColor Color { get; }
    public double StrokeWidth { get; }
    public double Opacity { get; }
    public bool IsForeground => RoleSuffix.Length == 0;

    /// <summary>Gets whether this is the white sheen drawn over the line: a derived colour that never takes a token property.</summary>
    public bool IsHighlight => RoleSuffix == HighlightSuffix;

    public const string HighlightSuffix = "-highlight";
    public bool IsVisible => StrokeWidth > 0 && Opacity > 0 && (IsForeground || Color.A > 0);

    public ChartColor ColorWithOpacity() {
        if (Color.A == 0 || Opacity >= 1) return Color;
        var alpha = Math.Min(Color.A, Math.Max(0, (int)Math.Round(Color.A * Opacity)));
        return ChartColor.FromRgba(Color.R, Color.G, Color.B, (byte)alpha);
    }
}

internal static class ChartLineVisualLayers {
    public static IReadOnlyList<ChartLineVisualLayer> Build(ChartColor color, double strokeWidth, ChartLineVisualStyle style, double defaultUnits = 1) {
        if (style == null) throw new ArgumentNullException(nameof(style));
        if (double.IsNaN(defaultUnits) || double.IsInfinity(defaultUnits) || defaultUnits <= 0) throw new ArgumentOutOfRangeException(nameof(defaultUnits));
        // A prepared default-unit basis replays the same logical layer arithmetic before one projection.
        // Unit1 keeps every incumbent operation and primitive order unchanged.
        var foregroundWidth = strokeWidth;
        if (defaultUnits != 1) strokeWidth /= defaultUnits;
        var layers = new List<ChartLineVisualLayer>(4);
        if (style.AmbientHaloOpacity > 0 && style.AmbientHaloStrokeExtra > 0) {
            layers.Add(new ChartLineVisualLayer("-ambient-halo", color, (strokeWidth + style.AmbientHaloStrokeExtra) * defaultUnits, style.AmbientHaloOpacity));
        }

        if (style.HaloOpacity > 0 && style.HaloStrokeExtra > 0) {
            layers.Add(new ChartLineVisualLayer("-halo", color, (strokeWidth + style.HaloStrokeExtra) * defaultUnits, style.HaloOpacity));
        }

        layers.Add(new ChartLineVisualLayer(string.Empty, color, foregroundWidth, 1));
        var highlightOpacity = HighlightOpacity(color, style);
        if (highlightOpacity > 0) {
            layers.Add(new ChartLineVisualLayer(ChartLineVisualLayer.HighlightSuffix, ChartColor.White, Math.Max(1.0, strokeWidth * style.HighlightStrokeRatio) * defaultUnits, highlightOpacity));
        }

        return layers;
    }

    public static double HighlightOpacity(ChartColor color, ChartLineVisualStyle style) =>
        color.A == 0 ? 0 : style.HighlightOpacity * (color.A / 255.0);
}
