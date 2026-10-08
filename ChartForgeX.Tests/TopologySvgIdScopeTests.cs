using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>An id scope keeps every SVG id of a topology render unique when one chart is embedded twice.</summary>
public sealed class TopologySvgIdScopeTests {
    [Fact]
    public void TwoScopedRenders_ShareNoIdAndEveryReferenceResolvesInItsOwnSvg() {
        var chart = Chart();
        var first = chart.ToSvg("panel-a");
        var second = chart.ToSvg("panel-b");

        var firstIds = Ids(first);
        var secondIds = Ids(second);
        var elements = XDocument.Parse(first).Descendants().ToArray();
        Assert.Contains(elements, element => (string?)element.Attribute("data-cfx-role") == "topology-marker");
        Assert.Contains(elements, element => (string?)element.Attribute("data-node-id") == "dc1");
        Assert.Contains(elements, element => (string?)element.Attribute("data-group-id") == "north");
        Assert.Contains(elements, element => (string?)element.Attribute("data-edge-id") == "a");
        Assert.Empty(firstIds.Intersect(secondIds));
        Assert.All(firstIds, id => Assert.StartsWith("panel-a-", id, StringComparison.Ordinal));

        foreach (var svg in new[] { first, second }) {
            var ids = Ids(svg);
            var references = References(svg).ToArray();
            Assert.NotEmpty(references);
            Assert.All(references, reference => Assert.Contains(reference, ids));
        }
    }

    [Fact]
    public void WithoutAScope_IdsAreDeterministicAndSourceIdentityIsRetained() {
        var chart = Chart();
        var svg = chart.ToSvg();
        Assert.Equal(svg, chart.ToSvg(new TopologyRenderOptions { IdScope = "  " }));
        Assert.Contains("data-chart-id=\"sites\"", svg, StringComparison.Ordinal);
        Assert.Contains("data-node-id=\"dc1\"", svg, StringComparison.Ordinal);
        Assert.All(References(svg), reference => Assert.Contains(reference, Ids(svg)));
        Assert.Equal(chart.ToSvg(new TopologyRenderOptions { IdScope = "panel-a" }), chart.ToSvg("panel-a"));
    }

    [Fact]
    public void ScopedRender_KeepsTheChartOptionsAndDoesNotChangeThem() {
        var carried = new TopologyRenderOptions { CssClassPrefix = "site-map" };
        var chart = Chart().WithRenderOptions(carried);
        var svg = chart.ToSvg("panel-a");
        Assert.Contains("class=\"site-map", svg, StringComparison.Ordinal);
        Assert.Null(carried.IdScope);
        var html = chart.ToHtmlFragment(new TopologyRenderOptions { IdScope = "panel-a" });
        Assert.Contains("data-node-id=\"dc1\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"panel-a-", html, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => chart.ToSvg(" "));
    }

    [Fact]
    public void ImportedIconArtworkIsIsolatedFromTheHostIdNamespace() {
        // The SVG pack importer rewrites artwork ids to cfxi-<pack>-<icon>-<id>; this body has that shape.
        var artwork = TopologyIconArtwork.InlineSvg("<defs><linearGradient id=\"cfxi-vendor-service-fill\"><stop offset=\"0\" stop-color=\"#2a78d6\"/></linearGradient></defs><rect width=\"24\" height=\"24\" fill=\"url(#cfxi-vendor-service-fill)\"/>", "0 0 24 24");
        var catalog = new TopologyIconCatalog().AddPack(new TopologyIconPack("vendor", "Vendor")
            .AddIcon(new TopologyIconDefinition("vendor", "service", "Service", TopologyNodeKind.Service).WithArtwork(artwork)));
        var chart = TopologyChart.Create().WithId("icons").AddIconNode("a", "Service", "vendor:service", 100, 100, catalog: catalog);
        var options = new TopologyRenderOptions { IconCatalog = catalog };

        var plain = chart.ToSvg(options);
        var scoped = chart.ToSvg("panel-a", options);
        var plainImage = XDocument.Parse(plain).Descendants().Single(element => element.Name.LocalName == "image");
        var scopedImage = XDocument.Parse(scoped).Descendants().Single(element => element.Name.LocalName == "image");
        Assert.Equal((string?)plainImage.Attribute("href"), (string?)scopedImage.Attribute("href"));
        Assert.StartsWith("data:image/png;base64,", (string?)scopedImage.Attribute("href"), StringComparison.Ordinal);
        Assert.DoesNotContain("cfxi-vendor-service-fill", scoped, StringComparison.Ordinal);
        Assert.All(References(scoped), reference => Assert.Contains(reference, Ids(scoped)));
    }

    [Fact]
    public void MotionPathsAndGeographicCallouts_AreScoped() {
        var motion = Chart().ToSvg("panel-a", new TopologyRenderOptions { Motion = TopologyMotionOptions.RoutePulseForEdges("a", "b") });
        Assert.Contains("data-cfx-role=\"topology-motion-marker\"", motion, StringComparison.Ordinal);
        AssertScopedAndResolved(motion, "panel-a-");

        var map = TopologyChart.Create()
            .WithId("map")
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("EMEA", "EMEA", 0, 0, 100, 80, TopologyHealthStatus.Warning, "56 sites")
            .WithGroupCoordinates("EMEA", 10, 50)
            .AddNode("emea-hub", "EMEA Hub", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "EMEA", width: 56, height: 44, symbol: "H")
            .WithNodeCoordinates("emea-hub", 0.1276, 51.5072);
        var callouts = map.ToSvg("panel-b", new TopologyRenderOptions { IncludeLegend = false, IncludeGroups = false, IncludeGeographicCallouts = true });
        Assert.Contains("data-cfx-visual-role=\"topology-geographic-callout\"", callouts, StringComparison.Ordinal);
        Assert.Contains("data-group-id=\"EMEA\"", callouts, StringComparison.Ordinal);
        AssertScopedAndResolved(callouts, "panel-b-");
    }

    [Fact]
    public void IconScoping_TouchesOnlyArtworkIdsAndTheirReferences() {
        var artwork = TopologyIconArtwork.InlineSvg("<defs><linearGradient id=\"cfxi-vendor-service-fill\"><stop offset=\"0\" stop-color=\"#2a78d6\"/></linearGradient></defs><rect width=\"24\" height=\"24\" fill=\"url(#cfxi-vendor-service-fill)\"/>", "0 0 24 24");
        var catalog = new TopologyIconCatalog().AddPack(new TopologyIconPack("vendor", "Vendor")
            .AddIcon(new TopologyIconDefinition("vendor", "service", "Service", TopologyNodeKind.Service).WithArtwork(artwork)));
        var chart = TopologyChart.Create().WithId("icons")
            .AddIconNode("cfxi-x", "Service", "vendor:service", 100, 100, catalog: catalog, tooltip: "See #cfxi-notes");
        var svg = chart.ToSvg("cfxi", new TopologyRenderOptions { IconCatalog = catalog });

        Assert.Contains("data-node-id=\"cfxi-x\"", svg, StringComparison.Ordinal);
        Assert.Contains("See #cfxi-notes", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("cfxi-vendor-service-fill", svg, StringComparison.Ordinal);
        AssertScopedAndResolved(svg, "cfxi-");
    }

    private static void AssertScopedAndResolved(string svg, string scope) {
        var ids = Ids(svg);
        Assert.All(ids, id => Assert.StartsWith(scope, id, StringComparison.Ordinal));
        Assert.All(References(svg), reference => Assert.Contains(reference, ids));
    }

    private static TopologyChart Chart() {
        var chart = TopologyChart.Create()
            .WithId("sites")
            .AddAutoGroup("north", "North")
            .AddAutoGroup("south", "South")
            .AddAutoNode("dc1", "DC1", groupId: "north")
            .AddAutoNode("dc2", "DC2", groupId: "north")
            .AddAutoNode("dc3", "DC3", groupId: "south")
            .AddEdge("a", "dc1", "dc2", direction: VisualLinkDirection.Forward)
            .AddEdge("b", "dc2", "dc3", direction: VisualLinkDirection.Forward);
        chart.Edges[1].SourceMarker = TopologyMarkerKind.Circle;
        return chart;
    }

    private static HashSet<string> Ids(string svg) =>
        new(XDocument.Parse(svg).Descendants().Select(element => (string?)element.Attribute("id")).Where(id => id != null)!, StringComparer.Ordinal);

    private static IEnumerable<string> References(string svg) {
        foreach (Match match in Regex.Matches(svg, "url\\(#([^)]+)\\)|href=\"#([^\"]+)\"")) yield return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        foreach (Match match in Regex.Matches(svg, "aria-labelledby=\"([^\"]+)\"")) {
            foreach (var id in match.Groups[1].Value.Split(' ')) yield return id;
        }
    }
}
