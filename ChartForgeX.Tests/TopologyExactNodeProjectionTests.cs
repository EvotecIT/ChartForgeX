using System.Text.Json.Nodes;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyExactNodeProjectionTests {
    [Fact]
    public void CapturedProjectionRetainsItsExactOffsetAcrossCloneRepeatedCaptureAndPositionEdits() {
        var before = TopologyTranslatedContourCausalTests.Source().Prepare(); Capture("initial", before.Visual);
        var saved = before.ToInterchangeEnvelope(); var basis = saved.Nodes[0].Topology!.PreparedNodeBasis!;
        Assert.NotNull(basis.Projection); Assert.Equal(1, basis.Factor);
        var chart = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        var clone = TopologyLayoutEngine.Clone(chart); var initial = clone.Prepare(); Capture("clone", initial.Visual);
        Assert.Equal(before.ToPng(), initial.ToPng());
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(initial.ToSvg()));
        var repeated = initial.ToInterchangeEnvelope().ToTopologyChart().Prepare(); Capture("repeated", repeated.Visual);
        Assert.Equal(before.ToPng(), repeated.ToPng());
        Assert.Equal(basis.Projection.Offset, repeated.ToInterchangeEnvelope().Nodes[0].Topology!.PreparedNodeBasis!.Projection!.Offset);

        var node = clone.Nodes[0]; var oldX = node.X; var oldY = node.Y;
        node.X += 13; node.Y += 9;
        var edited = clone.Prepare(); Capture("position-edited", edited.Visual);
        var editedSaved = edited.ToInterchangeEnvelope();
        var editedAgain = editedSaved.ToTopologyChart().Prepare(); Capture("position-edited-restored", editedAgain.Visual);
        Assert.Equal(edited.ToPng(), editedAgain.ToPng());
        var projection = editedSaved.Nodes[0].Topology!.PreparedNodeBasis!.Projection!;
        Assert.Equal(basis.Projection.Offset.X + (node.X - oldX), projection.Offset.X);
        Assert.Equal(basis.Projection.Offset.Y + (node.Y - oldY), projection.Offset.Y);
        Assert.Equal(new ChartPoint(node.X, node.Y), projection.BodyOrigin);
        Assert.Equal(oldX, chart.Nodes[0].X); Assert.Equal(oldY, chart.Nodes[0].Y);
    }

    [Theory] [InlineData("size")] [InlineData("shape")] [InlineData("radius")] [InlineData("automatic")]
    public void ExistingGeometryPolicyLifecycleDiscardsTheCapturedProjectionBeforeRecomputing(string edit) {
        var chart = TopologyTranslatedContourCausalTests.Source().Prepare().ToInterchangeEnvelope().ToTopologyChart();
        var retained = chart.Nodes[0].PreparedNodeBasis; Assert.NotNull(retained?.Projection);
        if (edit == "size") chart.Nodes[0].Width += 10;
        else if (edit == "shape") chart.Nodes[0].Shape = TopologyNodeShape.Rectangle;
        else if (edit == "radius") chart.Nodes[0].CornerRadius = 4;
        else chart.WithLayout(TopologyLayoutMode.Layered);
        var ordinary = TopologyLayoutEngine.Clone(chart); ordinary.Nodes[0].PreparedNodeBasis = null;
        var actual = chart.Prepare(); var expected = ordinary.Prepare();
        Capture(edit + "-actual", actual.Visual); Capture(edit + "-ordinary", expected.Visual);
        Assert.Equal(expected.ToPng(), actual.ToPng());
        Assert.Equal(TopologyStaticPaintObservation.Paint(expected.ToSvg()), TopologyStaticPaintObservation.Paint(actual.ToSvg()));
        Assert.NotNull(retained!.Projection);
    }

    [Fact]
    public void FreshStyleAndLabelInputsRemainAuthoritativeWhileTheImmutableProjectionKeepsItsUnits() {
        var chart = TopologyTranslatedContourCausalTests.Source().Prepare().ToInterchangeEnvelope().ToTopologyChart();
        var basis = chart.Nodes[0].PreparedNodeBasis; chart.Nodes[0].BackgroundColor = "#ff0000";
        chart.Nodes[0].BorderColor = "#0000ff"; chart.Nodes[0].StrokeWidth = 3;
        chart.Nodes[0].ShowLabel = true; chart.Nodes[0].Label = "Edited";
        var edited = chart.Prepare(); Capture("style-edited", edited.Visual);
        var surfaces = edited.Visual.Scene.Nodes.OfType<VisualScenePath>().Where(mark => mark.Role == "topology-node-surface").ToArray();
        Assert.NotEmpty(surfaces); Assert.All(surfaces, mark => {
            Assert.Equal(ChartColor.FromHex("#ff0000"), mark.Fill); Assert.Equal(ChartColor.FromHex("#0000ff"), mark.Stroke); Assert.Equal(3, mark.StrokeWidth);
        });
        Assert.Contains(edited.Visual.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Text.Lines.Any(line => line.Text == "Edited"));
        var after = edited.ToInterchangeEnvelope().ToTopologyChart().Prepare(); Capture("style-edited-restored", after.Visual);
        Assert.Equal(edited.ToPng(), after.ToPng()); Assert.Equal(basis!.Projection!.Offset, after.ToInterchangeEnvelope().Nodes[0].Topology!.PreparedNodeBasis!.Projection!.Offset);
    }

    [Theory] [InlineData("offset")] [InlineData("incoherent-body")] [InlineData("missing")] [InlineData("type")]
    public void PortableProjectionRejectsNonfiniteDomainOverflowAndContradictoryOrigins(string mutation) {
        var saved = TopologyTranslatedContourCausalTests.Source().Prepare().ToInterchangeEnvelope();
        var valid = saved.ToJson(); var raw = JsonNode.Parse(valid)!;
        var projection = raw["nodes"]![0]!["topology"]!["preparedNodeBasis"]!["projection"]!;
        if (mutation == "offset") projection["offsetY"] = 1e100;
        else if (mutation == "incoherent-body") projection["bodyY"] = projection["bodyY"]!.GetValue<double>() + 1;
        else if (mutation == "missing") projection.AsObject().Remove("offsetX");
        else projection["bodyX"] = "invalid";
        var malformed = raw.ToJsonString();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "exact-node-projection-" + mutation + "-valid.json"), valid);
            File.WriteAllText(Path.Combine(output, "exact-node-projection-" + mutation + "-invalid.json"), malformed);
        }
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(malformed));
    }

    [Fact]
    public void UntranslatedOrdinaryNativeOutputKeepsItsAbsentBasisAndLegacyWireAdmission() {
        var chart = TopologyChart.Create().WithTitle("").WithLegend(null).WithViewport(480, 360, 24)
            .WithLayout(TopologyLayoutMode.Manual).AddNode("node", "Node", 60, 100, width: 160, height: 64);
        chart.Nodes[0].Shape = TopologyNodeShape.Rounded; chart.Nodes[0].PreserveDisplayModeSize = true;
        chart.Nodes[0].ShowLabel = false; chart.Nodes[0].ShowStatusBadge = false;
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, FitContentToViewport = false });
        var before = chart.Prepare(); Capture("ordinary-absent", before.Visual); var saved = before.ToInterchangeEnvelope();
        Assert.Null(saved.Nodes[0].Topology!.PreparedNodeBasis); Assert.Equal(1, saved.Version);
        var after = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart().Prepare(); Capture("ordinary-restored", after.Visual);
        Assert.Equal(before.ToPng(), after.ToPng()); Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(after.ToSvg()));
    }

    [Fact]
    public void OlderBasisWithoutProjectionFactsRetainsItsExistingAdmissionAndDoesNotInventAnOffset() {
        var saved = TopologyTranslatedContourCausalTests.Source().Prepare().ToInterchangeEnvelope();
        var raw = JsonNode.Parse(saved.ToJson())!;
        raw["nodes"]![0]!["topology"]!["preparedNodeBasis"]!.AsObject().Remove("projection");
        var legacy = raw.ToJsonString();
        var imported = VisualArtifactInterchangeEnvelope.FromJson(legacy).ToTopologyChart();
        Assert.NotNull(imported.Nodes[0].PreparedNodeBasis);
        Assert.Null(imported.Nodes[0].PreparedNodeBasis!.Projection);
        var prepared = imported.Prepare(); Capture("legacy-absent-projection", prepared.Visual);
        Assert.Equal(saved.Nodes[0].X!.Value, imported.Nodes[0].X);
        Assert.Equal(saved.Nodes[0].Y!.Value, imported.Nodes[0].Y);
    }

    private static void Capture(string name, PreparedVisual value) => TopologyTranslatedContourCausalTests.Capture("post-" + name, value);
}
