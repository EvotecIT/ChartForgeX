using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Verify = Xunit.Assert;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static PreparedVisual PreparedFamily(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneGroup[] FamilyGroups(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == role).ToArray();
    private static VisualSceneText[] FamilyLabels(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == role).ToArray();
    private static double FamilyNumber(VisualSceneGroup group, string key) => double.Parse(group.Metadata[key], CultureInfo.InvariantCulture);
    private static string FamilyContent(VisualSceneText label) => string.Join("\n", label.Text.Lines.Select(line => line.Text));

    private static void AssertFamilyTextFitsReservedRegion(PreparedVisual prepared, string role, string full) {
        var labels = FamilyLabels(prepared, role);
        Verify.NotEmpty(labels);
        Verify.Contains(prepared.Regions, region => region.Role == role && region.Label!.Contains(full));
        foreach (var label in labels) {
            var region = Verify.Single(prepared.Regions, candidate => candidate.Id == label.Id && candidate.Role == role);
            Verify.True(label.Text.Metrics.Width <= region.Bounds.Width + .01);
            Verify.True(label.Text.Metrics.Height <= region.Bounds.Height + .01);
        }
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void GaugeSeriesRenderValueArcs() {
        var chart = Chart.Create().WithSize(640, 420).WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%").AddGauge("Security score", 87);
        var prepared = PreparedFamily(chart);
        var source = Verify.Single(FamilyGroups(prepared, "gauge"));
        Verify.Equal(87, FamilyNumber(source, "data-cfx-value"));
        Verify.Equal(.87, FamilyNumber(source, "data-cfx-percent"));
        Verify.Equal("Security score: 87%", source.Metadata["aria-label"]);
        Verify.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "gauge-track");
        var arc = Verify.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "gauge-value");
        Verify.True(arc.Outer > arc.Inner);
        Verify.Equal(87d / 100, arc.Sweep / (Math.PI * 4 / 3), 6);
        Verify.Contains(FamilyLabels(prepared, "gauge-label"), label => FamilyContent(label) == "87%");
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void RadialBarSeriesRenderProgressRings() {
        var chart = Chart.Create().WithSize(720, 460).WithXLabels("Mail auth", "DNSSEC", "TLS")
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%").AddRadialBar("Control coverage", Points(92, 74, 88));
        var prepared = PreparedFamily(chart);
        Verify.Equal(3, FamilyGroups(prepared, "radial-bar-point").Length);
        var tracks = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(node => node.Role == "radial-bar-track").ToArray();
        var rings = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(node => node.Role == "radial-bar-ring").ToArray();
        Verify.Equal(3, tracks.Length); Verify.Equal(3, rings.Length);
        for (var index = 0; index < rings.Length; index++) {
            Verify.Equal(chart.Series[0].Points[index].Y / 100, rings[index].Sweep / tracks[index].Sweep, 6);
            Verify.Equal(tracks[index].Outer, rings[index].Outer);
        }
        Verify.Contains(prepared.Regions, region => region.Role == "radial-bar-ring" && region.Label == "Mail auth: 92%");
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void SankeyLinksRenderWeightedFlows() {
        var chart = Chart.Create().WithSize(900, 520).WithDataLabels().AddSankey("Finding flow", new[] { new ChartNode("Discovered", "Discovered"), new ChartNode("Validated", "Validated"), new ChartNode("Accepted risk", "Accepted risk"), new ChartNode("Remediated", "Remediated"), new ChartNode("Monitoring", "Monitoring") }, new[] {
            new ChartFlowLink("flow-1", "Discovered", "Validated", 70), new("flow-2", "Discovered", "Accepted risk", 20),
            new("flow-3", "Validated", "Remediated", 44), new("flow-4", "Validated", "Monitoring", 26)
        });
        var prepared = PreparedFamily(chart);
        var links = FamilyGroups(prepared, "sankey-link");
        Verify.Equal(4, links.Length); Verify.Equal(5, FamilyGroups(prepared, "sankey-node").Length);
        Verify.Equal(new[] { 70d, 20, 44, 26 }, links.Select(link => FamilyNumber(link, "data-cfx-value")));
        Verify.Equal(3.5, FamilyNumber(links[0], "data-cfx-width") / FamilyNumber(links[1], "data-cfx-width"), 6);
        Verify.Contains(prepared.Regions, region => region.Role == "sankey-link" && region.Label == "Discovered to Validated: 70");
        Verify.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "sankey-ribbon"));
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void TreeLinksRenderHierarchy() {
        var chart = Chart.Create().WithSize(900, 520).AddTree("Control hierarchy", new[] { new ChartNode("Security posture", "Security posture"), new ChartNode("Mail authentication", "Mail authentication"), new ChartNode("Certificate lifecycle", "Certificate lifecycle"), new ChartNode("SPF", "SPF"), new ChartNode("DKIM", "DKIM"), new ChartNode("Expiry monitoring", "Expiry monitoring") }, new[] {
            new ChartTreeLink("Security posture", "Mail authentication"), new("Security posture", "Certificate lifecycle"),
            new("Mail authentication", "SPF"), new("Mail authentication", "DKIM"), new("Certificate lifecycle", "Expiry monitoring")
        });
        var prepared = PreparedFamily(chart);
        Verify.Equal(5, FamilyGroups(prepared, "tree-link").Length);
        var nodes = FamilyGroups(prepared, "tree-node");
        Verify.Equal(6, nodes.Length);
        Verify.Contains(nodes, node => FamilyNumber(node, "data-cfx-depth") == 2);
        Verify.Contains(prepared.Regions, region => region.Role == "tree-node" && region.Label == "Security posture: level 0");
        Verify.NotEmpty(FamilyLabels(prepared, "tree-node-label")); Verify.NotEmpty(prepared.ToPng());
    }

    private static void BulletSeriesRenderTargetAndRangeBars() {
        var chart = Chart.Create().WithSize(720, 420).WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .AddBullet("DMARC enforcement", 88, 95, 0, 100, new[] { 60d, 80d }, ChartColor.FromRgb(37, 99, 235))
            .AddBullet("DNSSEC coverage", 74, 90, 0, 100, new[] { 60d, 80d }, ChartColor.FromRgb(14, 165, 233));
        var prepared = PreparedFamily(chart);
        var rows = FamilyGroups(prepared, "bullet-row");
        Verify.Equal(2, rows.Length); Verify.All(rows, row => Verify.Equal("below-target", row.Metadata["data-cfx-status"]));
        Verify.Equal(6, prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Count(node => node.Role == "bullet-range"));
        Verify.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "bullet-target"));
        Verify.Equal(new[] { "88%", "74%" }, FamilyLabels(prepared, "bullet-value-label").Select(label => FamilyContent(label)));
        Verify.Equal(new[] { "/ 95%", "/ 90%" }, FamilyLabels(prepared, "bullet-target-label").Select(label => FamilyContent(label)));
        Verify.Equal(new[] { "0%", "25%", "50%", "75%", "100%" }, FamilyLabels(prepared, "bullet-axis-label").Select(label => FamilyContent(label)));
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void RadarSeriesRenderPolarPolygons() {
        var chart = Chart.Create().WithSize(760, 460).WithDataLabels().WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy")
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .AddRadar("Current", Points(92, 74, 88, 96, 81)).AddRadar("Target", Points(96, 90, 92, 98, 90));
        var prepared = PreparedFamily(chart);
        Verify.Equal(2, FamilyGroups(prepared, "radar-series").Length);
        Verify.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "radar-area"));
        Verify.Equal(5, prepared.Scene.Nodes.Count(node => node.Role == "radar-spoke"));
        Verify.Equal(10, FamilyGroups(prepared, "radar-point-source").Length);
        Verify.Contains(prepared.Regions, region => region.Role == "radar-point" && region.Label == "Mail auth: 92%");
        Verify.Contains(FamilyLabels(prepared, "radar-axis-label"), label => FamilyContent(label) == "Mail auth");
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void PolarAreaSeriesRenderRadialSegments() {
        var chart = Chart.Create().WithSize(760, 460).WithDataLabels().WithXLabels("Mail auth", "DNSSEC", "TLS", "CT")
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%").AddPolarArea("Control share", Points(92, 74, 88, 96));
        var prepared = PreparedFamily(chart);
        var segments = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(node => node.Role == "polar-area-segment").ToArray();
        Verify.Equal(4, segments.Length);
        Verify.All(segments, segment => Verify.Equal(Math.PI / 2, segment.Sweep, 6));
        Verify.True(segments[3].Outer > segments[1].Outer);
        Verify.Equal(4, FamilyGroups(prepared, "polar-area-point-source").Length);
        Verify.Contains(prepared.Regions, region => region.Role == "polar-area-segment" && region.Label!.Contains("Mail auth") && region.Label.Contains("92%"));
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void CircleSeriesRenderSingleProgressRings() {
        var chart = Chart.Create().WithSize(760, 460).WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .AddCircle("Readiness", 87, 0, 100, ChartColor.FromRgb(52, 211, 153));
        var prepared = PreparedFamily(chart);
        var source = Verify.Single(FamilyGroups(prepared, "circle-chart"));
        Verify.Equal(.87, FamilyNumber(source, "data-cfx-percent")); Verify.Equal(87, FamilyNumber(source, "data-cfx-value"));
        var track = Verify.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "circle-track");
        var value = Verify.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "circle-value");
        Verify.Equal(.87, value.Sweep / track.Sweep, 6);
        Verify.Contains(FamilyLabels(prepared, "circle-label"), label => FamilyContent(label) == "87%");
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void FunnelSeriesRenderStagedSegments() {
        var chart = Chart.Create().WithSize(760, 460).WithDataLabels().WithXLabels("Discovered", "Verified", "Prioritized", "Remediated")
            .AddFunnel("Domain remediation funnel", Points(420, 318, 174, 96));
        var prepared = PreparedFamily(chart);
        var stages = FamilyGroups(prepared, "funnel-stage");
        Verify.Equal(4, stages.Length); Verify.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "funnel-segment"));
        Verify.Equal(318d / 420, FamilyNumber(stages[1], "data-cfx-retention"), 12);
        Verify.Equal(102d / 420, FamilyNumber(stages[1], "data-cfx-dropoff"), 12);
        Verify.Contains(prepared.Regions, region => region.Role == "funnel-stage" && region.Label!.Contains("Verified: 318, retained 75.7%, drop-off 24.3%"));
        Verify.Equal(3, FamilyLabels(prepared, "funnel-ratio").Length);
        Verify.Contains(FamilyLabels(prepared, "funnel-ratio"), label => FamilyContent(label).Contains("75.7% retained"));
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void TreemapItemsRenderProportionalTiles() {
        var chart = Chart.Create().WithSize(720, 420).WithDataLabels().AddTreemap("Findings", new[] {
            new ChartTreemapItem("Critical", 50), new("High", 28), new("Medium", 14), new("Low", 8)
        });
        var prepared = PreparedFamily(chart);
        var tiles = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "treemap-tile-mark").ToArray();
        Verify.Equal(4, tiles.Length);
        var areas = tiles.Select(tile => tile.Bounds.Width * tile.Bounds.Height).ToArray();
        // Visible tiles include a small fixed gutter; it removes proportionally more area from small tiles.
        Verify.InRange(areas[0] / areas[3], 50d / 8 * .95, 50d / 8 * 1.05);
        Verify.True(areas.Zip(areas.Skip(1), (first, second) => first > second).All(larger => larger));
        for (var index = 0; index < tiles.Length; index++)
            for (var other = index + 1; other < tiles.Length; other++) Verify.False(MapOverlap(tiles[index].Bounds, tiles[other].Bounds));
        Verify.Equal(new[] { 50d, 28, 14, 8 }, FamilyGroups(prepared, "treemap-tile").Select(tile => FamilyNumber(tile, "data-cfx-value")));
        Verify.Contains(prepared.Regions, region => region.Role == "treemap-tile" && region.Label == "Critical: 50");
        Verify.NotEmpty(FamilyLabels(prepared, "treemap-label")); Verify.NotEmpty(prepared.ToPng());
    }
}
