using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

public sealed class HostTypographyTokensBrowserTests {
    private readonly ITestOutputHelper _output;

    public HostTypographyTokensBrowserTests(ITestOutputHelper output) => _output = output;

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpenTooltipFollowsCanonicalHostLineHeightWhenLegacyAliasRemainsUnchanged(bool dark) {
        if (!BrowserTestPrerequisites.IsEnabled()) return;
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(600, 320).WithXLabels("Monday", "Tuesday")
            .AddLine("Requests", ChartPoints.FromValues(11d, 18d));
        var html = chart.ToInteractiveHtmlPage().Replace("</head>", """
            <style>:root {
              --hfx-type-small-size:13px;
              --hfx-type-small-line-height:1.4;
              --hfx-type-small-line:1.4;
            }</style></head>
            """, StringComparison.Ordinal);
        await using var session = await HtmlBrowser.OpenSessionForNavigationAsync(new HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine,
            BrowserChannel = Environment.GetEnvironmentVariable("HFX_TEST_BROWSER_CHANNEL"),
            Headless = true, Timeout = 15000, ViewportWidth = 700, ViewportHeight = 460
        });
        var page = session.Page;
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        const string url = "http://127.0.0.1/__cfx_host_typography.html";
        await page.RouteAsync(url, route => route.FulfillAsync(new() { ContentType = "text/html; charset=utf-8", Body = html }));
        await page.GotoAsync(url);
        await page.WaitForFunctionAsync("()=>!!window.ChartForgeXCharts?.get(document.querySelector('.cfx-interactive-chart'))");
        await page.EvaluateAsync("async()=>{await document.fonts.ready;}");
        await page.Locator(".cfx-interactive-chart [data-cfx-point]:visible").First.HoverAsync();
        var tooltip = page.Locator(".cfx-tooltip");
        await tooltip.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Assert.Equal("18.2px", await tooltip.EvaluateAsync<string>("node=>getComputedStyle(node).lineHeight"));

        await page.EvaluateAsync("""
            () => {
                const root=document.documentElement;
                root.style.setProperty('--hfx-type-small-size','15px');
                root.style.setProperty('--hfx-type-small-line-height','1.75');
            }
            """);
        var measured = await tooltip.EvaluateAsync<JsonElement>("""
            async node => {
                await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
                const css=getComputedStyle(node), root=getComputedStyle(document.documentElement);
                return {fontSize:css.fontSize,lineHeight:css.lineHeight,
                    canonical:root.getPropertyValue('--hfx-type-small-line-height').trim(),
                    legacy:root.getPropertyValue('--hfx-type-small-line').trim()};
            }
            """);
        _output.WriteLine(measured.ToString());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output);
            await page.ScreenshotAsync(new() { Path = Path.Combine(output, dark ? "host-line-height-dark.png" : "host-line-height-light.png") });
        }
        Assert.True(await tooltip.IsVisibleAsync());
        Assert.Equal("1.4", measured.GetProperty("legacy").GetString());
        Assert.Equal("1.75", measured.GetProperty("canonical").GetString());
        Assert.Equal("15px", measured.GetProperty("fontSize").GetString());
        Assert.Equal("26.25px", measured.GetProperty("lineHeight").GetString());

        await page.Locator(".cfx-interactive-chart").EvaluateAsync("node=>node.style.setProperty('--cfx-tooltip-font','monospace')");
        Assert.Equal("monospace", await tooltip.EvaluateAsync<string>("node=>getComputedStyle(node).fontFamily"));
        Assert.Equal("26.25px", await tooltip.EvaluateAsync<string>("node=>getComputedStyle(node).lineHeight"));
        Assert.Empty(errors);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
