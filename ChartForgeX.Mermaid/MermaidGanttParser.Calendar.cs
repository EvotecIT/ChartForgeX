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
            var excluded = CalendarTokens(document.Excludes);
            if (excluded.Count == 0 || end - start < TimeSpan.FromDays(1)) return true;
            var included = CalendarTokens(document.Includes);
            var cursor = start.AddDays(1);
            var extension = 0;
            var previousExcluded = false;
            while (cursor <= end) {
                // Mermaid keeps trailing excluded days for scheduling while the bar
                // ends before that trailing run. Interior exclusions still extend it.
                if (!previousExcluded) renderEnd = end;
                var date = cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var formatted = MermaidGanttInputDateFormat.FormatCalendarDate(cursor, document.DateFormat);
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

    private static HashSet<string> CalendarTokens(string? value) => new(
        (value ?? string.Empty).Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);

    private static string MergeCalendarTokens(string? existing, string added) {
        var tokens = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in ((existing ?? string.Empty) + "," + added).Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) {
            if (seen.Add(token)) tokens.Add(token.ToLowerInvariant());
        }
        return string.Join(", ", tokens);
    }
}
