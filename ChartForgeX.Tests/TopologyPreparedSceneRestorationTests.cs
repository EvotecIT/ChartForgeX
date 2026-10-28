using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyPreparedSceneRestorationTests {
    [Fact]
    public void RepeatedTypedImportKeepsTheSceneOriginWithoutConsultingOpaqueExtensions() {
        var saved = TopologyPreparedSceneRestorationCausalTests.Source(true).Prepare().ToInterchangeEnvelope();
        var expected = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson());
        for (var pass = 0; pass < 3; pass++) {
            saved.Extensions.Remove("chartforgex.geometry");
            var imported = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
            var after = imported.ToVisualArtifact().ToInterchangeEnvelope();
            Capture(imported.Prepare(), "prepared-scene-repeat-" + pass);
            AssertGeometry(expected, after); Assert.Equal(expected.Width, after.Width); Assert.Equal(expected.Height, after.Height);
            Assert.Equal(expected.Groups[0].Y, (double?)imported.Groups[0].Y); saved = after;
        }
    }

    [Fact]
    public void EditingTheSceneCanExtendItsCanvasWithoutTranslatingItsRetainedOrigin() {
        var saved = TopologyPreparedSceneRestorationCausalTests.Source(false).Prepare().ToInterchangeEnvelope();
        var imported = saved.ToTopologyChart(); const double movement = 900;
        foreach (var group in imported.Groups) group.X += movement;
        foreach (var node in imported.Nodes) node.X += movement;
        foreach (var edge in imported.Edges)
            for (var point = 0; point < edge.Waypoints.Count; point++) edge.Waypoints[point] = new ChartPoint(edge.Waypoints[point].X + movement, edge.Waypoints[point].Y);
        var prepared = imported.Prepare(); var after = prepared.ToInterchangeEnvelope(); Capture(prepared, "prepared-scene-edited-canvas");
        Assert.Equal(saved.Groups[0].X!.Value + movement, after.Groups[0].X!.Value); Assert.Equal(saved.Groups[0].Y, after.Groups[0].Y);
        Assert.True(after.Width!.Value >= after.Groups[0].X!.Value + after.Groups[0].Width!.Value); Assert.True(after.Width > saved.Width);
        Assert.Equal(saved.Height, after.Height);
        Assert.Equal(saved.Edges[0].Topology!.Waypoints.Select(point => (point.X + movement, point.Y)), after.Edges[0].Topology!.Waypoints.Select(point => (point.X, point.Y)));
        Assert.Equal(saved.Groups[0].X!.Value + movement, imported.Groups[0].X);
    }

    [Fact]
    public void FixedCanvasRejectsOverflowAndExplicitFittingTransformsGeometryUniformly() {
        var saved = TopologyPreparedSceneRestorationCausalTests.Source(true).Prepare().ToInterchangeEnvelope(); var imported = saved.ToTopologyChart();
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(420, 310), 24));
        Assert.Throws<NotSupportedException>(() => imported.Prepare(context));
        var prepared = imported.Prepare(context, new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false });
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "prepared-scene-explicit-fit.svg"), prepared.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "prepared-scene-explicit-fit.png"), prepared.ToPng());
            File.WriteAllText(Path.Combine(output, "prepared-scene-explicit-fit.json"), prepared.SemanticInterchange!.ToJson());
        }
        var fitted = prepared.SemanticInterchange!; var group = fitted.Groups[0]; var original = saved.Groups[0];
        var scale = group.Width!.Value / original.Width!.Value;
        Assert.InRange(scale, .001, .999); Assert.Equal(original.Height!.Value * scale, group.Height!.Value, 6);
        for (var index = 0; index < saved.Edges[0].Topology!.Waypoints.Count; index++) {
            var before = saved.Edges[0].Topology!.Waypoints[index]; var after = fitted.Edges[0].Topology!.Waypoints[index];
            Assert.Equal((before.X - original.X!.Value) * scale, after.X - group.X!.Value, 6);
            Assert.Equal((before.Y - original.Y!.Value) * scale, after.Y - group.Y!.Value, 6);
        }
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "topology.content-fitted");
        Assert.Equal(saved.Groups[0].Y, (double?)imported.Groups[0].Y); Assert.Equal(420, prepared.Size.Width); Assert.Equal(310, prepared.Size.Height);
    }

    [Fact]
    public void SelectingAnAutomaticLayoutResolvesAContentDomainInsteadOfRetainingTheSceneOrigin() {
        var saved = TopologyPreparedSceneRestorationCausalTests.Source(false).Prepare().ToInterchangeEnvelope(); var imported = saved.ToTopologyChart();
        imported.LayoutMode = TopologyLayoutMode.Layered; foreach (var node in imported.Nodes) node.HasPositionOverride = false;
        foreach (var group in imported.Groups) group.HasPositionOverride = false;
        var prepared = imported.Prepare(); var after = prepared.ToInterchangeEnvelope(); Capture(prepared, "prepared-scene-automatic-layout");
        Assert.Equal(TopologyLayoutMode.Layered, after.Topology!.LayoutMode); Assert.Equal(saved.Nodes.Select(node => node.Id), after.Nodes.Select(node => node.Id));
        Assert.NotEqual(saved.Groups[0].Y, after.Groups[0].Y); Assert.Contains("Saved title", prepared.ToSvg());
        Assert.Equal(saved.Groups[0].Y, (double?)imported.Groups[0].Y);
    }

    [Fact]
    public void RawHierarchyImportRemainsAuthoredCoordinatesWhenTheCallerChoosesManualLayout() {
        var source = TopologyChart.Create().WithTitle("Raw hierarchy title").WithViewport(900, 650, 24).WithLegend(null)
            .AddHierarchy(new[] { new TopologyHierarchyItem("root", "Root"), new TopologyHierarchyItem("child", "Child", "root") });
        var envelope = source.Prepare(new TopologyRenderOptions { HierarchyView = new TopologyHierarchyView(), IncludeLegend = false }).ToInterchangeEnvelope();
        var raw = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyHierarchySource(); raw.LayoutMode = TopologyLayoutMode.Manual;
        for (var index = 0; index < raw.Nodes.Count; index++) {
            var node = raw.Nodes[index]; node.X = 100 + 300 * index; node.Y = 150; node.Width = 144; node.Height = 64;
            node.DisplayMode = TopologyNodeDisplayMode.Card; node.PreserveDisplayModeSize = true;
        }
        var prepared = raw.Prepare(new TopologyRenderOptions { IncludeLegend = false }); var after = prepared.ToInterchangeEnvelope(); Capture(prepared, "prepared-scene-raw-hierarchy-control");
        Assert.Equal(150, raw.Nodes[0].Y); Assert.True(after.Nodes[0].Y > raw.Nodes[0].Y); Assert.Contains("Raw hierarchy title", prepared.ToSvg());
        Assert.Equal(2, after.Nodes.Count); Assert.Null(after.Topology!.Hierarchy);
    }

    internal static void AssertGeometry(VisualArtifactInterchangeEnvelope expected, VisualArtifactInterchangeEnvelope actual) {
        Assert.Equal(expected.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)), actual.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)));
        Assert.Equal(expected.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)), actual.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Assert.Equal(expected.Edges[0].Topology!.Waypoints.Select(point => (point.X, point.Y)), actual.Edges[0].Topology!.Waypoints.Select(point => (point.X, point.Y)));
    }

    private static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name);
        File.WriteAllText(path + ".svg", prepared.ToSvg()); File.WriteAllBytes(path + ".png", prepared.ToPng()); File.WriteAllText(path + ".json", prepared.ToInterchangeEnvelope().ToJson());
    }
}
