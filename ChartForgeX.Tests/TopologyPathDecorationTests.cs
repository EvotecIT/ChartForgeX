using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyPathDecorationTests {
    [Fact]
    public void OrdinaryMaintenanceBoundaryUsesNativeInkWithoutAddingGraphFacts() {
        var chart = TopologyPathDecorationRecipe.Create();
        var prepared = chart.Prepare();
        var svg = prepared.ToSvg();
        var document = XDocument.Parse(svg);
        var decoration = Assert.Single(document.Descendants(), element => (string?)element.Attribute("data-path-decoration-id") == "maintenance-zone");
        var path = Assert.Single(decoration.Descendants(), element => element.Name.LocalName == "path");
        Assert.Contains("C", path.Attribute("d")!.Value);
        Assert.Equal("8 4", path.Attribute("stroke-dasharray")!.Value);
        Assert.Equal(2, prepared.NodeCount); Assert.Equal(1, prepared.EdgeCount);
        Assert.NotEmpty(prepared.ToPng());
        Assert.Equal("maintenance-zone", Assert.Single(prepared.ToInterchangeEnvelope().Topology!.PathDecorations).Id);
        Assert.Single(chart.ToVisualArtifact().ToInterchangeEnvelope().Topology!.PathDecorations);
        Assert.True(chart.ToVisualArtifact().ExportFormats.HasFlag(VisualArtifactExportFormat.Json));
    }

    [Fact]
    public void EmptyAndClearedDecorationsPreserveOrdinarySvgPngAndPortableBytes() {
        var ordinary = TopologyPathDecorationRecipe.Create("plain");
        var before = ordinary.Prepare();
        var svg = before.ToSvg(); var png = before.ToPng(); var json = before.ToInterchangeEnvelope().ToJson();
        var decorated = TopologyPathDecorationRecipe.Create();
        var frozen = decorated.Prepare(); var frozenSvg = frozen.ToSvg(); var frozenPng = frozen.ToPng();
        decorated.PathDecorations.Clear();
        var after = decorated.Prepare();
        Assert.Equal(svg, after.ToSvg()); Assert.Equal(png, after.ToPng()); Assert.Equal(json, after.ToInterchangeEnvelope().ToJson());
        Assert.Equal(frozenSvg, frozen.ToSvg()); Assert.Equal(frozenPng, frozen.ToPng());
        Assert.Single(frozen.ToInterchangeEnvelope().Topology!.PathDecorations);
    }

    [Fact]
    public void PathAndPaintCaptureOwnsDashInputAndRejectsUnsupportedCommandsAndIds() {
        var dash = new[] { 6d, 3d };
        var source = new TopologyPathDecoration("curve", "M 0 0 C 0 100 100 100 100 0", stroke: ChartColor.FromHex("#123456"), dashPattern: dash);
        dash[0] = 90;
        Assert.Equal(6, source.DashPattern![0]);
        Assert.Throws<ArgumentException>(() => new TopologyPathDecoration("quadratic", "M 0 0 Q 10 10 20 0"));
        Assert.Throws<ArgumentException>(() => new TopologyPathDecoration("relative", "M 0 0 l 20 20"));
        Assert.Throws<ArgumentException>(() => new TopologyPathDecoration("missing-move", "L 0 0 L 20 20"));
        var chart = TopologyPathDecorationRecipe.Create("plain");
        chart.PathDecorations.Add(new TopologyPathDecoration("gateway", "M 0 0 L 20 20"));
        Assert.Contains("gateway", Assert.Throws<ArgumentException>(() => chart.Prepare()).Message);
    }

    [Fact]
    public void RealCubicExtremaExpandNaturalCanvasAndFixedFittingKeepsTheCurve() {
        var chart = TopologyPathDecorationRecipe.Create("plain");
        chart.PathDecorations.Add(new TopologyPathDecoration("extent", "M 0 0 C 0 1000 100 1000 100 0", stroke: ChartColor.FromHex("#FF0000"), strokeWidth: 2));
        var natural = chart.Prepare();
        Assert.InRange(natural.Height, 750, 950);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(380, 240), 20));
        var fitted = chart.Prepare(context, new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false });
        Assert.Contains("topology-path-decoration-surface", fitted.ToSvg());
        Assert.NotEmpty(fitted.ToPng());
        Assert.Single(fitted.ToArtifact().ToInterchangeEnvelope().Topology!.PathDecorations);
    }
}
