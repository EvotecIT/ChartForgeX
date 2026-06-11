using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static class MermaidEventModelingParser {
    private static readonly Regex DataReferencePattern = new(@"\[\[(?<id>[^\]]+)\]\]", RegexOptions.Compiled);
    private static readonly Regex DataTypePattern = new(@"`(?<type>[^`]+)`\s*$", RegexOptions.Compiled);

    public static void ParseStatements(MermaidEventModelingDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var framesByNumber = new Dictionary<string, MermaidEventModelingFrame>(StringComparer.Ordinal);
        var previousForInference = default(MermaidEventModelingFrame);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "data")) {
                line = ParseDataBlock(document, lines, line, trimmed.Substring(4).Trim(), span, result);
                continue;
            }

            if (TryParseFrame(trimmed, span, result, out var frame)) {
                if (frame == null) continue;
                if (framesByNumber.ContainsKey(frame.Number)) {
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Event Modeling timeframe numbers must be unique: " + frame.Number + ".");
                    continue;
                }

                framesByNumber.Add(frame.Number, frame);
                document.Frames.Add(frame);
                if (previousForInference != null && !frame.IsReset && frame.ExplicitSources.Count == 0) {
                    document.Relations.Add(new MermaidEventModelingRelation(previousForInference.Number, frame.Number, isInferred: true, frame.Span));
                }

                previousForInference = frame.IsReset ? null : frame;
                continue;
            }

            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Event Modeling statement was retained but is not rendered by ChartForgeX yet: " + trimmed);
        }

        foreach (var frame in document.Frames) {
            foreach (var source in frame.ExplicitSources) {
                if (!framesByNumber.ContainsKey(source)) {
                    MermaidParserUtilities.Add(result, frame.Span, MermaidDiagnosticSeverity.Error, "Mermaid Event Modeling explicit relations must reference existing timeframe numbers.");
                    continue;
                }

                document.Relations.Add(new MermaidEventModelingRelation(source, frame.Number, isInferred: false, frame.Span));
            }
        }

        if (document.Frames.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid Event Modeling diagrams require at least one timeframe.");
    }

    private static bool TryParseFrame(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, out MermaidEventModelingFrame frame) {
        frame = null!;
        var parts = SplitCommand(text, 4);
        if (parts.Count < 4) return false;
        var isTimeFrame = string.Equals(parts[0], "tf", StringComparison.OrdinalIgnoreCase) || string.Equals(parts[0], "timeframe", StringComparison.OrdinalIgnoreCase);
        var isReset = string.Equals(parts[0], "rf", StringComparison.OrdinalIgnoreCase) || string.Equals(parts[0], "resetframe", StringComparison.OrdinalIgnoreCase);
        if (!isTimeFrame && !isReset) return false;

        var kind = ToEntityKind(parts[2]);
        if (kind == MermaidEventModelingEntityKind.Unknown) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Event Modeling entity type is not recognized: " + parts[2] + ".");
            return true;
        }

        var entity = parts[3];
        var remainder = parts.Count > 4 ? parts[4].Trim() : string.Empty;
        frame = new MermaidEventModelingFrame(parts[1], kind, entity, isReset, span);
        SplitNamespace(entity, out var ns, out var name);
        frame.Namespace = ns;
        frame.Name = name;
        ParseFrameSuffix(frame, remainder, span, result);
        return true;
    }

    private static void ParseFrameSuffix(MermaidEventModelingFrame frame, string suffix, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var rest = suffix.Trim();
        if (rest.Length == 0) return;

        if (TryExtractInlineData(rest, out var dataType, out var data, out var before, out var after)) {
            frame.DataType = dataType;
            frame.InlineData = data;
            rest = (before + " " + after).Trim();
        }

        var relationIndex = rest.IndexOf("->>", StringComparison.Ordinal);
        if (relationIndex >= 0) {
            var relationText = rest.Substring(relationIndex + 3).Trim();
            foreach (var source in SplitRelationSources(relationText)) frame.ExplicitSources.Add(source);
            rest = rest.Substring(0, relationIndex).Trim();
        }

        var dataRef = DataReferencePattern.Match(rest);
        if (dataRef.Success) {
            frame.DataReference = dataRef.Groups["id"].Value.Trim();
            rest = (rest.Substring(0, dataRef.Index) + rest.Substring(dataRef.Index + dataRef.Length)).Trim();
        }

        if (rest.Length > 0) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Event Modeling timeframe suffix was retained but is not rendered exactly: " + rest);
    }

    private static int ParseDataBlock(MermaidEventModelingDocument document, string[] lines, int currentLine, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var open = text.IndexOf('{');
        if (open < 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Event Modeling data block was retained but is missing a data body.");
            return currentLine;
        }

        var header = text.Substring(0, open).Trim();
        var tokens = header.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Event Modeling data blocks require an identifier.");
            return currentLine;
        }

        var dataType = ExtractDataType(header.Substring(tokens[0].Length).Trim(), out _);
        var firstContent = text.Substring(open + 1);
        var builder = new StringBuilder();
        var depth = 1;
        if (AppendDataBlockContent(builder, firstContent, ref depth)) {
            document.DataBlocks.Add(new MermaidEventModelingDataBlock(tokens[0], dataType, builder.ToString().Trim(), span));
            return currentLine;
        }

        if (builder.Length > 0) builder.AppendLine();
        for (var line = currentLine + 1; line <= lines.Length; line++) {
            var raw = lines[line - 1];
            if (AppendDataBlockContent(builder, raw, ref depth)) {
                document.DataBlocks.Add(new MermaidEventModelingDataBlock(tokens[0], dataType, builder.ToString().TrimEnd(), span));
                return line;
            }

            builder.AppendLine();
        }

        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Event Modeling data block '" + tokens[0] + "' must close with '}'.");
        return lines.Length;
    }

    private static bool AppendDataBlockContent(StringBuilder builder, string text, ref int depth) {
        var quote = '\0';
        var escaped = false;
        for (var index = 0; index < text.Length; index++) {
            var ch = text[index];
            if (quote != '\0') {
                builder.Append(ch);
                if (escaped) {
                    escaped = false;
                    continue;
                }

                if (ch == '\\') {
                    escaped = true;
                    continue;
                }

                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch == '"' || ch == '\'' || ch == '`') {
                quote = ch;
                builder.Append(ch);
                continue;
            }

            if (ch == '{') {
                depth++;
                builder.Append(ch);
                continue;
            }

            if (ch == '}') {
                depth--;
                if (depth == 0) return true;
                builder.Append(ch);
                continue;
            }

            builder.Append(ch);
        }

        return false;
    }

    private static bool TryExtractInlineData(string text, out string? dataType, out string data, out string before, out string after) {
        dataType = null;
        data = string.Empty;
        before = text;
        after = string.Empty;
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open < 0 || close <= open) return false;
        var prefix = text.Substring(0, open).TrimEnd();
        dataType = ExtractDataType(prefix, out before);
        data = text.Substring(open + 1, close - open - 1).Trim();
        after = text.Substring(close + 1).Trim();
        return true;
    }

    private static string? ExtractDataType(string prefix, out string beforeType) {
        beforeType = prefix;
        var match = DataTypePattern.Match(prefix);
        if (!match.Success) return null;
        beforeType = prefix.Substring(0, match.Index).Trim();
        return match.Groups["type"].Value.Trim();
    }

    private static List<string> SplitCommand(string text, int maxTokensBeforeRemainder) {
        var result = new List<string>();
        var index = 0;
        while (result.Count < maxTokensBeforeRemainder && index < text.Length) {
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
            if (index >= text.Length) break;
            var start = index;
            while (index < text.Length && !char.IsWhiteSpace(text[index])) index++;
            result.Add(text.Substring(start, index - start));
        }

        if (index < text.Length) result.Add(text.Substring(index).Trim());
        return result;
    }

    private static IEnumerable<string> SplitRelationSources(string text) {
        foreach (var item in text.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) {
            var cleaned = item.Trim();
            if (cleaned.Length > 0) yield return cleaned;
        }
    }

    private static void SplitNamespace(string entity, out string? ns, out string name) {
        var dot = entity.IndexOf('.');
        if (dot <= 0 || dot >= entity.Length - 1) {
            ns = null;
            name = entity;
            return;
        }

        ns = entity.Substring(0, dot);
        name = entity.Substring(dot + 1);
    }

    private static MermaidEventModelingEntityKind ToEntityKind(string value) {
        if (string.Equals(value, "ui", StringComparison.OrdinalIgnoreCase)) return MermaidEventModelingEntityKind.Ui;
        if (string.Equals(value, "pcr", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "processor", StringComparison.OrdinalIgnoreCase)) return MermaidEventModelingEntityKind.Processor;
        if (string.Equals(value, "cmd", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "command", StringComparison.OrdinalIgnoreCase)) return MermaidEventModelingEntityKind.Command;
        if (string.Equals(value, "rmo", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "readmodel", StringComparison.OrdinalIgnoreCase)) return MermaidEventModelingEntityKind.ReadModel;
        if (string.Equals(value, "evt", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "event", StringComparison.OrdinalIgnoreCase)) return MermaidEventModelingEntityKind.Event;
        return MermaidEventModelingEntityKind.Unknown;
    }

    private static bool StartsWithKeyword(string text, string keyword) =>
        text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) && (text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]));
}
