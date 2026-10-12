using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttParser {
    private static readonly HashSet<string> KnownTags = new(StringComparer.OrdinalIgnoreCase) { "active", "done", "crit", "milestone", "vert" };

    public static void ParseStatements(MermaidGanttDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        string? currentSection = null;
        var tasks = new List<TaskDefinition>();
        var taskIds = new Dictionary<string, TaskDefinition>(StringComparer.Ordinal);
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal)) continue;

            var span = new MermaidSourceSpan(index + 1, LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "title")) document.Title = trimmed.Substring(5).Trim();
            else if (StartsWithKeyword(trimmed, "dateFormat")) document.DateFormat = trimmed.Substring(10).Trim();
            else if (StartsWithKeyword(trimmed, "axisFormat")) document.AxisFormat = trimmed.Substring(10).Trim();
            else if (StartsWithKeyword(trimmed, "tickInterval")) document.TickInterval = trimmed.Substring(12).Trim();
            else if (StartsWithKeyword(trimmed, "weekday")) {
                var weekday = trimmed.Substring(7).Trim();
                if (Enum.TryParse<DayOfWeek>(weekday, true, out var day) && Enum.IsDefined(typeof(DayOfWeek), day)
                    && string.Equals(weekday, day.ToString(), StringComparison.OrdinalIgnoreCase)) document.Weekday = day;
                else Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt weekday must name a day from monday through sunday.");
            }
            else if (StartsWithKeyword(trimmed, "excludes")) document.Excludes = MergeCalendarTokens(document.Excludes, trimmed.Substring(8));
            else if (StartsWithKeyword(trimmed, "includes")) document.Includes = MergeCalendarTokens(document.Includes, trimmed.Substring(8));
            else if (StartsWithKeyword(trimmed, "weekend")) {
                document.Weekend = trimmed.Substring(7).Trim().ToLowerInvariant();
                if (document.Weekend != "friday" && document.Weekend != "saturday") Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt weekend must start on friday or saturday.");
            }
            else if (StartsWithKeyword(trimmed, "todayMarker")) {
                document.TodayMarker = trimmed.Substring(11).Trim();
                if (document.TodayMarker.Length > 0 && !string.Equals(document.TodayMarker, "off", StringComparison.OrdinalIgnoreCase))
                    MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "Gantt today-marker styling");
            }
            else if (StartsWithKeyword(trimmed, "section")) {
                currentSection = trimmed.Substring(7).Trim();
                if (currentSection.Length == 0) Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt section names must not be empty.");
                else document.Sections.Add(new MermaidGanttSection(currentSection, span));
            } else {
                var task = ParseTaskDefinition(trimmed, span, currentSection, tasks.Count, result);
                if (task == null) continue;
                tasks.Add(task);
                if (!string.IsNullOrWhiteSpace(task.Id)) {
                    if (taskIds.ContainsKey(task.Id!)) Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt task id '" + task.Id + "' is already declared.");
                    else taskIds.Add(task.Id!, task);
                }
            }
        }

        ResolveSchedule(document, tasks, taskIds, result);
        if (document.Tasks.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid Gantt diagrams require at least one task.");
    }

    private static TaskDefinition? ParseTaskDefinition(string text, MermaidSourceSpan span, string? section, int index, MermaidParseResult<MermaidDocument> result) {
        var colon = text.IndexOf(':');
        if (colon <= 0) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt tasks must use 'title : metadata' syntax.");
            return null;
        }

        var title = text.Substring(0, colon).Trim();
        var rawMetadata = text.Substring(colon + 1).Trim();
        if (title.Length == 0 || rawMetadata.Length == 0) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt tasks require a title and metadata.");
            return null;
        }

        var parts = SplitMetadata(rawMetadata);
        var tags = new List<string>();
        while (parts.Count > 0 && KnownTags.Contains(parts[0])) {
            tags.Add(parts[0]);
            parts.RemoveAt(0);
        }

        string? id = null;
        string? startSpec = null;
        string? endSpec = null;
        if (parts.Count == 1) endSpec = parts[0];
        else if (parts.Count == 2) {
            startSpec = parts[0];
            endSpec = parts[1];
        } else if (parts.Count == 3) {
            id = parts[0];
            startSpec = parts[1];
            endSpec = parts[2];
        } else {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt task metadata must contain an optional id, start/after clause, and end date or duration.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(endSpec)) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt tasks require an end date or duration.");
            return null;
        }

        return new TaskDefinition(title, id, section, startSpec, endSpec!, tags, rawMetadata, span, index);
    }

    private static List<string> SplitMetadata(string text) {
        var parts = new List<string>();
        foreach (var raw in text.Split(',')) {
            var part = raw.Trim();
            if (part.Length > 0) parts.Add(part);
        }

        return parts;
    }

    private static bool TryParseDate(string text, string dateFormat, out DateTime value) {
        var format = ToDotNetDateFormat(dateFormat);
        if (DateTime.TryParseExact(text.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value)) return true;
        var fallbackFormats = new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss" };
        if (DateTime.TryParseExact(text.Trim(), fallbackFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value)) return true;
        return DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value);
    }

    internal static string ToDotNetDateFormat(string value) {
        if (string.IsNullOrWhiteSpace(value)) return "yyyy-MM-dd";
        var trimmed = value.Trim();
        if (trimmed.IndexOf('%') >= 0) {
            return trimmed
                .Replace("%Y", "yyyy")
                .Replace("%y", "yy")
                .Replace("%m", "MM")
                .Replace("%d", "dd")
                .Replace("%H", "HH")
                .Replace("%M", "mm")
                .Replace("%S", "ss")
                .Replace("%b", "MMM")
                .Replace("%B", "MMMM");
        }

        return trimmed
            .Replace("YYYY", "yyyy")
            .Replace("YY", "yy")
            .Replace("DD", "dd");
    }

    private static bool ContainsTag(List<string> tags, string tag) => tags.Exists(item => string.Equals(item, tag, StringComparison.OrdinalIgnoreCase));

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }

    private static int LeadingWhitespace(string text) {
        var count = 0;
        while (count < text.Length && char.IsWhiteSpace(text[count])) count++;
        return count;
    }

    private static void Add<TDocument>(MermaidParseResult<TDocument> result, int line, int column, int length, MermaidDiagnosticSeverity severity, string message) where TDocument : MermaidDocument {
        result.Diagnostics.Add(new MermaidDiagnostic {
            Span = new MermaidSourceSpan(line, column, length),
            Severity = severity,
            Message = message
        });
    }
}
