using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Checks that an automatic shape hint does not resolve an absent authored catalog identity.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyAutomaticIconResolutionCausalTests {
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeDisplayMode.Artwork)]
    public void AutomaticNativeShapeDoesNotBypassRequiredCatalogResolution(TopologyNodeDisplayMode mode) {
        var chart = Chart(mode, TopologyIconShape.Auto, "owned:missing");
        Assert.IsType<TopologyValidationException>(Observe("missing-" + (chart.Nodes[0].IconShape?.ToString() ?? "null") + "-" + mode, () => chart.Prepare(StrictOptions())));
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeDisplayMode.Artwork)]
    public void NullShapeRetainsTheIncumbentMissingCatalogFailure(TopologyNodeDisplayMode mode) {
        var chart = Chart(mode, null, "owned:missing");
        Assert.IsType<TopologyValidationException>(Observe("missing-" + (chart.Nodes[0].IconShape?.ToString() ?? "null") + "-" + mode, () => chart.Prepare(StrictOptions())));
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeDisplayMode.Artwork)]
    public void AutomaticShapeKeepsTheResolvedCatalogGlyphAndPixels(TopologyNodeDisplayMode mode) {
        var chart = Chart(mode, null, "owned:service");
        var options = StrictOptions();
        var control = chart.Prepare(options);
        chart.Nodes[0].IconShape = TopologyIconShape.Auto;
        var automatic = chart.Prepare(options);
        Capture("catalog-" + mode + "-null", control); Capture("catalog-" + mode + "-auto", automatic);
        Assert.Equal(control.ToPng(), automatic.ToPng());
        Assert.NotEmpty(automatic.ToSvg());
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeDisplayMode.CompactCard)]
    [InlineData(TopologyNodeDisplayMode.Pill)]
    public void AutomaticShapeDoesNotAddAnInlineGlyphOrChangeGenericCardPixels(TopologyNodeDisplayMode mode) {
        var chart = Chart(mode, null, null); chart.Nodes[0].Kind = TopologyNodeKind.Generic; chart.Nodes[0].FitLabelToContent = true;
        var control = chart.Prepare(); Capture("generic-" + mode + "-null", control);
        chart.Nodes[0].IconShape = TopologyIconShape.Auto;
        var automatic = chart.Prepare(); Capture("generic-" + mode + "-auto", automatic);
        Assert.Equal(control.ToPng(), automatic.ToPng());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AutomaticShapePreservesOrdinaryEmbeddedTableAdmissionAndPixels(bool portable) {
        var parsed = MermaidRenderer.Render(UseCaseJsonTableFixture.Read(), MermaidUseCaseJsonTableTests.Options(1100, false));
        Assert.Empty(parsed.Diagnostics); var chart = Assert.IsType<TopologyChart>(parsed.Artifact!.Model);
        var control = chart.Prepare(); var stem = "table-" + (portable ? "portable" : "direct"); Capture(stem + "-null", control);
        PreparedTopology? automatic = null;
        var error = Observe(stem, () => {
            if (portable) {
                var envelope = control.ToInterchangeEnvelope();
                envelope.Nodes.Single(node => node.Id == "Details").Topology!.IconShape = TopologyIconShape.Auto;
                chart = envelope.ToTopologyChart();
            } else chart.Nodes.Single(node => node.Id == "Details").IconShape = TopologyIconShape.Auto;
            automatic = chart.Prepare(); return automatic;
        });
        Assert.Null(error); Assert.NotNull(automatic);
        Assert.Equal(control.ToPng(), automatic!.ToPng());
        Assert.Equal(TopologyIconShape.Auto, automatic.ToInterchangeEnvelope().Nodes.Single(node => node.Id == "Details").Topology!.IconShape);
    }

    private static Exception? Observe(string stem, Func<PreparedTopology> prepare) {
        Exception? error = null;
        try { Capture(stem, prepare()); } catch (Exception actual) { error = actual; }
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, stem + ".observed.json"), JsonSerializer.Serialize(new { exception = error?.GetType().FullName, message = error?.Message }));
        }
        return error;
    }
    private static void Capture(string stem, PreparedTopology prepared) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, stem + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, stem + ".png"), prepared.ToPng());
        TopologyActorCaptionProjectionTests.CaptureCaption(Path.Combine(output, stem + ".caption-facts.json"), prepared);
        File.WriteAllText(Path.Combine(output, stem + ".json"), prepared.ToInterchangeEnvelope().ToJson());
    }

    private static TopologyChart Chart(TopologyNodeDisplayMode mode, TopologyIconShape? shape, string? iconId) {
        var chart = TopologyChart.Create().WithViewport(600, 300, 20).WithLayout(TopologyLayoutMode.Manual);
        chart.Nodes.Add(new TopologyNode { Id = "service", Label = "Service", X = 80, Y = 80, Width = 180, Height = 90,
            IconId = iconId, IconShape = shape, DisplayMode = mode });
        return chart;
    }

    private static TopologyRenderOptions StrictOptions() => new() {
        RequireResolvedIcons = true,
        IconCatalog = new TopologyIconCatalog().AddPack(new TopologyIconPack("owned", "Owned").AddIcon(
            new TopologyIconDefinition("owned", "service", "Service", TopologyNodeKind.Service) { Shape = TopologyIconShape.Server }))
    };
}
