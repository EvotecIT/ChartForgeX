using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyEdgePresentationTests {
    [Theory]
    [InlineData(TopologyHealthStatus.Healthy, TopologyEdgeLineStyle.Auto, "4 4")]
    [InlineData(TopologyHealthStatus.Warning, TopologyEdgeLineStyle.Auto, "4 4")]
    [InlineData(TopologyHealthStatus.Disabled, TopologyEdgeLineStyle.Auto, "3 6")]
    [InlineData(TopologyHealthStatus.Warning, TopologyEdgeLineStyle.Solid, "none")]
    [InlineData(TopologyHealthStatus.Healthy, TopologyEdgeLineStyle.Dotted, "2 5")]
    public void AssumedLinksUseTheirOwnDashBelowExplicitStylesAndDisabledStatus(TopologyHealthStatus status, TopologyEdgeLineStyle style, string expected) {
        var edge = new TopologyEdge { Status = status, LineStyle = style, Evidence = TopologyEdgeEvidence.Assumed };
        Assert.Equal(expected, TopologyRenderPrimitives.EffectiveEdgeDash(edge));
        Assert.Equal(expected == "none" ? null : expected, TopologyRenderPrimitives.EffectiveEdgePngDashArray(edge) is { } dash
            ? string.Join(" ", dash.Select(value => value.ToString(CultureInfo.InvariantCulture))) : null);
    }

    [Fact]
    public void ConfirmedAndMutedAssumedLinksKeepTheirEvidenceInThePaintedDash() {
        Assert.Equal("none", TopologyRenderPrimitives.EffectiveEdgeDash(new TopologyEdge { Evidence = TopologyEdgeEvidence.Confirmed }));
        Assert.Equal("4 4", TopologyRenderPrimitives.EffectiveEdgeDash(new TopologyEdge { Evidence = TopologyEdgeEvidence.Assumed, IsMuted = true }));
        var custom = new TopologyEdge { Evidence = TopologyEdgeEvidence.Assumed };
        custom.DashPattern.AddRange(new[] { 9d, 2d });
        Assert.Equal("9 2", TopologyRenderPrimitives.EffectiveEdgeDash(custom));
    }

    [Fact]
    public void AssumedLinkPaintsDashedWithEvidenceInTooltipMetadataAndLegend() {
        var chart = Sites();
        chart.WithEdgeEvidence("link", TopologyEdgeEvidence.Assumed, "Inferred from KCC connection objects");
        chart.Edges.Single(edge => edge.Id == "link").Tooltip = "London to Paris";
        chart.WithLegend(TopologyLegend.Create("Legend").AddEdgeKind("Replication", TopologyEdgeKind.Replication));
        var document = XDocument.Parse(chart.Prepare(Options()).ToSvg());
        var edge = Edge(document, "link");
        Assert.Equal("assumed", (string?)edge.Attribute("data-edge-evidence"));
        Assert.Equal("Inferred from KCC connection objects", (string?)edge.Attribute("data-edge-evidence-text"));
        Assert.All(edge.Descendants().Where(element => Role(element) == "topology-edge-line"),
            line => Assert.Equal("4 4", (string?)line.Attribute("stroke-dasharray")));
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "title"
            && element.Value == "London to Paris" + Environment.NewLine + "Assumed link: Inferred from KCC connection objects");
        var legend = TopologyLegend.Resolve(chart, TopologyLegendMode.Explicit)!;
        var assumed = Assert.Single(legend.Items, item => item.Evidence == TopologyEdgeEvidence.Assumed);
        Assert.Equal("Assumed link", assumed.Label);
        Assert.Single(chart.Legend!.Items);
        Assert.Contains(document.Descendants(), element => Role(element) == "topology-legend-edge" && (string?)element.Attribute("stroke-dasharray") == "4 4");
    }

    [Fact]
    public void StripesTileTheRouteInEveryColourWithLegendEntries() {
        var chart = Sites();
        chart.AddEdgeStripe("link", "#2563EB", "IP").AddEdgeStripe("link", "#F97316", "SMTP").AddEdgeStripe("link", "#16A34A");
        chart.WithLegend(TopologyLegend.Create("Legend"));
        var prepared = chart.Prepare(Options());
        var document = XDocument.Parse(prepared.ToSvg());
        var edge = Edge(document, "link");
        Assert.Equal("#2563EB;#F97316;#16A34A", (string?)edge.Attribute("data-edge-stripes"));
        var stripes = edge.Descendants().Where(element => Role(element) == "topology-edge-stripe").ToArray();
        Assert.Equal(new[] { "#2563EB", "#F97316", "#16A34A" }, stripes.Select(stripe => ((string?)stripe.Attribute("stroke"))!.ToUpperInvariant()));
        Assert.DoesNotContain(edge.Descendants(), element => Role(element) == "topology-edge-line");
        var period = 3 * TopologyRenderPrimitives.EdgeStripeLength;
        Assert.All(stripes, stripe => Assert.Equal("12 24", (string?)stripe.Attribute("stroke-dasharray")));
        Assert.Equal(new[] { 0d, period - 12, period - 24 }, stripes.Select(stripe => double.Parse((string?)stripe.Attribute("stroke-dashoffset") ?? "0", CultureInfo.InvariantCulture)));
        var legend = TopologyLegend.Resolve(chart, TopologyLegendMode.Explicit)!;
        Assert.Equal(new[] { "IP", "SMTP" }, legend.Items.Select(item => item.Label));

        var image = prepared.Visual.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.True(Count(image, 0x25, 0x63, 0xEB) > 10, "The PNG paints the first stripe colour.");
        Assert.True(Count(image, 0xF9, 0x73, 0x16) > 10, "The PNG paints the second stripe colour.");
        Assert.True(Count(image, 0x16, 0xA3, 0x4A) > 10, "The PNG paints the third stripe colour.");
    }

    [Fact]
    public void AssumedStripesKeepAGapAfterEveryBand() {
        var chart = Sites();
        chart.AddEdgeStripe("link", "#2563EB").AddEdgeStripe("link", "#F97316").WithEdgeEvidence("link", TopologyEdgeEvidence.Assumed);
        var document = XDocument.Parse(chart.Prepare(Options()).ToSvg());
        Assert.All(Edge(document, "link").Descendants().Where(element => Role(element) == "topology-edge-stripe"),
            stripe => Assert.Equal("8 16", (string?)stripe.Attribute("stroke-dasharray")));
    }

    [Fact]
    public void ChipsSitOnTheRouteClearOfNodesLabelsAndEachOther() {
        var chart = Sites();
        chart.WithEdgeEndpointLabels("link", "London uplink", "Paris edge");
        chart.AddEdgeChip("link", "cost 100").AddEdgeChip("link", "lag 4m", TopologyTone.Warn).AddEdgeChip("link", "15.4W", TopologyTone.Good);
        var options = Options(); options.IncludeEdgeLabelBackplates = true;
        var prepared = chart.Prepare(options);
        var document = XDocument.Parse(prepared.ToSvg());
        var chips = document.Descendants().Where(element => Role(element) == "topology-edge-chip-surface").Select(Rect).ToArray();
        Assert.Equal(3, chips.Length);
        Assert.Equal(new[] { "cost 100", "lag 4m", "15.4W" }, prepared.Visual.Regions.Where(region => region.Role == "topology-edge-chip").Select(region => region.Label));
        var obstacles = document.Descendants().Where(element => Role(element) is "topology-endpoint-label-surface" or "topology-node-surface")
            .Select(Rect).ToArray();
        Assert.Equal(2, document.Descendants().Count(element => Role(element) == "topology-endpoint-label-surface"));
        foreach (var chip in chips) {
            Assert.False(obstacles.Any(obstacle => Overlaps(chip, obstacle)), $"{chip.X},{chip.Y},{chip.Width},{chip.Height} overlaps " + string.Join("; ", obstacles.Where(obstacle => Overlaps(chip, obstacle)).Select(o => $"{o.X},{o.Y},{o.Width},{o.Height}")));
            Assert.DoesNotContain(chips, other => !other.Equals(chip) && Overlaps(chip, other));
        }
        Assert.All(chips, chip => Assert.True(chip.X >= 0 && chip.Y >= 0 && chip.Right <= prepared.Width && chip.Bottom <= prepared.Height));
        var warn = document.Descendants().Single(element => Role(element) == "topology-edge-chip" && (string?)element.Attribute("data-chip-text") == "lag 4m");
        Assert.Equal("tone-warn", (string?)warn.Attribute("data-chip-tone"));
    }

    [Fact]
    public void ChipsWithRoomAreCentredOnTheirRoute() {
        var chart = Sites().AddEdgeChip("link", "cost 100");
        var document = XDocument.Parse(chart.Prepare(Options()).ToSvg());
        var chip = Rect(document.Descendants().Single(element => Role(element) == "topology-edge-chip-surface"));
        var route = Points((string?)Edge(document, "link").Descendants().First(element => Role(element) == "topology-edge-line").Attribute("d"));
        Assert.True(DistanceToRoute(new ChartPoint(chip.X + chip.Width / 2, chip.Y + chip.Height / 2), route) < .5, "The chip is centred on its route.");
    }

    [Fact]
    public void ChipsWithoutAFreePlaceAreOmittedWithADiagnostic() {
        var chart = TopologyChart.Create().WithId("tight").WithViewport(330, 100, 0).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("a", "A", 10, 10).AddNode("b", "B", 150, 10)
            .AddEdge("a-b", "a", "b").AddEdgeChip("a-b", "a long replication value").AddEdgeChip("a-b", "another long value");
        var prepared = chart.Prepare(Options());
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.DoesNotContain(document.Descendants(), element => Role(element) == "topology-edge-chip");
        Assert.Contains(prepared.Visual.Diagnostics, diagnostic => diagnostic.Code == "topology.edge-chips-omitted");
        Assert.Equal("a long replication value · another long value", (string?)Edge(document, "a-b").Attribute("data-edge-chips"));
    }

    [Fact]
    public void UplinkMarkerPrefixesTheChildEndpointLabelOnce() {
        var chart = Sites();
        chart.WithEdgeEndpointLabels("link", "hub", "par").WithEdgeUplink("link", TopologyEdgeEnd.Target);
        var prepared = chart.Prepare(Options());
        var edge = Edge(XDocument.Parse(prepared.ToSvg()), "link");
        Assert.Equal("hub", (string?)edge.Attribute("data-source-label"));
        Assert.Equal("↑ par", (string?)edge.Attribute("data-target-label"));
        Assert.Equal("target", (string?)edge.Attribute("data-edge-uplink"));
        Assert.Equal("par", chart.Edges.Single(item => item.Id == "link").TargetLabel);

        var bare = Sites().WithEdgeUplink("link", TopologyEdgeEnd.Source);
        Assert.Equal("↑", (string?)Edge(XDocument.Parse(bare.Prepare(Options()).ToSvg()), "link").Attribute("data-source-label"));
    }

    internal static TopologyChart Sites() => TopologyChart.Create().WithId("sites").WithViewport(900, 420, 30)
        .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight).WithLegend(null)
        .AddAutoNode("lon", "London", TopologyNodeKind.Location, TopologyHealthStatus.Healthy)
        .AddAutoNode("par", "Paris", TopologyNodeKind.Location, TopologyHealthStatus.Healthy)
        .AddEdge("link", "lon", "par", kind: TopologyEdgeKind.Replication, status: TopologyHealthStatus.Healthy);

    internal static TopologyRenderOptions Options() => new() { FitContentToViewport = false };

    internal static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");

    private static XElement Edge(XDocument document, string id) =>
        document.Descendants().Single(element => Role(element) == "topology-edge" && (string?)element.Attribute("data-edge-id") == id);

    internal static ChartRect Rect(XElement element) => new(Number(element, "x"), Number(element, "y"), Number(element, "width"), Number(element, "height"));

    private static double Number(XElement element, string name) => double.Parse((string?)element.Attribute(name) ?? "0", CultureInfo.InvariantCulture);

    internal static bool Overlaps(ChartRect a, ChartRect b) => Math.Min(a.Right, b.Right) - Math.Max(a.X, b.X) > .01 && Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Y, b.Y) > .01;

    internal static ChartPoint[] Points(string? path) {
        var numbers = Regex.Matches(path ?? string.Empty, @"-?\d+(?:\.\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        return Enumerable.Range(0, numbers.Length / 2).Select(index => new ChartPoint(numbers[index * 2], numbers[index * 2 + 1])).ToArray();
    }

    private static double DistanceToRoute(ChartPoint point, ChartPoint[] route) => Enumerable.Range(1, route.Length - 1).Min(index => {
        var a = route[index - 1]; var b = route[index];
        var dx = b.X - a.X; var dy = b.Y - a.Y; var length = dx * dx + dy * dy;
        var t = length <= 0 ? 0 : Math.Max(0, Math.Min(1, ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / length));
        var x = a.X + dx * t - point.X; var y = a.Y + dy * t - point.Y;
        return Math.Sqrt(x * x + y * y);
    });

    private static int Count(ChartForgeX.Raster.RgbaImage image, int r, int g, int b) {
        var pixels = image.Pixels; var count = 0;
        for (var i = 0; i < pixels.Length; i += 4)
            if (Math.Abs(pixels[i] - r) < 8 && Math.Abs(pixels[i + 1] - g) < 8 && Math.Abs(pixels[i + 2] - b) < 8) count++;
        return count;
    }
}
