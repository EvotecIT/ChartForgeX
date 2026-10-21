using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Content-sized gallery fixtures keep exact baselines across installed font environments.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidGalleryTypographyTests {
    private static readonly string[] Samples = {
        "mermaid-class-direction", "mermaid-class-notation",
        "mermaid-class-native-notes-light", "mermaid-class-native-notes-dark",
        "mermaid-state-direction", "mermaid-state-synchronization-light", "mermaid-state-synchronization-dark"
    };

    [Fact]
    public void PublishedClassAndStateSamplesRetainNativeGeometryWhenHostFallbackFacesChange() {
        var output = Path.Combine(Path.GetTempPath(), "CFX-Mermaid-gallery-font-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var first = Generate("Regular", Path.Combine(output, "regular"));
            var second = Generate("Bold", Path.Combine(output, "bold"));
            // The unpinned public renderer proves these really are different font environments.
            Assert.NotEqual(first.UnpinnedWidth, second.UnpinnedWidth);
            foreach (var name in Samples) {
                Assert.Equal(first.Artifacts[name].Svg, second.Artifacts[name].Svg);
                Assert.Equal(first.Artifacts[name].Png, second.Artifacts[name].Png);
                var svg = XDocument.Parse(first.Artifacts[name].Svg);
                Assert.Contains(svg.Descendants().Attributes("font-family"), attribute => attribute.Value.Split(',')[0].Trim().Trim('\'', '"') == "CFX Proof Carlito");
                var html = File.ReadAllText(Path.Combine(output, "regular", name + ".html"));
                Assert.Contains("@font-face", html, StringComparison.Ordinal);
                Assert.Contains("data:font/ttf;base64,", html, StringComparison.Ordinal);
            }
        } finally {
            FontRegistry.Clear();
            Directory.Delete(output, recursive: true);
        }
    }

    private static (double UnpinnedWidth, Dictionary<string, (string Svg, byte[] Png)> Artifacts) Generate(string hostFace, string output) {
        FontRegistry.Clear();
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-" + hostFace + ".ttf");
        foreach (var family in new[] { "Calibri", "Carlito", "Segoe UI", "Arial", "Inter", "system-ui", "sans-serif" })
            foreach (var weight in new[] { 400, 600, 700 }) FontRegistry.Register(family, path, weight);
        const string control = "classDiagram\ndirection RL\nService --> Store : writes\nnote for Service \"Requests: Service --> Store\"\n";
        var rendered = MermaidRenderer.Render(control);
        Assert.False(rendered.HasErrors);
        var unpinnedWidth = double.Parse(XDocument.Parse(rendered.Artifact!.ToSvg()).Root!.Attribute("width")!.Value, CultureInfo.InvariantCulture);

        // Exercise the existing executable entry point, including its real sample routing and exports.
        var entryPoint = typeof(GalleryWriter).Assembly.EntryPoint!;
        try { entryPoint.Invoke(null, new object[] { new[] { "--mermaid-only", "--output", output } }); }
        catch (TargetInvocationException exception) when (exception.InnerException != null) {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
        using var comparison = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
        var rows = comparison.RootElement.GetProperty("charts").EnumerateArray().ToDictionary(row => row.GetProperty("name").GetString()!);
        var artifacts = new Dictionary<string, (string Svg, byte[] Png)>();
        foreach (var name in Samples) {
            var row = rows[name];
            foreach (var format in new[] { "svg", "png", "html" }) Assert.True(row.GetProperty(format).GetProperty("healthy").GetBoolean(), name + " " + format);
            artifacts.Add(name, (File.ReadAllText(Path.Combine(output, name + ".svg")), File.ReadAllBytes(Path.Combine(output, name + ".png"))));
        }
        // Source typography probes remain source-owned; the proof face is only the gallery default.
        var authored = XDocument.Load(Path.Combine(output, "mermaid-source-font.svg"));
        Assert.Contains(authored.Descendants().Attributes("font-family"), attribute => attribute.Value.Contains("Georgia", StringComparison.Ordinal));
        var authoredSize = XDocument.Load(Path.Combine(output, "mermaid-source-font-size-icon-fit.svg"));
        Assert.Contains(authoredSize.Descendants().Attributes("font-family"), attribute => attribute.Value.Contains("Arial", StringComparison.Ordinal));
        return (unpinnedWidth, artifacts);
    }
}
