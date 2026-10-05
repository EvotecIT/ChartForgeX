using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteRenderingTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void InsideBarLabelsHaveReadablePairedInkAndRespectExplicitColours(bool dark, bool semantic) {
        var tokens = dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight();
        var chart = Chart.Create().WithDesignTokens(tokens).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside)
            .AddBar("Counts", new[] { new ChartPoint(1, 80), new ChartPoint(2, 60) });
        if (semantic) chart.WithSeriesState("Counts", ChartSeriesState.Danger);
        var document = XDocument.Parse(chart.ToSvg());
        var labels = document.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "data-label" && (string?)e.Attribute("display") != "none").ToArray();
        Assert.Equal(2, labels.Length);
        foreach (var label in labels) {
            var mark = document.Descendants().Single(e => (string?)e.Attribute("data-cfx-mark-key") == (string?)label.Attribute("data-cfx-label-mark"));
            Assert.True(ChartColor.TryParse((string)label.Attribute("fill")!, out var ink));
            Assert.True(ChartColor.TryParse((string)mark.Attribute("fill")!, out var fill));
            Assert.True(ChartColorMath.ContrastRatio(ink, fill) >= 4.5, label + " on " + mark);
        }
        var literalPng = chart.ToPng();
        chart.WithSvgColorVariables(tokens.ToSvgColorVariables());
        Assert.Contains(semantic ? "mark-danger-ink" : "series-1-ink", chart.ToSvg());
        Assert.Equal(literalPng, chart.ToPng());
        var image = chart.ToRgbaImage();
        var expected = ChartColorMath.AccessibleTextOnBackground(semantic ? chart.Options.Theme.Negative : chart.Options.Theme.Palette[0]);
        foreach (var label in labels) {
            double Read(string key) => double.Parse(label.Attribute("data-cfx-label-" + key)!.Value, System.Globalization.CultureInfo.InvariantCulture);
            var pixels = Enumerable.Range((int)Math.Floor(Read("y")), (int)Math.Ceiling(Read("height")) + 1)
                .SelectMany(y => Enumerable.Range((int)Math.Floor(Read("x")), (int)Math.Ceiling(Read("width")) + 1).Select(x => y * image.Width + x));
            var closest = pixels.Min(p => Math.Abs(image.Pixels[p*4]-expected.R) + Math.Abs(image.Pixels[p*4+1]-expected.G) + Math.Abs(image.Pixels[p*4+2]-expected.B));
            var background = semantic ? chart.Options.Theme.Negative : chart.Options.Theme.Palette[0];
            var distance = Math.Abs(background.R-expected.R) + Math.Abs(background.G-expected.G) + Math.Abs(background.B-expected.B);
            // Thin glyphs can be antialiased at 1x; require a pixel at least 75% towards the intended ink.
            Assert.True(closest <= distance * .25, image.Width + "x" + image.Height + ", closest " + closest + ", " + label);
        }
        chart.WithSvgColorVariables(null);
        chart.Series[0].DataLabelStyle.Color = ChartColor.FromHex("#C2418A");
        Assert.All(XDocument.Parse(chart.ToSvg()).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "data-label"),
            e => Assert.Equal("#C2418A", (string?)e.Attribute("fill")));
    }

    [Theory]
    [InlineData(false, "bar")]
    [InlineData(true, "bar")]
    [InlineData(false, "horizontal")]
    [InlineData(true, "horizontal")]
    [InlineData(false, "line")]
    [InlineData(true, "area")]
    public void DenseValueLabelsFitTheirActivePlotClipInBothRendererScenes(bool dark, string kind) {
        var chart = Chart.Create().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithSize(300, 220)
            .WithTitle("Findings by severity").WithSubtitle("Current and previous run").WithXLabels("Critical", "High", "Medium", "Low", "Info").WithDataLabels();
        var first = new[] { 6d, 30, 82, 124, 208 }.Select((v,i) => new ChartPoint(i+1,v)).ToArray();
        var second = new[] { 9d, 39, 95, 116, 186 }.Select((v,i) => new ChartPoint(i+1,v)).ToArray();
        if (kind == "horizontal") chart.AddHorizontalBar("Current", first).AddHorizontalBar("Previous", second);
        else if (kind == "line") chart.AddLine("Current", first).AddLine("Previous", second);
        else if (kind == "area") chart.AddArea("Current", first).AddArea("Previous", second);
        else chart.AddBar("Current", first).AddBar("Previous", second);
        chart.Options.YAxis.Maximum = 250;
        chart.Options.YAxis.TickCount = 6;
        foreach (var svg in new[] { chart.ToSvg(), new SvgChartRenderer().RenderLabelScene(chart).ToSvg() }) {
            var document = XDocument.Parse(svg);
            var rect = document.Descendants().First(e => e.Name.LocalName == "clipPath").Elements().Single();
            double Read(XElement e, string key) => double.Parse(e.Attribute(key)!.Value, System.Globalization.CultureInfo.InvariantCulture);
            var left = Read(rect, "x"); var top = Read(rect, "y"); var right = left + Read(rect, "width"); var bottom = top + Read(rect, "height");
            var labels = document.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "data-label" && (string?)e.Attribute("display") != "none").ToArray();
            Assert.NotEmpty(labels);
            foreach (var label in labels) {
                if (!label.Ancestors().Any(e => e.Attribute("clip-path") != null)) continue;
                Assert.True(Read(label, "data-cfx-label-x") >= left - .002);
                Assert.True(Read(label, "data-cfx-label-y") >= top - .002);
                Assert.True(Read(label, "data-cfx-label-x") + Read(label, "data-cfx-label-width") <= right + .002);
                Assert.True(Read(label, "data-cfx-label-y") + Read(label, "data-cfx-label-height") <= bottom + .002);
            }
        }
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    public void AxisFontStacksSurviveXmlEncodingOnce(double rotation) {
        const string family = "Calibri, \"Segoe UI\", \"Sample & Family\", sans-serif";
        var chart = Chart.Create().WithSize(600, 360).WithXLabels("One", "Two").WithDataLabels()
            .AddBar("Counts", new[] { new ChartPoint(1, 12), new ChartPoint(2, 24) });
        chart.Options.Theme.FontFamily = family;
        chart.Options.XAxis.LabelAngle = rotation;
        var text = XDocument.Parse(chart.ToSvg()).Descendants().Where(e => e.Name.LocalName == "text").ToArray();
        Assert.Contains(text, e => (string?)e.Attribute("data-cfx-role") == "x-axis-label");
        Assert.Contains(text, e => (string?)e.Attribute("data-cfx-role") == "y-axis-label");
        Assert.All(text.Where(e => e.Attribute("font-family") != null),
            e => Assert.Equal(family, (string?)e.Attribute("font-family")));
        Assert.NotEmpty(chart.ToPng());
    }

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
        var hosted = renderer.Render(chart);
        Assert.Empty(Roles(hosted, "card-surface"));
        Assert.Equal("placed", (string?)Assert.Single(Roles(hosted, "chart-title")).Attribute("data-cfx-label-status"));
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
