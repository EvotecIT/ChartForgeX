using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidC4EdgeCaptionTests {
    // c4-config: a labelled relationship between grid neighbours. c4-style-shapes: a person into an adjacent
    // boundary. c4-layout-config: a person above a boundary it operates.
    [Theory]
    [InlineData("c4-config-typography-native", 1100)] [InlineData("c4-config-typography-native", 390)]
    [InlineData("c4-style-shapes-native", 1100)] [InlineData("c4-layout-config-native", 1100)]
    public void ShortAndBoundaryRelationshipsKeepTheirCaptionOnTheirOwnRoute(string fixture, int width) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", fixture + ".mmd"));
        var options = new MermaidRenderOptions {
            C4 = new MermaidTopologyRenderOptions { Width = width, Height = 800, Padding = 32 },
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, ThemeMode = VisualThemeMode.Light }
        };
        var result = MermaidRenderer.Render(source, options); Assert.False(result.HasErrors);
        var labels = XDocument.Parse(result.Artifact!.ToSvg()).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-label").ToArray();
        Assert.NotEmpty(labels);
        foreach (var label in labels) {
            // The painted caption sits where the route anchors it rather than beside the line.
            Assert.Equal(Number(label, "data-label-anchor-x"), Number(label, "data-label-x"), 3);
            Assert.Equal(Number(label, "data-label-anchor-y"), Number(label, "data-label-y"), 3);
        }
    }

    private static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
}
