using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttParser {
    // Duration-based ends advance past excluded calendar dates. Explicit ends remain authored dates.
    private static bool TryResolveDurationEnd(DateTime start, double amount, string unit, MermaidGanttDocument document, out DateTime end) {
        end = start;
        try {
            end = AddDuration(start, amount, unit);
            var excluded = CalendarTokens(document.Excludes);
            if (excluded.Count == 0 || end - start < TimeSpan.FromDays(1)) return true;
            var included = CalendarTokens(document.Includes);
            var cursor = start.AddDays(1);
            var extension = 0;
            while (cursor <= end) {
                var date = cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var formatted = cursor.ToString(ToDotNetDateFormat(document.DateFormat), CultureInfo.InvariantCulture);
                var weekday = cursor.DayOfWeek.ToString().ToLowerInvariant();
                var weekend = document.Weekend == "friday"
                    ? cursor.DayOfWeek == DayOfWeek.Friday || cursor.DayOfWeek == DayOfWeek.Saturday
                    : cursor.DayOfWeek == DayOfWeek.Saturday || cursor.DayOfWeek == DayOfWeek.Sunday;
                if (!included.Contains(date) && !included.Contains(formatted) &&
                    (excluded.Contains(date) || excluded.Contains(formatted) || excluded.Contains(weekday) || (weekend && excluded.Contains("weekends")))) {
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
}
