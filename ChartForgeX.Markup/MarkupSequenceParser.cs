using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Markup;

/// <summary>
/// Parses ChartForgeX sequence markup into a product-neutral sequence artifact.
/// </summary>
public sealed class MarkupSequenceParser {
    /// <summary>
    /// Parses raw sequence markup or Markdown containing a <c>chartforgex sequence v1</c> fence.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<SequenceArtifact> Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var scan = VisualMarkupScanner.Scan(text);
        var result = new MarkupParseResult<SequenceArtifact>();
        foreach (var diagnostic in scan.Diagnostics) result.Diagnostics.Add(diagnostic);
        foreach (var block in scan.Blocks) {
            if (block.Kind == VisualMarkupKind.Sequence) return ParseBlockCore(block, result);
        }

        if (result.Diagnostics.Count > 0) return result;
        return ParseBlockCore(CreateRawBlock(text), result);
    }

    /// <summary>
    /// Parses a pre-scanned ChartForgeX sequence block while preserving fence attributes and source lines.
    /// </summary>
    /// <param name="block">The sequence visual block.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<SequenceArtifact> ParseBlock(VisualMarkupBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        var result = new MarkupParseResult<SequenceArtifact>();
        if (block.Kind != VisualMarkupKind.Sequence) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Expected a ChartForgeX sequence visual block.");
            return result;
        }

        return ParseBlockCore(block, result);
    }

    private static MarkupParseResult<SequenceArtifact> ParseBlockCore(VisualMarkupBlock block, MarkupParseResult<SequenceArtifact> result) {
        var sequence = SequenceArtifact.Create("sequence");
        result.Document = sequence;
        if (block.SchemaVersion != 1) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "ChartForgeX sequence markup requires schema version v1.");
        ApplyFenceAttributes(result, sequence, block);

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
                tableHeaders = ParseTableLine(result, sequence, section, tableHeaders, line, lineNumber);
                continue;
            }

            ParseCommand(result, sequence, line, lineNumber, section);
        }

        if (sequence.Participants.Count == 0) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Sequence markup must declare at least one participant.");
        if (sequence.Messages.Count == 0 && sequence.Notes.Count == 0) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Sequence markup must declare at least one message or note.");
        return result;
    }

    private static VisualMarkupBlock CreateRawBlock(string text) =>
        new(VisualMarkupKind.Sequence, "chartforgex sequence", string.Empty, 1, text, 1, 1, Math.Max(1, text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length), EmptyAttributes.Value);

    private static void ApplyFenceAttributes(MarkupParseResult<SequenceArtifact> result, SequenceArtifact sequence, VisualMarkupBlock block) {
        try {
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) sequence.Id = id;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) sequence.Title = title;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) sequence.Subtitle = subtitle;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "width", out var width) && !string.IsNullOrWhiteSpace(width)) sequence.Width = VisualMarkupFenceOptions.ParseDouble(width, "width");
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "height", out var height) && !string.IsNullOrWhiteSpace(height)) sequence.Height = VisualMarkupFenceOptions.ParseDouble(height, "height");
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "padding", out var padding) && !string.IsNullOrWhiteSpace(padding)) sequence.Padding = VisualMarkupFenceOptions.ParseDouble(padding, "padding");
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static void ParseCommand(MarkupParseResult<SequenceArtifact> result, SequenceArtifact sequence, string line, int lineNumber, string section) {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;
        var command = NormalizeKey(tokens[0].TrimEnd(':'));

        if (section == "participants" && !IsParticipantCommand(command)) {
            if (IsKnownSequenceCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside participants section.");
                return;
            }

            tokens.Insert(0, "participant");
            command = "participant";
        }

        if (section == "messages" && command != "message") {
            if (IsKnownSequenceCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside messages section.");
                return;
            }

            tokens.Insert(0, "message");
            command = "message";
        }

        if (section == "notes" && command != "note") {
            if (IsKnownSequenceCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside notes section.");
                return;
            }

            tokens.Insert(0, "note");
            command = "note";
        }

        if (section == "blocks" && command != "block") {
            if (IsKnownSequenceCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside blocks section.");
                return;
            }

            tokens.Insert(0, "block");
            command = "block";
        }

        try {
            switch (command) {
                case "id":
                    RequireTokenCount(tokens, 2, "id");
                    sequence.Id = tokens[1];
                    break;
                case "title":
                    sequence.Title = JoinTail(tokens, 1);
                    break;
                case "subtitle":
                    sequence.Subtitle = JoinTail(tokens, 1);
                    break;
                case "viewport":
                    ParseViewport(sequence, tokens);
                    break;
                case "size":
                    RequireTokenCount(tokens, 2, "size");
                    ParseSize(sequence, tokens[1]);
                    break;
                case "participant":
                    ParseParticipant(sequence, tokens, SequenceArtifactParticipantKind.Participant);
                    break;
                case "actor":
                case "boundary":
                case "control":
                case "entity":
                case "database":
                case "collections":
                case "queue":
                    ParseParticipant(sequence, tokens, ParseParticipantKind(command));
                    break;
                case "message":
                case "msg":
                    ParseMessage(sequence, tokens);
                    break;
                case "note":
                    ParseNote(sequence, tokens);
                    break;
                case "block":
                    ParseBlock(sequence, tokens);
                    break;
                default:
                    Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown sequence command '" + tokens[0] + "'.");
                    break;
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static List<string>? ParseTableLine(MarkupParseResult<SequenceArtifact> result, SequenceArtifact sequence, string section, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) return cells.Select(NormalizeKey).ToList();

        try {
            var row = Row(headers, cells);
            if (section == "participants") {
                var id = Required(row, "id");
                var kind = row.TryGetValue("kind", out var kindValue) || row.TryGetValue("type", out kindValue) ? ParseParticipantKind(kindValue) : SequenceArtifactParticipantKind.Participant;
                sequence.AddParticipant(id, Value(row, "label", Value(row, "name", id)), kind);
                return headers;
            }

            if (section == "messages") {
                AddMessage(sequence, Required(row, "from"), Required(row, "to"), Value(row, "text", Value(row, "label", string.Empty)), row);
                return headers;
            }

            if (section == "notes") {
                AddNote(sequence, Value(row, "placement", "rightOf"), Value(row, "participants", Value(row, "participant", string.Empty)), Required(row, "text"), OptionalInt(row, "step"));
                return headers;
            }

            if (section == "blocks") {
                sequence.AddBlock(ParseBlockKind(Value(row, "kind", Value(row, "type", "loop"))), Value(row, "text", Value(row, "label", string.Empty)), Int(row, "start", 0), Int(row, "end", 0));
                return headers;
            }

            Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown sequence table section '" + section + "'.");
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static void ParseParticipant(SequenceArtifact sequence, List<string> tokens, SequenceArtifactParticipantKind defaultKind) {
        RequireTokenCount(tokens, 2, tokens[0]);
        var id = tokens[1];
        var label = tokens.Count > 2 && !IsAttribute(tokens[2]) ? tokens[2] : id;
        var attributeStart = label == id ? 2 : 3;
        var attributes = Attributes(tokens, attributeStart);
        var kind = attributes.TryGetValue("kind", out var kindValue) || attributes.TryGetValue("type", out kindValue) ? ParseParticipantKind(kindValue) : defaultKind;
        sequence.AddParticipant(id, label, kind);
    }

    private static void ParseMessage(SequenceArtifact sequence, List<string> tokens) {
        RequireTokenCount(tokens, 3, tokens[0]);
        var source = tokens[1];
        var targetIndex = 2;
        if (IsArrow(tokens[2])) {
            if (tokens.Count <= 3) throw new ArgumentException("Sequence message arrow syntax requires a target participant.");
            targetIndex = 3;
        }

        var target = tokens[targetIndex];
        var textIndex = targetIndex + 1;
        var text = textIndex < tokens.Count && !IsAttribute(tokens[textIndex]) ? tokens[textIndex] : string.Empty;
        var attributeStart = string.IsNullOrWhiteSpace(text) ? textIndex : textIndex + 1;
        AddMessage(sequence, source, target, text, Attributes(tokens, attributeStart));
    }

    private static void AddMessage(SequenceArtifact sequence, string source, string target, string text, Dictionary<string, string> attributes) {
        var lineStyle = attributes.TryGetValue("style", out var style) || attributes.TryGetValue("line", out style) ? ParseLineStyle(style) : SequenceArtifactMessageLineStyle.Solid;
        sequence.AddMessage(source, target, text, lineStyle);
        var message = sequence.Messages[sequence.Messages.Count - 1];
        if (attributes.TryGetValue("activate", out var activate)) message.ActivatesTarget = VisualMarkupFenceOptions.ParseBoolean(activate, "activate");
        if (attributes.TryGetValue("deactivate", out var deactivate)) message.Deactivates = VisualMarkupFenceOptions.ParseBoolean(deactivate, "deactivate");
    }

    private static void ParseNote(SequenceArtifact sequence, List<string> tokens) {
        RequireTokenCount(tokens, 4, "note");
        var placement = tokens[1];
        var participants = tokens[2];
        var text = tokens[3];
        var attributes = Attributes(tokens, 4);
        AddNote(sequence, placement, participants, text, attributes.TryGetValue("step", out var step) ? int.Parse(step, CultureInfo.InvariantCulture) : (int?)null);
    }

    private static void AddNote(SequenceArtifact sequence, string placement, string participants, string text, int? stepIndex) {
        var ids = SplitIds(participants);
        sequence.AddNote(ParseNotePlacement(placement), ids, text);
        if (stepIndex.HasValue) sequence.Notes[sequence.Notes.Count - 1].StepIndex = stepIndex.Value;
    }

    private static void ParseBlock(SequenceArtifact sequence, List<string> tokens) {
        RequireTokenCount(tokens, 5, "block");
        sequence.AddBlock(ParseBlockKind(tokens[1]), tokens[2], int.Parse(tokens[3], CultureInfo.InvariantCulture), int.Parse(tokens[4], CultureInfo.InvariantCulture));
    }

    private static void ParseViewport(SequenceArtifact sequence, List<string> tokens) {
        RequireTokenCount(tokens, 2, "viewport");
        ParseSize(sequence, tokens[1]);
        if (tokens.Count > 2) sequence.Padding = VisualMarkupFenceOptions.ParseDouble(tokens[2], "padding");
    }

    private static void ParseSize(SequenceArtifact sequence, string value) {
        var parts = value.Split(new[] { 'x', 'X', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) throw new ArgumentException("Sequence size must use WIDTHxHEIGHT syntax.");
        sequence.Width = double.Parse(parts[0], CultureInfo.InvariantCulture);
        sequence.Height = double.Parse(parts[1], CultureInfo.InvariantCulture);
    }

    private static SequenceArtifactParticipantKind ParseParticipantKind(string value) {
        switch (NormalizeKey(value)) {
            case "participant": return SequenceArtifactParticipantKind.Participant;
            case "actor": return SequenceArtifactParticipantKind.Actor;
            case "boundary": return SequenceArtifactParticipantKind.Boundary;
            case "control": return SequenceArtifactParticipantKind.Control;
            case "entity": return SequenceArtifactParticipantKind.Entity;
            case "database":
            case "db": return SequenceArtifactParticipantKind.Database;
            case "collections":
            case "collection": return SequenceArtifactParticipantKind.Collections;
            case "queue": return SequenceArtifactParticipantKind.Queue;
            default:
                throw new ArgumentException("Unknown sequence participant kind '" + value + "'.");
        }
    }

    private static SequenceArtifactMessageLineStyle ParseLineStyle(string value) {
        switch (NormalizeKey(value)) {
            case "solid": return SequenceArtifactMessageLineStyle.Solid;
            case "dash":
            case "dashed": return SequenceArtifactMessageLineStyle.Dashed;
            default:
                throw new ArgumentException("Unknown sequence message line style '" + value + "'.");
        }
    }

    private static SequenceArtifactNotePlacement ParseNotePlacement(string value) {
        switch (NormalizeKey(value)) {
            case "left":
            case "leftof": return SequenceArtifactNotePlacement.LeftOf;
            case "over":
            case "above": return SequenceArtifactNotePlacement.Over;
            case "right":
            case "rightof": return SequenceArtifactNotePlacement.RightOf;
            default:
                throw new ArgumentException("Unknown sequence note placement '" + value + "'.");
        }
    }

    private static SequenceArtifactBlockKind ParseBlockKind(string value) {
        switch (NormalizeKey(value)) {
            case "loop": return SequenceArtifactBlockKind.Loop;
            case "alt": return SequenceArtifactBlockKind.Alt;
            case "opt":
            case "optional": return SequenceArtifactBlockKind.Opt;
            case "par":
            case "parallel": return SequenceArtifactBlockKind.Par;
            case "critical": return SequenceArtifactBlockKind.Critical;
            case "rect":
            case "highlight": return SequenceArtifactBlockKind.Rect;
            case "break": return SequenceArtifactBlockKind.Break;
            default:
                throw new ArgumentException("Unknown sequence block kind '" + value + "'.");
        }
    }

    private static bool IsKnownSequenceCommand(string command) =>
        command == "id" || command == "title" || command == "subtitle" || command == "viewport" || command == "size" || IsParticipantCommand(command) || command == "message" || command == "msg" || command == "note" || command == "block";

    private static bool IsParticipantCommand(string command) =>
        command == "participant" || command == "actor" || command == "boundary" || command == "control" || command == "entity" || command == "database" || command == "collections" || command == "queue";

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
        return line.EndsWith(":", StringComparison.Ordinal) && (value == "participants" || value == "messages" || value == "notes" || value == "blocks");
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
        throw new ArgumentException("Sequence row requires '" + key + "'.");
    }

    private static string Value(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static int Int(Dictionary<string, string> values, string key, int fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;

    private static int? OptionalInt(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? int.Parse(value, CultureInfo.InvariantCulture) : (int?)null;

    private static List<string> SplitIds(string value) =>
        value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).Where(part => part.Length > 0).ToList();

    private static void RequireTokenCount(List<string> tokens, int count, string command) {
        if (tokens.Count < count) throw new ArgumentException("Sequence command '" + command + "' requires more values.");
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
