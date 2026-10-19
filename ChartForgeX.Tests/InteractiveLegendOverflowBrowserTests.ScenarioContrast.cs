using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveLegendOverflowBrowserTests {
    private static async Task AssertScenarioPanelContrastAsync(IPage page, bool dark, string palette) {
        var state = "scenario-panel-" + palette + "-" + dark;
        var title = page.Locator(".cfx-scenario-panel__title");
        var meta = page.Locator(".cfx-scenario-panel__meta");
        await AssertReadableControlAsync(page, title, state + "-all-title");
        await AssertReadableControlAsync(page, meta, state + "-all-meta");
        await page.Locator("[data-cfx-scenario=route]").ClickAsync();
        await AssertReadableControlAsync(page, title, state + "-route-title");
        await AssertReadableControlAsync(page, meta, state + "-route-meta");
        var steps = page.Locator(".cfx-scenario-panel__steps li");
        Assert.Equal(2, await steps.CountAsync());
        for (var index = 0; index < 2; index++) {
            await AssertReadableControlAsync(page, steps.Nth(index), state + "-step-" + index);
            await AssertReadableControlAsync(page, steps.Nth(index), state + "-badge-" + index, "::before");
        }
        await AssertScenarioProgressAsync(page, 0, state + "-zero");
        for (var index = 0; index < 2; index++) {
            await steps.Nth(index).ClickAsync();
            Assert.Equal("step", await steps.Nth(index).GetAttributeAsync("aria-current"));
            await AssertReadableControlAsync(page, steps.Nth(index), state + "-current-" + index);
            await AssertScenarioProgressAsync(page, (index + 1) * 50, state + "-progress-" + index);
        }
        await CaptureAsync(page, state + "-complete");
        var play = page.Locator("[data-cfx-scenario-step-control=play]");
        await play.ClickAsync();
        Assert.Equal("true", await play.GetAttributeAsync("aria-pressed"));
        Assert.Equal("Pause", await play.InnerTextAsync());
        await AssertReadableControlStatesAsync(page, play, state + "-playing");
        await AssertScenarioProgressAsync(page, 50, state + "-playing-progress");
        await CaptureAsync(page, state + "-playing");
        await play.ClickAsync();
        Assert.Equal("false", await play.GetAttributeAsync("aria-pressed"));
        await page.Locator(".cfx-scenario[data-cfx-scenario='']").ClickAsync();
        Assert.Equal(0, await steps.CountAsync());
    }

    private static async Task AssertScenarioProgressAsync(IPage page, int percent, string state) {
        await page.WaitForFunctionAsync("""
            percent=>{
                const progress=document.querySelector('.cfx-scenario-progress'),bar=progress.querySelector('.cfx-scenario-progress__bar');
                const expected=progress.clientWidth*percent/100;
                return Math.abs(parseFloat(getComputedStyle(bar).width)-expected)<1;
            }
            """, percent);
        var report = await page.Locator(".cfx-scenario-progress").EvaluateAsync<JsonElement>("""
            progress=>{
                const bar=progress.querySelector('.cfx-scenario-progress__bar'),text=progress.querySelector('.cfx-scenario-progress__text');
                const range=document.createRange();range.selectNodeContents(text);
                const label=range.getBoundingClientRect(),strip=bar.getBoundingClientRect();
                return{value:progress.getAttribute('aria-valuenow'),maximum:progress.getAttribute('aria-valuemax'),
                    bar:strip.toJSON(),label:label.toJSON(),progress:progress.getBoundingClientRect().toJSON(),
                    text:text.textContent,fill:getComputedStyle(bar).backgroundColor};
            }
            """);
        Assert.Equal((percent / 50).ToString(), report.GetProperty("value").GetString());
        Assert.Equal("2", report.GetProperty("maximum").GetString());
        Assert.True(report.GetProperty("bar").GetProperty("top").GetDouble() >= report.GetProperty("label").GetProperty("bottom").GetDouble(),
            "The progress strip remains below the caption glyphs, preserving one readable caption surface.");
        await AssertReadableControlAsync(page, page.Locator(".cfx-scenario-progress__text"), state + "-caption");
        if (percent > 0) await AssertReadableControlAsync(page, page.Locator(".cfx-scenario-progress__bar"), state + "-fill", nonTextFill: true);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, state + ".json"), JsonSerializer.Serialize(report));
        }
    }
}
