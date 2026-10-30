using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

/// <summary>
/// Illustrative change records (example data, not real releases) and four charts of different families whose marks
/// carry the records' keys and the field values that define them, so a CanopyX grid of the same records can follow them.
/// </summary>
public static class EnhanceRecipe {
    /// <summary>One illustrative record, as a grid row. <see cref="Week"/> is the Monday that starts the record's week.</summary>
    public sealed record Record(string Id, DateTime Day, string Kind, string Product, string Title, string Severity) {
        /// <summary>The Monday (UTC) of the record's week.</summary>
        public DateTime Week => Day.AddDays(-(((int)Day.DayOfWeek + 6) % 7));
    }

    /// <summary>Record kinds: the raw value the grid filters by, the series name and the series colour role.</summary>
    public static readonly (string Id, string Name, int Slot)[] Kinds = { ("Update", "Updates", 1), ("Note", "Notes", 2), ("Retirement", "Retirements", 4) };

    /// <summary>Products with their short row labels (eight characters fit a phone-width row label) and illustrative support windows; a null end is still open.</summary>
    public static readonly (string Name, string Short, DateTime Start, DateTime? End)[] Products = {
        ("Windows 11 24H2", "Win 24H2", new DateTime(2024, 10, 1), new DateTime(2026, 10, 13)),
        ("Windows 11 25H2", "Win 25H2", new DateTime(2025, 10, 1), new DateTime(2027, 10, 12)),
        ("Windows Server 2025", "Srv 2025", new DateTime(2024, 11, 1), new DateTime(2029, 11, 13)),
        ("Microsoft 365 Apps", "365 Apps", new DateTime(2025, 1, 1), null)
    };

    /// <summary>Severities from the most to the least urgent.</summary>
    public static readonly string[] Severities = { "Critical", "Important", "Moderate", "Low" };

    /// <summary>The first day of the illustrative month, 1 October 2026 (UTC).</summary>
    public static readonly DateTime Start = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>The page's "today", the last day of the records.</summary>
    public static readonly DateTime Today = Start.AddDays(29);

    /// <summary>Creates about 150 deterministic illustrative records for October 2026.</summary>
    public static IReadOnlyList<Record> Records() {
        var seed = 11;
        double Next() { seed = (int)(seed * 16807L % 2147483647); return (seed - 1) / 2147483646.0; }
        var titles = new Dictionary<string, string[]> {
            ["Update"] = new[] { "Cumulative update", "Out-of-band update", "Servicing stack update", "Dynamic update", "Security update" },
            ["Note"] = new[] { "Known issue resolved", "Release notes revised", "Rollout paused for some devices", "Guidance updated" },
            ["Retirement"] = new[] { "Feature deprecated", "Legacy component removed", "End of servicing announced" }
        };
        var records = new List<Record>();
        for (var d = 0; d < 30; d++) {
            var day = Start.AddDays(d);
            var weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var counts = new Dictionary<string, int> {
                ["Update"] = (weekend ? 1 : 3) + (int)(Next() * 3) + (day.Day == 13 ? 5 : 0),
                ["Note"] = (int)(Next() * (weekend ? 2 : 3)) + (day.Day == 14 ? 3 : 0),
                ["Retirement"] = Next() < .2 ? 1 : 0
            };
            foreach (var kind in Kinds) for (var i = 0; i < counts[kind.Id]; i++) {
                var list = titles[kind.Id];
                var severity = kind.Id == "Update" ? Severities[(int)(Next() * 3)] : Severities[2 + (int)(Next() * 2)];
                // Products are uneven (most activity on the current release), and the 13 October spike is a 25H2 update wave.
                var pick = Next();
                var product = day.Day == 13 && kind.Id == "Update" && i >= 2 ? 1 : pick < .12 ? 0 : pick < .52 ? 1 : pick < .7 ? 2 : 3;
                records.Add(new Record("r" + (records.Count + 1).ToString("D3", CultureInfo.InvariantCulture), day, kind.Id,
                    Products[product].Name, list[(int)(Next() * list.Length)], severity));
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
                var marks = Marks(records.Where(r => r.Day == day && r.Kind == kind.Id), new Dictionary<string, object> { ["day"] = day });
                return new ChartPoint(i + 1, marks.Keys.Count).WithMarkOptions(marks);
            });
            chart.AddBar(kind.Name, points, Series(kind.Slot, mode));
            chart.Series[chart.Series.Count - 1].WithMarkOptions(new ChartMarkOptions(fields: new Dictionary<string, object> { ["kind"] = kind.Id }));
        }
        return chart;
    }

    /// <summary>Records per day for the month on a time axis, all records as an area and updates as a line.</summary>
    public static Chart Trend(IReadOnlyList<Record> records, VisualThemeMode mode = VisualThemeMode.Light) {
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithLegend(true)
            .ConfigureXAxis(axis => {
                axis.Scale = ChartScaleKind.Time; axis.TickCount = 5;
                axis.ValueFormat = ChartValueFormat.Custom(value => DateTime.FromOADate(value).ToString("d MMM", CultureInfo.InvariantCulture));
            })
            .ConfigureYAxis(axis => axis.IntegerTicks = true);
        ChartPoint Day(int d, Func<Record, bool> include) {
            var day = Start.AddDays(d);
            var marks = Marks(records.Where(r => r.Day == day && include(r)), new Dictionary<string, object> { ["day"] = day });
            return new ChartPoint(day.ToOADate(), marks.Keys.Count).WithMarkOptions(marks);
        }
        chart.AddArea("All records", Enumerable.Range(0, 30).Select(d => Day(d, _ => true)), Series(1, mode));
        chart.AddLine("Updates", Enumerable.Range(0, 30).Select(d => Day(d, r => r.Kind == "Update")), Series(2, mode));
        return chart;
    }

    /// <summary>Records per product and week; each cell carries its records' keys, its product and its week.</summary>
    public static Chart Heatmap(IReadOnlyList<Record> records, VisualThemeMode mode = VisualThemeMode.Light) {
        var weeks = records.Select(r => r.Week).Distinct().OrderBy(w => w).ToArray();
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithLegend(false)
            .WithXLabels(weeks.Select(w => w.ToString("d MMM", CultureInfo.InvariantCulture)).ToArray());
        foreach (var product in Products) {
            var cells = weeks.Select((week, i) => {
                var marks = Marks(records.Where(r => r.Product == product.Name && r.Week == week), new Dictionary<string, object> { ["week"] = week });
                return new ChartPoint(i + 1, marks.Keys.Count).WithMarkOptions(marks);
            });
            chart.AddHeatmapRow(product.Short, cells, Series(1, mode));
            chart.Series[chart.Series.Count - 1].WithMarkOptions(new ChartMarkOptions(fields: new Dictionary<string, object> { ["product"] = product.Name }));
        }
        return chart;
    }

    /// <summary>Support windows with the current release as the current row; bars carry the product's records.</summary>
    public static Chart Support(IReadOnlyList<Record> records, VisualThemeMode mode = VisualThemeMode.Light) {
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithLegend(false)
            .WithGanttOpenEndStyle(ChartGanttOpenEndStyle.Fade)
            .WithXAxisBounds(new DateTime(2024, 7, 1).ToOADate(), new DateTime(2030, 1, 1).ToOADate())
            .ConfigureXAxis(axis => {
                axis.Scale = ChartScaleKind.Time; axis.TickCount = 4;
                axis.ValueFormat = ChartValueFormat.Custom(value => DateTime.FromOADate(value).ToString("yyyy", CultureInfo.InvariantCulture));
            });
        for (var i = 0; i < Products.Length; i++) {
            var (name, label, start, end) = Products[i];
            var fields = new Dictionary<string, object> { ["product"] = name };
            var marks = Marks(records.Where(r => r.Product == name), fields);
            var item = (end == null ? ChartGanttItem.OpenEnded(start) : new ChartGanttItem(start, end.Value)).WithMarkOptions(marks);
            var ended = end != null && end.Value < Today;
            chart.AddSeries(ChartSeries.CreateGanttTask(new ChartSeriesId("release-" + i.ToString(CultureInfo.InvariantCulture)), label, start, end ?? new DateTime(2030, 1, 1),
                    color: ended ? Secondary(mode) : Series(1, mode))
                .ReplaceGanttItems(new[] { item })
                .WithRowOptions(new ChartMarkOptions(current: name == "Windows 11 25H2", fields: fields)));
        }
        return chart.WithGanttToday(Today);
    }

    private static ChartMarkOptions Marks(IEnumerable<Record> records, IReadOnlyDictionary<string, object> fields) =>
        new(keys: records.Select(r => r.Id).ToArray(), fields: fields);

    // Series colours name the shared --hfx-* role, with the Graphite value of the prepared mode as the fallback.
    private static ChartColor Series(int slot, VisualThemeMode mode) {
        var tokens = mode == VisualThemeMode.Dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight();
        return tokens.Palette[slot - 1].WithCssVariable("--hfx-color-series-" + slot.ToString(CultureInfo.InvariantCulture));
    }

    private static ChartColor Secondary(VisualThemeMode mode) =>
        (mode == VisualThemeMode.Dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight()).MutedForeground.WithCssVariable("--hfx-color-text-secondary");
}
