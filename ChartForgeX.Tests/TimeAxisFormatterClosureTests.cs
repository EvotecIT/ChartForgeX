using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TimeAxisFormatterClosureTests {
    private static readonly DateTime Start = new(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredFormatterExceptionsFromFineCalendarCandidatesEscapePreparation(bool overflow) {
        var failure = overflow ? (Exception)new OverflowException("Authored clock overflow") : new ArgumentException("Authored clock argument");
        var failed = false;
        var chart = Chart.Create().WithSize(960, 360).WithHeader(false).WithLegend(false).WithXAxisTimeScale()
            .AddLine("Readings", new[] { new ChartPoint(Start, 10), new ChartPoint(Start.AddDays(4), 20) })
            .ConfigureXAxis(axis => {
                axis.WithBounds(Start.ToOADate(), Start.AddDays(4).ToOADate());
                axis.LabelDensity = ChartLabelDensity.Dense;
                axis.WithLabelFormatter(value => {
                    if (!failed && DateTime.FromOADate(value).TimeOfDay != TimeSpan.Zero) { failed = true; throw failure; }
                    return DateTime.FromOADate(value).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                });
            });
        Assert.Same(failure, Record.Exception(() => chart.Prepare(VisualExportRequest.ForChart(chart).Context)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObservationCadenceFitDoesNotConsumeTheAuthoredFitException(bool overflow) {
        var failure = overflow ? (Exception)new OverflowException("Authored cadence overflow") : new ArgumentException("Authored cadence argument");
        var observations = Enumerable.Range(0, 15).Select(index => Start.AddDays(index * 5).ToOADate()).ToArray();
        Assert.Same(failure, Record.Exception(() => ChartTimeScale.GenerateFitting(new ChartAxis().WithTimeScale(), observations[0], observations[^1],
            _ => throw failure, allowCompactDates: true, regularObservations: observations)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GanttClockCallbacksRetainSubdayTicksAcrossMultiDayWindows(bool useValueFormat) {
        Func<double, string> clock = value => DateTime.FromOADate(value).ToString("HH:mm", CultureInfo.InvariantCulture);
        var chart = Chart.Create().WithSize(960, 360).WithHeader(false).WithLegend(false)
            .WithPngFont(Font()).AddGanttTask("Window", Start, Start.AddDays(2)).ConfigureXAxis(axis => {
                if (useValueFormat) axis.WithValueFormat(ChartValueFormat.Custom(clock));
                else axis.WithLabelFormatter(clock);
            });
        Assert.NotNull(chart.Options.XAxis.ValueFormat);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = Captions(prepared);
        Assert.True(labels.Length > 3, "An authored clock formatter may select more than one tick per day when complete captions fit.");
        Assert.Contains(labels, label => label.Text.Lines.Single().Text != "00:00");
        for (var index = 1; index < labels.Length; index++)
            Assert.NotEqual(labels[index - 1].Text.Lines.Single().Text, labels[index].Text.Lines.Single().Text);
        Assert.All(labels, label => Assert.Matches("^[0-2][0-9]:[0-5][0-9]$", label.Text.Lines.Single().Text));
        ScatterBubbleColorRenderingTests.Capture(prepared, "gantt-authored-clock-" + useValueFormat);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 960)]
    [InlineData(false, 320)]
    [InlineData(true, 320)]
    public void AutomaticDateFormatsChooseDistinctCalendarCaptionsBeforePaint(bool dark, int width) {
        var chart = new MermaidParser().ParseGantt("gantt\naxisFormat %a %d %b\nFirst :first, 2026-01-04, 4d").Document!
            .ToChart(new MermaidGanttRenderOptions { Width = width, Height = 360 })
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithPngFont(Font()).ConfigureGridStyle(style => style.ShowVerticalLines = true);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"gantt-distinct-date-{dark}-{width}");
        var labels = Captions(prepared);
        var texts = labels.Select(label => label.Text.Lines.Single().Text).ToArray();
        Assert.InRange(labels.Length, 2, 5);
        Assert.Equal(texts.Length, texts.Distinct(StringComparer.Ordinal).Count());
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").ToArray();
        Assert.Equal(labels.Length, grid.Length);
        for (var index = 0; index < labels.Length; index++) {
            Assert.Equal(grid[index].Start.X, labels[index].X, 7);
            Assert.True(DateTime.TryParseExact(texts[index] + " 2026", "ddd dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _));
        }
        var svgLabels = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "schedule-tick-label");
        Assert.Equal(texts, svgLabels.Select(node => node.Value));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "schedule.axis-label-overflow" or "text.overflow");
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.Equal(width, image.Width); Assert.Equal(360, image.Height);
        chart.ConfigureXAxis(axis => axis.TickCount = 4);
        Assert.InRange(Captions(chart.Prepare(VisualExportRequest.ForChart(chart).Context)).Length, 2, 4);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void CartesianDateCallbacksUseTheSameAutomaticPrecisionPolicy(VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(960, 360).WithHeader(false).WithLegend(false).WithPngFont(Font()).WithThemeMode(mode)
            .WithXAxisTimeScale().AddLine("Readings", new[] { new ChartPoint(Start, 10), new ChartPoint(Start.AddDays(4), 20) })
            .ConfigureXAxis(axis => {
                axis.WithBounds(Start.ToOADate(), Start.AddDays(4).ToOADate());
                axis.WithLabelFormatter(value => DateTime.FromOADate(value).ToString("ddd dd MMM", CultureInfo.InvariantCulture));
            });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "axis-x-label")
            .Select(text => text.Text.Lines.Single().Text).ToArray();
        Assert.InRange(labels.Length, 2, 5);
        Assert.Equal(labels.Length, labels.Distinct(StringComparer.Ordinal).Count());
        ScatterBubbleColorRenderingTests.Capture(prepared, "cartesian-distinct-date-" + mode);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredFixedIntervalsAndAllDensityKeepTheirRepeatedDateCaptions(bool fixedInterval) {
        var chart = Chart.Create().WithSize(1600, 360).WithHeader(false).WithLegend(false).WithPngFont(Font())
            .AddGanttTask("Window", Start, Start.AddDays(2))
            .ConfigureXAxis(axis => axis.WithLabelFormatter(value => DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        if (fixedInterval) chart.WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Hour, 12));
        else chart.ConfigureXAxis(axis => axis.LabelDensity = ChartLabelDensity.All);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = Captions(prepared).Select(label => label.Text.Lines.Single().Text).ToArray();
        Assert.True(labels.Length > labels.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(labels.Length, prepared.Regions.Count(region => region.Role == "schedule-tick-label" && region.Bounds.Width > 0));
    }

    private static string Font() => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role == "schedule-tick-label").ToArray();
}
