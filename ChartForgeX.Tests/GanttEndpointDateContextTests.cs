using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttEndpointDateContextTests {
    private static readonly DateTime Start = new(2026, 3, 2, 6, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Finish = new(2026, 3, 19, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("automatic", VisualThemeMode.Light)]
    [InlineData("automatic", VisualThemeMode.Dark)]
    [InlineData("rejected", VisualThemeMode.Light)]
    [InlineData("rejected", VisualThemeMode.Dark)]
    [InlineData("counted", VisualThemeMode.Light)]
    [InlineData("counted", VisualThemeMode.Dark)]
    [InlineData("windowed", VisualThemeMode.Light)]
    [InlineData("windowed", VisualThemeMode.Dark)]
    public void DefaultDayScaleProjectEndpointsKeepDateIdentityWithoutMovingTheirInstants(string policy, VisualThemeMode mode) {
        var chart = Fixture(mode); var first = Start; var last = Finish;
        if (policy == "rejected") chart.WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Millisecond));
        if (policy == "counted") chart.ConfigureXAxis(axis => axis.TickCount = 2);
        if (policy == "windowed") {
            first = Start.AddDays(2).AddHours(3); last = Finish.AddDays(-2).AddHours(3);
            chart.ConfigureXAxis(axis => axis.WithBounds(first.ToOADate(), last.ToOADate()));
        }
        var prepared = Prepare(chart);
        var labels = Captions(prepared);
        Assert.InRange(labels.Length, 2, 6);
        Assert.Equal(first.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), labels[0].Text.Lines.Single().Text);
        Assert.Equal(last.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), labels[^1].Text.Lines.Single().Text);
        Assert.All(labels, label => Assert.Matches("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", label.Text.Lines.Single().Text));
        var task = Assert.Single(prepared.Regions, region => region.Role == "gantt-task");
        Assert.Equal(task.Bounds.Left, labels[0].X, 7);
        Assert.Equal(task.Bounds.Right, labels[^1].X, 7);
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").ToArray();
        Assert.Equal(labels.Length, grid.Length);
        for (var index = 0; index < labels.Length; index++) Assert.Equal(grid[index].Start.X, labels[index].X, 7);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(labels.Select(label => label.Text.Lines.Single().Text), svg.Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "schedule-tick-label").Select(node => node.Value));
        Assert.Equal(Start.ToOADate(), chart.Series[0].GanttItems[0].Start);
        Assert.Equal(Finish.ToOADate(), chart.Series[0].GanttItems[0].End);
        if (policy == "counted") Assert.Equal(2, labels.Length);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "schedule.axis-label-overflow");
        ScatterBubbleColorRenderingTests.Capture(prepared, $"gantt-endpoint-date-context-{policy}-{mode}");
        var pixels = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.Equal(400, pixels.Width); Assert.Equal(300, pixels.Height);
    }

    [Theory]
    [InlineData("formatter")]
    [InlineData("value-format")]
    [InlineData("fixed")]
    [InlineData("all")]
    [InlineData("subday")]
    public void AuthoredAndSubdayClockPoliciesKeepTheirExistingNotation(string policy) {
        var chart = Fixture(VisualThemeMode.Light);
        Func<double, string> clock = value => DateTime.FromOADate(value).ToString("HH:mm", CultureInfo.InvariantCulture);
        if (policy == "formatter") chart.ConfigureXAxis(axis => axis.WithLabelFormatter(clock));
        if (policy == "value-format") chart.ConfigureXAxis(axis => axis.WithValueFormat(ChartValueFormat.Custom(clock)));
        if (policy == "fixed") chart.WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Hour, 12));
        if (policy == "all") chart.ConfigureXAxis(axis => axis.LabelDensity = ChartLabelDensity.All);
        if (policy == "subday") chart = Chart.Create().WithSize(400, 300).WithHeader(false).WithLegend(false).WithPngFont(Font())
            .AddGanttTask("Project", Start, Start.AddHours(6));
        var prepared = Prepare(chart);
        var text = Captions(prepared).Select(label => label.Text.Lines.Single().Text).ToArray();
        if (policy is "formatter" or "value-format" or "subday") {
            Assert.All(text, label => Assert.Matches("^[0-2][0-9]:[0-5][0-9]$", label));
            Assert.Contains("06:00", text);
            Assert.Contains("12:00", text);
        } else {
            Assert.Contains(prepared.Regions, region => region.Role == "schedule-tick-label" && region.Label == "12:00");
            if (policy == "all") Assert.Contains("06:00", text);
            else {
                Assert.Equal(ChartTimeTickUnit.Hour, chart.Options.GanttTickInterval!.Unit);
                Assert.Equal(12, chart.Options.GanttTickInterval.Count);
                Assert.Equal(35, prepared.Scene.Nodes.OfType<VisualSceneLine>().Count(line => line.Role == "schedule-grid"));
            }
        }
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void MermaidNonMidnightPlansKeepTheirExistingDefaultAndAuthoredDateContext() {
        const string source = "gantt\ndateFormat YYYY-MM-DD HH:mm\nTask :2026-03-02 06:00, 17d";
        var document = new MermaidParser().ParseGantt(source).Document!;
        var chart = document.ToChart().WithSize(400, 300).WithPngFont(Font());
        Assert.All(Captions(Prepare(chart)), label => Assert.Matches("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", label.Text.Lines.Single().Text));
        document.AxisFormat = "%d/%m";
        var authored = document.ToChart().WithSize(400, 300).WithPngFont(Font());
        Assert.All(Captions(Prepare(authored)), label => Assert.Matches("^[0-9]{2}/03$", label.Text.Lines.Single().Text));
        Assert.Equal(Start.ToOADate(), chart.Series[0].GanttItems[0].Start);
    }

    private static Chart Fixture(VisualThemeMode mode) => Chart.Create().WithSize(400, 300).WithHeader(false).WithLegend(false)
        .WithPngFont(Font()).WithTheme(VisualTheme.Graphite(), mode).ConfigureGridStyle(style => style.ShowVerticalLines = true)
        .AddGanttTask("Project", Start, Finish);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role == "schedule-tick-label").ToArray();
    private static string Font() => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
}
