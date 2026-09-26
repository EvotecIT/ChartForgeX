using System.Xml.Linq;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NeutralHeatmapScaleTests {
    [Fact]
    public void SequentialHeatmap_CellsAndScaleCarryLevelsNotStatus() {
        var chart = Chart.Create().WithSize(640, 320).AddHeatmapRow("Logons", new[] { 2d, 40d, 90d, 400d });
        var svg = XDocument.Parse(chart.ToSvg());
        var cells = ByRole(svg, "heatmap-cell");
        Assert.All(cells, cell => Assert.Null(cell.Attribute("data-cfx-status")));
        Assert.Equal(new[] { "0", "1", "1", "4" }, cells.Select(cell => (string)cell.Attribute("data-cfx-level")!).ToArray());
        var steps = ByRole(svg, "heatmap-scale-step");
        Assert.NotEmpty(steps);
        Assert.All(steps, step => Assert.Null(step.Attribute("data-cfx-status")));
        Assert.Equal("0", (string?)steps[0].Attribute("data-cfx-level"));
        Assert.Equal("4", (string?)steps[steps.Length - 1].Attribute("data-cfx-level"));
    }

    [Fact]
    public void SemanticHeatmap_KeepsStatusForStatusData() {
        var chart = Chart.Create().WithSize(640, 320).WithHeatmapScale(ChartHeatmapScale.Semantic).AddHeatmapRow("Pass rate", new[] { 20d, 70d, 95d });
        var cells = ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-cell");
        Assert.Equal(new[] { "negative", "warning", "positive" }, cells.Select(cell => (string)cell.Attribute("data-cfx-status")!).ToArray());
        Assert.All(cells, cell => Assert.Null(cell.Attribute("data-cfx-level")));
    }

    [Fact]
    public void RelativeScale_RejectsSemanticStatusColours() {
        var chart = Chart.Create().WithHeatmapScale(ChartHeatmapScale.Semantic).AddHeatmapRow("Failures", new[] { 3d, 9d });
        chart.Options.HeatmapRelativeScale = true;
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        Assert.Throws<InvalidOperationException>(() => chart.ToPng());
    }

    [Fact]
    public void HexbinHeatmap_CellsCarryLevelsNotStatus() {
        var chart = Chart.Create().WithSize(640, 320).AddHexbinHeatmapRow("Density", new[] { 1d, 50d, 100d });
        var cells = ByRole(XDocument.Parse(chart.ToSvg()), "hexbin-cell");
        Assert.All(cells, cell => Assert.Null(cell.Attribute("data-cfx-status")));
        Assert.Equal("4", (string?)cells[cells.Length - 1].Attribute("data-cfx-level"));
    }

    [Fact]
    public void CalendarHeatmap_UsesNeutralRampAndNoStatusForValues() {
        var start = new DateTime(2026, 1, 5);
        var chart = Chart.Create().WithSize(720, 260).AddCalendarHeatmap("Commits", Enumerable.Range(0, 20).Select(day => new ChartCalendarHeatmapItem(start.AddDays(day), day % 5)));
        var svg = XDocument.Parse(chart.ToSvg());
        var valued = ByRole(svg, "calendar-heatmap-cell").Where(cell => (string?)cell.Attribute("data-cfx-empty") == "false").ToArray();
        Assert.All(valued, cell => Assert.Null(cell.Attribute("data-cfx-status")));
        Assert.All(ByRole(svg, "calendar-heatmap-scale-step"), step => Assert.Null(step.Attribute("data-cfx-status")));
        var strongest = valued.First(cell => (string?)cell.Attribute("data-cfx-level") == "4");
        Assert.NotEqual(chart.Options.Theme.Positive.ToCss(), (string)strongest.Attribute("fill")!);
    }

    [Fact]
    public void LocalizedLevelLabel_IsEmittedOnlyWhenChangedAndReadByTooltips() {
        Chart Create() => Chart.Create().WithSize(640, 320).AddHeatmapRow("Logons", new[] { 2d, 40d, 90d, 400d });
        Assert.Null(ByRole(XDocument.Parse(Create().ToSvg()), "heatmap").Single().Attribute("data-cfx-label-level"));
        var localized = Create().WithLabels(labels => labels.Level = "Poziom");
        Assert.Equal("Poziom", (string?)ByRole(XDocument.Parse(localized.ToSvg()), "heatmap").Single().Attribute("data-cfx-label-level"));
        var calendar = Chart.Create().WithLabels(labels => labels.Level = "Poziom").AddCalendarHeatmap("Days", new[] { new ChartCalendarHeatmapItem(new DateTime(2026, 9, 1), 3) });
        Assert.Equal("Poziom", (string?)ByRole(XDocument.Parse(calendar.ToSvg()), "calendar-heatmap").Single().Attribute("data-cfx-label-level"));
        var script = ChartForgeX.Interactivity.Html.HtmlInteractiveChartRenderer.BuildInteractionScript();
        Assert.Contains("push(rowName(node, 'level', 'Level'), data.cfxLevel);", script, StringComparison.Ordinal);
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
