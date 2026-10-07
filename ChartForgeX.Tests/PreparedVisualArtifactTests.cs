using System.Text;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedVisualArtifactTests {
    [Fact]
    public void PreparedSemanticSnapshotAutomaticallyTransfersToIndependentHostArtifacts() {
        var source = CreateChart().Prepare(Context());
        var semantics = source.ToArtifact("source", VisualArtifactKind.Chart).ToInterchangeEnvelope();
        semantics.Extensions["source-note"] = "Captured";
        var prepared = new PreparedVisual(source.Scene, source.Accessibility, semantics);
        semantics.Extensions["source-note"] = "Changed caller";
        var first = prepared.ToArtifact("host-first", VisualArtifactKind.Chart);
        var second = prepared.ToArtifact("host-second", VisualArtifactKind.Chart);
        Assert.True(first.SupportsExport(VisualArtifactExportFormat.Json));
        Assert.Equal("host-first", first.ToInterchangeEnvelope().Id);
        Assert.Equal("host-second", second.ToInterchangeEnvelope().Id);
        Assert.Equal("Captured", first.ToInterchangeEnvelope().Extensions["source-note"]);
        first.Metadata["source-note"] = "Changed host";
        Assert.Equal("Captured", second.ToInterchangeEnvelope().Extensions["source-note"]);
        Assert.Equal("source", prepared.SemanticInterchange!.Id);
        Assert.Equal(source.ToPng(), prepared.ToPng());
        Assert.Throws<ArgumentException>(() => prepared.ToArtifact("host-invalid", VisualArtifactKind.Topology));
    }
    [Fact]
    public void StaticArtifactUsesPreparedOutputAndKeepsPortableHostMetadata() {
        var prepared = CreateChart().Prepare(Context());
        var artifact = prepared.ToArtifact("cpu-load", VisualArtifactKind.Chart, title: "CPU load");

        Assert.Same(prepared, artifact.Model);
        Assert.Equal(PaintGeometry(prepared.ToSvg()), PaintGeometry(artifact.ToSvg()));
        Assert.Equal(prepared.ToPng(), artifact.ToPng());
        Assert.Contains(artifact.ToSvg(), artifact.ToHtmlPage());
        Assert.Contains("<html lang=\"pl-PL\">", artifact.ToHtmlPage());
        Assert.DoesNotContain("<script", artifact.ToHtmlPage());
        Assert.Equal(320, artifact.NaturalSize!.Value.Width);
        Assert.Equal(200, artifact.NaturalSize.Value.Height);
        Assert.True(artifact.PreserveNaturalSize);
        Assert.Equal(prepared.Regions.Select(region => region.Id), artifact.Regions.Select(region => region.Id));
        Assert.NotEmpty(artifact.Regions);
        Assert.Equal(prepared.Regions[0].Bounds, artifact.Regions[0].Bounds);

        // These byte/JSON properties are the ALC-safe boundary used by ImagePlayground and OfficeIMO.
        byte[] officeVisualSvg = Encoding.UTF8.GetBytes(artifact.ToSvg());
        byte[] officeVisualInterchangeJson = artifact.ToInterchangeUtf8Json();
        var portable = VisualArtifactInterchangeEnvelope.FromUtf8Json(officeVisualInterchangeJson);
        Assert.Equal(artifact.ToSvg(), Encoding.UTF8.GetString(officeVisualSvg));
        Assert.Equal("cpu-load", portable.Id);
        Assert.Equal("CPU load", portable.Title);
        Assert.Equal("Synthetic CPU utilization.", portable.AccessibleDescription);
        Assert.Equal("pl-PL", portable.Language);
        Assert.Equal(VisualArtifactInterchangeFamily.None, portable.Family);
        Assert.Empty(portable.Nodes);
        Assert.Empty(portable.Edges);
    }

    [Fact]
    public void HostEnvelopeChangesDoNotMutatePreparedOutputOrReturnedRegions() {
        var prepared = CreateChart().Prepare(Context());
        string svg = prepared.ToSvg();
        byte[] png = prepared.ToPng();
        var artifact = prepared.ToArtifact("cpu-load", VisualArtifactKind.Chart);
        artifact.Title = "Host title";
        artifact.Accessibility.Name = "Host accessible name";
        artifact.Accessibility.Description = "Host description";
        artifact.Accessibility.Language = "de-DE";
        artifact.Metadata["owner"] = "host";
        artifact.Regions[0].Label = "Host region label";

        var portable = artifact.ToInterchangeEnvelope();
        Assert.Equal("Host title", portable.Title);
        Assert.Equal("Host description", portable.AccessibleDescription);
        Assert.Equal("host", portable.Extensions["owner"]);
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(PaintGeometry(svg), PaintGeometry(artifact.ToSvg()));
        Assert.Contains("aria-label=\"Host accessible name\"", artifact.ToSvg());
        Assert.Contains("Host description", artifact.ToSvg());
        Assert.Contains("lang=\"de-DE\"", artifact.ToSvg());
        Assert.Contains("<html lang=\"de-DE\">", artifact.ToHtmlPage());
        Assert.Equal(png, artifact.ToPng());
        Assert.NotEqual("Host region label", prepared.Regions[0].Label);

        artifact.Accessibility.AsDecorative();
        Assert.Contains("aria-hidden=\"true\"", artifact.ToSvg());
        Assert.Null(XDocument.Parse(artifact.ToSvg()).Root!.Attribute("aria-label"));
        Assert.Equal(svg, prepared.ToSvg());

        artifact.NaturalSize = new VisualArtifactSize(640, 400);
        Assert.Throws<InvalidOperationException>(() => artifact.ToSvg());
        Assert.Throws<InvalidOperationException>(() => artifact.ToInterchangeEnvelope());
    }

    [Fact]
    public void SeparateArtifactIdentitiesScopeGeneratedSvgIds() {
        var prepared = CreateChart().Prepare(Context());
        var first = prepared.ToArtifact("1: source / first", VisualArtifactKind.Chart);
        var second = prepared.ToArtifact("2: source / second", VisualArtifactKind.Chart);
        var firstIds = XDocument.Parse(first.ToSvg()).Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        var secondIds = XDocument.Parse(second.ToSvg()).Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.NotEmpty(firstIds);
        Assert.Empty(firstIds.Intersect(secondIds));
        Assert.Equal(PaintGeometry(first.ToSvg()), PaintGeometry(second.ToSvg()));
    }

    [Fact]
    public void SemanticHandoffRejectsAnotherArtifactOrAnotherViewport() {
        var chart = CreateChart().WithSize(320, 200);
        var prepared = chart.Prepare(Context());
        var semantics = chart.ToVisualArtifact("cpu-load").ToInterchangeEnvelope();

        Assert.Throws<ArgumentException>(() => prepared.ToArtifact("another-chart", VisualArtifactKind.Chart, semantics));
        Assert.Throws<ArgumentException>(() => prepared.ToArtifact("cpu-load", VisualArtifactKind.Sequence, semantics));
        semantics.Width = 640;
        Assert.Throws<ArgumentException>(() => prepared.ToArtifact("cpu-load", VisualArtifactKind.Chart, semantics));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeDiagramSemanticsSurviveCallerAndReaderMutation(bool topology) {
        var (prepared, semantics) = CreateDiagram(topology);
        semantics.Nodes[0].Metrics.Add(new VisualArtifactInterchangeMetric { Name = "capacity", Value = "1" });
        if (semantics.Edges[0].ResolvedRoute.Count == 0) {
            semantics.Edges[0].ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = 20, Y = 30 });
            semantics.Edges[0].ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = 80, Y = 30 });
        }
        string original = semantics.ToJson();
        var captured = new PreparedVisual(prepared.Scene, prepared.Accessibility, semantics);
        var artifact = prepared.ToArtifact(semantics.Id, semantics.Kind, semantics);
        Assert.True(artifact.SupportsExport(VisualArtifactExportFormat.Json));
        Assert.Equal(original, artifact.ToInterchangeJson());
        foreach (var node in semantics.Nodes) Assert.Contains(artifact.Regions, region => region.Id == node.Id);

        semantics.Nodes[0].Label = "Changed source label";
        semantics.Nodes[0].Extensions["source-note"] = "Changed source metadata";
        semantics.Edges[0].Label = "Changed source message";
        semantics.Nodes[0].Metrics[0].Value = "Changed metric";
        semantics.Edges[0].ResolvedRoute[0].X = 999;
        var firstRead = artifact.ToInterchangeEnvelope();
        firstRead.Nodes[0].Extensions["reader-note"] = "Changed reader metadata";
        firstRead.Edges.Clear();
        Assert.Equal(original, artifact.ToInterchangeJson());
        Assert.Equal(original, captured.SemanticInterchange!.ToJson());

        artifact.Metadata["owner"] = "reviewed-host";
        var portable = VisualArtifactInterchangeEnvelope.FromUtf8Json(artifact.ToInterchangeUtf8Json());
        Assert.Equal("reviewed-host", portable.Extensions["owner"]);
        Assert.Equal(2, portable.Nodes.Count);
        Assert.Single(portable.Edges);
        Assert.Equal(topology ? VisualArtifactInterchangeFamily.Topology : VisualArtifactInterchangeFamily.Sequence, portable.Family);
    }

    [Fact]
    public void StaticPreparedOutputDoesNotApplyPortableMetricBudgetsDuringDefensiveCapture() {
        var (prepared, semantics) = CreateDiagram(topology: true);
        for (var index = 0; index < 1025; index++)
            semantics.Nodes[0].Metrics.Add(new VisualArtifactInterchangeMetric { Name = "metric-" + index, Value = "1" });
        var captured = new PreparedVisual(prepared.Scene, prepared.Accessibility, semantics);
        var exportOptions = new VisualSvgOptions();
        Assert.Equal(prepared.ToSvg(exportOptions), captured.ToSvg(exportOptions));
        Assert.Equal(prepared.ToPng(), captured.ToPng());
        Assert.Throws<ArgumentOutOfRangeException>(() => captured.SemanticInterchange);
    }

    private static (PreparedVisual Prepared, VisualArtifactInterchangeEnvelope Semantics) CreateDiagram(bool topology) {
        if (topology) {
            var chart = TopologyChart.Create().WithViewport(640, 400);
            chart.Id = "native-topology";
            chart.Title = "Service dependency";
            chart.LayoutMode = TopologyLayoutMode.Manual;
            chart.Accessibility.WithTextAlternative("Service dependency", "API depends on worker.");
            chart.Nodes.Add(new TopologyNode { Id = "api", Label = "API", X = 24, Y = 60, Width = 120, Height = 64 });
            chart.Nodes.Add(new TopologyNode { Id = "worker", Label = "Worker", X = 240, Y = 60, Width = 120, Height = 64 });
            chart.Edges.Add(new TopologyEdge { Id = "request", SourceNodeId = "api", TargetNodeId = "worker", Label = "request", Routing = TopologyEdgeRouting.Straight });
            var semantics = chart.ToVisualArtifact().ToInterchangeEnvelope();
            return (chart.Prepare(DiagramContext(semantics)), semantics);
        }
        var sequence = SequenceArtifact.Create("native-sequence").WithTitle("Service request").WithSize(640, 400)
            .AddParticipant("api", "API").AddParticipant("worker", "Worker").AddMessage("api", "worker", "request");
        sequence.Accessibility.WithTextAlternative("Service request", "API calls worker.");
        var envelope = sequence.ToVisualArtifact().ToInterchangeEnvelope();
        return (sequence.Prepare(DiagramContext(envelope)), envelope);
    }

    private static VisualRenderContext DiagramContext(VisualArtifactInterchangeEnvelope semantics) => new(
        layout: new VisualLayoutOptions(new VisualSize(semantics.Width!.Value, semantics.Height!.Value)),
        frame: new VisualFrame(showLegend: false));

    private static Chart CreateChart() => Chart.Create().WithTitle("CPU load")
        .WithAccessibility(accessibility => accessibility.WithTextAlternative("CPU load", "Synthetic CPU utilization.", "pl-PL"))
        .AddLine("CPU", new[] { new ChartPoint(0, 20), new ChartPoint(1, 35), new ChartPoint(2, 28) });

    private static VisualRenderContext Context() => new(
        layout: new VisualLayoutOptions(new VisualSize(320, 200), padding: 16),
        frame: new VisualFrame(title: "CPU load", showLegend: false));

    private static string[] PaintGeometry(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => element.Name.LocalName is "path" or "rect" or "line" or "ellipse")
        .Select(element => new XElement(element.Name, element.Attributes()
            .Where(attribute => attribute.Name.LocalName is not ("id" or "clip-path"))).ToString(SaveOptions.DisableFormatting)).ToArray();
}
