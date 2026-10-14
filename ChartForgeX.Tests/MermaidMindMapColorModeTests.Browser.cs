using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class MermaidMindMapColorModeTests {
    [BrowserTheory]
    [InlineData(MermaidMindMapColorMode.Branch, VisualThemeMode.Light, 960)]
    [InlineData(MermaidMindMapColorMode.Branch, VisualThemeMode.Dark, 640)]
    [InlineData(MermaidMindMapColorMode.Depth, VisualThemeMode.Light, 640)]
    [InlineData(MermaidMindMapColorMode.Depth, VisualThemeMode.Dark, 960)]
    public async Task StaticMindmapHostsDisplaySvgAndNativePng(MermaidMindMapColorMode mode, VisualThemeMode themeMode, int width) {
        if (!Enabled) return;
        var parsed = new MermaidParser().ParseMindMap(Configuration(mode, false) + Uneven);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart(new MermaidTopologyRenderOptions { Width = 760, Height = 460 })
            .WithTheme(VisualTheme.Graphite(), themeMode);
        var artifact = chart.ToVisualArtifact();
        var png = artifact.ToPng(); var svg = artifact.ToSvg();
        var html = "<!doctype html><html><body style='margin:0;background:" + chart.Theme.Resolve(themeMode).Background.ToCss()
            + "'><main style='max-width:960px;margin:auto'><section id='svg'>" + svg
            + "</section><section id='png'><img alt='Native PNG of the same mindmap' style='width:100%;height:auto' src='data:image/png;base64,"
            + Convert.ToBase64String(png) + "'></section></main><style>svg{display:block;width:100%;height:auto}</style></body></html>";
        await using var session = await OpenAsync(html, width, 720);
        await session.Page.Locator("#png img").EvaluateAsync("image => image.decode()");
        Assert.Equal(8, await session.Page.Locator("[data-cfx-role=topology-node]").CountAsync());
        Assert.Equal(7, await session.Page.Locator("[data-cfx-role=topology-edge]").CountAsync());
        Assert.Contains("Owner A", await session.Page.Locator("#svg").InnerTextAsync());
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var receipt = await session.Page.EvaluateAsync<JsonElement>("""
            () => { const canvas=document.createElement('canvas');canvas.width=canvas.height=1;const context=canvas.getContext('2d');
              const rgba=paint=>{context.clearRect(0,0,1,1);context.fillStyle=paint;context.fillRect(0,0,1,1);return [...context.getImageData(0,0,1,1).data];};
              return { viewport:innerWidth, image:{width:document.querySelector('img').naturalWidth,height:document.querySelector('img').naturalHeight},
              nodes:[...document.querySelectorAll('[data-cfx-role=topology-node]')].map(node=>({label:node.dataset.nodeLabel,
                paletteIndex:node.dataset.nodePaletteIndex,stroke:getComputedStyle(node.querySelector('[data-cfx-role=topology-node-surface]')).stroke,
                ink:rgba(getComputedStyle(node.querySelector('[data-cfx-role=topology-node-label] text')||node.querySelector('[data-cfx-role=topology-node-label]')).fill),
                surface:rgba(getComputedStyle(node.querySelector('[data-cfx-role=topology-node-surface]')).fill)})) }; }
            """);
        var contrast = receipt.GetProperty("nodes").EnumerateArray().Select(node => new {
            label = node.GetProperty("label").GetString(),
            ratio = ChartColorMath.ContrastRatio(Color(node.GetProperty("ink")), Color(node.GetProperty("surface")))
        }).ToArray();
        await CaptureAsync(session.Page, $"mindmap-{mode}-{themeMode}-{width}", svg, png, new { rendered = receipt, contrast });
        Assert.All(contrast, node => Assert.True(node.ratio >= 4.5, node.label + " contrast is " + node.ratio));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task EqualNativePaletteColorsKeepIndependentHostSlotsForNodesTintsAndEdges() {
        if (!Enabled) return;
        var tokens = new VisualDesignTokens { Palette = ChartPalettes.FromHex("#607080", "#607080", "#607080") };
        var document = new MermaidParser().ParseMindMap("mindmap\n Root\n   Owner\n      Child").Document!;
        document.ColorBy = MermaidMindMapColorMode.Depth;
        var chart = document.ToTopologyChart(new MermaidTopologyRenderOptions { Width = 640, Height = 360 })
            .WithTheme(new VisualTheme(tokens, tokens));
        var options = new TopologyRenderOptions { SvgColorVariables = tokens.ToSvgColorVariables(path => "--host-" + path.Replace('.', '-')) }.WithMindMapStyle();
        var prepared = chart.Prepare(options); var svg = prepared.ToSvg();
        await using var session = await OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 960, 540);
        var target = tokens.Clone();
        target.Palette = ChartPalettes.FromHex("#BE123C", "#0F766E", "#4338CA");
        var variables = target.ToSvgColorVariables(path => "--host-" + path.Replace('.', '-'));
        await session.Page.Locator("svg").EvaluateAsync("(svg,css)=>svg.setAttribute('style',css)",
            string.Join(";", variables.GetVariablesForSvg(svg).Select(variable => variable.Name + ":" + variable.Color.ToCss())));
        chart.WithTheme(new VisualTheme(target, target));
        var receipt = await session.Page.Locator("[data-cfx-role=topology-node-surface]").EvaluateAllAsync<object>(
            "nodes=>nodes.map(node=>({stroke:getComputedStyle(node).stroke,fill:getComputedStyle(node).fill,source:node.getAttribute('fill')}))");
        await CaptureAsync(session.Page, "mindmap-equal-palette-host-slots", svg, chart.ToPng(options), new {
            initial = tokens.Palette.Select(color => color.ToCss()), host = target.Palette.Select(color => color.ToCss()), receipt
        });
        for (var index = 0; index < 3; index++) {
            var surface = session.Page.Locator("[data-cfx-role=topology-node-surface]").Nth(index);
            await AssertCssColorsEqualAsync(session.Page, target.Palette[index].ToCss(), await surface.EvaluateAsync<string>("node=>getComputedStyle(node).stroke"));
            var expectedTint = "color-mix(in srgb, " + target.Background.ToCss() + " 90%, " + target.Palette[index].ToCss() + ")";
            // Normalize both paints through computed style before the canvas comparison: its six-decimal
            // serialization can otherwise change one channel at an exact half-byte rounding boundary.
            var browserTint = await session.Page.EvaluateAsync<string>("""
                paint=>{const probe=document.createElement('span');probe.style.color=paint;document.body.append(probe);
                  try{return getComputedStyle(probe).color;}finally{probe.remove();}}
                """, expectedTint);
            await AssertCssColorsEqualAsync(session.Page, browserTint, await surface.EvaluateAsync<string>("node=>getComputedStyle(node).fill"));
        }
        for (var index = 0; index < 2; index++)
            await AssertCssColorsEqualAsync(session.Page, target.Palette[index + 1].ToCss(), await session.Page.Locator("[data-cfx-role=topology-edge-line]")
                .Nth(index).EvaluateAsync<string>("node=>getComputedStyle(node).stroke"));
        AssertNoConsoleErrors(session);
    }

    private static async Task CaptureAsync(IPage page, string name, string svg, byte[] png, object receipt) {
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var path = Path.Combine(captures, name);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(path + ".svg", svg);
        await File.WriteAllBytesAsync(path + ".native.png", png);
        await File.WriteAllTextAsync(path + ".json", JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static ChartColor Color(JsonElement channels) => ChartColor.FromRgba(channels[0].GetByte(), channels[1].GetByte(), channels[2].GetByte(), channels[3].GetByte());
}
