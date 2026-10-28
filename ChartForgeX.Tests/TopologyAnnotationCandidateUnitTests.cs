using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyAnnotationCandidateUnitTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void EmbeddedTableNotesKeepTheirActualCandidateEndpointsAndPixelsOnBothSides(bool left) {
        var parsed = MermaidRenderer.Render(UseCaseJsonTableFixture.Read(), MermaidUseCaseJsonTableTests.Options(1100, false));
        Assert.Empty(parsed.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(parsed.Artifact!.Model);
        var note = Assert.Single(chart.Annotations);
        chart.Annotations[0] = new TopologyAnnotation(note.Id, note.Text, note.TargetId,
            left ? TopologyAnnotationSide.Left : TopologyAnnotationSide.Right);
        var direct = chart.Prepare(); var saved = direct.ToInterchangeEnvelope(); Capture("table-" + left + "-direct", direct);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart().Prepare();
        Capture("table-" + left + "-restored", restored);
        Assert.NotEqual(1, saved.Annotations[0].TopologyDefaultUnits!.Value);
        Assert.Equal(Leader(direct.ToSvg()), Leader(restored.ToSvg()));
        Assert.Equal(direct.ToPng(), restored.ToPng());
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void AlignedNoteCandidatesProjectTheSameGapAndInsetForEnlargementAndShrink(bool left, bool shrink) {
        var chart = TopologyChart.Create().WithId("aligned-note-candidate").WithTitle("")
            .WithViewport(shrink ? 240 : 800, shrink ? 180 : 500, 24).WithLegend(null).WithLayout(TopologyLayoutMode.Layered)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .AddNode("node", "Node", 220, 100, width: 160, height: 40);
        // The existing automatic layout admits both enlargement and shrink. A shaped Manual graph
        // that already fits deliberately retains unit scale and cannot prove an enlargement contract.
        var node = chart.Nodes[0]; node.Shape = TopologyNodeShape.Rectangle; node.PreserveDisplayModeSize = true;
        node.ShowLabel = false; node.ShowStatusBadge = false;
        chart.Annotations.Add(new TopologyAnnotation("note", "Aligned note", node.Id,
            left ? TopologyAnnotationSide.Left : TopologyAnnotationSide.Right));
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, FitContentToViewport = true });
        var direct = chart.Prepare(); var saved = direct.ToInterchangeEnvelope(); Capture("aligned-" + left + "-" + shrink + "-direct", direct);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart().Prepare();
        Capture("aligned-" + left + "-" + shrink + "-restored", restored);
        var units = saved.Annotations[0].TopologyDefaultUnits!.Value;
        Assert.True(shrink ? units < 1 : units > 1);
        Assert.Equal(Leader(direct.ToSvg()), Leader(restored.ToSvg()));
        Assert.Equal(direct.ToPng(), restored.ToPng());
    }

    private static string Leader(string svg) => Assert.Single(XDocument.Parse(svg).Descendants(),
        element => (string?)element.Attribute("data-cfx-role") == "topology-annotation-leader").Attribute("d")!.Value;

    private static void Capture(string name, PreparedTopology prepared) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "note-candidate189-" + name);
        File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + ".png", prepared.ToPng());
        TopologyActorCaptionProjectionTests.CaptureCaption(stem + ".caption-facts.json", prepared);
        File.WriteAllText(stem + ".json", prepared.ToInterchangeEnvelope().ToJson());
    }
}
