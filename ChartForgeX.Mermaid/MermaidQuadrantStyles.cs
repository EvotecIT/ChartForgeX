using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

/// <summary>Supplies Quadrant's pinned four-property policy and cascade to the existing style/native marker owners.</summary>
internal static class MermaidQuadrantStyles {
    internal static bool TryDefinition(MermaidQuadrantDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!MermaidParserUtilities.StartsStatement(text, "classDef")) return false;
        if (!MermaidStyleParser.SplitHead(text.Substring(8).Trim(), out var name, out var declarations)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Quadrant classDef requires one name and declarations.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        var definition = new MermaidStyleDefinition(name, span);
        Read(definition.Styles, declarations, span, result);
        MermaidResourceGuard.ReserveElements(checked(1L + definition.Styles.Count), span);
        document.ClassDefinitions.Add(definition);
        return true;
    }

    internal static void Read(Dictionary<string, string> styles, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) =>
        MermaidStyleParser.Read(styles, text, span, result, "Quadrant", accepts: (property, value) => {
            if (!Known(property)) return false;
            if (!TryValue(property, value, out _, out _)) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "Quadrant style '" + property + "' must use a pinned solid hex color or a whole native radius/stroke width from 0 through 1024.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }, preservePropertyCase: true);

    internal static Dictionary<string, Dictionary<string, string>> Definitions(MermaidQuadrantDocument document) {
        var definitions = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        // Pinned Quadrant uses Map.set: a later classDef replaces its whole earlier declaration object.
        foreach (var definition in document.ClassDefinitions) {
            MermaidResourceGuard.Checkpoint();
            foreach (var style in definition.Styles)
                if (Known(style.Key) && !TryValue(style.Key, style.Value, out _, out _))
                    throw new ArgumentException("Quadrant style '" + style.Key + "' must use a pinned solid hex color or a whole native radius/stroke width from 0 through 1024.");
            definitions[definition.Name] = definition.Styles;
        }
        return definitions;
    }

    internal static Dictionary<string, string> Resolve(Dictionary<string, Dictionary<string, string>> definitions, MermaidQuadrantPoint point) =>
        MermaidStyleParser.Resolve(definitions, point.ClassName == null ? Array.Empty<string>() : new[] { point.ClassName },
            point.Styles, point.Span, includeDefault: false);

    internal static void Apply(ChartSeries series, int index, Dictionary<string, string> styles, ChartColor strokeDefault) {
        if (!System.Linq.Enumerable.Any(styles.Keys, Known)) return;
        series.ConfigurePointMarkers(index, marker => {
            // Pinned Quadrant defaults to no outline; its default stroke ink is the theme point fill.
            marker.Stroke = strokeDefault; marker.StrokeWidth = 0;
            foreach (var declaration in styles) {
                if (!Known(declaration.Key)) continue;
                if (!TryValue(declaration.Key, declaration.Value, out var color, out var number))
                    throw new ArgumentException("Quadrant style '" + declaration.Key + "' must use a pinned solid hex color or a whole native radius/stroke width from 0 through 1024.");
                if (declaration.Key == "color") marker.Fill = color;
                else if (declaration.Key == "stroke-color") marker.Stroke = color;
                else if (declaration.Key == "radius") marker.Radius = number;
                else marker.StrokeWidth = number;
            }
        });
    }

    internal static bool IsInlineClassName(string name) => Regex.IsMatch(name, @"^[A-Za-z0-9_]+$", RegexOptions.CultureInvariant);

    private static bool Known(string property) => property is "radius" or "color" or "stroke-color" or "stroke-width";
    private static bool TryValue(string property, string value, out ChartColor color, out double number) {
        color = default; number = 0;
        if (property is "color" or "stroke-color") return Regex.IsMatch(value, @"^#?(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.CultureInvariant)
            && ChartColor.TryParse(value[0] == '#' ? value : "#" + value, out color);
        var digits = property == "stroke-width" && value.EndsWith("px", StringComparison.Ordinal) ? value.Substring(0, value.Length - 2) : value;
        if (property == "stroke-width" && digits == value) return false;
        return digits.Length > 0 && Regex.IsMatch(digits, @"^[0-9]+$", RegexOptions.CultureInvariant)
            && double.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number <= 1024;
    }
}
