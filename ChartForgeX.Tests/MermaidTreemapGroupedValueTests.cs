using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidTreemapGroupedValueTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(320)]
    [InlineData(960)]
    public async Task GroupedMagnitudesRemainVisibleInCompactAndWideStaticHosts(int width) {
        var source = "treemap-beta\n\"Storage\"\n  \"Documents\": 1,234.5\n  \"Media\": 765.5\n  \"Archive\"\n    \"Documents\": 240\n    \"Media\": 160\n";
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var svg = XDocument.Parse(artifact.ToSvg());
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "treemap-group-label" && element.Value.Contains("Storage", StringComparison.Ordinal));
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "treemap-label" && element.Value.Contains("Documents", StringComparison.Ordinal));
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 520);
        Assert.Equal("1234.5", await session.Page.Locator("[data-cfx-target-id=node-1]").GetAttributeAsync("data-cfx-value"));
        Assert.True(await session.Page.Locator("[data-cfx-target-id=node-1]").IsVisibleAsync());
        Assert.Contains("Documents", await session.Page.Locator("[data-cfx-target-id=node-1] [data-cfx-role=treemap-label]").TextContentAsync());
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "treemap-values-browser-" + width + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, "treemap-values-native.png"), artifact.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("1,234.5", 1234.5)]
    [InlineData("1,23.5", 123.5)]
    [InlineData("1,,234", 1234)]
    [InlineData("0,000", 0)]
    public void CommaSeparatedValuesRetainReferenceMagnitudeAndHierarchyInDirectAndFencedOutput(string spelling, double value) {
        var source = "treemap-beta\n\"Root\"\n  \"Measured\": " + spelling + "\n  \"Measured\": 1\n";
        var culture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var parsed = new MermaidParser().ParseTreemap(source);
            Assert.Empty(parsed.Diagnostics);
            Assert.Equal(value, parsed.Document!.Nodes[1].Value);
            var artifact = parsed.Document!.ToVisualArtifact();
            var chart = Assert.IsType<Chart>(artifact.Model);
            Assert.Equal(value, chart.Series[0].HierarchyItems[1].Value);
            Assert.Equal("node-0", chart.Series[0].HierarchyItems[1].ParentId);
            var svg = XDocument.Parse(artifact.ToSvg());
            var mark = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-target-id") == "node-1");
            Assert.Equal(value.ToString(CultureInfo.InvariantCulture), (string?)mark.Attribute("data-cfx-value"));
            var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
            Assert.Empty(fenced.Diagnostics);
            Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
            Assert.Equal(artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
        } finally { CultureInfo.CurrentCulture = culture; }
    }
}
