using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyFrameThemeRoleCausalTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceTypographyRetainsItsFrameSubtitleAndExplicitLegendRoles(bool configured) {
        var source = (configured ? "---\nconfig:\n  themeVariables:\n    fontSize: 14\n---\n" : "") + "flowchart LR\na[Service]";
        var name = configured ? "configured-14" : "incumbent-default";
        Write(name + ".input.mmd", source);
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Flowchart = new MermaidFlowchartRenderOptions { Title = "", FitContent = false,
                Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, TopologySizing = MermaidTopologySizing.Natural } }
        });
        Write(name + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message })));
        var chart = result.Artifact?.Model as TopologyChart ?? throw new InvalidOperationException("The existing source control must produce topology.");
        chart.WithTitle("Service topology").WithSubtitle("Frame subtitle measured with the source font")
            .WithLegend(TopologyLegend.Create("Health").AddStatus("Available service", TopologyHealthStatus.Healthy))
            .WithViewport(800, 500, 24).WithLayout(TopologyLayoutMode.Manual);
        chart.Nodes[0].X = 200; chart.Nodes[0].Y = 170;
        chart.DefaultRenderOptions = new TopologyRenderOptions { IncludeLegend = true, FitContentToViewport = false };
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var json = prepared.ToInterchangeEnvelope().ToJson();
        Capture(name + "-direct", svg, png, json);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); var after = restored.Prepare();
        var afterSvg = after.ToSvg(); var afterPng = after.ToPng(); var afterJson = after.ToInterchangeEnvelope().ToJson();
        Capture(name + "-restored", afterSvg, afterPng, afterJson);
        Write(name + ".roles.json", JsonSerializer.Serialize(new {
            originalSubtitle = chart.Theme.Typography.SubtitleSize, restoredSubtitle = restored.Theme.Typography.SubtitleSize,
            originalLegend = chart.Theme.Typography.LegendSize, restoredLegend = restored.Theme.Typography.LegendSize
        }));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(chart.Theme.Typography.SubtitleSize, restored.Theme.Typography.SubtitleSize);
        Assert.Equal(chart.Theme.Typography.LegendSize, restored.Theme.Typography.LegendSize);
        Assert.Equal(png, afterPng); Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    private static void Capture(string name, string svg, byte[] png, string json) {
        Write(name + ".svg", svg); Write(name + ".json", json); var directory = Output();
        if (directory != null) File.WriteAllBytes(Path.Combine(directory, "frame-roles-" + name + ".native.png"), png);
    }
    private static void Write(string name, string value) { var directory = Output(); if (directory != null) File.WriteAllText(Path.Combine(directory, "frame-roles-" + name), value); }
    private static string? Output() {
        var directory = Environment.GetEnvironmentVariable("CFX_EXTERNAL_UNIT_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return null; Directory.CreateDirectory(directory); return directory;
    }
}
