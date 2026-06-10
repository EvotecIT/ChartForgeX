using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Markup;

/// <summary>
/// Parses ChartForgeX chart markup into renderer-independent chart models.
/// </summary>
public sealed class MarkupChartParser {
    /// <summary>
    /// Parses raw chart markup or Markdown containing a chartforgex chart fence.
    /// </summary>
    public MarkupParseResult<MarkupChartDocument> Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var block = ExtractFirstChartBlock(text);
        var state = new ChartState();
        var result = new MarkupParseResult<MarkupChartDocument>();
        ApplyFenceAttributes(result, state, block);
        var lines = block.Payload.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lineOffset = block.StartLine - 1;
        List<string>? headers = null;

        for (var index = 0; index < lines.Length; index++) {
            var lineNumber = lineOffset + index + 1;
            var line = StripComment(lines[index]).Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (IsTableLine(line, headers)) {
                headers = ParseTableLine(result, state, headers, line, lineNumber);
                continue;
            }

            ParseCommand(result, state, line, lineNumber);
        }

        if (state.Values.Count == 0) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Chart markup must declare at least one numeric value.");
        if (!result.HasErrors) result.Document = new MarkupChartDocument { Id = state.Id, Chart = BuildChart(state) };
        return result;
    }

    private static VisualMarkupBlock ExtractFirstChartBlock(string text) {
        var scan = VisualMarkupScanner.Scan(text);
        foreach (var block in scan.Blocks) {
            if (block.Kind == VisualMarkupKind.Chart) return block;
        }

        return new VisualMarkupBlock(VisualMarkupKind.Chart, "chartforgex chart", string.Empty, text, 1, 1, Math.Max(1, text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length), EmptyAttributes.Value);
    }

    private static void ApplyFenceAttributes(MarkupParseResult<MarkupChartDocument> result, ChartState state, VisualMarkupBlock block) {
        if (block.Attributes.Count == 0) return;
        try {
            if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) state.Id = id;
            if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) state.Title = title;
            if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) state.Subtitle = subtitle;
            if (TryGetAttribute(block, "type", out var type) && !string.IsNullOrWhiteSpace(type)) state.Type = type;
            if (TryGetAttribute(block, "series", out var series) && !string.IsNullOrWhiteSpace(series)) state.SeriesName = series;
            if (TryGetAttribute(block, "size", out var size) && !string.IsNullOrWhiteSpace(size)) ParseSize(state, size);
            if (TryGetAttribute(block, "width", out var width) && !string.IsNullOrWhiteSpace(width)) state.Width = int.Parse(width, CultureInfo.InvariantCulture);
            if (TryGetAttribute(block, "height", out var height) && !string.IsNullOrWhiteSpace(height)) state.Height = int.Parse(height, CultureInfo.InvariantCulture);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static bool TryGetAttribute(VisualMarkupBlock block, string key, out string value) {
        if (block.Attributes.TryGetValue(key, out var exact)) {
            value = exact;
            return true;
        }

        var normalized = NormalizeKey(key);
        foreach (var item in block.Attributes) {
            if (NormalizeKey(item.Key) == normalized) {
                value = item.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static Chart BuildChart(ChartState state) {
        var chart = Chart.Create()
            .WithTitle(state.Title)
            .WithSubtitle(state.Subtitle)
            .WithSize(state.Width, state.Height);
        if (state.Labels.Count > 0) chart.WithXLabels(state.Labels.ToArray());
        if (state.DataLabels) chart.WithDataLabels();
        if (state.PointLegend) chart.WithPointLegend();
        chart.WithLegend(state.ShowLegend);

        var points = new ChartPoint[state.Values.Count];
        for (var i = 0; i < points.Length; i++) points[i] = new ChartPoint(i + 1, state.Values[i]);
        switch (NormalizeKey(state.Type)) {
            case "line":
                chart.AddLine(state.SeriesName, points);
                break;
            case "area":
                chart.AddArea(state.SeriesName, points);
                break;
            case "pie":
                chart.WithPointLegend().WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).AddPie(state.SeriesName, points);
                break;
            case "horizontalbar":
            case "hbar":
                chart.AddHorizontalBar(state.SeriesName, points);
                break;
            case "bar":
            case "column":
            default:
                chart.AddBar(state.SeriesName, points);
                break;
        }

        return chart;
    }

    private static void ParseCommand(MarkupParseResult<MarkupChartDocument> result, ChartState state, string line, int lineNumber) {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;
        var command = NormalizeKey(tokens[0].TrimEnd(':'));
        try {
            switch (command) {
                case "id":
                    RequireTokenCount(tokens, 2, "id");
                    state.Id = tokens[1];
                    break;
                case "title":
                    state.Title = JoinTail(tokens, 1);
                    break;
                case "subtitle":
                    state.Subtitle = JoinTail(tokens, 1);
                    break;
                case "type":
                case "kind":
                    RequireTokenCount(tokens, 2, tokens[0]);
                    state.Type = tokens[1];
                    break;
                case "series":
                    state.SeriesName = JoinTail(tokens, 1);
                    break;
                case "labels":
                case "categories":
                    state.Labels.Clear();
                    state.Labels.AddRange(tokens.Skip(1));
                    break;
                case "values":
                    state.Values.Clear();
                    for (var i = 1; i < tokens.Count; i++) state.Values.Add(ParseDouble(tokens[i]));
                    break;
                case "value":
                    RequireTokenCount(tokens, 3, "value");
                    state.Labels.Add(tokens[1]);
                    state.Values.Add(ParseDouble(tokens[2]));
                    break;
                case "size":
                    RequireTokenCount(tokens, 2, "size");
                    ParseSize(state, tokens[1]);
                    break;
                case "datalabels":
                case "labelsvisible":
                    state.DataLabels = tokens.Count == 1 || ParseBoolean(tokens[1]);
                    break;
                case "legend":
                    state.ShowLegend = tokens.Count == 1 || ParseBoolean(tokens[1]);
                    break;
                case "pointlegend":
                    state.PointLegend = tokens.Count == 1 || ParseBoolean(tokens[1]);
                    break;
                default:
                    Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown chart command '" + tokens[0] + "'.");
                    break;
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static List<string>? ParseTableLine(MarkupParseResult<MarkupChartDocument> result, ChartState state, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) return cells.Select(NormalizeKey).ToList();

        try {
            var row = Row(headers, cells);
            var label = Value(row, "label", Value(row, "category", Value(row, "name", string.Empty)));
            var value = Required(row, "value");
            state.Labels.Add(label);
            state.Values.Add(ParseDouble(value));
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static Dictionary<string, string> Row(List<string> headers, List<string> cells) {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count && i < cells.Count; i++) row[headers[i]] = cells[i];
        return row;
    }

    private static string Required(Dictionary<string, string> values, string key) {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        throw new ArgumentException("Chart row requires '" + key + "'.");
    }

    private static string Value(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static void ParseSize(ChartState state, string value) {
        var parts = value.Split(new[] { 'x', 'X', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) throw new ArgumentException("Chart size must use WIDTHxHEIGHT syntax.");
        state.Width = int.Parse(parts[0], CultureInfo.InvariantCulture);
        state.Height = int.Parse(parts[1], CultureInfo.InvariantCulture);
    }

    private static bool ParseBoolean(string value) {
        switch (NormalizeKey(value)) {
            case "true":
            case "yes":
            case "1":
                return true;
            case "false":
            case "no":
            case "0":
                return false;
            default:
                throw new ArgumentException("Boolean chart value '" + value + "' is not valid.");
        }
    }

    private static double ParseDouble(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static void RequireTokenCount(List<string> tokens, int count, string command) {
        if (tokens.Count < count) throw new ArgumentException("Chart command '" + command + "' requires a value.");
    }

    private static string JoinTail(List<string> tokens, int start) =>
        start >= tokens.Count ? string.Empty : string.Join(" ", tokens.Skip(start));

    private static string StripComment(string line) {
        var inQuote = false;
        for (var i = 0; i < line.Length - 1; i++) {
            if (line[i] == '"') inQuote = !inQuote;
            if (!inQuote && line[i] == '/' && line[i + 1] == '/' && (i == 0 || char.IsWhiteSpace(line[i - 1]))) return line.Substring(0, i);
        }

        return line;
    }

    private static List<string> Tokenize(string line) {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuote = false;
        for (var i = 0; i < line.Length; i++) {
            var ch = line[i];
            if (ch == '"') {
                inQuote = !inQuote;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuote) {
                if (current.Length > 0) {
                    tokens.Add(current.ToString());
                    current.Length = 0;
                }

                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0) tokens.Add(current.ToString());
        return tokens;
    }

    private static bool IsTableLine(string line, List<string>? headers) {
        if (line.IndexOf("|", StringComparison.Ordinal) < 0) return false;
        if (line.TrimStart().StartsWith("|", StringComparison.Ordinal)) return true;
        return headers != null;
    }

    private static List<string> SplitTableCells(string line) {
        var text = line.Trim();
        if (text.StartsWith("|", StringComparison.Ordinal)) text = text.Substring(1);
        if (text.EndsWith("|", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 1);
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var escaped = false;
        foreach (var ch in text) {
            if (escaped) {
                current.Append(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                escaped = true;
                continue;
            }

            if (ch == '|') {
                cells.Add(current.ToString().Trim());
                current.Length = 0;
                continue;
            }

            current.Append(ch);
        }

        cells.Add(current.ToString().Trim());
        return cells;
    }

    private static bool IsTableSeparator(List<string> cells) {
        if (cells.Count == 0) return false;
        foreach (var cell in cells) {
            var value = cell.Trim().Trim(':');
            if (value.Length == 0 || value.Any(ch => ch != '-')) return false;
        }

        return true;
    }

    private static string NormalizeKey(string value) {
        var chars = new List<char>(value.Length);
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch)) chars.Add(char.ToLowerInvariant(ch));
        }

        return new string(chars.ToArray());
    }

    private static void Add(MarkupParseResult<MarkupChartDocument> result, int line, MarkupDiagnosticSeverity severity, string message) {
        result.Diagnostics.Add(new MarkupDiagnostic {
            Line = line,
            Severity = severity,
            Message = message
        });
    }

    private sealed class ChartState {
        public string Id { get; set; } = "chart";
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Type { get; set; } = "bar";
        public string SeriesName { get; set; } = "Series";
        public int Width { get; set; } = 820;
        public int Height { get; set; } = 460;
        public bool DataLabels { get; set; }
        public bool ShowLegend { get; set; } = true;
        public bool PointLegend { get; set; }
        public List<string> Labels { get; } = new();
        public List<double> Values { get; } = new();
    }

    private static class EmptyAttributes {
        public static readonly IReadOnlyDictionary<string, string> Value = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
