using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class MermaidQuadrantRegionBrowserTests {
    [BrowserTheory]
    [InlineData(360, false)]
    [InlineData(800, true)]
    public async Task EqualPaletteSlotsFollowIndependentHostThemeValues(int width, bool dark) {
        if (!Enabled) return;
        var neutral = ChartColor.FromHex("#607080");
        var tokens = dark ? VisualDesignTokens.Dark() : new VisualDesignTokens();
        tokens.Palette = new[] { neutral, neutral };
        var chart = new MermaidParser().ParseQuadrant("quadrantChart\nquadrant-1 Plan\nquadrant-2 Prioritize\nquadrant-3 Consider\nquadrant-4 Reassess\n")
            .Document!.ToChart(new MermaidQuadrantRenderOptions { Width = width, Height = 440 }).WithDesignTokens(tokens);
        var context = VisualExportRequest.ForChart(chart).Context;
        var svg = chart.Prepare(context).ToSvg(new VisualSvgOptions("equal-palette", tokens.ToSvgColorVariables(path => "--host-" + path.Replace('.', '-'))));
        await using var session = await OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 440);
        var target = tokens.Clone();
        target.Palette = new[] { ChartColor.FromRgba(224, 32, 48, 128), ChartColor.FromRgba(16, 160, 224, 64) };
        var variables = target.ToSvgColorVariables(path => "--host-" + path.Replace('.', '-'));
        await session.Page.Locator("svg").EvaluateAsync("(svg, css) => svg.setAttribute('style', css)",
            string.Join(";", variables.GetVariablesForSvg(svg).Select(variable => variable.Name + ":" + variable.Color.ToCss())));
        var surface = context.Theme.Resolve(context.ThemeMode).Surface;
        for (var index = 0; index < 4; index++) {
            var color = target.Palette[index % 2];
            var fill = ChartForgeX.Rendering.ChartColorMath.WithOpacity(color, .14);
            var plate = ChartForgeX.Rendering.ChartColorMath.Blend(ChartColor.FromRgb(surface.R, surface.G, surface.B), ChartColor.FromRgb(color.R, color.G, color.B), .12);
            foreach (var (role, expected) in new[] { ("plot-region-fill", fill), ("plot-region-label-backplate", plate) }) {
                var pixels = await session.Page.Locator("[data-cfx-role='" + role + "']").Nth(index).EvaluateAsync<int[][]>("""
                    (node, expected) => { const c=document.createElement('canvas').getContext('2d');
                      const read=paint => { c.clearRect(0,0,1,1); c.fillStyle=paint; c.fillRect(0,0,1,1);
                        return [...c.getImageData(0,0,1,1).data]; }; return [read(getComputedStyle(node).fill), read(expected)]; }
                    """, expected.ToCss());
                Assert.Equal(pixels[1], pixels[0]);
            }
            var ink = ChartForgeX.Rendering.ChartColorMath.AccessibleTextOnBackground(plate);
            Assert.Equal($"rgb({ink.R}, {ink.G}, {ink.B})", await session.Page.Locator("[data-cfx-role=plot-region-label] text").Nth(index)
                .EvaluateAsync<string>("node => getComputedStyle(node).fill"));
        }
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var path = Path.Combine(capture, $"quadrant-equal-palette-{width}-{dark}");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png" });
            chart.WithDesignTokens(target);
            await File.WriteAllBytesAsync(path + ".native.png", chart.ToPng());
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(360, 128)]
    [InlineData(360, 255)]
    [InlineData(800, 128)]
    [InlineData(800, 255)]
    public async Task TranslucentPaletteCaptionTracksHostThemeWithoutRerendering(int width, int initialAlpha) {
        if (!Enabled) return;
        var color = ChartColor.FromRgba(224, 32, 48, (byte)initialAlpha);
        var chart = new MermaidParser().ParseQuadrant("quadrantChart\nquadrant-1 Plan\n").Document!
            .ToChart(new MermaidQuadrantRenderOptions { Width = width, Height = 440 });
        chart.WithTheme(ChartTheme.GraphiteLight().WithPalette(new[] { color }));
        var context = VisualExportRequest.ForChart(chart).Context;
        var lightSurface = context.Theme.Resolve(context.ThemeMode).Surface;
        var light = new SvgColorVariables().Add("--surface", lightSurface, SvgColorRole.Surface).Add("--region", color, SvgColorRole.Series);
        var svg = chart.Prepare(context).ToSvg(new VisualSvgOptions("translucent-palette", light));
        await using var session = await OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 440);
        foreach (var dark in new[] { false, true }) {
            var surface = dark ? ChartColor.Black : lightSurface;
            var accent = dark ? ChartColor.FromRgba(16, 160, 224, 64) : color;
            var variables = new SvgColorVariables().Add("--surface", surface, SvgColorRole.Surface).Add("--region", accent, SvgColorRole.Series);
            await session.Page.Locator("svg").EvaluateAsync("(svg, css) => { svg.setAttribute('style', css); }",
                string.Join(";", variables.GetVariablesForSvg(svg).Select(variable => variable.Name + ":" + variable.Color.ToCss())));
            var expected = ChartForgeX.Rendering.ChartColorMath.Blend(surface, ChartColor.FromRgb(accent.R, accent.G, accent.B), .12);
            var actual = await session.Page.Locator("[data-cfx-role=plot-region-label-backplate]").EvaluateAsync<int[]>("""
                node => { const c=document.createElement('canvas').getContext('2d'); c.fillStyle=getComputedStyle(node).fill;
                  c.fillRect(0,0,1,1); return [...c.getImageData(0,0,1,1).data]; }
                """);
            Assert.Equal(new[] { (int)expected.R, expected.G, expected.B, 255 }, actual);
            var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(capture)) {
                Directory.CreateDirectory(capture);
                await File.WriteAllTextAsync(Path.Combine(capture, $"quadrant-translucent-host-{width}-{initialAlpha}-{dark}.html"), await session.Page.ContentAsync());
                await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, $"quadrant-translucent-host-{width}-{initialAlpha}-{dark}.png") });
            }
            var ink = ChartForgeX.Rendering.ChartColorMath.AccessibleTextOnBackground(expected);
            Assert.Equal($"rgb({ink.R}, {ink.G}, {ink.B})", await session.Page.Locator("[data-cfx-role=plot-region-label] text").EvaluateAsync<string>("node => getComputedStyle(node).fill"));
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(360, false)]
    [InlineData(800, true)]
    public async Task HostPaletteAndPointFreeDescriptionsReachStaticBrowserOutput(int width, bool dark) {
        if (!Enabled) return;
        var chart = new MermaidParser().ParseQuadrant("quadrantChart\ntitle Priorities\nquadrant-1 Plan\nquadrant-2 Prioritize\nquadrant-3 Consider\nquadrant-4 Reassess\n")
            .Document!.ToChart(new MermaidQuadrantRenderOptions { Width = width, Height = 440 });
        var palette = new[] { "#E02030", "#20B040", "#3040D0", "#D08010" }.Select(ChartColor.FromHex).ToArray();
        chart.WithTheme((dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithPalette(palette));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = "<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>";
        await using var session = await OpenAsync(html, width, 440);
        Assert.Equal("img", await session.Page.Locator("svg").GetAttributeAsync("role"));
        var description = await session.Page.Locator("svg desc").TextContentAsync();
        foreach (var label in new[] { "Plan", "Prioritize", "Consider", "Reassess" }) Assert.Contains(label, description);
        Assert.Equal(palette.Select(color => color.WithOpacity(.14).ToCss()),
            await session.Page.Locator("[data-cfx-role='plot-region-fill']").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.getAttribute('fill'))"));
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role='plot-region-label']").CountAsync());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var path = Path.Combine(capture, $"quadrant-host-browser-{width}-{dark}");
            await File.WriteAllTextAsync(path + ".html", html);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".png" });
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(360, false)]
    [InlineData(800, true)]
    public async Task NativeQuadrantCaptionsRemainInsideTheirRegionsInBrowserOutput(int width, bool dark) {
        if (!Enabled) return;
        var source = (dark ? "---\nconfig:\n  theme: dark\n---\n" : "")
            + "quadrantChart\ntitle Work priorities\nx-axis Low effort --> High effort\ny-axis Low impact --> High impact\n"
            + "quadrant-1 Plan\nquadrant-2 Prioritize\nquadrant-3 Consider\nquadrant-4 Reassess\n"
            + "Opportunity: [0.2, 0.8]\nInvestment: [0.8, 0.8]\n";
        var document = new MermaidParser().ParseQuadrant(source).Document!;
        var artifact = document.ToVisualArtifact(new MermaidQuadrantRenderOptions { Width = width, Height = 440 });
        var html = artifact.ToHtmlPage();
        await using var session = await OpenAsync(html, width + 24, 480);
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role='plot-region-label']").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('[data-cfx-role="plot-region"]')].every(region => {
              const fill = region.querySelector('[data-cfx-role="plot-region-fill"]').getBoundingClientRect();
              const caption = document.querySelector(`[data-cfx-source-id="${region.dataset.cfxSourceId}-caption"]`);
              const text = caption.querySelector('[data-cfx-role="plot-region-label"]').getBoundingClientRect();
              return text.left >= fill.left - 1 && text.right <= fill.right + 1
                && text.top >= fill.top - 1 && text.bottom <= fill.bottom + 1;
            })
            """));
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role='mermaid-quadrant-point']").CountAsync());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await File.WriteAllTextAsync(Path.Combine(capture, $"quadrant-browser-{width}-{dark}.html"), html);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, $"quadrant-browser-{width}-{dark}.png") });
        }
        AssertNoConsoleErrors(session);
    }
}
