using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    [BrowserTheory]
    [InlineData(false, 980)]
    [InlineData(true, 375)]
    public async Task WrappedLegendButtonsOwnTheirLabelAndDoNotOverlap(bool dark, int width) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(400, 400).WithPointLegend().WithDataLabels(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXLabels("Subscriptions", "Professional services", "Platform support", "Other")
            .AddSeries(new ChartSeries(new ChartSeriesId("revenue"), "Revenue", ChartSeriesKind.Donut, ChartPoints.FromValues(55, 25, 15, 5)));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width, 812);
        var page = session.Page;
        var subscriptions = page.Locator("[data-cfx-role=legend-item]:visible[data-cfx-label='Subscriptions']");
        var other = page.Locator("[data-cfx-role=legend-item]:visible[data-cfx-label='Other']");
        Assert.Equal("Subscriptions", await BrowserProofCaptions.TextAsync(subscriptions));
        Assert.Equal("Other", await BrowserProofCaptions.TextAsync(other));
        Assert.Equal("button", await subscriptions.GetAttributeAsync("role"));
        await subscriptions.ClickAsync();
        Assert.Equal("false", await subscriptions.GetAttributeAsync("aria-pressed"));
        Assert.Equal("true", await other.GetAttributeAsync("aria-pressed"));
        var boxes = await page.Locator("[data-cfx-role=legend-item]:visible").EvaluateAllAsync<JsonElement>("nodes=>nodes.map(node=>{const b=node.getBoundingClientRect();return{x:b.x,y:b.y,right:b.right,bottom:b.bottom,width:b.width,height:b.height}})");
        var entries = boxes.EnumerateArray().ToArray();
        Assert.All(entries, box => Assert.True(box.GetProperty("height").GetDouble() >= (width < 560 ? 44 : 24) - .1));
        for (var first = 0; first < entries.Length; first++) for (var second = first + 1; second < entries.Length; second++) {
            var a = entries[first]; var b = entries[second];
            Assert.False(a.GetProperty("x").GetDouble() < b.GetProperty("right").GetDouble() - .1
                && a.GetProperty("right").GetDouble() > b.GetProperty("x").GetDouble() + .1
                && a.GetProperty("y").GetDouble() < b.GetProperty("bottom").GetDouble() - .1
                && a.GetProperty("bottom").GetDouble() > b.GetProperty("y").GetDouble() + .1, "Legend buttons must have disjoint hit boxes.");
        }
        await CaptureAsync(page, "ux-wrapped-legend-" + width + "-" + dark);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavedHierarchyDrillsIntoABranchAndBackRestoresTheOriginalDrawing(bool sunburst) {
        if (!Enabled) return;
        var items = new[] { new ChartHierarchyItem("all", "All teams"), new ChartHierarchyItem("engineering", "Engineering", "all"),
            new ChartHierarchyItem("platform", "Support", "engineering", .2), new ChartHierarchyItem("support", "Support", "engineering", .3),
            new ChartHierarchyItem("operations", "Operations", "all", .5) };
        var series = sunburst ? ChartSeries.CreateSunburst(new ChartSeriesId("work"), "Allocation", items)
            : ChartSeries.CreateTreemap(new ChartSeriesId("work"), "Allocation", items);
        var chart = Chart.Create().WithSize(800, 440).WithTheme(VisualTheme.Graphite(), VisualThemeMode.Light).AddSeries(series);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        var original = await page.Locator(".cfx-stage > svg").GetAttributeAsync("viewBox");
        var branch = page.Locator("[data-cfx-node=engineering]");
        await branch.ScrollIntoViewIfNeededAsync();
        var hit = await NativeInteriorPointAsync(page, "[data-cfx-node=engineering]", sunburst ? "[data-cfx-role=sunburst-segment-mark]" : "[data-cfx-role=treemap-group-mark]", 2);
        Assert.Equal("engineering", await page.EvaluateAsync<string>("p=>document.elementFromPoint(p[0],p[1]).closest('[data-cfx-node]')?.dataset.cfxNode", hit));
        await ClickNativePointAsync(branch, hit);
        Assert.True(await page.Locator("svg[data-cfx-standalone-hierarchy=engineering]").IsVisibleAsync());
        Assert.Contains("Engineering", await page.Locator(".cfx-hierarchy-breadcrumb").InnerTextAsync());
        await MoveToAsync(page, "svg[data-cfx-standalone-hierarchy] [data-cfx-node=platform]");
        Assert.Contains("Engineering", await TooltipTextAsync(page));
        Assert.Contains("% of parent", await TooltipTextAsync(page));
        if (sunburst) {
            var box = await page.Locator("svg[data-cfx-standalone-hierarchy] [data-cfx-node=platform] path").BoundingBoxAsync();
            Assert.NotNull(box);
            // A 40% arc spans the right side of its ring and finishes below the centre.
            var view = await page.Locator("svg[data-cfx-standalone-hierarchy]").BoundingBoxAsync();
            Assert.True(box!.Y + box.Height > view!.Y + view.Height / 2);
        }
        await page.Locator("svg[data-cfx-standalone-hierarchy] [data-cfx-node=platform]").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("platform", await page.EvaluateAsync<string>("()=>controller.getSelection()[0].targetId"));
        await CaptureAsync(page, "ux-hierarchy-" + sunburst);
        await page.Locator(".cfx-hierarchy-breadcrumb button").First.ClickAsync();
        Assert.Equal(0, await page.Locator("svg[data-cfx-standalone-hierarchy]").CountAsync());
        Assert.True(await page.Locator(".cfx-stage > svg").IsVisibleAsync());
        Assert.Equal(original, await page.Locator(".cfx-stage > svg").GetAttributeAsync("viewBox"));
        Assert.Equal("platform", await page.EvaluateAsync<string>("()=>controller.getSelection()[0].targetId"));
        Assert.Contains("Engineering", await page.Locator("[data-cfx-compare-tray]").InnerTextAsync());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task DataMenuDownloadsBothFormatsAndInvokesHostActionsWithData() {
        if (!Enabled) return;
        var series = new ChartSeries(new ChartSeriesId("requests"), "=Requests", ChartSeriesKind.Line, ChartPoints.FromValues(-2, 7, 4));
        series.WithObservationId(1, new ChartObservationId("tuesday"));
        var chart = Chart.Create().WithSize(640, 360).WithXLabels("=1+1", "\t@Tuesday", "-Wednesday").AddSeries(series);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.IncludeContextMenu = true; options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.Zoom);
        }), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        await page.EvaluateAsync("()=>controller.setContextMenuItems([{id:'inspect',label:'Inspect selected data',action:context=>window.inspectedRows=context.data}])");
        var next = chart.ToInteractiveHtmlFragmentWithoutAssets(options => {
            options.IncludeContextMenu = true; options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.Zoom);
        });
        await page.EvaluateAsync("html=>controller.update(html)", next);
        await page.EvaluateAsync("()=>controller.setXWindow(1.75,2.25)");
        Assert.Equal("false", await page.Locator("[data-cfx-point='0'][data-cfx-series='0']").GetAttributeAsync("data-cfx-visible"));
        foreach (var format in new[] { "csv", "json" }) {
            await page.Locator(".cfx-stage").ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
            var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-context-action=" + format + "]").ClickAsync());
            Assert.EndsWith("." + format, download.SuggestedFilename);
            var path = await download.PathAsync();
            Assert.NotNull(path);
            var content = await File.ReadAllTextAsync(path!);
            Assert.Contains("Requests", content);
            if (format == "json") {
                var rows = JsonDocument.Parse(content).RootElement;
                Assert.Equal(3, rows.GetArrayLength());
                Assert.Equal(7, rows[1].GetProperty("value").GetDouble());
                Assert.Equal("=1+1", rows[0].GetProperty("category").GetString());
                Assert.Equal("requests", rows[1].GetProperty("seriesKey").GetString());
                Assert.Equal("tuesday", rows[1].GetProperty("observationId").GetString());
            } else {
                Assert.Contains("\"7\"", content); Assert.Contains("\"-2\"", content);
                Assert.Contains("\"'=1+1\"", content); Assert.Contains("\"'\t@Tuesday\"", content);
                Assert.Contains("\"'-Wednesday\"", content); Assert.Contains("\"'=Requests\"", content);
            }
        }
        // Clipboard is an OS boundary shared with the workstation. Capture the
        // browser write here while exercising the real menu and export formatter.
        await page.EvaluateAsync("()=>Object.defineProperty(navigator,'clipboard',{configurable:true,value:{writeText:async value=>{window.copiedChartData=value;}}})");
        await page.Locator(".cfx-stage").ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await page.Locator("[data-cfx-context-action=copy-json]").ClickAsync();
        var copied = JsonDocument.Parse(await page.EvaluateAsync<string>("()=>window.copiedChartData")).RootElement;
        Assert.Equal(3, copied.GetArrayLength());
        Assert.Equal(7, copied[1].GetProperty("value").GetDouble());
        await page.Locator(".cfx-stage").ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await page.Locator("[data-cfx-context-action=inspect]").ClickAsync();
        Assert.Equal(3, await page.EvaluateAsync<int>("()=>window.inspectedRows.length"));
        await page.EvaluateAsync("()=>controller.dispose()");
        await MountAsync(page);
        await page.Locator(".cfx-stage").ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        Assert.Equal(0, await page.Locator("[data-cfx-context-action=inspect]").CountAsync());
        AssertNoConsoleErrors(session);
    }
}
