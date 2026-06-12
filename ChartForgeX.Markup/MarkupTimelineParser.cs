using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Markup;

/// <summary>
/// Parses ChartForgeX timeline markup into native timeline or Gantt chart models.
/// </summary>
public sealed class MarkupTimelineParser {
    /// <summary>
    /// Parses raw timeline markup or Markdown containing a <c>chartforgex timeline v1</c> fence.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<MarkupChartDocument> Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var scan = VisualMarkupScanner.Scan(text);
        var result = new MarkupParseResult<MarkupChartDocument>();
        foreach (var diagnostic in scan.Diagnostics) result.Diagnostics.Add(diagnostic);
        foreach (var block in scan.Blocks) {
            if (block.Kind == VisualMarkupKind.Timeline) return ParseBlockCore(block, result);
        }

        if (result.Diagnostics.Count > 0) return result;
        return ParseBlockCore(CreateRawBlock(text), result);
    }

    /// <summary>
    /// Parses a pre-scanned ChartForgeX timeline block while preserving fence attributes and source lines.
    /// </summary>
    /// <param name="block">The timeline visual block.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<MarkupChartDocument> ParseBlock(VisualMarkupBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        var result = new MarkupParseResult<MarkupChartDocument>();
        if (block.Kind != VisualMarkupKind.Timeline) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Expected a ChartForgeX timeline visual block.");
            return result;
        }

        return ParseBlockCore(block, result);
    }

    private static MarkupParseResult<MarkupChartDocument> ParseBlockCore(VisualMarkupBlock block, MarkupParseResult<MarkupChartDocument> result) {
        var state = new TimelineState();
        if (block.SchemaVersion != 1) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "ChartForgeX timeline markup requires schema version v1.");
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

        if (state.Items.Count == 0) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Timeline markup must declare at least one item, task, or milestone.");
        if (!result.HasErrors) result.Document = new MarkupChartDocument { Id = state.Id, Chart = BuildChart(state) };
        return result;
    }

    private static VisualMarkupBlock CreateRawBlock(string text) =>
        new(VisualMarkupKind.Timeline, "chartforgex timeline", string.Empty, 1, text, 1, 1, Math.Max(1, text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length), EmptyAttributes.Value);

    private static void ApplyFenceAttributes(MarkupParseResult<MarkupChartDocument> result, TimelineState state, VisualMarkupBlock block) {
        try {
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) state.Id = id;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) state.Title = title;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) state.Subtitle = subtitle;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "mode", out var mode) && !string.IsNullOrWhiteSpace(mode)) state.Mode = ParseMode(mode);
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "type", out var type) && !string.IsNullOrWhiteSpace(type)) state.Mode = ParseMode(type);
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "width", out var width) && !string.IsNullOrWhiteSpace(width)) state.Width = ParsePositiveInt32(width, "width");
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "height", out var height) && !string.IsNullOrWhiteSpace(height)) state.Height = ParsePositiveInt32(height, "height");
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "today", out var today) && !string.IsNullOrWhiteSpace(today)) state.Today = ParseTimelineValue(today);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static Chart BuildChart(TimelineState state) {
        var chart = Chart.Create()
            .WithTitle(state.Title)
            .WithSubtitle(state.Subtitle)
            .WithSize(state.Width, state.Height);
        if (state.Today.HasValue) chart.WithGanttToday(state.Today.Value.ToAxisValue());

        foreach (var item in state.Items) {
            var color = string.IsNullOrWhiteSpace(item.Color) ? (ChartColor?)null : ChartColor.FromHex(item.Color!);
            var start = item.Start.ToAxisValue();
            var end = item.End.ToAxisValue();
            if (item.Kind == TimelineItemKind.Milestone) {
                if (state.Mode == TimelineMode.Gantt) chart.AddGanttMilestone(item.Label, start, item.DependsOn, color);
                else chart.AddTimelineRange(item.Label, start, start, color);
                continue;
            }

            if (state.Mode == TimelineMode.Gantt || item.Kind == TimelineItemKind.Task) chart.AddGanttTask(item.Label, start, end, item.Progress, item.DependsOn, color);
            else chart.AddTimelineRange(item.Label, start, end, color);
        }

        return chart;
    }

    private static void ParseCommand(MarkupParseResult<MarkupChartDocument> result, TimelineState state, string line, int lineNumber) {
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
                case "mode":
                case "type":
                    RequireTokenCount(tokens, 2, tokens[0]);
                    state.Mode = ParseMode(tokens[1]);
                    break;
                case "today":
                    RequireTokenCount(tokens, 2, "today");
                    state.Today = ParseTimelineValue(tokens[1]);
                    break;
                case "size":
                    RequireTokenCount(tokens, 2, "size");
                    ParseSize(state, tokens[1]);
                    break;
                case "item":
                case "range":
                case "event":
                    state.Items.Add(ParseCommandItem(tokens, TimelineItemKind.Item));
                    break;
                case "task":
                    state.Items.Add(ParseCommandItem(tokens, TimelineItemKind.Task));
                    break;
                case "milestone":
                    state.Items.Add(ParseCommandItem(tokens, TimelineItemKind.Milestone));
                    break;
                default:
                    Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown timeline command '" + tokens[0] + "'.");
                    break;
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static TimelineItem ParseCommandItem(List<string> tokens, TimelineItemKind kind) {
        var minimum = kind == TimelineItemKind.Milestone ? 3 : 4;
        RequireTokenCount(tokens, minimum, tokens[0]);
        var item = new TimelineItem {
            Kind = kind,
            Label = tokens[1],
            Start = ParseTimelineValue(tokens[2]),
            End = kind == TimelineItemKind.Milestone ? ParseTimelineValue(tokens[2]) : ParseTimelineValue(tokens[3])
        };
        var attributes = Attributes(tokens, minimum);
        if (attributes.TryGetValue("progress", out var progress)) item.Progress = ParseProgress(progress);
        if (attributes.TryGetValue("dependson", out var dependsOn)) item.DependsOn = VisualMarkupFenceOptions.ParseInt32(dependsOn, "dependsOn");
        if (attributes.TryGetValue("color", out var color)) {
            ValidateColor(color, "Timeline item color");
            item.Color = color;
        }

        ValidateTimelineItem(item);
        return item;
    }

    private static List<string>? ParseTableLine(MarkupParseResult<MarkupChartDocument> result, TimelineState state, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) return cells.Select(NormalizeKey).ToList();

        try {
            var row = Row(headers, cells);
            var kind = row.TryGetValue("kind", out var kindValue) || row.TryGetValue("type", out kindValue) ? ParseItemKind(kindValue) : TimelineItemKind.Item;
            var label = Value(row, "label", Value(row, "name", Value(row, "item", Value(row, "task", string.Empty))));
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Timeline table rows require a label, name, item, or task column.");
            var start = Required(row, "start");
            var end = kind == TimelineItemKind.Milestone ? start : Value(row, "end", start);
            var color = Value(row, "color", null!);
            ValidateColor(color, "Timeline item color");
            var item = new TimelineItem {
                Kind = kind,
                Label = label,
                Start = ParseTimelineValue(start),
                End = ParseTimelineValue(end),
                Progress = row.TryGetValue("progress", out var progress) && !string.IsNullOrWhiteSpace(progress) ? ParseProgress(progress) : 0,
                DependsOn = row.TryGetValue("dependson", out var dependsOn) && !string.IsNullOrWhiteSpace(dependsOn) ? VisualMarkupFenceOptions.ParseInt32(dependsOn, "dependsOn") : -1,
                Color = color
            };
            ValidateTimelineItem(item);
            state.Items.Add(item);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static Dictionary<string, string> Attributes(List<string> tokens, int start) {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = start; i < tokens.Count; i++) {
            var split = tokens[i].IndexOf(':');
            if (split <= 0) split = tokens[i].IndexOf('=');
            if (split <= 0) continue;
            attributes[NormalizeKey(tokens[i].Substring(0, split))] = tokens[i].Substring(split + 1);
        }

        return attributes;
    }

    private static TimelineMode ParseMode(string value) {
        switch (NormalizeKey(value)) {
            case "timeline":
            case "range":
                return TimelineMode.Timeline;
            case "gantt":
                return TimelineMode.Gantt;
            default:
                throw new ArgumentException("Unknown timeline mode '" + value + "'.");
        }
    }

    private static TimelineItemKind ParseItemKind(string value) {
        switch (NormalizeKey(value)) {
            case "item":
            case "range":
                return TimelineItemKind.Item;
            case "task":
                return TimelineItemKind.Task;
            case "milestone":
                return TimelineItemKind.Milestone;
            default:
                throw new ArgumentException("Unknown timeline item kind '" + value + "'.");
        }
    }

    private static TimelineValue ParseTimelineValue(string value) {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return new TimelineValue(number);
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date)) return new TimelineValue(date);
        return new TimelineValue(VisualMarkupFenceOptions.ParseDouble(value, "timeline value"));
    }

    private static double ParseProgress(string value) {
        var progress = VisualMarkupFenceOptions.ParseDouble(value, "progress");
        if (double.IsNaN(progress) || double.IsInfinity(progress) || progress < 0 || progress > 1) throw new ArgumentException("Timeline progress must be between 0 and 1.");
        return progress;
    }

    private static void ValidateTimelineItem(TimelineItem item) {
        if (item.Kind != TimelineItemKind.Milestone && item.End.ToAxisValue() < item.Start.ToAxisValue()) throw new ArgumentException("Timeline item end must be greater than or equal to start.");
    }

    private static void ParseSize(TimelineState state, string value) {
        var parts = value.Split(new[] { 'x', 'X', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) throw new ArgumentException("Timeline size must use WIDTHxHEIGHT syntax.");
        state.Width = ParsePositiveInt32(parts[0], "width");
        state.Height = ParsePositiveInt32(parts[1], "height");
    }

    private static int ParsePositiveInt32(string value, string name) {
        var parsed = VisualMarkupFenceOptions.ParseInt32(value, name);
        if (parsed <= 0) throw new ArgumentException("Timeline " + name + " must be positive.");
        return parsed;
    }

    private static void ValidateColor(string? value, string name) {
        if (string.IsNullOrWhiteSpace(value)) return;
        try {
            ChartColor.FromHex(value!);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            throw new ArgumentException(name + " must be a valid hex color.", ex);
        }
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

    private static string StripComment(string line) {
        var inQuote = false;
        for (var i = 0; i < line.Length - 1; i++) {
            if (line[i] == '"') inQuote = !inQuote;
            if (!inQuote && line[i] == '/' && line[i + 1] == '/' && (i == 0 || char.IsWhiteSpace(line[i - 1]))) return line.Substring(0, i);
        }

        return line;
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

    private static Dictionary<string, string> Row(List<string> headers, List<string> cells) {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count && i < cells.Count; i++) row[headers[i]] = cells[i];
        return row;
    }

    private static string Required(Dictionary<string, string> values, string key) {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        throw new ArgumentException("Timeline row requires '" + key + "'.");
    }

    private static string Value(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static void RequireTokenCount(List<string> tokens, int count, string command) {
        if (tokens.Count < count) throw new ArgumentException("Timeline command '" + command + "' requires more values.");
    }

    private static string JoinTail(List<string> tokens, int start) =>
        start >= tokens.Count ? string.Empty : string.Join(" ", tokens.Skip(start));

    private static string NormalizeKey(string value) {
        var chars = new List<char>(value.Length);
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch)) chars.Add(char.ToLowerInvariant(ch));
        }

        return new string(chars.ToArray());
    }

    private static void Add(MarkupParseResult<MarkupChartDocument> result, int line, MarkupDiagnosticSeverity severity, string message) {
        result.Diagnostics.Add(new MarkupDiagnostic { Line = line, Severity = severity, Message = message });
    }

    private enum TimelineMode { Timeline, Gantt }
    private enum TimelineItemKind { Item, Task, Milestone }

    private sealed class TimelineState {
        public string Id { get; set; } = "timeline";
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public TimelineMode Mode { get; set; }
        public int Width { get; set; } = 900;
        public int Height { get; set; } = 460;
        public TimelineValue? Today { get; set; }
        public List<TimelineItem> Items { get; } = new();
    }

    private sealed class TimelineItem {
        public TimelineItemKind Kind { get; set; }
        public string Label { get; set; } = string.Empty;
        public TimelineValue Start { get; set; }
        public TimelineValue End { get; set; }
        public double Progress { get; set; }
        public int DependsOn { get; set; } = -1;
        public string? Color { get; set; }
    }

    private readonly struct TimelineValue {
        private readonly DateTime _date;
        private readonly double _number;
        private readonly bool _isDate;

        public TimelineValue(DateTime date) {
            _date = date;
            _number = 0;
            _isDate = true;
        }

        public TimelineValue(double number) {
            _number = number;
            _date = default;
            _isDate = false;
        }

        public double ToAxisValue() => _isDate ? _date.ToOADate() : _number;
    }

    private static class EmptyAttributes {
        public static readonly IReadOnlyDictionary<string, string> Value = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
