using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace ChartForgeX.Markup;

/// <summary>
/// Parses ChartForgeX architecture markup: typed nodes, groups and boundaries, labelled connections, and use cases
/// whose ordered steps carry request and response payloads. The result is a topology chart whose scenarios drive
/// playback, the matching sequence diagram, per-step renders, and terminal views.
/// </summary>
public sealed partial class MarkupArchitectureParser {
    /// <summary>
    /// Parses raw architecture markup or Markdown containing a <c>chartforgex architecture v1</c> fence.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The parse result. The chart is set when parsing produced no errors.</returns>
    public MarkupParseResult<TopologyChart> Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var scan = VisualMarkupScanner.Scan(text);
        var result = new MarkupParseResult<TopologyChart>();
        foreach (var diagnostic in scan.Diagnostics) result.Diagnostics.Add(diagnostic);
        foreach (var block in scan.Blocks) {
            if (block.Kind == VisualMarkupKind.Architecture) return ParseCore(block, result);
        }

        if (scan.Blocks.Count > 0 || result.Diagnostics.Count > 0) {
            if (!result.HasErrors) Add(result, 1, 1, MarkupArchitectureDiagnosticCodes.InvalidStatement, MarkupDiagnosticSeverity.Error, "The text has no chartforgex architecture fence.");
            return result;
        }

        var lines = Math.Max(1, text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length);
        var raw = new VisualMarkupBlock(VisualMarkupKind.Architecture, "chartforgex architecture", string.Empty, 1, text, 1, 1, lines, new Dictionary<string, string>());
        return ParseCore(raw, result);
    }

    /// <summary>
    /// Parses a pre-scanned architecture block while preserving fence attributes and source lines.
    /// </summary>
    /// <param name="block">The architecture visual block.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<TopologyChart> ParseBlock(VisualMarkupBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        var result = new MarkupParseResult<TopologyChart>();
        if (block.Kind != VisualMarkupKind.Architecture) {
            Add(result, block.FenceLine, 1, MarkupArchitectureDiagnosticCodes.InvalidStatement, MarkupDiagnosticSeverity.Error, "Expected a ChartForgeX architecture visual block.");
            return result;
        }

        return ParseCore(block, result);
    }

    private static MarkupParseResult<TopologyChart> ParseCore(VisualMarkupBlock block, MarkupParseResult<TopologyChart> result) {
        var state = new ParseState(result, block);
        if (block.SchemaVersion != 1) state.Error(block.FenceLine, 1, MarkupArchitectureDiagnosticCodes.InvalidStatement, "ChartForgeX architecture markup requires schema version v1.");
        ApplyFenceAttributes(state, block);
        var lines = block.Payload.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var index = 0; index < lines.Length; index++) {
            var lineNumber = block.StartLine + index;
            var text = MarkupArchitectureLine.StripComment(lines[index]);
            if (string.IsNullOrWhiteSpace(text)) continue;
            var tokens = MarkupArchitectureLine.Tokenize(text, out var tokenError);
            if (tokenError != null) {
                state.Error(lineNumber, 1, MarkupArchitectureDiagnosticCodes.InvalidStatement, tokenError);
                continue;
            }

            if (tokens.Count == 0) continue;
            if (state.InUseCase) ParseUseCaseLine(state, tokens, text, lines, ref index, lineNumber);
            else ParseStatement(state, tokens, lineNumber);
        }

        foreach (var frame in state.Frames) {
            state.Error(frame.Line, 1, MarkupArchitectureDiagnosticCodes.UnclosedBlock, (frame.UseCase != null ? "Use case '" : "Group '") + frame.Id + "' is not closed with 'end'.");
        }

        state.Complete();
        result.Document = result.HasErrors ? null : state.Chart;
        return result;
    }

    private static void ApplyFenceAttributes(ParseState state, VisualMarkupBlock block) {
        foreach (var attribute in block.Attributes) {
            var key = VisualMarkupFenceOptions.NormalizeKey(attribute.Key);
            var value = attribute.Value;
            if (string.IsNullOrWhiteSpace(value)) continue;
            try {
                switch (key) {
                    case "id": state.Chart.Id = value.Trim(); break;
                    case "title": state.Chart.Title = value.Trim(); break;
                    case "subtitle": state.Chart.Subtitle = value.Trim(); break;
                    case "direction": state.Chart.LayoutDirection = ParseDirection(value); break;
                    case "layout": state.Chart.LayoutMode = VisualMarkupFenceOptions.ParseEnum<TopologyLayoutMode>(value, "layout"); break;
                    case "theme": state.Chart.ThemeMode = VisualMarkupFenceOptions.ParseEnum<VisualThemeMode>(value, "theme"); break;
                    case "width": state.Width = Positive(value, "width"); break;
                    case "height": state.Height = Positive(value, "height"); break;
                }
            } catch (ArgumentException ex) {
                state.Error(block.FenceLine, 1, MarkupArchitectureDiagnosticCodes.InvalidValue, ex.Message);
            }
        }
    }

    private static void ParseStatement(ParseState state, List<MarkupArchitectureToken> tokens, int line) {
        var command = tokens[0].Quoted ? string.Empty : tokens[0].Text.ToLowerInvariant();
        if (command == "end" || command == "}") {
            if (state.Frames.Count == 0) state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.UnexpectedEnd, "'end' has no open group or use case to close.");
            else state.Frames.Pop();
            return;
        }

        try {
            if (tokens.Count >= 3 && IsConnectionArrow(tokens[1])) {
                ParseConnection(state, tokens, 0, line);
                return;
            }

            switch (command) {
                case "title": state.Chart.Title = Tail(tokens, 1, line, "title"); return;
                case "subtitle": state.Chart.Subtitle = Tail(tokens, 1, line, "subtitle"); return;
                case "direction": state.Chart.LayoutDirection = ParseDirection(Argument(tokens, 1, line, "direction")); return;
                case "layout":
                    state.Chart.LayoutMode = VisualMarkupFenceOptions.ParseEnum<TopologyLayoutMode>(Argument(tokens, 1, line, "layout"), "layout");
                    if (tokens.Count > 2) state.Chart.LayoutDirection = ParseDirection(tokens[2].Text);
                    return;
                case "viewport": ParseViewport(state, tokens, line); return;
                case "legend": ParseLegend(state, tokens, line); return;
                case "connect":
                    if (tokens.Count >= 4 && IsConnectionArrow(tokens[2])) ParseConnection(state, tokens, 1, line);
                    else state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidStatement, "Write a connection as 'connect <from> -> <to> \"label\"'.");
                    return;
                case "group":
                case "boundary":
                    ParseGroup(state, tokens, line, command == "boundary");
                    return;
                case "usecase":
                case "story":
                    ParseUseCaseHeader(state, tokens, line);
                    return;
            }

            if (NodeKinds.TryGetValue(command, out var kind)) {
                ParseNode(state, tokens, line, kind);
                return;
            }

            state.Warning(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.UnknownCommand, "Unknown architecture statement '" + tokens[0].Text + "'. Declare nodes with a type such as 'server', 'service', 'database' or 'node', and connections as '<from> -> <to>'.");
        } catch (FormatException ex) {
            state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidStatement, ex.Message);
        } catch (ArgumentException ex) {
            state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidValue, ex.Message);
        }
    }

    private static void ParseViewport(ParseState state, List<MarkupArchitectureToken> tokens, int line) {
        var value = Argument(tokens, 1, line, "viewport");
        var parts = value.Split('x', 'X');
        if (parts.Length != 2) throw new ArgumentException("Viewport requires a size like 1200x700.");
        state.Width = Positive(parts[0], "viewport width");
        state.Height = Positive(parts[1], "viewport height");
    }

    private static void ParseLegend(ParseState state, List<MarkupArchitectureToken> tokens, int line) {
        var mode = tokens.Count > 1 ? tokens[1].Text.ToLowerInvariant() : "auto";
        if (mode == "auto" || mode == "on") state.InferLegend = true;
        else if (mode == "none" || mode == "off") state.InferLegend = false;
        else state.Error(line, tokens[1].Column, MarkupArchitectureDiagnosticCodes.InvalidValue, "Legend accepts 'auto' or 'none'.");
    }

    private static void ParseGroup(ParseState state, List<MarkupArchitectureToken> tokens, int line, bool boundary) {
        if (tokens.Count < 2 || tokens[1].Quoted) {
            state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidStatement, "Write a group as '" + tokens[0].Text + " <id> \"Label\"' and close it with 'end'.");
            return;
        }

        var id = tokens[1].Text;
        var label = tokens.Count > 2 && tokens[2].Quoted ? tokens[2].Text : id;
        var group = new TopologyGroup { Id = id, Label = label, ParentGroupId = state.CurrentGroupId };
        if (boundary) {
            group.DashPattern.Add(6);
            group.DashPattern.Add(4);
        }

        foreach (var (key, value, column) in Attributes(tokens, tokens.Count > 2 && tokens[2].Quoted ? 3 : 2)) {
            switch (key) {
                case "status": group.Status = VisualMarkupFenceOptions.ParseEnum<TopologyHealthStatus>(value, "status"); break;
                case "color": group.Color = value; break;
                case "icon": group.IconId = value; state.CheckIcon(value, line, column); break;
                case "subtitle":
                case "note": group.Subtitle = value; break;
                case "direction": group.LayoutDirection = ParseDirection(value); break;
                default: state.UnknownAttribute(key, line, column); break;
            }
        }

        if (!state.DeclareId(id, line, tokens[1].Column)) return;
        state.Chart.Groups.Add(group);
        state.Frames.Push(new Frame(id, line, null));
    }

    private static void ParseNode(ParseState state, List<MarkupArchitectureToken> tokens, int line, TopologyNodeKind kind) {
        if (tokens.Count < 2 || tokens[1].Quoted) {
            state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidStatement, "Write a node as '" + tokens[0].Text + " <id> \"Label\"'.");
            return;
        }

        var id = tokens[1].Text;
        var label = tokens.Count > 2 && tokens[2].Quoted ? tokens[2].Text : id;
        string? subtitle = null, icon = null, symbol = null, color = null, badge = null, tooltip = null, href = null;
        var status = TopologyHealthStatus.Unknown;
        TopologyNodeDisplayMode? display = null;
        foreach (var (key, value, column) in Attributes(tokens, tokens.Count > 2 && tokens[2].Quoted ? 3 : 2)) {
            switch (key) {
                case "status": status = VisualMarkupFenceOptions.ParseEnum<TopologyHealthStatus>(value, "status"); break;
                case "kind": kind = VisualMarkupFenceOptions.ParseEnum<TopologyNodeKind>(value, "kind"); break;
                case "icon": icon = value; state.CheckIcon(value, line, column); break;
                case "symbol": symbol = value; break;
                case "subtitle":
                case "note":
                case "description": subtitle = value; break;
                case "color": color = value; break;
                case "badge": badge = value; break;
                case "tooltip": tooltip = value; break;
                case "href":
                case "link": href = value; break;
                case "display": display = VisualMarkupFenceOptions.ParseEnum<TopologyNodeDisplayMode>(value, "display"); break;
                default: state.UnknownAttribute(key, line, column); break;
            }
        }

        if (!state.DeclareId(id, line, tokens[1].Column)) return;
        state.Chart.AddAutoNode(id, label, kind, status, state.CurrentGroupId, subtitle, href, tooltip, symbol: symbol, color: color, iconId: icon);
        var node = state.Chart.Nodes[state.Chart.Nodes.Count - 1];
        if (badge != null) node.Badge = badge;
        if (display.HasValue) node.DisplayMode = display.Value;
        state.NodeLines[id] = line;
    }

    private static void ParseConnection(ParseState state, List<MarkupArchitectureToken> tokens, int start, int line) {
        var from = tokens[start];
        var arrow = tokens[start + 1].Text;
        var to = tokens[start + 2];
        if (from.Quoted || to.Quoted) {
            state.Error(line, from.Column, MarkupArchitectureDiagnosticCodes.InvalidStatement, "Connections join node ids, not quoted text.");
            return;
        }

        var (label, next) = Label(tokens, start + 3);
        var connection = new PendingConnection(from.Text, to.Text, label, line, from.Column, to.Column) {
            Direction = arrow switch { "<-" => VisualLinkDirection.Backward, "<->" => VisualLinkDirection.Bidirectional, "--" => VisualLinkDirection.None, _ => VisualLinkDirection.Forward }
        };
        foreach (var (key, value, column) in Attributes(tokens, next)) {
            switch (key) {
                case "id": connection.Id = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Connection 'id' needs a value.") : value.Trim(); break;
                case "status": connection.Status = VisualMarkupFenceOptions.ParseEnum<TopologyHealthStatus>(value, "status"); break;
                case "kind": connection.Kind = VisualMarkupFenceOptions.ParseEnum<TopologyEdgeKind>(value, "kind"); break;
                case "style": connection.LineStyle = VisualMarkupFenceOptions.ParseEnum<TopologyEdgeLineStyle>(value, "style"); break;
                case "color": connection.Color = value; break;
                default: state.UnknownAttribute(key, line, column); break;
            }
        }

        state.Connections.Add(connection);
    }

    private static (string? Label, int Next) Label(List<MarkupArchitectureToken> tokens, int index) {
        if (index < tokens.Count && tokens[index].Quoted) return (tokens[index].Text, index + 1);
        var words = new List<string>();
        while (index < tokens.Count && !MarkupArchitectureLine.TryAttribute(tokens[index], out _, out _) && !IsPayloadStart(tokens[index])) words.Add(tokens[index++].Text);
        return (words.Count == 0 ? null : string.Join(" ", words), index);
    }

    private static IEnumerable<(string Key, string Value, int Column)> Attributes(List<MarkupArchitectureToken> tokens, int start) {
        for (var i = start; i < tokens.Count; i++) {
            if (tokens[i].Text == "{" && i == tokens.Count - 1) yield break;
            if (!MarkupArchitectureLine.TryAttribute(tokens[i], out var key, out var value)) throw new FormatException("Expected an attribute like 'status:healthy' but found '" + tokens[i].Text + "'.");
            yield return (key, value, tokens[i].Column);
        }
    }

    private static bool IsConnectionArrow(MarkupArchitectureToken token) => !token.Quoted && (token.Text == "->" || token.Text == "<-" || token.Text == "<->" || token.Text == "--");

    private static string Argument(List<MarkupArchitectureToken> tokens, int index, int line, string name) =>
        index < tokens.Count ? tokens[index].Text : throw new ArgumentException("'" + name + "' on line " + line.ToString(CultureInfo.InvariantCulture) + " needs a value.");

    private static string Tail(List<MarkupArchitectureToken> tokens, int index, int line, string name) =>
        index < tokens.Count ? string.Join(" ", tokens.Skip(index).Select(token => token.Text)) : throw new ArgumentException("'" + name + "' on line " + line.ToString(CultureInfo.InvariantCulture) + " needs text.");

    private static double Positive(string value, string name) {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || number <= 0 || double.IsInfinity(number)) throw new ArgumentException("'" + name + "' must be a positive number.");
        return number;
    }

    private static TopologyLayoutDirection ParseDirection(string value) => VisualMarkupFenceOptions.NormalizeKey(value) switch {
        "right" or "lr" or "lefttoright" => TopologyLayoutDirection.LeftToRight,
        "left" or "rl" or "righttoleft" => TopologyLayoutDirection.RightToLeft,
        "down" or "td" or "tb" or "toptobottom" => TopologyLayoutDirection.TopToBottom,
        "up" or "bt" or "bottomtotop" => TopologyLayoutDirection.BottomToTop,
        _ => throw new ArgumentException("Direction accepts right, down, left or up.")
    };

    private static void Add<T>(MarkupParseResult<T> result, int line, int column, string code, MarkupDiagnosticSeverity severity, string message) where T : class =>
        result.Diagnostics.Add(new MarkupDiagnostic { Line = line, Column = column, Code = code, Severity = severity, Message = message });

    private static readonly Dictionary<string, TopologyNodeKind> NodeKinds = new(StringComparer.Ordinal) {
        ["node"] = TopologyNodeKind.Generic,
        ["user"] = TopologyNodeKind.Person, ["person"] = TopologyNodeKind.Person, ["actor"] = TopologyNodeKind.Person,
        ["team"] = TopologyNodeKind.Team,
        ["client"] = TopologyNodeKind.Application, ["app"] = TopologyNodeKind.Application, ["application"] = TopologyNodeKind.Application,
        ["service"] = TopologyNodeKind.Service, ["api"] = TopologyNodeKind.Service, ["function"] = TopologyNodeKind.Service,
        ["server"] = TopologyNodeKind.Server, ["host"] = TopologyNodeKind.Server, ["vm"] = TopologyNodeKind.Server,
        ["database"] = TopologyNodeKind.Database, ["db"] = TopologyNodeKind.Database,
        ["storage"] = TopologyNodeKind.Storage, ["share"] = TopologyNodeKind.Storage, ["bucket"] = TopologyNodeKind.Storage,
        ["queue"] = TopologyNodeKind.Queue, ["topic"] = TopologyNodeKind.Queue, ["bus"] = TopologyNodeKind.Queue,
        ["gateway"] = TopologyNodeKind.Gateway, ["proxy"] = TopologyNodeKind.Gateway, ["firewall"] = TopologyNodeKind.Gateway,
        ["network"] = TopologyNodeKind.Network,
        ["cloud"] = TopologyNodeKind.Cloud, ["external"] = TopologyNodeKind.Cloud,
        ["endpoint"] = TopologyNodeKind.Endpoint, ["device"] = TopologyNodeKind.Endpoint, ["workstation"] = TopologyNodeKind.Endpoint,
        ["process"] = TopologyNodeKind.Process, ["job"] = TopologyNodeKind.Process, ["worker"] = TopologyNodeKind.Process,
        ["certificate"] = TopologyNodeKind.Certificate,
        ["site"] = TopologyNodeKind.Location, ["location"] = TopologyNodeKind.Location,
        ["hub"] = TopologyNodeKind.Hub
    };
}
