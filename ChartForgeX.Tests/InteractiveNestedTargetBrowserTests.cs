using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveNestedTargetBrowserTests {
    [Theory]
    [InlineData(false, 800)]
    [InlineData(true, 360)]
    public async Task NestedAuthoredTargetsSelectOnceWhileTheirDecorationsStayWithTheirOwner(bool dark, int width) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(width, 320).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .AddTree("Teams", new[] { new ChartNode("teams", "All teams"), new ChartNode("north", "Support"), new ChartNode("south", "Support") },
                new[] { new ChartTreeLink("teams", "north", 5), new ChartTreeLink("teams", "south", 8) });
        // A host can retain nested source groups in its SVG. Keep the actual renderer's marks,
        // geometry and facts; only put the children beneath their authored parent before binding.
        var html = chart.ToInteractiveHtmlPage();
        var start = html.IndexOf("<svg", StringComparison.Ordinal); var end = html.IndexOf("</svg>", start, StringComparison.Ordinal) + 6;
        var svg = XElement.Parse(html.Substring(start, end - start));
        var nodes = svg.Descendants().Where(node => node.Attribute("data-cfx-node") != null).ToDictionary(node => (string)node.Attribute("data-cfx-node")!);
        foreach (var id in new[] { "north", "south" }) { nodes[id].Remove(); nodes["teams"].Add(nodes[id]); }
        html = html.Substring(0, start) + svg.ToString(SaveOptions.DisableFormatting) + html.Substring(end);
        await using var session = await OpenAsync(html, width + 24, 430);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.selections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selections.push(event.detail.target)); }");
        var child = page.Locator("[data-cfx-node=south]");
        Assert.Equal("-1", await child.GetAttributeAsync("tabindex"));
        Assert.Equal(1, await page.Locator("[data-cfx-node][tabindex='0']").CountAsync());
        Assert.Equal(0, await child.Locator("[tabindex], [data-cfx-target-kind]").CountAsync());
        await child.ClickAsync();
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.selections.length"));
        Assert.Equal("south", await page.EvaluateAsync<string>("() => window.selections[0].targetId"));
        Assert.Equal("8", await page.EvaluateAsync<string>("() => window.selections[0].value"));
        Assert.True(await page.EvaluateAsync<bool>("() => window.selections[0].point === undefined && window.selections[0].sourcePoint === undefined"));
        Assert.Null(await page.Locator("[data-cfx-node=teams]").GetAttributeAsync("aria-selected"));
        Assert.Contains("Support", await TooltipTextAsync(page)); Assert.Contains("8", await TooltipTextAsync(page));
        await child.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.selections.length"));
        Assert.Equal("south", await page.EvaluateAsync<string>("() => window.selections[1].targetId"));
        Assert.Equal("false", await child.GetAttributeAsync("aria-selected"));
        await page.Keyboard.PressAsync("ArrowLeft");
        Assert.Equal("north", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        Assert.Equal(1, await page.Locator("[data-cfx-node][tabindex='0']").CountAsync());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await File.WriteAllTextAsync(Path.Combine(capture, "nested-authored-targets-" + (dark ? "dark" : "light") + ".html"), html);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "nested-authored-targets-" + (dark ? "dark" : "light") + ".png") });
        }
        AssertNoConsoleErrors(session);
    }
}
