using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttParser {
    private static readonly HashSet<string> KnownTags = new(StringComparer.OrdinalIgnoreCase) { "active", "done", "crit", "milestone", "vert" };

    public static void ParseStatements(MermaidGanttDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        string? currentSection = null;
        var tasks = new List<TaskDefinition>();
        var taskStatements = new List<(MermaidRawStatement Statement, string? Section)>();
        var taskIds = new Dictionary<string, TaskDefinition>(StringComparer.Ordinal);
        for (var index = Math.Max(0, startLine - 1); index < lines.Length; index++) {
            var raw = lines[index];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal)) continue;

            var span = new MermaidSourceSpan(index + 1, LeadingWhitespace(raw) + 1, trimmed.Length);
            var statement = new MermaidRawStatement(trimmed, span);
            document.Statements.Add(statement);

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
                MermaidGanttTodayStyle.Parse(document.TodayMarker, out var supportedStyle);
                if (!supportedStyle)
                    MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "Gantt today-marker styling");
            }
            else if (StartsWithKeyword(trimmed, "section")) {
                currentSection = trimmed.Substring(7).Trim();
                if (currentSection.Length == 0) Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt section names must not be empty.");
                else document.Sections.Add(new MermaidGanttSection(currentSection, span));
            } else taskStatements.Add((statement, currentSection));
        }
        foreach (var declaration in taskStatements) {
            var task = ParseTaskDefinition(declaration.Statement.Text, declaration.Statement.Span, declaration.Section, tasks.Count, document, result);
            if (task == null) continue;
            tasks.Add(task);
            if (!string.IsNullOrWhiteSpace(task.Id)) {
                if (taskIds.ContainsKey(task.Id!)) Add(result, task.Span.Line, task.Span.Column, task.Span.Length, MermaidDiagnosticSeverity.Error, "Gantt task id '" + task.Id + "' is already declared.");
                else taskIds.Add(task.Id!, task);
            }
        }

        // Complete task-date fallbacks remain available; calendar matching also formats the declared fields.
        if ((!string.IsNullOrWhiteSpace(document.Excludes) || !string.IsNullOrWhiteSpace(document.Includes))
            && !ValidateDateFormat(document, result)) return;
        if (!ValidateCalendarAnchor(document, result)) return;
        ResolveSchedule(document, tasks, taskIds, result);
        if (document.Tasks.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid Gantt diagrams require at least one task.");
    }

    private static bool ValidateDateFormat(MermaidGanttDocument document, MermaidParseResult<MermaidDocument> result) {
        try {
            if (document.DateFormat.Length <= 256) {
                MermaidGanttInputDateFormat.FormatCalendarDate(new DateTime(2000, 2, 29, 12, 34, 56), document.DateFormat);
                return true;
            }
        } catch (FormatException) { }
        var statement = document.Statements.FindLast(item => StartsWithKeyword(item.Text, "dateFormat"));
        var span = statement?.Span ?? document.HeaderSpan;
        var text = statement?.Text ?? string.Empty;
        var offset = text.Length > 10 ? 10 + LeadingWhitespace(text.Substring(10)) : text.Length;
        Add(result, span.Line, span.Column + offset, document.DateFormat.Length, MermaidDiagnosticSeverity.Error,
            "Gantt dateFormat must define a valid format of at most 256 characters.");
        return false;
    }

    private static TaskDefinition? ParseTaskDefinition(string text, MermaidSourceSpan span, string? section, int index, MermaidGanttDocument document, MermaidParseResult<MermaidDocument> result) {
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

        var parts = SplitMetadata(rawMetadata, document);
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

    private static List<string> SplitMetadata(string text, MermaidGanttDocument document) {
        var chunks = text.Split(',');
        // Complete English timestamps can contain weekday, date and date/time commas.
        var maximumCommas = 3;
        if (document.DateFormat.Length <= 256) {
            try { maximumCommas = Math.Max(maximumCommas, MermaidGanttInputDateFormat.CalendarCommaCount(document.DateFormat)); }
            catch (FormatException) { /* Complete-date fallbacks can still parse an unused invalid format. */ }
        }
        var prefix = new List<string>();
        var first = 0;
        while (first < chunks.Length && (string.IsNullOrWhiteSpace(chunks[first]) || KnownTags.Contains(chunks[first].Trim()))) {
            if (StartsConfiguredDate(first)) break;
            if (!string.IsNullOrWhiteSpace(chunks[first])) prefix.Add(chunks[first].Trim());
            first++;
        }
        // An authored complete end date takes precedence over fallback parsing of its
        // comma-separated pieces as an explicit start/end pair.
        if (first < chunks.Length && chunks.Length - first <= maximumCommas + 1) {
            var end = string.Join(",", chunks, first, chunks.Length - first).Trim();
            if (MermaidGanttInputDateFormat.TryParseConfigured(end, document.DateFormat, out _)) {
                prefix.Add(end);
                return prefix;
            }
        }
        // Configured dates have priority, followed by complete fallback dates containing
        // commas. Equal matches preserve explicit IDs, including numeric IDs.
        List<string>? selected = null;
        var bestScore = -1;
        var weekdayPrefix = first < chunks.Length && MermaidGanttInputDateFormat.IsWeekdayName(chunks[first].Trim());
        foreach (var hasId in new[] { true, false }) {
            var startIndex = first + (hasId ? 1 : 0);
            if (startIndex + 1 >= chunks.Length || hasId && string.IsNullOrWhiteSpace(chunks[first])) continue;
            var start = chunks[startIndex];
            for (var boundary = startIndex; boundary < chunks.Length - 1 && boundary <= startIndex + maximumCommas; boundary++) {
                if (boundary > startIndex) start += "," + chunks[boundary];
                var endCount = chunks.Length - boundary - 1;
                if (endCount > maximumCommas + 1) continue;
                var end = string.Join(",", chunks, boundary + 1, endCount).Trim();
                if (!IsStart(start.Trim()) || !IsEnd(end)) continue;
                var matches = (MermaidGanttInputDateFormat.TryParseConfigured(start.Trim(), document.DateFormat, out _, document.DateAnchor) ? 1 : 0)
                    + (MermaidGanttInputDateFormat.TryParseConfigured(end, document.DateFormat, out _, document.DateAnchor) ? 1 : 0);
                // A new weekday-prefixed fallback must not consume an existing viable task ID.
                var score = matches * 3 + (start.IndexOf(',') >= 0 ? 1 : 0) + (end.IndexOf(',') >= 0 ? 1 : 0)
                    + (hasId && weekdayPrefix ? 1 : 0);
                if (score <= bestScore) continue;
                var parts = new List<string>(prefix);
                if (hasId) parts.Add(chunks[first].Trim());
                parts.Add(start.Trim());
                parts.Add(end);
                selected = parts;
                bestScore = score;
            }
        }
        // A complete fallback end inherits the previous task's end. Do not split its
        // date and time into an explicit pair unless a configured format wins.
        if (bestScore < 3 && first < chunks.Length && chunks.Length - first <= maximumCommas + 1) {
            var end = string.Join(",", chunks, first, chunks.Length - first).Trim();
            if (IsEnd(end)) { prefix.Add(end); return prefix; }
        }
        if (selected != null) return selected;
        // Preserve existing error locations and diagnostics when no complete arrangement parses.
        var original = new List<string>();
        foreach (var chunk in chunks) if (chunk.Trim().Length > 0) original.Add(chunk.Trim());
        return original;

        bool StartsConfiguredDate(int startIndex) {
            if (chunks.Length - startIndex <= maximumCommas + 1
                && MermaidGanttInputDateFormat.TryParseConfigured(string.Join(",", chunks, startIndex, chunks.Length - startIndex).Trim(), document.DateFormat, out _, document.DateAnchor)) return true;
            var start = chunks[startIndex];
            for (var boundary = startIndex; boundary < chunks.Length - 1 && boundary <= startIndex + maximumCommas; boundary++) {
                if (boundary > startIndex) start += "," + chunks[boundary];
                var endCount = chunks.Length - boundary - 1;
                if (endCount <= maximumCommas + 1
                    && MermaidGanttInputDateFormat.TryParseConfigured(start.Trim(), document.DateFormat, out _, document.DateAnchor)
                    && IsEnd(string.Join(",", chunks, boundary + 1, endCount).Trim())) return true;
            }
            return false;
        }

        bool IsStart(string value) => IsClause(value, "after") || TryParseDate(value, document.DateFormat, out _, document.DateAnchor);
        bool IsEnd(string value) => IsClause(value, "until") || TryParseDuration(value, out _, out _) || TryParseDate(value, document.DateFormat, out _, document.DateAnchor);
        bool IsClause(string value, string keyword) => value.IndexOf(',') < 0 && StartsWithKeyword(value, keyword) && value.Substring(keyword.Length).Trim().Length > 0;
    }

    private static bool TryParseDate(string text, string dateFormat, out DateTime value, DateTime? dateAnchor = null) {
        value = default;
        if (dateFormat.Length > 256) return false;
        try {
            if (MermaidGanttInputDateFormat.TryParseConfigured(text, dateFormat, out value, dateAnchor)) return true;
            // Legacy complete-date spellings remain available; unrestricted platform parsing
            // can silently acquire today's year or date from partial text.
            return MermaidGanttInputDateFormat.TryParseCompleteFallback(text, out value);
        } catch (FormatException) { return false; }
        catch (RegexMatchTimeoutException) { return false; }
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
        MermaidParserUtilities.Add(result, new MermaidSourceSpan(line, column, length), severity, message);
    }
}
