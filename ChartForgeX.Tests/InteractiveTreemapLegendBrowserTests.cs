using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveTreemapLegendBrowserTests {
    private const string FirstId = "north/support:A";
    private static string Key(string id) => "[data-cfx-role=legend-item][data-cfx-legend-target-id='" + id + "']";
    private static string Node(string id) => "[data-cfx-target-kind=node][data-cfx-target-id='" + id + "']";
    private static Chart Model(bool dark, bool reordered = false, bool missing = false) {
        var items = new[] {
            new ChartHierarchyItem("north", "North"), new ChartHierarchyItem("services", "Services", "north"),
            new ChartHierarchyItem(FirstId, "Support", "services", 6), new ChartHierarchyItem("sibling", "Support", "services", 3),
            new ChartHierarchyItem("south", "South"), new ChartHierarchyItem("other", "Support", "south", 2)
        }.AsEnumerable();
        if (missing) items = items.Where(item => item.Id != FirstId);
        if (reordered) items = items.Reverse().Select(item => new ChartHierarchyItem(item.Id,
            item.Id == FirstId ? "Customer care" : item.Label, item.ParentId, item.Value, item.ColorValue));
        var chart = Chart.Create().WithPointLegend().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Work by team").AddTreemap("Allocation", items);
        chart.Series[0].WithInteractionKey("allocation-source");
        return chart;
    }

    [Theory]
    [InlineData(false, 800)]
    [InlineData(true, 800)]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    public async Task RepeatedLeafKeysReadRawFactsAndMuteOrIsolateOnlyTheirAuthoredNode(bool dark, int width) {
        if (!Enabled) return;
        var chart = Model(dark).WithSize(width, 360);
        var html = chart.ToInteractiveHtmlPage(); await using var session = await OpenAsync(html, width + 24, 490);
        var page = session.Page; var key = page.Locator(Key(FirstId));
        var identities = await page.Locator("[data-cfx-role=legend-item]").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxTargetId)");
        Assert.Equal(3, identities.Distinct().Count());
        Assert.Equal(0, await page.Locator("[data-cfx-point],[data-cfx-source-point]").CountAsync());
        await page.EvaluateAsync("() => { window.events = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxseries', event => window.events.push(event.detail)); }");
        await key.FocusAsync();
        Assert.Equal(new[] { "Support", "Value", "6" }, (await TooltipTextAsync(page)).Split('\n'));
        var colors = await page.EvaluateAsync<string[]>("() => [getComputedStyle(document.querySelector('.cfx-tooltip__swatch')).backgroundColor, getComputedStyle(document.querySelector('[data-cfx-legend-target-id=\"north/support:A\"] [data-cfx-role=legend-swatch]')).fill]");
        Assert.Equal(colors[1], colors[0]);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal(FirstId, await page.EvaluateAsync<string>("() => window.events[0].targetId"));
        Assert.Equal("node", await page.EvaluateAsync<string>("() => window.events[0].targetKind"));
        Assert.True(await page.EvaluateAsync<bool>("() => window.events[0].point === undefined"));
        Assert.Equal(1, await page.Locator("[data-cfx-target-kind=node].cfx-series-muted").CountAsync());
        Assert.Equal(1, await page.Locator("[data-cfx-role=legend-item][data-cfx-muted=true]").CountAsync());
        await key.BlurAsync(); await MoveAwayAsync(page);
        Assert.Equal(1, await EffectiveOpacity(page.Locator(Node("sibling"))));
        Assert.Equal(1, await EffectiveOpacity(page.Locator(Node("services"))));
        Assert.True(await EffectiveOpacity(page.Locator(Node(FirstId))) < .4);
        await Capture(page, html, "leaf-legend-muted-" + width + "-" + dark);

        await key.FocusAsync(); await page.Keyboard.PressAsync("Enter");
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.events.length"));
        Assert.Equal(0, await page.Locator(".cfx-series-muted").CountAsync());
        await page.Keyboard.PressAsync("i");
        Assert.Equal("true", await key.GetAttributeAsync("data-cfx-isolated"));
        Assert.Equal(1, await EffectiveOpacity(page.Locator(Node(FirstId))));
        Assert.Equal(1, await EffectiveOpacity(page.Locator(Node("services"))));
        Assert.True(await EffectiveOpacity(page.Locator(Node("sibling"))) < .2);
        Assert.True(await EffectiveOpacity(page.Locator(Node("other"))) < .2);
        await Capture(page, html, "leaf-legend-isolated-" + width + "-" + dark);
        await page.Keyboard.PressAsync("i");
        Assert.Equal(0, await page.Locator(".cfx-series-isolated-out,.cfx-series-isolated-in").CountAsync());
        await page.Keyboard.PressAsync("Space");
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal(0, await page.Locator(".cfx-series-muted").CountAsync());
        Assert.True(await page.Locator("[data-cfx-reset]").IsHiddenAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SyncedLeafKeysResolveIDsAcrossReorderAndRenameAndSkipAbsentItems(bool dark) {
        if (!Enabled) return;
        var html = new[] { Model(dark), Model(dark, reordered: true), Model(dark, missing: true) }
            .Select(chart => chart.WithSize(360, 330)).ToInteractiveHtmlDashboardPage(options => {
                options.Interaction.GroupName = "allocation-peers";
                options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
            });
        await using var session = await OpenAsync(html, 1180, 880);
        var page = session.Page; var roots = page.Locator(".cfx-interactive-chart");
        var key = roots.Nth(0).Locator(Key(FirstId)); var renamed = roots.Nth(1).Locator(Key(FirstId));
        Assert.Equal("Customer care", await renamed.GetAttributeAsync("data-cfx-label"));
        Assert.Equal(await key.GetAttributeAsync("data-cfx-target-id"), await renamed.GetAttributeAsync("data-cfx-target-id"));
        await key.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await renamed.GetAttributeAsync("data-cfx-muted"));
        Assert.Equal(1, await roots.Nth(1).Locator("[data-cfx-target-kind=node].cfx-series-muted").CountAsync());
        Assert.Equal(0, await roots.Nth(2).Locator(".cfx-series-muted").CountAsync());
        await page.Keyboard.PressAsync("Space"); await page.Keyboard.PressAsync("i");
        Assert.Equal("true", await renamed.GetAttributeAsync("data-cfx-isolated"));
        Assert.Equal(1, await EffectiveOpacity(roots.Nth(1).Locator(Node(FirstId))));
        Assert.True(await EffectiveOpacity(roots.Nth(1).Locator(Node("sibling"))) < .2);
        Assert.Equal(0, await roots.Nth(2).Locator(".cfx-series-isolated-out").CountAsync());
        await Capture(page, html, "leaf-legend-synced-" + dark);
        await page.Keyboard.PressAsync("i");
        await page.EvaluateAsync("() => document.querySelectorAll('.cfx-interactive-chart')[1].addEventListener('cfxselect', event => window.selection = event.detail.target)");
        await roots.Nth(1).Locator(Node(FirstId)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal(FirstId, await page.EvaluateAsync<string>("() => window.selection.targetId"));
        Assert.Equal("Customer care", await page.EvaluateAsync<string>("() => window.selection.label"));
        Assert.Equal("6", await page.EvaluateAsync<string>("() => window.selection.value"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task LeafKeysHonorTheLegendToggleFeatureGate() {
        if (!Enabled) return;
        var html = Model(false).WithSize(540, 350).ToInteractiveHtmlPage(options => options.Interaction.Features =
            ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Selection);
        await using var session = await OpenAsync(html);
        var key = session.Page.Locator(Key(FirstId)); await key.FocusAsync();
        await session.Page.Keyboard.PressAsync("Space"); await session.Page.Keyboard.PressAsync("i");
        Assert.Equal(0, await session.Page.Locator(".cfx-series-muted,.cfx-series-isolated-in,.cfx-series-isolated-out").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LeafKeysKeepZeroAndTinyRawValuesWithoutInventingPaintedArea(bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithDataLabels().WithPointLegend().WithSize(540, 350)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).AddTreemap("Sizes", new[] {
                new ChartHierarchyItem("large", "Large", value: 1e308),
                new ChartHierarchyItem("tiny", "Tiny", value: 1e-310), new ChartHierarchyItem("zero", "Zero", value: 0)
            });
        var html = chart.ToInteractiveHtmlPage(); await using var session = await OpenAsync(html);
        foreach (var id in new[] { "tiny", "zero" }) {
            var node = session.Page.Locator(Node(id)); var key = session.Page.Locator(Key(id));
            Assert.Equal(0, await node.Locator("rect,path,circle").CountAsync());
            var raw = await node.GetAttributeAsync("data-cfx-value");
            Assert.Equal(raw, await key.GetAttributeAsync("data-cfx-value"));
            await key.FocusAsync();
            Assert.Equal(new[] { id == "tiny" ? "Tiny" : "Zero", "Value", raw }, (await TooltipTextAsync(session.Page)).Split('\n'));
        }
        Assert.NotEqual("0", await session.Page.Locator(Key("tiny")).GetAttributeAsync("data-cfx-value"));
        await Capture(session.Page, html, "leaf-legend-small-values-" + dark);
        await session.Page.Locator(Key("zero")).BlurAsync(); await MoveAwayAsync(session.Page);
        var large = session.Page.Locator(Node("large"));
        var bounds = await large.Locator(":scope > [data-cfx-role=treemap-tile-mark]").BoundingBoxAsync();
        Assert.NotNull(bounds);
        var visible = PngReader.Decode(await session.Page.ScreenshotAsync());
        var label = large.Locator("[data-cfx-role=treemap-label]");
        await label.EvaluateAsync("node => node.style.opacity = '0'");
        var withoutLabel = PngReader.Decode(await session.Page.ScreenshotAsync());
        await label.EvaluateAsync("node => node.style.removeProperty('opacity')");
        // Inspect painted pixels: SVG text bounds include clipped glyphs and cannot prove containment.
        var right = (int)Math.Ceiling(bounds.X + bounds.Width);
        Assert.Equal(0, ChangedPixels(visible, withoutLabel, right + 1, (int)bounds.Y,
            Math.Min(visible.Width, right + 48), (int)bounds.Y + 42));
        Assert.True(ChangedPixels(visible, withoutLabel, (int)bounds.X + 4, (int)bounds.Y,
            right - 4, (int)bounds.Y + 42) > 0);
        await Capture(session.Page, html, "leaf-legend-small-values-bounded-" + dark);
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task LeafKeysOutsideTheNativeLegendBudgetRemainDescriptiveWithoutKeyboardTargets() {
        if (!Enabled) return;
        var chart = Model(false).WithSize(360, 360); chart.Options.LegendMaximumHeightFraction = .03;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var omitted = session.Page.Locator("[data-cfx-role=legend-entry-omitted]");
        Assert.Equal(3, await omitted.CountAsync());
        Assert.Equal(3, await session.Page.Locator("[data-cfx-role=legend-entry-omitted][data-cfx-legend-target-kind=node]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("[data-cfx-role=legend-entry-omitted][tabindex],[data-cfx-role=legend-entry-omitted][data-cfx-target-kind]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 800)]
    public async Task HostPaletteChangesKeepLeafTilesKeysAndTooltipSwatchesConsistent(bool dark, int width) {
        if (!Enabled) return;
        var red = ChartColor.FromRgb(255, 0, 0); var blue = ChartColor.FromRgb(0, 0, 255);
        var missing = ChartColor.FromRgb(13, 47, 81);
        var chart = Chart.Create().WithPointLegend().WithSize(width, 360)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).AddTreemap("Colors", new[] {
                new ChartHierarchyItem("low", "Low", value: 4, colorValue: 0),
                new ChartHierarchyItem("explicit", "Explicit", value: 3, colorValue: 100),
                new ChartHierarchyItem("missing", "Missing", value: 2)
            }).ConfigureTreemap(options => {
                options.ColorScale = ChartColorScale.Sequential(red, blue).WithValueRange(0, 100).WithNoDataColor(missing);
                options.ShowColorScaleLegend = false;
            });
        chart.Series[0].WithPointColor(1, blue);
        chart.Options.SvgColorVariables = new SvgColorVariables().Add("--host-low", red, SvgColorRole.Ramp)
            .Add("--host-explicit", blue, SvgColorRole.Series).Add("--host-missing", missing, SvgColorRole.Ramp);
        var html = chart.ToInteractiveHtmlPage(); await using var session = await OpenAsync(html, width + 24, 490);
        var page = session.Page;
        await page.EvaluateAsync("() => { const root = document.querySelector('.cfx-interactive-chart'); root.style.setProperty('--host-low', '#00ff00'); root.style.setProperty('--host-explicit', '#ff00ff'); root.style.setProperty('--host-missing', '#ffff00'); }");
        foreach (var (id, expected) in new[] { ("low", "rgb(0, 255, 0)"), ("explicit", "rgb(255, 0, 255)"), ("missing", "rgb(255, 255, 0)") }) {
            var tile = page.Locator(Node(id) + " > [data-cfx-role=treemap-tile-mark]");
            var key = page.Locator(Key(id));
            Assert.Equal(expected, await tile.EvaluateAsync<string>("node => getComputedStyle(node).fill"));
            Assert.Equal(expected, await key.Locator("[data-cfx-role=legend-swatch]").EvaluateAsync<string>("node => getComputedStyle(node).fill"));
            await key.FocusAsync();
            Assert.Equal(expected, await page.Locator(".cfx-tooltip__swatch").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
        }
        await Capture(page, html, "leaf-legend-host-palette-" + width + "-" + dark);
        AssertNoConsoleErrors(session);
    }

    private static Task<double> EffectiveOpacity(ILocator node) => node.EvaluateAsync<double>("node => { let opacity = 1; for (let ancestor = node; ancestor && ancestor.tagName !== 'BODY'; ancestor = ancestor.parentElement) opacity *= Number(getComputedStyle(ancestor).opacity); return opacity; }");
    private static int ChangedPixels(RgbaImage first, RgbaImage second, int left, int top, int right, int bottom) {
        var changed = 0;
        for (var y = Math.Max(0, top); y < Math.Min(first.Height, bottom); y++)
            for (var x = Math.Max(0, left); x < Math.Min(first.Width, right); x++) {
                var offset = (y * first.Width + x) * 4;
                if (!first.Pixels.AsSpan(offset, 4).SequenceEqual(second.Pixels.AsSpan(offset, 4))) changed++;
            }
        return changed;
    }
    private static async Task Capture(IPage page, string html, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        var facts = await page.EvaluateAsync<JsonElement>("() => Array.from(document.querySelectorAll('[data-cfx-role=legend-item],[data-cfx-target-kind=node]')).map(node => ({...node.dataset, classes: node.getAttribute('class'), opacity: getComputedStyle(node).opacity}))");
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".runtime.json"), JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }));
    }
}
