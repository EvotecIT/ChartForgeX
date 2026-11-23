using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects scene ownership at raw composition and static host boundaries.</summary>
public sealed class V2EmbeddingTests {
    [Fact]
    public void ImageCompositionEmbedsTheSamePreparedChartAndGridPixelsWithFitAndOpacity() {
        var chart = Sample().Png(p => p.OutputScale(2)).TransparentBackground(false);
        var chartRequest = VisualExportRequest.ForChart(chart);
        var chartPixels = chart.Prepare(chartRequest.Context).ToRgbaImage(chartRequest.RasterOptions);
        var actualChart = ImageComposition.Create(340, 230, ChartColor.White)
            .DrawChart(chart, 11, 13, 300, 180, VisualCanvasImageFit.Contain, .65).ToRgbaImage();
        var expectedChart = ImageComposition.Create(340, 230, ChartColor.White)
            .DrawImage(chartPixels, 11, 13, 300, 180, VisualCanvasImageFit.Contain, .65).ToRgbaImage();
        Assert.Equal(expectedChart.Pixels, actualChart.Pixels);
        var grid = ChartGrid.Create().Columns(2).PanelSize(180, 130).Padding(8)
            .PngOutputScale(2).Add(chart).Add(Sample());
        var gridRequest = VisualExportRequest.ForGrid(grid);
        var gridPixels = grid.Prepare(gridRequest.Context).ToRgbaImage(gridRequest.RasterOptions);
        var actualGrid = ImageComposition.CreateTransparent(410, 180)
            .DrawChartGrid(grid, 5, 7, 390, 160, VisualCanvasImageFit.Stretch, .8).ToRgbaImage();
        var expectedGrid = ImageComposition.CreateTransparent(410, 180)
            .DrawImage(gridPixels, 5, 7, 390, 160, VisualCanvasImageFit.Stretch, .8).ToRgbaImage();
        Assert.Equal(expectedGrid.Pixels, actualGrid.Pixels);
        Assert.Equal(2, chart.Options.Png.OutputScale);
        Assert.False(chart.Options.TransparentBackground);
    }

    [Fact]
    public void StaticHtmlHostsEmbedPreparedGeometryAndKeepArbitraryHostScopesDistinct() {
        // An explicit mode embeds the literal prepared SVG; Auto writes it with scoped properties (AutoThemeHtmlTests).
        var chart = Sample().Title("<Revenue & cost>").ThemeMode(ThemeMode.Light);
        var chartPrepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var chartHtml = new HtmlChartRenderer().RenderFragment(chart);
        Assert.Contains(chartPrepared.ToSvg(new VisualSvgOptions(colorVariables: chart.Options.SvgColorVariables, responsive: true)), chartHtml);
        var chartSvg = EmbeddedSvg(chartHtml);
        Assert.Equal("max-width:100%;height:auto;display:block", chartSvg.Attribute("style")?.Value);
        Assert.Equal(chartPrepared.Size.Width.ToString(System.Globalization.CultureInfo.InvariantCulture), chartSvg.Attribute("width")?.Value);
        Assert.Null(XDocument.Parse(chartPrepared.ToSvg()).Root!.Attribute("style"));
        Assert.DoesNotContain("<script", new HtmlChartRenderer().RenderPage(chart), StringComparison.OrdinalIgnoreCase);
        var grid = ChartGrid.Create().Title("Shared comparison").ThemeMode(ThemeMode.Light).Columns(2).Add(chart).Add(chart);
        grid.Accessibility.Language = "pl";
        var prepared = grid.Prepare(VisualExportRequest.ForGrid(grid).Context);
        var renderer = new HtmlChartGridRenderer();
        var gridHtml = renderer.RenderFragment(grid);
        Assert.Contains(prepared.ToSvg(new VisualSvgOptions(colorVariables: grid.SvgColorVariables, responsive: true)), gridHtml);
        Assert.Equal("max-width:100%;height:auto;display:block", EmbeddedSvg(gridHtml).Attribute("style")?.Value);
        Assert.Contains("lang=\"pl\"", renderer.RenderPage(grid));
        var a = EmbeddedSvg(renderer.RenderFragment(grid, "scope with spaces"));
        var b = EmbeddedSvg(renderer.RenderFragment(grid, "scope|with|spaces"));
        var ids = a.DescendantsAndSelf().Concat(b.DescendantsAndSelf()).Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.NotEmpty(ids); Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Single(a.DescendantsAndSelf(), element => element.Name.LocalName == "svg");
        Assert.Equal(prepared.ToRgbaImage().Pixels, grid.ToRgbaImage().Pixels);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VisualGridHostsSharePreparedChartSourcesWithoutMutatingTransparency(bool dark) {
        var chart = Sample().Theme(VisualTheme.Graphite()).ThemeMode(dark ? ThemeMode.Dark : ThemeMode.Light);
        chart.TransparentBackground(false).Png(p => p.OutputScale(2));
        var grid = VisualGrid.Create().Padding(0).Gap(0).PanelSize(240, 160).Add(chart);
        var svg = new SvgVisualGridRenderer().Render(grid);
        var html = new HtmlVisualGridRenderer().RenderFragment(grid, "host scope");
        var pixels = grid.ToRgbaImage();
        Assert.Contains("data-cfx-series=\"0\"", svg);
        Assert.Contains("data-cfx-series=\"0\"", html);
        Assert.DoesNotContain(XDocument.Parse(svg).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "background");
        Assert.DoesNotContain(XDocument.Parse(html).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "background");
        Assert.Equal(240, pixels.Width); Assert.Equal(160, pixels.Height);
        Assert.Contains(pixels.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
        Assert.False(chart.Options.TransparentBackground);
        Assert.Equal(2, chart.Options.Png.OutputScale);
        Assert.Equal("Source", chart.Title);
    }

    private static Chart Sample() => Chart.Create().Title("Source").Size(240, 160).Padding(8, 8, 8, 8).Legend(false)
        .AddLine("Samples", new[] { new ChartPoint(0, 1), new ChartPoint(1, 4), new ChartPoint(2, 2) });
    private static XElement EmbeddedSvg(string html) => XDocument.Parse(html).Descendants().Single(element => element.Name.LocalName == "svg");
}
