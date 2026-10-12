using System;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

internal static class MermaidGanttTicks {
    internal static ChartTimeTickInterval? Parse(string? text, DayOfWeek weekStart) {
        if (string.IsNullOrEmpty(text) || text![0] < '1' || text[0] > '9') return null;
        var end = 1;
        while (end < text.Length && text[end] >= '0' && text[end] <= '9') end++;
        if (!int.TryParse(text.Substring(0, end), NumberStyles.None, CultureInfo.InvariantCulture, out var count)) return null;
        ChartTimeTickUnit unit;
        switch (text.Substring(end)) {
            case "millisecond": unit = ChartTimeTickUnit.Millisecond; break;
            case "second": unit = ChartTimeTickUnit.Second; break;
            case "minute": unit = ChartTimeTickUnit.Minute; break;
            case "hour": unit = ChartTimeTickUnit.Hour; break;
            case "day": unit = ChartTimeTickUnit.Day; break;
            case "week": unit = ChartTimeTickUnit.Week; break;
            case "month": unit = ChartTimeTickUnit.Month; break;
            default: return null;
        }
        return new ChartTimeTickInterval(unit, count, weekStart);
    }
}
