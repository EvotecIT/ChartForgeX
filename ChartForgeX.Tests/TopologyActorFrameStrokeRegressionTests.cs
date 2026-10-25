using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyActorFrameStrokeRegressionTests {
    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    [InlineData(32)]
    public void CapturedPaddingKeepsTheActualFrameAndFittedActorGlyphPixels(double padding) {
        var family = new MermaidFlowchartRenderOptions { Width = 640, Height = 360, Padding = padding, FitContent = false,
            IconCatalog = new TopologyIconCatalog().AddPack(new TopologyIconPack("tools", "Finite test icons")
                .AddIcon(new TopologyIconDefinition("tools", "gateway", "Router", TopologyNodeKind.Generic, TopologyIconShape.Router) { Symbol = "R", Color = "#2563EB" })),
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, TopologySizing = MermaidTopologySizing.FixedViewport }
        };
        var chart = new MermaidParser().ParseFlowchart("usecase-beta\nactor Gateway @{icon:'tools:gateway'} <<external system>>").Document!.ToTopologyChart(family);
        var direct = chart.Prepare(); var svg = direct.ToSvg(); var png = direct.ToPng(); var envelope = direct.ToInterchangeEnvelope(); var json = envelope.ToJson();
        Capture("actor-frame-padding-" + padding + "-direct", svg, png, json);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); var after = restored.Prepare();
        var afterSvg = after.ToSvg(); var afterPng = after.ToPng(); Capture("actor-frame-padding-" + padding + "-restored", afterSvg, afterPng, after.ToInterchangeEnvelope().ToJson());
        Assert.Equal(padding, restored.Viewport.Padding);
        if (padding == new TopologyViewport().Padding) Assert.Null(envelope.Topology!.ViewportPadding);
        else AssertPadding(ChartPadding.All(padding), envelope.Topology!.ViewportPadding!.Value);
        Assert.Equal(FrameTitle(svg), FrameTitle(afterSvg));
        Assert.NotEmpty(FrameTitle(svg)); Assert.Equal(ActorPaint(svg), ActorPaint(afterSvg));
        Assert.Equal(png, afterPng); Assert.Equal(svg, direct.ToSvg()); Assert.Equal(png, direct.ToPng());
    }

    [Fact]
    public void DefaultPaddingAndUnitNonActorControlKeepsVersionOneAndAnOmittedPaddingField() {
        var chart = TopologyChart.Create().WithTitle("Native default frame").WithLegend(null).WithViewport(320, 240, new TopologyViewport().Padding);
        chart.LayoutMode = TopologyLayoutMode.Manual; chart.WithTheme(VisualTheme.Graphite());
        chart.Nodes.Add(new TopologyNode { Id = "plain", Label = "Native caption", X = 40, Y = 40, Width = 100, Height = 50,
            Shape = TopologyNodeShape.Rectangle, Kind = TopologyNodeKind.Generic, ShowStatusBadge = false, PreserveDisplayModeSize = true });
        var direct = chart.Prepare(); var svg = direct.ToSvg(); var png = direct.ToPng(); var envelope = direct.ToInterchangeEnvelope(); var json = envelope.ToJson();
        Capture("actor-frame-default-v1-direct", svg, png, json);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); var after = restored.Prepare();
        Capture("actor-frame-default-v1-restored", after.ToSvg(), after.ToPng(), after.ToInterchangeEnvelope().ToJson());
        Assert.Null(envelope.Topology!.ViewportPadding); Assert.DoesNotContain("viewportPadding", json, StringComparison.Ordinal); Assert.Equal(1, envelope.Version);
        Assert.Equal(new TopologyViewport().Padding, restored.Viewport.Padding); Assert.Equal(FrameTitle(svg, "Native default frame"), FrameTitle(after.ToSvg(), "Native default frame")); Assert.Equal(png, after.ToPng());
    }

    [Fact]
    public void ATableNullFittedActorRecipeStillCarriesItsRequiredVersionTwoActorBasis() {
        var chart = TopologyTableContentRecipe.Create("wide").WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
        chart.Nodes.Single(node => node.Id == "Details").TableContent = null;
        var prepared = chart.Prepare(); var envelope = prepared.ToInterchangeEnvelope(); var json = envelope.ToJson();
        Capture("actor-frame-fitted-table-null-v2", prepared.ToSvg(), prepared.ToPng(), json);
        Assert.All(envelope.Nodes, node => Assert.Null(node.Topology!.TableContent));
        Assert.NotNull(envelope.Nodes.Single(node => node.Id == "User").Topology!.PreparedNodeBasis); Assert.Equal(2, envelope.Version);
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1e400")]
    [InlineData("null")]
    [InlineData("\"32\"")]
    public void InvalidViewportPaddingFailsAtTheExistingPortableValidationBoundary(string invalid) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(320, 240, 32).WithLegend(null);
        chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Node", X = 40, Y = 40, Width = 100, Height = 50, Shape = TopologyNodeShape.Rectangle, ShowStatusBadge = false });
        var json = chart.ToVisualArtifact().ToInterchangeJson();
        var candidate = json.Replace("\"left\":32", "\"left\":" + invalid, StringComparison.Ordinal);
        Write("actor-frame-invalid-padding-" + (invalid == "-1" ? "negative" : invalid == "1e400" ? "nonfinite" : invalid == "null" ? "null" : "string") + ".json", candidate);
        var error = Record.Exception(() => VisualArtifactInterchangeEnvelope.FromJson(candidate));
        Write("actor-frame-invalid-padding.error.json", JsonSerializer.Serialize(new { type = error?.GetType().FullName, error?.Message }));
        Assert.NotEqual(json, candidate); Assert.IsAssignableFrom<ArgumentException>(error);
    }

    [Fact]
    public void PaddingIsDetachedDuringPreparationAndCannotBeDowngradedOrMutatedThroughAReader() {
        var chart = TopologyChart.Create().WithTitle("Captured frame").WithViewport(320, 240, 32).WithLegend(null);
        chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Node", X = 70, Y = 60, Width = 100, Height = 50, Shape = TopologyNodeShape.Rectangle, ShowStatusBadge = false });
        var prepared = chart.Prepare(); var json = prepared.ToInterchangeEnvelope().ToJson(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Capture("actor-frame-snapshot-before-edits", svg, png, json);
        chart.Viewport.Padding = 0; var reader = prepared.ToInterchangeEnvelope(); reader.Topology!.ViewportPadding = ChartPadding.All(0);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var clone = prepared.Visual.ToArtifact().Clone(); var returned = clone.ToInterchangeEnvelope(); returned.Topology!.ViewportPadding = ChartPadding.All(16);
        AssertPadding(ChartPadding.All(32), prepared.ToInterchangeEnvelope().Topology!.ViewportPadding!.Value);
        AssertPadding(ChartPadding.All(32), clone.ToInterchangeEnvelope().Topology!.ViewportPadding!.Value);
        Assert.Equal(32, restored.Viewport.Padding); Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1", StringComparison.Ordinal)));
    }

    [Fact]
    public void AsymmetricContextRetainsAllFourInsetsAndTheSameFramePixels() {
        var (chart, direct) = Asymmetric();
        var svg = direct.ToSvg(); var png = direct.ToPng(); var envelope = direct.ToArtifact().ToInterchangeEnvelope(); var json = envelope.ToJson();
        Capture("actor-frame-asymmetric-direct", svg, png, json);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); var after = restored.Prepare();
        Capture("actor-frame-asymmetric-restored", after.ToSvg(), after.ToPng(), after.ToInterchangeEnvelope().ToJson());
        AssertPadding(new ChartPadding(13, 31, 47, 19), envelope.Topology!.ViewportPadding!.Value);
        Assert.Equal(13, restored.Viewport.Padding); Assert.NotEmpty(FrameTitle(svg, chart.Title!));
        Assert.Equal(FrameTitle(svg, chart.Title!), FrameTitle(after.ToSvg(), chart.Title!)); Assert.Equal(png, after.ToPng());
        Assert.Equal(2, envelope.Version); Assert.Equal(svg, direct.ToSvg()); Assert.Equal(png, direct.ToPng());
    }

    [Theory]
    [InlineData("width")]
    [InlineData("height")]
    [InlineData("padding")]
    [InlineData("automatic")]
    [InlineData("fit")]
    public void ViewportAndLayoutPolicyEditsDiscardTheCapturedFourInsetRequest(string edit) {
        var (_, direct) = Asymmetric(); var json = direct.ToArtifact().ToInterchangeJson();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        if (edit == "width") restored.Viewport.Width += 40;
        if (edit == "height") restored.Viewport.Height += 40;
        if (edit == "padding") restored.Viewport.Padding = 18;
        if (edit == "automatic") restored.LayoutMode = TopologyLayoutMode.Layered;
        var prepared = edit == "fit" ? restored.Prepare(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true }) : restored.Prepare();
        var envelope = prepared.ToInterchangeEnvelope(); Capture("actor-frame-asymmetric-edit-" + edit, prepared.ToSvg(), prepared.ToPng(), envelope.ToJson());
        AssertPadding(ChartPadding.All(restored.Viewport.Padding), envelope.Topology!.ViewportPadding!.Value);
        AssertPadding(new ChartPadding(13, 31, 47, 19), VisualArtifactInterchangeEnvelope.FromJson(json).Topology!.ViewportPadding!.Value);
    }

    [Fact]
    public void ExplicitContextsKeepTheirOwnInsetsAndARealPaddingTwentyEightRecipeRemainsVersionTwo() {
        var (_, direct) = Asymmetric(); var imported = direct.ToArtifact().ToInterchangeEnvelope().ToTopologyChart();
        var explicitInsets = new ChartPadding(7, 9, 11, 15);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(520, 340), explicitInsets), imported.Theme, imported.ThemeMode,
            new VisualFrame(showLegend: false), FontSpec.FromFamily(BrowserProofFonts.Family));
        var changed = imported.Prepare(context, new TopologyRenderOptions { IncludeLegend = false });
        var changedEnvelope = changed.ToArtifact().ToInterchangeEnvelope(); Capture("actor-frame-explicit-context", changed.ToSvg(), changed.ToPng(), changedEnvelope.ToJson());
        var plain = TopologyPathDecorationRecipe.Create("plain").WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
        var original = plain.Prepare(); var envelope = original.ToInterchangeEnvelope(); var json = envelope.ToJson();
        Capture("actor-frame-real-padding-28-direct", original.ToSvg(), original.ToPng(), json);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();
        Capture("actor-frame-real-padding-28-restored", restored.ToSvg(), restored.ToPng(), restored.ToInterchangeEnvelope().ToJson());
        AssertPadding(explicitInsets, changedEnvelope.Topology!.ViewportPadding!.Value);
        AssertPadding(ChartPadding.All(28), envelope.Topology!.ViewportPadding!.Value); Assert.Empty(envelope.Topology.PathDecorations); Assert.Equal(2, envelope.Version);
        Assert.Equal(FrameTitle(original.ToSvg(), plain.Title!), FrameTitle(restored.ToSvg(), plain.Title!)); Assert.Equal(original.ToPng(), restored.ToPng());
    }

    [Fact]
    public void RawHierarchyPaddingRetainsItsExistingUniformOwnerAndRejectsContradictoryFourInsetFacts() {
        var chart = TopologyChart.Create().WithViewport(640, 400, 32).WithLegend(null).AddHierarchy(new[] {
            new TopologyHierarchyItem("root", "Root"), new TopologyHierarchyItem("child", "Child", "root")
        });
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, HierarchyView = new TopologyHierarchyView(new[] { "root" }) });
        var envelope = prepared.ToInterchangeEnvelope(); var json = envelope.ToJson();
        Capture("actor-frame-raw-hierarchy-padding", prepared.ToSvg(), prepared.ToPng(), json);
        var retained = VisualArtifactInterchangeEnvelope.FromJson(json); var hierarchy = retained.Topology!.Hierarchy!;
        Assert.Equal(32, retained.ToTopologyHierarchySource().Viewport.Padding);
        AssertPadding(ChartPadding.All(32), hierarchy.Source.Topology!.ViewportPadding!.Value);
        hierarchy.Source.Topology.ViewportPadding = new ChartPadding(32, 33, 32, 32);
        Assert.Throws<ArgumentException>(retained.Validate);
    }

    private static (TopologyChart Chart, PreparedVisual Prepared) Asymmetric() {
        var chart = TopologyChart.Create().WithTitle("Four captured insets").WithViewport(520, 340, new TopologyViewport().Padding).WithLegend(null)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)); chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Direct context", X = 90, Y = 100, Width = 140, Height = 70,
            Shape = TopologyNodeShape.Rectangle, ShowStatusBadge = false, PreserveDisplayModeSize = true });
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(520, 340), new ChartPadding(13, 31, 47, 19)), chart.Theme, chart.ThemeMode,
            new VisualFrame(showLegend: false), FontSpec.FromFamily(BrowserProofFonts.Family));
        return (chart, chart.Prepare(context, new TopologyRenderOptions { IncludeLegend = false }));
    }
    private static void AssertPadding(ChartPadding expected, ChartPadding actual) =>
        Assert.Equal((expected.Left, expected.Top, expected.Right, expected.Bottom), (actual.Left, actual.Top, actual.Right, actual.Bottom));

    private static string ActorPaint(string svg) {
        var node = XDocument.Parse(svg).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-node"
            && (string?)element.Attribute("data-node-id") == "Gateway");
        return string.Join("\n", node.Descendants().Where(element => (string?)element.Attribute("data-cfx-role")
                is "topology-node-surface" or "topology-node-icon" or "topology-icon-surface")
            .Select(element => element.Name.LocalName + " " + string.Join(" ", element.Attributes().Where(attribute =>
                attribute.Name.LocalName is "x" or "y" or "width" or "height" or "cx" or "cy" or "rx" or "ry" or "d" or "fill" or "stroke" or "stroke-width" or "stroke-linejoin" or "stroke-linecap")
                .Select(attribute => attribute.Name.LocalName + "=" + attribute.Value))));
    }
    private static string FrameTitle(string svg, string title = "Mermaid use case") => string.Join("\n", XDocument.Parse(svg).Descendants()
        .Where(element => element.Name.LocalName == "text" && element.Value == title)
        .Select(element => string.Join(" ", element.Attributes().Where(attribute => attribute.Name.LocalName is "x" or "y" or "font-size" or "font-family" or "font-weight" or "fill")
            .Select(attribute => attribute.Name.LocalName + "=" + attribute.Value))));
    private static void Capture(string name, string svg, byte[] png, string json) { Write(name + ".svg", svg); Write(name + ".json", json); var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png); }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
