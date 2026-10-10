using System;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttInputDateFormat {
    internal static bool OmitsYear(string format) {
        format = format.Trim();
        if (format == "X" || format == "x") return false;
        var fields = Parts(format);
        if (UsesMermaidFields(fields)) return !fields.Exists(part => part.Token == "YYYY" || part.Token == "YY");
        DateFields(ExpandNativeFormat(MermaidGanttParser.ToDotNetDateFormat(format)), out var year, out _, out _, out _);
        return !year;
    }

    internal static string CalendarKey(DateTime value, string format) => "format:" +
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "|" + FormatCalendarDate(value, format);

    private static bool ApplyDateDefaults(StringBuilder input, StringBuilder format, DateTime? anchor) {
        DateFields(format.ToString(), out var year, out var month, out var day, out _);
        if (!year && !anchor.HasValue) return false;
        void Append(string token, int value) {
            input.Append('|').Append(value.ToString(token == "yyyy" ? "0000" : "0", CultureInfo.InvariantCulture));
            format.Append("\\|").Append(token);
        }
        if (!year) Append("yyyy", anchor!.Value.Year);
        if (!month) Append("M", !year ? anchor!.Value.Month : 1);
        if (!day) Append("d", !year && !month ? anchor!.Value.Day : 1);
        return true;
    }

    private static string ExpandNativeFormat(string format) {
        if (!IsNativeStandardFormat(format)) return format;
        var patterns = CultureInfo.InvariantCulture.DateTimeFormat.GetAllDateTimePatterns(format[0]);
        return patterns.Length > 0 ? patterns[0] : format;
    }

    // Scan only calendar fields: escaped and quoted literals never supply a year.
    private static bool IsNativeStandardFormat(string format) => format.Length == 1 && "dDfFgGmMoOrRsStTuUyY".IndexOf(format[0]) >= 0;

    private static void DateFields(string format, out bool year, out bool month, out bool day, out bool offset) {
        year = month = day = offset = false;
        for (var index = 0; index < format.Length; index++) {
            var token = format[index];
            if (token == '\\') { index++; continue; }
            if (token == '\'' || token == '"') {
                while (++index < format.Length && format[index] != token) if (format[index] == '\\') index++;
                continue;
            }
            if (token == '%') { if (++index >= format.Length) break; token = format[index]; }
            var end = index + 1;
            while (end < format.Length && format[end] == token) end++;
            if (token == 'y') year = true;
            else if (token == 'M') month = true;
            else if (token == 'd' && end - index < 3) day = true;
            else if (token == 'z' || token == 'K') offset = true;
            index = end - 1;
        }
    }
}
