using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidTreemapParser {
    public static void ParseStatements(MermaidTreemapDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var stack = new List<MermaidTreemapNode>();
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal)) continue;

            var indent = LeadingWhitespace(raw);
            var span = new MermaidSourceSpan(index + 1, indent + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));
            if (TryDefinition(document, trimmed, span, result)) continue;
            var parsed = ParseNode(trimmed, span, result);
            if (!parsed.HasValue) continue;

            while (stack.Count > 0 && stack[stack.Count - 1].Indent >= indent) stack.RemoveAt(stack.Count - 1);
            var parent = stack.Count == 0 ? null : stack[stack.Count - 1];
            MermaidResourceGuard.CheckDepth(stack.Count + 1, span);
            var node = new MermaidTreemapNode(parsed.Value.Label, parsed.Value.Value, indent, parent, parsed.Value.ClassName, span);
            if (node.ClassName != null && !ClassIdentifier(node.ClassName.TrimEnd(';')))
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                    "Treemap class suffix is retained without native application; use one simple class identifier.", MermaidDiagnosticCodes.UnsupportedStatement);
            if (parent == null) document.Roots.Add(node);
            else parent.Children.Add(node);
            document.Nodes.Add(node);
            if (!node.IsLeaf) stack.Add(node);
        }

        if (document.Nodes.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid treemap diagrams require at least one node.");
        if (LeafCount(document) == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid treemap diagrams require at least one leaf node with a value.");
        foreach (var node in document.Nodes) {
            if (node.IsLeaf && node.Children.Count > 0) Add(result, node.Span.Line, node.Span.Column, node.Span.Length, MermaidDiagnosticSeverity.Error, "Mermaid treemap leaf nodes with values cannot have child nodes.");
            if (!node.IsLeaf && node.Children.Count == 0) Add(result, node.Span.Line, node.Span.Column, node.Span.Length, MermaidDiagnosticSeverity.Error, "Mermaid treemap sections require child nodes; leaves require a value.");
        }
    }

    /// <summary>Treemap selects one literal class; its grammar does not inherit the other families' implicit default or multiple selectors.</summary>
    private static bool TryDefinition(MermaidTreemapDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (text != "classDef" && !MermaidParserUtilities.StartsStatement(text, "classDef")) return false;
        var body = text.Length > 8 ? text.Substring(8).Trim() : string.Empty;
        if (!MermaidStyleParser.SplitHead(body, out var name, out _) || !ClassIdentifier(name)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "Treemap classDef requires one simple class identifier followed by declarations.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        var first = document.ClassDefinitions.Count;
        MermaidStyleParser.TryDefinition(text, span, result, document.ClassDefinitions, "Treemap");
        for (var index = first; index < document.ClassDefinitions.Count; index++)
            foreach (var declaration in document.ClassDefinitions[index].Styles)
                if (declaration.Key is "fill" or "stroke" or "color"
                    && MermaidStyleValues.IsColor(declaration.Value)
                    && !ChartForgeX.SvgRaster.SvgRasterColor.TryParse(declaration.Value, out _))
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                        "Treemap source paints require a native color; CSS variables are retained without application.",
                        MermaidDiagnosticCodes.UnsupportedStatement);
        return true;
    }

    private static bool ClassIdentifier(string name) {
        if (name.Length == 0 || !(name[0] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_')) return false;
        foreach (var character in name)
            if (!(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')) return false;
        return true;
    }

    private static ParsedNode? ParseNode(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (text.Length == 0 || text[0] != '"') {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Treemap nodes must start with a quoted label.");
            return null;
        }

        var close = FindClosingQuote(text, 1);
        if (close < 0) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Treemap node labels must close with a quote.");
            return null;
        }

        var label = UnescapeQuoted(text.Substring(1, close - 1));
        var remainder = text.Substring(close + 1).Trim();
        string? className = null;
        double? value = null;
        if (remainder.StartsWith(":", StringComparison.Ordinal) && !remainder.StartsWith("::", StringComparison.Ordinal)) {
            var valueAndClass = SplitClass(remainder.Substring(1).Trim());
            if (!TryParseValue(valueAndClass.Text, out var parsedValue)) {
                Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Treemap leaf values must be finite non-negative numbers.");
                return null;
            }

            value = parsedValue;
            className = valueAndClass.ClassName;
        } else {
            var sectionAndClass = SplitClass(remainder);
            if (sectionAndClass.Text.Length != 0) Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Treemap section nodes can only include an optional :::class suffix.");
            className = sectionAndClass.ClassName;
        }

        return new ParsedNode(label, value, className);
    }

    private static (string Text, string? ClassName) SplitClass(string text) {
        var marker = text.IndexOf(":::", StringComparison.Ordinal);
        if (marker < 0) marker = text.IndexOf("::", StringComparison.Ordinal);
        if (marker < 0) return (text.Trim(), null);
        var markerLength = marker + 2 < text.Length && text[marker + 2] == ':' ? 3 : 2;
        var value = text.Substring(0, marker).Trim();
        var className = text.Substring(marker + markerLength).Trim();
        return (value, className.Length == 0 ? null : className);
    }

    private static int FindClosingQuote(string text, int start) {
        var escaped = false;
        for (var i = start; i < text.Length; i++) {
            var ch = text[i];
            if (escaped) {
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                escaped = true;
                continue;
            }

            if (ch == '"') return i;
        }

        return -1;
    }

    private static string UnescapeQuoted(string value) => value.Replace("\\\"", "\"").Replace("\\\\", "\\");

    private static bool TryParseValue(string text, out double value) {
        if (text.IndexOf(',') >= 0) {
            foreach (var ch in text) if (ch != ',' && ch != '.' && (ch < '0' || ch > '9')) { value = 0; return false; }
            text = text.Replace(",", string.Empty);
        }
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0) return true;
        value = 0;
        return false;
    }

    private static int LeafCount(MermaidTreemapDocument document) {
        var count = 0;
        foreach (var node in document.Nodes) if (node.IsLeaf) count++;
        return count;
    }

    private static int LeadingWhitespace(string text) {
        var count = 0;
        while (count < text.Length && char.IsWhiteSpace(text[count])) count++;
        return count;
    }

    private static void Add<TDocument>(MermaidParseResult<TDocument> result, int line, int column, int length, MermaidDiagnosticSeverity severity, string message) where TDocument : MermaidDocument {
        MermaidParserUtilities.Add(result, new MermaidSourceSpan(line, column, length), severity, message);
    }

    private readonly struct ParsedNode {
        public ParsedNode(string label, double? value, string? className) {
            Label = label;
            Value = value;
            ClassName = className;
        }

        public string Label { get; }

        public double? Value { get; }

        public string? ClassName { get; }
    }
}
