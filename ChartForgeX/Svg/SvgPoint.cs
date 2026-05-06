namespace ChartForgeX.Svg;

internal readonly struct SvgPoint {
    public SvgPoint(double x, double y) {
        SvgMarkupWriter.FormatNumber(x);
        SvgMarkupWriter.FormatNumber(y);
        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }

    public string ToMarkup() =>
        SvgMarkupWriter.FormatNumber(X) + " " + SvgMarkupWriter.FormatNumber(Y);

    public override string ToString() => ToMarkup();
}
