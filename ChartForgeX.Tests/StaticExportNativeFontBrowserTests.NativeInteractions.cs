using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StaticExportNativeFontBrowserTests {
    [BrowserTheory]
    [InlineData("color-cbdt1", "word")]
    [InlineData("color-cbdt1", "annotation")]
    [InlineData("color-cbdt1", "legend")]
    [InlineData("bitmap-1-1-1", "word")]
    [InlineData("bitmap-1-1-1", "annotation")]
    [InlineData("bitmap-1-1-1", "legend")]
    public async Task CapturedRasterCaptionKeepsRealInputCuesVisibilityAndDetachedExports(string fixture, string owner) {
        if (!Enabled) return;
        var label = fixture.StartsWith("bitmap", StringComparison.Ordinal) ? "A" : "😀";
        var chart = HtmlNativeLineInkTests.Model(fixture).WithAxes(false).WithHeader(false).WithLegend(owner == "legend");
        chart.ClearSeries();
        if (owner == "word") chart.AddWordCloud(label, new[] { new ChartWordCloudItem(label, 100) });
        else {
            chart.AddLine(label, new[] { new ChartPoint(1, 2), new ChartPoint(2, 4) });
            if (owner == "annotation") chart.AddAnnotation(new ChartAnnotation(ChartAnnotationKind.HorizontalBand,
                2, 3, label, ChartColor.FromHex("#ef4444"), 0));
        }
        var selector = owner switch {
            "word" => "[data-cfx-role=word-cloud-term]",
            "annotation" => "[data-cfx-role=annotation]",
            _ => "[data-cfx-role=legend-item]"
        };
        void Configure(HtmlChartInteractionOptions options) {
            OwnedOptions(options);
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Export | ChartInteractionFeatures.LegendToggles;
            options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            options.Tooltip.PinOnSelect = false; // Keep paint eligibility separate from the retained pinned-readout policy.
        }
        var fragment = chart.ToInteractiveHtmlFragment(Configure);
        await using var session = await OpenAsync(OwnedPage(fragment, "window.FontFace=undefined;"), 760, 700);
        var page = session.Page;
        if (owner == "annotation") await page.AddStyleTagAsync(new() { Content = "[data-cfx-role=annotation-label-backplate]{display:none}" });
        if (owner == "legend") await page.AddStyleTagAsync(new() { Content = "[data-cfx-role=legend-item] :is(rect,circle,ellipse,line,polyline,path,polygon):not([data-cfx-native-text-ink] *){opacity:0!important}" });
        var target = page.Locator(selector).First;
        var id = await target.GetAttributeAsync("data-cfx-target-id");
        var ink = target.Locator("[data-cfx-native-raster-text-ink]").First;
        Assert.Equal(label, await BrowserProofCaptions.TextAsync(ink.Locator("..")));
        Assert.Equal(1, await ink.Locator("image[data-cfx-image-alpha-bounds]").CountAsync());
        var before = await ink.Locator("image").First.EvaluateAsync<string>("node=>node.outerHTML");
        await BrowserProofCaptions.MoveAsync(page, target);
        var observed = await target.EvaluateAsync<string>("""
            node=>{const root=node.closest('.cfx-interactive-chart'),tip=root.querySelector('.cfx-tooltip');return JSON.stringify({owner:node.outerHTML,
                root:Object.fromEntries([...root.attributes].filter(a=>a.name.startsWith('data-cfx-')).map(a=>[a.name,a.value])),
                tooltip:{hidden:tip.hidden,text:tip.innerText},available:ChartForgeXCharts.get(root).resolveTargets(),
                active:document.activeElement?.outerHTML.slice(0,500)});}
            """);
        HtmlOwnedFontDeliveryTests.Save("raster-input-" + fixture + "-" + owner + ".observed.json", observed);
        await page.ScreenshotAsync(new() { Path = CapturePath("raster-input-" + fixture + "-" + owner + ".png") });
        Assert.Contains(label, await TooltipTextAsync(page), StringComparison.Ordinal);
        await BrowserProofCaptions.ClickAsync(page, target);
        if (owner == "legend") Assert.Equal("true", await target.GetAttributeAsync("data-cfx-muted"));
        else {
            Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
            Assert.True(await page.Locator("[data-cfx-state-outline]").CountAsync() > 0);
        }
        Assert.Equal(before, await ink.Locator("image").First.EvaluateAsync<string>("node=>node.outerHTML"));
        await target.BlurAsync(); await MoveAwayAsync(page);
        await ink.EvaluateAsync("node=>node.style.opacity='0'");
        await target.FocusAsync();
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await ink.EvaluateAsync("node=>node.style.removeProperty('opacity')");
        await target.BlurAsync(); await FocusWithKeyboardAsync(page, target);
        Assert.Contains(label, await TooltipTextAsync(page), StringComparison.Ordinal);
        var exported = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", "native-interaction-" + fixture + "-" + owner);
        var svg = XDocument.Parse(exported.Svg);
        Assert.Contains(svg.Descendants(), node => node.Name.LocalName == "image" && node.Attribute("data-cfx-image-alpha-bounds") != null);
        Assert.Contains(label, BrowserProofCaptions.ReadXml(svg.Root!), StringComparison.Ordinal);
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCompletedCompositionDifferentPixels").GetInt32());
        await page.EvaluateAsync("html=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).update(html)",
            chart.ToInteractiveHtmlFragmentWithoutAssets(Configure));
        Assert.Equal(id, await target.GetAttributeAsync("data-cfx-target-id"));
        await BrowserProofCaptions.MoveAsync(page, target);
        Assert.Contains(label, await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Empty(await page.EvaluateAsync<object[]>("()=>ownedFontViolations"));
        AssertNoConsoleErrors(session);
    }
}
