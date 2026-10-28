using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidEventModelingParser {
    public static void ParseStatements(MermaidEventModelingDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var ids = new HashSet<string>(StringComparer.Ordinal); var dataIds = new HashSet<string>(StringComparer.Ordinal);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[line - 1]; var text = raw.Trim();
            if (MermaidParserUtilities.IsSkippable(text)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, text.Length);
            document.Statements.Add(new MermaidRawStatement(text, span));
            if (Keyword(text, "title")) { document.Title = MermaidParserUtilities.StripInlineComment(text.Substring(5)).Trim(); continue; }
            if (Keyword(text, "data")) {
                var initial = line;
                try {
                    var block = ReadDataBlock(lines, ref line, text, span);
                    if (!dataIds.Add(block.Id)) throw new ArgumentException("Event Modeling data block identifiers must be unique: " + block.Id + ".");
                    MermaidResourceGuard.ReserveElements(1, block.Span);
                    block.CapturedResourceLimits = MermaidResourceGuard.CapturedLimits();
                    var literal = MermaidEventModelingData.Validate(block.DataType, block.Content, block.Span);
                    document.DataBlocks.Add(block); DiagnoseData(block.DataType, block.Span, result, literal);
                } catch (ArgumentException error) { Invalid(result, span, error.Message); }
                if (line < initial) line = initial;
                continue;
            }
            if (Keyword(text, "tf") || Keyword(text, "timeframe") || Keyword(text, "rf") || Keyword(text, "resetframe")) {
                try {
                    var tokens = SplitCommand(text, 4);
                    if (tokens.Count < 4) throw new ArgumentException("Event Modeling frames require a number, entity kind and qualified entity identifier.");
                    var kind = EntityKind(tokens[2]);
                    if (kind == MermaidEventModelingEntityKind.Unknown) throw new ArgumentException("Mermaid Event Modeling entity type is not recognized: " + tokens[2] + ".");
                    if (!FrameId(tokens[1])) throw new ArgumentException("Event Modeling timeframe numbers must contain only decimal digits and fit the 512-character identity limit.");
                    if (!Identifier(tokens[3], qualified: true)) throw new ArgumentException("Event Modeling entity identifiers require nonempty dot-separated identifier segments.");
                    if (!ids.Add(tokens[1])) throw new ArgumentException("Mermaid Event Modeling timeframe numbers must be unique: " + tokens[1] + ".");
                    var frame = new MermaidEventModelingFrame(tokens[1], kind, tokens[3], tokens[0].Equals("rf", StringComparison.OrdinalIgnoreCase) || tokens[0].Equals("resetframe", StringComparison.OrdinalIgnoreCase), span);
                    var dot = tokens[3].IndexOf('.'); frame.Namespace = dot < 0 ? null : tokens[3].Substring(0, dot); frame.Name = dot < 0 ? tokens[3] : tokens[3].Substring(dot + 1);
                    ReadSuffix(frame, tokens.Count > 4 ? tokens[4] : string.Empty, result);
                    MermaidResourceGuard.ReserveElements(1, span);
                    if (frame.InlineData != null) { var literal = MermaidEventModelingData.Validate(frame.DataType, frame.InlineData, span); DiagnoseData(frame.DataType, span, result, literal); }
                    if (frame.Number.Length > 3) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                        "Event Modeling frame identities longer than three digits are a ChartForgeX extension; source order remains the timeline order.", MermaidDiagnosticCodes.ApproximateMapping);
                    document.Frames.Add(frame);
                } catch (ArgumentException error) { Invalid(result, span, error.Message); }
                continue;
            }
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                "Event Modeling statement is retained without native execution: " + text, MermaidDiagnosticCodes.UnsupportedStatement);
        }
        var frames = document.Frames.ToDictionary(frame => frame.Number, StringComparer.Ordinal);
        var frameOrder = document.Frames.Select((frame, index) => new { frame.Number, Index = index }).ToDictionary(item => item.Number, item => item.Index, StringComparer.Ordinal);
        MermaidEventModelingFrame? previous = null, previousDifferent = null;
        foreach (var frame in document.Frames) {
            if (frame.DataReference != null && !dataIds.Contains(frame.DataReference)) Invalid(result, frame.Span, "Event Modeling data references must name an existing data block: " + frame.DataReference + ".");
            if (frame.ExplicitSources.Count > 0) {
                foreach (var id in frame.ExplicitSources.OrderBy(id => frameOrder.TryGetValue(id, out var order) ? order : int.MaxValue)) {
                    if (!frames.TryGetValue(id, out var source)) { Invalid(result, frame.Span, "Mermaid Event Modeling explicit relations must reference existing timeframe numbers."); continue; }
                    if (id == frame.Number) { Invalid(result, frame.Span, "Event Modeling frames cannot explicitly reference themselves."); continue; }
                    if (!AllowedSource(source.EntityKind, frame.EntityKind)) { Invalid(result, frame.Span, "Event Modeling explicit source entity kind is incompatible with the target kind."); continue; }
                    AddRelation(document, id, frame, false, result);
                }
            } else if (!frame.IsReset && previous != null) {
                var source = LaneKey(previous) != LaneKey(frame) ? previous : previousDifferent;
                if (source != null) AddRelation(document, source.Number, frame, true, result);
            }
            if (previous != null && LaneKey(previous) != LaneKey(frame)) previousDifferent = previous;
            previous = frame;
        }
        if (document.Frames.Count == 0) Invalid(result, document.HeaderSpan, "Mermaid Event Modeling diagrams require at least one timeframe.");
    }

    internal static string RelationId(string source, string target) {
        const string prefix = "eventmodeling-edge-";
        if ((long)prefix.Length + source.Length + 1 + target.Length > 512)
            throw new ArgumentException("Event Modeling derived relation identities must fit the existing 512-character portable limit.");
        return prefix + source + "-" + target;
    }
    private static void AddRelation(MermaidEventModelingDocument document, string source, MermaidEventModelingFrame target, bool inferred, MermaidParseResult<MermaidDocument> result) {
        try { RelationId(source, target.Number); }
        catch (ArgumentException error) { Invalid(result, target.Span, error.Message); return; }
        if (inferred) MermaidResourceGuard.ReserveEdges(1, target.Span);
        document.Relations.Add(new MermaidEventModelingRelation(source, target.Number, inferred, target.Span));
    }

    internal static string LaneKey(MermaidEventModelingFrame frame) => Band(frame.EntityKind).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + (frame.Namespace ?? "");
    internal static int Band(MermaidEventModelingEntityKind kind) => kind is MermaidEventModelingEntityKind.Ui or MermaidEventModelingEntityKind.Processor ? 0
        : kind is MermaidEventModelingEntityKind.Command or MermaidEventModelingEntityKind.ReadModel ? 1 : 2;
    private static bool AllowedSource(MermaidEventModelingEntityKind source, MermaidEventModelingEntityKind target) => target switch {
        MermaidEventModelingEntityKind.Command => source is MermaidEventModelingEntityKind.Ui or MermaidEventModelingEntityKind.Processor,
        MermaidEventModelingEntityKind.Event => source == MermaidEventModelingEntityKind.Command,
        MermaidEventModelingEntityKind.ReadModel => source == MermaidEventModelingEntityKind.Event,
        _ => source == MermaidEventModelingEntityKind.ReadModel
    };
    private static MermaidEventModelingEntityKind EntityKind(string value) => value.ToLowerInvariant() switch {
        "ui" => MermaidEventModelingEntityKind.Ui, "pcr" or "processor" => MermaidEventModelingEntityKind.Processor,
        "cmd" or "command" => MermaidEventModelingEntityKind.Command, "rmo" or "readmodel" => MermaidEventModelingEntityKind.ReadModel,
        "evt" or "event" => MermaidEventModelingEntityKind.Event, _ => MermaidEventModelingEntityKind.Unknown
    };
    internal static bool FrameId(string value) => value.Length is > 0 and <= 512 && value.All(character => character >= '0' && character <= '9');
    internal static bool Identifier(string value, bool qualified = false) {
        if (value.Length == 0 || value.Length > 512) return false;
        foreach (var segment in qualified ? value.Split('.') : new[] { value }) {
            if (segment.Length == 0 || !(char.IsLetter(segment[0]) || segment[0] == '_')) return false;
            if (segment.Any(character => !(char.IsLetterOrDigit(character) || character == '_'))) return false;
        }
        return true;
    }
    private static bool Keyword(string text, string keyword) => text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)
        && (text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]));
    private static void Invalid(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, string message) =>
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
    private static void DiagnoseData(string? type, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, bool literal) {
        if (literal) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
            "Event Modeling data type '" + type + "' is retained and displayed as literal text; no HTML, JavaScript, math or remote asset is executed.", MermaidDiagnosticCodes.UnsupportedStatement);
    }
}
