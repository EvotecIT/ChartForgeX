using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyLabelBackgroundPaintTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RelationshipAndEndpointSurfacesKeepTheirOwnPaintAndRestoreTheSameNativePixels(bool dark) {
        var chart = Source(dark); var edge = chart.Edges[0]; edge.LabelBackgroundColor = "#193857";
        var direct = chart.Prepare(); var svg = direct.ToSvg(); var png = direct.ToPng(); var saved = direct.ToInterchangeEnvelope(); var json = saved.ToJson();
        Capture("label-background-" + dark + "-direct", svg, png, json);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); var after = restored.Prepare();
        Capture("label-background-" + dark + "-restored", after.ToSvg(), after.ToPng(), after.ToInterchangeEnvelope().ToJson());
        var surfaces = Surfaces(svg); Assert.Equal(3, surfaces.Length);
        Assert.All(surfaces, item => { Assert.True(ChartColor.TryParse((string?)item.Attribute("fill"), out var actual)); Assert.Equal(ChartColor.FromHex("#193857"), actual); });
        Assert.Equal("#E14726", edge.Color); Assert.Equal("#23196B", edge.LabelColor);
        Assert.Equal("#193857", saved.Edges[0].Topology!.LabelBackgroundColor); Assert.Equal(2, saved.Version);
        Assert.Equal("#193857", restored.Edges[0].LabelBackgroundColor); Assert.Equal(png, after.ToPng());
        edge.LabelBackgroundColor = "#C61527"; saved.Edges[0].Topology!.LabelBackgroundColor = "#C61527";
        Assert.Equal(svg, direct.ToSvg()); Assert.Equal(png, direct.ToPng());
        var clone = direct.Visual.ToArtifact().Clone(); var reader = clone.ToInterchangeEnvelope(); reader.Edges[0].Topology!.LabelBackgroundColor = "#C61527";
        Assert.Equal("#193857", clone.ToInterchangeEnvelope().Edges[0].Topology!.LabelBackgroundColor);
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1", StringComparison.Ordinal)));
    }

    [Fact]
    public void NullAndClearedBackgroundKeepOrdinaryNativeSvgPngAndVersionOneBytes() {
        var chart = Source(false); chart.Theme=VisualTheme.Graphite(); var original = chart.Prepare(); var svg = original.ToSvg(); var png = original.ToPng(); var json = original.ToInterchangeEnvelope().ToJson();
        Capture("label-background-null-default", svg, png, json);
        chart.Edges[0].LabelBackgroundColor = "#193857"; chart.Edges[0].LabelBackgroundColor = null; var after = chart.Prepare();
        Assert.Equal(svg, after.ToSvg()); Assert.Equal(png, after.ToPng()); Assert.Equal(json, after.ToInterchangeEnvelope().ToJson());
        Assert.Equal(1, after.ToInterchangeEnvelope().Version); Assert.DoesNotContain("labelBackgroundColor", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingHostBackplateVisibilityStillOwnsBothSurfaces() {
        var chart = Source(false); chart.Edges[0].LabelBackgroundColor = "#193857";
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false });
        var svg = prepared.ToSvg(); Capture("label-background-disabled-host", svg, prepared.ToPng(), prepared.ToInterchangeEnvelope().ToJson());
        Assert.Empty(Surfaces(svg)); Assert.Contains("Transfer", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void MalformedTypedPaintCannotBypassExistingPortableStringValidation() {
        var chart = Source(false); chart.Edges[0].LabelBackgroundColor = "#193857"; var json = chart.Prepare().ToInterchangeEnvelope().ToJson();
        var malformed = json.Replace("\"labelBackgroundColor\":\"#193857\"", "\"labelBackgroundColor\":42", StringComparison.Ordinal);
        Write("label-background-malformed.json", malformed); Assert.NotEqual(json, malformed);
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(malformed));
    }

    private static TopologyChart Source(bool dark) {
        var chart = TopologyChart.Create().WithId("native-label-background").WithTitle("").WithViewport(760, 400, 24).WithLegend(null)
            .WithLayout(TopologyLayoutMode.Manual).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.AddNode("a", "Plan", 60, 100, width: 140, height: 80); chart.AddNode("b", "Deliver", 500, 100, width: 140, height: 80);
        foreach (var node in chart.Nodes) { node.Kind = TopologyNodeKind.Generic; node.Shape = TopologyNodeShape.Rectangle; node.ShowStatusBadge = false; }
        chart.AddEdge("handoff", "a", "b", "Transfer", direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.Straight);
        var edge = chart.Edges[0]; edge.SourceLabel = "Ready"; edge.TargetLabel = "Saved"; edge.Color = "#E14726"; edge.LabelColor = "#23196B";
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false }); return chart;
    }
    private static XElement[] Surfaces(string svg) => XDocument.Parse(svg).Descendants().Where(item =>
        (string?)item.Attribute("data-cfx-role") is "topology-edge-label-surface" or "topology-endpoint-label-surface").ToArray();
    private static void Capture(string name, string svg, byte[] png, string json) {
        Write(name + ".svg", svg); Write(name + ".json", json); var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png);
    }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
