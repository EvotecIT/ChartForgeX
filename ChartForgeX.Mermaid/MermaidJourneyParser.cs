using System;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidJourneyParser {
    public static void ParseStatements(MermaidJourneyDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        string? currentSection = null;

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

            if (StartsWithKeyword(trimmed, "section")) {
                currentSection = trimmed.Substring(7).Trim();
                document.Sections.Add(new MermaidJourneySection(currentSection, span));
                continue;
            }

            ParseTask(document, trimmed, currentSection, span, result);
        }

        if (document.Tasks.Count == 0) {
            MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid journey diagrams require at least one scored task.");
        }
    }

    private static void ParseTask(MermaidJourneyDocument document, string text, string? section, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var colonParts = text.Split(':');
        if (colonParts.Length < 2) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Journey tasks must use Mermaid syntax 'task: score: actor, actor'.");
            return;
        }

        var taskText = colonParts[0].Trim();
        var scoreText = colonParts[1].Trim();
        if (taskText.Length == 0 || !double.TryParse(scoreText, NumberStyles.Float, CultureInfo.InvariantCulture, out var score) || double.IsNaN(score) || double.IsInfinity(score)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Journey tasks require non-empty task text and a numeric score.");
            return;
        }

        var task = new MermaidJourneyTask(taskText, section, score, span);
        if (colonParts.Length > 2) {
            var actorsText = string.Join(":", colonParts, 2, colonParts.Length - 2);
            foreach (var actor in MermaidParserUtilities.SplitCsvLike(actorsText)) {
                var value = MermaidParserUtilities.Unquote(actor);
                if (value.Length > 0) task.Actors.Add(value);
            }
        }

        document.Tasks.Add(task);
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }
}
