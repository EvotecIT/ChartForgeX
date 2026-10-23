using System;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidJourneyParser {
    public static void ParseStatements(MermaidJourneyDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        MermaidJourneySection? currentSection = null;

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

            if (StartsWithKeyword(trimmed, "section")) {
                if (trimmed.Substring(7).Trim().Length == 0) {
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Journey sections require a label.");
                    continue;
                }
                currentSection = new MermaidJourneySection(trimmed.Substring(7).Trim(), span);
                document.Sections.Add(currentSection);
                continue;
            }

            ParseTask(document, trimmed, currentSection, span, result);
        }

        if (document.Tasks.Count == 0 && !result.HasErrors) {
            MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid journey diagrams require at least one scored task.");
        }
    }

    private static void ParseTask(MermaidJourneyDocument document, string text, MermaidJourneySection? section, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
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

        if (score < 1 || score > 5) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Journey scores must be between one and five, inclusive.");
            return;
        }
        var task = new MermaidJourneyTask(taskText, section, score, span);
        if (colonParts.Length > 2) {
            var actorsText = string.Join(":", colonParts, 2, colonParts.Length - 2);
            foreach (var actor in MermaidParserUtilities.SplitCsvLike(actorsText)) {
                var value = MermaidParserUtilities.Unquote(actor);
                if (value.Length > 0 && !task.Actors.Contains(value)) task.Actors.Add(value);
            }
        }

        document.Tasks.Add(task);
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }
}
