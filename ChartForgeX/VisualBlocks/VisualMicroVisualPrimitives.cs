using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

internal readonly struct VisualMiniBar {
    public VisualMiniBar(int index, double value, double x, double y, double width, double height, double radius, ChartColor color, bool highlighted) {
        Index = index;
        Value = value;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Radius = radius;
        Color = color;
        Highlighted = highlighted;
    }

    public int Index { get; }

    public double Value { get; }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    public double Radius { get; }

    public ChartColor Color { get; }

    public bool Highlighted { get; }
}

internal readonly struct VisualMiniSparkline {
    public VisualMiniSparkline(ChartPoint[] points, ChartPoint[] area, ChartColor lineColor, ChartColor fillColor, double strokeWidth, double currentRadius) {
        Points = points;
        Area = area;
        LineColor = lineColor;
        FillColor = fillColor;
        StrokeWidth = strokeWidth;
        CurrentRadius = currentRadius;
    }

    public ChartPoint[] Points { get; }

    public ChartPoint[] Area { get; }

    public ChartColor LineColor { get; }

    public ChartColor FillColor { get; }

    public double StrokeWidth { get; }

    public double CurrentRadius { get; }

    public ChartPoint Current => Points[Points.Length - 1];
}
