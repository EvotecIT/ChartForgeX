using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveSunburstBranchHighlightBrowserTests {
    [Theory]
    [InlineData(360, false, false, false)]
    [InlineData(360, true, false, false)]
    [InlineData(800, false, false, false)]
    [InlineData(800, true, false, false)]
    [InlineData(360, false, true, false)]
    [InlineData(360, true, true, false)]
    [InlineData(800, false, true, false)]
    [InlineData(800, true, true, false)]
    [InlineData(360, true, false, true)]
    [InlineData(800, false, true, true)]
    public async Task PointerFocusAndSelectionComposeSparsePaintWithoutChangingNativeInk(int width, bool dark, bool graphite, bool composed) {
        if (!Enabled) return;
        var chart = SunburstBranchHighlightExamples.CreateChart(width, dark, graphite);
        if (composed) {
            chart.WithHeader(false).WithLegend(false);
            RoundedRadialSeriesTests.UseFixtureFont(chart);
            chart.ConfigureSunburst(options => {
                options.CornerRadius = 6;
                options.ShowColorScaleLegend = false;
                options.SecondaryLabelFormatter = context => context.Item.Id == "engineering-support" ? "Delivery team" : context.Item.Id == "engineering" ? "Delivery" : null;
            });
        }
        var html = chart.ToInteractiveHtmlPage(options => {
            Configure(options);
            if (composed) {
                options.Tooltip.DelayMilliseconds = 60;
                options.Tooltip.Position.Anchor = width < 500 ? HtmlChartTooltipAnchor.Node : HtmlChartTooltipAnchor.Pointer;
            }
        });
        if (composed) {
            var font = Convert.ToBase64String(await File.ReadAllBytesAsync(chart.Options.PngFontPath!));
            html = html.Replace("</head>", "<style>@font-face{font-family:'Carlito';src:url(data:font/ttf;base64," + font + ")}</style></head>", StringComparison.Ordinal);
        }
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 540 : 650);
        var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var ink = await NativeInk(page, "engineering-support");
        Assert.NotEqual("none", ink[3]); Assert.True(int.Parse(ink[5], System.Globalization.CultureInfo.InvariantCulture) > 0);
        await MoveToNative(page, Node("engineering-support"));
        await WaitState(page, "engineering-support", "item");
        Assert.Equal(new[] { "1", "0.55", "0.55", "0", "1" }, await PaintOpacities(page, "engineering-support", "platform", "engineering", "operations-support", "all"));
        Assert.Equal(ink, await NativeInk(page, "engineering-support"));
        Assert.Equal("rgb(20, 180, 230)", await page.Locator(Node("engineering-support") + " [data-cfx-highlight-outline]").EvaluateAsync<string>("node => getComputedStyle(node).stroke"));
        Assert.Equal(0, await page.Locator(Node("operations-support") + " [data-cfx-highlight-outline]").EvaluateAsync<double>("node => parseFloat(getComputedStyle(node).strokeWidth)"));
        Assert.Equal("rgba(0, 0, 0, 0)", await page.Locator(Node("platform") + " [data-cfx-highlight-outline]").EvaluateAsync<string>("node => getComputedStyle(node).stroke"));
        Assert.True(await page.Locator("[data-cfx-highlight-outline]").EvaluateAllAsync<bool>("nodes => nodes.every(n => !n.id && !n.hasAttribute('tabindex') && !n.hasAttribute('data-cfx-target-id') && getComputedStyle(n).pointerEvents === 'none')"));
        if (composed) {
            await page.WaitForFunctionAsync("() => { const tip=document.querySelector('.cfx-tooltip'); return !tip.hidden && tip.innerText.includes('Delivery team'); }");
            Assert.Equal("Delivery team", await page.Locator(".cfx-tooltip__secondary").TextContentAsync());
            Assert.Equal(await page.Locator(Node("engineering-support") + " [data-cfx-role=sunburst-segment-mark]").GetAttributeAsync("d"),
                await page.Locator(Node("engineering-support") + " [data-cfx-highlight-outline]").GetAttributeAsync("d"));
            if (width == 800) Assert.True(await page.Locator("[data-cfx-role=sunburst-secondary-label]").CountAsync() > 0);
        }
        var composition = composed ? "-composed" : "";
        await Capture(session, html, $"branch-{width}-{(dark ? "dark" : "light")}-{(graphite ? "graphite" : "report")}{composition}");
        // Adapter opacity zero must not turn another native branch into an unreachable target.
        await MoveToNative(page, Node("operations-support"));
        await WaitState(page, "operations-support", "item");
        Assert.Equal("0", (await PaintOpacities(page, "engineering-support")).Single());
        await page.Locator(Node("engineering-support")).FocusAsync();
        await WaitState(page, "engineering-support", "item");
        await page.Keyboard.PressAsync("Enter");
        Assert.Equal("true", await page.Locator(Node("engineering-support")).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        await MoveToNative(page, Node("operations-support"));
        await WaitState(page, "operations-support", "item");
        Assert.Equal("1", (await PaintOpacities(page, "engineering-support")).Single());
        Assert.Contains("drop-shadow", await page.Locator(Node("engineering-support") + " > [data-cfx-highlight-paint]").EvaluateAsync<string>("node => getComputedStyle(node).filter"), StringComparison.Ordinal);
        Assert.Contains("25", await TooltipTextAsync(page), StringComparison.Ordinal);
        if (width == 360 && dark && !graphite || width == 800 && !dark && graphite)
            await Capture(session, html, $"branch-selected-pinned-{width}-{(dark ? "dark-report" : "light-graphite")}{composition}");
        await page.Keyboard.PressAsync("Escape");
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hovering"));
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("node => node.hidden"));
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal(0, await page.Locator(".cfx-selected,[data-cfx-highlight-state],[data-cfx-highlight-outline]").CountAsync());
        Assert.Equal(ink, await NativeInk(page, "engineering-support"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("source-opacity", true, HtmlChartTooltipAnchor.Node)]
    [InlineData("primitive-opacity", true, HtmlChartTooltipAnchor.Node)]
    [InlineData("primitive-opacity", false, HtmlChartTooltipAnchor.Node)]
    [InlineData("display", true, HtmlChartTooltipAnchor.Node)]
    [InlineData("clip", true, HtmlChartTooltipAnchor.Node)]
    [InlineData("primitive-opacity", true, HtmlChartTooltipAnchor.Pointer)]
    [InlineData("primitive-opacity", false, HtmlChartTooltipAnchor.Pointer)]
    public async Task CurrentHostHiddenPaintStaysUnavailableAfterHighlightAndRecovers(string hidden, bool keyboard, HtmlChartTooltipAnchor anchor) {
        if (!Enabled) return;
        var html = SunburstBranchHighlightExamples.CreateChart().ToInteractiveHtmlPage(options => {
            Configure(options);
            options.Tooltip.Position.Anchor = anchor;
            if (!keyboard) options.Interaction.Disable(ChartInteractionFeatures.KeyboardNavigation);
        });
        await using var session = await OpenAsync(html, 850, 650);
        var page = session.Page;
        await MoveToNative(page, Node("platform"));
        await WaitState(page, "platform", "item");
        await page.EvaluateAsync("""
            hidden => {
                const source=document.querySelector('[data-cfx-node="platform"]');
                if (hidden === 'source-opacity') source.style.opacity='0';
                if (hidden === 'primitive-opacity') {
                    const style=document.createElement('style'); style.id='host-paint';
                    style.textContent='[data-cfx-node="platform"] [data-cfx-role="sunburst-segment-mark"], [data-cfx-node="platform"] [data-cfx-role="sunburst-pattern"] { opacity:0 !important }';
                    document.head.appendChild(style); source.style.setProperty('--host-update', '1');
                }
                if (hidden === 'display') source.style.display='none';
                if (hidden === 'clip') {
                    const ns='http://www.w3.org/2000/svg', clip=document.createElementNS(ns,'clipPath'), rect=document.createElementNS(ns,'rect');
                    clip.id='host-empty-clip'; clip.setAttribute('clipPathUnits','userSpaceOnUse'); rect.setAttribute('width','0'); rect.setAttribute('height','0');
                    clip.appendChild(rect); source.ownerSVGElement.querySelector('defs').appendChild(clip); source.setAttribute('clip-path','url(#host-empty-clip)');
                }
            }
            """, hidden);
        await page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-node=platform]')?.hasAttribute('data-cfx-highlight-unavailable')");
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-tooltip').hidden");
        Assert.Equal(0, await page.Locator(Node("platform") + " [data-cfx-highlight-outline]:visible").CountAsync());
        if (hidden == "primitive-opacity" && !keyboard) await Capture(session, html, "branch-host-hidden-native-no-keyboard-" + anchor);
        await page.Locator(Node("platform")).EvaluateAsync("node => node.dispatchEvent(new PointerEvent('pointerenter', { bubbles:false, clientX:400, clientY:300 }))");
        Assert.NotEqual("item", await page.Locator(Node("platform")).GetAttributeAsync("data-cfx-highlight-state"));
        if (keyboard) {
            Assert.Equal("-1", await page.Locator(Node("platform")).GetAttributeAsync("tabindex"));
            await page.Locator(Node("platform")).FocusAsync();
            Assert.NotEqual("item", await page.Locator(Node("platform")).GetAttributeAsync("data-cfx-highlight-state"));
        }
        await page.EvaluateAsync("""
            () => { const source=document.querySelector('[data-cfx-node="platform"]');
                source.removeAttribute('style'); source.removeAttribute('clip-path'); document.getElementById('host-paint')?.remove(); }
            """);
        await MoveAwayAsync(page);
        await MoveToNative(page, Node("platform"));
        await WaitState(page, "platform", "item");
        Assert.False(await page.Locator(".cfx-interactive-chart").EvaluateAsync<bool>("root => root.hasAttribute('data-cfx-highlight-probe')"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SynchronizedDashboardResolvesLocalBranchesAndMixedFamiliesKeepTheirOwnEmphasis() {
        if (!Enabled) return;
        var first = SunburstBranchHighlightExamples.CreateChart(360);
        var second = Chart.Create().WithSize(360, 360).AddSunburst("Renamed allocation", first.Series[0].HierarchyItems.Reverse());
        first.Series[0].WithInteractionKey("allocation"); second.Series[0].WithInteractionKey("allocation");
        var line = StatusLines(false);
        var html = new[] { first, second, line }.ToInteractiveHtmlDashboardPage(options => {
            options.Highlight.Mode = HtmlChartHighlightMode.RootBranch;
            options.Highlight.UnhighlightedBranch.Opacity = 0;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Columns = 1;
        });
        await using var session = await OpenAsync(html, 800, 1000);
        var page = session.Page;
        await page.Locator(".cfx-interactive-chart").Nth(0).Locator(Node("platform")).FocusAsync();
        await page.WaitForFunctionAsync("() => document.querySelectorAll('.cfx-interactive-chart')[1].querySelector('[data-cfx-node=platform]')?.dataset.cfxHighlightState === 'item'");
        var peer = page.Locator(".cfx-interactive-chart").Nth(1);
        Assert.Equal("branch", await peer.Locator(Node("engineering-support")).GetAttributeAsync("data-cfx-highlight-state"));
        Assert.Equal("other-branch", await peer.Locator(Node("operations-support")).GetAttributeAsync("data-cfx-highlight-state"));
        await Capture(session, html, "branch-synchronized-reordered-dashboard");
        Assert.Equal(0, await page.Locator(".cfx-interactive-chart").Nth(2).Locator("[data-cfx-highlight-managed]").CountAsync());
        await page.Locator(".cfx-interactive-chart").Nth(0).Locator(Node("all")).FocusAsync();
        Assert.Equal(new[] { "1", "1" }, await PaintOpacities(page, "platform", "operations-support"));
        await peer.Locator(Node("platform")).EvaluateAsync("node => node.style.opacity='0'");
        await page.Locator(".cfx-interactive-chart").Nth(0).Locator(Node("platform")).FocusAsync();
        await WaitState(page, "platform", "item");
        Assert.NotEqual("item", await peer.Locator(Node("platform")).GetAttributeAsync("data-cfx-highlight-state"));
        await page.Locator(".cfx-interactive-chart").Nth(0).Locator(Node("reserve")).FocusAsync();
        await WaitState(page, "reserve", "item");
        Assert.Contains("0", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Equal(0, await page.Locator(Node("reserve") + " [data-cfx-role=sunburst-segment-mark]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultBranchStylesAndLegendTargetsKeepTheirPresentation(bool graphite) {
        if (!Enabled) return;
        var chart = SunburstBranchHighlightExamples.CreateChart(graphite: graphite).WithLegend().ConfigureSunburst(options => options.ShowColorScaleLegend = false);
        var html = chart.ToInteractiveHtmlPage(options => options.Highlight.Mode = HtmlChartHighlightMode.RootBranch);
        await using var session = await OpenAsync(html, 850, 650);
        var page = session.Page;
        await page.Locator(Node("platform")).FocusAsync();
        await WaitState(page, "platform", "item");
        Assert.Equal(new[] { "1", graphite ? "1" : "0.82", graphite ? "0.3" : "0.38", "1" }, await PaintOpacities(page, "platform", "engineering-support", "operations-support", "all"));
        await page.Locator("[data-cfx-role=legend-item]").First.FocusAsync();
        Assert.NotEqual("root-branch", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-mode"));
        Assert.Equal(0, await page.Locator("[data-cfx-highlight-state]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FragmentsAndExternalAssetsUseTheSameRuntime(bool externalAssets) {
        if (!Enabled) return;
        var chart = SunburstBranchHighlightExamples.CreateChart();
        var html = externalAssets
            ? chart.ToInteractiveHtmlPage(options => { Configure(options); options.ExternalAssets = new HtmlAssetReferences("https://assets.example.test/"); })
            : "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style></head><body>"
                + chart.ToInteractiveHtmlFragmentWithoutAssets(Configure) + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", 850, 650);
        var errors = new List<string>(); session.Page.PageError += (_, error) => errors.Add(error);
        if (externalAssets) await session.Page.RouteAsync("https://assets.example.test/**", async route => {
            var asset = HtmlInteractiveAssetFiles.Charts().Single(file => route.Request.Url.EndsWith(file.FileName, StringComparison.Ordinal));
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = asset.ContentType, Body = asset.Content });
        });
        await session.Page.SetContentAsync(html);
        Assert.Empty(errors);
        await session.Page.Locator(Node("platform")).FocusAsync();
        Assert.Empty(errors);
        await WaitState(session.Page, "platform", "item");
        Assert.Equal("0", (await PaintOpacities(session.Page, "operations-support")).Single());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task MovingWithinOneNativeSectorRetainsOutlinesAndDoesNotRepublishHover() {
        if (!Enabled) return;
        var html = SunburstBranchHighlightExamples.CreateChart().ToInteractiveHtmlPage(options => {
            Configure(options);
            options.Interaction.GroupName = "branch-hover";
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.FocusTrail);
        });
        await using var session = await OpenAsync(html, 850, 650);
        var page = session.Page;
        var point = await page.Locator(Node("platform")).EvaluateAsync<double[]>("""
            node => {
                const mark=node.querySelector('[data-cfx-role="sunburst-segment-mark"]'), box=mark.getBoundingClientRect();
                const hit=(x,y)=>document.elementFromPoint(x,y)?.closest('[data-cfx-target-kind=node]')===node;
                for(let y=1;y<40;y++) for(let x=1;x<40;x++) {
                    const px=box.left+box.width*x/40,py=box.top+box.height*y/40;
                    if([-4,0,4].every(dx=>[-4,0,4].every(dy=>hit(px+dx,py+dy)))) return [px,py];
                }
                throw new Error('No interior native sector hit');
            }
            """);
        await page.EvaluateAsync("""
            () => {
                const root=document.querySelector('.cfx-interactive-chart');
                window.branchHoverNotifications={hover:0,sync:0};
                root.addEventListener('cfxhover',()=>window.branchHoverNotifications.hover++);
                root.addEventListener('cfxsync',event=>{if(event.detail.action==='hover') window.branchHoverNotifications.sync++;});
            }
            """);
        await page.Mouse.MoveAsync((float)point[0], (float)point[1]);
        await WaitState(page, "platform", "item");
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        var before = await page.EvaluateAsync<int[]>("""
            () => {
                window.branchOutlines=Array.from(document.querySelectorAll('[data-cfx-highlight-outline]'));
                return [window.branchHoverNotifications.hover,window.branchHoverNotifications.sync,window.branchOutlines.length];
            }
            """);
        Assert.True(before[0] > 0); Assert.True(before[1] > 0); Assert.True(before[2] > 0);
        await page.Mouse.MoveAsync((float)point[0] + 2, (float)point[1] + 1, new MouseMoveOptions { Steps = 4 });
        await page.Mouse.MoveAsync((float)point[0] - 2, (float)point[1] - 1, new MouseMoveOptions { Steps = 4 });
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        var after = await page.EvaluateAsync<int[]>("() => [window.branchHoverNotifications.hover,window.branchHoverNotifications.sync,document.querySelectorAll('[data-cfx-highlight-outline]').length]");
        var retained = await page.EvaluateAsync<bool>("() => window.branchOutlines.every(outline=>outline.isConnected && document.querySelectorAll('[data-cfx-highlight-outline]')[window.branchOutlines.indexOf(outline)]===outline)");
        await Capture(session, html, "branch-hover-same-native-sector");
        var captureDirectory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captureDirectory))
            await File.WriteAllTextAsync(Path.Combine(captureDirectory, "branch-hover-same-native-sector.notifications.json"),
                System.Text.Json.JsonSerializer.Serialize(new { before, after, retained }));
        Assert.Equal(before, after);
        Assert.True(retained);
        Assert.Equal("root-branch", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-mode"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task DelayedPointerReadoutsDoNotObserveTheirOwnNativePaintProbe() {
        if (!Enabled) return;
        var html = SunburstBranchHighlightExamples.CreateChart().ToInteractiveHtmlPage(options => { Configure(options); options.Tooltip.DelayMilliseconds = 60; });
        await using var session = await OpenAsync(html, 850, 650);
        await session.Page.EvaluateAsync("""
            () => {
                window.highlightMutationCount = 0;
                new MutationObserver(records => window.highlightMutationCount += records.length)
                    .observe(document.querySelector('.cfx-interactive-chart'), { subtree:true, attributes:true,
                        attributeFilter:['class','style','data-cfx-highlight-probe','data-cfx-highlight-unavailable'] });
            }
            """);
        await MoveToNative(session.Page, Node("platform"));
        await session.Page.WaitForFunctionAsync("() => { const tip=document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && tip.innerText.includes('Platform'); }");
        await MoveToNative(session.Page, Node("operations-support"));
        await session.Page.WaitForFunctionAsync("() => { const tip=document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && tip.innerText.includes('Support'); }");
        // A settled visible readout must not repeatedly observe and write its own native-paint suspension.
        await session.Page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        var settled = await session.Page.EvaluateAsync<int>("() => window.highlightMutationCount");
        await session.Page.EvaluateAsync("() => new Promise(resolve => setTimeout(resolve, 100))");
        Assert.Equal(settled, await session.Page.EvaluateAsync<int>("() => window.highlightMutationCount"));
        Assert.Equal(0, await session.Page.Locator("[data-cfx-highlight-probe]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    private static void Configure(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Highlight.Mode = HtmlChartHighlightMode.RootBranch;
        options.Highlight.HighlightedBranch.Opacity = .7;
        options.Highlight.HighlightedBranch.Stroke = ChartColor.FromRgb(20, 180, 230);
        options.Highlight.HighlightedBranch.StrokeWidth = 2;
        options.Highlight.HighlightedItem.Opacity = 1;
        options.Highlight.UnhighlightedItem.Opacity = .55;
        options.Highlight.UnhighlightedItem.Stroke = ChartColor.FromRgb(0, 0, 0).WithAlpha(0);
        options.Highlight.UnhighlightedBranch.Opacity = 0;
        options.Highlight.UnhighlightedBranch.StrokeWidth = 0;
    }

    private static string Node(string id) => "[data-cfx-node=\"" + id + "\"]";
    private static Task WaitState(IPage page, string id, string state) => page.WaitForFunctionAsync("args => document.querySelector('[data-cfx-node=\"'+args.id+'\"]')?.dataset.cfxHighlightState === args.state", new { id, state });
    private static Task<string[]> PaintOpacities(IPage page, params string[] ids) => page.EvaluateAsync<string[]>("ids => ids.map(id => getComputedStyle(document.querySelector('[data-cfx-node=\"'+id+'\"] > [data-cfx-highlight-paint]')).opacity)", ids);
    private static Task<string[]> NativeInk(IPage page, string id) => page.Locator(Node(id)).EvaluateAsync<string[]>("""
        node => { const mark=node.querySelector('[data-cfx-role="sunburst-segment-mark"]'), style=getComputedStyle(mark), hatch=node.querySelector('[data-cfx-role="sunburst-pattern"]');
            return [style.fill,style.stroke,style.strokeWidth,hatch ? getComputedStyle(hatch).stroke : '',
                hatch ? getComputedStyle(hatch).strokeWidth : '',String(node.querySelectorAll('[data-cfx-role="sunburst-pattern"]').length)]; }
        """);
    private static async Task MoveToNative(IPage page, string selector) {
        var point = await page.Locator(selector).First.EvaluateAsync<double[]>("""
            node => { const mark=node.querySelector('[data-cfx-role="sunburst-segment-mark"]'), box=mark.getBoundingClientRect();
                for(let y=1;y<40;y++) for(let x=1;x<40;x++) { const px=box.left+box.width*x/40,py=box.top+box.height*y/40,hit=document.elementFromPoint(px,py);
                    if(hit && hit.closest('[data-cfx-target-kind=node]')===node && !hit.hasAttribute('data-cfx-highlight-outline')) return [px,py]; }
                throw new Error('No native Sunburst hit for '+node.dataset.cfxNode); }
            """);
        await page.Mouse.MoveAsync((float)point[0], (float)point[1], new MouseMoveOptions { Steps = 4 });
    }
    private static async Task Capture(HtmlBrowserSession session, string html, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        var state = await session.Page.EvaluateAsync<string>("() => JSON.stringify(Array.from(document.querySelectorAll('.cfx-interactive-chart')).map(root => ({ hover:root.dataset.cfxHoverKey, mode:root.dataset.cfxHoverMode, pinned:root.dataset.cfxPinnedTarget, tooltipHidden:root.querySelector('.cfx-tooltip').hidden, probe:root.hasAttribute('data-cfx-highlight-probe') })))");
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".runtime.json"), System.Text.Json.JsonSerializer.Serialize(new {
            viewport = session.Page.ViewportSize, state = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(state),
            console = session.ConsoleLog.Select(entry => new { type = entry.Type.ToString(), text = entry.Text })
        }));
    }
}
