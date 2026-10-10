using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

/// <summary>Maps explicit Mermaid input-date fields to the platform calendar parser.</summary>
internal static class MermaidGanttInputDateFormat {
    private static readonly CultureInfo English = CreateEnglishCalendar();
    private static readonly string[] Tokens = { "YYYY", "MMMM", "DDDD", "MMM", "DDD", "SSS", "YY", "MM", "DD", "Do", "HH", "hh", "mm", "ss", "SS", "ZZ", "Q", "M", "D", "H", "h", "m", "s", "S", "A", "a", "Z" };

    internal static bool TryParse(string input, string format, out DateTime value) {
        value = default;
        input = input.Trim(); format = format.Trim();
        if (format == "X" || format == "x") return TryUnix(input, format == "X", out value);
        var fields = Parts(format);
        if (!fields.Exists(part => part.Token == "YYYY" || part.Token == "YY")) return false;
        var pattern = new StringBuilder("\\A");
        for (var i = 0; i < fields.Count; i++) {
            var part = fields[i];
            pattern.Append(part.Token == null ? Regex.Escape(part.Text) : "(?<f" + i.ToString(CultureInfo.InvariantCulture) + ">" + Pattern(part.Token) + ")");
        }
        pattern.Append("\\z");
        var match = Regex.Match(input, pattern.ToString(), RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        if (!match.Success) return false;
        var normalized = new StringBuilder(); var nativeFormat = new StringBuilder(); var offset = false;
        for (var i = 0; i < fields.Count; i++) {
            var part = fields[i];
            if (part.Token == null) { normalized.Append(part.Text); AppendLiteral(nativeFormat, part.Text); continue; }
            var text = match.Groups["f" + i.ToString(CultureInfo.InvariantCulture)].Value;
            var token = part.Token;
            if (token == "Q") text = ((int.Parse(text, CultureInfo.InvariantCulture) - 1) * 3 + 1).ToString(CultureInfo.InvariantCulture);
            else if (token == "Do") {
                var day = int.Parse(text.Substring(0, text.Length - 2), CultureInfo.InvariantCulture);
                if (text.Substring(text.Length - 2) != Ordinal(day)) return false;
                text = day.ToString(CultureInfo.InvariantCulture);
            } else if (token == "Z" || token == "ZZ") {
                offset = true;
                if (text == "Z") text = "+00:00";
                else if (text.Length == 5) text = text.Insert(3, ":");
            } else if (token == "DDD" || token == "DDDD") {
                var year = Year(fields, match);
                var day = int.Parse(text, CultureInfo.InvariantCulture);
                if (year < 1 || day < 1 || day > (DateTime.IsLeapYear(year) ? 366 : 365)) return false;
                text = new DateTime(year, 1, 1).AddDays(day - 1).ToString("MM-dd", CultureInfo.InvariantCulture);
            }
            normalized.Append(text); nativeFormat.Append(Native(token));
        }
        // No implicit current date or machine-local offset enters a resolved schedule.
        if (offset) {
            if (!DateTimeOffset.TryParseExact(normalized.ToString(), nativeFormat.ToString(), English, DateTimeStyles.None, out var instant)) return false;
            value = DateTime.SpecifyKind(instant.UtcDateTime, DateTimeKind.Unspecified);
            return true;
        }
        return DateTime.TryParseExact(normalized.ToString(), nativeFormat.ToString(), English, DateTimeStyles.NoCurrentDateDefault, out value);
    }

    internal static string FormatCalendarDate(DateTime value, string format) {
        if (format.IndexOf('%') >= 0 || format.IndexOf("ffff", StringComparison.Ordinal) >= 0)
            return value.ToString(MermaidGanttParser.ToDotNetDateFormat(format), CultureInfo.InvariantCulture);
        if (format.Trim() == "X" || format.Trim() == "x") {
            var milliseconds = (value.Ticks - new DateTime(1970, 1, 1).Ticks) / TimeSpan.TicksPerMillisecond;
            return format.Trim() == "x" ? milliseconds.ToString(CultureInfo.InvariantCulture)
                : (milliseconds / 1000m).ToString("0.###", CultureInfo.InvariantCulture);
        }
        var result = new StringBuilder();
        foreach (var part in Parts(format)) {
            if (part.Token == null) result.Append(part.Text);
            else if (part.Token == "Q") result.Append(((value.Month - 1) / 3 + 1).ToString(CultureInfo.InvariantCulture));
            else if (part.Token == "Do") result.Append(value.Day.ToString(CultureInfo.InvariantCulture)).Append(Ordinal(value.Day));
            else if (part.Token == "DDD" || part.Token == "DDDD") result.Append(value.DayOfYear.ToString(part.Token == "DDD" ? "0" : "000", CultureInfo.InvariantCulture));
            else if (part.Token == "Z" || part.Token == "ZZ") result.Append(part.Token == "Z" ? "+00:00" : "+0000");
            else {
                var text = value.ToString(Native(part.Token).Length == 1 ? "%" + Native(part.Token) : Native(part.Token), English);
                result.Append(part.Token == "a" ? text.ToLowerInvariant() : text);
            }
        }
        return result.ToString();
    }

    private static bool TryUnix(string text, bool seconds, out DateTime value) {
        value = default;
        if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)) return false;
        try {
            var ticks = decimal.Truncate(amount * (seconds ? TimeSpan.TicksPerSecond : TimeSpan.TicksPerMillisecond));
            value = new DateTime(1970, 1, 1).AddTicks(checked((long)ticks));
            return true;
        } catch (ArgumentOutOfRangeException) { return false; }
        catch (OverflowException) { return false; }
    }

    private static int Year(List<Part> fields, Match match) {
        for (var i = 0; i < fields.Count; i++) if (fields[i].Token == "YYYY" || fields[i].Token == "YY") {
            var year = int.Parse(match.Groups["f" + i.ToString(CultureInfo.InvariantCulture)].Value, CultureInfo.InvariantCulture);
            return fields[i].Token == "YY" ? English.DateTimeFormat.Calendar.ToFourDigitYear(year) : year;
        }
        return 0;
    }

    private static List<Part> Parts(string format) {
        var parts = new List<Part>(); var literal = new StringBuilder();
        for (var i = 0; i < format.Length;) {
            if (format[i] == '[' && format.IndexOf(']', i + 1) is var end && end >= 0) {
                literal.Append(format.Substring(i + 1, end - i - 1)); i = end + 1; continue;
            }
            string? token = null;
            foreach (var candidate in Tokens) if (i + candidate.Length <= format.Length && string.CompareOrdinal(format, i, candidate, 0, candidate.Length) == 0) { token = candidate; break; }
            if (token == null) { literal.Append(format[i++]); continue; }
            if (literal.Length > 0) { parts.Add(new Part(literal.ToString(), null)); literal.Clear(); }
            parts.Add(new Part(token, token)); i += token.Length;
        }
        if (literal.Length > 0) parts.Add(new Part(literal.ToString(), null));
        return parts;
    }

    private static string Pattern(string token) => token switch {
        "YYYY" => "[0-9]{4}", "YY" => "[0-9]{2}", "MMM" or "MMMM" => "[A-Za-z]+",
        "Q" => "[1-4]", "Do" => "[0-9]{1,2}(?:st|nd|rd|th)", "DDD" or "DDDD" => "[0-9]{1,3}",
        "Z" or "ZZ" => "(?:Z|[+-][0-9]{2}:?[0-9]{2})", "A" => "(?:AM|PM)", "a" => "(?:am|pm)",
        "SSS" => "[0-9]{3}", "SS" => "[0-9]{2}", "S" => "[0-9]",
        _ => token.Length == 2 ? "[0-9]{2}" : "[0-9]{1,2}"
    };

    private static string Native(string token) => token switch {
        "YYYY" => "yyyy", "YY" => "yy", "D" or "Do" => "d", "DD" => "dd", "Q" => "M",
        "DDD" or "DDDD" => "MM-dd", "S" => "f", "SS" => "ff", "SSS" => "fff",
        "a" or "A" => "tt", "Z" or "ZZ" => "zzz", _ => token
    };

    private static void AppendLiteral(StringBuilder format, string text) {
        foreach (var character in text) format.Append('\\').Append(character);
    }

    private static string Ordinal(int day) => day % 100 is 11 or 12 or 13 ? "th" : day % 10 == 1 ? "st" : day % 10 == 2 ? "nd" : day % 10 == 3 ? "rd" : "th";

    private static CultureInfo CreateEnglishCalendar() {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.Calendar = new GregorianCalendar { TwoDigitYearMax = 2068 };
        return CultureInfo.ReadOnly(culture);
    }

    private sealed class Part {
        internal Part(string text, string? token) { Text = text; Token = token; }
        internal string Text { get; }
        internal string? Token { get; }
    }
}
