using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Markup;

/// <summary>
/// Parses ChartForgeX topology markup.
/// </summary>
public sealed class MarkupTopologyParser {
    /// <summary>
    /// Parses raw topology markup or Markdown containing a chartforgex topology fence.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<MarkupTopologyDocument> Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var block = ChartForgeXMarkdown.ExtractFirstTopologyBlock(text);
        var payload = block.Payload;
        var lineOffset = block.StartLine - 1;
        var result = new MarkupParseResult<MarkupTopologyDocument> { Document = new MarkupTopologyDocument() };
        var lines = payload.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var section = string.Empty;
        List<string>? tableHeaders = null;

        for (var index = 0; index < lines.Length; index++) {
            var lineNumber = lineOffset + index + 1;
            var line = StripComment(lines[index]).Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (IsSection(line)) {
                section = line.TrimEnd(':').ToLowerInvariant();
                tableHeaders = null;
                continue;
            }

            if (section.Length > 0 && IsSectionEnd(line)) {
                section = string.Empty;
                tableHeaders = null;
                continue;
            }

            if (section.Length > 0 && IsTableLine(line, tableHeaders)) {
                tableHeaders = ParseTableLine(result, result.Document!, section, tableHeaders, line, lineNumber);
                continue;
            }

            ParseCommand(result, result.Document!, line, lineNumber, section);
        }

        if (result.Document!.Nodes.Count == 0) Add(result, 0, MarkupDiagnosticSeverity.Error, "Topology markup must declare at least one node.");
        return result;
    }

    private static void ParseCommand(MarkupParseResult<MarkupTopologyDocument> result, MarkupTopologyDocument document, string line, int lineNumber, string section) {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;
        var command = tokens[0].TrimEnd(':').ToLowerInvariant();

        if (section == "groups" && command != "group") {
            if (IsKnownTopologyCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside groups section.");
                return;
            }

            command = "group";
            tokens.Insert(0, command);
        }

        if (section == "nodes" && command != "node") {
            if (IsKnownTopologyCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside nodes section.");
                return;
            }

            command = "node";
            tokens.Insert(0, command);
        }

        if (section == "edges" && command != "edge") {
            if (IsKnownTopologyCommand(command)) {
                Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Command '" + command + "' cannot appear inside edges section.");
                return;
            }

            command = "edge";
            tokens.Insert(0, command);
        }

        try {
            switch (command) {
                case "id":
                    document.Id = tokens.Count > 1 ? tokens[1] : string.Empty;
                    break;
                case "title":
                    document.Title = JoinTail(tokens, 1);
                    break;
                case "subtitle":
                    document.Subtitle = JoinTail(tokens, 1);
                    break;
                case "viewport":
                    ParseViewport(document, tokens);
                    break;
                case "layout":
                    ParseLayout(document, tokens);
                    break;
                case "group":
                    ParseGroup(document, tokens, lineNumber);
                    break;
                case "node":
                    ParseNode(document, tokens, lineNumber);
                    break;
                case "edge":
                    ParseEdge(document, tokens, lineNumber, line);
                    break;
                default:
                    Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown topology command '" + tokens[0] + "'.");
                    break;
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is FormatException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static List<string>? ParseTableLine(MarkupParseResult<MarkupTopologyDocument> result, MarkupTopologyDocument document, string section, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) return cells.Select(cell => NormalizeKey(cell)).ToList();

        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count && i < cells.Count; i++) row[headers[i]] = cells[i];
        try {
            if (section == "groups") document.Groups.Add(new MarkupTopologyGroup {
                Id = Required(row, "id"),
                Label = Value(row, "label", Required(row, "id")),
                Status = ParseEnum<TopologyHealthStatus>(Value(row, "status", "unknown")),
                Subtitle = Optional(row, "subtitle"),
                Color = Optional(row, "color"),
                Icon = Optional(row, "icon"),
                Width = Number(row, "width", 260),
                Height = Number(row, "height", 160)
            });
            if (section == "nodes") document.Nodes.Add(new MarkupTopologyNode {
                Id = Required(row, "id"),
                Label = Value(row, "label", Required(row, "id")),
                Kind = ParseEnum<TopologyNodeKind>(Value(row, "kind", "generic")),
                Status = ParseEnum<TopologyHealthStatus>(Value(row, "status", "unknown")),
                Group = Optional(row, "group"),
                Subtitle = Optional(row, "subtitle"),
                Icon = Optional(row, "icon"),
                Symbol = Optional(row, "symbol"),
                Badge = Optional(row, "badge"),
                Color = Optional(row, "color"),
                Display = row.TryGetValue("display", out var display) && !string.IsNullOrWhiteSpace(display) ? ParseEnum<TopologyNodeDisplayMode>(display) : null,
                Width = Number(row, "width", 120),
                Height = Number(row, "height", 64)
            });
            if (section == "edges") AddEdge(document, Value(row, "id", string.Empty), Required(row, "from"), Required(row, "to"), Optional(row, "label"), row);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is InvalidOperationException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static void ParseViewport(MarkupTopologyDocument document, List<string> tokens) {
        if (tokens.Count < 2) throw new ArgumentException("Viewport requires a value like 1200x700.");
        var parts = tokens[1].Split('x', 'X');
        if (parts.Length != 2) throw new ArgumentException("Viewport requires a value like 1200x700.");
        document.Width = double.Parse(parts[0], CultureInfo.InvariantCulture);
        document.Height = double.Parse(parts[1], CultureInfo.InvariantCulture);
        if (tokens.Count > 2) document.Padding = double.Parse(tokens[2], CultureInfo.InvariantCulture);
    }

    private static void ParseLayout(MarkupTopologyDocument document, List<string> tokens) {
        if (tokens.Count < 2) throw new ArgumentException("Layout requires a mode.");
        document.LayoutMode = ParseEnum<TopologyLayoutMode>(tokens[1]);
        if (tokens.Count > 2) document.LayoutDirection = ParseDirection(tokens[2]);
    }

    private static void ParseGroup(MarkupTopologyDocument document, List<string> tokens, int lineNumber) {
        if (tokens.Count < 3) throw new ArgumentException("Group on line " + lineNumber.ToString(CultureInfo.InvariantCulture) + " requires id and label.");
        var attributes = Attributes(tokens, 3);
        document.Groups.Add(new MarkupTopologyGroup {
            Id = tokens[1],
            Label = tokens[2],
            Status = ParseEnum<TopologyHealthStatus>(Value(attributes, "status", "unknown")),
            Subtitle = Optional(attributes, "subtitle"),
            Color = Optional(attributes, "color"),
            Icon = Optional(attributes, "icon"),
            Width = Number(attributes, "width", 260),
            Height = Number(attributes, "height", 160)
        });
    }

    private static void ParseNode(MarkupTopologyDocument document, List<string> tokens, int lineNumber) {
        if (tokens.Count < 3) throw new ArgumentException("Node on line " + lineNumber.ToString(CultureInfo.InvariantCulture) + " requires id and label.");
        var attributes = Attributes(tokens, 3);
        document.Nodes.Add(new MarkupTopologyNode {
            Id = tokens[1],
            Label = tokens[2],
            Kind = ParseEnum<TopologyNodeKind>(Value(attributes, "kind", "generic")),
            Status = ParseEnum<TopologyHealthStatus>(Value(attributes, "status", "unknown")),
            Group = Optional(attributes, "group"),
            Subtitle = Optional(attributes, "subtitle"),
            Icon = Optional(attributes, "icon"),
            Symbol = Optional(attributes, "symbol"),
            Badge = Optional(attributes, "badge"),
            Color = Optional(attributes, "color"),
            Display = attributes.TryGetValue("display", out var display) ? ParseEnum<TopologyNodeDisplayMode>(display) : null,
            Width = Number(attributes, "width", 120),
            Height = Number(attributes, "height", 64)
        });
    }

    private static void ParseEdge(MarkupTopologyDocument document, List<string> tokens, int lineNumber, string line) {
        if (tokens.Count < 4) throw new ArgumentException("Edge on line " + lineNumber.ToString(CultureInfo.InvariantCulture) + " requires source, arrow, and target.");
        if (tokens[2] != "->" && tokens[2] != "--") throw new ArgumentException("Edge arrow must be '->' or '--'.");
        var firstAttribute = 4;
        string? label = null;
        if (tokens.Count > 4 && (IsTokenQuoted(line, 4) || !IsAttribute(tokens[4]))) {
            label = tokens[4];
            firstAttribute = 5;
        }

        var defaultDirection = tokens[2] == "->" ? TopologyDirection.Forward : TopologyDirection.None;
        AddEdge(document, string.Empty, tokens[1], tokens[3], label, Attributes(tokens, firstAttribute), defaultDirection);
    }

    private static void AddEdge(MarkupTopologyDocument document, string id, string source, string target, string? label, Dictionary<string, string> attributes, TopologyDirection defaultDirection = TopologyDirection.None) {
        var edgeId = string.IsNullOrWhiteSpace(id) ? Value(attributes, "id", MakeEdgeId(document, source, target)) : id;
        document.Edges.Add(new MarkupTopologyEdge {
            Id = edgeId,
            Source = source,
            Target = target,
            Label = label,
            Kind = ParseEnum<TopologyEdgeKind>(Value(attributes, "kind", "dependency")),
            Status = ParseEnum<TopologyHealthStatus>(Value(attributes, "status", "unknown")),
            Direction = attributes.TryGetValue("direction", out var direction) && !string.IsNullOrWhiteSpace(direction) ? ParseEnum<TopologyDirection>(direction) : defaultDirection,
            Routing = ParseEnum<TopologyEdgeRouting>(Value(attributes, "routing", "orthogonal"))
        });
    }

    private static Dictionary<string, string> Attributes(List<string> tokens, int start) {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = start; i < tokens.Count; i++) {
            var token = tokens[i];
            var split = token.IndexOf(':');
            if (split <= 0 || !IsKnownAttribute(token.Substring(0, split))) continue;
            attributes[NormalizeKey(token.Substring(0, split))] = token.Substring(split + 1);
        }

        return attributes;
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

    private static bool IsTokenQuoted(string line, int tokenIndex) {
        var currentToken = 0;
        var inQuote = false;
        var inToken = false;
        var tokenQuoted = false;
        for (var i = 0; i < line.Length; i++) {
            var ch = line[i];
            if (ch == '"') {
                if (!inToken) {
                    inToken = true;
                    tokenQuoted = true;
                }

                inQuote = !inQuote;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuote) {
                if (inToken) {
                    if (currentToken == tokenIndex) return tokenQuoted;
                    currentToken++;
                    inToken = false;
                    tokenQuoted = false;
                }

                continue;
            }

            if (!inToken) {
                inToken = true;
                tokenQuoted = inQuote;
            }
        }

        return inToken && currentToken == tokenIndex && tokenQuoted;
    }

    private static TEnum ParseEnum<TEnum>(string value) where TEnum : struct {
        var normalized = NormalizeKey(value);
        foreach (var name in Enum.GetNames(typeof(TEnum))) {
            if (NormalizeKey(name) == normalized) return (TEnum)Enum.Parse(typeof(TEnum), name, false);
        }

        throw new ArgumentException("Unknown " + typeof(TEnum).Name + " value '" + value + "'.");
    }

    private static TopologyLayoutDirection ParseDirection(string value) {
        switch (NormalizeKey(value)) {
            case "lr":
            case "lefttoright":
                return TopologyLayoutDirection.LeftToRight;
            case "rl":
            case "righttoleft":
                return TopologyLayoutDirection.RightToLeft;
            case "bt":
            case "bottomtotop":
                return TopologyLayoutDirection.BottomToTop;
            case "tb":
            case "toptobottom":
                return TopologyLayoutDirection.TopToBottom;
            default:
                return ParseEnum<TopologyLayoutDirection>(value);
        }
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
        if (line.StartsWith("|", StringComparison.Ordinal)) return true;
        if (!HasUnquotedPipe(line)) return false;
        var firstToken = Tokenize(line).FirstOrDefault()?.TrimEnd(':').ToLowerInvariant();
        if (firstToken != null && IsTopologyEntryCommand(firstToken)) return false;
        var cells = SplitTableCells(line);
        if (cells.Count < 2) return false;
        if (headers != null) return true;
        return cells.All(cell => IsKnownTableHeader(cell));
    }

    private static bool HasUnquotedPipe(string line) {
        var inQuote = false;
        for (var i = 0; i < line.Length; i++) {
            var ch = line[i];
            if (ch == '"') inQuote = !inQuote;
            if (ch == '\\' && i + 1 < line.Length) {
                i++;
                continue;
            }

            if (!inQuote && ch == '|') return true;
        }

        return false;
    }

    private static bool IsTableSeparator(List<string> cells) {
        foreach (var cell in cells) {
            var value = cell.Trim();
            if (value.Length == 0) continue;
            var hyphenCount = 0;
            foreach (var ch in value) {
                if (ch == '-') {
                    hyphenCount++;
                    continue;
                }

                if (ch != ':' && !char.IsWhiteSpace(ch)) return false;
            }

            if (hyphenCount == 0) return false;
        }

        return true;
    }

    private static bool IsSection(string line) {
        var normalized = line.TrimEnd(':').ToLowerInvariant();
        return normalized == "groups" || normalized == "nodes" || normalized == "edges";
    }

    private static bool IsSectionEnd(string line) => string.Equals(line, "end", StringComparison.OrdinalIgnoreCase);
    private static bool IsAttribute(string token) {
        var split = token.IndexOf(':');
        return split > 0 && IsKnownAttribute(token.Substring(0, split));
    }

    private static bool IsKnownAttribute(string key) {
        switch (NormalizeKey(key)) {
            case "id":
            case "kind":
            case "status":
            case "direction":
            case "routing":
            case "group":
            case "subtitle":
            case "icon":
            case "symbol":
            case "badge":
            case "color":
            case "display":
            case "width":
            case "height":
                return true;
            default:
                return false;
        }
    }

    private static bool IsKnownTableHeader(string key) {
        switch (NormalizeKey(key)) {
            case "id":
            case "from":
            case "to":
            case "label":
            case "kind":
            case "status":
            case "direction":
            case "routing":
            case "group":
            case "subtitle":
            case "icon":
            case "symbol":
            case "badge":
            case "color":
            case "display":
            case "width":
            case "height":
                return true;
            default:
                return false;
        }
    }

    private static bool IsTopologyEntryCommand(string command) => command == "group" || command == "node" || command == "edge";
    private static bool IsKnownTopologyCommand(string command) => command == "id" || command == "title" || command == "subtitle" || command == "viewport" || command == "layout" || IsTopologyEntryCommand(command);
    private static string JoinTail(List<string> tokens, int start) => start >= tokens.Count ? string.Empty : string.Join(" ", tokens.Skip(start));
    private static string NormalizeKey(string value) => new string((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    private static string? Optional(Dictionary<string, string> row, string key) => row.TryGetValue(NormalizeKey(key), out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    private static string Value(Dictionary<string, string> row, string key, string fallback) => row.TryGetValue(NormalizeKey(key), out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    private static string Required(Dictionary<string, string> row, string key) => Optional(row, key) ?? throw new ArgumentException("Missing required '" + key + "' column.");
    private static double Number(Dictionary<string, string> row, string key, double fallback) => row.TryGetValue(NormalizeKey(key), out var value) && !string.IsNullOrWhiteSpace(value) ? double.Parse(value, CultureInfo.InvariantCulture) : fallback;
    private static void Add<TDocument>(MarkupParseResult<TDocument> result, int line, MarkupDiagnosticSeverity severity, string message) where TDocument : class => result.Diagnostics.Add(new MarkupDiagnostic { Line = line, Severity = severity, Message = message });

    private static List<string> SplitTableCells(string line) {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("|", StringComparison.Ordinal)) trimmed = trimmed.Substring(1);
        if (EndsWithUnescapedPipe(trimmed)) trimmed = trimmed.Substring(0, trimmed.Length - 1);
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        for (var i = 0; i < trimmed.Length; i++) {
            var ch = trimmed[i];
            if (ch == '\\' && i + 1 < trimmed.Length && trimmed[i + 1] == '|') {
                current.Append('|');
                i++;
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

    private static bool EndsWithUnescapedPipe(string value) {
        if (!value.EndsWith("|", StringComparison.Ordinal)) return false;
        var slashCount = 0;
        for (var i = value.Length - 2; i >= 0 && value[i] == '\\'; i--) slashCount++;
        return slashCount % 2 == 0;
    }

    private static string MakeEdgeId(MarkupTopologyDocument document, string source, string target) {
        var baseId = NormalizeId(source + "-" + target);
        var id = baseId;
        var index = 2;
        while (document.Edges.Any(edge => string.Equals(edge.Id, id, StringComparison.Ordinal))) {
            id = baseId + "-" + index.ToString(CultureInfo.InvariantCulture);
            index++;
        }

        return id;
    }

    private static string NormalizeId(string value) {
        var chars = value.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-').ToArray();
        return new string(chars).Trim('-');
    }
}
