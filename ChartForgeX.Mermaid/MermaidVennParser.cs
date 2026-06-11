using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

internal static class MermaidVennParser {
    public static void ParseStatements(MermaidVennDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "title")) {
                document.Title = MermaidParserUtilities.Unquote(trimmed.Substring(5).Trim());
                continue;
            }

            if (StartsWithKeyword(trimmed, "set")) {
                ParseSet(document, trimmed.Substring(3).Trim(), span, ids, result);
                continue;
            }

            if (StartsWithKeyword(trimmed, "union")) {
                ParseUnion(document, trimmed.Substring(5).Trim(), span, ids, result);
                continue;
            }

            if (StartsWithKeyword(trimmed, "text")) {
                ParseText(document, trimmed.Substring(4).Trim(), span, ids, result);
                continue;
            }

            if (StartsWithKeyword(trimmed, "style")) {
                ParseStyle(document, trimmed, span, ids, result);
                continue;
            }

            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Venn statement was retained but is not rendered by ChartForgeX yet.");
        }

        if (document.Sets.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid Venn diagrams require at least one set.");
    }

    private static void ParseSet(MermaidVennDocument document, string text, MermaidSourceSpan span, HashSet<string> ids, MermaidParseResult<MermaidDocument> result) {
        if (!TryParseRegionPayload(text, singleId: true, out var setIds, out var label, out var sizeText)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn set statements must use 'set <id> [label] : <size>'.");
            return;
        }

        var id = setIds[0];
        if (ids.Contains(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn set id was already declared: " + id + ".");
            return;
        }

        if (document.Sets.Count >= 3) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn diagrams support no more than three sets.");
            return;
        }

        if (!TryParseSize(sizeText, 10, span, result, out var size)) return;
        ids.Add(id);
        document.Sets.Add(new MermaidVennSet(id, label.Length == 0 ? id : label, size, span));
    }

    private static void ParseUnion(MermaidVennDocument document, string text, MermaidSourceSpan span, HashSet<string> ids, MermaidParseResult<MermaidDocument> result) {
        if (!TryParseRegionPayload(text, singleId: false, out var setIds, out var label, out var sizeText)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn union statements must use 'union <id,id> [label] : <size>'.");
            return;
        }

        if (!ValidateKnownIds(setIds, ids, span, result)) return;
        if (!TryParseSize(sizeText, 10 / Math.Pow(Math.Max(1, setIds.Count), 2), span, result, out var size)) return;
        document.Intersections.Add(new MermaidVennIntersection(setIds, label, size, span));
    }

    private static void ParseText(MermaidVennDocument document, string text, MermaidSourceSpan span, HashSet<string> ids, MermaidParseResult<MermaidDocument> result) {
        var label = ExtractBracketedLabel(ref text);
        var pieces = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length < 2) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn text statements must use 'text <id,id> <textId> [label]'.");
            return;
        }

        var setIds = ParseIdentifierList(pieces[0]);
        if (!ValidateKnownIds(setIds, ids, span, result)) return;
        var id = pieces[1].Trim();
        if (id.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn text statements must define a text id.");
            return;
        }

        document.TextNodes.Add(new MermaidVennTextNode(setIds, id, label.Length == 0 ? id : label, span));
    }

    private static void ParseStyle(MermaidVennDocument document, string text, MermaidSourceSpan span, HashSet<string> ids, MermaidParseResult<MermaidDocument> result) {
        document.StyleStatements.Add(new MermaidRawStatement(text, span));
        var body = text.Substring(5).Trim();
        var split = FirstWhitespace(body);
        if (split <= 0 || split >= body.Length - 1) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Venn style statements must include targets and style entries.");
            return;
        }

        var setIds = ParseIdentifierList(body.Substring(0, split));
        if (!ValidateKnownIds(setIds, ids, span, result)) return;
        if (!TryFindStyleTarget(document, setIds, out var set, out var intersection)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Venn style target does not match a rendered set or union.");
            return;
        }

        foreach (var entry in MermaidParserUtilities.SplitCsvLike(body.Substring(split + 1))) {
            var colon = entry.IndexOf(':');
            if (colon <= 0 || colon == entry.Length - 1) continue;
            var key = entry.Substring(0, colon).Trim();
            var value = entry.Substring(colon + 1).Trim();
            if (TryApplyStyle(set, intersection, key, value)) continue;
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid Venn style key '" + key + "' is retained but is not rendered by ChartForgeX yet.");
        }
    }

    private static bool TryParseRegionPayload(string text, bool singleId, out List<string> setIds, out string label, out string sizeText) {
        label = ExtractBracketedLabel(ref text);
        sizeText = string.Empty;
        var colon = text.IndexOf(':');
        if (colon >= 0) {
            sizeText = text.Substring(colon + 1).Trim();
            text = text.Substring(0, colon).Trim();
        }

        var parts = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        setIds = parts.Length == 0 ? new List<string>() : ParseIdentifierList(parts[0]);
        if (sizeText.Length == 0 && parts.Length > 1) sizeText = parts[1];
        return setIds.Count > 0 && (!singleId || setIds.Count == 1);
    }

    private static string ExtractBracketedLabel(ref string text) {
        var start = text.IndexOf('[');
        if (start < 0) return string.Empty;
        var end = text.IndexOf(']', start + 1);
        if (end <= start) return string.Empty;
        var label = MermaidParserUtilities.Unquote(text.Substring(start + 1, end - start - 1).Trim());
        text = (text.Substring(0, start) + " " + text.Substring(end + 1)).Trim();
        return label;
    }

    private static List<string> ParseIdentifierList(string text) {
        var result = new List<string>();
        foreach (var part in MermaidParserUtilities.SplitCsvLike(text)) {
            var id = part.Trim();
            if (id.Length > 0) result.Add(id);
        }

        return result;
    }

    private static bool TryParseSize(string value, double fallback, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, out double size) {
        size = fallback;
        if (value.Length == 0) return true;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out size) && !double.IsNaN(size) && !double.IsInfinity(size) && size >= 0) return true;
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn sizes must be finite numbers greater than or equal to zero.");
        return false;
    }

    private static bool ValidateKnownIds(IReadOnlyList<string> setIds, HashSet<string> known, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (setIds.Count == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn statements must reference at least one set id.");
            return false;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in setIds) {
            if (!known.Contains(id)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn statement references unknown set id: " + id + ".");
                return false;
            }

            if (!seen.Add(id)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid Venn statements must not repeat set ids: " + id + ".");
                return false;
            }
        }

        return true;
    }

    private static bool TryFindStyleTarget(MermaidVennDocument document, IReadOnlyList<string> setIds, out MermaidVennSet? set, out MermaidVennIntersection? intersection) {
        set = null;
        intersection = null;
        if (setIds.Count == 1) {
            set = document.Sets.Find(candidate => string.Equals(candidate.Id, setIds[0], StringComparison.Ordinal));
            return set != null;
        }

        intersection = document.Intersections.Find(candidate => SameIds(candidate.SetIds, setIds));
        return intersection != null;
    }

    private static bool TryApplyStyle(MermaidVennSet? set, MermaidVennIntersection? intersection, string key, string value) {
        if (!ChartColor.TryParse(value, out var color)) return false;
        var normalized = NormalizeStyleKey(key);
        if (normalized == "fill") {
            if (set != null) set.Fill = color;
            if (intersection != null) intersection.Fill = color;
            return true;
        }

        if (normalized == "stroke") {
            if (set != null) set.Stroke = color;
            if (intersection != null) intersection.Stroke = color;
            return true;
        }

        if (normalized == "color") {
            if (set != null) set.TextColor = color;
            if (intersection != null) intersection.TextColor = color;
            return true;
        }

        return false;
    }

    private static bool SameIds(IReadOnlyList<string> left, IReadOnlyList<string> right) {
        if (left.Count != right.Count) return false;
        var remaining = new HashSet<string>(left, StringComparer.Ordinal);
        foreach (var id in right) if (!remaining.Remove(id)) return false;
        return true;
    }

    private static int FirstWhitespace(string text) {
        for (var index = 0; index < text.Length; index++) if (char.IsWhiteSpace(text[index])) return index;
        return -1;
    }

    private static string NormalizeStyleKey(string value) => value.Replace("-", string.Empty).Trim().ToLowerInvariant();

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]) || text[keyword.Length] == ':';
    }
}
