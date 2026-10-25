using System;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidQuadrantParser {
    public static void ParseStatements(MermaidQuadrantDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[index];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;

            var span = new MermaidSourceSpan(index + 1, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "title")) {
                document.Title = trimmed.Substring(5).Trim();
                continue;
            }

            if (StartsWithKeyword(trimmed, "x-axis")) {
                ParseAxis(trimmed.Substring(6), out var start, out var end);
                document.XAxisStart = start;
                document.XAxisEnd = end;
                continue;
            }

            if (StartsWithKeyword(trimmed, "y-axis")) {
                ParseAxis(trimmed.Substring(6), out var start, out var end);
                document.YAxisStart = start;
                document.YAxisEnd = end;
                continue;
            }

            if (TryParseQuadrantLabel(document, trimmed)) continue;
            if (MermaidQuadrantStyles.TryDefinition(document, trimmed, span, result)) continue;
            ParsePoint(document, trimmed, span, result);
        }

    }

    private static void ParseAxis(string text, out string start, out string end) {
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var ch = text[index];
            if (quote != '\0') { if (ch == quote) quote = '\0'; continue; }
            if (ch == '"' || ch == '`') { quote = ch; continue; }
            if (ch != '-') continue;
            var next = index; while (next < text.Length && text[next] == '-') next++;
            if (next - index < 2 || next >= text.Length || text[next] != '>') continue;
            start = MermaidParserUtilities.Unquote(text.Substring(0, index).Trim());
            end = MermaidParserUtilities.Unquote(text.Substring(next + 1).Trim());
            if (end.Length == 0) start += " \u27f6";
            return;
        }
        start = MermaidParserUtilities.Unquote(text.Trim()); end = string.Empty;
    }

    private static bool TryParseQuadrantLabel(MermaidQuadrantDocument document, string text) {
        for (var quadrant = 1; quadrant <= 4; quadrant++) {
            var prefix = "quadrant-" + quadrant.ToString(CultureInfo.InvariantCulture);
            if (!StartsWithKeyword(text, prefix)) continue;
            document.QuadrantLabels[quadrant] = text.Substring(prefix.Length).Trim();
            return true;
        }

        return false;
    }

    private static void ParsePoint(MermaidQuadrantDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var colon = CoordinateColon(text);
        if (colon <= 0) { Invalid("Quadrant points must use Mermaid syntax 'label: [x, y]'."); return; }
        var head = text.Substring(0, colon).Trim(); var open = text.IndexOf('[', colon + 1); var close = text.IndexOf(']', open + 1);
        if (open < 0 || close < 0) { Invalid("Quadrant points require a label and bracketed numeric coordinates."); return; }
        var suffix = MermaidStyleParser.Suffix(head); string? className = null;
        if (suffix >= 0) {
            className = head.Substring(suffix + 3).Trim(); head = head.Substring(0, suffix).Trim();
            if (!MermaidQuadrantStyles.IsInlineClassName(className)) {
                Invalid("Quadrant inline classes require one word after :::."); return;
            }
        }
        var label = MermaidParserUtilities.Unquote(head);
        if (label.Length == 0) { Invalid("Quadrant points require a nonempty label."); return; }
        var parts = MermaidParserUtilities.SplitCsvLike(text.Substring(open + 1, close - open - 1));
        if (parts.Count != 2 || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
            || double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y)) {
            Invalid("Quadrant point coordinates must contain two finite numbers."); return;
        }
        if (x < 0 || x > 1 || y < 0 || y > 1) { Invalid("Quadrant point coordinates must be normalized values between zero and one."); return; }
        var point = new MermaidQuadrantPoint(label, x, y, span) { ClassName = className };
        var style = text.Substring(close + 1).Trim().TrimEnd(';').TrimEnd();
        if (style.Length > 0) MermaidQuadrantStyles.Read(point.Styles, style, span, result);
        MermaidResourceGuard.ReserveElements(point.Styles.Count + (className == null ? 0 : 1), span);
        document.Points.Add(point);

        void Invalid(string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
    }

    private static int CoordinateColon(string text) {
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var ch = text[index];
            if (quote != '\0') { if (ch == quote) quote = '\0'; continue; }
            if (ch == '"' || ch == '`') { quote = ch; continue; }
            if (ch != ':' || index + 1 < text.Length && text[index + 1] == ':') continue;
            var next = index + 1; while (next < text.Length && char.IsWhiteSpace(text[next])) next++;
            if (next < text.Length && text[next] == '[') return index;
        }
        return -1;
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }
}
