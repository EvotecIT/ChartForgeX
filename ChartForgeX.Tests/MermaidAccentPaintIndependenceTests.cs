using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidAccentPaintIndependenceTests {
    [Theory]
    [InlineData("f-circ", "default", null)]
    [InlineData("f-circ", "fill-only", "fill:#ff0000")]
    [InlineData("f-circ", "stroke-only", "stroke:#0000ff")]
    [InlineData("f-circ", "both", "fill:#ff0000,stroke:#0000ff")]
    [InlineData("fr-circ", "default", null)]
    [InlineData("fr-circ", "fill-only", "fill:#ff0000")]
    [InlineData("fr-circ", "stroke-only", "stroke:#0000ff")]
    [InlineData("fr-circ", "both", "fill:#ff0000,stroke:#0000ff")]
    [InlineData("fork", "default", null)]
    [InlineData("fork", "fill-only", "fill:#ff0000")]
    [InlineData("fork", "stroke-only", "stroke:#0000ff")]
    [InlineData("fork", "both", "fill:#ff0000,stroke:#0000ff")]
    public void AuthoredAccentFillAndStrokeRemainIndependentThroughNativePaintAndPortableReplay(string shape, string variant, string? style) {
        var source = "flowchart LR\nwork@{ shape: " + shape + ", label: 'Work' }"
            + (style == null ? "" : "\nstyle work " + style);
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Flowchart = new MermaidFlowchartRenderOptions { Title = "", Width = 400, Height = 260 },
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family }
        });
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact);
        var prepared = Assert.IsType<TopologyChart>(artifact.Model).Prepare();
        var marks = prepared.Visual.Scene.Nodes.OfType<VisualScenePath>()
            .Where(mark => mark.Role == "topology-node-surface").ToArray();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson())
            .ToTopologyChart().Prepare();
        var output = Environment.GetEnvironmentVariable("CFX_M01_ACCENT_FILL_BEFORE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, shape + "-" + variant);
            File.WriteAllText(stem + ".mmd", source);
            File.WriteAllText(stem + ".svg", artifact.ToSvg());
            File.WriteAllBytes(stem + ".png", artifact.ToPng());
            File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
            File.WriteAllText(stem + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics));
            File.WriteAllText(stem + ".paint.json", JsonSerializer.Serialize(marks.Select(mark => new {
                Fill = mark.Fill?.ToHexRgba(), Stroke = mark.Stroke?.ToHexRgba(),
                mark.StrokeWidth, mark.Opacity
            })));
            File.WriteAllText(stem + "-restored.svg", restored.ToSvg());
            File.WriteAllBytes(stem + "-restored.png", restored.ToPng());
        }
        Assert.False(result.HasErrors);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(shape == "fr-circ" ? 2 : 1, marks.Length);
        if (variant is "fill-only" or "both")
            Assert.All(marks, mark => Assert.Equal(ChartColor.FromHex("#ff0000"), mark.Fill));
        if (variant is "stroke-only" or "both")
            Assert.All(marks, mark => Assert.Equal(ChartColor.FromHex("#0000ff"), mark.Stroke));
        // These omitted-property defaults are observed independently in the pinned
        // reference: f-circ has a literal default fill until a style replaces it.
        var expectedFill = variant is "fill-only" or "both" ? "#ff0000"
            : shape == "f-circ" && variant == "default" ? "#9370db" : "#ececff";
        var expectedStroke = variant is "stroke-only" or "both" ? "#0000ff" : "#9370db";
        Assert.All(marks, mark => {
            Assert.Equal(ChartColor.FromHex(expectedFill), mark.Fill);
            Assert.Equal(ChartColor.FromHex(expectedStroke), mark.Stroke);
        });
        Assert.Equal(artifact.ToPng(), restored.ToPng());
    }

    [Theory]
    [InlineData("f-circ")] [InlineData("fr-circ")] [InlineData("fork")]
    public void ClassFillAndDirectStrokeKeepTheirIndependentPaintOwners(string shape) {
        var source = "flowchart LR\nwork@{ shape: " + shape + ", label: 'Work' }\n"
            + "classDef selected fill:#ff0000\nclass work selected\nstyle work stroke:#0000ff";
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family }
        });
        Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact);
        var marks = Assert.IsType<TopologyChart>(artifact.Model).Prepare().Visual.Scene.Nodes
            .OfType<VisualScenePath>().Where(mark => mark.Role == "topology-node-surface").ToArray();
        Assert.NotEmpty(marks);
        Assert.All(marks, mark => {
            Assert.Equal(ChartColor.FromHex("#ff0000"), mark.Fill);
            Assert.Equal(ChartColor.FromHex("#0000ff"), mark.Stroke);
        });
        var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart().Prepare();
        Assert.Equal(artifact.ToPng(), restored.ToPng());
    }
}
