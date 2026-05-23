namespace ChartForgeX.Rendering;

internal static class ChartActionGlyphGeometry {
    public static ChartChevronGlyph RightChevron(double centerX, double centerY, double size) => new(
        centerX - size * 0.25,
        centerY - size * 0.36,
        centerX + size * 0.25,
        centerY,
        centerX - size * 0.25,
        centerY + size * 0.36);
}

internal readonly struct ChartChevronGlyph {
    public ChartChevronGlyph(double x1, double y1, double x2, double y2, double x3, double y3) {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        X3 = x3;
        Y3 = y3;
    }

    public double X1 { get; }

    public double Y1 { get; }

    public double X2 { get; }

    public double Y2 { get; }

    public double X3 { get; }

    public double Y3 { get; }
}
