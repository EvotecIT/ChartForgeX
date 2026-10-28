using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidRequirementThemeColorTests {
    [Fact]
    public void AuthoredRelationLabelBackgroundReachesTheExistingNativeLabelSurface() {
        var source = "%%{init:{\"themeVariables\":{\"relationLabelBackground\":\"#193857\"}}}%%\n" + Body;
        var result = MermaidRenderer.Render(source, Options(1100, false));
        Capture("requirement-label-background-before", result);
        Assert.False(result.HasErrors); Assert.NotNull(result.Artifact); Assert.Empty(result.Diagnostics);
        var surfaces = XDocument.Parse(result.Artifact.ToSvg()).Descendants()
            .Where(item => (string?)item.Attribute("data-cfx-role") == "topology-edge-label-surface").ToArray();
        Assert.NotEmpty(surfaces);
        Assert.All(surfaces, item => {
            Assert.True(ChartColor.TryParse((string?)item.Attribute("fill"), out var actual));
            Assert.Equal(ChartColor.FromHex("#193857"), actual);
        });
    }
}
