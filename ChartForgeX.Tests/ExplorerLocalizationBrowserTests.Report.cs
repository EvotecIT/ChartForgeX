using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ExplorerLocalizationBrowserTests {
    [BrowserFact]
    public async Task PublicReportStartsWithCanonicalMessagesAndKeepsAuthoredSearchLanguageDuringNavigation() {
        if (!Enabled) return;
        var report = TopologyFixture().PrepareReport(new TopologyReportOptions { MaximumNodesPerPage = 1 });
        await using var session = await OpenAsync(report.ToInteractiveHtmlPage(Arabic), 390, 850);
        var page = session.Page;
        await ObserveInputAsync(page);
        await AssertLeadingEnglishNumberOrderAsync(page.Locator("[data-cfx-report-summary]"));
        Assert.Equal("ltr", await page.Locator(".diagram svg").EvaluateAsync<string>("svg=>getComputedStyle(svg).direction"));
        Assert.Equal("rtl", await page.Locator("main").EvaluateAsync<string>("root=>getComputedStyle(root).direction"));
        Assert.Equal("نظرة عامة", await page.Locator("#page-status").TextContentAsync());
        Assert.Equal("الصفحات التفصيلية", await page.Locator(".connections h2").TextContentAsync());
        Assert.Equal("البحث عن اسم", await page.Locator("#search").GetAttributeAsync("placeholder"));
        await page.Locator("#search").FillAsync("Authored source");
        Assert.Equal("نتيجة 1", await page.Locator("#search-status").TextContentAsync());
        var result = page.Locator("#results button");
        Assert.Equal("pl-PL", await result.GetAttributeAsync("lang"));
        Assert.Equal("Authored source {count} — source-a", await result.TextContentAsync());
        await result.ClickAsync();
        Assert.Equal("علاقات الصفحات", await page.Locator(".connections h2").TextContentAsync());
        Assert.Equal("pl-PL", await page.Locator(".links button").First.GetAttributeAsync("lang"));
        Assert.Equal("ar", await page.Locator(".links button span").First.GetAttributeAsync("lang"));
        Assert.Equal(1, await page.Locator("#content svg").CountAsync());
        Assert.Contains("صفحة", await page.Locator("#page-status").TextContentAsync());
        Assert.Equal("ar", await page.Locator("main").GetAttributeAsync("lang"));
        Assert.Contains("Authored source {count}", await page.Locator("#content").TextContentAsync());
        await CaptureAsync(page, "report-detail");
        await page.Locator("#page").SelectOptionAsync("0");
        Assert.Equal("الصفحات التفصيلية", await page.Locator(".connections h2").TextContentAsync());
        Assert.Equal(1, await page.Locator("#content svg").CountAsync());
        await CaptureAsync(page, "report-overview");
        AssertNoConsoleErrors(session);
    }
}
