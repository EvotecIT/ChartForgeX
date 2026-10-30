using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

/// <summary>Illustrative change records and three linked charts whose marks carry the records' keys and defining fields.</summary>
public static class EnhanceRecipe {
    /// <summary>One illustrative record, as a grid row.</summary>
    public sealed record Record(string Id, DateTime Day, string Kind, string Product, string Title);

    private static readonly (string Id, string Name, string Variable)[] Kinds = {
        ("update", "Updates", "--hfx-color-series-1"), ("note", "Publisher notes", "--hfx-color-series-2"), ("retire", "Retirements", "--hfx-color-series-4")
    };
    private static readonly string[] Products = { "Windows 11 24H2", "Windows 11 25H2", "Windows 11 26H1" };

    /// <summary>The first day of the illustrative month, 1 October 2026 (UTC).</summary>
    public static readonly DateTime Start = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Creates deterministic illustrative records for October 2026.</summary>
    public static IReadOnlyList<Record> Records() {
        var seed = 7;
        double Next() { seed = (int)(seed * 16807L % 2147483647); return (seed - 1) / 2147483646.0; }
        var titles = new Dictionary<string, string[]> {
            ["update"] = new[] { "Cumulative update", "Out-of-band update", "Servicing stack update", "Dynamic update" },
            ["note"] = new[] { "Known issue resolved", "Release notes revised", "Rollout paused for some devices", "Guidance updated" },
            ["retire"] = new[] { "Feature deprecated", "Legacy component removed", "End of servicing announced" }
        };
        var records = new List<Record>();
        for (var d = 0; d < 30; d++) {
            var day = Start.AddDays(d);
            var weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var counts = new Dictionary<string, int> {
                ["update"] = (weekend ? 1 : 3) + (int)(Next() * 3) + (d == 12 ? 6 : 0),
                ["note"] = (int)(Next() * (weekend ? 2 : 4)) + (d == 13 ? 3 : 0),
                ["retire"] = Next() < .18 ? 1 : 0
            };
            foreach (var kind in Kinds) for (var i = 0; i < counts[kind.Id]; i++) {
                var list = titles[kind.Id];
                records.Add(new Record("r" + (records.Count + 1).ToString(CultureInfo.InvariantCulture), day, kind.Id,
                    Products[(int)(Next() * Products.Length)], list[(int)(Next() * list.Length)]));
            }
        }
        return records;
    }

    /// <summary>The last seven days as stacked columns; each segment carries its records' keys, its day and its kind.</summary>
    public static Chart Week(IReadOnlyList<Record> records, VisualThemeMode mode = VisualThemeMode.Light) {
        var days = Enumerable.Range(23, 7).Select(d => Start.AddDays(d)).ToArray();
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithStackedBars().WithLegend(true)
            .WithXLabels(days.Select(d => d.ToString("ddd d", CultureInfo.InvariantCulture)).ToArray())
            .ConfigureYAxis(axis => axis.IntegerTicks = true);
        foreach (var kind in Kinds) {
            var points = days.Select((day, i) => {
                var keys = records.Where(r => r.Day == day && r.Kind == kind.Id).Select(r => r.Id).ToArray();
                return new ChartPoint(i + 1, keys.Length).WithMarkOptions(new ChartMarkOptions(keys: keys,
                    fields: new Dictionary<string, object> { ["day"] = day }));
            });
            chart.AddBar(kind.Name, points, Color(kind.Variable, mode));
            chart.Series[chart.Series.Count - 1].WithMarkOptions(new ChartMarkOptions(fields: new Dictionary<string, object> { ["kind"] = kind.Id }));
        }
        return chart;
    }

    /// <summary>Records per day for the month on a time axis; points carry their day's records for both kinds shown.</summary>
    public static Chart Month(IReadOnlyList<Record> records, VisualThemeMode mode = VisualThemeMode.Light) {
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithLegend(true)
            .ConfigureXAxis(axis => {
                axis.Scale = ChartScaleKind.Time; axis.TickCount = 5;
                axis.ValueFormat = ChartValueFormat.Custom(value => DateTime.FromOADate(value).ToString("d MMM", CultureInfo.InvariantCulture));
            })
            .ConfigureYAxis(axis => axis.IntegerTicks = true);
        foreach (var kind in Kinds.Take(2)) {
            var points = Enumerable.Range(0, 30).Select(d => {
                var day = Start.AddDays(d);
                var keys = records.Where(r => r.Day == day && r.Kind == kind.Id).Select(r => r.Id).ToArray();
                return new ChartPoint(day.ToOADate(), keys.Length).WithMarkOptions(new ChartMarkOptions(keys: keys,
                    fields: new Dictionary<string, object> { ["day"] = day, ["kind"] = kind.Id }));
            });
            chart.AddLine(kind.Name, points, Color(kind.Variable, mode));
        }
        return chart;
    }

    /// <summary>Support windows with the current release as the current row; bars carry the product's records.</summary>
    public static Chart Support(IReadOnlyList<Record> records, VisualThemeMode mode = VisualThemeMode.Light) {
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithLegend(false)
            .WithGanttOpenEndStyle(ChartGanttOpenEndStyle.Fade)
            .WithXAxisBounds(new DateTime(2024, 7, 1).ToOADate(), new DateTime(2028, 1, 1).ToOADate())
            .ConfigureXAxis(axis => {
                axis.Scale = ChartScaleKind.Time; axis.TickCount = 4;
                axis.ValueFormat = ChartValueFormat.Custom(value => DateTime.FromOADate(value).ToString("yyyy", CultureInfo.InvariantCulture));
            });
        var windows = new[] {
            (Products[0], new DateTime(2024, 10, 1), (DateTime?)new DateTime(2026, 10, 13)),
            (Products[1], new DateTime(2025, 10, 1), (DateTime?)new DateTime(2027, 10, 12)),
            (Products[2], new DateTime(2026, 3, 1), (DateTime?)null)
        };
        for (var i = 0; i < windows.Length; i++) {
            var (product, start, end) = windows[i];
            var keys = records.Where(r => r.Product == product).Select(r => r.Id).ToArray();
            var fields = new Dictionary<string, object> { ["product"] = product };
            var item = (end == null ? ChartGanttItem.OpenEnded(start) : new ChartGanttItem(start, end.Value))
                .WithMarkOptions(new ChartMarkOptions(keys: keys, fields: fields));
            var id = "release-" + i.ToString(CultureInfo.InvariantCulture);
            chart.AddSeries(ChartSeries.CreateGanttTask(new ChartSeriesId(id), product.Replace("Windows 11 ", "Win 11 "), start, end ?? new DateTime(2028, 1, 1),
                    color: Color(i == 0 ? "--hfx-color-text-secondary" : "--hfx-color-series-1", mode))
                .ReplaceGanttItems(new[] { item })
                .WithRowOptions(new ChartMarkOptions(current: i == 1, fields: fields)));
        }
        return chart.WithGanttToday(new DateTime(2026, 10, 30));
    }

    private static ChartColor Color(string variable, VisualThemeMode mode) {
        var dark = mode == VisualThemeMode.Dark;
        return ChartColor.FromHex(variable switch {
            "--hfx-color-series-1" => dark ? "#3987E5" : "#2A78D6",
            "--hfx-color-series-2" => dark ? "#179C89" : "#0F9F8C",
            "--hfx-color-series-4" => dark ? "#C06A42" : "#A0522D",
            _ => dark ? "#A3A9B3" : "#5C626C"
        }).WithCssVariable(variable);
    }
}
