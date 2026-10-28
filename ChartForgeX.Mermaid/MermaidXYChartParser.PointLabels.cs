using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidXYChartParser {
    // XY's pinned datum grammar is NUMBER [STR], not an object, colon pair or a second numeric series.
    private static List<double> ParseDatumList(string text, MermaidSourceSpan span,
        List<MermaidXYChartPointLabel> labels, MermaidParseResult<MermaidDocument> result) {
        var values = new List<double>();
        var start = 0; var quoted = false;
        for (var index = 0; index <= text.Length; index++) {
            var end = index == text.Length;
            if (!end) {
                var ch = text[index];
                if (ch == '"') { quoted = !quoted; continue; }
                if (ch != ',' || quoted) continue;
            }
            var part = text.Substring(start, index - start);
            var leading = part.Length - part.TrimStart().Length;
            var item = part.Trim();
            var itemSpan = new MermaidSourceSpan(span.Line, span.Column + start + leading, Math.Max(1, item.Length));
            ParseDatum(item, itemSpan, values, labels, result);
            start = index + 1;
        }
        return values;
    }

    private static void ParseDatum(string text, MermaidSourceSpan span, List<double> values,
        List<MermaidXYChartPointLabel> labels, MermaidParseResult<MermaidDocument> result) {
        var quote = text.IndexOf('"');
        var number = quote < 0 ? text : text.Substring(0, quote).TrimEnd();
        if (!TryParseNumber(number, out var value)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "XY chart data must be a finite number followed by an optional quoted caption.");
            return;
        }
        if (quote < 0) { values.Add(value); return; }
        var close = text.IndexOf('"', quote + 1);
        if (close < 0) close = text.Length;
        if (close == text.Length || text.Substring(close + 1).Trim().Length != 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "An XY datum caption must be one complete quoted string after its numeric value.");
            return;
        }
        var raw = text.Substring(quote, close - quote + 1);
        var labelSpan = new MermaidSourceSpan(span.Line, span.Column + quote, raw.Length);
        var label = new MermaidXYChartPointLabel(values.Count, raw.Substring(1, raw.Length - 2), raw, labelSpan);
        values.Add(value); labels.Add(label);
        // Shared caption lowering retains raw strings and reports rich HTML/math losses before artifact creation.
        MermaidLabelText.Diagnose(label.Text, labelSpan, result, "XY datum caption");
    }
}
