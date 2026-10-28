using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyStyledLayeredAllocationTests {
    [Theory]
    [InlineData("mirrored")] [InlineData("horizontal")] [InlineData("group")]
    [InlineData("compound")] [InlineData("bucket")] [InlineData("long-lane")]
    public void EveryReachableLayeredAllocatorReservesTheActualOptedInSubtitleInkAndKeepsClearedDefaultsExact(string pose) {
        var chart = Source(pose); var ordinary = chart.Prepare(Options());
        var ordinarySvg = ordinary.ToSvg(); var ordinaryPng = ordinary.ToPng(); var ordinaryJson = ordinary.ToInterchangeEnvelope().ToJson();
        var authored = chart.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height, node.GroupId)).ToArray();
        foreach (var node in chart.Nodes) node.SubtitleTextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family, FontSize = 64, Color = ChartColor.FromHex("#2367AB") };
        var prepared = chart.Prepare(Options()); var report = prepared.Analyze(); var xml = XDocument.Parse(prepared.ToSvg());
        foreach (var node in report.Nodes) {
            var chip = Chip(xml, node.Id); var caption = node.CaptionBounds!.Value;
            Assert.True(chip.Height > 17, "The fixture must exercise the authored subtitle size rather than the ordinary chip.");
            Assert.True(caption.Left <= chip.Left + .01 && caption.Right + .01 >= chip.Right);
            Assert.True(caption.Top <= chip.Top + .01 && caption.Bottom + .01 >= chip.Bottom);
            Assert.DoesNotContain(report.Nodes, other => other.Id != node.Id && InteriorOverlap(chip, other.Bounds));
        }
        var facts = prepared.ToInterchangeEnvelope();
        Assert.Equal(chart.Nodes.Select(node => node.Id), facts.Nodes.Select(node => node.Id));
        // Envelope typography is in prepared output units; authored source typography remains unscaled.
        var nativeScale = prepared.Geometry.Scale;
        Assert.All(facts.Nodes, node => Assert.Equal(64d * nativeScale, node.Topology!.SubtitleTextStyle!.FontSize));
        Assert.All(chart.Nodes, node => Assert.Equal(64d, node.SubtitleTextStyle!.FontSize));
        if (pose is "group" or "compound") {
            foreach (var node in chart.Nodes.Where(node => node.GroupId != null)) {
                var group = facts.Groups.Single(item => item.Id == node.GroupId);
                var chip = Chip(xml, node.Id);
                Assert.True(chip.Left >= group.X!.Value - .01 && chip.Right <= group.X.Value + group.Width!.Value + .01);
                Assert.True(chip.Top >= group.Y!.Value - .01 && chip.Bottom <= group.Y.Value + group.Height!.Value + .01);
            }
        }
        if (pose == "bucket") Assert.All(facts.Nodes.Where(node => node.Id != "first"), node => Assert.Equal("Vertical", node.Extensions["layout.hierarchyPolicy"]));
        if (pose == "long-lane") Assert.Equal("long-edge-lane", facts.Edges.Single(edge => edge.Id == "skip").Extensions["layout.route"]);
        Assert.Equal(authored, chart.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height, node.GroupId)));
        Capture(prepared, "styled-layered-" + pose);
        foreach (var node in chart.Nodes) node.SubtitleTextStyle = null;
        var cleared = chart.Prepare(Options());
        Assert.Equal(ordinarySvg, cleared.ToSvg()); Assert.Equal(ordinaryPng, cleared.ToPng()); Assert.Equal(ordinaryJson, cleared.ToInterchangeEnvelope().ToJson());
        Assert.Equal(ordinary.Analyze().Nodes.Select(node => (node.Id, node.Bounds, node.CaptionBounds)), cleared.Analyze().Nodes.Select(node => (node.Id, node.Bounds, node.CaptionBounds)));
    }

    private static TopologyChart Source(string pose) {
        var direction = pose == "mirrored" ? TopologyLayoutDirection.BottomToTop
            : pose is "horizontal" or "long-lane" ? TopologyLayoutDirection.LeftToRight : TopologyLayoutDirection.TopToBottom;
        var chart = TopologyChart.Create().WithId("styled-layered-" + pose).WithViewport(1600, 1400, 24).WithLegend(null)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithLayout(TopologyLayoutMode.Layered, direction)
            .AddAutoNode("first", "First", width: 100, height: 80).AddAutoNode("second", "Second", width: 100, height: 80)
            .AddEdge("advance", "first", "second");
        chart.Nodes[0].Metadata["layer"] = "0"; chart.Nodes[1].Metadata["layer"] = "1";
        if (pose is "horizontal" or "group" or "bucket") {
            chart.AddAutoNode("peer", "Peer", width: 100, height: 80).AddEdge("branch", "first", "peer");
            chart.Nodes[2].Metadata["layer"] = "1";
        }
        if (pose == "group") {
            chart.AddAutoGroup("alpha", "Team"); chart.Nodes[0].GroupId = "alpha"; chart.Nodes[1].GroupId = "alpha";
        }
        if (pose == "compound") {
            chart.AddAutoGroup("outside", "Outer").AddAutoGroup("inside", "Inner");
            chart.Groups[1].ParentGroupId = "outside"; chart.Groups[1].LayoutDirection = TopologyLayoutDirection.TopToBottom;
            chart.Nodes[0].GroupId = "inside"; chart.Nodes[1].GroupId = "inside";
        }
        if (pose == "bucket") {
            chart.Nodes[0].Metadata["hierarchy.layoutPolicy"] = "Vertical";
            chart.Nodes[1].Metadata["hierarchy.parentId"] = "first"; chart.Nodes[2].Metadata["hierarchy.parentId"] = "first";
        }
        if (pose == "long-lane") {
            chart.Edges[0].Id = "skip"; chart.Nodes[1].Metadata["layer"] = "2";
            chart.AddAutoNode("middle", "Middle", width: 100, height: 80).AddAutoNode("peer", "Peer", width: 100, height: 80)
                .AddEdge("via-middle", "first", "middle").AddEdge("from-middle", "middle", "second")
                .AddEdge("via-peer", "first", "peer").AddEdge("from-peer", "peer", "second");
            chart.Nodes[2].Metadata["layer"] = "1"; chart.Nodes[3].Metadata["layer"] = "1";
        }
        foreach (var node in chart.Nodes) { node.Subtitle = "Standby"; node.ShowStatusBadge = false; }
        return chart;
    }
    private static TopologyRenderOptions Options() => new() {
        NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeTileSubtitles = true, ReadableDenseLayout = true,
        IncludeLegend = false, IncludeStatusBadges = false, LayeredRankSpacing = 8, FitContentToViewport = true
    };
    private static bool InteriorOverlap(ChartRect first, ChartRect second) => first.Left < second.Right && first.Right > second.Left && first.Top < second.Bottom && first.Bottom > second.Top;
    private static ChartRect Chip(XDocument document, string id) {
        var element = Assert.Single(document.Descendants(), item => item.Name.LocalName == "rect" && (string?)item.Attribute("data-cfx-role") == "topology-subtitle-chip"
            && item.Ancestors().Any(parent => (string?)parent.Attribute("data-node-id") == id));
        return new ChartRect((double)element.Attribute("x")!, (double)element.Attribute("y")!, (double)element.Attribute("width")!, (double)element.Attribute("height")!);
    }
    private static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), prepared.ToPng()); File.WriteAllText(Path.Combine(output, name + ".json"), prepared.ToInterchangeEnvelope().ToJson());
    }
}
