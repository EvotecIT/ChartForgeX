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
        var chart = Sample().WithPngOutputScale(2).WithTransparentBackground(false);
        var chartRequest = VisualExportRequest.ForChart(chart);
        var chartPixels = chart.Prepare(chartRequest.Context).ToRgba(chartRequest.RasterOptions);
        var actualChart = ImageComposition.Create(340, 230, ChartColor.White)
            .DrawChart(chart, 11, 13, 300, 180, VisualCanvasImageFit.Contain, .65).ToImage();
        var expectedChart = ImageComposition.Create(340, 230, ChartColor.White)
            .DrawImage(chartPixels, 11, 13, 300, 180, VisualCanvasImageFit.Contain, .65).ToImage();
        Assert.Equal(expectedChart.Pixels, actualChart.Pixels);
        var grid = ChartGrid.Create().WithColumns(2).WithPanelSize(180, 130).WithPadding(8)
            .WithPngOutputScale(2).Add(chart).Add(Sample());
        var gridRequest = VisualExportRequest.ForGrid(grid);
        var gridPixels = grid.Prepare(gridRequest.Context).ToRgba(gridRequest.RasterOptions);
        var actualGrid = ImageComposition.CreateTransparent(410, 180)
            .DrawChartGrid(grid, 5, 7, 390, 160, VisualCanvasImageFit.Stretch, .8).ToImage();
        var expectedGrid = ImageComposition.CreateTransparent(410, 180)
            .DrawImage(gridPixels, 5, 7, 390, 160, VisualCanvasImageFit.Stretch, .8).ToImage();
        Assert.Equal(expectedGrid.Pixels, actualGrid.Pixels);
        Assert.Equal(2, chart.Options.PngOutputScale);
        Assert.False(chart.Options.TransparentBackground);
    }

    [Fact]
    public void StaticHtmlHostsEmbedPreparedGeometryAndKeepArbitraryHostScopesDistinct() {
        var chart = Sample().WithTitle("<Revenue & cost>");
        var chartPrepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var chartHtml = new HtmlChartRenderer().RenderFragment(chart);
        Assert.Contains(chartPrepared.ToSvg(), chartHtml);
        Assert.DoesNotContain("<script", new HtmlChartRenderer().RenderPage(chart), StringComparison.OrdinalIgnoreCase);
        var grid = ChartGrid.Create().WithTitle("Shared comparison").WithColumns(2).Add(chart).Add(chart);
        grid.Accessibility.Language = "pl";
        var prepared = grid.Prepare(VisualExportRequest.ForGrid(grid).Context);
        var renderer = new HtmlChartGridRenderer();
        Assert.Contains(prepared.ToSvg(), renderer.RenderFragment(grid));
        Assert.Contains("lang=\"pl\"", renderer.RenderPage(grid));
        var a = EmbeddedSvg(renderer.RenderFragment(grid, "scope with spaces"));
        var b = EmbeddedSvg(renderer.RenderFragment(grid, "scope|with|spaces"));
        var ids = a.DescendantsAndSelf().Concat(b.DescendantsAndSelf()).Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.NotEmpty(ids); Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Single(a.DescendantsAndSelf(), element => element.Name.LocalName == "svg");
        Assert.Equal(prepared.ToRgba().Pixels, grid.ToRgbaImage().Pixels);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VisualGridHostsSharePreparedChartSourcesWithoutMutatingTransparency(bool dark) {
        var chart = Sample().WithTheme(dark ? ChartTheme.ReportDark() : ChartTheme.ReportLight());
        chart.WithTransparentBackground(false).WithPngOutputScale(2);
        var grid = VisualGrid.Create().WithPadding(0).WithGap(0).WithPanelSize(240, 160).Add(chart);
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
        Assert.Equal(2, chart.Options.PngOutputScale);
        Assert.Equal("Source", chart.Title);
    }

    private static Chart Sample() => Chart.Create().WithTitle("Source").WithSize(240, 160).WithPadding(8, 8, 8, 8).WithLegend(false)
        .AddLine("Samples", new[] { new ChartPoint(0, 1), new ChartPoint(1, 4), new ChartPoint(2, 2) });
    private static XElement EmbeddedSvg(string html) => XDocument.Parse(html).Descendants().Single(element => element.Name.LocalName == "svg");
}
