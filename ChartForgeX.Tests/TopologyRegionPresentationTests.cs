using System.Xml.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyRegionPresentationTests {
    [Theory]
    [InlineData(1440, false)]
    [InlineData(390, true)]
    public void RegionLayoutIsDeterministicDetachedAndUsesReferenceAnatomy(int width, bool dark) {
        var chart = TopologyHierarchyExamples.CreateTopology(width, dark);
        var options = new TopologyRenderOptions { IncludeRegionDetails = true };
        var prepared = chart.Prepare(options);
        var svg = prepared.ToSvg();
        Assert.Equal(svg, chart.ToSvg(options));
        Assert.Contains("cfx-topo", svg); Assert.Contains("cfx-region", svg); Assert.Contains("cfx-node", svg);
        Assert.Contains("cfx-edge", svg); Assert.Contains("cfx-legend", svg);
        Assert.DoesNotContain("<script", svg);
        var nodes = prepared.ToInterchangeEnvelope().Nodes;
        Assert.Equal(width < 640 ? 52 : 60, nodes.Single(node => node.Id == "gateway").Width);
        Assert.Equal(width < 640 ? 36 : 40, nodes.Single(node => node.Id == "cache").Width);
        chart.Nodes[0].Label = "Changed source";
        Assert.Equal(svg, prepared.ToSvg());
        Assert.True(prepared.ToPng().Length > 1000);
        Assert.Equal(width < 640, svg.Contains("Links to other regions"));
    }

    [Fact]
    public void BadgesNamesAndWeightsSurviveInterchangeWithoutLosingFullText() {
        var chart = TopologyHierarchyExamples.CreateTopology(1440, false);
        var node = chart.Nodes[0];
        node.StatusText = "Healthy <fresh>";
        node.AddBadge(new TopologyNodeBadge("lock", "Protected <key>", true));
        node.AddBadge(new TopologyNodeBadge("star", "Third badge"));
        node.Subtitle = "A long subtitle with {{literal}} template-looking text";
        var prepared = chart.Prepare();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToInterchangeEnvelope().ToJson());
        Assert.Equal("Primary", restored.Nodes[0].Extensions["chartforgex.region.weight"]);
        Assert.Equal("Third badge", restored.Nodes[0].Extensions["chartforgex.region.badge.2.meaning"]);
        var document = XDocument.Parse(prepared.ToSvg());
        var accessible = document.Descendants().Single(element => (string?)element.Attribute("data-node-id") == node.Id);
        Assert.Contains(node.Subtitle, (string?)accessible.Attribute("aria-label"));
        Assert.Contains("Third badge", (string?)accessible.Attribute("aria-label"));
        var html = chart.ToHtmlFragment(new TopologyRenderOptions { IncludeRegionDetails = true });
        Assert.Contains("Protected &lt;key&gt;", html);
        Assert.DoesNotContain("Protected <key>", html);
    }

    [Fact]
    public void ManualPositionsArePreservedAndOverlappingManualCoresAreRejected() {
        var chart = TopologyHierarchyExamples.CreateTopology(1440, false);
        chart.Nodes[0].X = 100; chart.Nodes[0].Y = 160;
        var node = chart.Prepare().ToInterchangeEnvelope().Nodes[0];
        Assert.Equal(100, node.X); Assert.Equal(160, node.Y);
        chart.Nodes[1].X = 100; chart.Nodes[1].Y = 160;
        Assert.Contains("overlap", Assert.Throws<InvalidOperationException>(() => chart.ToSvg()).Message);
    }

    [Fact]
    public void PrintUsesLightTokensAndNonColourStatusPatterns() {
        var chart = TopologyHierarchyExamples.CreateTopology(1440, true);
        var svg = chart.ToSvg(new TopologyRenderOptions { RegionPrint = true });
        Assert.Contains("stroke-dasharray=\"2 4\"", svg);
        Assert.Contains("stroke-dasharray=\"8 6\"", svg);
        Assert.Contains("#C2413A", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#F0716A", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#F0716A", chart.ToHtmlFragment(new TopologyRenderOptions { RegionPrint = true }), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VisibleEdgeLabelsNeverCoverNodeCoresSubtitlesOrEachOther() {
        var envelope = TopologyHierarchyExamples.CreateTopology(1440, false).Prepare().ToInterchangeEnvelope();
        var labels = envelope.Edges.Where(edge => edge.ResolvedLabelBounds.HasValue).Select(edge => edge.ResolvedLabelBounds!.Value).ToArray();
        Assert.NotEmpty(labels);
        foreach (var label in labels) foreach (var node in envelope.Nodes) {
            Assert.False(Overlap(label, new ChartForgeX.Primitives.ChartRect(node.X!.Value, node.Y!.Value, node.Width!.Value, node.Height!.Value)));
            Assert.False(Overlap(label, new ChartForgeX.Primitives.ChartRect(node.X!.Value + node.Width!.Value / 2 - 70, node.Y!.Value + node.Height!.Value + 4, 140, 18)));
        }
        for (var i = 0; i < labels.Length; i++) for (var j = i + 1; j < labels.Length; j++) Assert.False(Overlap(labels[i], labels[j]));
    }
    private static bool Overlap(ChartForgeX.Primitives.ChartRect a, ChartForgeX.Primitives.ChartRect b) => a.X < b.Right && a.Right > b.X && a.Y < b.Bottom && a.Bottom > b.Y;

    [Fact]
    public void OptionalRegionStatusChipsKeepHostWordingAndEscaping() {
        var chart = TopologyHierarchyExamples.CreateTopology(1440, false);
        chart.Groups[0].StatusText = "<Busy>";
        var prepared = chart.Prepare();
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(2, svg.Descendants().Count(element => ((string?)element.Attribute("class"))?.StartsWith("cfx-region-status ", StringComparison.Ordinal) == true));
        Assert.Contains("&lt;Busy&gt;", prepared.ToSvg());
        Assert.Equal("<Busy>", prepared.ToInterchangeEnvelope().Groups[0].Extensions["chartforgex.region.status"]);
        chart.Groups[0].StatusText = null;
        Assert.Single(XDocument.Parse(chart.ToSvg()).Descendants(), element => ((string?)element.Attribute("class"))?.StartsWith("cfx-region-status ", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ReverseParallelRelationshipsUseDistinctPreparedArcsAndPreserveInformationalTone() {
        var chart = TopologyHierarchyExamples.CreateTopology(1440, false);
        chart.AddEdge("ack", "worker", "gateway", "Ack");
        chart.Edges.Last().Tone = TopologyTone.Info;
        var envelope = chart.Prepare().ToInterchangeEnvelope();
        var forward = envelope.Edges.Single(edge => edge.Id == "process").ResolvedRoute;
        var reverse = envelope.Edges.Single(edge => edge.Id == "ack").ResolvedRoute;
        Assert.True(forward.Count > 3 && reverse.Count > 3);
        Assert.NotEqual(forward[forward.Count / 2].Y, reverse[reverse.Count / 2].Y);
        Assert.Equal("Info", envelope.Edges.Single(edge => edge.Id == "ack").Extensions["chartforgex.region.tone"]);
    }

    [Fact]
    public void TemplatePaintsKeepThemeTokenIdentityAndStaticFallbacks() {
        var chart = TopologyHierarchyExamples.CreateTopology(1440, false);
        chart.Nodes[0].Tone = TopologyTone.Info;
        var policy = new TopologyRenderOptions { SvgColorVariables = new ChartForgeX.Themes.SvgColorVariables()
            .AddThemeToken(ChartForgeX.Themes.VisualThemeColorToken.Info, "--host-info")
            .AddThemeToken(ChartForgeX.Themes.VisualThemeColorToken.ElevatedSurface, "--host-surface") };
        var svg = chart.ToSvg(policy);
        Assert.Contains("var(--host-info,", svg);
        Assert.Contains("var(--host-surface,", svg);
        Assert.Equal(chart.ToPng(), chart.ToPng(policy));
    }

    [Fact]
    public void ProducerSvgCallbacksRunOnceAndAssetFreeFragmentsKeepHostOwnership() {
        var chart = TopologyHierarchyExamples.CreateTopology(1440, false);
        var calls = 0;
        var html = new ChartForgeX.Interactivity.Html.HtmlInteractiveTopologyRenderer().RenderPresentationPage(chart, prepared => {
            calls++; return prepared.ToSvg().Replace("</svg>", "<desc>producer marker</desc></svg>", StringComparison.Ordinal);
        });
        Assert.Equal(1, calls);
        Assert.Contains("producer marker", html);
        Assert.Contains("cfx-region-custom", html);
        Assert.DoesNotContain("<style", chart.ToHtmlFragmentWithoutAssets());
    }
}
