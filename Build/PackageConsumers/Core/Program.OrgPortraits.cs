using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void OrganizationPortraitContracts() {
        var bytes = new byte[8 * 4 * 4];
        for (var index = 0; index < 32; index++) { bytes[index * 4] = 32; bytes[index * 4 + 1] = 160; bytes[index * 4 + 2] = 96; bytes[index * 4 + 3] = 255; }
        var artwork = TopologyIconArtwork.Portrait(new RgbaImage(8, 4, bytes), TopologyPortraitFit.Slice);
        var chart = TopologyChart.Create().WithViewport(600, 320).WithLegend(null).AddTeam("team", "Example team", new[] {
            new TopologyTeamMember("person", "Avery Quinn", "Example role") { Artwork = artwork }
        }, new TopologyTeamOptions { IncludeTeamNode = false });
        var captured = chart.Nodes[0].Artwork!.ImageHref; Array.Clear(bytes, 0, bytes.Length); artwork.ImageHref = null;
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false }); var svg = prepared.ToSvg();
        PackageAssertions.Require(XDocument.Parse(svg).Descendants().Any(node => node.Name.LocalName == "image"),
            "Packed organization portraits did not use native captured image media.");
        var restored = chart.ToVisualArtifact().ToInterchangeEnvelope().ToTopologyChart();
        PackageAssertions.Require(restored.Nodes[0].Id == "person" && restored.Nodes[0].Artwork!.ImageHref == captured
            && restored.Nodes[0].Artwork.PreserveAspectRatio == "xMidYMid slice", "Packed portrait identity or crop was lost in interchange.");
        chart.Nodes[0].Artwork = null;
        PackageAssertions.Require(prepared.ToSvg() == svg, "Changing a source portrait changed its prepared export.");
        PackageAssertions.Png(prepared.ToPng()); PackageAssertions.Png(restored.ToPng(new TopologyRenderOptions { IncludeLegend = false }));
    }
}
