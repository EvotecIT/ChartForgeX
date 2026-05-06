using System;
using System.Globalization;

namespace ChartForgeX.Svg;

internal readonly struct SvgViewBox {
    public SvgViewBox(double minX, double minY, double width, double height) {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "SVG viewBox width cannot be negative.");
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "SVG viewBox height cannot be negative.");
        SvgMarkupWriter.FormatNumber(minX);
        SvgMarkupWriter.FormatNumber(minY);
        SvgMarkupWriter.FormatNumber(width);
        SvgMarkupWriter.FormatNumber(height);

        MinX = minX;
        MinY = minY;
        Width = width;
        Height = height;
    }

    public double MinX { get; }

    public double MinY { get; }

    public double Width { get; }

    public double Height { get; }

    public static SvgViewBox Parse(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));

        var parts = value.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) throw new FormatException("SVG viewBox must contain exactly four numbers.");

        return new SvgViewBox(
            ParseNumber(parts[0]),
            ParseNumber(parts[1]),
            ParseNumber(parts[2]),
            ParseNumber(parts[3]));
    }

    public SvgViewBox WithMin(double minX, double minY) =>
        new(minX, minY, Width, Height);

    public SvgViewBox WithSize(double width, double height) =>
        new(MinX, MinY, width, height);

    public SvgViewBox Expand(double padding) =>
        new(MinX - padding, MinY - padding, Width + padding * 2, Height + padding * 2);

    public string ToMarkup() =>
        SvgMarkupWriter.FormatNumber(MinX) + " " +
        SvgMarkupWriter.FormatNumber(MinY) + " " +
        SvgMarkupWriter.FormatNumber(Width) + " " +
        SvgMarkupWriter.FormatNumber(Height);

    public override string ToString() => ToMarkup();

    private static double ParseNumber(string value) {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) {
            throw new FormatException("SVG viewBox contains an invalid number.");
        }

        return number;
    }
}
