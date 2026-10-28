using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyPathDecorationInterchangeTests {
    [Fact]
    public void OrdinaryCurveRetainsEveryDoublePaintClosureIdentityAndOrderAcrossPublicUtf8AndNativeRestore() {
        var chart = TopologyPathDecorationRecipe.Create("plain");
        var ink = new TopologyPathDecoration("precision", "M 0.12345678901234566 0.98765432109876539 C 1.1234567890123457 80.765432109876543 99.123456789012351 90.987654321098759 100.12345678901235 0.98765432109876539 M 10.123456789012346 10.987654321098766 L 20.123456789012344 20.987654321098766",
            fill: ChartColor.FromHex("#12345678"), stroke: ChartColor.FromHex("#654321ab"), strokeWidth: 1.1234567890123457,
            close: true, dashPattern: new[] { 6.1234567890123457, 3.9876543210987654 });
        Assert.Contains("0.12345678901234566", ink.PathData);
        Assert.Equal(ink.PathData, new TopologyPathDecoration("copy", ink.PathData).PathData);
        chart.PathDecorations.Add(ink);
        chart.PathDecorations.Add(new TopologyPathDecoration("upper", "M 5 5 L 20 20", stroke: ChartColor.FromHex("#112233")));
        var prepared = chart.Prepare(); var saved = prepared.ToInterchangeEnvelope(); var bytes = saved.ToUtf8Json();
        Assert.Equal(2, saved.Version); Assert.Contains("\"commands\":[[\"M\"", Encoding.UTF8.GetString(bytes));
        var loaded = VisualArtifactInterchangeEnvelope.FromUtf8Json(bytes); Assert.Equal(bytes, loaded.ToUtf8Json());
        var restored = loaded.ToTopologyChart();
        Equal(saved.Topology!.PathDecorations, restored.PathDecorations);
        Equal(saved.Topology!.PathDecorations, restored.Prepare().ToInterchangeEnvelope().Topology!.PathDecorations);
        Assert.Equal(2, restored.Nodes.Count); Assert.Single(restored.Edges);
        Assert.Contains("precision", restored.ToSvg()); Assert.NotEmpty(restored.ToPng());
        var artifact = chart.ToVisualArtifact(); Assert.True(artifact.ExportFormats.HasFlag(VisualArtifactExportFormat.Json));
        Equal(saved.Topology!.PathDecorations, artifact.ToInterchangeEnvelope().Topology!.PathDecorations);
        chart.PathDecorations.Clear(); Assert.Equal(bytes, prepared.ToInterchangeEnvelope().ToUtf8Json());
    }

    [Fact]
    public void FittedCurveUsesTheSamePointTransformForEndpointsControlsStrokeAndDashes() {
        var chart = TopologyPathDecorationRecipe.Create("plain");
        var source = new TopologyPathDecoration("curve", "M 40 40 C 50 60 120 80 200 100", stroke: ChartColor.FromHex("#112233"), strokeWidth: 2.5, dashPattern: new[] { 8d, 4d });
        chart.PathDecorations.Add(source);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 220), 12)),
            new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false });
        var saved = prepared.ToArtifact().ToInterchangeEnvelope(); var fitted = Assert.Single(saved.Topology!.PathDecorations);
        var scale = saved.Nodes[0].Width!.Value / chart.Nodes[0].Width;
        var dx = saved.Nodes[0].X!.Value - chart.Nodes[0].X * scale;
        var dy = saved.Nodes[0].Y!.Value - chart.Nodes[0].Y * scale;
        for (var command = 0; command < source.Commands.Count; command++)
            for (var coordinate = 0; coordinate < source.Commands[command].Values.Count; coordinate++)
                Assert.Equal(source.Commands[command].Values[coordinate] * scale + (coordinate % 2 == 0 ? dx : dy), fitted.Commands[command].Values[coordinate], 9);
        Assert.Equal(2.5 * scale, fitted.StrokeWidth, 9); Assert.Equal(8 * scale, fitted.DashPattern![0], 9);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        var second = restored.Prepare().ToInterchangeEnvelope();
        Assert.Equal(saved.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)), second.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Equal(saved.Topology!.PathDecorations, second.Topology!.PathDecorations);
    }

    [Fact]
    public void PreparedManualSceneKeepsLowOriginOverlappingBodiesGroupsAndAuthoredCoordinateSiblingsUntilFreshLayout() {
        var chart = TopologyChart.Create().WithId("retained-scene-domain").WithViewport(400, 260, 0).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("a", "A", 40, 60, width: 120, height: 70).AddNode("b", "B", 220, 60, width: 120, height: 70)
            .AddEdge("hints", "a", "b").AddEdge("curve", "a", "b", "Label");
        chart.Groups.Add(new TopologyGroup { Id = "frame", Label = "Frame", X = 30, Y = 30, Width = 320, Height = 180, ShowLabel = false });
        foreach (var node in chart.Nodes) { node.Kind = TopologyNodeKind.Generic; node.GroupId = "frame"; node.ShowStatusBadge = false; }
        var saved = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false }).ToInterchangeEnvelope();
        // A supported prepared Manual input is authoritative even when bodies overlap and ink lies inside default authoring padding.
        saved.Groups[0].X = 2; saved.Groups[0].Y = 3;
        saved.Nodes[0].X = 60; saved.Nodes[0].Y = 60; saved.Nodes[1].X = 65; saved.Nodes[1].Y = 60;
        saved.Topology!.PathDecorations.Add(new TopologyPathDecoration("origin-ink", "M 1 20 C 30 10 70 10 90 20", stroke: ChartColor.FromHex("#112233")));
        var hints = saved.Edges.Single(edge => edge.Id == "hints").Topology!;
        hints.Waypoints.Add(new VisualArtifactInterchangePoint { X = 40, Y = 90 }); hints.Waypoints.Add(new VisualArtifactInterchangePoint { X = 200, Y = 90 });
        hints.LabelAnchor = new VisualArtifactInterchangePoint { X = 35, Y = 35 }; hints.RouteLane = 2;
        var curve = saved.Edges.Single(edge => edge.Id == "curve").Topology!;
        curve.PathGeometry = new TopologyPathGeometry("M 180 90 C 210 30 100 35 65 90");
        curve.LabelPosition = new TopologyLabelPosition(new ChartPoint(40, 25));
        var imported = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        var prepared = imported.Prepare(new TopologyRenderOptions { IncludeLegend = false }); var second = prepared.ToInterchangeEnvelope();
        Assert.Equal(saved.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)), second.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Assert.Equal(saved.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)), second.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)));
        Equal(saved.Topology.PathDecorations, second.Topology!.PathDecorations);
        var retainedHints = second.Edges.Single(edge => edge.Id == "hints").Topology!;
        Assert.Equal(hints.Waypoints.Select(point => (point.X, point.Y)), retainedHints.Waypoints.Select(point => (point.X, point.Y)));
        Assert.Equal(hints.RouteLane, retainedHints.RouteLane); Assert.Equal((35d, 35d), (retainedHints.LabelAnchor!.X, retainedHints.LabelAnchor!.Y));
        var retainedCurve = second.Edges.Single(edge => edge.Id == "curve").Topology!;
        Assert.Equal(curve.PathGeometry!.PathData, retainedCurve.PathGeometry!.PathData); Assert.Equal(curve.LabelPosition!.Point, retainedCurve.LabelPosition!.Point);
        Assert.NotEmpty(prepared.ToPng()); Assert.Contains("origin-ink", prepared.ToSvg());
        Assert.Equal(2, imported.Groups[0].X); Assert.Equal(65, imported.Nodes[1].X);
        imported.Edges[0].Waypoints.Clear(); imported.Edges[1].PathGeometry = null; imported.Edges[1].LabelPosition = null;
        imported.LayoutMode = TopologyLayoutMode.Layered;
        var fresh = imported.Prepare(new TopologyRenderOptions { IncludeLegend = false }).ToInterchangeEnvelope();
        Assert.Equal(saved.Nodes.Select(node => node.Id), fresh.Nodes.Select(node => node.Id));
        Assert.NotEqual(second.Nodes.Select(node => (node.X, node.Y)).ToArray(), fresh.Nodes.Select(node => (node.X, node.Y)).ToArray());
    }

    [Fact]
    public void HierarchyKeepsRawAuthoredInkSeparateFromItsFittedVisibleSnapshot() {
        var chart = TopologyChart.Create().WithId("organization").WithViewport(1000, 650).WithLegend(null).AddHierarchy(new[] {
            new TopologyHierarchyItem("lead", "Avery Quinn"), new TopologyHierarchyItem("engineering", "Engineering", "lead"),
            new TopologyHierarchyItem("platform", "Platform", "engineering") });
        var ink = new TopologyPathDecoration("service-zone", "M 10.123456789012346 20 L 900 500", stroke: ChartColor.FromHex("#123456"));
        chart.PathDecorations.Add(ink);
        var saved = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(420, 300), 16)),
            new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true, HierarchyView = new TopologyHierarchyView(new[] { "engineering" }) }).ToArtifact().ToInterchangeEnvelope();
        Assert.Equal(ink.PathData, Assert.Single(saved.Topology!.Hierarchy!.Source.Topology!.PathDecorations).PathData);
        Assert.NotEqual(ink.PathData, Assert.Single(saved.Topology!.PathDecorations).PathData);
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson());
        Assert.Equal(ink.PathData, Assert.Single(loaded.ToTopologyHierarchySource().PathDecorations).PathData);
        Equal(saved.Topology!.PathDecorations, loaded.ToTopologyChart().PathDecorations);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[[\"M\",0],[\"L\",1,2]]")]
    [InlineData("[[\"L\",0,0],[\"L\",1,2]]")]
    [InlineData("[[\"M\",0,0],[\"C\",1,2]]")]
    [InlineData("[[\"M\",0,0],[\"Q\",1,2,3,4]]")]
    [InlineData("[[\"M\",0,0],[\"M\",1,2]]")]
    [InlineData("[[\"M\",0,0],[\"L\",1,2],[\"M\",3,4]]")]
    [InlineData("[[\"M\",0,0],[\"L\",1000000001,2]]")]
    public void MalformedTypedCommandsFailBeforeNativeRestorationWithAnArgumentError(string commands) {
        var json = JsonWithPath(); var start = json.IndexOf("\"commands\":", StringComparison.Ordinal) + "\"commands\":".Length;
        var end = json.IndexOf(",\"stroke\"", start, StringComparison.Ordinal);
        var malformed = json.Substring(0, start) + commands + json.Substring(end);
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(malformed));
    }

    [Fact]
    public void VersionOneCannotSilentlyDropPathsAndFutureVersionsStillFailWhileEmptyOutputKeepsVersionOne() {
        var json = JsonWithPath();
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1")));
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":3")));
        var ordinaryChart = TopologyPathDecorationRecipe.Create("plain");
        ordinaryChart.Viewport.Padding = new TopologyViewport().Padding;
        var ordinaryPrepared = ordinaryChart.Prepare(); var ordinary = ordinaryPrepared.ToInterchangeEnvelope();
        if (Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY") is { Length: > 0 } capture) {
            Directory.CreateDirectory(capture); var stem = Path.Combine(capture, "path-empty-genuine-default-v1");
            File.WriteAllText(stem + ".svg", ordinaryPrepared.ToSvg()); File.WriteAllBytes(stem + ".native.png", ordinaryPrepared.ToPng()); File.WriteAllText(stem + ".json", ordinary.ToJson());
        }
        Assert.Equal(1, ordinary.Version); Assert.DoesNotContain("pathDecorations", ordinary.ToJson());
        Assert.Equal(ordinary.ToJson(), VisualArtifactInterchangeEnvelope.FromJson(ordinary.ToJson()).ToJson());
    }

    [Fact]
    public void PortableIdsRejectMappedEntityCollisionsRatherThanDroppingOrRenamingInk() {
        var chart = TopologyChart.Create().WithViewport(400, 250).WithLegend(null).AddNode("shared", "Worker", 40, 40);
        chart.Groups.Add(new TopologyGroup { Id = "shared", Label = "Group", X = 0, Y = 0, Width = 300, Height = 180 });
        chart.PathDecorations.Add(new TopologyPathDecoration("node-shared", "M 0 0 L 20 20"));
        Assert.Contains("node-shared", Assert.Throws<ArgumentException>(() => chart.Prepare().ToInterchangeEnvelope().ToJson()).Message);
    }

    [Fact]
    public void NativeAndPortableCollectionsShareCumulativeCommandBudgetsAndPaintValidation() {
        var commands = "M 0 0 " + string.Join(" ", Enumerable.Repeat("L 1 1", 4095));
        var chart = TopologyPathDecorationRecipe.Create("plain");
        for (var index = 0; index < 17; index++) chart.PathDecorations.Add(new TopologyPathDecoration("limit-" + index, commands));
        var nativeLimit = Assert.Throws<TopologyValidationException>(() => chart.Prepare());
        Assert.Contains(nativeLimit.Result.Errors, error => error.Code == "topology-path-budget");
        var portable = TopologyPathDecorationRecipe.Create("plain").Prepare().ToInterchangeEnvelope();
        portable.Topology!.PathDecorations.AddRange(chart.PathDecorations);
        Assert.Throws<ArgumentException>(() => portable.ToJson());
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(JsonWithPath().Replace("\"strokeWidth\":1", "\"strokeWidth\":-1")));
        Assert.ThrowsAny<ArgumentException>(() => new TopologyPathDecoration("finite", "M 0 0 L 1 1", strokeWidth: double.NaN));
    }

    private static string JsonWithPath() {
        var chart = TopologyPathDecorationRecipe.Create("plain");
        chart.PathDecorations.Add(new TopologyPathDecoration("curve", "M 0 0 L 20 20", stroke: ChartColor.FromHex("#112233")));
        return chart.Prepare().ToInterchangeEnvelope().ToJson();
    }
    private static void Equal(IReadOnlyList<TopologyPathDecoration> expected, IReadOnlyList<TopologyPathDecoration> actual) {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++) {
            Assert.Equal(expected[index].Id, actual[index].Id); Assert.Equal(expected[index].PathData, actual[index].PathData);
            Assert.Equal(expected[index].Fill, actual[index].Fill); Assert.Equal(expected[index].Stroke, actual[index].Stroke);
            Assert.Equal(expected[index].StrokeWidth, actual[index].StrokeWidth); Assert.Equal(expected[index].Close, actual[index].Close);
            Assert.Equal(expected[index].DashPattern, actual[index].DashPattern);
        }
    }
}
