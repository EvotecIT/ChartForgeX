using ChartForgeX;
using ChartForgeX.Core;
using System.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void HierarchyAndFlowSvgExposeDataMetadata() {
        var treemap = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).AddTreemap("Findings", new[] {
            new ChartTreemapItem("Critical", 50),
            new ChartTreemapItem("High", 28)
        }).ToSvg();
        FamilyMetadata(treemap, "treemap-tile", ("point", "0"), ("label", "Critical"), ("value", "50"));
        var positionedTreemap = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithLegend(true).WithLegendPosition(ChartLegendPosition.Right)
            .AddTreemap("Findings", new[] {
                new ChartTreemapItem("Critical", 50),
                new ChartTreemapItem("High", 28)
            });
        var positioned = PreparedFamily(positionedTreemap);
        var tile = positioned.Regions.First(region => region.Role == "treemap-tile");
        var legend = positioned.Regions.Where(region => region.Role == "legend").ToArray();
        Assert(legend.Length > 0 && legend.All(region => region.Bounds.Left >= tile.Bounds.Right),
            "The common right legend should occupy space beside the treemap content.");
        Assert(positionedTreemap.ToPng().Length > 64, "Positioned treemap legends should render PNG output.");

        var tree = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).AddTree("Hierarchy", new[] {
            new ChartTreeLink("Root", "Mail", 3),
            new ChartTreeLink("Mail", "SPF", 2)
        }).ToSvg();
        FamilyMetadata(tree, "tree-node", ("point", "0"), ("depth", "0"), ("label", "Root"));
        FamilyMetadata(tree, "tree-link", ("parent", "0"), ("child", "1"), ("value", "3"), ("source-label", "Root"), ("target-label", "Mail"));

        var sunburst = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).AddSunburst("Hierarchy", new[] {
            new ChartTreeLink("Root", "Mail", 3),
            new ChartTreeLink("Mail", "SPF", 2)
        }).ToSvg();
        FamilyMetadata(sunburst, "sunburst-segment", ("point", "0"), ("parent", "-1"), ("depth", "0"), ("label", "Root"));
        FamilyMetadata(sunburst, "sunburst-segment", ("point", "1"), ("parent", "0"), ("depth", "1"), ("label", "Mail"));

        var sankey = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).AddSankey("Flow", new[] {
            new ChartSankeyLink("Discovered", "Validated", 70),
            new ChartSankeyLink("Validated", "Remediated", 44)
        }).ToSvg();
        FamilyMetadata(sankey, "sankey-node", ("node", "0"), ("layer", "0"), ("label", "Discovered"), ("value", "70"));
        FamilyMetadata(sankey, "sankey-link", ("source", "0"), ("target", "1"), ("value", "70"), ("source-label", "Discovered"), ("target-label", "Validated"));
    }
}
