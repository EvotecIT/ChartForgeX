using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyGraphProjectionCarriesHierarchyAndArtwork() {
        var pack = new TopologyIconPack("custom", "Custom")
            .AddIcon(new TopologyIconDefinition("custom", "identity", "Identity", TopologyNodeKind.Service, TopologyIconShape.Service) {
                Symbol = "ID",
                Color = "#7C3AED",
                Artwork = TopologyIconArtwork.InlineSvg("<circle cx=\"12\" cy=\"12\" r=\"9\" fill=\"#7C3AED\"/><path d=\"M8 12h8\" stroke=\"white\" stroke-width=\"2\"/>")
            });
        var catalog = new TopologyIconCatalog().AddPack(pack);
        var items = new List<TopologyHierarchyItem> {
            new TopologyHierarchyItem("platform", "Platform") { Kind = TopologyNodeKind.Namespace },
            new TopologyHierarchyItem("identity", "Identity", "platform") { Kind = TopologyNodeKind.Service, IconId = "custom:identity", Subtitle = "Authentication", Symbol = null }
        };
        var topology = TopologyChart.Create().WithId("premium-hierarchy").AddHierarchy(items);
        topology.Nodes.Single(node => node.Id == "identity").Badge = "7";

        var scene = topology.ToGraphScene(options => options.IconCatalog = catalog);
        var identity = scene.Nodes.Single(node => node.Id == "identity");
        Assert(identity.ParentId == "platform" && identity.Level == 1, "Topology hierarchy metadata should become native graph parent and level fields.");
        Assert(identity.SecondaryLabel == "Authentication" && identity.BadgeText == "7", "Topology subtitles and badges should reach detailed graph node visuals.");
        Assert(identity.Shape == GraphNodeShape.Image && identity.ImageUrl != null && identity.ImageUrl.StartsWith("data:image/svg+xml;base64,", StringComparison.Ordinal), "Safe topology catalog artwork should become a self-contained graph image node.");
        Assert(identity.IconText == "ID" && identity.Style.BorderColor == "#7C3AED" && scene.Options.HasFeature(GraphSceneFeatures.HierarchyNavigation), "Topology icon symbols, colors, and hierarchy capability should survive the projection bridge.");
    }
}
