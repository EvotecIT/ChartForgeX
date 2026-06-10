using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static class MermaidRadarParser {
    public static void ParseStatements(MermaidRadarDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal)) continue;

            var span = new MermaidSourceSpan(index + 1, LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "title")) document.Title = trimmed.Substring(5).Trim();
            else if (StartsWithKeyword(trimmed, "axis")) ParseAxes(document, trimmed.Substring(4).Trim(), span, result);
            else if (StartsWithKeyword(trimmed, "curve")) ParseCurves(document, trimmed.Substring(5).Trim(), span, result);
            else if (StartsWithKeyword(trimmed, "showLegend")) ParseBoolean(trimmed.Substring(10).Trim(), span, value => document.ShowLegend = value, result, "showLegend");
            else if (StartsWithKeyword(trimmed, "min")) ParseDouble(trimmed.Substring(3).Trim(), span, value => document.Minimum = value, result, "min");
            else if (StartsWithKeyword(trimmed, "max")) ParseDouble(trimmed.Substring(3).Trim(), span, value => document.Maximum = value, result, "max");
            else if (StartsWithKeyword(trimmed, "ticks")) ParseInteger(trimmed.Substring(5).Trim(), span, value => document.Ticks = value, result, "ticks");
            else if (StartsWithKeyword(trimmed, "graticule")) document.Graticule = trimmed.Substring(9).Trim();
            else Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Radar statement must be title, axis, curve, showLegend, min, max, ticks, or graticule.");
        }

        if (document.Axes.Count < 3) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid radar diagrams require at least three axes.");
        if (document.Curves.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid radar diagrams require at least one curve.");
        var effectiveMinimum = document.Minimum ?? 0;
        if (document.Maximum.HasValue && document.Maximum.Value <= effectiveMinimum) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid radar max must be greater than min.");
        ValidateCurves(document, result);
    }

    private static void ParseAxes(MermaidRadarDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        foreach (var item in SplitTopLevel(text)) {
            var axis = ParseIdLabel(item, span, result, "Radar axis");
            if (axis.HasValue) document.Axes.Add(new MermaidRadarAxis(axis.Value.Id, axis.Value.Label, span));
        }
    }

    private static void ParseCurves(MermaidRadarDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        foreach (var item in SplitTopLevel(text)) {
            var open = FindTopLevel(item, '{');
            var close = item.LastIndexOf('}');
            if (open <= 0 || close <= open) {
                Add(result, span.Line, span.Column, item.Length, MermaidDiagnosticSeverity.Error, "Radar curves must use Mermaid syntax 'curve id[\"Label\"]{values}'.");
                continue;
            }

            var idLabel = ParseIdLabel(item.Substring(0, open).Trim(), span, result, "Radar curve");
            if (!idLabel.HasValue) continue;
            var curve = new MermaidRadarCurve(idLabel.Value.Id, idLabel.Value.Label, span);
            ParseCurveValues(curve, item.Substring(open + 1, close - open - 1), span, result);
            document.Curves.Add(curve);
        }
    }

    private static void ParseCurveValues(MermaidRadarCurve curve, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var items = SplitTopLevel(text);
        var hasKeyValues = false;
        foreach (var item in items) {
            if (FindTopLevel(item, ':') >= 0) {
                hasKeyValues = true;
                break;
            }
        }

        foreach (var item in items) {
            if (hasKeyValues) {
                var colon = FindTopLevel(item, ':');
                if (colon <= 0) {
                    Add(result, span.Line, span.Column, item.Length, MermaidDiagnosticSeverity.Error, "Radar key-value curves must use 'axisId: value' entries.");
                    continue;
                }

                var axisId = item.Substring(0, colon).Trim();
                var valueText = item.Substring(colon + 1).Trim();
                if (axisId.Length == 0 || !TryParseDouble(valueText, out var value)) {
                    Add(result, span.Line, span.Column, item.Length, MermaidDiagnosticSeverity.Error, "Radar key-value curve entries must have a non-empty axis id and finite numeric value.");
                    continue;
                }

                curve.ValuesByAxisId[axisId] = value;
            } else if (TryParseDouble(item.Trim(), out var value)) {
                curve.OrderedValues.Add(value);
            } else {
                Add(result, span.Line, span.Column, item.Length, MermaidDiagnosticSeverity.Error, "Radar curve values must be finite numbers.");
            }
        }
    }

    private static void ValidateCurves(MermaidRadarDocument document, MermaidParseResult<MermaidDocument> result) {
        var axisIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var axis in document.Axes) axisIds.Add(axis.Id);
        foreach (var curve in document.Curves) {
            if (curve.ValuesByAxisId.Count > 0) {
                foreach (var axisId in curve.ValuesByAxisId.Keys) {
                    if (!axisIds.Contains(axisId)) Add(result, curve.Span.Line, curve.Span.Column, curve.Span.Length, MermaidDiagnosticSeverity.Error, "Radar curve '" + curve.Id + "' references unknown axis '" + axisId + "'.");
                }
            } else if (curve.OrderedValues.Count != document.Axes.Count) {
                Add(result, curve.Span.Line, curve.Span.Column, curve.Span.Length, MermaidDiagnosticSeverity.Error, "Radar curve '" + curve.Id + "' must provide one ordered value per axis.");
            }
        }
    }

    private static IdLabel? ParseIdLabel(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, string role) {
        text = text.Trim();
        if (text.Length == 0) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, role + " must declare an id.");
            return null;
        }

        var open = FindTopLevel(text, '[');
        if (open >= 0) {
            var close = text.LastIndexOf(']');
            if (close <= open) {
                Add(result, span.Line, span.Column, text.Length, MermaidDiagnosticSeverity.Error, role + " labels must close with ']'.");
                return null;
            }

            var id = text.Substring(0, open).Trim();
            var label = Unquote(text.Substring(open + 1, close - open - 1).Trim());
            if (id.Length == 0) {
                Add(result, span.Line, span.Column, text.Length, MermaidDiagnosticSeverity.Error, role + " id must not be empty.");
                return null;
            }

            return new IdLabel(id, label.Length == 0 ? id : label);
        }

        return new IdLabel(text, text);
    }

    private static List<string> SplitTopLevel(string text) {
        var values = new List<string>();
        var current = new StringBuilder();
        var bracketDepth = 0;
        var braceDepth = 0;
        var inQuote = false;
        var escaped = false;
        foreach (var ch in text) {
            if (escaped) {
                current.Append(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                current.Append(ch);
                escaped = true;
                continue;
            }

            if (ch == '"') {
                current.Append(ch);
                inQuote = !inQuote;
                continue;
            }

            if (!inQuote) {
                if (ch == '[') bracketDepth++;
                else if (ch == ']' && bracketDepth > 0) bracketDepth--;
                else if (ch == '{') braceDepth++;
                else if (ch == '}' && braceDepth > 0) braceDepth--;
                else if (ch == ',' && bracketDepth == 0 && braceDepth == 0) {
                    AddPart(values, current);
                    continue;
                }
            }

            current.Append(ch);
        }

        AddPart(values, current);
        return values;
    }

    private static int FindTopLevel(string text, char match) {
        var inQuote = false;
        var escaped = false;
        for (var i = 0; i < text.Length; i++) {
            var ch = text[i];
            if (escaped) {
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                escaped = true;
                continue;
            }

            if (ch == '"') {
                inQuote = !inQuote;
                continue;
            }

            if (!inQuote && ch == match) return i;
        }

        return -1;
    }

    private static void AddPart(List<string> values, StringBuilder current) {
        var value = current.ToString().Trim();
        current.Length = 0;
        if (value.Length > 0) values.Add(value);
    }

    private static void ParseBoolean(string text, MermaidSourceSpan span, Action<bool> assign, MermaidParseResult<MermaidDocument> result, string keyword) {
        if (bool.TryParse(text, out var value)) assign(value);
        else Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Radar '" + keyword + "' must be true or false.");
    }

    private static void ParseDouble(string text, MermaidSourceSpan span, Action<double> assign, MermaidParseResult<MermaidDocument> result, string keyword) {
        if (TryParseDouble(text, out var value)) assign(value);
        else Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Radar '" + keyword + "' must be a finite number.");
    }

    private static void ParseInteger(string text, MermaidSourceSpan span, Action<int> assign, MermaidParseResult<MermaidDocument> result, string keyword) {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 2) assign(value);
        else Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Radar '" + keyword + "' must be an integer greater than one.");
    }

    private static bool TryParseDouble(string text, out double value) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value);

    private static string Unquote(string value) {
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"') return value.Substring(1, value.Length - 2);
        return value;
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
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

    private readonly struct IdLabel {
        public IdLabel(string id, string label) {
            Id = id;
            Label = label;
        }

        public string Id { get; }

        public string Label { get; }
    }
}
