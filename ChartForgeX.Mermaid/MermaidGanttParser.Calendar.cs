using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttParser {
    private static bool ValidateCalendarAnchor(MermaidGanttDocument document, MermaidParseResult<MermaidDocument> result) {
        if (document.DateAnchor.HasValue || !MermaidGanttInputDateFormat.OmitsYear(document.DateFormat)) return true;
        var valid = true;
        foreach (var statement in document.Statements) {
            var offset = StartsWithKeyword(statement.Text, "excludes") || StartsWithKeyword(statement.Text, "includes") ? 8 : 0;
            if (offset == 0) continue;
            CalendarTokens(statement.Text.Substring(offset), document.DateFormat, null, (token, tokenOffset) => {
                Add(result, statement.Span.Line, statement.Span.Column + offset + tokenOffset, token.Length, MermaidDiagnosticSeverity.Error,
                    "Gantt calendar dates using '" + document.DateFormat + "' require MermaidParseOptions.GanttDateAnchor or complete ISO dates.");
                valid = false;
            });
        }
        return valid;
    }

    // Duration-based ends advance past excluded calendar dates. Explicit ends remain authored dates.
    private static bool TryResolveDurationEnd(DateTime start, double amount, string unit, MermaidGanttDocument document, out DateTime end, out DateTime renderEnd) {
        end = renderEnd = start;
        try {
            return TryResolveCalendarEnd(start, AddDuration(start, amount, unit), document, out end, out renderEnd);
        } catch (ArgumentOutOfRangeException) { return false; }
        catch (OverflowException) { return false; }
    }

    private static bool TryResolveCalendarEnd(DateTime start, DateTime authoredEnd, MermaidGanttDocument document, out DateTime end, out DateTime renderEnd) {
        end = authoredEnd;
        renderEnd = authoredEnd;
        try {
            var excluded = CalendarTokens(document.Excludes, document.DateFormat, document.DateAnchor);
            if (excluded.Count == 0 || end - start < TimeSpan.FromDays(1)) return true;
            var included = CalendarTokens(document.Includes, document.DateFormat, document.DateAnchor);
            var cursor = start.AddDays(1);
            var extension = 0;
            var previousExcluded = false;
            while (cursor <= end) {
                // Mermaid keeps trailing excluded days for scheduling while the bar
                // ends before that trailing run. Interior exclusions still extend it.
                if (!previousExcluded) renderEnd = end;
                var date = "iso:" + cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var formatted = MermaidGanttInputDateFormat.CalendarKey(cursor, document.DateFormat);
                var weekday = cursor.DayOfWeek.ToString().ToLowerInvariant();
                var weekend = document.Weekend == "friday"
                    ? cursor.DayOfWeek == DayOfWeek.Friday || cursor.DayOfWeek == DayOfWeek.Saturday
                    : cursor.DayOfWeek == DayOfWeek.Saturday || cursor.DayOfWeek == DayOfWeek.Sunday;
                previousExcluded = !included.Contains(date) && !included.Contains(formatted) &&
                    (excluded.Contains(date) || excluded.Contains(formatted) || excluded.Contains(weekday) || (weekend && excluded.Contains("weekends")));
                if (previousExcluded) {
                    if (++extension > 10000) return false;
                    end = end.AddDays(1);
                }
                if (end - cursor < TimeSpan.FromDays(1)) break;
                cursor = cursor.AddDays(1);
            }
            return true;
        } catch (ArgumentOutOfRangeException) { return false; }
        catch (OverflowException) { return false; }
    }

    private static HashSet<string> CalendarTokens(string? value, string format, DateTime? dateAnchor, Action<string, int>? unrecognized = null) {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var chunkOffset = 0;
        foreach (var chunk in (value ?? string.Empty).Split(',')) {
            var trimmed = chunk.Trim();
            var sourceOffset = chunkOffset + chunk.IndexOf(trimmed, StringComparison.Ordinal);
            chunkOffset += chunk.Length + 1;
            if (trimmed.Length == 0) continue;
            // Only complete lists of standalone dates/weekdays use whitespace shorthand.
            // An unrecognized timestamp must not lose its time through partial splitting.
            if (TryNormalizeCalendarDate(trimmed, format, out var date, dateAnchor)) tokens.Add(date);
            else {
                var list = new List<string>(); var standalone = true; var tokenOffset = 0;
                var unknownStart = -1; var unknownEnd = 0;
                void ReportUnknown() {
                    if (unknownStart < 0) return;
                    unrecognized?.Invoke(trimmed.Substring(unknownStart, unknownEnd - unknownStart), sourceOffset + unknownStart);
                    unknownStart = -1;
                }
                foreach (var token in trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) {
                    tokenOffset = trimmed.IndexOf(token, tokenOffset, StringComparison.Ordinal);
                    if (TryNormalizeCalendarDate(token, format, out date, dateAnchor)) { ReportUnknown(); list.Add(date); }
                    else if (IsCalendarKeyword(token)) { ReportUnknown(); list.Add(token); }
                    else {
                        standalone = false;
                        if (unknownStart < 0) unknownStart = tokenOffset;
                        unknownEnd = tokenOffset + token.Length;
                        if (!MermaidGanttInputDateFormat.CalendarDateContainsWhitespace(format, token)) ReportUnknown();
                    }
                    tokenOffset += token.Length;
                }
                ReportUnknown();
                if (standalone) tokens.UnionWith(list);
                else tokens.Add(trimmed);
            }
        }
        return tokens;
    }

    private static bool TryNormalizeCalendarDate(string token, string format, out string date, DateTime? dateAnchor) {
        date = string.Empty;
        if (token.Length == 0) return false;
        if (MermaidGanttInputDateFormat.TryParseConfigured(token, format, out var value, dateAnchor)) {
            date = MermaidGanttInputDateFormat.CalendarKey(value, format);
            return true;
        }
        if (!DateTime.TryParseExact(token, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value)) return false;
        date = "iso:" + value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    private static bool IsCalendarKeyword(string token) => string.Equals(token, "weekends", StringComparison.OrdinalIgnoreCase)
        || Enum.TryParse<DayOfWeek>(token, true, out var day) && Enum.IsDefined(typeof(DayOfWeek), day)
            && string.Equals(token, day.ToString(), StringComparison.OrdinalIgnoreCase);

    private static string MergeCalendarTokens(string? existing, string added) {
        var tokens = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Keep complete declarations until all directives establish the final format.
        foreach (var raw in ((existing ?? string.Empty) + "," + added).Split(',')) {
            var token = raw.Trim();
            if (token.Length > 0 && seen.Add(token)) tokens.Add(token);
        }
        return string.Join(", ", tokens);
    }
}
