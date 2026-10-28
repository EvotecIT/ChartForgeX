using System;
using System.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;

internal static partial class Program {
    private static void NativeOrganizationHierarchySection() {
        var source = TopologyChart.Create().WithId("package-organization").WithViewport(720, 420).WithLegend(null).AddHierarchy(new[] {
            new TopologyHierarchyItem("lead", "Avery Quinn"), new TopologyHierarchyItem("engineering", "Engineering", "lead"),
            new TopologyHierarchyItem("platform", "Platform", "engineering"), new TopologyHierarchyItem("operations", "Operations", "lead")
        }, new TopologyHierarchyOptions { NodeDisplayMode = TopologyNodeDisplayMode.Card });
        source.Nodes.Single(node => node.Id == "engineering").Badge = "Owner";
        var section = new HtmlInteractiveTopologyRenderer().PrepareHierarchySection(source, new TopologyHierarchyView(new[] { "engineering" }), options => {
            options.IdScope = "package-organization"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        });
        PackageAssertions.Require(source.Nodes.Count == 4 && section.PreparedVisual.Regions.Count(region => region.Role == "topology-node") == 3
            && section.PreparedVisual.TopologyHierarchyView!.CollapsedNodeIds.SequenceEqual(new[] { "engineering" })
            && section.NarrowPreparedVisual!.TopologyHierarchyView!.CollapsedNodeIds.SequenceEqual(new[] { "engineering" }),
            "Packed organization adapter lost genuine source identity or matching adaptive native views.");
        PackageAssertions.Require(section.PreparedVisual.XProjection == null && section.PreparedVisual.YProjection == null && section.PreparedVisual.HierarchyView == null
            && section.Html.Contains("data-cfx-hierarchy-node=\"engineering\"", StringComparison.Ordinal), "Packed organization adapter manufactured numeric axes or lost branch controls.");
        var png = section.PreparedVisual.ToPng(); source.Nodes.Clear(); source.Edges.Clear();
        PackageAssertions.Require(png.SequenceEqual(section.PreparedVisual.ToPng()), "Packed organization source mutation changed prepared pixels.");
        PackageAssertions.Png(png);
    }
}
