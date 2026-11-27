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
        var html = new HtmlChartRenderer { Nonce = Nonce }.RenderFragment(Chart.Create().Size(320, 200).AddLine("A", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) }), "a");
        var svg = html.Substring(html.IndexOf("<svg", StringComparison.Ordinal));
        Assert.Matches("<style nonce=\"n0nce\">[^<]*@font-face", svg);
        Assert.DoesNotContain("@font-face", HtmlStrictStyles.Extract(html).Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractKeepsDeclarationsInsideTheirStyleElement() {
        var result = HtmlStrictStyles.Extract("<p style=\"content:&quot;&lt;/style&gt;&quot;\"></p><b style=\"color:red}body{display:none\"></b>");
        Assert.DoesNotContain("</style", result.StyleSheet, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\3c /style>", result.StyleSheet, StringComparison.Ordinal);
        // A brace cannot end the generated rule and start one for the page.
        Assert.Equal(2, result.StyleSheet.Count(ch => ch == '}'));
        Assert.Contains("color:red\\7d body\\7b display:none", result.StyleSheet, StringComparison.Ordinal);
        // An opened comment cannot swallow the rules after it.
        Assert.DoesNotContain("/*", HtmlStrictStyles.Extract("<i style=\"color:red/*\"></i><u style=\"color:blue\"></u>").StyleSheet, StringComparison.Ordinal);
    }

    public static TheoryData<string> Outputs => new() {
        "static-fragment", "static-page", "responsive-fragment", "interactive-fragment", "interactive-page", "interactive-section",
        "dashboard-page", "interactive-adaptive-page", "topology-fragment", "topology-page", "topology-interactive-fragment", "topology-interactive-page", "graph-fragment",
        "flow-interactive-page", "sequence-interactive-fragment"
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
    [InlineData("interactive-adaptive-page")]
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
              .filter(node => !node.matches('script,style'))
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

    [Fact]
    public void ScenarioColorsMustBeCssColors() {
        var scenario = new ChartForgeX.Interactivity.ChartInteractionScenario { Color = " #2563eb " };
        Assert.Equal("#2563eb", scenario.Color);
        scenario.Color = "rgb(37 99 235 / 50%)";
        scenario.Color = "rgb(37\t99\t235)";
        Assert.Equal("rgb(37 99 235)", scenario.Color);
        Assert.Equal("var(--brand_accent)", new TopologyScenario { Color = "var(--brand_accent)" }.Color);
        Assert.Equal("hsl(38.82 calc(2 * 50%) 50%)", new TopologyScenario { Color = "hsl(38.82 calc(2 * 50%) 50%)" }.Color);
        Assert.Throws<ArgumentException>(() => scenario.Color = "red}body{display:none");
        Assert.Throws<ArgumentException>(() => scenario.Color = "#12_");
        Assert.Throws<ArgumentException>(() => scenario.Color = "red/*");
        Assert.Throws<ArgumentException>(() => new TopologyScenario { Color = "red */" });
        var longReference = "var(--" + new string('a', 80) + ")";
        Assert.Equal(longReference, new TopologyScenario { Color = longReference }.Color);
        Assert.Equal(longReference, new ChartForgeX.Interactivity.ChartInteractionScenario { Color = longReference }.Color);
        Assert.Throws<ArgumentException>(() => new TopologyScenario { Color = "#12345" });
        Assert.Throws<ArgumentException>(() => new TopologyScenario { Color = "red;background:url(x)" });
    }

    [Trait("Category", "Browser")]
    [Fact]
    public async Task UpdateAcceptsAStrictSectionElementWhoseRulesThePageHolds() {
        if (!InteractiveChartBrowser.Enabled) return;
        var policy = "default-src 'none'; img-src data:; font-src data:; style-src 'nonce-" + Nonce + "'; script-src 'nonce-" + Nonce + "'";
        var next = Chart.Create().Title("Checks").Size(520, 300).XLabels("Mon", "Tue")
            .AddLine("Passed", new[] { new ChartPoint(1, 2), new ChartPoint(2, 9) });
        var expected = HtmlStrictStyles.Extract(next.ToInteractiveHtmlFragmentWithoutAssets());
        var wider = HtmlStrictStyles.Extract(next.Size(700, 300).ToInteractiveHtmlFragmentWithoutAssets());
        var page = Render("interactive-page", Nonce).Replace("</body>", "<style nonce=\"" + Nonce + "\">" + expected.StyleSheet + wider.StyleSheet + "</style><template id=\"next\">" + expected.Markup
            + "</template><template id=\"wider\">" + wider.Markup + "</template></body>", StringComparison.Ordinal);
        await using var session = await OpenAsync(page, policy);
        var width = await session.Page.EvaluateAsync<string>("""
            () => {
              const root = document.querySelector('.cfx-interactive-chart'), section = document.getElementById('next').content.firstElementChild.cloneNode(true);
              const controller = ChartForgeXCharts.mount(root);
              controller.update(section);
              const fromElement = root.style.getPropertyValue('--cfx-intrinsic-width');
              // Markup alone, with its rules registered by the host, restores the same properties.
              controller.update(document.getElementById('wider').content.firstElementChild.cloneNode(true));
              if (root.style.getPropertyValue('--cfx-intrinsic-width').trim() !== '700px') return 'wider update did not apply';
              controller.update(document.getElementById('next').innerHTML);
              return fromElement === root.style.getPropertyValue('--cfx-intrinsic-width') ? fromElement : 'element ' + fromElement + ', markup ' + root.style.getPropertyValue('--cfx-intrinsic-width');
            }
            """);
        Assert.Equal("520px", width.Trim());
        // A property the host set inline before mounting keeps the host's value; the others are restored.
        var kept = await session.Page.EvaluateAsync<string[]>("""
            () => {
              const section = document.getElementById('next').content.firstElementChild.cloneNode(true);
              section.style.setProperty('--cfx-intrinsic-width', '999px');
              window[Symbol.for('ChartForgeX.strictStyles')](section);
              return [section.style.getPropertyValue('--cfx-intrinsic-width').trim(), String(section.style.length > 1)];
            }
            """);
        Assert.Equal(new[] { "999px", "true" }, kept);
        Assert.Empty(await session.Page.EvaluateAsync<string[]>("() => window.__cfxViolations || []"));
    }

    // The graph explorer under a nonce policy that refuses blob workers, style attributes and style elements without the
    // nonce: physics stays on the main thread when asked, live edge patches style through the CSSOM, and the print
    // surface brings its export styles without breaking the policy.
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData("physics")]
    [InlineData("patch")]
    [InlineData("print")]
    [InlineData("print host rules")]
    public async Task GraphExplorerKeepsAStrictPolicy(string action) {
        if (!InteractiveChartBrowser.Enabled) return;
        var scene = GraphScene.Create("strict-graph", "Services");
        for (var index = 0; index < 170; index++) scene.AddNode("n" + index, "Service " + index);
        for (var index = 1; index < 170; index++) scene.AddEdge("e" + index, "n" + (index - 1), "n" + index);
        scene.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.IncrementalUpdates);
        scene.Options.Physics.Solver = GraphPhysicsSolver.BarnesHut;
        var policy = "default-src 'none'; img-src data: blob:; font-src data:; style-src 'nonce-" + Nonce + "'; script-src 'nonce-" + Nonce + "'";
        var graphPage = scene.ToGraphExplorerHtmlPage(options => {
            options.Nonce = Nonce; options.PhysicsThread = HtmlGraphPhysicsThread.Main; options.PersistThemePreference = false;
        });
        // A host's own print rule, which the print snapshot's rules must not override.
        if (action == "print host rules") graphPage = graphPage.Replace("</head>", "<style nonce=\"" + Nonce + "\">@media print{.cfx-graph-shell{margin-left:27px}}</style></head>", StringComparison.Ordinal);
        await using var session = await OpenAsync(graphPage, policy);
        var page = session.Page;
        if (action == "physics") {
            await page.WaitForFunctionAsync("() => ['main', 'stopped'].includes(document.querySelector('.cfx-graph-explorer').dataset.cfxGraphPhysicsThread)");
            Assert.NotEqual("worker", await page.EvaluateAsync<string>("() => document.querySelector('.cfx-graph-explorer').dataset.cfxGraphPhysicsThread"));
        } else if (action == "patch") {
            Assert.NotEqual("none", await page.EvaluateAsync<string>("""
                () => {
                  const root = document.querySelector('.cfx-graph-explorer');
                  ChartForgeXGraphExplorer.update(root, { upsertEdges: [{ id: 'e1', sourceNodeId: 'n0', targetNodeId: 'n1', dashed: true, style: { width: 6 } }] });
                  return getComputedStyle(root.querySelector('[data-edge-id="e1"]')).strokeDasharray;
                }
                """));
        } else {
            await page.EmulateMediaAsync(new() { Media = Microsoft.Playwright.Media.Print });
            if (action == "print host rules") {
                await page.WaitForSelectorAsync(".cfx-graph-explorer[data-cfx-graph-print-ready=true]", new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
                Assert.Equal("27px", await page.EvaluateAsync<string>("() => getComputedStyle(document.body).marginLeft"));
            }
            await page.WaitForSelectorAsync(".cfx-graph-explorer[data-cfx-graph-print-ready=true]", new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
            Assert.True(await page.EvaluateAsync<bool>("() => !!document.querySelector('[data-cfx-role=graph-print-surface] svg')"));
        }
        var violations = await page.EvaluateAsync<string[]>("() => window.__cfxViolations || []");
        Assert.True(violations.Length == 0, string.Join(" | ", violations));
    }

    // A responsive drawing handed to the layout runtime keeps its fragment references after the runtime renames its ids and
    // moves its styles into the CSS object model under a nonce policy.
    [Trait("Category", "Browser")]
    [Fact]
    public async Task ResponsiveLayoutKeepsFragmentReferencesUnderAStrictPolicy() {
        if (!InteractiveChartBrowser.Enabled) return;
        using var stream = typeof(HtmlResponsiveChartRenderer).Assembly.GetManifestResourceStream("ChartForgeX.Interactivity.Html.Assets.chart-layout.js")!;
        string runtime = new StreamReader(stream).ReadToEnd();
        var page = "<!doctype html><html><head></head><body><div id=\"host\"><svg></svg></div><script nonce=\"" + Nonce + "\">" + runtime + "</script></body></html>";
        var policy = "default-src 'none'; style-src 'nonce-" + Nonce + "'; script-src 'nonce-" + Nonce + "'";
        await using var session = await OpenAsync(page, policy);
        var references = await session.Page.EvaluateAsync<string[]>("""
            () => {
              const svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><defs><clipPath id="plot"><rect width="50" height="50"/></clipPath></defs><rect data-probe="true" width="100" height="100" style="clip-path:url(#plot);fill:red"/></svg>';
              try { ChartForgeXLayout.create(document.getElementById('host'), { schema: 'chartforgex/layout-1', svg }); } catch { }
              const probe = document.querySelector('#host [data-probe]');
              return [probe.style.clipPath, document.querySelector('#host clipPath').id, probe.style.fill];
            }
            """);
        Assert.Contains(references[1], references[0]);
        Assert.Equal("red", references[2]);
        Assert.Empty(await session.Page.EvaluateAsync<string[]>("() => window.__cfxViolations || []"));
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
        var chart = Chart.Create().Title("Checks").Size(640, 360).XLabels("Mon", "Tue", "Wed")
            .AddLine("Passed", new[] { new ChartPoint(1, 4), new ChartPoint(2, 6), new ChartPoint(3, 5) });
        var topology = TopologyChart.Create().Id("notes").Title("Save a note")
            .LayoutMode(TopologyLayoutMode.Layered).LayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddNode("app", "Notes app", 0, 0, TopologyNodeKind.Person)
            .AddNode("api", "Notes API", 0, 0, TopologyNodeKind.Server)
            .AddEdge("http", "app", "api", "HTTPS")
            .AddScenario("save", "Save a note", scenario => scenario.AddEdgeStep("http"));
        var topologyOptions = new TopologyRenderOptions { Html = { Nonce = nonce } };
        void Interactive(HtmlChartInteractionOptions options) { options.Nonce = nonce; }
        return output switch {
            "static-fragment" => new HtmlChartRenderer { Nonce = nonce }.RenderFragment(chart, "checks"),
            "static-page" => new HtmlChartRenderer { Nonce = nonce }.RenderPage(chart),
            "responsive-fragment" => new HtmlResponsiveChartRenderer { Nonce = nonce }.RenderFragment(chart, idScope: "checks"),
            "interactive-fragment" => chart.ToInteractiveHtmlFragment(Interactive),
            "interactive-page" => chart.ToInteractiveHtmlPage(Interactive),
            // The narrow drawing waits in a template until a narrow container selects it.
            "interactive-adaptive-page" => chart.ToInteractiveHtmlPage(options => { Interactive(options); options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.Breakpoint = 1200; }),
            "interactive-section" => chart.ToInteractiveHtmlFragmentWithoutAssets(Interactive),
            "dashboard-page" => new HtmlInteractiveDashboardRenderer().RenderPage(new[] { chart }, options => { options.Nonce = nonce; }),
            "topology-fragment" => new TopologyHtmlRenderer().RenderFragment(topology, topologyOptions),
            "topology-page" => new TopologyHtmlRenderer().RenderPage(topology, topologyOptions),
            "topology-interactive-fragment" => new HtmlInteractiveTopologyRenderer().RenderFragment(topology, topologyOptions),
            "topology-interactive-page" => new HtmlInteractiveTopologyRenderer().RenderPage(topology, topologyOptions),
            "graph-fragment" => new HtmlGraphExplorerRenderer().RenderFragment(GraphScene.Create("map", "Map").AddNode("a", "A").AddNode("b", "B").AddEdge("ab", "a", "b"),
                options => { options.Nonce = nonce; }),
            "flow-interactive-page" => FlowRecipe.Create().ToInteractiveHtmlPage(configure: options => options.Nonce = nonce),
            "sequence-interactive-fragment" => SequenceRecipe.Create().ToInteractiveHtmlFragment(configure: options => options.Nonce = nonce),
            _ => throw new ArgumentOutOfRangeException(nameof(output))
        };
    }
}
