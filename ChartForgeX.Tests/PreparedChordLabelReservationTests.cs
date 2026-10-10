using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;
using static ChartForgeX.Tests.PreparedChordTests;

namespace ChartForgeX.Tests;

public sealed class PreparedChordLabelReservationTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AddingZeroOnlyCaptionsPreservesVisibleGeometryAndNativePixels(bool oversizedStyle, bool zeroLink) {
        var baseline = ZeroCaptionChart(false, false, zeroLink).Prepare(Context(460, 460));
        var chart = ZeroCaptionChart(true, oversizedStyle, zeroLink);
        var extended = chart.Prepare(Context(460, 460));
        Capture(baseline, "zero-base"); Capture(extended, "zero-" + oversizedStyle + "-" + zeroLink);

        AssertSameVisibleScene(baseline, extended);
        var semantic = Node(extended, "semantic");
        Assert.Equal("zero", (string?)semantic.Attribute("data-cfx-geometry-status"));
        Assert.Equal(0, Number(semantic, "value"));
        Assert.Equal(chart.Series[0].Nodes[2].Label, (string?)semantic.Attribute("data-cfx-label"));
        Assert.Equal(new[] { "north", "south", "semantic" }, Role(extended, "chord-node").Select(node => (string?)node.Attribute("data-cfx-target-id")));
        Assert.Equal(0, extended.Regions.Single(region => region.Id == "series-0-node-semantic").Bounds.Width);
        if (zeroLink) {
            var link = Role(extended, "chord-link").Single(node => (string?)node.Attribute("data-cfx-target-id") == "zero");
            Assert.Equal(0, Number(link, "value"));
            Assert.Equal("north", (string?)link.Attribute("data-cfx-source"));
            Assert.Equal("semantic", (string?)link.Attribute("data-cfx-target"));
        }
    }

    [Theory]
    [InlineData(double.Epsilon, false)]
    [InlineData(double.Epsilon, true)]
    [InlineData(1e-20, false)]
    [InlineData(1e-20, true)]
    public void PrecisionCollapsedCaptionsDoNotReserveSpaceForSemanticOnlyPositiveFacts(double tiny, bool oversizedStyle) {
        var baseline = PrecisionCaptionChart(tiny, false, false).Prepare(Context(460, 460));
        var changed = PrecisionCaptionChart(tiny, true, oversizedStyle).Prepare(Context(460, 460));
        Capture(baseline, "precision-base-" + tiny.ToString("R", CultureInfo.InvariantCulture));
        Capture(changed, "precision-" + tiny.ToString("R", CultureInfo.InvariantCulture) + "-" + oversizedStyle);

        AssertSameVisibleScene(baseline, changed);
        var semantic = Node(changed, "semantic");
        Assert.Equal("precision-collapse", (string?)semantic.Attribute("data-cfx-geometry-status"));
        Assert.Equal(tiny * 2, Number(semantic, "value"));
        var link = Role(changed, "chord-link").Single(node => (string?)node.Attribute("data-cfx-target-id") == "tiny");
        Assert.Equal(tiny, Number(link, "value"));
        Assert.Equal("precision-collapse", (string?)link.Attribute("data-cfx-geometry-status"));
        Assert.Contains(changed.Diagnostics, diagnostic => diagnostic.Code == "chord.precision-collapse");
    }

    [Fact]
    public void AllZeroCaptionsKeepTheUnreservedRadiusAndZeroFacts() {
        var chart = Chart.Create().AddChord("Transfers", new[] { new ChartNode("north", "N"), new ChartNode("south", "S") },
            new[] { new ChartFlowLink("zero", "north", "south", 0) });
        var unlabelled = chart.ConfigureChord(options => options.LabelContent = ChartChordLabelContent.None).Prepare(Context());
        chart.Options.Chord.LabelContent = ChartChordLabelContent.LabelAndTotals;
        chart.Series[0].WithPointLabel(1, new string('W', 180)).ConfigurePointDataLabelStyle(1, style => style.WithFontSize(96));
        var labelled = chart.Prepare(Context());
        Capture(labelled, "all-zero");

        Assert.Equal(Radius(unlabelled), Radius(labelled));
        Assert.DoesNotContain(labelled.Scene.Nodes, node => node.Role == "chord-node-label" || node.Role == "chord-node-mark" || node.Role == "chord-ribbon");
        Assert.Equal(2, Role(labelled, "chord-node").Length); Assert.Single(Role(labelled, "chord-link"));
        Assert.All(labelled.Regions, region => { Assert.Equal(0, region.Bounds.Width); Assert.Equal(0, region.Bounds.Height); });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisabledCaptionsDoNotReserveSpaceForVisibleOrSemanticNodes(bool disableAtSeries) {
        var chart = ZeroCaptionChart(true, true);
        if (disableAtSeries) chart.Series[0].ShowDataLabels = false;
        else chart.Options.Chord.LabelContent = ChartChordLabelContent.None;
        var baseline = chart.Prepare(Context());
        chart.Series[0].WithPointLabel(0, new string('W', 180)).ConfigurePointDataLabelStyle(0, style => style.WithFontSize(96));
        var changed = chart.Prepare(Context());
        AssertSameVisibleScene(baseline, changed);
        Assert.DoesNotContain(changed.Scene.Nodes, node => node.Role == "chord-node-label");
    }

    internal static Chart ZeroCaptionChart(bool semanticNode, bool oversizedStyle, bool zeroLink = true) {
        var nodes = new List<ChartNode> { new("north", "North"), new("south", "South") };
        var links = new List<ChartFlowLink> { new("flow", "north", "south", 6) };
        if (semanticNode) {
            nodes.Add(new ChartNode("semantic", oversizedStyle ? "Hidden" : new string('W', 180)));
            if (zeroLink) links.Add(new ChartFlowLink("zero", "north", "semantic", 0));
        }
        var chart = Chart.Create().AddChord("Transfers", nodes, links);
        if (semanticNode && oversizedStyle) chart.Series[0].ConfigurePointDataLabelStyle(2, style => style.WithFontSize(96));
        return chart;
    }

    private static Chart PrecisionCaptionChart(double tiny, bool changedCaption, bool oversizedStyle) {
        var label = changedCaption && !oversizedStyle ? new string('W', 180) : "Hidden";
        var chart = Chart.Create().AddChord("Transfers", new[] { new ChartNode("semantic", label), new ChartNode("north", "North"), new ChartNode("south", "South") },
            new[] { new ChartFlowLink("tiny", "semantic", "semantic", tiny), new ChartFlowLink("flow", "north", "south", 1) })
            .ConfigureChord(options => options.StartAngleDegrees = 0);
        if (changedCaption && oversizedStyle) chart.Series[0].ConfigurePointDataLabelStyle(0, style => style.WithFontSize(96));
        return chart;
    }

    private static void AssertSameVisibleScene(PreparedVisual expected, PreparedVisual actual) {
        Assert.Equal(Radius(expected), Radius(actual));
        var roles = new[] { "chord-node-mark", "chord-ribbon", "chord-target-cue", "chord-node-label", "chord-label-leader" };
        foreach (var role in roles) Assert.Equal(Role(expected, role).Select(VisibleContent), Role(actual, role).Select(VisibleContent));
        Assert.Equal(expected.ToPng(new VisualRenderOptions(supersampling: 1)), actual.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    private static string VisibleContent(XElement node) {
        // Primitive IDs are local to each scene; authored node and flow identities are checked separately.
        node.DescendantsAndSelf().Attributes().Where(attribute => attribute.Name == "id" || attribute.Name == "data-cfx-source-id").Remove();
        return node.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement Node(PreparedVisual visual, string id) => Role(visual, "chord-node").Single(node => (string?)node.Attribute("data-cfx-target-id") == id);
    private static XElement[] Role(PreparedVisual visual, string role) => XDocument.Parse(visual.ToSvg("caption-proof")).Descendants()
        .Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
    private static double Radius(PreparedVisual visual) => Number(Node(visual, "north"), "outer-radius");
    private static double Number(XElement node, string name) => double.Parse(node.Attribute("data-cfx-" + name)!.Value, CultureInfo.InvariantCulture);

    private static void Capture(PreparedVisual visual, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), visual.ToSvg("caption-proof"));
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), visual.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
