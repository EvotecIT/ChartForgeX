using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HostAccentSurvivesReplacementAndAdaptiveDrawingsWhileDefaultsFollowTheNewTheme(bool hostAccent) {
        if (!Enabled) return;
        void Configure(HtmlChartInteractionOptions options) {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Interaction.ChartId = "host-accent";
        }
        var pageErrors = new List<string>();
        await using var session = await OpenAsync(RuntimePage(), 980, 740, configurePage: page => page.PageError += (_, error) => pageErrors.Add(error));
        var page = session.Page;
        // Apply the host override before mounting, as an embedding host normally would.
        await page.EvaluateAsync("""
            ({html,hostAccent})=>{
                const template=document.createElement('template');template.innerHTML=html;
                const root=template.content.firstElementChild;
                if(hostAccent)root.style.setProperty('--cfx-host-accent','#ff00aa','important');
                document.body.append(root);window.chartController=ChartForgeXCharts.mount(root);
                chartController.select({observationIds:['north']});
            }
            """, new { html = Readings().ToInteractiveHtmlFragmentWithoutAssets(Configure), hostAccent });
        async Task AssertAccent(bool dark, string state) {
            var expected = hostAccent ? "rgb(255, 0, 170)" : Css(VisualTheme.Graphite().Resolve(dark ? VisualThemeMode.Dark : VisualThemeMode.Light).Palette[0]);
            await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-reset]"));
            Assert.Equal(expected, await page.Locator("[data-cfx-reset]").EvaluateAsync<string>("node=>getComputedStyle(node).outlineColor"));
            await FocusWithKeyboardAsync(page, page.Locator(North));
            Assert.Equal(expected, await page.Locator("[data-cfx-focus-ink]").First.EvaluateAsync<string>("node=>getComputedStyle(node).stroke"));
            Assert.Equal(hostAccent ? "important" : "", await page.Locator(Root).EvaluateAsync<string>("root=>root.style.getPropertyPriority('--cfx-host-accent')"));
            Assert.Equal("north", Assert.Single((await page.EvaluateAsync<System.Text.Json.JsonElement>("()=>chartController.getSelection()")).EnumerateArray()).GetProperty("observationId").GetString());
            await CaptureAsync(page, "host-accent-" + hostAccent + "-" + state, new {
                pageErrors, consoleErrors = session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).Select(entry => entry.Text).ToArray()
            });
        }
        await AssertAccent(false, "initial");
        await UpdateAsync(page, Readings(dark: true).ToInteractiveHtmlFragmentWithoutAssets(Configure));
        await AssertAccent(true, "replacement");
        foreach (var width in new[] { 390, 980 }) {
            await page.SetViewportSizeAsync(width, 740);
            await page.WaitForFunctionAsync("drawing=>chartController.root.dataset.cfxAdaptiveDrawing===drawing", width == 390 ? "narrow" : "wide");
            await AssertAccent(true, "resize-" + width);
        }
        Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }

    private static string Css(ChartForgeX.Primitives.ChartColor color) => $"rgb({color.R}, {color.G}, {color.B})";
}
