using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidBlockTypographyTests {
    [Fact]
    public void EnlargedWardleyAnnotationKeepsItsNumberInsideTheBadge() {
        var body = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "wardley-basic.mmd"));
        var svg = XDocument.Parse(Render(body, 21).ToSvg());
        var circle = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "wardley-annotation");
        var number = Assert.Single(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "1");
        var callout = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "wardley-annotation-callout");
        Assert.Same(callout, circle.Parent);
        Assert.Same(callout, number.Parent);
        // An authored annotations box owns the complete caption; the plotted callout owns its matching number.
        var legend = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "wardley-annotation-legend-entry"
            && (string?)element.Attribute("data-annotation-number") == number.Value);
        Assert.Contains(legend.Descendants(), element => element.Name.LocalName == "text"
            && element.Value.Contains("Improve automation", StringComparison.Ordinal));
        Assert.Equal("placed", (string?)callout.Attribute("data-cfx-label-status"));
        Assert.Equal((string?)circle.Attribute("transform"), (string?)number.Attribute("transform"));
        var x = double.Parse(number.Attribute("x")!.Value, CultureInfo.InvariantCulture);
        var y = double.Parse(number.Attribute("y")!.Value, CultureInfo.InvariantCulture);
        var cx = double.Parse(circle.Attribute("cx")!.Value, CultureInfo.InvariantCulture);
        var cy = double.Parse(circle.Attribute("cy")!.Value, CultureInfo.InvariantCulture);
        var radius = double.Parse(circle.Attribute("r")!.Value, CultureInfo.InvariantCulture);
        Assert.InRange(x, cx - radius, cx + radius);
        Assert.InRange(y, cy - radius, cy + radius);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData("packet-basic.mmd")]
    [InlineData("block-basic.mmd")]
    [InlineData("gitgraph-basic.mmd")]
    [InlineData("wardley-basic.mmd")]
    [InlineData("venn-basic.mmd")]
    [InlineData("ishikawa-basic.mmd")]
    public async Task BodyTextRolesScaleWhenOnlyTheSourcePixelSizeChanges(string fixture) {
        var body = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", fixture));
        var small = Render(body, 10.5);
        var large = Render(body, 21);
        var originalSvg = small.ToSvg(); var enlargedSvg = large.ToSvg();
        var originalSizes = BodyFontSizes(originalSvg);
        var enlargedSizes = BodyFontSizes(enlargedSvg);
        var captureDirectory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captureDirectory)) {
            Directory.CreateDirectory(captureDirectory);
            File.WriteAllText(Path.Combine(captureDirectory, fixture + "-10.5.svg"), originalSvg);
            File.WriteAllText(Path.Combine(captureDirectory, fixture + "-21.svg"), enlargedSvg);
        }
        Assert.NotEmpty(originalSizes);
        if (fixture == "venn-basic.mmd") {
            // Dense intersections may omit captions that cannot be placed inside all
            // referenced circles. Typography must retain the set identities and fit
            // the remaining captions, rather than guarantee an incidental count.
            foreach (var svg in new[] { originalSvg, enlargedSvg }) {
                var labels = XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value).ToArray();
                foreach (var label in new[] { "API", "UI", "Operations" }) Assert.Contains(label, labels);
            }
            Assert.All(enlargedSizes, size => Assert.InRange(size, 1, originalSizes.Max() * 2 + .001));
        } else {
            Assert.Equal(originalSizes.Length, enlargedSizes.Length);
            for (var index = 0; index < originalSizes.Length; index++) {
                if (fixture is "wardley-basic.mmd" or "ishikawa-basic.mmd") Assert.InRange(enlargedSizes[index], 1, originalSizes[index] * 2 + .001);
                // Packet captions fit their measured cell bounds. Different installed
                // faces can reach that cap before the requested size doubles.
                else if (fixture is "gitgraph-basic.mmd" or "packet-basic.mmd") Assert.InRange(enlargedSizes[index], originalSizes[index], originalSizes[index] * 2 + .001);
                else Assert.Equal(originalSizes[index] * 2, enlargedSizes[index], 3);
            }
        }
        Assert.True(enlargedSizes.Max() > originalSizes.Max());
        Assert.False(small.ToPng().SequenceEqual(large.ToPng()), "Changing body typography must change native raster ink without a heading or family change.");
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(large.ToHtmlPage(), 960, 720);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = Path.GetFileNameWithoutExtension(fixture);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-21-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-10.5-native.png"), small.ToPng());
        File.WriteAllBytes(Path.Combine(captures, name + "-21-native.png"), large.ToPng());
    }

    [Theory]
    [InlineData("large")]
    [InlineData("larger")]
    [InlineData("clamp(12px, 2vw, 24px)")]
    public void UnsupportedCssKeywordsAndFunctionsKeepRenderingAvailable(string size) {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontSize':'" + size + "'}}}%%\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Artifact);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, Assert.Single(result.Diagnostics).Code);
    }

    private static VisualArtifact Render(string body, double size) {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontSize':" + size.ToString(CultureInfo.InvariantCulture) + "}}}%%\n" + body);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error || diagnostic.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
        var artifact = result.Artifact!;
        switch (artifact.Model) {
            case PacketLayoutBlock item: item.WithTitle("").WithSubtitle(""); break;
            case BlockLayoutBlock item: item.WithTitle("").WithSubtitle(""); break;
            case GitGraphBlock item: item.WithTitle("").WithSubtitle(""); break;
            case WardleyMapBlock item: item.WithTitle("").WithSubtitle(""); break;
            case VennDiagramBlock item: item.WithTitle("").WithSubtitle(""); break;
            case FishboneDiagramBlock item: item.WithTitle("").WithSubtitle(""); break;
            default: throw new InvalidOperationException("Expected native block model.");
        }
        return artifact;
    }

    private static double[] BodyFontSizes(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => element.Name.LocalName == "text" && element.Attribute("font-size") != null &&
            (string?)element.Attribute("data-cfx-role") != "visual-title" && (string?)element.Attribute("data-cfx-role") != "visual-subtitle")
        .Select(element => double.Parse(element.Attribute("font-size")!.Value, CultureInfo.InvariantCulture)).ToArray();
}
