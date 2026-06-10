using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidTreemapParser {
    public static void ParseStatements(MermaidTreemapDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var stack = new List<MermaidTreemapNode>();
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal)) continue;

            var indent = LeadingWhitespace(raw);
            var span = new MermaidSourceSpan(index + 1, indent + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));
            var parsed = ParseNode(trimmed, span, result);
            if (!parsed.HasValue) continue;

            while (stack.Count > 0 && stack[stack.Count - 1].Indent >= indent) stack.RemoveAt(stack.Count - 1);
            var parent = stack.Count == 0 ? null : stack[stack.Count - 1];
            var node = new MermaidTreemapNode(parsed.Value.Label, parsed.Value.Value, indent, parent, parsed.Value.ClassName, span);
            if (parent == null) document.Roots.Add(node);
            else parent.Children.Add(node);
            document.Nodes.Add(node);
            if (!node.IsLeaf) stack.Add(node);
        }

        if (document.Nodes.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid treemap diagrams require at least one node.");
        if (LeafCount(document) == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid treemap diagrams require at least one leaf node with a value.");
        foreach (var node in document.Nodes) {
            if (node.IsLeaf && node.Children.Count > 0) Add(result, node.Span.Line, node.Span.Column, node.Span.Length, MermaidDiagnosticSeverity.Error, "Mermaid treemap leaf nodes with values cannot have child nodes.");
        }
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
        result.Diagnostics.Add(new MermaidDiagnostic {
            Span = new MermaidSourceSpan(line, column, length),
            Severity = severity,
            Message = message
        });
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
