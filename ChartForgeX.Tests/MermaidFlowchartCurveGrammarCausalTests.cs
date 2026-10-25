using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidFlowchartCurveGrammarCausalTests {
    [Theory]
    [InlineData("default", "linkStyle default interpolate basis")]
    [InlineData("indexed", "linkStyle 0 interpolate bumpX,stroke:#123456")]
    [InlineData("multiple", "linkStyle 0,1 interpolate stepAfter")]
    public void PinnedLinkStyleInterpolationGrammarReachesExistingNativeSourceRendering(string name, string declaration) {
        const string body = "flowchart LR\nA[Start] --> B[Upper]\nA --> C[Lower]\nB --> D[Finish]\nC --> D";
        var source = body + "\n" + declaration; Write(name + ".input.mmd", source);
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Allow, Flowchart = new MermaidFlowchartRenderOptions {
                Width = 720, Height = 480, FitContent = false, Title = "", Presentation = new MermaidPresentationOptions {
                    FontFamily = BrowserProofFonts.Family, TopologySizing = MermaidTopologySizing.Natural }
            }
        });
        Write(name + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message, item.Severity, item.Span.Line, item.Span.Column, item.Span.Length })));
        var failures = new List<Exception>(); var paths = Array.Empty<string>(); var artifact = result.Artifact;
        if (artifact != null) {
            Attempt("svg", () => { var svg = artifact.ToSvg(); Write(name + ".svg", svg); paths = XDocument.Parse(svg).Descendants()
                .Where(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line").Select(element => (string?)element.Attribute("d") ?? "").ToArray();
                Write(name + ".paths.json", JsonSerializer.Serialize(paths)); });
            Attempt("png", () => { var output = Output(); var bytes = artifact.ToPng(); if (output != null) File.WriteAllBytes(Path.Combine(output, "source-curve-grammar-" + name + ".native.png"), bytes); });
            Attempt("portable", () => Write(name + ".json", artifact.ToInterchangeJson()));
        }
        Assert.Empty(failures); Assert.False(result.HasErrors); Assert.NotNull(artifact); Assert.Empty(result.Diagnostics); Assert.NotEmpty(paths);
        if (name == "indexed") Assert.Contains("#123456", artifact.ToSvg());
        void Attempt(string lane, Action action) { try { action(); } catch (Exception error) { failures.Add(error); Write(name + "." + lane + ".failure.json", JsonSerializer.Serialize(new { Type = error.GetType().FullName, error.Message })); } }
    }
    private static void Write(string name, string value) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, "source-curve-grammar-" + name), value); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_CURVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
