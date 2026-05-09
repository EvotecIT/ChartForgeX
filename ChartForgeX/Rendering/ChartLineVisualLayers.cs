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
    public bool IsVisible => StrokeWidth > 0 && Opacity > 0 && (IsForeground || Color.A > 0);

    public ChartColor ColorWithOpacity() {
        if (Color.A == 0 || Opacity >= 1) return Color;
        var alpha = Math.Min(Color.A, Math.Max(0, (int)Math.Round(Color.A * Opacity)));
        return ChartColor.FromRgba(Color.R, Color.G, Color.B, (byte)alpha);
    }
}

internal static class ChartLineVisualLayers {
    public static IReadOnlyList<ChartLineVisualLayer> Build(ChartColor color, double strokeWidth, ChartLineVisualStyle style) {
        if (style == null) throw new ArgumentNullException(nameof(style));
        var layers = new List<ChartLineVisualLayer>(4);
        if (style.AmbientHaloOpacity > 0 && style.AmbientHaloStrokeExtra > 0) {
            layers.Add(new ChartLineVisualLayer("-ambient-halo", color, strokeWidth + style.AmbientHaloStrokeExtra, style.AmbientHaloOpacity));
        }

        if (style.HaloOpacity > 0 && style.HaloStrokeExtra > 0) {
            layers.Add(new ChartLineVisualLayer("-halo", color, strokeWidth + style.HaloStrokeExtra, style.HaloOpacity));
        }

        layers.Add(new ChartLineVisualLayer(string.Empty, color, strokeWidth, 1));
        var highlightOpacity = HighlightOpacity(color, style);
        if (highlightOpacity > 0) {
            layers.Add(new ChartLineVisualLayer("-highlight", ChartColor.White, Math.Max(1.0, strokeWidth * style.HighlightStrokeRatio), highlightOpacity));
        }

        return layers;
    }

    public static double HighlightOpacity(ChartColor color, ChartLineVisualStyle style) =>
        color.A == 0 ? 0 : style.HighlightOpacity * (color.A / 255.0);
}
