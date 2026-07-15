using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Markup;

/// <summary>
/// Parses ChartForgeX flow markup into a product-neutral flow artifact.
/// </summary>
public sealed class MarkupFlowParser {
    /// <summary>
    /// Parses raw flow markup or Markdown containing a <c>chartforgex flow v1</c> fence.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<FlowArtifact> Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var scan = VisualMarkupScanner.Scan(text);
        var result = new MarkupParseResult<FlowArtifact>();
        foreach (var diagnostic in scan.Diagnostics) result.Diagnostics.Add(diagnostic);
        foreach (var block in scan.Blocks) {
            if (block.Kind == VisualMarkupKind.Flow) return ParseBlockCore(block, result);
        }

        if (result.Diagnostics.Count > 0) return result;
        return ParseBlockCore(CreateRawBlock(text), result);
    }

    /// <summary>
    /// Parses a pre-scanned ChartForgeX flow block while preserving fence attributes and source lines.
    /// </summary>
    /// <param name="block">The flow visual block.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<FlowArtifact> ParseBlock(VisualMarkupBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        var result = new MarkupParseResult<FlowArtifact>();
        if (block.Kind != VisualMarkupKind.Flow) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Expected a ChartForgeX flow visual block.");
            return result;
        }

        return ParseBlockCore(block, result);
    }

    private static MarkupParseResult<FlowArtifact> ParseBlockCore(VisualMarkupBlock block, MarkupParseResult<FlowArtifact> result) {
        var flow = FlowArtifact.Create("flow");
        result.Document = flow;
        if (block.SchemaVersion != 1) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "ChartForgeX flow markup requires schema version v1.");
        ApplyFenceAttributes(result, flow, block);

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

            if (section.Length > 0 && IsTableLine(line, tableHeaders)) {
                tableHeaders = ParseTableLine(result, flow, section, tableHeaders, line, lineNumber);
                continue;
            }

            ParseCommand(result, flow, line, lineNumber, section);
        }

        if (flow.Steps.Count == 0) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Flow markup must declare at least one step.");
        return result;
    }

    private static VisualMarkupBlock CreateRawBlock(string text) =>
        new(VisualMarkupKind.Flow, "chartforgex flow", string.Empty, 1, text, 1, 1, Math.Max(1, text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length), EmptyAttributes.Value);

    private static void ApplyFenceAttributes(MarkupParseResult<FlowArtifact> result, FlowArtifact flow, VisualMarkupBlock block) {
        try {
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) flow.Id = id;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) flow.Title = title;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) flow.Subtitle = subtitle;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "width", out var width) && !string.IsNullOrWhiteSpace(width)) flow.Width = ParsePositiveFiniteDouble(width, "width");
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "height", out var height) && !string.IsNullOrWhiteSpace(height)) flow.Height = ParsePositiveFiniteDouble(height, "height");
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "padding", out var padding) && !string.IsNullOrWhiteSpace(padding)) flow.Padding = ParseNonNegativeFiniteDouble(padding, "padding");
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "layout", out var layout) && !string.IsNullOrWhiteSpace(layout)) flow.LayoutMode = ParseLayout(layout);
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "direction", out var direction) && !string.IsNullOrWhiteSpace(direction)) flow.Direction = ParseDirection(direction);
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static void ParseCommand(MarkupParseResult<FlowArtifact> result, FlowArtifact flow, string line, int lineNumber, string section) {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;
        var command = NormalizeKey(tokens[0].TrimEnd(':'));

        if (section == "lanes" && command != "lane") {
            if (IsKnownFlowCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside lanes section.");
                return;
            }

            tokens.Insert(0, "lane");
            command = "lane";
        }

        if (section == "steps" && !IsStepCommand(command)) {
            if (IsKnownFlowCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside steps section.");
                return;
            }

            tokens.Insert(0, "step");
            command = "step";
        }

        if ((section == "connectors" || section == "links") && command != "connect" && command != "connector" && command != "link" && command != "edge") {
            if (IsKnownFlowCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside connectors section.");
                return;
            }

            tokens.Insert(0, "connect");
            command = "connect";
        }

        try {
            switch (command) {
                case "id":
                    RequireTokenCount(tokens, 2, "id");
                    flow.Id = tokens[1];
                    break;
                case "title":
                    flow.Title = JoinTail(tokens, 1);
                    break;
                case "subtitle":
                    flow.Subtitle = JoinTail(tokens, 1);
                    break;
                case "viewport":
                    ParseViewport(flow, tokens);
                    break;
                case "size":
                    RequireTokenCount(tokens, 2, "size");
                    ParseSize(flow, tokens[1]);
                    break;
                case "layout":
                    RequireTokenCount(tokens, 2, "layout");
                    flow.LayoutMode = ParseLayout(tokens[1]);
                    if (tokens.Count > 2) flow.Direction = ParseDirection(tokens[2]);
                    break;
                case "direction":
                    RequireTokenCount(tokens, 2, "direction");
                    flow.Direction = ParseDirection(tokens[1]);
                    break;
                case "lane":
                    ParseLane(flow, tokens);
                    break;
                case "step":
                    ParseStep(flow, tokens, FlowArtifactStepKind.Step);
                    break;
                case "process":
                case "decision":
                case "start":
                case "end":
                case "input":
                case "output":
                case "data":
                case "external":
                case "document":
                case "manual":
                case "delay":
                case "event":
                    ParseStep(flow, tokens, ParseStepKind(command));
                    break;
                case "connect":
                case "connector":
                case "link":
                case "edge":
                    ParseConnector(flow, tokens);
                    break;
                default:
                    Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown flow command '" + tokens[0] + "'.");
                    break;
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static List<string>? ParseTableLine(MarkupParseResult<FlowArtifact> result, FlowArtifact flow, string section, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) return cells.Select(NormalizeKey).ToList();

        try {
            var row = Row(headers, cells);
            if (section == "lanes") {
                var id = Required(row, "id");
                flow.AddLane(id, Value(row, "label", id), ParseStatus(Value(row, "status", string.Empty)), ValidatedColorOrNull(row, "Flow lane color"));
                return headers;
            }

            if (section == "steps") {
                var id = Required(row, "id");
                var kind = row.TryGetValue("kind", out var kindValue) || row.TryGetValue("type", out kindValue) ? ParseStepKind(kindValue) : FlowArtifactStepKind.Step;
                flow.AddStep(id, Value(row, "label", Value(row, "name", id)), kind, ValueOrNull(row, "lane"), ParseStatus(Value(row, "status", string.Empty)));
                ConfigureStep(flow.Steps[flow.Steps.Count - 1], row);
                return headers;
            }

            if (section == "connectors" || section == "links") {
                AddConnector(flow, Required(row, "from"), Required(row, "to"), Value(row, "label", string.Empty), row);
                return headers;
            }

            Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown flow table section '" + section + "'.");
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static void ParseLane(FlowArtifact flow, List<string> tokens) {
        RequireTokenCount(tokens, 2, "lane");
        var id = tokens[1];
        var label = tokens.Count > 2 && !IsAttribute(tokens[2]) ? tokens[2] : id;
        var attributeStart = label == id ? 2 : 3;
        var attributes = Attributes(tokens, attributeStart);
        flow.AddLane(id, label, ParseStatus(Value(attributes, "status", string.Empty)), ValidatedColorOrNull(attributes, "Flow lane color"));
    }

    private static void ParseStep(FlowArtifact flow, List<string> tokens, FlowArtifactStepKind defaultKind) {
        RequireTokenCount(tokens, 2, tokens[0]);
        var id = tokens[1];
        var label = tokens.Count > 2 && !IsAttribute(tokens[2]) ? tokens[2] : id;
        var attributeStart = label == id ? 2 : 3;
        var attributes = Attributes(tokens, attributeStart);
        var kind = attributes.TryGetValue("kind", out var kindValue) || attributes.TryGetValue("type", out kindValue) ? ParseStepKind(kindValue) : defaultKind;
        flow.AddStep(id, label, kind, ValueOrNull(attributes, "lane"), ParseStatus(Value(attributes, "status", string.Empty)));
        ConfigureStep(flow.Steps[flow.Steps.Count - 1], attributes);
    }

    private static void ConfigureStep(FlowArtifactStep step, Dictionary<string, string> attributes) {
        if (attributes.TryGetValue("subtitle", out var subtitle)) step.Subtitle = subtitle;
        if (attributes.TryGetValue("icon", out var icon)) step.Icon = icon;
        if (attributes.TryGetValue("symbol", out var symbol)) step.Symbol = symbol;
        if (attributes.TryGetValue("color", out var color)) {
            ValidateColor(color, "Flow step color");
            step.Color = color;
        }
        if (attributes.TryGetValue("badge", out var badge)) step.Badge = badge;
        if (attributes.TryGetValue("width", out var width)) step.Width = ParsePositiveFiniteDouble(width, "width");
        if (attributes.TryGetValue("height", out var height)) step.Height = ParsePositiveFiniteDouble(height, "height");
    }

    private static void ParseConnector(FlowArtifact flow, List<string> tokens) {
        RequireTokenCount(tokens, 3, tokens[0]);
        var source = tokens[1];
        var targetIndex = 2;
        if (tokens.Count > 3 && IsArrow(tokens[2])) targetIndex = 3;
        var target = tokens[targetIndex];
        var labelIndex = targetIndex + 1;
        var label = labelIndex < tokens.Count && !IsAttribute(tokens[labelIndex]) ? tokens[labelIndex] : string.Empty;
        var attributeStart = string.IsNullOrWhiteSpace(label) ? labelIndex : labelIndex + 1;
        AddConnector(flow, source, target, label, Attributes(tokens, attributeStart));
    }

    private static void AddConnector(FlowArtifact flow, string source, string target, string label, Dictionary<string, string> attributes) {
        var kind = attributes.TryGetValue("kind", out var kindValue) || attributes.TryGetValue("type", out kindValue) ? ParseConnectorKind(kindValue) : FlowArtifactConnectorKind.Flow;
        var direction = attributes.TryGetValue("direction", out var directionValue) ? ParseConnectorDirection(directionValue) : VisualLinkDirection.Forward;
        var status = ParseStatus(Value(attributes, "status", string.Empty));
        flow.AddConnector(source, target, label, kind, direction, status, ValidatedColorOrNull(attributes, "Flow connector color"));
    }

    private static void ParseViewport(FlowArtifact flow, List<string> tokens) {
        RequireTokenCount(tokens, 2, "viewport");
        ParseSize(flow, tokens[1]);
        if (tokens.Count > 2) flow.Padding = ParseNonNegativeFiniteDouble(tokens[2], "padding");
    }

    private static void ParseSize(FlowArtifact flow, string value) {
        var parts = value.Split(new[] { 'x', 'X', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) throw new ArgumentException("Flow size must use WIDTHxHEIGHT syntax.");
        flow.Width = ParsePositiveFiniteDouble(parts[0], "width");
        flow.Height = ParsePositiveFiniteDouble(parts[1], "height");
    }

    private static FlowArtifactLayoutMode ParseLayout(string value) {
        switch (NormalizeKey(value)) {
            case "layered":
            case "layer":
                return FlowArtifactLayoutMode.Layered;
            case "dense":
            case "densegrouped":
                return FlowArtifactLayoutMode.Dense;
            case "force":
            case "forcedirected":
                return FlowArtifactLayoutMode.Force;
            default:
                throw new ArgumentException("Unknown flow layout '" + value + "'.");
        }
    }

    private static FlowArtifactDirection ParseDirection(string value) {
        switch (NormalizeKey(value)) {
            case "lr":
            case "leftright":
            case "lefttoright":
                return FlowArtifactDirection.LeftToRight;
            case "tb":
            case "td":
            case "topbottom":
            case "toptobottom":
                return FlowArtifactDirection.TopToBottom;
            case "rl":
            case "rightleft":
            case "righttoleft":
                return FlowArtifactDirection.RightToLeft;
            case "bt":
            case "bottomtop":
            case "bottomtotop":
                return FlowArtifactDirection.BottomToTop;
            default:
                throw new ArgumentException("Unknown flow direction '" + value + "'.");
        }
    }

    private static FlowArtifactStepKind ParseStepKind(string value) {
        switch (NormalizeKey(value)) {
            case "step": return FlowArtifactStepKind.Step;
            case "process": return FlowArtifactStepKind.Process;
            case "decision": return FlowArtifactStepKind.Decision;
            case "start": return FlowArtifactStepKind.Start;
            case "end": return FlowArtifactStepKind.End;
            case "input": return FlowArtifactStepKind.Input;
            case "output": return FlowArtifactStepKind.Output;
            case "data": return FlowArtifactStepKind.Data;
            case "external": return FlowArtifactStepKind.External;
            case "document": return FlowArtifactStepKind.Document;
            case "manual": return FlowArtifactStepKind.Manual;
            case "delay": return FlowArtifactStepKind.Delay;
            case "event": return FlowArtifactStepKind.Event;
            default:
                throw new ArgumentException("Unknown flow step kind '" + value + "'.");
        }
    }

    private static FlowArtifactConnectorKind ParseConnectorKind(string value) {
        switch (NormalizeKey(value)) {
            case "flow": return FlowArtifactConnectorKind.Flow;
            case "dependency": return FlowArtifactConnectorKind.Dependency;
            case "data": return FlowArtifactConnectorKind.Data;
            case "reject":
            case "rejection": return FlowArtifactConnectorKind.Rejection;
            case "retry": return FlowArtifactConnectorKind.Retry;
            case "error": return FlowArtifactConnectorKind.Error;
            case "async":
            case "asynchronous": return FlowArtifactConnectorKind.Async;
            default:
                throw new ArgumentException("Unknown flow connector kind '" + value + "'.");
        }
    }

    private static VisualLinkDirection ParseConnectorDirection(string value) {
        switch (NormalizeKey(value)) {
            case "none": return VisualLinkDirection.None;
            case "forward":
            case "to": return VisualLinkDirection.Forward;
            case "back":
            case "backward": return VisualLinkDirection.Backward;
            case "both":
            case "bidirectional": return VisualLinkDirection.Bidirectional;
            default:
                throw new ArgumentException("Unknown flow connector direction '" + value + "'.");
        }
    }

    private static VisualStatus ParseStatus(string value) {
        if (string.IsNullOrWhiteSpace(value)) return VisualStatus.None;
        switch (NormalizeKey(value)) {
            case "ok":
            case "healthy":
            case "success":
            case "pass":
            case "passed":
            case "online":
            case "ready":
            case "positive":
                return VisualStatus.Positive;
            case "warn":
            case "warning":
            case "attention":
            case "degraded":
            case "partial":
                return VisualStatus.Warning;
            case "error":
            case "failed":
            case "fail":
            case "critical":
            case "down":
            case "offline":
            case "negative":
                return VisualStatus.Negative;
            case "info":
            case "note":
            case "pending":
            case "unknown":
                return VisualStatus.Info;
            case "neutral":
            case "disabled":
                return VisualStatus.Neutral;
            default:
                return VisualStatus.Neutral;
        }
    }

    private static bool IsKnownFlowCommand(string command) =>
        command == "id" || command == "title" || command == "subtitle" || command == "viewport" || command == "size" || command == "layout" || command == "direction" || command == "lane" || IsStepCommand(command) || command == "connect" || command == "connector" || command == "link" || command == "edge";

    private static bool IsStepCommand(string command) =>
        command == "step" || command == "process" || command == "decision" || command == "start" || command == "end" || command == "input" || command == "output" || command == "data" || command == "external" || command == "document" || command == "manual" || command == "delay" || command == "event";

    private static bool IsArrow(string value) => value == "->" || value == "-->" || value == "=>" || value == "--";

    private static bool IsAttribute(string token) => AttributeSplitIndex(token) > 0;

    private static Dictionary<string, string> Attributes(List<string> tokens, int start) {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = start; i < tokens.Count; i++) {
            var split = AttributeSplitIndex(tokens[i]);
            if (split <= 0) continue;
            attributes[NormalizeKey(tokens[i].Substring(0, split))] = tokens[i].Substring(split + 1);
        }

        return attributes;
    }

    private static int AttributeSplitIndex(string token) {
        var colon = token.IndexOf(':');
        var equals = token.IndexOf('=');
        if (colon <= 0) return equals;
        if (equals <= 0) return colon;
        return Math.Min(colon, equals);
    }

    private static bool IsSection(string line) {
        var value = NormalizeKey(line.TrimEnd(':'));
        return line.EndsWith(":", StringComparison.Ordinal) && (value == "lanes" || value == "steps" || value == "connectors" || value == "links");
    }

    private static bool IsSectionEnd(string line) => string.Equals(line, "end", StringComparison.OrdinalIgnoreCase);

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
        throw new ArgumentException("Flow row requires '" + key + "'.");
    }

    private static string Value(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static string? ValueOrNull(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string? ValidatedColorOrNull(Dictionary<string, string> values, string name) {
        var color = ValueOrNull(values, "color");
        ValidateColor(color, name);
        return color;
    }

    private static void ValidateColor(string? value, string name) {
        if (string.IsNullOrWhiteSpace(value)) return;
        try {
            ChartColor.FromHex(value!);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            throw new ArgumentException(name + " must be a valid hex color.", ex);
        }
    }

    private static double ParsePositiveFiniteDouble(string value, string name) {
        var parsed = VisualMarkupFenceOptions.ParseDouble(value, name);
        if (double.IsNaN(parsed) || double.IsInfinity(parsed) || parsed <= 0) throw new ArgumentException("Flow " + name + " must be a positive finite number.");
        return parsed;
    }

    private static double ParseNonNegativeFiniteDouble(string value, string name) {
        var parsed = VisualMarkupFenceOptions.ParseDouble(value, name);
        if (double.IsNaN(parsed) || double.IsInfinity(parsed) || parsed < 0) throw new ArgumentException("Flow " + name + " must be a non-negative finite number.");
        return parsed;
    }

    private static void RequireTokenCount(List<string> tokens, int count, string command) {
        if (tokens.Count < count) throw new ArgumentException("Flow command '" + command + "' requires more values.");
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

    private static string NormalizeKey(string value) {
        var chars = new List<char>(value.Length);
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch)) chars.Add(char.ToLowerInvariant(ch));
        }

        return new string(chars.ToArray());
    }

    private static void Add<TDocument>(MarkupParseResult<TDocument> result, int line, MarkupDiagnosticSeverity severity, string message) where TDocument : class =>
        result.Diagnostics.Add(new MarkupDiagnostic { Line = line, Severity = severity, Message = message });

    private static class EmptyAttributes {
        public static readonly IReadOnlyDictionary<string, string> Value = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
