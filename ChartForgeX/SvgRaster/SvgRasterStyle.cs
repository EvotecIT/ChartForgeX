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
        StrokeMiterLimit = 4,
        FillRule = "nonzero",
        ClipRule = "nonzero",
        FontSize = 16,
        FontWeight = "normal",
        TextAnchor = "start",
        DominantBaseline = "auto",
        WhiteSpace = "normal",
        MaskType = "luminance",
        Overflow = null,
        Displayed = true,
        AncestorsDisplayed = true,
        VisibilityVisible = true
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
    public double StrokeMiterLimit { get; set; } = 4;
    public IReadOnlyList<double>? StrokeDashArray { get; set; }
    public string FillRule { get; set; } = "nonzero";
    public string ClipRule { get; set; } = "nonzero";
    public string? ClipPath { get; set; }
    public double FontSize { get; set; }
    public string FontWeight { get; set; } = "normal";
    public string TextAnchor { get; set; } = "start";
    public string DominantBaseline { get; set; } = "auto";
    public string WhiteSpace { get; set; } = "normal";
    public string MaskType { get; set; } = "luminance";
    public string? Overflow { get; set; }
    public bool Displayed { get; private set; }
    public bool VisibilityVisible { get; private set; }
    private bool AncestorsDisplayed { get; set; }
    public Dictionary<string, string> CustomProperties { get; } = new(StringComparer.Ordinal);

    public SvgRasterStyle Inherit() {
        var style = new SvgRasterStyle {
            Fill = Fill,
            Stroke = Stroke,
            Color = Color,
            // SVG opacity applies to the element as a composited layer; unlike fill/stroke
            // opacity it is not inherited by descendants.
            Opacity = 1,
            FillOpacity = FillOpacity,
            StrokeOpacity = StrokeOpacity,
            StrokeWidth = StrokeWidth,
            StrokeLineCap = StrokeLineCap,
            StrokeLineJoin = StrokeLineJoin,
            StrokeMiterLimit = StrokeMiterLimit,
            StrokeDashArray = StrokeDashArray,
            FillRule = FillRule,
            ClipRule = ClipRule,
            FontSize = FontSize,
            FontWeight = FontWeight,
            TextAnchor = TextAnchor,
            DominantBaseline = DominantBaseline,
            WhiteSpace = WhiteSpace,
            // mask-type applies only to mask elements and is not inherited.
            MaskType = "luminance",
            // overflow applies to the current viewport element and is not inherited.
            Overflow = null,
            Displayed = Displayed,
            AncestorsDisplayed = Displayed,
            VisibilityVisible = VisibilityVisible
        };
        foreach (var property in CustomProperties) style.CustomProperties[property.Key] = property.Value;
        return style;
    }

    public static SvgRasterStyle Resolve(SvgRasterStyle parent, SvgRasterElement element, SvgRasterStyleSheet? styleSheet = null, IReadOnlyList<SvgRasterElement>? ancestors = null) {
        var style = parent.Inherit();
        var declarations = new List<SvgStyleDeclaration>();
        AddPresentation(declarations, element);
        if (styleSheet != null) {
            declarations.AddRange(styleSheet.DeclarationsFor(element, ancestors));
        }

        var inline = element.Get("style");
        if (!string.IsNullOrWhiteSpace(inline)) {
            declarations.AddRange(SvgStyleDeclarationList.Parse(inline!).Declarations);
        }

        ApplyCustomProperties(style, declarations);
        foreach (var declaration in declarations) {
            if (SvgRasterCssVariables.IsCustomProperty(declaration.Name)) continue;
            Apply(style, declaration.Name, SvgRasterCssVariables.Resolve(declaration.Value, style.CustomProperties));
        }

        return style;
    }

    public static IReadOnlyDictionary<string, string> ResolveCustomProperties(SvgRasterStyleSheet styleSheet, IReadOnlyList<SvgRasterElement> ancestors, SvgRasterElement element) {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < ancestors.Count; i++) {
            var ancestorDeclarations = new List<SvgStyleDeclaration>();
            ancestorDeclarations.AddRange(styleSheet.DeclarationsFor(ancestors[i], AncestorsBefore(ancestors, i)));
            AddInline(ancestorDeclarations, ancestors[i]);
            ApplyCustomProperties(properties, ancestorDeclarations);
        }

        var declarations = new List<SvgStyleDeclaration>();
        declarations.AddRange(styleSheet.DeclarationsFor(element, ancestors));
        AddInline(declarations, element);
        ApplyCustomProperties(properties, declarations);
        return properties;
    }

    public ChartColor FillColor() => WithOpacity(Fill.Color ?? ChartColor.Transparent, Opacity * FillOpacity);

    public ChartColor StrokeColor() => WithOpacity(Stroke.Color ?? ChartColor.Transparent, Opacity * StrokeOpacity);

    private static void AddPresentation(List<SvgStyleDeclaration> declarations, SvgRasterElement element) {
        AddAttribute(declarations, element, "color");
        AddAttribute(declarations, element, "fill");
        AddAttribute(declarations, element, "fill-rule");
        AddAttribute(declarations, element, "clip-rule");
        AddAttribute(declarations, element, "clip-path");
        AddAttribute(declarations, element, "stroke");
        AddAttribute(declarations, element, "stroke-width");
        AddAttribute(declarations, element, "stroke-linecap");
        AddAttribute(declarations, element, "stroke-linejoin");
        AddAttribute(declarations, element, "stroke-miterlimit");
        AddAttribute(declarations, element, "stroke-dasharray");
        AddAttribute(declarations, element, "opacity");
        AddAttribute(declarations, element, "fill-opacity");
        AddAttribute(declarations, element, "stroke-opacity");
        AddAttribute(declarations, element, "font-size");
        AddAttribute(declarations, element, "font-weight");
        AddAttribute(declarations, element, "text-anchor");
        AddAttribute(declarations, element, "dominant-baseline");
        AddAttribute(declarations, element, "alignment-baseline");
        if (element.TryGet("xml:space", out var xmlSpace)) declarations.Add(new SvgStyleDeclaration("white-space", string.Equals(xmlSpace.Trim(), "preserve", StringComparison.OrdinalIgnoreCase) ? "pre" : "normal"));
        AddAttribute(declarations, element, "white-space");
        AddAttribute(declarations, element, "display");
        AddAttribute(declarations, element, "visibility");
        AddAttribute(declarations, element, "mask-type");
        AddAttribute(declarations, element, "overflow");
    }

    private static void AddAttribute(List<SvgStyleDeclaration> declarations, SvgRasterElement element, string name) {
        if (element.TryGet(name, out var value)) declarations.Add(new SvgStyleDeclaration(name, value));
    }

    private static void AddInline(List<SvgStyleDeclaration> declarations, SvgRasterElement element) {
        var inline = element.Get("style");
        if (!string.IsNullOrWhiteSpace(inline)) declarations.AddRange(SvgStyleDeclarationList.Parse(inline!).Declarations);
    }

    private static IReadOnlyList<SvgRasterElement> AncestorsBefore(IReadOnlyList<SvgRasterElement> ancestors, int count) {
        if (count <= 0) return Array.Empty<SvgRasterElement>();
        var before = new SvgRasterElement[count];
        for (var i = 0; i < count; i++) before[i] = ancestors[i];
        return before;
    }

    private static void ApplyCustomProperties(SvgRasterStyle style, IEnumerable<SvgStyleDeclaration> declarations) =>
        ApplyCustomProperties(style.CustomProperties, declarations);

    private static void ApplyCustomProperties(Dictionary<string, string> properties, IEnumerable<SvgStyleDeclaration> declarations) {
        foreach (var declaration in declarations) {
            if (SvgRasterCssVariables.IsCustomProperty(declaration.Name)) properties[declaration.Name] = declaration.Value;
        }
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
            case "clip-path":
                style.ClipPath = value.Trim();
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
            case "stroke-miterlimit":
                style.StrokeMiterLimit = ParseMiterLimit(value, style.StrokeMiterLimit);
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
            case "white-space":
                style.WhiteSpace = value.Trim();
                break;
            case "display":
                style.Displayed = style.AncestorsDisplayed && !string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase);
                break;
            case "visibility":
                var visibility = value.Trim();
                if (string.Equals(visibility, "visible", StringComparison.OrdinalIgnoreCase)) style.VisibilityVisible = true;
                else if (string.Equals(visibility, "hidden", StringComparison.OrdinalIgnoreCase) || string.Equals(visibility, "collapse", StringComparison.OrdinalIgnoreCase)) style.VisibilityVisible = false;
                break;
            case "mask-type":
                var maskType = value.Trim();
                if (string.Equals(maskType, "alpha", StringComparison.OrdinalIgnoreCase) || string.Equals(maskType, "luminance", StringComparison.OrdinalIgnoreCase)) style.MaskType = maskType;
                break;
            case "overflow":
                style.Overflow = value.Trim();
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

    private static double ParseMiterLimit(string value, double fallback) {
        if (!double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return fallback;
        if (double.IsNaN(parsed) || double.IsInfinity(parsed) || parsed < 1) return fallback;
        return parsed;
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
