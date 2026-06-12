using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal static class MermaidWardleyParser {
    private const int MaximumWardleyNodes = 256;
    private const int MaximumWardleyLinks = 512;

    private static readonly Regex LinkPattern = new(@"\s(?<arrow>-\.\->|-->|->|\+'[^']*'<>|\+'[^']*'<|\+'[^']*'>|\+<>|\+<|\+>)\s", RegexOptions.Compiled);

    public static void ParseStatements(MermaidWardleyDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "title")) { document.Title = trimmed.Substring(5).Trim(); continue; }
            if (StartsWithKeyword(trimmed, "size")) { ParseSize(document, trimmed.Substring(4).Trim(), span, result); continue; }
            if (StartsWithKeyword(trimmed, "evolution")) { ParseEvolutionStages(document, trimmed.Substring(9).Trim(), span); continue; }
            if (StartsWithKeyword(trimmed, "anchor")) { ParseNode(document, trimmed.Substring(6).Trim(), WardleyMapNodeKind.Anchor, span, nodeIds, result); continue; }
            if (StartsWithKeyword(trimmed, "component")) { ParseNode(document, trimmed.Substring(9).Trim(), WardleyMapNodeKind.Component, span, nodeIds, result); continue; }
            if (StartsWithKeyword(trimmed, "evolve")) { ParseEvolve(document, trimmed.Substring(6).Trim(), span, nodeIds, result); continue; }
            if (StartsWithKeyword(trimmed, "note")) { ParseNote(document, trimmed.Substring(4).Trim(), span, result); continue; }
            if (StartsWithKeyword(trimmed, "annotation")) { ParseAnnotation(document, trimmed.Substring(10).Trim(), span, result); continue; }
            if (StartsWithKeyword(trimmed, "accelerator")) { ParseMarker(document, trimmed.Substring(11).Trim(), WardleyMapMarkerKind.Accelerator, span, result); continue; }
            if (StartsWithKeyword(trimmed, "deaccelerator")) { ParseMarker(document, trimmed.Substring(13).Trim(), WardleyMapMarkerKind.Deaccelerator, span, result); continue; }
            if (StartsWithKeyword(trimmed, "pipeline")) { line = ParsePipeline(document, lines, line, trimmed.Substring(8).Trim(), span, nodeIds, result); continue; }
            if (TryParseLink(document, trimmed, span, nodeIds, result)) continue;

            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Wardley statement was retained but is not rendered by ChartForgeX yet.");
        }

        if (document.Nodes.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid Wardley maps require at least one anchor or component.");
    }

    private static void ParseSize(MermaidWardleyDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!TryParseCoordinatePair(text, allowIntegers: true, out var width, out var height)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley size statements must use 'size [width, height]'.");
            return;
        }

        document.CanvasWidth = Math.Max(1, (int)Math.Round(width));
        document.CanvasHeight = Math.Max(1, (int)Math.Round(height));
    }

    private static void ParseEvolutionStages(MermaidWardleyDocument document, string text, MermaidSourceSpan span) {
        document.Stages.Clear();
        foreach (var part in text.Split(new[] { "->" }, StringSplitOptions.None)) {
            var stage = part.Trim();
            if (stage.Length > 0) document.Stages.Add(stage);
        }

        if (document.Stages.Count == 0) document.Statements.Add(new MermaidRawStatement(text, span));
    }

    private static void ParseNode(MermaidWardleyDocument document, string text, WardleyMapNodeKind kind, MermaidSourceSpan span, HashSet<string> nodeIds, MermaidParseResult<MermaidDocument> result) {
        if (!TryExtractCoordinates(text, out var name, out var visibility, out var evolution, out var suffix)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley nodes must use '<kind> name [visibility, evolution]'.");
            return;
        }

        if (!TryNormalizeCoordinate(visibility, span, result, out var normalizedVisibility) || !TryNormalizeCoordinate(evolution, span, result, out var normalizedEvolution)) return;
        if (document.Nodes.Count >= MaximumWardleyNodes) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley maps support no more than " + MaximumWardleyNodes.ToString(CultureInfo.InvariantCulture) + " nodes.");
            return;
        }

        if (!nodeIds.Add(name)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley node names must be unique: " + name + ".");
            return;
        }

        var node = new MermaidWardleyNode(name, normalizedVisibility, normalizedEvolution, kind, span);
        ParseNodeSuffix(node, suffix, span, result);
        document.Nodes.Add(node);
    }

    private static void ParseNodeSuffix(MermaidWardleyNode node, string suffix, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var rest = suffix.Trim();
        if (rest.Length == 0) return;
        if (rest.IndexOf("inertia", StringComparison.OrdinalIgnoreCase) >= 0) {
            node.Inertia = true;
            rest = Regex.Replace(rest, @"\binertia\b", string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        var strategy = ExtractParenthesized(rest);
        if (strategy == "build" || strategy == "buy" || strategy == "outsource" || strategy == "market") {
            node.Strategy = strategy;
            rest = Regex.Replace(rest, @"\((build|buy|outsource|market)\)", string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        var labelIndex = rest.IndexOf("label", StringComparison.OrdinalIgnoreCase);
        if (labelIndex >= 0 && TryExtractCoordinatePair(rest.Substring(labelIndex + 5).Trim(), allowIntegers: true, out var x, out var y, out _)) {
            node.LabelOffsetX = x;
            node.LabelOffsetY = y;
            rest = rest.Substring(0, labelIndex).Trim();
        }

        if (rest.Length > 0) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Wardley node suffix was retained but is not rendered exactly: " + rest);
    }

    private static bool TryParseLink(MermaidWardleyDocument document, string text, MermaidSourceSpan span, HashSet<string> nodeIds, MermaidParseResult<MermaidDocument> result) {
        var match = LinkPattern.Match(text);
        if (!match.Success) return false;
        var from = text.Substring(0, match.Index).Trim();
        var arrow = match.Groups["arrow"].Value;
        var to = text.Substring(match.Index + match.Length).Trim();
        var label = string.Empty;
        var semicolon = to.IndexOf(';');
        if (semicolon >= 0) {
            label = to.Substring(semicolon + 1).Trim();
            to = to.Substring(0, semicolon).Trim();
        }

        if (!nodeIds.Contains(from) || !nodeIds.Contains(to)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley links must reference previously declared nodes.");
            return true;
        }

        var flow = WardleyMapFlow.None;
        var flowMatch = Regex.Match(arrow, @"^\+'(?<label>[^']*)'(?<direction><>|<|>)$");
        if (flowMatch.Success) {
            label = flowMatch.Groups["label"].Value;
            flow = FlowFromSymbol(flowMatch.Groups["direction"].Value);
        } else if (arrow.StartsWith("+", StringComparison.Ordinal)) {
            flow = FlowFromSymbol(arrow.Substring(1));
        }

        if (document.Links.Count >= MaximumWardleyLinks) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley maps support no more than " + MaximumWardleyLinks.ToString(CultureInfo.InvariantCulture) + " links.");
            return true;
        }

        document.Links.Add(new MermaidWardleyLink(from, to, label, arrow.IndexOf(".", StringComparison.Ordinal) >= 0, flow, span));
        return true;
    }

    private static void ParseEvolve(MermaidWardleyDocument document, string text, MermaidSourceSpan span, HashSet<string> nodeIds, MermaidParseResult<MermaidDocument> result) {
        var split = LastWhitespace(text);
        if (split <= 0 || split >= text.Length - 1 || !double.TryParse(text.Substring(split + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley evolve statements must use 'evolve component target'.");
            return;
        }

        var node = text.Substring(0, split).Trim();
        if (!nodeIds.Contains(node)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley evolve statements must reference a previously declared node.");
            return;
        }

        if (TryNormalizeCoordinate(value, span, result, out var normalized)) document.Evolutions.Add(new MermaidWardleyEvolution(node, normalized, span));
    }

    private static void ParseNote(MermaidWardleyDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!TryExtractCoordinates(text, out var label, out var visibility, out var evolution, out _) || !IsQuoted(label)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley note statements must use 'note \"text\" [visibility, evolution]'.");
            return;
        }

        if (TryNormalizeCoordinate(visibility, span, result, out var v) && TryNormalizeCoordinate(evolution, span, result, out var e)) document.Notes.Add(new MermaidWardleyNote(MermaidParserUtilities.Unquote(label), v, e, span));
    }

    private static void ParseAnnotation(MermaidWardleyDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var comma = text.IndexOf(',');
        if (comma <= 0 || !int.TryParse(text.Substring(0, comma).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Wardley annotation statement was retained but is not rendered by ChartForgeX yet.");
            return;
        }

        var rest = text.Substring(comma + 1).Trim();
        if (!TryExtractCoordinatePair(rest, allowIntegers: true, out var visibility, out var evolution, out var suffix) || !IsQuoted(suffix)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Wardley annotation statement was retained but is not rendered by ChartForgeX yet.");
            return;
        }

        if (TryNormalizeCoordinate(visibility, span, result, out var v) && TryNormalizeCoordinate(evolution, span, result, out var e)) document.Annotations.Add(new MermaidWardleyAnnotation(number, MermaidParserUtilities.Unquote(suffix), v, e, span));
    }

    private static void ParseMarker(MermaidWardleyDocument document, string text, WardleyMapMarkerKind kind, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!TryExtractCoordinates(text, out var label, out var visibility, out var evolution, out _)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley marker statements must use '<marker> name [visibility, evolution]'.");
            return;
        }

        if (TryNormalizeCoordinate(visibility, span, result, out var v) && TryNormalizeCoordinate(evolution, span, result, out var e)) document.Markers.Add(new MermaidWardleyMarker(label, v, e, kind, span));
    }

    private static int ParsePipeline(MermaidWardleyDocument document, string[] lines, int currentLine, string text, MermaidSourceSpan span, HashSet<string> nodeIds, MermaidParseResult<MermaidDocument> result) {
        var brace = text.IndexOf('{');
        var parent = (brace >= 0 ? text.Substring(0, brace) : text).Trim();
        if (parent.Length == 0 || !nodeIds.Contains(parent)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley pipelines must reference a previously declared parent component.");
            return currentLine;
        }

        if (brace < 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Wardley pipeline statement was retained but is missing a component block.");
            return currentLine;
        }

        var pipeline = new MermaidWardleyPipeline(parent, span);
        for (var line = currentLine + 1; line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            if (trimmed == "}") {
                document.Pipelines.Add(pipeline);
                return line;
            }

            var childSpan = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            if (!StartsWithKeyword(trimmed, "component") || !TryParsePipelineComponent(trimmed.Substring(9).Trim(), childSpan, result, out var component)) {
                MermaidParserUtilities.Add(result, childSpan, MermaidDiagnosticSeverity.Warning, "Mermaid Wardley pipeline child statement was retained but is not rendered by ChartForgeX yet.");
                continue;
            }

            if (component == null) continue;
            pipeline.AddComponent(component);
        }

        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley pipeline block was not closed.");
        return lines.Length;
    }

    private static bool TryParsePipelineComponent(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, out MermaidWardleyPipelineComponent? component) {
        component = null;
        if (!TryExtractSingleCoordinate(text, out var label, out var evolution, out _)) return false;
        if (!TryNormalizeCoordinate(evolution, span, result, out var normalized)) return true;
        component = new MermaidWardleyPipelineComponent(label, normalized, span);
        return true;
    }

    private static bool TryExtractCoordinates(string text, out string prefix, out double first, out double second, out string suffix) {
        prefix = string.Empty;
        suffix = string.Empty;
        first = 0;
        second = 0;
        var start = text.IndexOf('[');
        var end = text.IndexOf(']', start + 1);
        if (start < 0 || end <= start) return false;
        prefix = text.Substring(0, start).Trim();
        suffix = text.Substring(end + 1).Trim();
        return prefix.Length > 0 && TryParseCoordinatePair(text.Substring(start, end - start + 1), allowIntegers: true, out first, out second);
    }

    private static bool TryExtractSingleCoordinate(string text, out string prefix, out double value, out string suffix) {
        prefix = string.Empty;
        suffix = string.Empty;
        value = 0;
        var start = text.LastIndexOf('[');
        var end = text.IndexOf(']', start + 1);
        if (start < 0 || end <= start) return false;
        prefix = text.Substring(0, start).Trim();
        suffix = text.Substring(end + 1).Trim();
        return prefix.Length > 0 && double.TryParse(text.Substring(start + 1, end - start - 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryExtractCoordinatePair(string text, bool allowIntegers, out double first, out double second, out string suffix) {
        suffix = string.Empty;
        first = 0;
        second = 0;
        var start = text.IndexOf('[');
        var end = text.IndexOf(']', start + 1);
        if (start < 0 || end <= start) return false;
        suffix = text.Substring(end + 1).Trim();
        return TryParseCoordinatePair(text.Substring(start, end - start + 1), allowIntegers, out first, out second);
    }

    private static bool TryParseCoordinatePair(string text, bool allowIntegers, out double first, out double second) {
        first = 0;
        second = 0;
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("[", StringComparison.Ordinal) || !trimmed.EndsWith("]", StringComparison.Ordinal)) return false;
        var parts = MermaidParserUtilities.SplitCsvLike(trimmed.Substring(1, trimmed.Length - 2));
        return parts.Count == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out first)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out second)
            && (allowIntegers || (parts[0].IndexOf(".", StringComparison.Ordinal) >= 0 && parts[1].IndexOf(".", StringComparison.Ordinal) >= 0));
    }

    private static bool TryNormalizeCoordinate(double value, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, out double normalized) {
        normalized = value <= 1 ? value : value / 100.0;
        if (normalized >= 0 && normalized <= 1 && !double.IsNaN(normalized) && !double.IsInfinity(normalized)) return true;
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Wardley coordinates must be between 0-1 or 0-100.");
        return false;
    }

    private static string ExtractParenthesized(string text) {
        var start = text.IndexOf('(');
        var end = text.IndexOf(')', start + 1);
        return start >= 0 && end > start ? text.Substring(start + 1, end - start - 1).Trim().ToLowerInvariant() : string.Empty;
    }

    private static WardleyMapFlow FlowFromSymbol(string value) {
        if (value == "<>") return WardleyMapFlow.Bidirectional;
        if (value == "<") return WardleyMapFlow.Backward;
        if (value == ">") return WardleyMapFlow.Forward;
        return WardleyMapFlow.None;
    }

    private static int LastWhitespace(string text) {
        for (var index = text.Length - 1; index >= 0; index--) if (char.IsWhiteSpace(text[index])) return index;
        return -1;
    }

    private static bool IsQuoted(string value) => value.Length >= 2 && ((value[0] == '"' && value[value.Length - 1] == '"') || (value[0] == '\'' && value[value.Length - 1] == '\''));

    private static bool StartsWithKeyword(string text, string keyword) =>
        text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) && (text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]));
}
