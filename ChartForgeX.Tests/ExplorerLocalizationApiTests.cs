using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ExplorerLocalizationApiTests {
    [Fact]
    public void GraphChromeKeepsAuthoredFactsAndLiteralPlaceholderText() {
        var graph = GraphScene.Create("localized-graph", "Authored title")
            .AddNode("a", "Source {target}", node => { node.Status = "Healthy"; node.X = 180; node.Y = 180; })
            .AddNode("b", "Target", node => { node.X = 480; node.Y = 180; })
            .AddEdge("flow", "a", "b");
        graph.Options.Physics.Repulsion.NodeDistance = 137;
        var native = new HtmlGraphExplorerRenderer().RenderStaticSvg(graph);
        var html = graph.ToGraphExplorerHtmlPage(options => {
            options.Localization.Language = "pl-PL";
            options.Localization.Direction = HtmlChartTextDirection.RightToLeft;
            options.Localization[HtmlChartText.GraphSearchPlaceholder] = "Znajd <węzeł>";
            options.Localization[HtmlChartText.GraphEndpoints] = "{source} → {target}";
            options.Localization[HtmlChartText.GraphSolverRepulsion] = "Odrzucanie";
        });
        Assert.Contains("<html lang=\"pl-PL\" dir=\"rtl\"", html);
        Assert.Contains("placeholder=\"" + WebUtility.HtmlEncode("Znajd <węzeł>") + "\"", html);
        Assert.Contains("aria-label=\"" + WebUtility.HtmlEncode("Source {target} → Target") + "\"", html);
        Assert.Equal("Source {target}", graph.Nodes[0].Label);
        Assert.Equal("Healthy", graph.Nodes[0].Status);
        Assert.Contains("data-cfx-physics-repulsion-node-distance=\"137\"", html);
        Assert.Equal(native, new HtmlGraphExplorerRenderer().RenderStaticSvg(graph));
    }

    [Fact]
    public void ExistingAndLocalizedTopologyExtensionCallsRemainDistinctAndDetached() {
        var chart = Topology();
        var options = new TopologyRenderOptions { EnableHtmlSelectionPanel = true, EnableHtmlViewportControls = true };
        var native = chart.ToSvg();
        Assert.Contains("Topology controls", chart.ToInteractiveHtmlPage());
        Assert.Contains("Topology controls", chart.ToInteractiveHtmlPage(options));
        Assert.Contains("Topology controls", chart.ToInteractiveHtmlPage(options: null));
        Assert.Throws<ArgumentNullException>(() => chart.ToInteractiveHtmlPage(options, (Func<PreparedTopology, string>)null!));
        var presentations = 0;
        Func<PreparedTopology, string> presentation = prepared => { presentations++; return prepared.ToSvg(); };
        _ = chart.ToInteractiveHtmlPage(options, presentation);
        Assert.Equal(1, presentations);
        void Configure(HtmlChartLocalization ui) {
            ui.Language = "pl-PL";
            ui[HtmlChartText.TopologyControls] = "Sterowanie topologią";
            ui[HtmlChartText.ExplorerNoItemSelected] = "Brak wyboru";
        }
        var html = chart.ToInteractiveHtmlPage(options, configureLocalization: Configure);
        Assert.Contains("aria-label=\"" + WebUtility.HtmlEncode("Sterowanie topologią") + "\"", html);
        Assert.Contains("Brak wyboru", html);
        _ = chart.ToInteractiveHtmlPage(options, presentation, Configure);
        Assert.Equal(2, presentations);
        Assert.Contains("Brak wyboru", chart.ToInteractiveHtmlFragment(options, Configure));
        var fragment = chart.ToInteractiveHtmlFragmentWithoutAssets(options, Configure);
        Assert.Contains("Brak wyboru", fragment);
        Assert.DoesNotContain("<script", fragment);
        Assert.False(options.EnableHtmlInteractions);
        Assert.Equal(native, chart.ToSvg());
        var path = Path.Combine(Path.GetTempPath(), "cfx-localization-" + Guid.NewGuid().ToString("N") + ".html");
        try {
            chart.SaveInteractiveHtml(path, options, configureLocalization: Configure);
            Assert.Contains("Brak wyboru", File.ReadAllText(path));
        } finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void OrdinaryMessageProjectionDoesNotAcquireExplorerMessages() {
        var html = Chart.Create().ToInteractiveHtmlPage(options => options.Localization[HtmlChartText.GraphSearch] = "Explorer-only sentinel");
        var raw = Regex.Match(html, "data-cfx-ui=\"([^\"]*)\"").Groups[1].Value;
        using var bundle = JsonDocument.Parse(WebUtility.HtmlDecode(raw));
        Assert.True(bundle.RootElement.TryGetProperty("ZoomIn", out _));
        Assert.False(bundle.RootElement.TryGetProperty("GraphSearch", out _));
    }

    [Fact]
    public void ReportMessagesDoNotChangePreparedPagesOrAuthoredObjectRecords() {
        var report = Topology().PrepareReport(new TopologyReportOptions { MaximumNodesPerPage = 1 });
        var native = report.Pages.Select(page => page.ToSvg()).Prepend(report.Overview.ToSvg()).ToArray();
        var html = report.ToInteractiveHtmlPage(ui => {
            ui.Language = "ar";
            ui.Direction = HtmlChartTextDirection.RightToLeft;
            ui[HtmlChartText.ReportFindPlaceholder] = "اسم <معرّف>";
            ui[HtmlChartText.ReportOtherRelationships] = "علاقات الصفحات";
            ui[HtmlChartText.ReportSummary] = "عناصر {objects} / صفحات {pages} / علاقات {relationships}";
        });
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", html);
        Assert.Contains("placeholder=\"" + WebUtility.HtmlEncode("اسم <معرّف>") + "\"", html);
        Assert.Equal(native, report.Pages.Select(page => page.ToSvg()).Prepend(report.Overview.ToSvg()).ToArray());
        var encodedPage = Regex.Match(html, "id=\"page-1\">([^<]*)</script>").Groups[1].Value;
        var detailPage = JsonSerializer.Deserialize<string>(encodedPage)!;
        Assert.Contains("<h2>" + WebUtility.HtmlEncode("علاقات الصفحات") + "</h2>", detailPage);
        Assert.Contains("source-a", WebUtility.HtmlDecode(html));
        Assert.Contains("Authored source", WebUtility.HtmlDecode(html));
    }

    private static TopologyChart Topology() => TopologyChart.Create().WithId("localized-topology").WithTitle("Authored title")
        .AddNode("source-a", "Authored source", 40, 60, status: TopologyHealthStatus.Healthy)
        .AddNode("source-b", "Authored target", 280, 60)
        .AddEdge("authored-link", "source-a", "source-b", "Raw relationship");
}
