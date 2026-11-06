using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.Interactivity;
using ChartForgeX.VisualArtifacts;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class HtmlOwnedFontDeliveryTests {
    [Fact]
    public void ChartSvgHostFontPolicyOnlyRemovesFontsAndRetainsScopedExportOptions() {
        var chart = Model("Archivo");
        chart.WithSvgColorVariables(SvgColorVariables.SharedRoles(chart.Options.Theme, chart.Options.ThemeMode));
        const string scope = "report chart/1|light";
        var defaults = new VisualSvgOptions(colorVariables: chart.Options.SvgColorVariables);
        var options = new VisualSvgOptions("original-policy", chart.Options.SvgColorVariables, responsive: true)
            .WithPatternTiles();
        var hostedOptions = options.WithoutProductFonts();
        Assert.NotSame(options, hostedOptions);
        Assert.Equal("original-policy", hostedOptions.IdPrefix);
        Assert.True(hostedOptions.Responsive);
        Assert.True(hostedOptions.PatternTiles);

        var standalone = XDocument.Parse(chart.ToSvg(options, scope));
        var hostedSvg = chart.ToSvg(hostedOptions, scope);
        var hosted = XDocument.Parse(hostedSvg);
        var fonts = Assert.Single(standalone.Descendants(), node => node.Name.LocalName == "style" && node.Value.Contains("@font-face", StringComparison.Ordinal));
        Assert.Contains("font-family:'Archivo'", fonts.Value, StringComparison.Ordinal);
        fonts.Remove();
        Assert.DoesNotContain("@font-face", hostedSvg, StringComparison.Ordinal);
        Assert.True(XNode.DeepEquals(standalone, hosted), "Host font ownership must preserve every other SVG node and attribute.");
        Assert.Contains("var(--hfx-", hostedSvg, StringComparison.Ordinal);
        Assert.Equal("original-policy", options.IdPrefix);
        Assert.Equal("original-policy", hostedOptions.IdPrefix);
        Assert.Equal(chart.ToSvg(scope), chart.ToSvg(defaults, scope));
        Assert.Equal(chart.ToSvg(), chart.ToSvg(defaults, null));
        var policyIds = XDocument.Parse(chart.ToSvg(hostedOptions, null)).Descendants().Attributes("id").ToArray();
        Assert.NotEmpty(policyIds);
        Assert.All(policyIds, id => Assert.StartsWith("original-policy", id.Value, StringComparison.Ordinal));
        Assert.Throws<ArgumentNullException>(() => chart.ToSvg(null!));
    }

    [Fact]
    public void StandaloneSvgDeliversTheBundledFaceActuallyUsedByAFallbackGlyph() {
        FontRegistry.Register("HostMono", ProductFont("JetBrainsMono", "ttf"));
        var chart = Model("HostMono, Archivo").WithTitle("Ĳ");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var heading = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Role == "frame-heading");
        Assert.Equal("Archivo", Assert.Single(Assert.Single(heading.Text.Lines).Glyphs).Face.SelectedFamily);
        var svg = prepared.ToSvg();
        Assert.Contains("@font-face{font-family:'Archivo'", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("@font-face{font-family:'JetBrains Mono'", svg, StringComparison.Ordinal);
        FontRegistry.Clear();
        Assert.Equal(svg, prepared.ToSvg());
    }

    [Theory]
    [InlineData("chart-host")]
    [InlineData("topology-page")]
    [InlineData("topology-host")]
    [InlineData("responsive-host")]
    [InlineData("graph-page")]
    [InlineData("sequence-page")]
    public void DocumentedHostRegistrationSuppliesProductFontsWhileFragmentsStayAssetFree(string path) {
        var (html, fragment) = ProductHost(path);
        Assert.Contains("@font-face{font-family:'Archivo'", html, StringComparison.Ordinal);
        Assert.Contains("@font-face{font-family:'JetBrains Mono'", html, StringComparison.Ordinal);
        if (fragment != null) {
            Assert.DoesNotContain("@font-face", fragment, StringComparison.Ordinal);
            Assert.DoesNotContain("<style", fragment, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BrowserHostsDoNotReplaceAnExplicitlyRegisteredProductFamily() {
        Assert.Contains("@font-face{font-family:'Archivo'", HtmlGraphExplorerRenderer.BuildFragmentStyle(), StringComparison.Ordinal);
        FontRegistry.Register("Archivo", ProductFont("JetBrainsMono", "ttf"));
        var renderer = new HtmlInteractiveChartRenderer();
        var chart = Model().WithTitle("11118888");
        foreach (var html in new[] {
            renderer.RenderPage(chart, value => value.TextPresentation = HtmlChartTextPresentation.Browser),
            ChartFontAssets.StyleSheet,
            HtmlGraphExplorerRenderer.BuildFragmentStyle()
        }) {
            Assert.DoesNotContain("@font-face{font-family:'Archivo'", html, StringComparison.Ordinal);
            Assert.Contains("@font-face{font-family:'JetBrains Mono'", html, StringComparison.Ordinal);
        }
        // An embeddable fragment leaves every product face to its host page.
        Assert.DoesNotContain("@font-face", renderer.RenderFragment(chart, value => value.TextPresentation = HtmlChartTextPresentation.Browser), StringComparison.Ordinal);
    }

    [Fact]
    public void StyleOnlyHelpersLetHostsRegisterOneSharedFontPayloadWithoutDuplication() {
        foreach (var css in new[] {
            HtmlInteractiveChartRenderer.BuildFragmentStyle(),
            TopologyHtmlRenderer.BuildFragmentStyle(), HtmlResponsiveChartRenderer.Stylesheet
        }) {
            Assert.DoesNotContain("@font-face", css, StringComparison.Ordinal);
            Assert.DoesNotContain("data:font", css, StringComparison.Ordinal);
            Assert.NotEmpty(css);
        }
        var shared = ChartFontAssets.StyleSheet + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + TopologyHtmlRenderer.BuildFragmentStyle() + HtmlResponsiveChartRenderer.Stylesheet;
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(shared, "@font-face\\{").Count);
    }

    [BrowserTheory]
    [InlineData("chart-host")]
    [InlineData("topology-page")]
    [InlineData("topology-host")]
    [InlineData("responsive-host")]
    [InlineData("graph-page")]
    [InlineData("sequence-page")]
    public async Task ProductHostLoadsItsOwnBrowserFontWithoutTheProofFontInjector(string path) {
        if (!InteractiveChartBrowser.Enabled) return;
        var (html, _) = ProductHost(path);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 800, 560, waitForOwnedFonts: false);
        Assert.True(await session.Page.EvaluateAsync<bool>("async()=>{const faces=await document.fonts.load('600 16px Archivo');await document.fonts.ready;return faces.length>0&&faces.every(face=>face.status==='loaded')}"));
        Assert.True(await session.Page.EvaluateAsync<bool>("async()=>{const faces=await document.fonts.load('500 16px \"JetBrains Mono\"');return faces.length>0&&faces.every(face=>face.status==='loaded')}"));
        if (path is "graph-page" or "sequence-page")
            Assert.StartsWith("Archivo", await session.Page.EvaluateAsync<string>("()=>getComputedStyle(document.body).fontFamily"), StringComparison.Ordinal);
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>[...document.querySelectorAll('svg text')].some(node=>node.getBoundingClientRect().width>0&&node.getComputedTextLength()>0)"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output);
            await session.Page.ScreenshotAsync(new() { Path = Path.Combine(output, "product-host-" + path + ".png") });
        }
    }

    [BrowserFact]
    public async Task StandaloneFallbackGlyphLoadsItsBundledBrowserFaceWithoutTheProofFontInjector() {
        if (!InteractiveChartBrowser.Enabled) return;
        FontRegistry.Register("HostMono", ProductFont("JetBrainsMono", "ttf"));
        var chart = Model("HostMono, Archivo").WithTitle("Ĳ");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var heading = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Role == "frame-heading");
        var host = "<style>@font-face{font-family:HostMono;font-weight:100 800;src:url(data:font/woff2;base64,"
            + Convert.ToBase64String(ProductFont("JetBrainsMono", "woff2")) + ") format('woff2')}</style>";
        await using var session = await InteractiveChartBrowser.OpenAsync(host + prepared.ToSvg(), 800, 560, waitForOwnedFonts: false);
        Assert.True(await session.Page.EvaluateAsync<bool>("async()=>{const faces=await document.fonts.load('600 16px Archivo');await document.fonts.ready;return faces.length>0&&faces.every(face=>face.status==='loaded')}"));
        var width = await session.Page.Locator("[data-cfx-role=frame-heading] text").First.EvaluateAsync<double>("node=>node.getComputedTextLength()");
        Assert.InRange(Math.Abs(width - Assert.Single(heading.Text.Lines).Width), 0, .05);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task BrowserOverrideKeepsTheHostFaceAndItsNativeMeasuredAdvance() {
        if (!InteractiveChartBrowser.Enabled) return;
        FontRegistry.Register("Archivo", ProductFont("JetBrainsMono", "ttf"));
        var chart = Model().WithTitle("11118888");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var heading = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Role == "frame-heading");
        var host = "<style>@font-face{font-family:Archivo;font-weight:100 800;src:url(data:font/woff2;base64,"
            + Convert.ToBase64String(ProductFont("JetBrainsMono", "woff2")) + ") format('woff2')}</style>";
        var html = host + new HtmlInteractiveChartRenderer().RenderFragment(chart, value => value.TextPresentation = HtmlChartTextPresentation.Browser);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 800, 560, waitForOwnedFonts: false);
        await session.Page.EvaluateAsync("async()=>{await document.fonts.load('600 16px Archivo');await document.fonts.ready}");
        var width = await session.Page.Locator("[data-cfx-role=frame-heading] text").First.EvaluateAsync<double>("node=>node.getComputedTextLength()");
        Assert.InRange(Math.Abs(width - Assert.Single(heading.Text.Lines).Width), 0, .05);
        Assert.Equal(1, await session.Page.EvaluateAsync<int>("()=>[...document.fonts].filter(face=>face.family.replace(/[\"']/g,'')==='Archivo').length"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task NativeContourHostAssetsStayFontFreeUnderStrictCsp() {
        if (!InteractiveChartBrowser.Enabled) return;
        var css = HtmlInteractiveChartRenderer.BuildFragmentStyle();
        var fragment = new HtmlInteractiveChartRenderer().RenderFragmentWithoutAssets(Model(), value => value.TextPresentation = HtmlChartTextPresentation.NativeContours);
        Assert.DoesNotContain("@font-face", css + fragment, StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-font-root=", fragment, StringComparison.Ordinal);
        const string policy = "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data:; font-src 'none'; connect-src 'none'; base-uri 'none'";
        var html = "<!doctype html><html><head><meta http-equiv=\"Content-Security-Policy\" content=\"" + policy + "\"><style>" + css
            + "</style><script>window.fontPolicyViolations=[];addEventListener('securitypolicyviolation',event=>fontPolicyViolations.push(event.effectiveDirective));</script></head><body>"
            + fragment + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 800, 560, waitForOwnedFonts: false);
        Assert.Equal(0, await session.Page.EvaluateAsync<int>("()=>document.fonts.size"));
        Assert.Equal("[]", await session.Page.EvaluateAsync<string>("()=>JSON.stringify(fontPolicyViolations)"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static (string Html, string? Fragment) ProductHost(string path) {
        var chart = Model().WithTitle("Product font delivery");
        var topology = TopologyChart.Create().WithTitle("Product font delivery").WithViewport(720, 420, 24)
            .AddNode("node", "Bundled face", 40, 60, width: 180, height: 72);
        if (path == "topology-page") return (new TopologyHtmlRenderer().RenderPage(topology), null);
        if (path == "graph-page") return (GraphScene.Create("product-graph", "Product font delivery")
            .AddNode("node", "Bundled face").ToGraphExplorerHtmlPage(), null);
        if (path == "sequence-page") {
            var sequence = SequenceArtifact.Create("product-navigation").AddParticipant("reader", "Reader")
                .AddParticipant("archive", "Archive").AddMessage("reader", "archive", "Save");
            sequence.Participants[0].NavigationLinks = new[] { new VisualNavigationLink("Guide", "#guide") };
            return (sequence.ToNavigationHtmlPage(), null);
        }
        var fragment = path switch {
            "chart-host" => new HtmlInteractiveChartRenderer().RenderFragmentWithoutAssets(chart, value => value.TextPresentation = HtmlChartTextPresentation.Browser),
            "topology-host" => new TopologyHtmlRenderer().RenderFragmentWithoutAssets(topology),
            "responsive-host" => new HtmlResponsiveChartRenderer().RenderFragmentWithoutAssets(chart),
            _ => throw new ArgumentOutOfRangeException(nameof(path))
        };
        var css = path switch {
            "chart-host" => HtmlInteractiveChartRenderer.BuildFragmentStyle(),
            "topology-host" => TopologyHtmlRenderer.BuildFragmentStyle(),
            _ => HtmlResponsiveChartRenderer.Stylesheet
        };
        var script = path switch {
            "responsive-host" => HtmlResponsiveChartRenderer.InitializationScript,
            "chart-host" => HtmlInteractiveChartRenderer.BuildInteractionScript(),
            _ => string.Empty
        };
        return ("<!doctype html><html><head><style>" + ChartFontAssets.StyleSheet + css + "</style></head><body>" + fragment + "<script>" + script + "</script></body></html>", fragment);
    }

    private static byte[] ProductFont(string name, string extension) {
        using var stream = typeof(ChartFontAssets).Assembly.GetManifestResourceStream("ChartForgeX.Typography.Fonts." + name + "." + extension)!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray();
    }
}
