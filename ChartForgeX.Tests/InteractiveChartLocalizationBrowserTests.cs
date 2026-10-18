using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class InteractiveChartLocalizationBrowserTests {
    [BrowserTheory]
    [InlineData(false, false, HtmlChartTextDirection.Automatic, "ltr")]
    [InlineData(true, true, HtmlChartTextDirection.Automatic, "rtl")]
    [InlineData(true, true, HtmlChartTextDirection.LeftToRight, "ltr")]
    [InlineData(false, false, HtmlChartTextDirection.RightToLeft, "rtl")]
    public async Task ChromeInheritsTheHostDirectionUnlessTheAuthorChoosesOne(bool fragment, bool rtlHost,
        HtmlChartTextDirection direction, string expected) {
        if (!Enabled) return;
        var chart = StatusLines(false).WithTitle(fragment ? "Measurements" : "القياسات");
        void Configure(HtmlChartInteractionOptions options) => options.Localization.Direction = direction;
        var html = fragment ? "<!doctype html><html dir='" + (rtlHost ? "rtl" : "ltr")
            + "'><head></head><body>" + chart.ToInteractiveHtmlFragment(Configure) + "</body></html>"
            : chart.ToInteractiveHtmlPage(Configure);
        await using var session = await OpenAsync(html, 700, 560);
        var page = session.Page;
        var root = page.Locator(".cfx-interactive-chart");
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await page.ScreenshotAsync(new() { Path = Path.Combine(captures, "direction-" + direction + "-" + (fragment ? "fragment" : "page") + ".png"), FullPage = true });
        }
        Assert.Equal(expected, await root.EvaluateAsync<string>("node=>getComputedStyle(node).direction"));
        Assert.Equal(fragment ? (rtlHost ? "rtl" : "ltr") : expected,
            await page.Locator("html").EvaluateAsync<string>("node=>getComputedStyle(node).direction"));
        if (fragment && direction == HtmlChartTextDirection.LeftToRight)
            Assert.Equal("rtl", await page.Locator("html").GetAttributeAsync("dir"));
        if (direction == HtmlChartTextDirection.Automatic) {
            Assert.Null(await root.GetAttributeAsync("dir"));
            if (!fragment) Assert.Null(await page.Locator("html").GetAttributeAsync("dir"));
        } else Assert.Equal(expected, await root.GetAttributeAsync("dir"));
        Assert.Equal("ltr", await page.Locator(".cfx-stage").EvaluateAsync<string>("node=>getComputedStyle(node).direction"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(390)]
    [InlineData(980)]
    public async Task TranslatedChromeAndDynamicAnnouncementsFollowNativeUpdatesWithoutReversingAxes(int width) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(680, 380).WithTitle("القياسات")
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("readings"), "القراءات",
                new[] { new ChartPoint(0, 4), new ChartPoint(1, 8) }).WithObservationId(0, new ChartObservationId("north")));
        void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "localized";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Export);
            options.Localization.Language = "ar";
            options.Localization.Direction = HtmlChartTextDirection.RightToLeft;
            options.Localization[HtmlChartText.ChartControls] = "أدوات الرسم";
            options.Localization[HtmlChartText.ResetView] = "إعادة العرض";
            options.Localization[HtmlChartText.Compare] = "مقارنة {count}";
            options.Localization[HtmlChartText.Clear] = "مسح <اختبار>";
            options.Localization[HtmlChartText.Pinned] = "مثبت";
            options.Localization[HtmlChartText.Highlighted] = "تمييز {count}";
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Configure), width, 720);
        var page = session.Page;
        Assert.Equal("ar", await page.Locator("html").GetAttributeAsync("lang"));
        Assert.Equal("rtl", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("dir"));
        Assert.Equal("أدوات الرسم", await page.Locator(".cfx-toolbar").GetAttributeAsync("aria-label"));
        Assert.Equal("ltr", await page.Locator(".cfx-stage").EvaluateAsync<string>("stage=>getComputedStyle(stage).direction"));
        await page.Locator("g[data-cfx-observation-id=north]").FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator("g[data-cfx-observation-id=north]").GetAttributeAsync("aria-selected"));
        Assert.True(await page.Locator("g[data-cfx-observation-id=north]").EvaluateAsync<bool>("node=>node.getRootNode().activeElement===node"));
        Assert.Contains("مثبت", await page.Locator(".cfx-tooltip").EvaluateAsync<string>("node=>getComputedStyle(node,'::before').content"));
        Assert.Contains("مقارنة 1", await page.Locator("[data-cfx-compare-tray]").InnerTextAsync());
        Assert.Contains("مسح <اختبار>", await page.Locator("[data-cfx-compare-tray]").InnerTextAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-compare-tray] اختبار").CountAsync());
        await page.EvaluateAsync("()=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).highlight({observationIds:['north']})");
        Assert.Contains("تمييز 1", await page.Locator("[data-cfx-live]").InnerTextAsync());
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await page.ScreenshotAsync(new() { Path = Path.Combine(captures, "localized-rtl-" + width + ".png"), FullPage = true });
        }
        var replacement = chart.ToInteractiveHtmlFragmentWithoutAssets(options => {
            Configure(options); options.Localization.Language = "pl-PL";
            options.Localization.Direction = HtmlChartTextDirection.LeftToRight;
            options.Localization[HtmlChartText.Compare] = "Porównaj {count}";
            options.Localization[HtmlChartText.Clear] = "Wyczyść";
            options.Localization[HtmlChartText.Pinned] = "Przypięte";
        });
        var beforeUpdate = await SelectionReceiptAsync(page);
        await page.EvaluateAsync("html=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).update(html)", replacement);
        Assert.Equal("pl-PL", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("lang"));
        Assert.Equal("ltr", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("dir"));
        Assert.Equal("Przypięte", await page.Locator(".cfx-tooltip").GetAttributeAsync("data-cfx-pinned-label"));
        var afterUpdate = await SelectionReceiptAsync(page);
        if (!string.IsNullOrWhiteSpace(captures)) {
            await File.WriteAllTextAsync(Path.Combine(captures, "localized-update-" + width + ".json"),
                JsonSerializer.Serialize(new { browser = Engine.ToString(), beforeUpdate, afterUpdate }));
            await page.ScreenshotAsync(new() { Path = Path.Combine(captures, "localized-update-" + width + ".png"), FullPage = true });
        }
        Assert.Contains("Porównaj 1", await page.Locator("[data-cfx-compare-tray]").InnerTextAsync());
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> SelectionReceiptAsync(Microsoft.Playwright.IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{
            const root=document.querySelector('.cfx-interactive-chart'),point=root.querySelector('g[data-cfx-observation-id=north]'),
                tray=root.querySelector('[data-cfx-compare-tray]'),focused=root.getRootNode().activeElement;
            return {selection:ChartForgeXCharts.get(root).getSelection(),point:{data:{...point.dataset},class:point.getAttribute('class'),selected:point.getAttribute('aria-selected')},
                tray:{hidden:tray.hidden,display:getComputedStyle(tray).display,text:tray.textContent,innerText:tray.innerText},
                focused:{tag:focused?.localName,data:{...focused?.dataset}},ui:root.dataset.cfxUi,language:root.lang,direction:root.dir};
        }
        """);
}
