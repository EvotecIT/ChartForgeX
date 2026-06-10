using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static class MermaidGanttParser {
    private static readonly HashSet<string> KnownTags = new(StringComparer.OrdinalIgnoreCase) { "active", "done", "crit", "milestone" };

    public static void ParseStatements(MermaidGanttDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        string? currentSection = null;
        DateTime? previousEnd = null;
        var taskIds = new Dictionary<string, MermaidGanttTask>(StringComparer.Ordinal);
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
            else if (StartsWithKeyword(trimmed, "excludes")) document.Excludes = trimmed.Substring(8).Trim();
            else if (StartsWithKeyword(trimmed, "todayMarker")) document.TodayMarker = trimmed.Substring(11).Trim();
            else if (StartsWithKeyword(trimmed, "section")) {
                currentSection = trimmed.Substring(7).Trim();
                if (currentSection.Length == 0) Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt section names must not be empty.");
                else document.Sections.Add(new MermaidGanttSection(currentSection, span));
            } else {
                var task = ParseTask(trimmed, span, currentSection, previousEnd, document.Tasks, taskIds, document.DateFormat, result);
                if (task == null) continue;
                document.Tasks.Add(task);
                previousEnd = task.End;
                if (!string.IsNullOrWhiteSpace(task.Id)) {
                    if (taskIds.ContainsKey(task.Id!)) Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt task id '" + task.Id + "' is already declared.");
                    else taskIds.Add(task.Id!, task);
                }
            }
        }

        if (document.Tasks.Count == 0) Add(result, document.HeaderSpan.Line, document.HeaderSpan.Column, document.HeaderSpan.Length, MermaidDiagnosticSeverity.Error, "Mermaid Gantt diagrams require at least one task.");
    }

    private static MermaidGanttTask? ParseTask(string text, MermaidSourceSpan span, string? section, DateTime? previousEnd, IReadOnlyList<MermaidGanttTask> previousTasks, Dictionary<string, MermaidGanttTask> taskIds, string dateFormat, MermaidParseResult<MermaidDocument> result) {
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

        var dependencies = new List<string>();
        var dependencyIndex = -1;
        DateTime start;
        if (string.IsNullOrWhiteSpace(startSpec)) {
            if (!previousEnd.HasValue) {
                Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "The first Gantt task must declare an explicit start date.");
                return null;
            }

            start = previousEnd.Value;
        } else {
            var concreteStartSpec = startSpec!;
            if (StartsWithKeyword(concreteStartSpec, "after")) {
                if (!ResolveAfter(concreteStartSpec, previousTasks, taskIds, dependencies, out start, out dependencyIndex)) {
                    Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt after clauses must reference earlier task ids.");
                    return null;
                }
            } else if (!TryParseDate(concreteStartSpec, dateFormat, out start)) {
                Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt task start dates must match dateFormat '" + dateFormat + "'.");
                return null;
            }
        }

        DateTime end;
        if (TryParseDuration(endSpec, out var duration)) end = start.Add(duration);
        else if (!TryParseDate(endSpec, dateFormat, out end)) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt task end values must be dates or durations.");
            return null;
        }

        if (end < start) {
            Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, "Gantt task end must be greater than or equal to start.");
            return null;
        }

        var milestone = ContainsTag(tags, "milestone");
        var progress = milestone || ContainsTag(tags, "done") ? 1.0 : ContainsTag(tags, "active") ? 0.5 : 0.0;
        if (milestone) end = start;
        var task = new MermaidGanttTask(title, id, section, start, end, progress, milestone, tags, dependencies, rawMetadata, span) {
            DependencyIndex = dependencyIndex
        };
        return task;
    }

    private static bool ResolveAfter(string text, IReadOnlyList<MermaidGanttTask> previousTasks, Dictionary<string, MermaidGanttTask> taskIds, List<string> dependencies, out DateTime start, out int dependencyIndex) {
        start = default;
        dependencyIndex = -1;
        var ids = text.Substring(5).Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (ids.Length == 0) return false;
        DateTime? latestEnd = null;
        var latestTaskIndex = -1;
        foreach (var id in ids) {
            if (!taskIds.TryGetValue(id, out var task)) return false;
            dependencies.Add(id);
            if (!latestEnd.HasValue || task.End > latestEnd.Value) {
                latestEnd = task.End;
                latestTaskIndex = IndexOf(previousTasks, task);
            }
        }

        if (latestTaskIndex < 0) return false;
        start = latestEnd!.Value;
        dependencyIndex = latestTaskIndex;
        return true;
    }

    private static int IndexOf(IReadOnlyList<MermaidGanttTask> tasks, MermaidGanttTask task) {
        for (var i = 0; i < tasks.Count; i++) if (ReferenceEquals(tasks[i], task)) return i;
        return -1;
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

    private static bool TryParseDuration(string text, out TimeSpan duration) {
        text = text.Trim();
        duration = default;
        var index = 0;
        while (index < text.Length && (char.IsDigit(text[index]) || text[index] == '.')) index++;
        if (index == 0 || index == text.Length) return false;
        if (!double.TryParse(text.Substring(0, index), NumberStyles.Float, CultureInfo.InvariantCulture, out var amount) || amount < 0) return false;
        var unit = text.Substring(index).Trim().ToLowerInvariant();
        switch (unit) {
            case "ms":
            case "millisecond":
            case "milliseconds":
                duration = TimeSpan.FromMilliseconds(amount);
                return true;
            case "s":
            case "second":
            case "seconds":
                duration = TimeSpan.FromSeconds(amount);
                return true;
            case "m":
            case "minute":
            case "minutes":
                duration = TimeSpan.FromMinutes(amount);
                return true;
            case "h":
            case "hour":
            case "hours":
                duration = TimeSpan.FromHours(amount);
                return true;
            case "d":
            case "day":
            case "days":
                duration = TimeSpan.FromDays(amount);
                return true;
            case "w":
            case "week":
            case "weeks":
                duration = TimeSpan.FromDays(amount * 7);
                return true;
            default:
                return false;
        }
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
