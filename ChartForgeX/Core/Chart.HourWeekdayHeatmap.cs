using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    private const int HoursPerDay = 24;
    private const int DaysPerWeek = 7;

    /// <summary>
    /// Adds an hour-of-day by weekday heatmap: seven weekday rows by 24 hour columns, each cell aggregating the values
    /// observed in that hour of that weekday. Buckets use wall-clock time in <paramref name="timeZone"/> (UTC when null),
    /// and cell intensity is relative to the observed range (<see cref="ChartOptions.HeatmapRelativeScale"/>).
    /// </summary>
    /// <param name="values">The timed values, for example one value of 1 per event.</param>
    /// <param name="aggregation">How values in one bucket are combined. Count and sum show empty buckets as zero; mean
    /// and maximum mask them.</param>
    /// <param name="timeZone">The zone whose wall-clock hours and weekdays define the buckets, or null for UTC.</param>
    /// <param name="firstDayOfWeek">The weekday shown in the top row.</param>
    /// <param name="color">An optional high-intensity cell colour.</param>
    /// <param name="dayNames">Optional localized row names: seven entries indexed by <see cref="DayOfWeek"/> (Sunday
    /// first). Null uses the invariant abbreviations <c>Sun</c> through <c>Sat</c>.</param>
    /// <param name="timeZoneLabel">Optional zone designator for the default x-axis title, for example <c>CET</c>. Null uses
    /// <c>UTC</c>, or the zone identifier when <paramref name="timeZone"/> is set; identifiers differ between Windows and
    /// IANA systems, so pass a label when output must match across platforms.</param>
    /// <returns>The current chart.</returns>
    /// <remarks>The heatmap owns the whole chart: it sets the <c>00</c>–<c>23</c> column labels and
    /// <see cref="ChartOptions.HeatmapRelativeScale"/>, and it cannot be combined with other heatmap rows. When the x-axis
    /// title is empty it becomes <see cref="ChartLabels.HourOfDay"/> (default <c>Hour of day</c>) followed by the zone
    /// designator in parentheses.</remarks>
    public Chart AddHourWeekdayHeatmap(IEnumerable<ChartTimedValue> values, ChartTimeAggregation aggregation = ChartTimeAggregation.Count, TimeZoneInfo? timeZone = null, DayOfWeek firstDayOfWeek = DayOfWeek.Monday, ChartColor? color = null, IReadOnlyList<string>? dayNames = null, string? timeZoneLabel = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (dayNames != null && (dayNames.Count != DaysPerWeek || dayNames.Any(string.IsNullOrWhiteSpace))) throw new ArgumentException("Day names must contain seven non-empty entries indexed by DayOfWeek.", nameof(dayNames));
        if (Series.Any(series => series.Kind == ChartSeriesKind.Heatmap || series.Kind == ChartSeriesKind.HexbinHeatmap)) throw new InvalidOperationException("An hour-by-weekday heatmap owns the whole chart and cannot be added to a chart that already has heatmap rows.");
        if (!Enum.IsDefined(typeof(ChartTimeAggregation), aggregation)) throw new ArgumentOutOfRangeException(nameof(aggregation), aggregation, "Unknown aggregation.");
        if (!Enum.IsDefined(typeof(DayOfWeek), firstDayOfWeek)) throw new ArgumentOutOfRangeException(nameof(firstDayOfWeek), firstDayOfWeek, "Unknown weekday.");
        var zone = timeZone ?? TimeZoneInfo.Utc;
        var count = new int[DaysPerWeek, HoursPerDay];
        var sum = new double[DaysPerWeek, HoursPerDay];
        var maximum = new double[DaysPerWeek, HoursPerDay];
        var observed = 0;
        foreach (var value in values) {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value.Timestamp, DateTimeKind.Utc), zone);
            var day = (int)local.DayOfWeek;
            var hour = local.Hour;
            maximum[day, hour] = count[day, hour] == 0 ? value.Value : Math.Max(maximum[day, hour], value.Value);
            count[day, hour]++;
            sum[day, hour] += value.Value;
            observed++;
        }

        if (observed == 0) throw new ArgumentException("Hour-by-weekday heatmaps require at least one value.", nameof(values));
        foreach (var total in sum) {
            if (double.IsInfinity(total)) throw new ArgumentException("Hour-by-weekday bucket sums must stay finite.", nameof(values));
        }

        var names = dayNames ?? CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedDayNames;
        for (var row = 0; row < DaysPerWeek; row++) {
            var day = ((int)firstDayOfWeek + row) % DaysPerWeek;
            var points = new List<ChartPoint>(HoursPerDay);
            for (var hour = 0; hour < HoursPerDay; hour++) {
                var n = count[day, hour];
                double? cell = aggregation switch {
                    ChartTimeAggregation.Count => n,
                    ChartTimeAggregation.Sum => sum[day, hour],
                    ChartTimeAggregation.Mean => n == 0 ? null : sum[day, hour] / n,
                    _ => n == 0 ? null : maximum[day, hour]
                };
                if (cell.HasValue) points.Add(new ChartPoint(hour + 1, cell.Value));
            }

            Series.Add(new ChartSeries(names[day], ChartSeriesKind.Heatmap, points) { Color = color, HeatmapColumnCount = HoursPerDay, ShowInLegend = false });
        }

        var labels = new string[HoursPerDay];
        for (var hour = 0; hour < HoursPerDay; hour++) labels[hour] = hour.ToString("00", CultureInfo.InvariantCulture);
        WithXLabels(labels);
        Options.HeatmapRelativeScale = true;
        if (string.IsNullOrWhiteSpace(XAxisTitle)) {
            var label = !string.IsNullOrWhiteSpace(timeZoneLabel) ? timeZoneLabel!.Trim() : timeZone == null ? "UTC" : timeZone.Id;
            XAxisTitle = Options.Labels.HourOfDay + " (" + label + ")";
        }

        return this;
    }
}
