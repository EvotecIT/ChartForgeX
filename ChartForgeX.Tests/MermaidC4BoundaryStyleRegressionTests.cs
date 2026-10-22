using ChartForgeX.Topology;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidC4BoundaryStyleRegressionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BorderOnlyBoundaryStylePaintsTheBorderWithoutRetintingTheSurface(bool dark) {
        const string source = "C4Context\nSystem_Boundary(scope, \"Scope\") {\nPerson(user, \"User\")\n}";
        var options = new MermaidTopologyRenderOptions { Presentation = new MermaidPresentationOptions {
            ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family } };
        var parser = new MermaidParser();
        var baseline = parser.ParseC4(source).Document!.ToTopologyChart(options);
        var styled = parser.ParseC4(source + "\nUpdateElementStyle(scope, $borderColor=\"#ff0000\")").Document!.ToTopologyChart(options);
        var originalGroup = Assert.Single(baseline.Groups); var group = Assert.Single(styled.Groups);
        Assert.Equal(originalGroup.Color, group.Color);
        Assert.Equal(originalGroup.BackgroundColor, group.BackgroundColor);
        Assert.Equal("#ff0000", group.BorderColor);
        var originalSurface = Assert.Single(XDocument.Parse(baseline.Prepare().ToSvg()).Descendants(),
            item => (string?)item.Attribute("data-cfx-role") == "topology-group-surface");
        var prepared = styled.Prepare();
        var surface = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(),
            item => (string?)item.Attribute("data-cfx-role") == "topology-group-surface");
        Assert.Equal((string?)originalSurface.Attribute("fill"), (string?)surface.Attribute("fill"));
        Assert.Equal("#ff0000", ((string?)surface.Attribute("stroke"))?.ToLowerInvariant());
        Assert.Equal("#ff0000", Assert.Single(prepared.ToInterchangeEnvelope().Groups).BorderColor);
        var png = RasterImageDecoder.Decode(prepared.ToPng()); var red = 0;
        for (var index = 0; index < png.Pixels.Length; index += 4)
            if (png.Pixels[index] == 255 && png.Pixels[index + 1] == 0 && png.Pixels[index + 2] == 0 && png.Pixels[index + 3] == 255) red++;
        Assert.True(red > 10, "The authored border must reach the native PNG, not only the semantic model.");
    }
}
