using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the compact and wide calendar rhythm together, including mixed date/clock captions.</summary>
public sealed class CompactCalendarAxisTests {
    [Theory]
    [InlineData("state", 400, VisualThemeMode.Light, 6)]
    [InlineData("state", 400, VisualThemeMode.Dark, 6)]
    [InlineData("state", 800, VisualThemeMode.Light, 6)]
    [InlineData("gantt", 400, VisualThemeMode.Light, 5)]
    [InlineData("gantt", 400, VisualThemeMode.Dark, 5)]
    [InlineData("timeline", 400, VisualThemeMode.Light, 4)]
    [InlineData("lane", 400, VisualThemeMode.Light, 4)]
    public void ShortScheduleCaptionsKeepUsefulCalendarStepsWithoutMovingOffTheirGrid(string family, int width, VisualThemeMode mode, int minimum) {
        var chart = family switch {
            "state" => StateTimelineRecipe.Create(mode: mode),
            "gantt" => GanttRecipe.Create(mode: mode),
            "timeline" => TimelineRecipe.Create(mode: mode),
            _ => GanttLaneRecipe.Create(mode: mode)
        };
        chart.WithTheme(VisualTheme.Graphite(), mode).WithPngFont(Font()).WithSize(width, width == 400 ? 280 : 440)
            .WithHeader(false).WithPadding(16, 16, 16, 16).ConfigureGridStyle(style => style.ShowVerticalLines = true);
        if (width == 400) chart.ConfigureTickLabelStyle(style => style.WithFontSize(13.5));
        var prepared = Prepare(chart); var labels = Captions(prepared, "schedule-tick-label");
        Assert.InRange(labels.Length, minimum, 8);
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "schedule-grid").ToArray();
        Assert.Equal(labels.Length, grid.Length);
        for (var index = 0; index < labels.Length; index++) {
            Assert.Equal(grid[index].Start.X, labels[index].X, 7);
            if (index > 1) Assert.Equal(labels[1].X - labels[0].X, labels[index].X - labels[index - 1].X, 7);
            if (index > 0) Assert.True(labels[index].X - labels[index - 1].X >= (labels[index].Text.Metrics.Width + labels[index - 1].Text.Metrics.Width) / 2);
        }
        if (family == "state") Assert.Equal(6, labels.Length);
        if (family == "timeline") {
            var sourceMinimum = chart.Series.SelectMany(series => series.Points).Min(point => point.X);
            var year = DateTime.FromOADate(sourceMinimum).Year;
            var dates = labels.Select(label => DateTime.ParseExact(year + " " + label.Text.Lines.Single().Text,
                "yyyy MMM d", CultureInfo.InvariantCulture)).ToArray();
            var days = (dates[1] - dates[0]).Days;
            // The axis opens on the first item's day, so the first caption is that day or the next step (design rule 1).
            Assert.True(dates[0].ToOADate() >= Math.Floor(sourceMinimum) && dates[0].ToOADate() < sourceMinimum + days);
            Assert.All(dates, date => Assert.Equal(0, date.Ticks / TimeSpan.TicksPerDay % days));
        }
        AssertPortable(prepared, labels, "schedule-tick-label", $"compact-calendar-{family}-{width}-{mode}");
    }

    [Theory]
    [InlineData(400, VisualThemeMode.Light)]
    [InlineData(400, VisualThemeMode.Dark)]
    [InlineData(420, VisualThemeMode.Light)]
    public void NarrowFullIsoDatesUseARegularSourceDayStrideBeforeFallingBackToTwoLabels(int width, VisualThemeMode mode) {
        var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithSize(width, 260).WithTransparentBackground().WithHeader(false).WithLegend(false)
            .WithPadding(64, 14, 20, 46).WithXAxisTimeScale(TimeZoneInfo.Utc, true, "UTC").WithThemeMode(mode).WithPngFont(Font())
            .AddLine("Score", Enumerable.Range(0, 19).Select(index => new ChartPoint(start.AddDays(index), 50 + index % 5 * 4)));
        var prepared = Prepare(chart); var labels = Captions(prepared, "axis-x-label");
        Assert.InRange(labels.Length, 4, 8);
        Assert.All(labels, label => Assert.Matches("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", label.Text.Lines.Single().Text));
        var dates = labels.Select(label => DateTime.ParseExact(label.Text.Lines.Single().Text, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray();
        var stride = dates[1] - dates[0];
        Assert.True(dates[0] >= start && dates[0] < start + stride, "The first full calendar caption must be the first boundary of the stable phase inside the window.");
        Assert.Equal(0, dates[0].Ticks / TimeSpan.TicksPerDay % stride.Days);
        for (var index = 2; index < dates.Length; index++) Assert.Equal(dates[1] - dates[0], dates[index] - dates[index - 1]);
        AssertPortable(prepared, labels, "axis-x-label", $"compact-calendar-iso-{width}-{mode}");
    }

    [Theory]
    [InlineData(480, VisualThemeMode.Light)]
    [InlineData(480, VisualThemeMode.Dark)]
    [InlineData(720, VisualThemeMode.Light)]
    public void ADateBesideAClockCaptionUsesTheirMeasuredMeanAndKeepsSixHourBoundaries(int width, VisualThemeMode mode) {
        var start = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithSize(width, 160).WithTransparentBackground().WithHeader(false).WithPadding(0, 10, 16, 0)
            .WithXAxisTimeScale(TimeZoneInfo.Utc, true, "UTC").WithThemeMode(mode).WithPngFont(Font())
            .WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#2E7D32")));
        chart.AddStateTimelineLane("Nightly run", new[] { new ChartStateTimelineSegment(start, start.AddHours(24), "up") });
        chart.AddStateTimelineLane("Weekly run", new[] { new ChartStateTimelineSegment(start, start.AddHours(24), "up") });
        var prepared = Prepare(chart); var labels = Captions(prepared, "schedule-tick-label");
        Assert.Equal(new[] { "2026-10-13", "06:00", "12:00", "18:00", "2026-10-14" }, labels.Select(label => label.Text.Lines.Single().Text));
        for (var index = 2; index < labels.Length; index++) Assert.Equal(labels[1].X - labels[0].X, labels[index].X - labels[index - 1].X, 7);
        AssertPortable(prepared, labels, "schedule-tick-label", $"compact-calendar-mixed-{width}-{mode}");
    }

    [Fact]
    public void WideMonthWindowRetainsWeeklyFullIsoLabelsRatherThanFillingEveryAvailableSlot() {
        var start = new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithSize(1440, 420).WithXAxisTimeScale()
            .AddLine("Health", Enumerable.Range(0, 30).Select(index => new ChartPoint(start.AddDays(index), 75 + index % 7)));
        var prepared = Prepare(chart); var labels = Captions(prepared, "axis-x-label");
        Assert.Equal(new[] { "2026-09-14", "2026-09-21", "2026-09-28", "2026-10-05", "2026-10-12" }, labels.Select(label => label.Text.Lines.Single().Text));
        AssertPortable(prepared, labels, "axis-x-label", "compact-calendar-wide-weekly");
    }

    [Theory]
    [InlineData(false, VisualThemeMode.Light)]
    [InlineData(false, VisualThemeMode.Dark)]
    [InlineData(true, VisualThemeMode.Light)]
    [InlineData(true, VisualThemeMode.Dark)]
    public void UnresolvedFontsKeepNonMidnightTasksAndRegularDayCaptionsInTheSameMeasuredProjection(bool fixedDays, VisualThemeMode mode) {
        var start = new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc); var end = start.AddDays(17);
        var chart = Chart.Create().WithSize(900, 300).WithHeader(false).WithLegend(false)
            .WithThemeMode(mode).ConfigureGridStyle(style => style.ShowVerticalLines = true).AddGanttTask("Project", start, end);
        if (fixedDays) chart.WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Day));
        var request = VisualExportRequest.ForChart(chart).Context;
        var context = new VisualRenderContext(request.Layout, request.Theme, request.ThemeMode, request.Frame,
            FontSpec.FromFamily("CFX Missing Calendar Regression 3A9859"));
        var prepared = chart.Prepare(context); var labels = Captions(prepared, "schedule-tick-label");
        Assert.InRange(labels.Length, 2, 18);
        var task = Assert.Single(prepared.Regions, region => region.Role == "gantt-task");
        var dates = labels.Select(label => DateTime.ParseExact(label.Text.Lines.Single().Text, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray();
        for (var index = 0; index < labels.Length; index++) {
            var x = task.Bounds.Left + (dates[index].ToOADate() - start.ToOADate()) / (end.ToOADate() - start.ToOADate()) * task.Bounds.Width;
            Assert.Equal(x, labels[index].X, 7);
            if (index > 1) Assert.Equal(dates[1] - dates[0], dates[index] - dates[index - 1]);
        }
        Assert.Equal(start.ToOADate(), chart.Series[0].GanttItems[0].Start);
        Assert.Equal(end.ToOADate(), chart.Series[0].GanttItems[0].End);
        AssertPortable(prepared, labels, "schedule-tick-label", $"compact-calendar-missing-font-{fixedDays}-{mode}", allowThinnedAxis: fixedDays);
    }

    private static void AssertPortable(PreparedVisual prepared, VisualSceneText[] labels, string role, string capture, bool allowThinnedAxis = false) {
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(labels.Select(label => label.Text.Lines.Single().Text), svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).Select(node => node.Value));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "cartesian.axis-label-overflow" or "text.overflow"
            || (!allowThinnedAxis && diagnostic.Code == "schedule.axis-label-overflow"));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        ScatterBubbleColorRenderingTests.Capture(prepared, capture);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Captions(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == role).ToArray();
    private static string Font() => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
}
