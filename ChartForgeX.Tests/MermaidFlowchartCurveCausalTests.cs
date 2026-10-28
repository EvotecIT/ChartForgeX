using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidFlowchartCurveCausalTests {
    private const string Diamond = "flowchart LR\nA[Start] --> B[Upper]\nA --> C[Lower]\nB --> D[Finish]\nC --> D";
    [Theory]
    [InlineData("default")] [InlineData("basis")] [InlineData("bumpX")] [InlineData("bumpY")]
    [InlineData("cardinal")] [InlineData("catmullRom")] [InlineData("monotoneX")] [InlineData("monotoneY")] [InlineData("natural")]
    public void LiteralSourceCurvesAndPinnedDefaultProduceNumericCubicPaint(string curve) {
        var source = curve == "default" ? Diamond : "---\nconfig:\n  flowchart:\n    curve: " + curve + "\n---\n" + Diamond;
        var result = Observe(source, "source-curve-" + curve); var (paths, failures) = Capture(result, "source-curve-" + curve);
        Assert.Empty(failures); Assert.NotNull(result.Artifact); Assert.Empty(result.Diagnostics);
        Assert.Contains(paths, path => path.Contains('C'));
        Assert.Equal(new[] { "A", "B", "C", "D" }, Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes.Select(node => node.Id));
    }

    [Fact]
    public void AuthoredEdgeCurveMetadataDoesNotMaterializeACollidingNode() {
        const string source = "flowchart LR\nA e@--> B\nA --> C\nB --> D\nC --> D\ne@{curve: basis}";
        var result = Observe(source, "source-edge-curve"); var (paths, failures) = Capture(result, "source-edge-curve");
        Assert.Empty(failures); Assert.NotNull(result.Artifact); Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(new[] { "A", "B", "C", "D" }, chart.Nodes.Select(node => node.Id));
        Assert.Contains(chart.Edges, edge => edge.Id == "e"); Assert.Contains(paths, path => path.Contains('C'));
    }

    [Fact]
    public void ExplicitLinearKeepsItsOrdinaryNativeLinePaintControl() {
        var source = "---\nconfig:\n  flowchart:\n    curve: linear\n---\n" + Diamond;
        var result = Observe(source, "source-curve-linear-control"); var (paths, failures) = Capture(result, "source-curve-linear-control");
        Assert.Empty(failures); Assert.False(result.HasErrors); Assert.NotEmpty(paths);
        Assert.All(paths, path => Assert.DoesNotContain("C", path));
    }

    private static MermaidRenderResult Observe(string source, string name) {
        Write(name + ".input.mmd", source);
        return MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Allow,
            Flowchart = new MermaidFlowchartRenderOptions { Width = 720, Height = 480, FitContent = false, Title = "",
                Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, TopologySizing = MermaidTopologySizing.Natural } } });
    }
    private static (string[] Paths, List<Exception> Failures) Capture(MermaidRenderResult result, string name) {
        var failures = new List<Exception>(); var paths = Array.Empty<string>();
        Write(name + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message, item.Severity, item.Span.Line, item.Span.Column, item.Span.Length })));
        var artifact = result.Artifact;
        if (artifact == null) return (paths, failures);
        Attempt("svg", () => { var svg = artifact.ToSvg(); Write(name + ".svg", svg);
            paths = XDocument.Parse(svg).Descendants().Where(item => ((string?)item.Attribute("data-cfx-role"))?.StartsWith("topology-edge-line", StringComparison.Ordinal) == true)
                .Select(item => (string?)item.Attribute("d") ?? "").ToArray(); Write(name + ".paths.json", JsonSerializer.Serialize(paths)); });
        Attempt("png", () => { var png = artifact.ToPng(); var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png); });
        Attempt("portable", () => Write(name + ".json", artifact.ToInterchangeJson()));
        return (paths, failures);
        void Attempt(string lane, Action action) { try { action(); } catch (Exception error) { failures.Add(error); Write(name + "." + lane + ".failure.json", JsonSerializer.Serialize(new { Type = error.GetType().FullName, error.Message })); } }
    }
    private static void Write(string name, string value) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), value); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_CURVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
