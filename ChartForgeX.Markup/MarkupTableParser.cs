using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Markup;

/// <summary>
/// Parses ChartForgeX table markup into reusable table artifacts.
/// </summary>
public sealed class MarkupTableParser {
    /// <summary>
    /// Parses raw table markup or Markdown containing a chartforgex table fence.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<TableArtifact> Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var scan = VisualMarkupScanner.Scan(text);
        var result = new MarkupParseResult<TableArtifact>();
        foreach (var diagnostic in scan.Diagnostics) result.Diagnostics.Add(diagnostic);
        foreach (var scannedBlock in scan.Blocks) {
            if (scannedBlock.Kind == VisualMarkupKind.Table) return ParseBlockCore(scannedBlock, result);
        }

        if (result.Diagnostics.Count > 0) return result;
        return ParseBlockCore(CreateRawBlock(text), result);
    }

    /// <summary>
    /// Parses a pre-scanned ChartForgeX table block while preserving fence attributes and source lines.
    /// </summary>
    /// <param name="block">The table visual block.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<TableArtifact> ParseBlock(VisualMarkupBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        var result = new MarkupParseResult<TableArtifact>();
        if (block.Kind != VisualMarkupKind.Table) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Expected a ChartForgeX table visual block.");
            return result;
        }

        return ParseBlockCore(block, result);
    }

    private static MarkupParseResult<TableArtifact> ParseBlockCore(VisualMarkupBlock block, MarkupParseResult<TableArtifact> result) {
        result.Document = TableArtifact.Create("table");
        if (block.SchemaVersion != 1) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "ChartForgeX table markup requires schema version v1.");
        ApplyFenceAttributes(result, result.Document, block);
        var lines = block.Payload.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lineOffset = block.StartLine - 1;
        var section = string.Empty;
        List<string>? tableHeaders = null;

        for (var index = 0; index < lines.Length; index++) {
            var lineNumber = lineOffset + index + 1;
            var line = StripComment(lines[index]).Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (IsSection(line)) {
                section = NormalizeKey(line.TrimEnd(':'));
                tableHeaders = null;
                continue;
            }

            if (section.Length > 0 && IsSectionEnd(line)) {
                section = string.Empty;
                tableHeaders = null;
                continue;
            }

            if (IsTableLine(line, tableHeaders)) {
                tableHeaders = ParseTableLine(result, result.Document!, section, tableHeaders, line, lineNumber);
                continue;
            }

            ParseCommand(result, result.Document!, line, lineNumber);
        }

        if (result.Document!.Columns.Count == 0) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Table markup must declare at least one column.");
        return result;
    }

    private static VisualMarkupBlock CreateRawBlock(string text) =>
        new VisualMarkupBlock(VisualMarkupKind.Table, "chartforgex table", string.Empty, 1, text, 1, 1, Math.Max(1, text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length), EmptyAttributes.Value);

    private static void ApplyFenceAttributes(MarkupParseResult<TableArtifact> result, TableArtifact table, VisualMarkupBlock block) {
        if (block.Attributes.Count == 0) return;
        try {
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) table.Id = id;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) table.Title = title;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) table.Subtitle = subtitle;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "capabilities", out var capabilities) && !string.IsNullOrWhiteSpace(capabilities)) table.Capabilities = ParseCapabilities(Tokenize("capabilities " + capabilities.Replace(',', ' ')), 1);
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "totalRows", out var totalRows) && !string.IsNullOrWhiteSpace(totalRows)) table.TotalRowCount = long.Parse(totalRows, CultureInfo.InvariantCulture);
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static void ParseCommand(MarkupParseResult<TableArtifact> result, TableArtifact table, string line, int lineNumber) {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;
        var command = NormalizeKey(tokens[0].TrimEnd(':'));

        try {
            switch (command) {
                case "id":
                    RequireTokenCount(tokens, 2, "id");
                    table.Id = tokens[1];
                    break;
                case "title":
                    table.Title = JoinTail(tokens, 1);
                    break;
                case "subtitle":
                    table.Subtitle = JoinTail(tokens, 1);
                    break;
                case "capabilities":
                case "capability":
                    table.Capabilities = ParseCapabilities(tokens, 1);
                    break;
                case "totalrows":
                case "totalrowcount":
                    RequireTokenCount(tokens, 2, tokens[0]);
                    table.TotalRowCount = long.Parse(tokens[1], CultureInfo.InvariantCulture);
                    break;
                default:
                    Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown table command '" + tokens[0] + "'.");
                    break;
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static List<string>? ParseTableLine(MarkupParseResult<TableArtifact> result, TableArtifact table, string section, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) {
            var normalized = cells.Select(cell => NormalizeKey(cell)).ToList();
            if (section.Length == 0) {
                try {
                    foreach (var cell in cells) table.AddColumn(MakeColumnId(cell, table.Columns.Count), cell);
                } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException) {
                    Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
                }
            }

            return normalized;
        }

        try {
            if (section == "columns") {
                var row = Row(headers, cells);
                table.AddColumn(
                    Required(row, "id"),
                    Value(row, "label", Required(row, "id")),
                    ParseColumnType(Value(row, "type", "text")),
                    ParseAlignment(Value(row, "alignment", "left")),
                    OptionalDouble(row, "width"));
                ConfigureColumn(table.Columns[table.Columns.Count - 1], row);
            } else {
                AddDataRow(table, headers, cells);
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static void AddDataRow(TableArtifact table, List<string> headers, List<string> cells) {
        if (table.Columns.Count == 0) throw new InvalidOperationException("Table rows require columns before row values.");
        var rowKey = RowKey(headers, cells, table.Rows.Count);
        var values = new object?[table.Columns.Count];
        for (var i = 0; i < values.Length; i++) {
            var column = table.Columns[i];
            var sourceIndex = FindColumnIndex(headers, column, i);
            values[i] = sourceIndex >= 0 && sourceIndex < cells.Count ? cells[sourceIndex] : string.Empty;
        }

        table.AddRow(rowKey, values);
        var row = table.Rows[table.Rows.Count - 1];
        for (var i = 0; i < row.Cells.Count; i++) {
            if (table.Columns[i].Type == TableArtifactColumnType.Status) row.Cells[i].Status = ParseStatus(row.Cells[i].DisplayText);
        }
    }

    private static void ConfigureColumn(TableArtifactColumn column, Dictionary<string, string> values) {
        column.Searchable = Boolean(values, "searchable", column.Searchable);
        column.Sortable = Boolean(values, "sortable", column.Sortable);
        column.Filterable = Boolean(values, "filterable", column.Filterable);
        column.Copyable = Boolean(values, "copyable", column.Copyable);
        column.Exportable = Boolean(values, "exportable", column.Exportable);
    }

    private static string RowKey(List<string> headers, List<string> cells, int rowIndex) {
        var index = headers.FindIndex(header => header == "key" || header == "id");
        if (index >= 0 && index < cells.Count && !string.IsNullOrWhiteSpace(cells[index])) return cells[index];
        return "row-" + (rowIndex + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static int FindColumnIndex(List<string> headers, TableArtifactColumn column, int fallbackIndex) {
        var id = NormalizeKey(column.Id);
        var label = NormalizeKey(column.Label);
        for (var i = 0; i < headers.Count; i++) {
            if (headers[i] == id || headers[i] == label) return i;
        }

        return fallbackIndex < headers.Count ? fallbackIndex : -1;
    }

    private static Dictionary<string, string> Row(List<string> headers, List<string> cells) {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count && i < cells.Count; i++) row[headers[i]] = cells[i];
        return row;
    }

    private static TableArtifactCapabilities ParseCapabilities(List<string> tokens, int start) {
        var capabilities = TableArtifactCapabilities.None;
        for (var i = start; i < tokens.Count; i++) {
            switch (NormalizeKey(tokens[i])) {
                case "search":
                case "searchable":
                    capabilities |= TableArtifactCapabilities.Search;
                    break;
                case "sort":
                case "sortable":
                    capabilities |= TableArtifactCapabilities.Sort;
                    break;
                case "filter":
                case "filterable":
                    capabilities |= TableArtifactCapabilities.Filter;
                    break;
                case "singleselection":
                case "select":
                    capabilities |= TableArtifactCapabilities.SingleSelection;
                    break;
                case "multiselection":
                case "multiselect":
                    capabilities |= TableArtifactCapabilities.MultiSelection;
                    break;
                case "cellselection":
                case "cellselect":
                    capabilities |= TableArtifactCapabilities.CellSelection;
                    break;
                case "copy":
                    capabilities |= TableArtifactCapabilities.Copy;
                    break;
                case "export":
                    capabilities |= TableArtifactCapabilities.Export;
                    break;
                case "virtualization":
                case "virtualized":
                    capabilities |= TableArtifactCapabilities.Virtualization;
                    break;
                default:
                    throw new ArgumentException("Unknown table capability '" + tokens[i] + "'.");
            }
        }

        return capabilities;
    }

    private static TableArtifactColumnType ParseColumnType(string value) {
        switch (NormalizeKey(value)) {
            case "text":
            case "string":
                return TableArtifactColumnType.Text;
            case "number":
            case "numeric":
                return TableArtifactColumnType.Number;
            case "bool":
            case "boolean":
                return TableArtifactColumnType.Boolean;
            case "date":
                return TableArtifactColumnType.Date;
            case "datetime":
                return TableArtifactColumnType.DateTime;
            case "time":
                return TableArtifactColumnType.Time;
            case "status":
            case "severity":
                return TableArtifactColumnType.Status;
            case "uri":
            case "url":
            case "link":
                return TableArtifactColumnType.Uri;
            default:
                throw new ArgumentException("Unknown TableArtifactColumnType value '" + value + "'.");
        }
    }

    private static TextAlignment ParseAlignment(string value) {
        switch (NormalizeKey(value)) {
            case "left":
                return TextAlignment.Left;
            case "center":
            case "middle":
                return TextAlignment.Center;
            case "right":
                return TextAlignment.Right;
            default:
                throw new ArgumentException("Unknown TextAlignment value '" + value + "'.");
        }
    }

    private static VisualStatus ParseStatus(string value) {
        switch (NormalizeKey(value)) {
            case "healthy":
            case "ok":
            case "success":
            case "positive":
            case "enabled":
                return VisualStatus.Positive;
            case "warning":
            case "warn":
            case "attention":
                return VisualStatus.Warning;
            case "negative":
            case "error":
            case "failed":
            case "disabled":
            case "critical":
                return VisualStatus.Negative;
            case "info":
            case "information":
                return VisualStatus.Info;
            case "neutral":
                return VisualStatus.Neutral;
            default:
                return VisualStatus.None;
        }
    }

    private static string MakeColumnId(string label, int index) {
        var normalized = NormalizeKey(label);
        return normalized.Length == 0 ? "column" + (index + 1).ToString(CultureInfo.InvariantCulture) : normalized;
    }

    private static bool Boolean(Dictionary<string, string> values, string key, bool fallback) {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)) return fallback;
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
                throw new ArgumentException("Boolean table value '" + value + "' is not valid.");
        }
    }

    private static double? OptionalDouble(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? double.Parse(value, CultureInfo.InvariantCulture) : null;

    private static string Required(Dictionary<string, string> values, string key) {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        throw new ArgumentException("Table row requires '" + key + "'.");
    }

    private static string Value(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static void RequireTokenCount(List<string> tokens, int count, string command) {
        if (tokens.Count < count) throw new ArgumentException("Table command '" + command + "' requires a value.");
    }

    private static string JoinTail(List<string> tokens, int start) {
        if (start >= tokens.Count) return string.Empty;
        return string.Join(" ", tokens.Skip(start));
    }

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

    private static bool IsSection(string line) {
        var value = NormalizeKey(line.TrimEnd(':'));
        return line.EndsWith(":", StringComparison.Ordinal) && (value == "columns" || value == "rows");
    }

    private static bool IsSectionEnd(string line) => NormalizeKey(line) == "end";

    private static string NormalizeKey(string value) {
        var chars = new List<char>(value.Length);
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch)) chars.Add(char.ToLowerInvariant(ch));
        }

        return new string(chars.ToArray());
    }

    private static void Add(MarkupParseResult<TableArtifact> result, int line, MarkupDiagnosticSeverity severity, string message) {
        result.Diagnostics.Add(new MarkupDiagnostic {
            Line = line,
            Severity = severity,
            Message = message
        });
    }

    private static class EmptyAttributes {
        public static readonly IReadOnlyDictionary<string, string> Value = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
