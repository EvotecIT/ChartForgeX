using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal static class MermaidBlockParser {
    public static void ParseStatements(MermaidBlockDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var spaceIndex = 0;
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

            if (StartsWithKeyword(trimmed, "columns")) {
                ParseColumns(document, trimmed, span, result);
                continue;
            }

            if (IsStyleStatement(trimmed)) {
                document.StyleStatements.Add(new MermaidRawStatement(trimmed, span));
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid block style and class statements are retained but are not rendered by ChartForgeX yet.");
                continue;
            }

            if (StartsWithKeyword(trimmed, "block") || string.Equals(trimmed, "end", StringComparison.OrdinalIgnoreCase)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid nested block/composite statements are retained but are not rendered by ChartForgeX block layout yet.");
                continue;
            }

            if (TryParseEdge(document, trimmed, span, ids, result)) continue;
            ParseNodeLine(document, trimmed, span, ids, ref spaceIndex, result);
        }

        if (document.Items.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid block diagrams require at least one block item.");
    }

    private static void ParseColumns(MermaidBlockDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var value = text.Substring(7).Trim();
        if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase)) {
            document.Columns = null;
            return;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var columns) || columns < 1 || columns > 24) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid block column counts must be between one and twenty-four.");
            return;
        }

        document.Columns = columns;
    }

    private static void ParseNodeLine(MermaidBlockDocument document, string text, MermaidSourceSpan span, HashSet<string> ids, ref int spaceIndex, MermaidParseResult<MermaidDocument> result) {
        var tokens = SplitTokens(text);
        if (tokens.Count == 0) return;
        for (var index = 0; index < tokens.Count; index++) {
            if (!TryParseItem(tokens[index], span, ref spaceIndex, out var item, out var diagnostic)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, diagnostic);
                continue;
            }

            if (!item.IsSpace && !ids.Add(item.Id)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid block item '" + item.Id + "' was already declared; the duplicate declaration was retained but not added to the rendered layout.");
                continue;
            }

            document.Items.Add(item);
        }
    }

    private static bool TryParseEdge(MermaidBlockDocument document, string text, MermaidSourceSpan span, HashSet<string> ids, MermaidParseResult<MermaidDocument> result) {
        var arrows = new[] { "-->", "---", "==>", "===", "-.->", "-.-", "~~~" };
        for (var index = 0; index < arrows.Length; index++) {
            var arrow = arrows[index];
            var position = text.IndexOf(arrow, StringComparison.Ordinal);
            if (position <= 0) continue;
            var left = text.Substring(0, position).Trim();
            var right = text.Substring(position + arrow.Length).Trim();
            var label = string.Empty;
            if (right.StartsWith("|", StringComparison.Ordinal)) {
                var end = right.IndexOf('|', 1);
                if (end > 1) {
                    label = right.Substring(1, end - 1).Trim();
                    right = right.Substring(end + 1).Trim();
                }
            }

            var edgeSpaceIndex = 0;
            if (!TryParseItem(left, span, ref edgeSpaceIndex, out var source, out _) || source.IsSpace || !TryParseItem(right, span, ref edgeSpaceIndex, out var target, out _) || target.IsSpace) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid block edge endpoints could not be parsed into renderable block ids.");
                return true;
            }

            AddImplicitItem(document, source, ids);
            AddImplicitItem(document, target, ids);
            document.Edges.Add(new MermaidBlockEdge(source.Id, target.Id, label, arrow.IndexOf('>') >= 0, span));
            return true;
        }

        return false;
    }

    private static void AddImplicitItem(MermaidBlockDocument document, MermaidBlockItem item, HashSet<string> ids) {
        if (ids.Contains(item.Id)) return;
        ids.Add(item.Id);
        document.Items.Add(item);
    }

    private static bool TryParseItem(string token, MermaidSourceSpan span, ref int spaceIndex, out MermaidBlockItem item, out string diagnostic) {
        item = null!;
        diagnostic = string.Empty;
        token = token.Trim();
        if (token.Length == 0) {
            diagnostic = "Empty Mermaid block tokens are ignored.";
            return false;
        }

        if (token.StartsWith("space", StringComparison.OrdinalIgnoreCase)) {
            var width = 1;
            var colon = token.IndexOf(':');
            if (colon >= 0 && (!int.TryParse(token.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out width) || width < 1 || width > 24)) {
                diagnostic = "Mermaid block space widths must be between one and twenty-four columns.";
                return false;
            }

            item = new MermaidBlockItem("space-" + (++spaceIndex).ToString(CultureInfo.InvariantCulture), string.Empty, width, BlockLayoutShape.Rectangle, true, span);
            return true;
        }

        var columnSpan = ExtractSpan(ref token);
        var shapeStart = FirstShapeIndex(token);
        var id = (shapeStart < 0 ? token : token.Substring(0, shapeStart)).Trim();
        if (id.Length == 0) {
            diagnostic = "Mermaid block items must define an id.";
            return false;
        }

        var shape = BlockLayoutShape.Rectangle;
        var label = id;
        if (shapeStart >= 0) {
            var shapeText = token.Substring(shapeStart);
            label = MermaidParserUtilities.Unquote(ExtractLabel(shapeText));
            if (label.Length == 0) label = id;
            if (shapeText.IndexOf("((", StringComparison.Ordinal) >= 0) shape = BlockLayoutShape.Circle;
            else if (shapeText.IndexOf("[(", StringComparison.Ordinal) >= 0) shape = BlockLayoutShape.Database;
            else if (shapeText.IndexOf("(", StringComparison.Ordinal) >= 0) shape = BlockLayoutShape.Rounded;
        }

        item = new MermaidBlockItem(id, label, columnSpan, shape, false, span);
        return true;
    }

    private static int ExtractSpan(ref string token) {
        var colon = token.LastIndexOf(':');
        if (colon <= 0 || colon == token.Length - 1) return 1;
        if (!int.TryParse(token.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var span)) return 1;
        token = token.Substring(0, colon);
        return Math.Max(1, Math.Min(24, span));
    }

    private static int FirstShapeIndex(string token) {
        var best = -1;
        var markers = new[] { '[', '(', '{', '<' };
        for (var index = 0; index < markers.Length; index++) {
            var position = token.IndexOf(markers[index]);
            if (position >= 0 && (best < 0 || position < best)) best = position;
        }

        return best;
    }

    private static string ExtractLabel(string shapeText) {
        var firstQuote = shapeText.IndexOf('"');
        if (firstQuote >= 0) {
            var secondQuote = shapeText.IndexOf('"', firstQuote + 1);
            if (secondQuote > firstQuote) return shapeText.Substring(firstQuote, secondQuote - firstQuote + 1);
        }

        var start = 0;
        while (start < shapeText.Length && !char.IsLetterOrDigit(shapeText[start])) start++;
        var end = shapeText.Length - 1;
        while (end >= start && !char.IsLetterOrDigit(shapeText[end])) end--;
        return end >= start ? shapeText.Substring(start, end - start + 1) : string.Empty;
    }

    private static List<string> SplitTokens(string text) {
        var tokens = new List<string>();
        var start = -1;
        var depth = 0;
        var inQuote = false;
        for (var index = 0; index < text.Length; index++) {
            var c = text[index];
            if (c == '"') inQuote = !inQuote;
            if (!inQuote) {
                if (c == '[' || c == '(' || c == '{' || c == '<') depth++;
                else if ((c == ']' || c == ')' || c == '}' || c == '>') && depth > 0) depth--;
            }

            if (char.IsWhiteSpace(c) && depth == 0 && !inQuote) {
                if (start >= 0) tokens.Add(text.Substring(start, index - start));
                start = -1;
            } else if (start < 0) {
                start = index;
            }
        }

        if (start >= 0) tokens.Add(text.Substring(start));
        return tokens;
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]) || text[keyword.Length] == ':';
    }

    private static bool IsStyleStatement(string text) =>
        StartsWithKeyword(text, "style") || StartsWithKeyword(text, "class") || StartsWithKeyword(text, "classDef");
}
