namespace ChartForgeX.Rendering;

internal readonly struct ChartTextHaloLayer {
    public ChartTextHaloLayer(double dx, double dy, double opacity) {
        Dx = dx;
        Dy = dy;
        Opacity = opacity;
    }

    public double Dx { get; }
    public double Dy { get; }
    public double Opacity { get; }
}

internal static class ChartTextHalo {
    public static ChartTextHaloLayer[] ReadableRasterLayers(double fontSize) {
        var inner = fontSize >= 12 ? 1.15 : 0.85;
        var outer = fontSize >= 12 ? 1.75 : 1.25;
        return new[] {
            new ChartTextHaloLayer(-outer, 0, ChartVisualPrimitives.PngTextHaloOuterOpacity),
            new ChartTextHaloLayer(outer, 0, ChartVisualPrimitives.PngTextHaloOuterOpacity),
            new ChartTextHaloLayer(0, -outer, ChartVisualPrimitives.PngTextHaloOuterOpacity),
            new ChartTextHaloLayer(0, outer, ChartVisualPrimitives.PngTextHaloOuterOpacity),
            new ChartTextHaloLayer(-inner, 0, ChartVisualPrimitives.PngTextHaloStrongOpacity),
            new ChartTextHaloLayer(inner, 0, ChartVisualPrimitives.PngTextHaloStrongOpacity),
            new ChartTextHaloLayer(0, -inner, ChartVisualPrimitives.PngTextHaloStrongOpacity),
            new ChartTextHaloLayer(0, inner, ChartVisualPrimitives.PngTextHaloStrongOpacity),
            new ChartTextHaloLayer(-inner, -inner, ChartVisualPrimitives.PngTextHaloSoftOpacity),
            new ChartTextHaloLayer(inner, -inner, ChartVisualPrimitives.PngTextHaloSoftOpacity),
            new ChartTextHaloLayer(-inner, inner, ChartVisualPrimitives.PngTextHaloSoftOpacity),
            new ChartTextHaloLayer(inner, inner, ChartVisualPrimitives.PngTextHaloSoftOpacity)
        };
    }

    public static ChartTextHaloLayer[] CompactRasterLayers(double fontSize) {
        var axis = fontSize >= 12 ? 1.45 : 1.08;
        var diagonal = fontSize >= 12 ? 1.02 : 0.76;
        return new[] {
            new ChartTextHaloLayer(-axis, 0, 0.82),
            new ChartTextHaloLayer(axis, 0, 0.82),
            new ChartTextHaloLayer(0, -axis, 0.82),
            new ChartTextHaloLayer(0, axis, 0.82),
            new ChartTextHaloLayer(-diagonal, -diagonal, 0.54),
            new ChartTextHaloLayer(diagonal, -diagonal, 0.54),
            new ChartTextHaloLayer(-diagonal, diagonal, 0.54),
            new ChartTextHaloLayer(diagonal, diagonal, 0.54)
        };
    }

    public static double SvgStrokeWidth(double fontSize, bool emphasized) =>
        fontSize >= 14 ? emphasized ? 2.0 : 1.6 : emphasized ? 1.7 : 1.35;
}
