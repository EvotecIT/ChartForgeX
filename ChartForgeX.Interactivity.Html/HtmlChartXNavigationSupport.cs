using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Why a saved page cannot run X navigation from its embedded data.</summary>
public enum HtmlChartXNavigationIssue {
    /// <summary>The chart has no Cartesian X and value projection, for example Pie, Radar, maps or hierarchy charts.</summary>
    NoCartesianProjection,
    /// <summary>A series family is outside the standalone re-projection families.</summary>
    UnsupportedSeriesKind,
    /// <summary>The chart uses additional value axes, which capture no single value domain.</summary>
    AdditionalValueAxes,
    /// <summary>A vertical chart has a secondary X axis, or a horizontal chart a secondary category axis.</summary>
    SecondaryCategoryAxis,
    /// <summary>An axis uses a calendar tick step or alternating grid bands, which are painted between prepared ticks.</summary>
    StaticTicks,
    /// <summary>A time axis is not an automatic calendar on the horizontal X axis: it has a value format, label formatter,
    /// explicit labels or tick count, sessions, discrete spacing, rotated captions, or an uncaptured culture or zone.</summary>
    UnsupportedTimeAxis,
    /// <summary>Stacked areas in one stack do not share the same X coordinates.</summary>
    UnalignedStackedArea
}

/// <summary>One reason a saved page shows no X navigation without a connected host.</summary>
public sealed class HtmlChartXNavigationDiagnostic {
    internal HtmlChartXNavigationDiagnostic(HtmlChartXNavigationIssue issue, string message, string? seriesName = null) {
        Issue = issue; Message = message; SeriesName = seriesName;
    }

    /// <summary>Gets the stable issue kind.</summary>
    public HtmlChartXNavigationIssue Issue { get; }
    /// <summary>Gets a readable explanation.</summary>
    public string Message { get; }
    /// <summary>Gets the series that caused the issue, or null for chart-level issues.</summary>
    public string? SeriesName { get; }

    /// <inheritdoc />
    public override string ToString() => Issue + ": " + Message;
}

/// <summary>Reports whether a chart's navigator, scrollbar and range presets work in a saved page without a native connection.</summary>
/// <remarks>A saved page computes windows, ticks and captions in the browser from the embedded drawing. The supported families are
/// Line, StepLine, Area, StepArea, StackedArea, Bar, HorizontalBar, Scatter, Bubble, Candlestick, OHLC, RangeBand, RangeArea,
/// BoxPlot, ErrorBar, Waterfall, Lollipop, Dumbbell and RangeBar on linear, logarithmic, symmetric-logarithmic or automatic calendar
/// time X axes. A connected host prepares every other chart natively instead.</remarks>
public static class HtmlChartXNavigationSupport {
    private static readonly HashSet<ChartSeriesKind> Families = new HashSet<ChartSeriesKind> {
        ChartSeriesKind.Line, ChartSeriesKind.StepLine, ChartSeriesKind.Area, ChartSeriesKind.StepArea, ChartSeriesKind.StackedArea,
        ChartSeriesKind.Bar, ChartSeriesKind.HorizontalBar, ChartSeriesKind.Scatter, ChartSeriesKind.Bubble, ChartSeriesKind.Candlestick,
        ChartSeriesKind.Ohlc, ChartSeriesKind.RangeBand, ChartSeriesKind.RangeArea, ChartSeriesKind.BoxPlot, ChartSeriesKind.ErrorBar,
        ChartSeriesKind.Waterfall, ChartSeriesKind.Lollipop, ChartSeriesKind.Dumbbell, ChartSeriesKind.RangeBar
    };

    /// <summary>Prepares the chart at its authored size and lists why a saved page could not navigate it.</summary>
    /// <param name="chart">The chart to check.</param>
    /// <returns>An empty list when a saved page supports the navigator, scrollbar, range presets and reset.</returns>
    public static IReadOnlyList<HtmlChartXNavigationDiagnostic> DiagnoseStandalone(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return Diagnose(chart, chart.Prepare(VisualExportRequest.ForChart(chart).Context));
    }

    internal static IReadOnlyList<HtmlChartXNavigationDiagnostic> Diagnose(Chart chart, PreparedVisual prepared) {
        var issues = new List<HtmlChartXNavigationDiagnostic>();
        if (chart.AdditionalValueAxes.Count > 0)
            issues.Add(new HtmlChartXNavigationDiagnostic(HtmlChartXNavigationIssue.AdditionalValueAxes,
                "Additional value axes capture no single value domain for a saved window."));
        foreach (var series in chart.Series.Where(series => !Families.Contains(series.Kind)))
            issues.Add(new HtmlChartXNavigationDiagnostic(HtmlChartXNavigationIssue.UnsupportedSeriesKind,
                series.Kind + " series need a connected host to prepare a window.", series.Name));
        var data = prepared.Scene.DataProjection;
        if (data == null || !(data.HasX || prepared.XProjection != null) || !(data.Y != null || prepared.YProjection != null)) {
            if (issues.Count == 0)
                issues.Add(new HtmlChartXNavigationDiagnostic(HtmlChartXNavigationIssue.NoCartesianProjection,
                    "The chart has no Cartesian X and value projection to navigate."));
            return issues.AsReadOnly();
        }
        var horizontal = prepared.CartesianOrientation == ChartOrientation.Horizontal;
        if (horizontal ? prepared.SecondaryYProjection != null : prepared.SecondaryXProjection != null)
            issues.Add(new HtmlChartXNavigationDiagnostic(HtmlChartXNavigationIssue.SecondaryCategoryAxis,
                "A secondary category axis has no single window for the navigator."));
        foreach (var (name, captured, axis) in new[] {
            ("x", prepared.XProjection, data.HasX ? data.X : null), ("y", prepared.YProjection, data.Y),
            ("secondaryX", prepared.SecondaryXProjection, null), ("secondaryY", prepared.SecondaryYProjection, null) }) {
            if (captured == null && axis == null) continue;
            if ((captured != null ? captured.TickFacts : axis!.TickFacts)?.Static == true)
                issues.Add(new HtmlChartXNavigationDiagnostic(HtmlChartXNavigationIssue.StaticTicks,
                    "The " + name + " axis uses a calendar tick step or grid bands, which a saved page cannot repaint."));
            var scale = captured?.Scale ?? axis!.Scale;
            if (scale != ChartScaleKind.Time) continue;
            var calendar = name == "x" && !horizontal && data.HasX && data.X.CalendarCaptions && data.X.LabelGap != null
                && (data.X.TimeZone == null || data.X.TimeZone.Offsets.Count > 0);
            if (!calendar)
                issues.Add(new HtmlChartXNavigationDiagnostic(HtmlChartXNavigationIssue.UnsupportedTimeAxis,
                    "The " + name + " time axis is not an automatic calendar a saved page can re-tick."));
        }
        var stacks = chart.Series.Where(series => series.Kind == ChartSeriesKind.StackedArea)
            .GroupBy(series => series.ValueAxis + "|" + series.StackGroup);
        foreach (var stack in stacks) {
            var shapes = stack.Select(series => string.Join(",", series.Points.Where(point => !double.IsNaN(point.Y))
                .Select(point => point.X).OrderBy(x => x))).Distinct(StringComparer.Ordinal).Count();
            if (shapes > 1)
                issues.Add(new HtmlChartXNavigationDiagnostic(HtmlChartXNavigationIssue.UnalignedStackedArea,
                    "Stacked areas in one stack must share their X coordinates.", stack.First().Name));
        }
        return issues.AsReadOnly();
    }
}
