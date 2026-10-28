using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StaticExportNativeFontBrowserTests {
    [BrowserTheory] [InlineData("color-cbdt1")] [InlineData("bitmap-1-1-1")]
    public async Task AutomaticNonOutlineTextRetainsNativeImageInkAndDetachedExportsUnderTheExistingSelfFontCsp(string fixture) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer();
        var section = renderer.PrepareSection(HtmlNativeLineInkTests.Model(fixture), configure: OwnedOptions);
        var html = OwnedPage("<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + section.Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>", "window.FontFace=undefined;");
        await using var session = await OpenAsync(html, 760, 700); var page = session.Page;
        await page.WaitForFunctionAsync("()=>[...document.querySelectorAll('[data-cfx-native-raster-text-ink] image')].some(image=>image.getBBox().width>0)");
        var observed = await page.EvaluateAsync<JsonElement>("()=>({violations:ownedFontViolations,ink:[...document.querySelectorAll('[data-cfx-native-raster-text-ink]')].map(node=>({display:getComputedStyle(node).display,images:node.querySelectorAll('image').length,text:node.querySelectorAll('text').length})),resources:performance.getEntriesByType('resource').map(row=>row.name)})");
        HtmlOwnedFontDeliveryTests.Save("native-color-" + fixture + ".html", html);
        HtmlOwnedFontDeliveryTests.Save("native-color-" + fixture + ".observed.json", observed.GetRawText());
        var exported = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", "native-color-" + fixture);
        Assert.Empty(observed.GetProperty("violations").EnumerateArray());
        Assert.Contains(observed.GetProperty("ink").EnumerateArray(), item => item.GetProperty("display").GetString() != "none" && item.GetProperty("images").GetInt32() == 1 && item.GetProperty("text").GetInt32() == 0);
        Assert.Contains(XDocument.Parse(exported.Svg).Descendants(), node => node.Name.LocalName == "image" && node.Parent?.Attribute("data-cfx-native-raster-text-ink") != null);
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        Assert.Equal(0, exported.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());
        AssertNoConsoleErrors(session);
    }
}
