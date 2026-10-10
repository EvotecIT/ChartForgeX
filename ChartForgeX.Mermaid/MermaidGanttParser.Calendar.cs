using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttParser {
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
            var excluded = CalendarTokens(document.Excludes, document.DateFormat);
            if (excluded.Count == 0 || end - start < TimeSpan.FromDays(1)) return true;
            var included = CalendarTokens(document.Includes, document.DateFormat);
            var cursor = start.AddDays(1);
            var extension = 0;
            var previousExcluded = false;
            while (cursor <= end) {
                // Mermaid keeps trailing excluded days for scheduling while the bar
                // ends before that trailing run. Interior exclusions still extend it.
                if (!previousExcluded) renderEnd = end;
                var date = "iso:" + cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var formatted = "format:" + MermaidGanttInputDateFormat.FormatCalendarDate(cursor, document.DateFormat);
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

    private static HashSet<string> CalendarTokens(string? value, string format) {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in (value ?? string.Empty).Split(',')) {
            var trimmed = chunk.Trim();
            if (trimmed.Length == 0) continue;
            // Only complete lists of standalone dates/weekdays use whitespace shorthand.
            // An unrecognized timestamp must not lose its time through partial splitting.
            if (TryNormalizeCalendarDate(trimmed, format, out var date)) tokens.Add(date);
            else {
                var list = new List<string>(); var standalone = true;
                foreach (var token in trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) {
                    if (TryNormalizeCalendarDate(token, format, out date)) list.Add(date);
                    else if (IsCalendarKeyword(token)) list.Add(token);
                    else { standalone = false; break; }
                }
                if (standalone) tokens.UnionWith(list); else tokens.Add(trimmed);
            }
        }
        return tokens;
    }

    private static bool TryNormalizeCalendarDate(string token, string format, out string date) {
        date = string.Empty;
        if (token.Length == 0) return false;
        if (MermaidGanttInputDateFormat.TryParseConfigured(token, format, out var value)) {
            date = "format:" + MermaidGanttInputDateFormat.FormatCalendarDate(value, format);
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
