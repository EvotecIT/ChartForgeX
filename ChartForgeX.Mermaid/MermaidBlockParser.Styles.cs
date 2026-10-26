using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidBlockParser {
    internal static bool BlockNativeStyle(string property, string value) => property switch {
        "stroke-width" => BlockNumber(value, out var width) && width >= 0 && width <= 1024,
        "font-size" => BlockNumber(value, out var size) && size > 0,
        "letter-spacing" => ChartForgeX.Typography.TypographyCss.TryLetterSpacing(value, 1, out _),
        "font-weight" => value == "bold" || value == "normal" || int.TryParse(value, out var weight) && weight >= 100 && weight <= 900,
        "font-style" => value == "normal" || value == "italic",
        "stroke-dasharray" => BlockDash(value, out _),
        _ => false
    };
    internal static bool BlockNumber(string text, out double value) => double.TryParse(text.Trim().EndsWith("px", StringComparison.OrdinalIgnoreCase)
        ? text.Trim().Substring(0, text.Trim().Length - 2) : text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && !double.IsNaN(value) && !double.IsInfinity(value);
    internal static bool BlockDash(string text, out double[] values) {
        values = Array.Empty<double>(); if (text.Trim() == "none") return true;
        var parts = text.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        var parsed = new double[parts.Length];
        for (var i = 0; i < parts.Length; i++) if (!BlockNumber(parts[i], out parsed[i]) || parsed[i] < .25 || parsed[i] > 1024) return false;
        values = parsed; return true;
    }

    private static void ApplyStyles(MermaidBlockDocument document, Dictionary<string, MermaidBlockItem> ids,
        List<(string Text, MermaidSourceSpan Span)> statements, MermaidParseResult<MermaidDocument> result) {
        foreach (var statement in statements) {
            var isClass = StartsWithKeyword(statement.Text, "class");
            var body = statement.Text.Substring(5).Trim();
            if (!MermaidStyleParser.SplitHead(body, out var selector, out var declarations)) {
                Invalid(statement.Span, "Block style and class statements require selectors and values."); continue;
            }
            var selected = MermaidStyleParser.Names(selector);
            var styles = new Dictionary<string, string>();
            if (!isClass) MermaidStyleParser.Read(styles, declarations, statement.Span, result, "Block", nativeProperty: BlockNativeStyle);
            var classes = isClass ? MermaidStyleParser.Names(declarations) : new List<string>();
            foreach (var id in selected) {
                if (!ids.TryGetValue(id, out var item) || item.IsSpace) { Invalid(statement.Span, "Block style selector was not declared: " + id + "."); continue; }
                if (isClass) { MermaidResourceGuard.ReserveElements(classes.Count, statement.Span); item.Classes.AddRange(classes); }
                else MermaidStyleParser.Copy(item.Styles, styles, statement.Span);
            }
        }
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach (var item in document.Items) {
            foreach (var name in item.Classes) if (!definitions.ContainsKey(name)) Invalid(item.Span, "Block class was not defined: " + name + ".");
            var resolved = MermaidStyleParser.Resolve(definitions, item.Classes, item.Styles, item.Span);
            item.Styles.Clear(); MermaidStyleParser.Copy(item.Styles, resolved);
            foreach (var pair in item.Styles) if ((pair.Key == "fill" || pair.Key == "stroke" || pair.Key == "color")
                && !ChartForgeX.SvgRaster.SvgRasterColor.TryParse(pair.Value, out _))
                MermaidParserUtilities.Add(result, item.Span, MermaidDiagnosticSeverity.Warning,
                    "Block style variable is retained; native static paint requires a resolved color.", MermaidDiagnosticCodes.UnsupportedStatement);
        }
        void Invalid(MermaidSourceSpan span, string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
            message, MermaidDiagnosticCodes.InvalidStatement);
    }
}
