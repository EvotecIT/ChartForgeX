using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteRenderingTests {
    [Fact]
    public void StaticHtmlSurfacesAndPanelTitlesFollowGraphite() {
        var chart = Chart.Create().WithTitle("Panel").AddBar("Counts", new[] { new ChartPoint(1, 2) });
        var table = ChartForgeX.VisualBlocks.ChartTable.Create().WithTitle("Table").AddColumn("Name").AddRow("Count");
        var grid = ChartGrid.Create().Add(chart);
        var visualGrid = ChartForgeX.VisualBlocks.VisualGrid.Create().Add(chart).Add(table);
        foreach (var html in new[] { chart.ToHtmlPage(), table.ToHtmlPage(), grid.ToHtmlPage(), visualGrid.ToHtmlPage() }) {
            Assert.DoesNotContain("linear-gradient(", html);
            Assert.DoesNotContain("radialGradient", html);
        }
        Assert.Contains("font-size=\"15\"", grid.ToSvg());
        Assert.Contains("font-size=\"15\"", visualGrid.ToSvg());
        Assert.Equal(17, chart.Options.Theme.TitleFontSize);
        Assert.Equal(17, table.Options.Theme.TitleFontSize);
    }
    private static XElement[] Roles(string svg, string role) => XDocument.Parse(svg).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == role).ToArray();

    [Fact]
    public void DefaultFrameIsSingleFlatSurfaceAndHostCanOwnIt() {
        var chart = Chart.Create().WithTitle("Readiness").AddBar("Checks", new[] { new ChartPoint(0, 8), new ChartPoint(1, 12) });
        var renderer = new SvgChartRenderer();
        var svg = renderer.Render(chart);
        Assert.True(chart.Options.Theme.UseGraphiteLayout);
        Assert.Single(Roles(svg, "card-surface"));
        Assert.Empty(Roles(svg, "card-inner-highlight"));
        Assert.DoesNotContain("linearGradient", svg);
        Assert.All(Roles(svg, "bar"), e => { Assert.Equal("path", e.Name.LocalName); Assert.Contains(" Q ", (string)e.Attribute("d")!); });
        chart.WithHostFrame();
        Assert.Equal(0, chart.Options.Padding.Left);
        Assert.Empty(Roles(renderer.Render(chart), "card-surface"));
    }

    [Fact]
    public void StateLinesUseSemanticPaintAndHealthySeriesIsUnderneath() {
        var chart = Chart.Create().WithTitle("Results").AddLine("Failures", new[] { new ChartPoint(0, 3), new ChartPoint(1, 4), new ChartPoint(2, 2) }).AddLine("Healthy", new[] { new ChartPoint(0, 8), new ChartPoint(1, 9), new ChartPoint(2, 10) })
            .WithSeriesState("Failures", ChartSeriesState.Danger).WithSeriesState("Healthy", ChartSeriesState.Quiet);
        var svg = new SvgChartRenderer().Render(chart);
        var lines = Roles(svg, "line");
        Assert.Equal("1", (string?)lines[0].Attribute("data-cfx-series"));
        Assert.Equal(chart.Options.Theme.QuietLine.ToCss(), (string?)lines[0].Attribute("stroke"));
        Assert.All(lines, e => Assert.Equal("2", (string?)e.Attribute("stroke-width")));
        Assert.Equal(2, Roles(svg, "line-marker").Length);
        chart.WithLineMarkers(ChartLineMarkerMode.All);
        Assert.Equal(6, Roles(new SvgChartRenderer().Render(chart), "line-marker").Length);
        chart.Options.Theme = ChartTheme.ReportLight();
        Assert.Equal(6, Roles(new SvgChartRenderer().Render(chart), "line-marker").Length);
    }
}
