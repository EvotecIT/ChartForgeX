using System;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Mermaid;

/// <summary>Formats Gantt wall-clock axis values using Mermaid's English D3 time directives.</summary>
internal static class MermaidGanttAxisFormat {
    internal static string Format(DateTime value, string format) {
        var result = new StringBuilder(format.Length + 16);
        for (var index = 0; index < format.Length; index++) {
            if (format[index] != '%') { result.Append(format[index]); continue; }
            if (++index == format.Length) break;
            var padding = '\0';
            if (format[index] == '-' || format[index] == '_' || format[index] == '0') {
                padding = format[index++];
                if (index == format.Length) break;
            }
            result.Append(Directive(value, format[index], padding));
        }
        return result.ToString();
    }

    private static string Directive(DateTime value, char directive, char padding) {
        var english = CultureInfo.InvariantCulture;
        switch (directive) {
            case 'a': return value.ToString("ddd", english);
            case 'A': return value.ToString("dddd", english);
            case 'b': return value.ToString("MMM", english);
            case 'B': return value.ToString("MMMM", english);
            case 'c': return Format(value, "%x, %X");
            case 'd': return Number(value.Day, 2, padding);
            case 'e': return Number(value.Day, 2, padding == '\0' ? '_' : padding);
            case 'f': return Number(value.Millisecond * 1000, 6, padding);
            case 'g': return Number(IsoThursday(value).Year % 100, 2, padding);
            case 'G': return Number(IsoThursday(value).Year, 4, padding);
            case 'H': return Number(value.Hour, 2, padding);
            case 'I': return Number(value.Hour % 12 == 0 ? 12 : value.Hour % 12, 2, padding);
            case 'j': return Number(value.DayOfYear, 3, padding);
            case 'L': return Number(value.Millisecond, 3, padding);
            case 'm': return Number(value.Month, 2, padding);
            case 'M': return Number(value.Minute, 2, padding);
            case 'p': return value.Hour < 12 ? "AM" : "PM";
            case 'q': return Number((value.Month - 1) / 3 + 1, 1, padding);
            case 'Q': return ((value.Ticks - EpochTicks) / TimeSpan.TicksPerMillisecond).ToString(english);
            case 's': return Math.Floor((value.Ticks - EpochTicks) / (double)TimeSpan.TicksPerSecond).ToString("0", english);
            case 'S': return Number(value.Second, 2, padding);
            case 'u': return Number(value.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)value.DayOfWeek, 1, padding);
            case 'U': return Number(Week(value, DayOfWeek.Sunday), 2, padding);
            case 'V': return Number((IsoThursday(value).DayOfYear - 1) / 7 + 1, 2, padding);
            case 'w': return Number((int)value.DayOfWeek, 1, padding);
            case 'W': return Number(Week(value, DayOfWeek.Monday), 2, padding);
            case 'x': return Format(value, "%-m/%-d/%Y");
            case 'X': return Format(value, "%-I:%M:%S %p");
            // Gantt's native date axis stores wall-clock dates without a machine-local zone.
            case 'Z': return "+0000";
            case 'y': return Number(value.Year % 100, 2, padding);
            case 'Y': return Number(value.Year, 4, padding);
            default: return directive.ToString();
        }
    }

    private static readonly long EpochTicks = new DateTime(1970, 1, 1).Ticks;

    private static string Number(int value, int width, char padding) {
        var text = value.ToString(CultureInfo.InvariantCulture);
        return padding == '-' ? text : text.PadLeft(width, padding == '_' ? ' ' : '0');
    }

    private static int Week(DateTime value, DayOfWeek firstDay) {
        var relativeDay = ((int)value.DayOfWeek - (int)firstDay + 7) % 7;
        return (value.DayOfYear + 6 - relativeDay) / 7;
    }

    private static DateTime IsoThursday(DateTime value) {
        var day = value.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)value.DayOfWeek;
        var shift = 4 - day;
        // At DateTime's boundaries, the ISO year remains representable for the available weekdays.
        return value.Date.AddDays(shift);
    }
}
