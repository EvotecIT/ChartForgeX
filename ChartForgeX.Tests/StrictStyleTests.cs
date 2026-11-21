using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StrictStyleTests {
    private const string Nonce = "n0nce";

    [Fact]
    public void ExtractMovesStyleAttributesAndElementsIntoOneStylesheet() {
        var result = HtmlStrictStyles.Extract(
            "<style media=\"print\">.a{color:red}</style><div class=\"box\" style=\"width:10px;font-family:&quot;A&quot;\">x</div>"
            + "<svg><style><![CDATA[.b{fill:blue}]]></style><rect style=\"width:10px;font-family:&quot;A&quot;\"/><g style=\"\"></g></svg>"
            + "<script>var s='<b style=\"kept\">';</script><!-- <i style=\"kept\"> -->");
        var name = Regex.Match(result.Markup, "cfx-s-[0-9a-f]{12}").Value;
        Assert.Equal("<div class=\"box " + name + "\">x</div><svg><rect class=\"" + name + "\"/><g></g></svg>"
            + "<script>var s='<b style=\"kept\">';</script><!-- <i style=\"kept\"> -->", result.Markup);
        Assert.Equal("@media print{.a{color:red}}\n.b{fill:blue}\n." + name + "." + name + "." + name + "{width:10px;font-family:\"A\"}", result.StyleSheet);
        var html = result.ToHtml(Nonce);
        Assert.StartsWith("<style nonce=\"n0nce\" data-cfx-strict-styles=\"true\">", html, StringComparison.Ordinal);
        Assert.EndsWith(result.Markup, html, StringComparison.Ordinal);
    }

    [Fact]
    public void RendererNonceKeepsSvgStyleElementsInTheirSvg() {
        var html = new HtmlChartRenderer { StyleNonce = Nonce }.RenderFragment(Chart.Create().WithSize(320, 200).AddLine("A", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) }), "a");
        var svg = html.Substring(html.IndexOf("<svg", StringComparison.Ordinal));
        Assert.Matches("<style nonce=\"n0nce\">[^<]*@font-face", svg);
        Assert.DoesNotContain("@font-face", HtmlStrictStyles.Extract(html).Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractKeepsDeclarationsInsideTheirStyleElement() {
        var result = HtmlStrictStyles.Extract("<p style=\"content:&quot;&lt;/style&gt;&quot;\"></p>");
        Assert.DoesNotContain("</style", result.StyleSheet, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\3c /style>", result.StyleSheet, StringComparison.Ordinal);
    }

    public static TheoryData<string> Outputs => new() {
        "static-fragment", "static-page", "responsive-fragment", "interactive-fragment", "interactive-page", "interactive-section",
        "dashboard-page", "topology-fragment", "topology-page", "topology-interactive-fragment", "topology-interactive-page", "graph-fragment"
    };

    [Theory]
    [MemberData(nameof(Outputs))]
    public void NonceMovesEveryInlineStyleIntoOneNoncedStylesheet(string output) {
        var html = Render(output, Nonce);
        var markup = Regex.Replace(html, "<script\\b[\\s\\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
        Assert.DoesNotMatch(new Regex("<[a-zA-Z][^<>]*\\sstyle\\s*=", RegexOptions.IgnoreCase), markup);
        // One page stylesheet; style elements inside an SVG stay there so exported SVG keeps them, all with the nonce.
        var styles = Regex.Matches(markup, "<style\\b[^>]*>", RegexOptions.IgnoreCase).Cast<Match>().ToArray();
        Assert.Single(styles, style => style.Value.Contains("data-cfx-strict-styles", StringComparison.Ordinal));
        Assert.All(styles, style => Assert.Contains("nonce=\"n0nce\"", style.Value, StringComparison.Ordinal));
        foreach (Match script in Regex.Matches(html, "<script\\b[^>]*>", RegexOptions.IgnoreCase))
            if (!script.Value.Contains("type=\"application/", StringComparison.Ordinal)) Assert.Contains("nonce=\"n0nce\"", script.Value, StringComparison.Ordinal);
        // Without a nonce the renderer keeps its ordinary output.
        Assert.DoesNotMatch(new Regex("<style[^>]*data-cfx-strict-styles"), Render(output, null));
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData("interactive-page")]
    [InlineData("topology-interactive-page")]
    [InlineData("static-page")]
    public async Task StrictPageRendersLikeTheOrdinaryPageUnderAStrictPolicy(string output) {
        if (!InteractiveChartBrowser.Enabled) return;
        const string Snapshot = """
            () => [...document.body.querySelectorAll('*')].filter(node => !node.closest('script,style')).map(node => {
              const computed = getComputedStyle(node);
              return [node.tagName, ...['display', 'width', 'height', 'max-width', 'min-width', 'fill', 'stroke', 'opacity', 'color', 'font-size', 'visibility',
                '--cfx-color-text', '--cfx-topology-scenario-ink'].map(name => computed.getPropertyValue(name).trim())].join('|');
            })
            """;
        // Runtimes restore inline styles inside the roots they mount; the declarations must match the ordinary page.
        const string Inline = """
            () => [...document.querySelectorAll('.cfx-interactive-chart, [data-cfx-runtime-bound]')].flatMap(root => [root, ...root.querySelectorAll('*')])
              .map(node => node.tagName + '|' + (node.getAttribute('class') || '').split(/\s+/).filter(name => name.startsWith('cfx-s-')).length + '|' + node.style.cssText)
            """;
        var policy = "default-src 'none'; img-src data:; font-src data:; style-src 'nonce-" + Nonce + "'; script-src 'nonce-" + Nonce + "'";
        await using var ordinary = await OpenAsync(Render(output, null), null);
        var expected = await ordinary.Page.EvaluateAsync<string[]>(Snapshot);
        var expectedInline = await ordinary.Page.EvaluateAsync<string[]>(Inline);
        // The same policy refuses the ordinary output, so the strict page below proves the policy is enforced.
        await using (var refused = await OpenAsync(Render(output, null), policy))
            Assert.NotEmpty(await refused.Page.EvaluateAsync<string[]>("() => window.__cfxViolations || []"));
        await using var session = await OpenAsync(Render(output, Nonce), policy);
        Assert.Empty(await session.Page.EvaluateAsync<string[]>("() => window.__cfxViolations || []"));
        Assert.Equal(expected, await session.Page.EvaluateAsync<string[]>(Snapshot));
        Assert.Equal(expectedInline, await session.Page.EvaluateAsync<string[]>(Inline));
    }

    // Serve the page with a real Content-Security-Policy header; a meta policy is not applied to injected content.
    private static async Task<HtmlTinkerX.HtmlBrowserSession> OpenAsync(string html, string? policy) {
        var session = await HtmlTinkerX.HtmlBrowser.OpenSessionForNavigationAsync(new HtmlTinkerX.HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 15000, ViewportWidth = 1000, ViewportHeight = 700,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
        });
        try {
            const string Url = "https://chartforgex.test/strict.html";
            await session.Page.AddInitScriptAsync("document.addEventListener('securitypolicyviolation', e => (window.__cfxViolations ||= []).push(e.violatedDirective + ' ' + e.sample))");
            var headers = new Dictionary<string, string> { ["Content-Type"] = "text/html; charset=utf-8" };
            if (policy != null) headers["Content-Security-Policy"] = policy;
            await session.Page.RouteAsync(Url, route => route.FulfillAsync(new() { Body = html, Headers = headers }));
            await session.Page.GotoAsync(Url, new() { WaitUntil = Microsoft.Playwright.WaitUntilState.Load, Timeout = 15000 });
            await InteractiveChartBrowser.SettlePresentationAsync(session.Page);
            return session;
        } catch {
            await session.DisposeAsync();
            throw;
        }
    }

    private static string Render(string output, string? nonce) {
        var chart = Chart.Create().WithTitle("Checks").WithSize(640, 360).WithXLabels("Mon", "Tue", "Wed")
            .AddLine("Passed", new[] { new ChartPoint(1, 4), new ChartPoint(2, 6), new ChartPoint(3, 5) });
        var topology = TopologyChart.Create().WithId("notes").WithTitle("Save a note")
            .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .AddNode("app", "Notes app", 0, 0, TopologyNodeKind.Person)
            .AddNode("api", "Notes API", 0, 0, TopologyNodeKind.Server)
            .AddEdge("http", "app", "api", "HTTPS")
            .AddScenario("save", "Save a note", scenario => scenario.AddEdgeStep("http"));
        var topologyOptions = new TopologyRenderOptions { HtmlNonce = nonce };
        void Interactive(HtmlChartInteractionOptions options) { options.StyleNonce = nonce; options.ScriptNonce = nonce; }
        return output switch {
            "static-fragment" => new HtmlChartRenderer { StyleNonce = nonce }.RenderFragment(chart, "checks"),
            "static-page" => new HtmlChartRenderer { StyleNonce = nonce }.RenderPage(chart),
            "responsive-fragment" => new HtmlResponsiveChartRenderer { StyleNonce = nonce, ScriptNonce = nonce }.RenderFragment(chart, idScope: "checks"),
            "interactive-fragment" => chart.ToInteractiveHtmlFragment(Interactive),
            "interactive-page" => chart.ToInteractiveHtmlPage(Interactive),
            "interactive-section" => chart.ToInteractiveHtmlFragmentWithoutAssets(Interactive),
            "dashboard-page" => new HtmlInteractiveDashboardRenderer().RenderPage(new[] { chart }, options => { options.StyleNonce = nonce; options.ScriptNonce = nonce; }),
            "topology-fragment" => new TopologyHtmlRenderer().RenderFragment(topology, topologyOptions),
            "topology-page" => new TopologyHtmlRenderer().RenderPage(topology, topologyOptions),
            "topology-interactive-fragment" => new HtmlInteractiveTopologyRenderer().RenderFragment(topology, topologyOptions),
            "topology-interactive-page" => new HtmlInteractiveTopologyRenderer().RenderPage(topology, topologyOptions),
            "graph-fragment" => new HtmlGraphExplorerRenderer().RenderFragment(GraphScene.Create("map", "Map").AddNode("a", "A").AddNode("b", "B").AddEdge("ab", "a", "b"),
                options => { options.StyleNonce = nonce; options.ScriptNonce = nonce; }),
            _ => throw new ArgumentOutOfRangeException(nameof(output))
        };
    }
}
