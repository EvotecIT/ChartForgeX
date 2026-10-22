using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MarkupTimelineDateIntentTests {
    private static readonly DateTime Start = new(2026, 10, 1);
    private const string Commands = "id launch\ntitle Launch plan\ntype gantt\ntoday 2026-10-12\n"
        + "task Design 2026-10-01 2026-10-08 progress=1\n"
        + "task \"Build engine\" 2026-10-06 2026-10-20 progress=0.6 dependsOn=0\n"
        + "task \"Playground UI\" 2026-10-14 2026-10-24 progress=0.2 dependsOn=1\n"
        + "milestone Launch 2026-10-27 dependsOn=2";
    private const string Table = "# Schedule\n\n```chartforgex timeline v1 {#launch title=\"Launch plan\" type=gantt today=2026-10-12}\n"
        + "| kind | label | start | end | progress | dependsOn |\n"
        + "| --- | --- | --- | --- | --- | --- |\n"
        + "| task | Design | 2026-10-01 | 2026-10-08 | 1 | |\n"
        + "| task | Build engine | 2026-10-06 | 2026-10-20 | 0.6 | 0 |\n"
        + "| task | Playground UI | 2026-10-14 | 2026-10-24 | 0.2 | 1 |\n"
        + "| milestone | Launch | 2026-10-27 | | | 2 |\n```";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void DatedCommandsAndFencedTablesMatchNativeCalendarSvgAndPng(bool fenced, bool dark) {
        var source = fenced ? Table : Commands;
        var (chart, artifact) = Parse(source, fenced);
        var expected = NativeLaunch();
        foreach (var model in new[] { chart, expected }) model.WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        Assert.Equal(ChartScaleKind.Time, chart.Options.XAxis.Scale);
        Assert.Equal(new[] { "Design", "Build engine", "Playground UI", "Launch" }, chart.Series.Select(series => series.Name));
        Assert.Equal(new[] { 0, 5, 13, 26 }.Select(day => Start.AddDays(day).ToOADate()), chart.Series.Select(series => series.GanttItems.Single().Start));
        Assert.Equal(new[] { 7, 19, 23, 26 }.Select(day => Start.AddDays(day).ToOADate()), chart.Series.Select(series => series.GanttItems.Single().End));
        Assert.Equal(new[] { 1, .6, .2, 1 }, chart.Series.Select(series => series.GanttItems.Single().Progress));
        Assert.True(chart.Series[3].GanttItems.Single().IsMilestone);
        Assert.Equal(Start.AddDays(11).ToOADate(), chart.Options.GanttToday);
        Assert.Equal(3, chart.GanttDependencies.Count);
        for (var index = 0; index < 3; index++) {
            Assert.Equal(chart.Series[index].Id, chart.GanttDependencies[index].PredecessorId);
            Assert.Equal(chart.Series[index + 1].Id, chart.GanttDependencies[index].SuccessorId);
        }

        var svg = artifact?.ToSvg() ?? chart.ToSvg();
        var document = XDocument.Parse(svg);
        var ticks = TickLabels(document);
        Assert.NotEmpty(ticks);
        Assert.All(ticks, label => Assert.True(DateTime.TryParseExact(label, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), "Expected a calendar tick, got " + label));
        Assert.Equal(TickLabels(XDocument.Parse(expected.ToSvg())), ticks);
        var window = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "gantt-chart");
        Assert.Equal(Start.ToOADate(), Number(window, "data-cfx-min"));
        Assert.Equal(Start.AddDays(26).ToOADate(), Number(window, "data-cfx-max"));
        var marks = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") is "gantt-task" or "gantt-milestone").ToArray();
        Assert.Equal(4, marks.Length);
        Assert.Equal(chart.Series.Select(series => series.GanttItems.Single().Start), marks.Select(node => Number(node, "data-cfx-start")));
        Assert.Equal(chart.Series.Select(series => series.GanttItems.Single().End), marks.Select(node => Number(node, "data-cfx-end")));
        var png = artifact?.ToPng() ?? chart.ToPng();
        Assert.Equal(expected.ToPng(), png);
        var image = PngReader.Decode(png);
        Assert.Equal(900, image.Width);
        Assert.Equal(460, image.Height);
        Capture("markup-gantt-date-" + (fenced ? "fenced-" : "direct-") + (dark ? "dark" : "light"), source, svg, png, ticks);
    }

    [Theory]
    [InlineData("type gantt\nmilestone Ship 2026-10-01", ChartSeriesKind.Gantt)]
    [InlineData("type timeline\nmilestone Ship 2026-10-01", ChartSeriesKind.Timeline)]
    [InlineData("item Work 2026-10-01 2026-10-08", ChartSeriesKind.Timeline)]
    [InlineData("task Work 2026-10-01 2026-10-08", ChartSeriesKind.Gantt)]
    public void EachDatedRowKindSelectsTheNativeTimeAxis(string source, ChartSeriesKind kind) {
        var (chart, _) = Parse(source, false);
        Assert.Equal(ChartScaleKind.Time, chart.Options.XAxis.Scale);
        var series = Assert.Single(chart.Series);
        Assert.Equal(kind, series.Kind);
        Assert.Equal(Start.ToOADate(), kind == ChartSeriesKind.Gantt ? series.GanttItems.Single().Start : series.Points.Single().X);
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericSchedulesRetainTheirNativeAxisAndPixels(bool gantt) {
        var source = "title Numeric\ntype " + (gantt ? "gantt\ntoday 3\ntask Work 1 5 progress=0.5\nmilestone Ship 7 dependsOn=0" : "timeline\nitem Work 1 5\nmilestone Ship 7");
        var (chart, _) = Parse(source, false);
        var native = Chart.Create().WithTitle("Numeric").WithSize(900, 460);
        if (gantt) native.WithGanttToday(3.0).AddGanttTask("Work", 1, 5, .5).AddGanttMilestone("Ship", 7, native.Series[0]);
        else native.AddTimelineRange("Work", 1, 5).AddTimelineRange("Ship", 7, 7);
        Assert.Equal(ChartScaleKind.Linear, chart.Options.XAxis.Scale);
        Assert.Equal(TickLabels(XDocument.Parse(native.ToSvg())), TickLabels(XDocument.Parse(chart.ToSvg())));
        Assert.Equal(native.ToPng(), chart.ToPng());
    }

    [Fact]
    public void MixedCoordinatesRetainSourceFactsWhileADatedRowSelectsTheSharedTimeAxis() {
        var day = Start.ToOADate().ToString(CultureInfo.InvariantCulture);
        var mixed = "type gantt\ntask Mixed " + day + " 2026-10-08";
        Assert.Equal(ChartScaleKind.Linear, Parse(mixed, false).Chart.Options.XAxis.Scale);
        var source = mixed + "\ntask Dated 2026-10-08 2026-10-14 dependsOn=0";
        var (chart, _) = Parse(source, false);
        Assert.Equal(ChartScaleKind.Time, chart.Options.XAxis.Scale);
        Assert.Equal(Start.ToOADate(), chart.Series[0].GanttItems.Single().Start);
        Assert.Equal(Start.AddDays(7).ToOADate(), chart.Series[0].GanttItems.Single().End);
        Assert.Equal(Start.AddDays(13).ToOADate(), chart.Series[1].GanttItems.Single().End);
        Assert.Single(chart.GanttDependencies);
        chart.Options.XAxis.WithBounds(Start.AddDays(2).ToOADate(), Start.AddDays(10).ToOADate())
            .WithLabelFormatter(value => DateTime.FromOADate(value).ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
        var document = XDocument.Parse(chart.ToSvg());
        var window = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "gantt-chart");
        Assert.Equal(Start.AddDays(2).ToOADate(), Number(window, "data-cfx-min"));
        Assert.Equal(Start.AddDays(10).ToOADate(), Number(window, "data-cfx-max"));
        var ticks = TickLabels(document);
        Assert.NotEmpty(ticks);
        Assert.All(ticks, label => Assert.True(DateTime.TryParseExact(label, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)));
        Assert.Equal(Start.ToOADate(), chart.Series[0].GanttItems.Single().Start);
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData("task Work 2026-10-08 2026-10-01", "end must be greater")]
    [InlineData("task Work 2026-10-08 1", "end must be greater")]
    [InlineData("task Work NaN 5", "finite number")]
    [InlineData("task Work 2026-10-01 2026-10-08 dependsOn=0", "earlier zero-based")]
    public void DateInferencePreservesLocatedRangeFiniteAndDependencyDiagnostics(string command, string message) {
        var result = new VisualMarkupParser().Parse("# Invalid schedule\n\n```chartforgex timeline v1 {type=gantt}\n" + command + "\n```");
        Assert.True(result.HasErrors);
        Assert.Empty(result.Artifacts);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Line == 4 && diagnostic.Severity == MarkupDiagnosticSeverity.Error
            && diagnostic.Message.Contains(message, StringComparison.Ordinal));
    }

    private static (Chart Chart, VisualArtifact? Artifact) Parse(string source, bool fenced) {
        if (fenced) {
            var parsed = new VisualMarkupParser().Parse(source);
            Assert.Empty(parsed.Diagnostics);
            var artifact = Assert.Single(parsed.Artifacts);
            Assert.Equal(VisualArtifactKind.Timeline, artifact.Kind);
            Assert.Equal("3", artifact.Metadata["sourceLine"]);
            return (Assert.IsType<Chart>(artifact.Model), artifact);
        }
        var direct = new MarkupTimelineParser().Parse(source);
        Assert.Empty(direct.Diagnostics);
        return (Assert.IsType<MarkupChartDocument>(direct.Document).Chart, null);
    }

    private static Chart NativeLaunch() {
        var chart = Chart.Create().WithTitle("Launch plan").WithSize(900, 460).WithGanttToday(Start.AddDays(11));
        chart.AddGanttTask("Design", Start, Start.AddDays(7), 1);
        chart.AddGanttTask("Build engine", Start.AddDays(5), Start.AddDays(19), .6, chart.Series[0]);
        chart.AddGanttTask("Playground UI", Start.AddDays(13), Start.AddDays(23), .2, chart.Series[1]);
        return chart.AddGanttMilestone("Launch", Start.AddDays(26), chart.Series[2]);
    }

    private static string[] TickLabels(XDocument document) => document.Descendants()
        .Where(node => (string?)node.Attribute("data-cfx-role") == "schedule-tick-label").Select(node => node.Value).ToArray();
    private static double Number(XElement node, string attribute) => double.Parse(node.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);

    private static void Capture(string name, string source, string svg, byte[] png, string[] ticks) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".source.txt"), source);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), png);
        File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new { ticks, width = 900, height = 460 }));
    }
}
