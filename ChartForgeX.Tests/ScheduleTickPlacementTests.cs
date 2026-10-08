using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects readable time labels after the first and last ticks move inside compact schedule frames.</summary>
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
    public void AutomaticDensityKeepsMeasuredStyledTicksApartAfterClampingAndRetainsEverySourceLabel(ChartSeriesKind kind, double fontSize) {
        var chart = Fixture(kind, fontSize);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = TickLabels(prepared);
        var expected = chart.Options.XAxis.Labels.Select(label => label.Text).ToArray();
        Assert.InRange(labels.Length, 2, expected.Length - 1);
        Assert.Equal(expected[0], labels[0].Text.Lines.Single().Text);
        Assert.Equal(expected[^1], labels[^1].Text.Lines.Single().Text);
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
            Assert.True(Left(labels[index]) - previousRight >= separation - .001,
                "Endpoint clamping must not consume the automatic spacing between two measured tick labels.");
        }
        var regions = prepared.Regions.Where(region => region.Role == "schedule-tick-label").ToArray();
        Assert.Equal(expected, regions.Select(region => region.Label));
        Assert.Equal(expected.Length, regions.Select(region => region.Id).Distinct().Count());
        Assert.Contains(regions, region => region.Bounds.Width == 0 && region.Bounds.Height == 0);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "schedule.axis-label-overflow");
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
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "schedule.axis-label-overflow");
    }

    private static Chart Fixture(ChartSeriesKind kind, double fontSize) {
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font), "The licensed Carlito fixture must be available for repeatable edge-label measurements.");
        var end = Start.AddHours(12);
        var chart = Chart.Create().WithSize(360, 360).WithTheme(ChartTheme.GraphiteLight()).WithPngFont(font)
            .WithLegend(false).WithXAxisTimeScale()
            .WithTickLabelStyle(style => style.WithFontSize(fontSize).WithWeight("700").WithColor(TickColor));
        switch (kind) {
            case ChartSeriesKind.StateTimeline: chart.AddStateTimelineLane("Primary", new[] { new ChartStateTimelineSegment(Start, end, "up") }); break;
            case ChartSeriesKind.GanttLane: chart.AddGanttLane("Primary", new[] { new ChartGanttLaneItem(Start, end, "up") }); break;
            case ChartSeriesKind.Timeline: chart.AddTimelineItem("Primary", Start, end); break;
            case ChartSeriesKind.Gantt: chart.AddGanttTask("Primary", Start, end, .5); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
        chart.ConfigureXAxis(axis => {
            axis.Minimum = Start.ToOADate(); axis.Maximum = end.ToOADate();
            for (var index = 0; index < 7; index++) {
                var time = Start.AddHours(index * 2);
                axis.Labels.Add(new ChartAxisLabel(time.ToOADate(), time.ToString("HH:mm", CultureInfo.InvariantCulture)));
            }
        });
        return chart;
    }

    private static VisualSceneText[] TickLabels(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role == "schedule-tick-label").OrderBy(Left).ToArray();

    private static double Left(VisualSceneText text) => text.Text.Lines.Min(text.LineLeft);
}
