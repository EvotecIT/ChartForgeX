using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveSunburstBrowserTests {
    [Theory]
    [InlineData(360, false, false)]
    [InlineData(360, true, false)]
    [InlineData(800, false, false)]
    [InlineData(800, true, false)]
    [InlineData(360, false, true)]
    [InlineData(360, true, true)]
    [InlineData(800, false, true)]
    [InlineData(800, true, true)]
    public async Task PoliciesRetainNativePointerKeyboardIdentityAndLocalizedIndependentFacts(int width, bool dark, bool inclusive) {
        if (!Enabled) return;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var variant = inclusive ? "authored-total" : "options";
        var chart = V2GalleryModels.Create(ChartSeriesKind.Sunburst, variant, mode)
            .WithSize(width, width < 500 ? 360 : 460).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Przydział pracy").WithSubtitle(inclusive ? "Inclusive parent totals retain unallocated remainder" : "Leaf totals size sectors; measured color stays independent")
            .ConfigureLabels(labels => { labels.Color = "Kolor"; labels.NoData = "Brak danych"; labels.AuthoredValue = "Dostarczono <&>"; labels.Remainder = "Pozostało <&>"; });
        chart.Options.Sunburst.ColorLegendTitle = "Zmiana <&>";
        chart.Series[0].WithInteractionKey("allocation-source");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        chart.Options.Sunburst.ParentValuePolicy = inclusive ? ChartHierarchyValuePolicy.LeafAggregate : ChartHierarchyValuePolicy.AuthoredTotal;
        chart.ConfigureLabels(labels => { labels.AuthoredValue = "Changed"; labels.Remainder = "Changed"; labels.NoData = "Changed"; });
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 510 : 610);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var name = "sunburst-" + variant + "-" + width + "-" + (dark ? "dark" : "light");
        await Capture(page, prepared, html, name, native: true);
        Assert.Equal(7, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(2, await page.Locator("[data-cfx-target-kind=node][data-cfx-label=Support]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-target-kind=node][data-cfx-point],[data-cfx-target-kind=node][data-cfx-source-point]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-node=reserve] path").CountAsync());
        Assert.Equal("0", await page.Locator("[data-cfx-node=operations]").GetAttributeAsync("data-cfx-color-value"));
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selection = event.detail.target)");
        var group = page.Locator("[data-cfx-node=engineering]");
        await group.FocusAsync(); await page.Keyboard.PressAsync("Space");
        using var selected = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.selection)"));
        Assert.Equal("engineering", selected.RootElement.GetProperty("targetId").GetString());
        Assert.Equal("node", selected.RootElement.GetProperty("targetKind").GetString());
        Assert.Equal("allocation-source", selected.RootElement.GetProperty("seriesKey").GetString());
        Assert.Equal(inclusive ? "80" : "60", selected.RootElement.GetProperty("value").GetString());
        Assert.False(selected.RootElement.TryGetProperty("point", out _));
        var rows = await Rows(page);
        Assert.Equal("12", rows["Zmiana <&>"]);
        if (inclusive) { Assert.Equal("20", rows["Pozostało <&>"]); Assert.False(rows.ContainsKey("Dostarczono <&>")); }
        else { Assert.Equal("80", rows["Dostarczono <&>"]); Assert.False(rows.ContainsKey("Pozostało <&>")); }
        await Capture(page, prepared, html, name + "-group", native: false);
        var pinnedId = "engineering";
        foreach (var id in new[] { "platform", "operations-support" }) {
            await page.Locator("[data-cfx-node=" + pinnedId + "]").FocusAsync();
            await page.Keyboard.PressAsync("Space");
            await page.EvaluateAsync("() => document.activeElement?.blur()");
            await MoveAwayAsync(page);
            var node = page.Locator("[data-cfx-node=" + id + "]");
            var coordinates = await node.EvaluateAsync<double[]>("""
                node => { const box = node.getBoundingClientRect();
                    for (let y=1; y<30; y++) for (let x=1; x<30; x++) {
                        const px=box.left+box.width*x/30, py=box.top+box.height*y/30, hit=document.elementFromPoint(px,py);
                        if (hit && hit.closest('[data-cfx-target-kind=node]') === node && !hit.hasAttribute('data-cfx-browser-hit-area')) return [px,py];
                    } throw new Error('No native Sunburst sector hit'); }
                """);
            await page.Mouse.MoveAsync((float)coordinates[0], (float)coordinates[1], new MouseMoveOptions { Steps = 4 });
            await page.WaitForFunctionAsync("id => document.querySelector('.cfx-hovered[data-cfx-target-kind=node]')?.dataset.cfxTargetId === id", id);
            rows = await Rows(page);
            Assert.Equal(id == "platform" ? "35" : "40", rows["Value"]);
            Assert.Equal(id == "platform" ? "-4" : "Brak danych", rows["Zmiana <&>"]);
            Assert.False(rows.ContainsKey("Dostarczono <&>")); Assert.False(rows.ContainsKey("Pozostało <&>"));
            Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
            await node.FocusAsync(); await page.Keyboard.PressAsync("Enter");
            Assert.Equal(id, await page.EvaluateAsync<string>("() => window.selection.targetId"));
            pinnedId = id;
        }
        await page.Locator("[data-cfx-node=reserve]").FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("reserve", await page.EvaluateAsync<string>("() => window.selection.targetId"));
        rows = await Rows(page); Assert.Equal("0", rows["Value"]); Assert.Equal("Brak danych", rows["Zmiana <&>"]);
        Assert.False(rows.ContainsKey("Dostarczono <&>")); Assert.False(rows.ContainsKey("Pozostało <&>"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static async Task<Dictionary<string, string>> Rows(IPage page) {
        await page.WaitForFunctionAsync("() => { const tip=document.querySelector('.cfx-tooltip'); return tip && !tip.hidden; }");
        using var rows = JsonDocument.Parse(await page.EvaluateAsync<string>("""
            () => JSON.stringify(Object.fromEntries(Array.from(document.querySelectorAll('.cfx-tooltip dt')).map(term => [term.textContent,term.nextElementSibling.textContent])))
            """));
        return rows.RootElement.EnumerateObject().ToDictionary(row => row.Name, row => row.Value.GetString()!);
    }
    private static async Task Capture(IPage page, PreparedVisual prepared, string html, string name, bool native) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        if (!native) return;
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        await File.WriteAllBytesAsync(Path.Combine(directory, name + ".native.png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
