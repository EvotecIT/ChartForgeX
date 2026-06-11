using System;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidPacketParser {
    private const int MaximumPacketBits = 10000;

    public static void ParseStatements(MermaidPacketDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var expectedStart = 0;
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "title")) {
                var title = trimmed.Substring(5).Trim();
                document.Title = MermaidParserUtilities.Unquote(title);
                continue;
            }

            if (!TryParseField(trimmed, expectedStart, span, out var field, out var diagnostic)) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, diagnostic);
                continue;
            }

            if (field.StartBit != expectedStart) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid packet fields must be contiguous from bit zero. Expected bit " + expectedStart.ToString(CultureInfo.InvariantCulture) + " but found bit " + field.StartBit.ToString(CultureInfo.InvariantCulture) + ".");
                continue;
            }

            if (field.EndBit == int.MaxValue) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid packet bit ranges are too large.");
                continue;
            }

            if (field.EndBit + 1 > MaximumPacketBits) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid packet total bit length is too large.");
                continue;
            }

            document.Fields.Add(field);
            expectedStart = field.EndBit + 1;
        }

        if (document.Fields.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid packet diagrams require at least one packet field.");
    }

    private static bool TryParseField(string text, int expectedStart, MermaidSourceSpan span, out MermaidPacketField field, out string diagnostic) {
        field = null!;
        diagnostic = string.Empty;
        var colon = text.IndexOf(':');
        if (colon <= 0) {
            diagnostic = "Mermaid packet fields must use '<start>-<end>: \"Label\"', '<start>: \"Label\"', or '+<bits>: \"Label\"' syntax.";
            return false;
        }

        var range = text.Substring(0, colon).Trim();
        var label = MermaidParserUtilities.Unquote(text.Substring(colon + 1));
        if (label.Length == 0) {
            diagnostic = "Mermaid packet fields must define a label.";
            return false;
        }

        int start;
        int end;
        if (range.StartsWith("+", StringComparison.Ordinal)) {
            if (!int.TryParse(range.Substring(1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) || length <= 0) {
                diagnostic = "Mermaid packet '+bits' fields must use a positive bit count.";
                return false;
            }

            start = expectedStart;
            if (length - 1 > int.MaxValue - start) {
                diagnostic = "Mermaid packet bit ranges are too large.";
                return false;
            }

            end = start + length - 1;
        } else {
            var dash = range.IndexOf('-');
            if (dash >= 0) {
                if (!TryParseBit(range.Substring(0, dash), out start) || !TryParseBit(range.Substring(dash + 1), out end)) {
                    diagnostic = "Mermaid packet field ranges must use non-negative integer bit positions.";
                    return false;
                }
            } else {
                if (!TryParseBit(range, out start)) {
                    diagnostic = "Mermaid packet field starts must use non-negative integer bit positions.";
                    return false;
                }

                end = start;
            }
        }

        if (end < start) {
            diagnostic = "Mermaid packet field end bit must be greater than or equal to the start bit.";
            return false;
        }

        if ((long)end - start + 1 > MaximumPacketBits) {
            diagnostic = "Mermaid packet bit ranges are too large.";
            return false;
        }

        field = new MermaidPacketField(start, end, label, span);
        return true;
    }

    private static bool TryParseBit(string value, out int bit) =>
        int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out bit) && bit >= 0;

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }
}
