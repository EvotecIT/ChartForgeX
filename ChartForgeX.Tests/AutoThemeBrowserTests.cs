using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class AutoThemeBrowserTests {
    private static Chart Line(ThemeMode mode) => Chart.Create().Size(480, 300).ThemeMode(mode).Title("Orders")
        .AddLine("Orders", new[] { new ChartPoint(1, 3), new ChartPoint(2, 5), new ChartPoint(3, 4) });

    private static TopologyChart Topology(ThemeMode mode) => TopologyChart.Create().Theme(VisualTheme.Graphite()).ThemeMode(mode).Viewport(new TopologyViewport(640, 240, 20))
        .AddNode("a", "API", 80, 100).AddNode("b", "Worker", 340, 100).AddEdge("ab", "a", "b", "calls");

    private static string Page(ThemeMode mode, string head = "") =>
        "<!doctype html><html><head><style>body{margin:0}" + head + "</style></head><body>"
        + "<div id='static'>" + new HtmlChartRenderer().RenderFragment(Line(mode)) + "</div>"
        + "<div id='interactive'>" + new HtmlInteractiveChartRenderer().RenderFragment(Line(mode)) + "</div>"
        + "<div id='topology'>" + Topology(mode).ToHtmlFragment() + "</div></body></html>";

    // The colours a viewer sees: the frame card, the series line, the topology canvas and node cards.
    private const string Probe = @"() => {
        const paint = (root, selector, property) => { const node = document.querySelector(root + ' ' + selector); return node ? getComputedStyle(node)[property] : 'missing'; };
        return [
            paint('#static', '[data-cfx-role=""frame-card""]', 'fill'), paint('#static', '[data-cfx-role=""line""]', 'stroke'),
            paint('#interactive', '[data-cfx-role=""frame-card""]', 'fill'), paint('#interactive', '[data-cfx-role=""line""]', 'stroke'),
            paint('#topology', 'svg rect', 'fill'), paint('#topology', '[data-node-id=""a""] rect', 'fill'), paint('#topology', '[data-node-id=""a""] text', 'fill')
        ];
    }";

    private static async Task<string[]> PaintsAsync(string html, ColorScheme scheme) {
        await using var session = await OpenAsync(html, 760, 1200);
        await session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = scheme });
        return await session.Page.EvaluateAsync<string[]>(Probe);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AutoHtmlPaintsTheColoursOfTheViewersScheme(bool dark) {
        if (!Enabled) return;
        var scheme = dark ? ColorScheme.Dark : ColorScheme.Light;
        var expected = await PaintsAsync(Page(dark ? ThemeMode.Dark : ThemeMode.Light), scheme);
        var actual = await PaintsAsync(Page(ThemeMode.Auto), scheme);
        Assert.DoesNotContain("missing", expected);
        Assert.Equal(expected, actual);
        // The explicit opposite mode does not follow the viewer.
        Assert.NotEqual(expected, await PaintsAsync(Page(dark ? ThemeMode.Light : ThemeMode.Dark), scheme));
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedDesignRolesOfTheHostPageWinOverAutoColours(bool dark) {
        if (!Enabled) return;
        var paints = await PaintsAsync(Page(ThemeMode.Auto, ":root{--hfx-color-surface-card:#123456;--hfx-color-chart-series-1:#AA3300}"),
            dark ? ColorScheme.Dark : ColorScheme.Light);
        Assert.Equal("rgb(18, 52, 86)", paints[0]);
        Assert.Equal("rgb(170, 51, 0)", paints[1]);
        Assert.Equal("rgb(18, 52, 86)", paints[2]);
    }

    [BrowserFact]
    public async Task ExportedSvgOfAnAutoChartKeepsTheColoursTheViewerSees() {
        if (!Enabled) return;
        var dark = VisualTheme.Graphite().Resolve(ThemeMode.Dark).ElevatedSurface;
        var html = new HtmlInteractiveChartRenderer().RenderPage(Line(ThemeMode.Auto), options =>
            options.Interaction.Features = ChartForgeX.Interactivity.ChartInteractionFeatures.Tooltips | ChartForgeX.Interactivity.ChartInteractionFeatures.Export);
        await using var session = await OpenAsync(html, 760, 600);
        await session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        var download = await session.Page.RunAndWaitForDownloadAsync(() => session.Page.Locator("[data-cfx-export=svg]").EvaluateAsync("button => button.click()"));
        var svg = File.ReadAllText(await download.PathAsync() ?? throw new InvalidOperationException("No SVG export."));
        Assert.Contains("fill: " + Rgb(dark), svg);
    }

    private static string Rgb(ChartColor color) => "rgb(" + color.R + ", " + color.G + ", " + color.B + ")";
}
