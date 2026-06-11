using System;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidQuadrantParser {
    public static void ParseStatements(MermaidQuadrantDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
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
            ParsePoint(document, trimmed, span, result);
        }

        if (document.Points.Count == 0) {
            MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid quadrant charts require at least one point.");
        }
    }

    private static void ParseAxis(string text, out string start, out string end) {
        var parts = text.Split(new[] { "-->" }, StringSplitOptions.None);
        start = parts.Length > 0 ? parts[0].Trim() : string.Empty;
        end = parts.Length > 1 ? parts[1].Trim() : string.Empty;
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
        var colon = text.IndexOf(':');
        if (colon <= 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Quadrant points must use Mermaid syntax 'label: [x, y]'.");
            return;
        }

        var label = text.Substring(0, colon).Trim();
        var values = text.Substring(colon + 1).Trim();
        if (label.Length == 0 || values.Length < 5 || values[0] != '[' || values[values.Length - 1] != ']') {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Quadrant points require a label and bracketed numeric coordinates.");
            return;
        }

        var parts = MermaidParserUtilities.SplitCsvLike(values.Substring(1, values.Length - 2));
        if (parts.Count != 2 ||
            !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Quadrant point coordinates must contain two finite numbers.");
            return;
        }

        if (x < 0 || x > 1 || y < 0 || y > 1) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Quadrant point coordinates must be normalized values between zero and one.");
            return;
        }

        document.Points.Add(new MermaidQuadrantPoint(label, x, y, span));
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }
}
