using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData("sankey", false)]
    [InlineData("sankey", true)]
    [InlineData("chord", false)]
    [InlineData("chord", true)]
    [InlineData("gauge", false)]
    [InlineData("gauge", true)]
    [InlineData("bullet", false)]
    [InlineData("bullet", true)]
    public async Task NativeLinksAndScalarFactsUsePublicWholeSetCommandsAndSurviveIdentifiedReplacement(string family, bool dark) {
        if (!Enabled) return;
        var initial = NativeKindChart(family, dark, 7);
        var updated = NativeKindChart(family, dark, 11).ToInteractiveHtmlFragmentWithoutAssets(NativeKindOptions);
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(NativeKindOptions), dark ? 390 : 800, 620);
        var page = session.Page;
        var kind = family is "sankey" or "chord" ? "link" : family == "gauge" ? "point" : "series";
        var selector = "[data-cfx-target-kind='" + kind + "'][data-cfx-value='7']";
        var target = page.Locator(selector).First;
        await page.Locator(Root).EvaluateAsync("""
            root=>{
                window.chartController=ChartForgeXCharts.get(root);window.kindEvents=[];
                for(const name of ['cfxselect','cfxselectionchange','cfxsync'])
                    document.body.addEventListener(name,event=>kindEvents.push({name,detail:event.detail}));
            }
            """);
        var result = await target.EvaluateAsync<JsonElement>("""
            node=>{
                window.kindMatch={targets:[{targetKind:node.dataset.cfxTargetKind,targetId:node.dataset.cfxTargetId,seriesKey:node.dataset.cfxSeriesKey}]};
                chartController.select(kindMatch);chartController.highlight(kindMatch,{dim:true});
                return {selection:chartController.getSelection(),selected:node.getAttribute('aria-selected'),
                    highlighted:node.dataset.cfxHighlighted,eventCount:kindEvents.length};
            }
            """);
        AssertNativeKindTarget(Assert.Single(result.GetProperty("selection").EnumerateArray()), kind, "7");
        Assert.Equal("true", result.GetProperty("selected").GetString());
        Assert.Equal("true", result.GetProperty("highlighted").GetString());
        Assert.Equal(0, result.GetProperty("eventCount").GetInt32());
        await page.EvaluateAsync("()=>chartController.clearSelection()");
        await target.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("keyboard", await page.EvaluateAsync<string>("()=>kindEvents.findLast(event=>event.name==='cfxselectionchange').detail.reason"));
        Assert.Equal(kind, await page.EvaluateAsync<string>("()=>kindEvents.findLast(event=>event.name==='cfxselect').detail.target.targetKind"));
        await page.EvaluateAsync("fragment=>chartController.update(fragment)", updated);
        var replacement = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>({selection:chartController.getSelection(),highlights:[...root.querySelectorAll('[data-cfx-highlighted=true]')].map(node=>({
                kind:node.dataset.cfxTargetKind,value:node.dataset.cfxValue,scope:node.dataset.cfxSeriesScope}))})
            """);
        AssertNativeKindTarget(Assert.Single(replacement.GetProperty("selection").EnumerateArray()), kind, "11");
        var highlight = Assert.Single(replacement.GetProperty("highlights").EnumerateArray());
        Assert.Equal(kind, highlight.GetProperty("kind").GetString());
        Assert.Equal("11", highlight.GetProperty("value").GetString());
        Assert.Equal("shared", highlight.GetProperty("scope").GetString());
        await page.EvaluateAsync("""
            ()=>{chartController.clearSelection();chartController.clearHighlight();chartController.root.dispatchEvent(
                new CustomEvent('cfx-select',{detail:{match:kindMatch,emit:true}}));}
            """);
        Assert.Equal("command", await page.EvaluateAsync<string>("()=>kindEvents.findLast(event=>event.name==='cfxselectionchange').detail.reason"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>chartController.getSelection().length"));
        Assert.Equal(0, await page.Locator(".cfx-highlighted,.cfx-linked-dim").CountAsync());
        await CaptureAsync(page, "native-kind-" + family + "-" + dark, replacement);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicAnnotationSelectionAndLassoContainNativeFactsWithoutSeriesWrappersOrPassiveGuidance(bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithLegend(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("observations"), "Observed", new[] { new ChartPoint(3, 4), new ChartPoint(7, 7) }))
            .AddHorizontalLine(6, "Reference guide")
            .AddPlotRegion(new(ChartColor.FromHex("#5c9e75"), label: "Background guide", opacity: .2));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(NativeKindOptions), dark ? 390 : 800, 620);
        var page = session.Page;
        await page.Locator(Root).EvaluateAsync("""
            root=>{window.chartController=ChartForgeXCharts.get(root);window.kindEvents=[];
                document.body.addEventListener('cfxselectionchange',event=>kindEvents.push(event.detail));
                const node=root.querySelector('[data-cfx-role=annotation]');
                chartController.select({targets:[{targetKind:node.dataset.cfxTargetKind,targetId:node.dataset.cfxTargetId}]});}
            """);
        var selected = await page.EvaluateAsync<JsonElement>("()=>chartController.getSelection()");
        var annotation = Assert.Single(selected.EnumerateArray());
        Assert.Equal("annotation", annotation.GetProperty("targetKind").GetString());
        Assert.Equal("6", annotation.GetProperty("value").GetString());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>kindEvents.length"));
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        var stage = await BoxAsync(page, "[data-cfx-role=plot-region-fill]");
        await page.Mouse.MoveAsync((float)(stage.X + 3), (float)(stage.Y + 3));
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(stage.X + stage.Width - 3), (float)(stage.Y + stage.Height - 3), new MouseMoveOptions { Steps = 8 });
        await page.Mouse.UpAsync();
        var lasso = await page.EvaluateAsync<JsonElement>("()=>({selection:chartController.getSelection(),event:kindEvents.at(-1)})");
        await CaptureAsync(page, "native-annotation-lasso-" + dark, lasso);
        var targets = lasso.GetProperty("selection").EnumerateArray().ToArray();
        Assert.Equal(3, targets.Length);
        Assert.Equal(2, targets.Count(item => item.GetProperty("targetKind").GetString() == "point"));
        Assert.Single(targets, item => item.GetProperty("targetKind").GetString() == "annotation");
        Assert.Equal("lasso", lasso.GetProperty("event").GetProperty("reason").GetString());
        Assert.Equal(3, lasso.GetProperty("event").GetProperty("targets").GetArrayLength());
        Assert.Equal(0, await page.Locator("[data-cfx-passive] .cfx-selected,[data-cfx-role=series].cfx-selected").CountAsync());
        AssertNoConsoleErrors(session);
    }

    private static void AssertNativeKindTarget(JsonElement target, string kind, string value) {
        Assert.Equal(kind, target.GetProperty("targetKind").GetString());
        Assert.Equal("facts", target.GetProperty("seriesKey").GetString());
        Assert.Equal("shared", target.GetProperty("seriesScope").GetString());
        Assert.Equal(value, target.GetProperty("value").GetString());
    }

    private static Chart NativeKindChart(string family, bool dark, double value) {
        var id = new ChartSeriesId("facts");
        var nodes = new[] { new ChartNode("source", "Source"), new ChartNode("destination", "Destination") };
        var links = new[] { new ChartFlowLink("flow", "source", "destination", value) };
        var series = family switch {
            "sankey" => ChartSeries.CreateSankey(id, "Transfers", nodes, links),
            "chord" => ChartSeries.CreateChord(id, "Transfers", nodes, links),
            "gauge" => ChartSeries.CreateGauge(id, "Capacity", value),
            _ => ChartSeries.CreateBullet(id, "Capacity", value, 20)
        };
        if (family == "gauge") series.WithObservationId(0, new ChartObservationId("capacity"));
        return Chart.Create().WithSize(640, 360).WithLegend()
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(series);
    }

    private static void NativeKindOptions(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Tooltip.Mode = HtmlChartTooltipMode.Single;
        options.Tooltip.PinOnSelect = false;
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
            | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Brush;
    }
}
