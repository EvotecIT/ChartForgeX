using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.SvgRaster;

internal sealed class SvgRasterStyle {
    public static SvgRasterStyle Default => new() {
        Fill = SvgRasterPaint.Solid(ChartColor.Black),
        Stroke = SvgRasterPaint.None,
        Color = ChartColor.Black,
        Opacity = 1,
        FillOpacity = 1,
        StrokeOpacity = 1,
        StrokeWidth = 1,
        StrokeLineCap = "butt",
        StrokeLineJoin = "miter",
        FillRule = "nonzero",
        ClipRule = "nonzero",
        FontSize = 16,
        FontWeight = "normal",
        TextAnchor = "start",
        DominantBaseline = "auto",
        Visible = true
    };

    public SvgRasterPaint Fill { get; set; }
    public SvgRasterPaint Stroke { get; set; }
    public ChartColor Color { get; set; }
    public double Opacity { get; set; }
    public double FillOpacity { get; set; }
    public double StrokeOpacity { get; set; }
    public double StrokeWidth { get; set; }
    public string StrokeLineCap { get; set; } = "butt";
    public string StrokeLineJoin { get; set; } = "miter";
    public IReadOnlyList<double>? StrokeDashArray { get; set; }
    public string FillRule { get; set; } = "nonzero";
    public string ClipRule { get; set; } = "nonzero";
    public double FontSize { get; set; }
    public string FontWeight { get; set; } = "normal";
    public string TextAnchor { get; set; } = "start";
    public string DominantBaseline { get; set; } = "auto";
    public bool Visible { get; set; }

    public SvgRasterStyle Inherit() =>
        new() {
            Fill = Fill,
            Stroke = Stroke,
            Color = Color,
            Opacity = Opacity,
            FillOpacity = FillOpacity,
            StrokeOpacity = StrokeOpacity,
            StrokeWidth = StrokeWidth,
            StrokeLineCap = StrokeLineCap,
            StrokeLineJoin = StrokeLineJoin,
            StrokeDashArray = StrokeDashArray,
            FillRule = FillRule,
            ClipRule = ClipRule,
            FontSize = FontSize,
            FontWeight = FontWeight,
            TextAnchor = TextAnchor,
            DominantBaseline = DominantBaseline,
            Visible = Visible
        };

    public static SvgRasterStyle Resolve(SvgRasterStyle parent, SvgRasterElement element, SvgRasterStyleSheet? styleSheet = null, IReadOnlyList<SvgRasterElement>? ancestors = null) {
        var style = parent.Inherit();
        ApplyPresentation(style, element);
        if (styleSheet != null) {
            foreach (var declaration in styleSheet.DeclarationsFor(element, ancestors)) Apply(style, declaration.Name, declaration.Value);
        }

        var inline = element.Get("style");
        if (!string.IsNullOrWhiteSpace(inline)) {
            foreach (var declaration in SvgStyleDeclarationList.Parse(inline!).Declarations) {
                Apply(style, declaration.Name, declaration.Value);
            }
        }

        return style;
    }

    public ChartColor FillColor() => WithOpacity(Fill.Color ?? ChartColor.Transparent, Opacity * FillOpacity);

    public ChartColor StrokeColor() => WithOpacity(Stroke.Color ?? ChartColor.Transparent, Opacity * StrokeOpacity);

    private static void ApplyPresentation(SvgRasterStyle style, SvgRasterElement element) {
        ApplyAttribute(style, element, "color");
        ApplyAttribute(style, element, "fill");
        ApplyAttribute(style, element, "fill-rule");
        ApplyAttribute(style, element, "clip-rule");
        ApplyAttribute(style, element, "stroke");
        ApplyAttribute(style, element, "stroke-width");
        ApplyAttribute(style, element, "stroke-linecap");
        ApplyAttribute(style, element, "stroke-linejoin");
        ApplyAttribute(style, element, "stroke-dasharray");
        ApplyAttribute(style, element, "opacity");
        ApplyAttribute(style, element, "fill-opacity");
        ApplyAttribute(style, element, "stroke-opacity");
        ApplyAttribute(style, element, "font-size");
        ApplyAttribute(style, element, "font-weight");
        ApplyAttribute(style, element, "text-anchor");
        ApplyAttribute(style, element, "dominant-baseline");
        ApplyAttribute(style, element, "alignment-baseline");
        ApplyAttribute(style, element, "display");
        ApplyAttribute(style, element, "visibility");
    }

    private static void ApplyAttribute(SvgRasterStyle style, SvgRasterElement element, string name) {
        if (element.TryGet(name, out var value)) Apply(style, name, value);
    }

    private static void Apply(SvgRasterStyle style, string name, string value) {
        if (string.IsNullOrWhiteSpace(value)) return;
        switch (name) {
            case "color":
                var colorPaint = SvgRasterPaint.Parse(value, style.Color, SvgRasterPaint.Solid(style.Color));
                style.Color = colorPaint.Color ?? style.Color;
                break;
            case "fill":
                style.Fill = SvgRasterPaint.Parse(value, style.Color, style.Fill);
                break;
            case "fill-rule":
                style.FillRule = value.Trim();
                break;
            case "clip-rule":
                style.ClipRule = value.Trim();
                break;
            case "stroke":
                style.Stroke = SvgRasterPaint.Parse(value, style.Color, style.Stroke);
                break;
            case "stroke-width":
                style.StrokeWidth = Math.Max(0, ParseLength(value, style.StrokeWidth));
                break;
            case "stroke-linecap":
                style.StrokeLineCap = value.Trim();
                break;
            case "stroke-linejoin":
                style.StrokeLineJoin = value.Trim();
                break;
            case "stroke-dasharray":
                style.StrokeDashArray = ParseDashArray(value);
                break;
            case "opacity":
                style.Opacity = ParseOpacity(value, style.Opacity);
                break;
            case "fill-opacity":
                style.FillOpacity = ParseOpacity(value, style.FillOpacity);
                break;
            case "stroke-opacity":
                style.StrokeOpacity = ParseOpacity(value, style.StrokeOpacity);
                break;
            case "font-size":
                style.FontSize = Math.Max(1, ParseLength(value, style.FontSize));
                break;
            case "font-weight":
                style.FontWeight = value.Trim();
                break;
            case "text-anchor":
                style.TextAnchor = value.Trim();
                break;
            case "dominant-baseline":
            case "alignment-baseline":
                style.DominantBaseline = value.Trim();
                break;
            case "display":
                if (string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase)) style.Visible = false;
                break;
            case "visibility":
                if (string.Equals(value.Trim(), "hidden", StringComparison.OrdinalIgnoreCase) || string.Equals(value.Trim(), "collapse", StringComparison.OrdinalIgnoreCase)) style.Visible = false;
                break;
        }
    }

    private static double ParseOpacity(string value, double fallback) {
        var trimmed = value.Trim();
        var percent = trimmed.EndsWith("%", StringComparison.Ordinal);
        if (percent) trimmed = trimmed.Substring(0, trimmed.Length - 1);
        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity)) return fallback;
        if (percent) opacity /= 100.0;
        return Math.Max(0, Math.Min(1, opacity));
    }

    private static double ParseLength(string value, double fallback) {
        var trimmed = value.Trim();
        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase)) trimmed = trimmed.Substring(0, trimmed.Length - 2);
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return fallback;
    }

    private static IReadOnlyList<double>? ParseDashArray(string value) {
        if (string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase)) return null;
        var values = SvgRasterNumbers.ParseList(value).Where(item => item > 0).ToArray();
        return values.Length == 0 ? null : values;
    }

    private static ChartColor WithOpacity(ChartColor color, double opacity) {
        opacity = Math.Max(0, Math.Min(1, opacity));
        return ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity));
    }
}
