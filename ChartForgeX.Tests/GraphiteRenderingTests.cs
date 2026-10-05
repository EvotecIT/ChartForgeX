using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteRenderingTests {
    [Theory]
    [InlineData(220)]
    [InlineData(260)]
    [InlineData(300)]
    public void CompactInlineLegendsRetainShortSeriesNames(int width) {
        var chart=Chart.Create().WithSize(width,320).WithTitle("Panel").WithSubtitle("Current status")
            .AddLine("Passed",new[]{new ChartPoint(1,100),new ChartPoint(2,110)})
            .AddLine("Warnings",new[]{new ChartPoint(1,10),new ChartPoint(2,12)})
            .AddLine("Failed",new[]{new ChartPoint(1,1),new ChartPoint(2,2)});
        var nodes=XDocument.Parse(chart.ToSvg()).Descendants().ToArray();
        Assert.Equal(3,nodes.Count(e=>(string?)e.Attribute("data-cfx-role")=="legend-item"));
        Assert.DoesNotContain(nodes,e=>(string?)e.Attribute("data-cfx-role")=="legend-overflow");
        Assert.Equal(chart.Series.Select(s=>s.Name),nodes.Where(e=>(string?)e.Attribute("data-cfx-role")=="legend-label").Select(e=>e.Value));
        Assert.All(nodes.Where(e=>(string?)e.Attribute("data-cfx-role")=="legend-label"),e=>Assert.Equal(chart.Options.Theme.LegendFontSize,(double)e.Attribute("font-size")!,2));
    }

    [Theory]
    [InlineData("timeline")]
    [InlineData("gantt")]
    public void TemporalChartsUseOnlyHorizontalGuidesAndMutedRowLabels(string kind) {
        var chart=Chart.Create().WithSize(600,360);
        if(kind=="gantt") chart.AddGanttTask("Plan",1,3).AddGanttTask("Build",3,7);
        else chart.AddTimelineRange("Plan",1,3).AddTimelineRange("Build",3,7);
        var nodes=XDocument.Parse(chart.ToSvg()).Descendants().ToArray();
        Assert.All(nodes.Where(e=>e.Name.LocalName=="line"),e=>Assert.Equal((string?)e.Attribute("y1"),(string?)e.Attribute("y2")));
        Assert.All(nodes.Where(e=>(string?)e.Attribute("data-cfx-role")==kind+"-row-label"),e=> {
            Assert.Equal("400",(string?)e.Attribute("font-weight"));
            Assert.Equal(chart.Options.Theme.MutedText.ToCss(),(string?)e.Attribute("fill"));
        });
        Assert.NotEmpty(chart.ToPng());
    }
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
