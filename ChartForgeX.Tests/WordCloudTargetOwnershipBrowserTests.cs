using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Word-cloud source inventory remains descriptive while painted terms own public targets.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed partial class WordCloudTargetOwnershipBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicSelectionResolvesOnePaintedOwnerPerTermInMultipleAndSingleModes(bool single) {
        if (!Enabled) return;
        var chart = Frame().AddWordCloud("Topics", new[] {
            new ChartWordCloudItem("Visible", 100), new ChartWordCloudItem("Companion", 50)
        });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => Options(options, single)), 800, 600);
        var page = session.Page;
        await BrowserProofCaptions.InstallNativeAsync(page);
        await page.AddStyleTagAsync(new() { Content = ".cfx-stage svg,.cfx-stage svg *{filter:none!important}" });
        var resolved = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                window.wordRoot=document.querySelector('.cfx-interactive-chart');
                window.wordController=ChartForgeXCharts.get(wordRoot);
                window.wordTargets=wordController.resolveTargets();
                window.wordNativeText=[...wordRoot.querySelectorAll('[data-cfx-role=word-cloud-term] [data-cfx-native-text-ink], [data-cfx-role=word-cloud-term] text')].map(node=>window.cfxCaptionSourceInk(node));
                return wordTargets;
            }
            """);
        var targets = resolved.EnumerateArray().ToArray();
        Assert.Equal(2, targets.Length);
        Assert.All(targets, target => {
            Assert.Equal("point", target.GetProperty("targetKind").GetString());
            Assert.Equal("word-cloud-term", target.GetProperty("role").GetString());
        });
        Assert.Equal(2, targets.Select(target => target.GetProperty("targetId").GetString()).Distinct().Count());
        await page.EvaluateAsync("()=>wordController.select({targets:[wordTargets[0]]})");
        await AssertSelectionAsync(page, 1);
        Assert.Equal(targets[0].GetProperty("targetId").GetString(), Assert.Single(
            (await SelectionAsync(page)).EnumerateArray()).GetProperty("targetId").GetString());

        await page.EvaluateAsync("()=>wordController.select({targets:[wordTargets[1]]},{mode:'add'})");
        await AssertSelectionAsync(page, single ? 1 : 2);
        var selection = (await SelectionAsync(page)).EnumerateArray().ToArray();
        if (single) Assert.Equal(targets[1].GetProperty("targetId").GetString(), Assert.Single(selection).GetProperty("targetId").GetString());
        else Assert.Equal(targets.Select(target => target.GetProperty("targetId").GetString()),
            selection.Select(target => target.GetProperty("targetId").GetString()));
        Assert.True(await page.EvaluateAsync<bool>("()=>JSON.stringify(wordNativeText)===JSON.stringify([...wordRoot.querySelectorAll('[data-cfx-role=word-cloud-term] [data-cfx-native-text-ink], [data-cfx-role=word-cloud-term] text')].map(node=>window.cfxCaptionSourceInk(node)))"));
        await page.EvaluateAsync("()=>wordController.clearSelection()");
        await AssertSelectionAsync(page, 0);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task LimitedAndZeroWeightTermsRetainExactInventoryWithoutBecomingInteractiveDuplicates() {
        if (!Enabled) return;
        var chart = Frame().WithWordCloudMaximumTerms(1).AddWordCloud("Topics", new[] {
            new ChartWordCloudItem("Visible", 100), new ChartWordCloudItem("Limited", 50), new ChartWordCloudItem("Zero", 0)
        });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => Options(options, false)), 800, 600);
        var page = session.Page;
        var inventory = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                window.wordRoot=document.querySelector('.cfx-interactive-chart');
                window.wordController=ChartForgeXCharts.get(wordRoot);
                const sources=[...wordRoot.querySelectorAll('[data-cfx-role=word-cloud-source]')];
                const regions=JSON.parse(wordRoot.dataset.cfxPreparedChart).regions.filter(region=>region.role==='word-cloud-source');
                const targets=wordController.resolveTargets(),key=targets[0].seriesKey;
                wordController.select({seriesKeys:[key]});
                return {sources:sources.map(node=>({label:node.dataset.cfxText,value:Number(node.dataset.cfxValue),
                        point:node.dataset.cfxSourcePoint,placed:node.dataset.cfxPlaced})),regions,
                    targets,omitted:wordController.resolveTargets({sourcePoints:[{seriesKey:key,index:1},{seriesKey:key,index:2}]}),
                    sourceHitAreas:sources.reduce((count,node)=>count+node.querySelectorAll('[data-cfx-browser-hit-area]').length,0)};
            }
            """);
        var sources = inventory.GetProperty("sources").EnumerateArray().ToArray();
        Assert.Equal(new[] { "Visible", "Limited", "Zero" }, sources.Select(source => source.GetProperty("label").GetString()));
        Assert.Equal(new[] { 100d, 50d, 0d }, sources.Select(source => source.GetProperty("value").GetDouble()));
        Assert.Equal(new[] { "0", "1", "2" }, sources.Select(source => source.GetProperty("point").GetString()));
        Assert.Equal(new[] { "true", "false", "false" }, sources.Select(source => source.GetProperty("placed").GetString()));
        var regions = inventory.GetProperty("regions").EnumerateArray().ToArray();
        Assert.Equal(3, regions.Length);
        Assert.Contains(regions, region => region.GetProperty("label").GetString()!.Contains("Limited", StringComparison.Ordinal));
        Assert.Contains(regions, region => region.GetProperty("label").GetString()!.Contains("Zero", StringComparison.Ordinal));
        Assert.Equal("word-cloud-term", Assert.Single(inventory.GetProperty("targets").EnumerateArray()).GetProperty("role").GetString());
        Assert.Empty(inventory.GetProperty("omitted").EnumerateArray());
        Assert.Equal(0, inventory.GetProperty("sourceHitAreas").GetInt32());
        await AssertSelectionAsync(page, 1);
        AssertNoConsoleErrors(session);
    }

    private static Chart Frame() => Chart.Create().WithSize(640, 360).WithTitle("Word-cloud target ownership")
        .WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark);

    private static void Options(HtmlChartInteractionOptions options, bool single) {
        options.Interaction.Features = ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation;
        options.Interaction.SelectionMode = single ? ChartSelectionMode.Single : ChartSelectionMode.Toggle;
        options.Tooltip.PinOnSelect = false;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
    }

    private static Task<JsonElement> SelectionAsync(IPage page) => page.EvaluateAsync<JsonElement>("()=>wordController.getSelection()");

    private static async Task AssertSelectionAsync(IPage page, int expected) {
        await page.WaitForFunctionAsync("expected=>wordRoot.querySelectorAll('[data-cfx-state-outline]').length===expected", expected);
        var selection = (await SelectionAsync(page)).EnumerateArray().ToArray();
        Assert.Equal(expected, selection.Length);
        Assert.All(selection, target => Assert.Equal("word-cloud-term", target.GetProperty("role").GetString()));
        Assert.Equal(expected, await page.Locator("[data-cfx-role=word-cloud-term][aria-selected=true]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role=word-cloud-source][aria-selected=true]").CountAsync());
    }
}
