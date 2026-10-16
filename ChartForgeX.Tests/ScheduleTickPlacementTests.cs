using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects measured schedule captions on their own ticks at compact and wide sizes.</summary>
public sealed class ScheduleTickPlacementTests {
    private static readonly DateTime Start = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
    private static readonly ChartColor TickColor = ChartColor.FromHex("#7030A0");

    [Theory]
    [InlineData(ChartSeriesKind.StateTimeline, 12)]
    [InlineData(ChartSeriesKind.StateTimeline, 20)]
    [InlineData(ChartSeriesKind.GanttLane, 12)]
    [InlineData(ChartSeriesKind.GanttLane, 20)]
    [InlineData(ChartSeriesKind.Timeline, 12)]
    [InlineData(ChartSeriesKind.Timeline, 20)]
    [InlineData(ChartSeriesKind.Gantt, 12)]
    [InlineData(ChartSeriesKind.Gantt, 20)]
    public void AuthoredCaptionsUseRegularSpacingOnTheirOwnTicksWithoutChangingTheirStyleOrSourceLabels(ChartSeriesKind kind, double fontSize) {
        var chart = Fixture(kind, fontSize);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "schedule-authored-" + kind + "-" + fontSize);
        var labels = TickLabels(prepared);
        var expected = chart.Options.XAxis.Labels.Select(label => label.Text).ToArray();
        Assert.InRange(labels.Length, 2, expected.Length - 1);
        Assert.Equal(expected[0], labels[0].Text.Lines.Single().Text);
        var indexes = labels.Select(label => Array.IndexOf(expected, label.Text.Lines.Single().Text)).ToArray();
        for (var index = 2; index < indexes.Length; index++) Assert.Equal(indexes[1] - indexes[0], indexes[index] - indexes[index - 1]);
        Assert.All(labels, label => {
            Assert.Equal(fontSize, label.Text.Size);
            Assert.Equal(700, label.Text.Style.Font.Weight);
            Assert.Equal(TickColor, label.Color);
            Assert.Contains(label.Text.Lines.Single().Text, expected);
            Assert.InRange(Left(label), chart.Options.Padding.Left - .001, chart.Options.Size.Width - chart.Options.Padding.Right);
            Assert.True(Left(label) + label.Text.Metrics.Width <= chart.Options.Size.Width - chart.Options.Padding.Right + .001);
        });
        var separation = VisualTheme.Graphite().Spacing / 2;
        for (var index = 1; index < labels.Length; index++) {
            var previousRight = Left(labels[index - 1]) + labels[index - 1].Text.Metrics.Width;
            Assert.True(Left(labels[index]) - previousRight >= separation - .001);
        }
        var regions = prepared.Regions.Where(region => region.Role == "schedule-tick-label").ToArray();
        Assert.Equal(expected, regions.Select(region => region.Label));
        Assert.Equal(expected.Length, regions.Select(region => region.Id).Distinct().Count());
        Assert.Contains(regions, region => region.Bounds.Width == 0 && region.Bounds.Height == 0);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "schedule.axis-label-overflow");
        AssertOwnTicks(prepared, expected);
        Assert.Equal(fontSize, chart.Options.TickLabelStyle.FontSize);
        Assert.Equal("700", chart.Options.TickLabelStyle.FontWeight);
        Assert.Equal(TickColor, chart.Options.TickLabelStyle.Color);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.StateTimeline)]
    [InlineData(ChartSeriesKind.GanttLane)]
    public void ExplicitAllRetainsEveryAuthoredTickAndStyleEvenWhenTheirMeasuredBoxesOverlap(ChartSeriesKind kind) {
        var chart = Fixture(kind, 20).ConfigureXAxis(axis => axis.LabelDensity = ChartLabelDensity.All);
        var expected = chart.Options.XAxis.Labels.Select(label => label.Text).ToArray();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = TickLabels(prepared);
        Assert.Equal(expected, labels.Select(label => label.Text.Lines.Single().Text));
        Assert.All(labels, label => {
            Assert.Equal(20, label.Text.Size);
            Assert.Equal(700, label.Text.Style.Font.Weight);
            Assert.Equal(TickColor, label.Color);
        });
        Assert.Contains(Enumerable.Range(1, labels.Length - 1), index =>
            Left(labels[index]) < Left(labels[index - 1]) + labels[index - 1].Text.Metrics.Width);
        AssertOwnTicks(prepared, expected);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "schedule.axis-label-overflow");
    }

    [Theory]
    [InlineData(ChartSeriesKind.StateTimeline, 360)]
    [InlineData(ChartSeriesKind.StateTimeline, 800)]
    [InlineData(ChartSeriesKind.GanttLane, 360)]
    [InlineData(ChartSeriesKind.GanttLane, 800)]
    [InlineData(ChartSeriesKind.Timeline, 360)]
    [InlineData(ChartSeriesKind.Timeline, 800)]
    [InlineData(ChartSeriesKind.Gantt, 360)]
    [InlineData(ChartSeriesKind.Gantt, 800)]
    public void AutomaticCalendarTicksChooseAnEvenReadableStepBeforeGridAndLabelPainting(ChartSeriesKind kind, int width) {
        var chart = Fixture(kind, 12).WithSize(width, 360);
        chart.ConfigureXAxis(axis => { axis.Labels.Clear(); axis.TickCount = 13; axis.LabelFormatter = value => DateTime.FromOADate(value).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture); });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "schedule-calendar-" + kind + "-" + width);
        var labels = TickLabels(prepared);
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").OrderBy(line => line.Start.X).ToArray();
        Assert.InRange(labels.Length, 2, 12);
        Assert.Equal(labels.Length, grid.Length);
        for (var index = 0; index < labels.Length; index++) {
            Assert.Equal(grid[index].Start.X, labels[index].X, 7);
            Assert.Equal(12, labels[index].Text.Size);
            Assert.DoesNotContain("…", labels[index].Text.Lines.Single().Text, StringComparison.Ordinal);
            Assert.InRange(Left(labels[index]), chart.Options.Padding.Left - .001, chart.Options.Size.Width - chart.Options.Padding.Right);
            Assert.True(Left(labels[index]) + labels[index].Text.Metrics.Width <= chart.Options.Size.Width - chart.Options.Padding.Right + .001);
            // A classic Gantt plan also prioritizes its exact project endpoints;
            // the calendar sequence between those edge ticks remains regular.
            if (kind != ChartSeriesKind.Gantt && index > 1)
                Assert.Equal(grid[1].Start.X - grid[0].Start.X, grid[index].Start.X - grid[index - 1].Start.X, 6);
            if (kind == ChartSeriesKind.Gantt && index > 2 && index < labels.Length - 1)
                Assert.Equal(grid[2].Start.X - grid[1].Start.X, grid[index].Start.X - grid[index - 1].Start.X, 6);
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "schedule.axis-label-overflow" or "text.overflow");
        Assert.Equal(13, chart.Options.XAxis.TickCount);
        Assert.Equal(Start.ToOADate(), chart.Options.XAxis.Minimum);
        Assert.Equal(Start.AddHours(12).ToOADate(), chart.Options.XAxis.Maximum);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(-45)]
    [InlineData(45)]
    public void RotatedEndCaptionsReserveTheirRealFootprintAndRotateAroundTheirOwnTick(double angle) {
        var chart = Fixture(ChartSeriesKind.StateTimeline, 12).WithSize(720, 360).ConfigureXAxis(axis => {
            axis.Labels.RemoveRange(1, axis.Labels.Count - 2); axis.LabelAngle = angle; axis.LabelDensity = ChartLabelDensity.All;
        });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "schedule-rotated-" + angle.ToString(CultureInfo.InvariantCulture));
        var labels = TickLabels(prepared);
        var expected = chart.Options.XAxis.Labels.Select(label => label.Text).ToArray();
        Assert.Equal(2, labels.Length); AssertOwnTicks(prepared, expected);
        var regions = prepared.Regions.Where(region => region.Role == "schedule-tick-label").ToArray();
        Assert.All(regions, region => {
            Assert.True(region.Bounds.Left >= chart.Options.Padding.Left - .001);
            Assert.True(region.Bounds.Right <= chart.Options.Size.Width - chart.Options.Padding.Right + .001);
        });
        var rotations = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Rotation?.Degrees == angle).ToArray();
        Assert.Equal(labels.Length, rotations.Length);
        for (var index = 0; index < labels.Length; index++) Assert.Equal(labels[index].X, rotations[index].Rotation!.Value.X, 7);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.StateTimeline, -45)]
    [InlineData(ChartSeriesKind.StateTimeline, 45)]
    [InlineData(ChartSeriesKind.GanttLane, -45)]
    [InlineData(ChartSeriesKind.GanttLane, 45)]
    [InlineData(ChartSeriesKind.Timeline, -45)]
    [InlineData(ChartSeriesKind.Timeline, 45)]
    [InlineData(ChartSeriesKind.Gantt, -45)]
    [InlineData(ChartSeriesKind.Gantt, 45)]
    public void FullRotatedDateAndZoneCaptionsReserveTheirMeasuredHeightAboveTheAxisTitle(ChartSeriesKind kind, double angle) {
        var chart = LongEndpointFixture(kind, 360, angle);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "schedule-full-date-zone-" + kind + "-" + angle.ToString(CultureInfo.InvariantCulture));
        var labels = TickLabels(prepared);
        var expected = chart.Options.XAxis.Labels.Select(label => label.Text).ToArray();
        Assert.Equal(expected, labels.Select(label => label.Text.Lines.Single().Text));
        AssertOwnTicks(prepared, expected);
        Assert.All(labels, label => Assert.Equal(12, label.Text.Size));
        var title = Assert.Single(prepared.Regions, region => region.Role == "schedule-x-axis-title");
        Assert.Equal("Release window", title.Label);
        Assert.All(prepared.Regions.Where(region => region.Role == "schedule-tick-label"), region => {
            Assert.True(region.Bounds.Width > 0 && region.Bounds.Height > 0);
            Assert.True(region.Bounds.Left >= chart.Options.Padding.Left - .001);
            Assert.True(region.Bounds.Right <= chart.Options.Size.Width - chart.Options.Padding.Right + .001);
            Assert.True(region.Bounds.Bottom <= title.Bounds.Top + .001, "The full rotated date must finish before the axis title begins.");
        });
        Assert.Equal(angle, chart.Options.XAxis.LabelAngle);
        Assert.Equal(12, chart.Options.TickLabelStyle.FontSize);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "schedule.axis-label-overflow");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void UncontainableRotatedCaptionsKeepCompleteSemanticsAndReportOverflowWithoutClippedPaint() {
        var chart = LongEndpointFixture(ChartSeriesKind.StateTimeline, 120, 45);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "schedule-full-date-zone-constrained");
        Assert.Empty(TickLabels(prepared));
        var regions = prepared.Regions.Where(region => region.Role == "schedule-tick-label").ToArray();
        Assert.Equal(chart.Options.XAxis.Labels.Select(label => label.Text), regions.Select(region => region.Label));
        Assert.All(regions, region => { Assert.Equal(0, region.Bounds.Width); Assert.Equal(0, region.Bounds.Height); });
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "schedule.axis-label-overflow");
        Assert.Equal(45, chart.Options.XAxis.LabelAngle);
        Assert.Equal(12, chart.Options.TickLabelStyle.FontSize);
        Assert.Equal(120, prepared.ToRgba(new VisualRenderOptions(supersampling: 1)).Height);
    }

    [Fact]
    public void FixedGanttIntervalsKeepTheirTicksWhileSparseCaptionsStayOnAnEvenStride() {
        var chart = Fixture(ChartSeriesKind.Gantt, 12).ConfigureXAxis(axis => axis.Labels.Clear())
            .WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Hour));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").OrderBy(line => line.Start.X).ToArray();
        Assert.Equal(13, grid.Length);
        var regions = prepared.Regions.Where(region => region.Role == "schedule-tick-label").ToArray();
        Assert.Equal(13, regions.Length);
        var shown = Enumerable.Range(0, regions.Length).Where(index => regions[index].Bounds.Width > 0).ToArray();
        Assert.InRange(shown.Length, 2, 12);
        for (var index = 0; index < shown.Length; index++) {
            Assert.Equal(grid[shown[index]].Start.X, regions[shown[index]].Bounds.Left + regions[shown[index]].Bounds.Width / 2, 7);
            if (index > 1) Assert.Equal(shown[1] - shown[0], shown[index] - shown[index - 1]);
        }
        Assert.Equal(ChartTimeTickUnit.Hour, chart.Options.GanttTickInterval!.Unit);
        Assert.Equal(1, chart.Options.GanttTickInterval.Count);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void OverBudgetFixedIntervalsUseMeasuredAutomaticTicksWithoutChangingTheConfiguredInterval() {
        var chart = Chart.Create().WithSize(240, 360).WithLegend(false).WithHeader(false).WithPngFont(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
            .ConfigureGridStyle(style => style.ShowVerticalLines = true)
            .AddGanttTask("Window", Start, Start.AddDays(7))
            .WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Millisecond, 100));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "schedule-fixed-budget-fallback");
        var labels = TickLabels(prepared);
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").OrderBy(line => line.Start.X).ToArray();
        Assert.InRange(labels.Length, 2, 3);
        Assert.Equal(labels.Length, grid.Length);
        for (var index = 0; index < labels.Length; index++) {
            Assert.Equal(grid[index].Start.X, labels[index].X, 7);
            Assert.DoesNotContain("…", labels[index].Text.Lines.Single().Text, StringComparison.Ordinal);
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "schedule.axis-label-overflow");
        Assert.Equal(ChartTimeTickUnit.Millisecond, chart.Options.GanttTickInterval!.Unit);
        Assert.Equal(100, chart.Options.GanttTickInterval.Count);
        Assert.Equal(Start.ToOADate(), chart.Series[0].GanttItems[0].Start);
        Assert.Equal(Start.AddDays(7).ToOADate(), chart.Series[0].GanttItems[0].End);
        Assert.NotEmpty(prepared.ToPng());
    }

    private static Chart LongEndpointFixture(ChartSeriesKind kind, int height, double angle) =>
        Fixture(kind, 12).WithSize(720, height).WithHeader(false).WithPadding(14, 14, 14, 14).WithXAxis("Release window")
            .ConfigureXAxis(axis => {
                axis.Labels.Clear(); axis.LabelAngle = angle; axis.LabelDensity = ChartLabelDensity.All;
                foreach (var time in new[] { Start, Start.AddHours(12) }) axis.Labels.Add(new ChartAxisLabel(time.ToOADate(),
                    time.ToString("O", CultureInfo.InvariantCulture) + " (UTC / Europe/Warsaw)"));
            });

    private static Chart Fixture(ChartSeriesKind kind, double fontSize) {
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font), "The licensed Carlito fixture must be available for repeatable edge-label measurements.");
        var end = Start.AddHours(12);
        var chart = Chart.Create().WithSize(360, 360).WithTheme(VisualTheme.Graphite()).WithPngFont(font)
            .WithLegend(false).WithXAxisTimeScale().ConfigureGridStyle(style => style.ShowVerticalLines = true)
            .ConfigureTickLabelStyle(style => style.WithFontSize(fontSize).WithWeight("700").WithColor(TickColor));
        switch (kind) {
            case ChartSeriesKind.StateTimeline: chart.AddStateTimelineLane("Primary", new[] { new ChartStateTimelineSegment(Start, end, "up") }); break;
            case ChartSeriesKind.GanttLane: chart.AddGanttLane("Primary", new[] { new ChartGanttLaneItem(Start, end, "up") }); break;
            case ChartSeriesKind.Timeline: chart.AddTimelineItem("Primary", Start, end); break;
            case ChartSeriesKind.Gantt: chart.AddGanttTask("Primary", Start, end, .5); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
        chart.ConfigureXAxis(axis => {
            axis.Minimum = Start.ToOADate(); axis.Maximum = end.ToOADate();
            for (var index = 0; index < 13; index++) {
                var time = Start.AddHours(index);
                axis.Labels.Add(new ChartAxisLabel(time.ToOADate(), time.ToString("HH:mm", CultureInfo.InvariantCulture)));
            }
        });
        return chart;
    }

    private static void AssertOwnTicks(PreparedVisual prepared, string[] authored) {
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").OrderBy(line => line.Start.X).ToArray();
        Assert.Equal(authored.Length, grid.Length);
        foreach (var label in TickLabels(prepared)) {
            var index = Array.IndexOf(authored, label.Text.Lines.Single().Text);
            Assert.InRange(index, 0, grid.Length - 1);
            Assert.InRange(Math.Abs(label.X - grid[index].Start.X), 0, .5);
        }
    }
    private static VisualSceneText[] TickLabels(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role == "schedule-tick-label").OrderBy(Left).ToArray();
    private static double Left(VisualSceneText text) => text.Text.Lines.Min(text.LineLeft);
}
