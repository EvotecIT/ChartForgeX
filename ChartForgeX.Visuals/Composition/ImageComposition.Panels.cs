using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Composition;

public sealed partial class ImageComposition {
    /// <summary>Creates a panel with an optional rounded fill and an outline painted inside its pixel bounds.</summary>
    /// <remarks>The corner radius is limited to half the shorter side. Transparent corners retain alpha;
    /// no shadow, padding, header, theme or host-specific placement is implied.</remarks>
    public static ImageComposition CreatePanel(int width, int height, ChartColor background,
        ChartColor? border = null, double cornerRadius = 0, double borderThickness = 1) {
        ValidateNonNegative(cornerRadius, nameof(cornerRadius));
        ValidatePositive(borderThickness, nameof(borderThickness));
        var radius = Math.Min(cornerRadius, Math.Min(width, height) / 2d);
        if (radius <= 0 && !border.HasValue) return Create(width, height, background);
        var panel = CreateTransparent(width, height);
        if (background.A > 0) panel.FillRoundedRectangle(0, 0, width, height, radius, background);
        if (border is { A: > 0 } stroke) panel.StrokeRoundedRectangle(0, 0, width, height, radius, stroke, borderThickness);
        return panel;
    }
}
