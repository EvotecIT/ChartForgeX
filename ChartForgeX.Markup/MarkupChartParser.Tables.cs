using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private static List<string>? ParseTableLine(MarkupParseResult<MarkupChartDocument> result, ChartState state, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) {
            ValidateTableHeaders(result, "data", cells, lineNumber);
            return cells;
        }

        try {
            var row = Row(headers, cells);
            if (row.ContainsKey("value")) {
                var label = Value(row, "label", Value(row, "category", Value(row, "name", string.Empty)));
                var value = Required(row, "value");
                state.Labels.Add(label);
                state.Values.Add(ParseDouble(value));
            } else {
                var labelIndex = FindHeader(headers, "label", "category", "name");
                if (labelIndex < 0) labelIndex = FindFirstNonNumericCell(cells);
                if (labelIndex < 0) throw new ArgumentException("Multi-series chart tables require at least one non-numeric label column.");
                var label = labelIndex < cells.Count ? cells[labelIndex] : string.Empty;
                state.Labels.Add(label);
                for (var i = 0; i < headers.Count; i++) {
                    if (i == labelIndex) continue;
                    if (i >= cells.Count || string.IsNullOrWhiteSpace(cells[i]))
                        throw new ArgumentException("Chart table row requires a numeric value for series '" + headers[i] + "'.");
                    var series = GetOrAddSeries(state, headers[i]);
                    series.Values.Add(ParseDouble(cells[i]));
                }
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static List<string>? ParseOptionTableLine(MarkupParseResult<MarkupChartDocument> result, ChartState state, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) {
            ValidateTableHeaders(result, "options", cells, lineNumber);
            return cells;
        }

        try {
            var row = Row(headers, cells);
            var key = Value(row, "option", Value(row, "key", Value(row, "name", string.Empty)));
            var value = Value(row, "value", string.Empty);
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Chart options table requires an 'option' or 'key' column.");
            ParseChartOption(result, state, key, value, lineNumber);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static int FindHeader(List<string> headers, params string[] names) {
        for (var index = 0; index < headers.Count; index++) {
            for (var nameIndex = 0; nameIndex < names.Length; nameIndex++) {
                if (NormalizeKey(headers[index]) == NormalizeKey(names[nameIndex])) return index;
            }
        }

        return -1;
    }

    private static int FindFirstNonNumericCell(List<string> cells) {
        for (var index = 0; index < cells.Count; index++) {
            if (string.IsNullOrWhiteSpace(cells[index])) continue;
            if (!double.TryParse(cells[index], NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return index;
        }

        return -1;
    }

    private static Dictionary<string, string> Row(List<string> headers, List<string> cells) {
        if (cells.Count > headers.Count) throw new ArgumentException("Chart table row has more cells than its header.");
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count && i < cells.Count; i++) row[NormalizeKey(headers[i])] = cells[i];
        return row;
    }

    // Typed fields normalize punctuation; numeric series retain their authored names.
    private static void ValidateTableHeaders(MarkupParseResult<MarkupChartDocument> result,
        string section, List<string> headers, int line) {
        var normalized = headers.Select(NormalizeKey).ToArray();
        var defaultValues = section == "data" && normalized.Contains("value");
        string[]? allowed = section switch {
            "data" => defaultValues ? new[] { "label", "category", "name", "value" } : null,
            "options" => new[] { "option", "key", "name", "value" },
            "candles" => new[] { "x", "open", "high", "low", "close" },
            "items" => new[] { "id", "label", "parent", "value", "colorvalue" },
            "nodes" => new[] { "id", "label" },
            _ => new[] { "id", "source", "target", "value", "colorvalue" }
        };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++) {
            var identity = section == "data" && !defaultValues ? headers[i].Trim() : normalized[i];
            if (!seen.Add(identity)) Add(result, line, MarkupDiagnosticSeverity.Error, "Duplicate chart table column '" + headers[i] + "'.");
            if (allowed != null && !allowed.Contains(normalized[i]))
                Add(result, line, MarkupDiagnosticSeverity.Error, "Unknown " + section + " column '" + headers[i] + "'.");
        }
        var aliases = section == "options" ? new[] { "option", "key", "name" }
            : section == "data" ? new[] { "label", "category", "name" } : Array.Empty<string>();
        if (normalized.Count(key => aliases.Contains(key)) > 1)
            Add(result, line, MarkupDiagnosticSeverity.Error, "Chart table requires only one " + (section == "options" ? "option/key/name" : "label/category/name") + " column.");
    }
}
