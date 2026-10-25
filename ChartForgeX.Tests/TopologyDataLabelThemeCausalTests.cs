using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyDataLabelThemeCausalTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingSourceFontSizeRetainsItsActualNativeThemeThroughPortableRestoration(bool configured) {
        var source = (configured ? "---\nconfig:\n  themeVariables:\n    fontSize: 14\n---\n" : "") + "flowchart LR\na[Native caption]";
        var options = new MermaidRenderOptions { Flowchart = new MermaidFlowchartRenderOptions { Width = 320, Height = 240, FitContent = false, Title = "",
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, TopologySizing = MermaidTopologySizing.Natural } } };
        var name = configured ? "source-data-label-14" : "source-data-label-default";
        Write(name + ".input.mmd", source);
        var result = MermaidRenderer.Render(source, options);
        Write(name + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message })));
        var artifact = result.Artifact ?? throw new InvalidOperationException("The existing source font control must produce an artifact.");
        var chart = (TopologyChart)artifact.Model!; var prepared = chart.Prepare();
        var beforeSvg = prepared.ToSvg(); var beforePng = prepared.ToPng(); var json = prepared.ToInterchangeEnvelope().ToJson();
        Capture(name + "-direct", beforeSvg, beforePng, json);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var after = restored.Prepare(); var afterSvg = after.ToSvg(); var afterPng = after.ToPng(); var afterJson = after.ToInterchangeEnvelope().ToJson();
        Capture(name + "-restored", afterSvg, afterPng, afterJson);
        Write(name + ".theme.json", JsonSerializer.Serialize(new { sourceDataLabelSize = chart.Theme.Typography.DataLabelSize,
            restoredDataLabelSize = restored.Theme.Typography.DataLabelSize, graphiteDataLabelSize = VisualTheme.Graphite().Typography.DataLabelSize }));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(configured ? 14 : 16, chart.Theme.Typography.DataLabelSize);
        Assert.Equal(chart.Theme.Typography.DataLabelSize, restored.Theme.Typography.DataLabelSize);
        Assert.Equal(beforePng, afterPng); Assert.Equal(beforeSvg, prepared.ToSvg()); Assert.Equal(beforePng, prepared.ToPng());
    }
    private static void Capture(string name, string svg, byte[] png, string json) { Write(name + ".svg", svg); Write(name + ".json", json); var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png); }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_EXTERNAL_UNIT_CAPTURE_DIRECTORY") ?? Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
