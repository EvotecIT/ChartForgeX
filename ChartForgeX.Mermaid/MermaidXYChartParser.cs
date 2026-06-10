using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static class MermaidXYChartParser {
    public static void ParseStatements(MermaidXYChartDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var barCount = 0;
        var lineCount = 0;
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal)) continue;

            var span = new MermaidSourceSpan(index + 1, LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "title")) {
                document.Title = Unquote(trimmed.Substring(5).Trim());
                continue;
            }

            if (StartsWithKeyword(trimmed, "x-axis")) {
                ParseAxis(document.XAxis, trimmed.Substring(6).Trim(), span, result, true);
                continue;
            }

            if (StartsWithKeyword(trimmed, "y-axis")) {
                ParseAxis(document.YAxis, trimmed.Substring(6).Trim(), span, result, false);
                continue;
            }

            if (StartsWithKeyword(trimmed, "bar")) {
                barCount++;
                ParseSeries(document, MermaidXYChartSeriesKind.Bar, "Bar " + barCount.ToString(CultureInfo.InvariantCulture), trimmed.Substring(3).Trim(), span, result);
                continue;
            }

            if (StartsWithKeyword(trimmed, "line")) {
                lineCount++;
                ParseSeries(document, MermaidXYChartSeriesKind.Line, "Line " + lineCount.ToString(CultureInfo.InvariantCulture), trimmed.Substring(4).Trim(), span, result);
                continue;
            }

            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "XY chart statement must be title, x-axis, y-axis, bar, or line.");
        }

        if (document.Series.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid XY charts require at least one bar or line series.");
    }

    private static void ParseAxis(MermaidXYChartAxis axis, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, bool allowLabels) {
        if (text.Length == 0) return;

        var listStart = FindOutsideQuotes(text, '[');
        if (listStart >= 0) {
            var listEnd = FindMatchingBracket(text, listStart);
            if (listEnd < 0) {
                Add(result, span.Line, span.Column + listStart, text.Length - listStart, MermaidDiagnosticSeverity.Error, "XY chart axis category lists must close with ']'.");
                return;
            }

            if (!allowLabels) {
                Add(result, span.Line, span.Column + listStart, listEnd - listStart + 1, MermaidDiagnosticSeverity.Error, "Mermaid XY chart y-axis cannot declare categorical labels.");
                return;
            }

            axis.Title = Unquote(text.Substring(0, listStart).Trim());
            foreach (var item in ParseTextList(text.Substring(listStart + 1, listEnd - listStart - 1))) axis.Labels.Add(item);
            return;
        }

        var arrow = FindArrow(text);
        if (arrow >= 0) {
            var left = text.Substring(0, arrow).Trim();
            var right = text.Substring(arrow + 3).Trim();
            if (!TryTakeTrailingNumber(left, out var title, out var minimum) || !TryParseNumber(right, out var maximum) || maximum <= minimum) {
                Add(result, span.Line, span.Column, text.Length, MermaidDiagnosticSeverity.Error, "XY chart axis ranges must use Mermaid syntax 'axis-title min --> max'.");
                return;
            }

            axis.Title = Unquote(title.Trim());
            axis.Minimum = minimum;
            axis.Maximum = maximum;
            return;
        }

        axis.Title = Unquote(text);
    }

    private static void ParseSeries(MermaidXYChartDocument document, MermaidXYChartSeriesKind kind, string defaultName, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var listStart = FindOutsideQuotes(text, '[');
        if (listStart < 0) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "XY chart series must use Mermaid syntax 'bar [numbers]' or 'line [numbers]'.");
            return;
        }

        var listEnd = FindMatchingBracket(text, listStart);
        if (listEnd < 0) {
            Add(result, span.Line, span.Column + listStart, text.Length - listStart, MermaidDiagnosticSeverity.Error, "XY chart series lists must close with ']'.");
            return;
        }

        var values = ParseNumberList(text.Substring(listStart + 1, listEnd - listStart - 1), span, result);
        if (values.Count == 0) {
            Add(result, span.Line, span.Column + listStart, listEnd - listStart + 1, MermaidDiagnosticSeverity.Error, "XY chart series require at least one numeric value.");
            return;
        }

        document.Series.Add(new MermaidXYChartSeries(kind, defaultName, values, span));
    }

    private static List<double> ParseNumberList(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var values = new List<double>();
        foreach (var item in SplitCommaList(text)) {
            if (!TryParseNumber(item, out var value)) {
                Add(result, span.Line, span.Column, item.Length, MermaidDiagnosticSeverity.Error, "XY chart series values must be finite numbers.");
                continue;
            }

            values.Add(value);
        }

        return values;
    }

    private static List<string> ParseTextList(string text) {
        var values = new List<string>();
        foreach (var item in SplitCommaList(text)) values.Add(Unquote(item.Trim()));
        return values;
    }

    private static List<string> SplitCommaList(string text) {
        var values = new List<string>();
        var current = new StringBuilder();
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

            if (!inQuote && ch == ',') {
                AddListPart(values, current);
                continue;
            }

            current.Append(ch);
        }

        AddListPart(values, current);
        return values;
    }

    private static void AddListPart(List<string> values, StringBuilder current) {
        var value = current.ToString().Trim();
        current.Length = 0;
        if (value.Length > 0) values.Add(value);
    }

    private static int FindArrow(string text) {
        var inQuote = false;
        var escaped = false;
        for (var i = 0; i <= text.Length - 3; i++) {
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

            if (!inQuote && text[i] == '-' && text[i + 1] == '-' && text[i + 2] == '>') return i;
        }

        return -1;
    }

    private static int FindOutsideQuotes(string text, char match) {
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

    private static int FindMatchingBracket(string text, int start) {
        var inQuote = false;
        var escaped = false;
        for (var i = start + 1; i < text.Length; i++) {
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

            if (!inQuote && ch == ']') return i;
        }

        return -1;
    }

    private static bool TryTakeTrailingNumber(string text, out string title, out double number) {
        title = string.Empty;
        number = 0;
        var trimmed = text.TrimEnd();
        if (trimmed.Length == 0) return false;
        var end = trimmed.Length - 1;
        var start = end;
        while (start >= 0 && !char.IsWhiteSpace(trimmed[start])) start--;
        var numberText = trimmed.Substring(start + 1);
        if (!TryParseNumber(numberText, out number)) return false;
        title = trimmed.Substring(0, start + 1).Trim();
        return true;
    }

    private static bool TryParseNumber(string text, out double value) {
        if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return false;
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }

    private static string Unquote(string value) {
        value = value.Trim();
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"') return UnescapeQuoted(value.Substring(1, value.Length - 2));
        return value;
    }

    private static string UnescapeQuoted(string value) {
        if (value.IndexOf('\\') < 0) return value;
        var chars = new List<char>(value.Length);
        var escaped = false;
        foreach (var ch in value) {
            if (escaped) {
                chars.Add(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                escaped = true;
                continue;
            }

            chars.Add(ch);
        }

        if (escaped) chars.Add('\\');
        return new string(chars.ToArray());
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
}
