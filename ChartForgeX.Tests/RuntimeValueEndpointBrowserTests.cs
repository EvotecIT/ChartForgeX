using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RuntimeValueEndpointBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TypedWaterfallBrushUsesSecondaryRawBoundsAndOmitsAnAmbiguousMixedValueAxis(bool primaryReference) {
        if (!Enabled) return;
        var chart = ChartLinkedValueEndpointContractsTests.CreateChart("waterfall", horizontal: false, secondary: true);
        chart.Options.SecondaryYAxis.Minimum = -5;
        chart.Options.SecondaryYAxis.Maximum = 35;
        if (primaryReference) chart.AddHorizontalLine(1, "Primary reference");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Brush);
            options.Interaction.BrushAxis = ChartBrushAxis.XY;
        }), 1000, 700);
        var page = session.Page;
        var projection = await page.EvaluateAsync<JsonElement>("() => JSON.parse(document.querySelector('.cfx-interactive-chart').dataset.cfxPreparedChart).dataProjection");
        Assert.Equal(!primaryReference, projection.TryGetProperty("y", out var y));
        if (!primaryReference) {
            Assert.Equal(-5, y.GetProperty("minimum").GetDouble());
            Assert.Equal(35, y.GetProperty("maximum").GetDouble());
            var ticks = y.GetProperty("displayTicks").EnumerateArray().ToArray();
            Assert.NotEmpty(ticks);
            Assert.All(ticks, tick => Assert.EndsWith(" ms", tick.GetProperty("text").GetString()));
        }
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        var points = await page.EvaluateAsync<JsonElement>("""
            () => {
              const root=document.querySelector('.cfx-interactive-chart'),svg=root.querySelector('.cfx-stage svg');
              window.waterfallBrush=null;root.addEventListener('cfxbrush',event=>window.waterfallBrush=event.detail,{once:true});
              const plot=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
              const screen=(x,y)=>{const point=new DOMPoint(plot.x+x*plot.width,plot.y+y*plot.height).matrixTransform(svg.getScreenCTM());return{x:point.x,y:point.y}};
              return{from:screen(.2,.2),to:screen(.8,.8)};
            }
            """);
        await page.Mouse.MoveAsync((float)points.GetProperty("from").GetProperty("x").GetDouble(), (float)points.GetProperty("from").GetProperty("y").GetDouble());
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)points.GetProperty("to").GetProperty("x").GetDouble(), (float)points.GetProperty("to").GetProperty("y").GetDouble(), new Microsoft.Playwright.MouseMoveOptions { Steps = 4 });
        await page.Mouse.UpAsync();
        var brush = await page.EvaluateAsync<JsonElement>("() => waterfallBrush");
        var range = brush.GetProperty("range");
        Assert.Equal(!primaryReference, range.TryGetProperty("y", out var rawY));
        if (!primaryReference) {
            Assert.InRange(rawY[0].GetDouble(), 2.5, 3.5);
            Assert.InRange(rawY[1].GetDouble(), 26.5, 27.5);
        }
        var output = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await File.WriteAllTextAsync(Path.Combine(output, "value-projection-waterfall-mixed-" + primaryReference + ".json"),
                JsonSerializer.Serialize(new { projection, brush }));
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("rangebar", false, false, false)]
    [InlineData("rangebar", true, false, false)]
    [InlineData("rangebar", false, true, false)]
    [InlineData("waterfall", false, false, false)]
    [InlineData("waterfall", true, false, false)]
    [InlineData("waterfall", false, true, false)]
    [InlineData("dumbbell", false, false, false)]
    [InlineData("dumbbell", false, true, false)]
    [InlineData("rangebar", false, false, true)]
    [InlineData("rangebar", true, false, true)]
    [InlineData("rangebar", false, true, true)]
    [InlineData("waterfall", false, false, true)]
    [InlineData("waterfall", true, false, true)]
    [InlineData("waterfall", false, true, true)]
    [InlineData("dumbbell", false, false, true)]
    [InlineData("dumbbell", false, true, true)]
    public async Task PublicValueBoundsUseTheirOwningAxisInTargetsAndReadouts(string family, bool horizontal, bool secondary, bool dark) {
        if (!Enabled) return;
        var chart = ChartLinkedValueEndpointContractsTests.CreateChart(family, horizontal, secondary)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Tooltips);
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Toolbar = HtmlChartToolbarLayout.Compact;
        }), dark ? 390 : 1440, 700);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.endpointRoot=document.querySelector('.cfx-interactive-chart'); ChartForgeXCharts.get(endpointRoot).select({observationIds:['value-first']}); }");
        await page.Locator("[data-cfx-observation-id='value-first']").FocusAsync();
        await page.Locator(".cfx-tooltip").WaitForAsync(new Microsoft.Playwright.LocatorWaitForOptions { State = Microsoft.Playwright.WaitForSelectorState.Visible });
        var result = await page.EvaluateAsync<JsonElement>("""
            () => ({target:ChartForgeXCharts.get(endpointRoot).getSelection()[0],
              tooltip:endpointRoot.querySelector('.cfx-tooltip').innerText,
              source:{start:endpointRoot.querySelector('[data-cfx-observation-id="value-first"]').dataset.cfxStart,
                end:endpointRoot.querySelector('[data-cfx-observation-id="value-first"]').dataset.cfxEnd}})
            """);
        var first = family == "waterfall" ? "0,00 ms" : "10,25 ms";
        var last = family == "waterfall" ? "10,25 ms" : "24,75 ms";
        Assert.Equal(first + " – " + last, result.GetProperty("target").GetProperty("rangeLabel").GetString());
        Assert.Contains(first, result.GetProperty("tooltip").GetString());
        Assert.Contains(last, result.GetProperty("tooltip").GetString());
        Assert.DoesNotContain("wrong-primary-axis", result.GetProperty("tooltip").GetString());
        Assert.Equal(family == "waterfall" ? "0" : "10.25", result.GetProperty("source").GetProperty("start").GetString());
        Assert.Equal(family == "waterfall" ? "10.25" : "24.75", result.GetProperty("source").GetProperty("end").GetString());
        var output = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var name = "value-endpoint-" + family + "-" + horizontal + "-" + secondary + "-" + dark;
            await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions { Path = Path.Combine(output, name + ".png"), FullPage = true });
            await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), result.GetRawText());
        }
        AssertNoConsoleErrors(session);
    }
}
