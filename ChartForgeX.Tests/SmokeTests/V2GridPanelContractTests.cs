using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects model visibility and common-context permission precedence in prepared comparisons.</summary>
public sealed class V2GridPanelContractTests {
    [Fact]
    public void DefaultGridExportsRetainDetachedHostColorVariablesWithoutChangingNativePixels() {
        var grid = ChartGrid.Create().Add(Sample("Colors"));
        var request = VisualExportRequest.ForGrid(grid);
        var before = grid.Prepare(request.Context).ToRgba().Pixels;
        var background = request.Context.Theme.Resolve(request.Context.ThemeMode).Background;
        grid.WithSvgColorVariables(new SvgColorVariables().Add("--report-background", background, SvgColorRole.Surface));
        var prepared = grid.Prepare(request.Context);
        Assert.Contains("var(--report-background", prepared.ToSvg());
        Assert.Contains("var(--report-background", grid.ToSvg());
        Assert.Contains("var(--report-background", new ChartForgeX.Html.HtmlChartGridRenderer().RenderFragment(grid));
        Assert.Equal(before, prepared.ToRgba().Pixels);
        grid.WithSvgColorVariables(null);
        Assert.Contains("var(--report-background", prepared.ToSvg());
    }

    [Fact]
    public void DefaultGridKeepsVisibleLegendsAndEachPanelsOwnHiddenHeaderAndSurfaces() {
        var shown = Sample("Visible").WithHeader().WithLegend().WithCard().WithPlotBackground();
        var hidden = Sample("Hidden").WithHeader(false).WithLegend(false).WithCard(false).WithPlotBackground(false);
        var grid = ChartGrid.Create().WithColumns(2).WithPanelSize(360, 260).Add(shown).Add(hidden);
        var request = VisualExportRequest.ForGrid(grid);
        var prepared = grid.Prepare(request.Context);
        var document = XDocument.Parse(prepared.ToSvg());
        var visiblePanel = Panel(document, 0); var hiddenPanel = Panel(document, 1);
        Assert.Contains(visiblePanel.Descendants(), element => Role(element) == "frame-heading" && element.Value == "Visible");
        Assert.Contains(visiblePanel.Descendants(), element => Role(element) == "legend-entry");
        Assert.Contains(visiblePanel.Descendants(), element => Role(element) == "content-surface");
        Assert.Contains(visiblePanel.Descendants(), element => Role(element) == "frame-card");
        Assert.DoesNotContain(hiddenPanel.Descendants(), element => Role(element) is "frame-heading" or "legend-entry" or "content-surface" or "frame-card");
        Assert.Contains(prepared.Regions, region => region.Role == "panel" && region.Label == "Hidden");
        Assert.False(hidden.Options.ShowHeader); Assert.False(hidden.Options.ShowLegend); Assert.False(hidden.Options.ShowCard);
    }

    [Fact]
    public void ExplicitContextHidesPanelFeaturesAndCapsModelLegendRowsWithoutChangingTheModels() {
        var chart = Sample("Panel").WithHeader(false).WithLegend().WithPlotBackground();
        for (var i = 0; i < 6; i++) chart.AddLine("Extra " + i, new[] { new ChartPoint(0, i), new ChartPoint(1, i + 1) });
        chart.Options.LegendMaximumRows = 3;
        var grid = ChartGrid.Create().WithColumns(1).Add(chart);
        var layout = new VisualLayoutOptions(new VisualSize(400, 300), 8);
        var capped = grid.Prepare(new VisualRenderContext(layout, frame: new VisualFrame(showLegend: true,
            legendMaximumRows: 1, legendMaximumHeightFraction: .5)));
        Assert.Single(Panel(XDocument.Parse(capped.ToSvg()), 0).Descendants(), element => Role(element) == "legend-entry");
        Assert.Contains(capped.Diagnostics, diagnostic => diagnostic.Code == "frame.legend-overflow");
        var hidden = grid.Prepare(new VisualRenderContext(layout, frame: new VisualFrame(showLegend: false, showSurface: false, showCard: false)));
        Assert.DoesNotContain(Panel(XDocument.Parse(hidden.ToSvg()), 0).Descendants(), element => Role(element) is "legend-entry" or "content-surface");
        Assert.True(chart.Options.ShowLegend); Assert.True(chart.Options.ShowPlotBackground); Assert.Equal(3, chart.Options.LegendMaximumRows);
    }

    [Fact]
    public void HostFrameOptOutAndAsymmetricModelPaddingSurviveGridEmbedding() {
        var padded = Sample("Padded").WithHeader(false).WithLegend(false).WithCard(false).WithPlotBackground();
        padded.Options.Padding = new ChartPadding(11, 17, 29, 37);
        var owned = Sample("Host owns frame").WithHostFrame();
        var grid = ChartGrid.Create().WithColumns(2).WithPadding(6).WithPanelSize(360, 260).Add(padded).Add(owned);
        var document = XDocument.Parse(grid.ToSvg());
        var plot = Panel(document, 0).Descendants().Single(element => Role(element) == "content-surface");
        Assert.Equal("11", plot.Attribute("x")!.Value); Assert.Equal("17", plot.Attribute("y")!.Value);
        Assert.Equal("320", plot.Attribute("width")!.Value); Assert.Equal("206", plot.Attribute("height")!.Value);
        Assert.DoesNotContain(Panel(document, 1).Descendants(), element => Role(element) is "frame-heading" or "legend-entry" or "content-surface" or "frame-card");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void SameSizedPanelKeepsStandaloneNativeDensityAndModelPadding(int scale) {
        var chart = Sample("Source").WithHeader(false).WithLegend(false).WithCard(false).WithPlotBackground(false).WithTransparentBackground();
        chart.Options.Padding = ChartPadding.All(8);
        var chartRequest = VisualExportRequest.ForChart(chart);
        var isolated = chart.Prepare(chartRequest.Context);
        var grid = ChartGrid.Create().WithColumns(1).WithPadding(0).WithGap(0).WithPanelSize(360, 260).Add(chart);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 260), 0), chartRequest.Context.Theme,
            frame: new VisualFrame(showLegend: false, transparentBackground: true, showCard: false), font: chartRequest.Context.Font);
        var prepared = grid.Prepare(context);
        var raster = new VisualRenderOptions(scale, chartRequest.RasterOptions.Supersampling, textHinting: chartRequest.RasterOptions.TextHinting);
        Assert.Equal(isolated.ToRgba(raster).Pixels, prepared.ToRgba(raster).Pixels);
        Assert.Equal(360 * scale, prepared.Size.Width * scale);
    }

    [Fact]
    public void NaturalHeightMeasuresWrappedHeadingsAndKeepsTheRequestedPanelHeight() {
        var grid = ChartGrid.Create().WithColumns(1).WithPadding(12).WithPanelSize(360, 260)
            .WithTitle("A report heading that deliberately wraps across the available width")
            .ConfigureTitleStyle(style => style.WithFontSize(34)).Add(Sample("Panel"));
        var prepared = grid.Prepare(VisualExportRequest.ForGrid(grid).Context);
        var panel = Assert.Single(prepared.Regions, region => region.Role == "panel");
        Assert.Equal(260, panel.Bounds.Height, 7);
        Assert.True(panel.Bounds.Top > 12 + 34);
        Assert.Equal(panel.Bounds.Top + 260 + 12, prepared.Size.Height, 7);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "frame.heading-overflow");
    }

    private static Chart Sample(string title) => Chart.Create().WithTitle(title).WithSize(360, 260)
        .AddLine("Values", new[] { new ChartPoint(0, 1), new ChartPoint(1, 4), new ChartPoint(2, 2) });
    private static XElement Panel(XDocument document, int index) => document.Descendants().Single(element => Role(element) == "panel"
        && (string?)element.Attribute("data-cfx-source-id") == "panel-" + index);
    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
}
