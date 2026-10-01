using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>
/// Renderer-neutral days, weeks, colours, words, and cell geometry of a calendar heatmap, so SVG and PNG draw the
/// same calendar. Weeks start on the series' first day of week; a day with the value zero is drawn neutral and the
/// colour ramp starts at the smallest non-zero value.
/// </summary>
internal sealed class ChartCalendarHeatmapModel {
    private static readonly string[] InvariantDays = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
    private static readonly string[] InvariantMonths = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    private readonly Dictionary<DateTime, ChartCalendarDay> _byDate;
    private readonly string[] _dayNames;
    private readonly string[] _monthNames;

    private ChartCalendarHeatmapModel(Chart chart, ChartSeries series, Dictionary<DateTime, ChartCalendarDay> byDate, DateTime minDate, DateTime maxDate, double min, double max, double rampMin) {
        Chart = chart;
        Series = series;
        _byDate = byDate;
        FirstDay = series.CalendarFirstDay;
        _dayNames = series.CalendarDayNames ?? InvariantDays;
        _monthNames = series.CalendarMonthNames ?? InvariantMonths;
        MinDate = minDate;
        MaxDate = maxDate;
        Start = minDate.AddDays(-Row(minDate));
        End = maxDate.AddDays(6 - Row(maxDate));
        Columns = Math.Max(1, (End - Start).Days / 7 + 1);
        Min = min;
        Max = max;
        RampMin = rampMin;
    }

    public Chart Chart { get; }

    public ChartSeries Series { get; }

    public DayOfWeek FirstDay { get; }

    /// <summary>Gets the first day drawn: the start of the week of the earliest value.</summary>
    public DateTime Start { get; }

    /// <summary>Gets the last day drawn: the end of the week of the latest value.</summary>
    public DateTime End { get; }

    public DateTime MinDate { get; }

    public DateTime MaxDate { get; }

    public int Columns { get; }

    /// <summary>Gets the smallest and largest values, as written to the chart metadata.</summary>
    public double Min { get; }

    public double Max { get; }

    /// <summary>Gets the value the colour ramp starts at: the smallest non-zero value when the data has zeros.</summary>
    public double RampMin { get; }

    public int TotalDays => (End - Start).Days + 1;

    public int FilledDays => _byDate.Count;

    public int EmptyDays => Math.Max(0, TotalDays - FilledDays);

    public static ChartCalendarHeatmapModel? Build(Chart chart) {
        ChartSeries? series = null;
        foreach (var item in chart.Series) {
            if (item.Kind == ChartSeriesKind.CalendarHeatmap) {
                series = item;
                break;
            }
        }

        if (series == null || series.Points.Count == 0) return null;
        var byDate = new Dictionary<DateTime, ChartCalendarDay>();
        var minDate = DateTime.MaxValue;
        var maxDate = DateTime.MinValue;
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        var minNonZero = double.PositiveInfinity;
        for (var i = 0; i < series.Points.Count; i++) {
            var date = DateTime.FromOADate(series.Points[i].X).Date;
            var value = series.Points[i].Y;
            byDate[date] = new ChartCalendarDay(date, value, i < series.PointColors.Count ? series.PointColors[i] : null);
            if (date < minDate) minDate = date;
            if (date > maxDate) maxDate = date;
            min = Math.Min(min, value);
            max = Math.Max(max, value);
            if (value != 0) minNonZero = Math.Min(minNonZero, value);
        }

        // With zeros in the data the ramp starts at the smallest non-zero value, unless that is the only other value:
        // then the ramp runs from zero so that value is drawn at full strength instead of the weakest step.
        var rampMin = min == 0 && !double.IsPositiveInfinity(minNonZero) && minNonZero < max ? minNonZero : min;
        return new ChartCalendarHeatmapModel(chart, series, byDate, minDate, maxDate, min, max, rampMin);
    }

    /// <summary>Returns the row of a day: zero for the first day of the week.</summary>
    public int Row(DateTime day) => ((int)day.DayOfWeek - (int)FirstDay + 7) % 7;

    public int Column(DateTime day) => (day - Start).Days / 7;

    /// <summary>Returns the weekday name of a row.</summary>
    public string DayName(int row) => _dayNames[((int)FirstDay + row) % 7];

    public string MonthName(DateTime month) => _monthNames[month.Month - 1];

    public bool TryGetDay(DateTime date, out ChartCalendarDay day) => _byDate.TryGetValue(date, out day);

    /// <summary>Returns true when a value is drawn neutral: zero in data that has no negative values.</summary>
    public bool IsZero(double value) => value == 0 && Min >= 0;

    /// <summary>Returns the colour of a day; a colour set on the day itself wins over the neutral zero.</summary>
    public ChartColor Color(double value, ChartColor? pointColor) => Blend(value, pointColor).Color;

    /// <summary>Returns the colour of a day as a blend, for SVG colour variables (see <see cref="Color"/>).</summary>
    public ChartColorBlend Blend(double value, ChartColor? pointColor) =>
        IsZero(value) && !pointColor.HasValue ? ChartHeatmapSurface.ZeroBlend(Chart) : ChartHeatmapSurface.CalendarBlend(Chart, Series, pointColor, value, RampMin, Max);

    /// <summary>Returns the intensity level: 0 for a neutral zero, otherwise 1 to 4 along the ramp.</summary>
    public int Level(double value) => IsZero(value) ? 0 : Math.Max(1, ChartHeatmapSurface.Level(ChartHeatmapSurface.CalendarRatio(value, RampMin, Max)));

    /// <summary>
    /// Returns the value of one of the five scale steps, from the start of the ramp to the maximum. When the ramp starts
    /// at zero while zeros are drawn neutral (zeros and one other value), the steps start above zero so no step claims
    /// the value the neutral swatch stands for.
    /// </summary>
    public double ScaleValue(int step) {
        var low = IsZero(RampMin) && Max > 0 ? Max / 5 : RampMin;
        return ChartHeatmapSurface.InterpolateObservedRange(low, Max, step / 4.0);
    }

    /// <summary>Returns the level of a scale step, the same level a cell with that value has.</summary>
    public int ScaleLevel(int step) => Level(ScaleValue(step));

    public string DateText(DateTime day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Returns the accessible name of the calendar group, through <see cref="ChartLabels.AccessibleTextFormatter"/>.</summary>
    public string Summary() => Chart.Options.Labels.Describe(new ChartDescriptionFacts(ChartDescriptionKind.CalendarHeatmapGroup, Chart.Title, new[] { Series.Name }, FilledDays, EmptyDays, Start, End));

    /// <summary>Gap the calendar keeps from the chart edge, or from the card when one is drawn.</summary>
    private const double EdgeInset = 8;

    /// <summary>
    /// Returns the frame a calendar is drawn in. The default padding is sized for cartesian axes and legends that a
    /// calendar does not draw (a short card would shrink the days to a pixel), so with it the calendar takes the chart
    /// area instead: below the header when one is drawn, inside the card when one is drawn, and a small gap from the
    /// edge. Padding set on the chart (<see cref="ChartOptions.Padding"/>, <c>WithPadding</c>, or a builder that sets it)
    /// is honoured and the frame is <paramref name="basePlot"/>. Renderers draw the plot surface in this frame too.
    /// </summary>
    public static ChartRect Frame(Chart chart, ChartRect basePlot) {
        var options = chart.Options;
        if (options.HasExplicitPadding) return basePlot;
        var inset = (options.ShowCard && options.Theme.UseCard ? ChartVisualPrimitives.CardSurfaceInset : 0) + EdgeInset;
        var top = options.ShowHeader ? Math.Max(inset, ChartLayout.HeaderBottom(chart)) : inset;
        return new ChartRect(inset, top, Math.Max(1, options.Size.Width - inset * 2), Math.Max(1, options.Size.Height - top - inset));
    }

    /// <summary>Returns the cell geometry in a <paramref name="frame"/> from <see cref="Frame"/>, inside the reserves.</summary>
    public ChartCalendarLayout Layout(ChartRect frame, double leftReserve, double topReserve, double bottomReserve) =>
        Layout(Inner(frame, leftReserve, topReserve, bottomReserve));

    private static ChartRect Inner(ChartRect area, double leftReserve, double topReserve, double bottomReserve) =>
        new(area.Left + leftReserve, area.Top + topReserve, Math.Max(1, area.Width - leftReserve - 8), Math.Max(1, area.Height - topReserve - bottomReserve));

    /// <summary>
    /// Returns the cell geometry inside <paramref name="plot"/>: cells fill the plot, limited by its width or height,
    /// unless a preferred size fits or a maximum size applies; the grid is centred.
    /// </summary>
    public ChartCalendarLayout Layout(ChartRect plot) {
        var options = Chart.Options;
        var gap = options.CalendarCellGap ?? (Columns > 32 ? 2.5 : 3.5);
        var fit = Math.Max(1, Math.Min((plot.Width - gap * (Columns - 1)) / Columns, (plot.Height - gap * 6) / 7));
        var cell = fit;
        if (options.CalendarMaximumCellSize.HasValue) cell = Math.Min(cell, options.CalendarMaximumCellSize.Value);
        if (options.CalendarCellSize.HasValue) cell = Math.Min(options.CalendarCellSize.Value, fit);
        var gridWidth = Columns * cell + (Columns - 1) * gap;
        var gridHeight = 7 * cell + 6 * gap;
        return new ChartCalendarLayout(cell, gap, plot.Left + Math.Max(0, (plot.Width - gridWidth) / 2), plot.Top + Math.Max(0, (plot.Height - gridHeight) / 2), gridWidth, gridHeight);
    }

    /// <summary>
    /// Returns the rows that get a weekday label: all of them when the cells are as tall as the text, otherwise Monday,
    /// Wednesday, and Friday wherever the week starts. The decision uses the theme font size, not a renderer's text
    /// metrics, so SVG and PNG label the same rows.
    /// </summary>
    public IEnumerable<int> LabelledRows(ChartCalendarLayout layout) {
        var all = layout.Cell + layout.Gap >= Chart.Options.Theme.TickLabelFontSize * 1.2 + 1;
        for (var row = 0; row < 7; row++) {
            var weekday = (DayOfWeek)(((int)FirstDay + row) % 7);
            if (all || weekday is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday) yield return row;
        }
    }

    /// <summary>
    /// Returns the month labels: the month of the first value above the first week, so a window that starts mid-month
    /// still names its month, then each month that begins inside the window where its label fits after the previous one.
    /// The first month gives way when the next one starts before its label ends. Widths are estimated from the theme
    /// font size, so SVG and PNG choose the same labels.
    /// </summary>
    public IEnumerable<(DateTime Month, double X)> MonthLabels(ChartCalendarLayout layout) {
        double Measure(string text) => text.Length * Chart.Options.Theme.TickLabelFontSize * 0.62;
        var first = new DateTime(MinDate.Year, MinDate.Month, 1);
        var month = first.AddMonths(1);
        var lastRight = double.NegativeInfinity;
        var nextX = layout.X0 + Math.Max(0, Column(month)) * (layout.Cell + layout.Gap);
        if (month > MaxDate || nextX >= layout.X0 + Measure(MonthName(first)) + 6) {
            yield return (first, layout.X0);
            lastRight = layout.X0 + Measure(MonthName(first));
        }

        while (month <= MaxDate) {
            var x = layout.X0 + Math.Max(0, Column(month)) * (layout.Cell + layout.Gap);
            if (x >= lastRight + 6) {
                yield return (month, x);
                lastRight = x + Measure(MonthName(month));
            }

            month = month.AddMonths(1);
        }
    }
}

/// <summary>One day with a value.</summary>
internal readonly struct ChartCalendarDay {
    public ChartCalendarDay(DateTime date, double value, ChartColor? color) {
        Date = date;
        Value = value;
        Color = color;
    }

    public DateTime Date { get; }

    public double Value { get; }

    public ChartColor? Color { get; }
}

/// <summary>Cell size, gap, and the top-left corner and size of the day grid.</summary>
internal readonly struct ChartCalendarLayout {
    public ChartCalendarLayout(double cell, double gap, double x0, double y0, double gridWidth, double gridHeight) {
        Cell = cell;
        Gap = gap;
        X0 = x0;
        Y0 = y0;
        GridWidth = gridWidth;
        GridHeight = gridHeight;
    }

    public double Cell { get; }

    public double Gap { get; }

    public double X0 { get; }

    public double Y0 { get; }

    public double GridWidth { get; }

    public double GridHeight { get; }

    public double Radius => Math.Min(4, Cell * 0.22);

    public double X(int column) => X0 + column * (Cell + Gap);

    public double Y(int row) => Y0 + row * (Cell + Gap);
}
