using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidLateTypographyClosureTests {
    [Theory]
    [InlineData(32)]
    [InlineData(256)]
    public async Task ClassCardsRetainBothMemberRowsAfterFittingTheirTitle(int size) {
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':" + size + "}}}%%\nclassDiagram\nclass Service {\n +int Count\n +Read()\n}");
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var svg = artifact.ToSvg();
        var rows = XDocument.Parse(svg).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "topology-node-detail").Select(node => node.Value).ToArray();
        Assert.Contains(rows, text => text.Contains("Count", StringComparison.Ordinal));
        Assert.Contains(rows, text => text.Contains("Read()", StringComparison.Ordinal));
        var png = artifact.ToPng();
        Assert.NotEmpty(png);
        await Capture(svg, png, "class-members-font-" + size, 960, 720);
    }

    [Theory]
    [InlineData(18, false)]
    [InlineData(18, true)]
    [InlineData(64, false)]
    [InlineData(64, true)]
    public async Task DenseVennRegionsRetainEveryAcceptedTextNode(int count, bool graphite) {
        var theme = ChartTheme.Light(); theme.FontFamily = "Arial"; theme.UseGraphiteLayout = graphite;
        var block = VennDiagramBlock.Create().WithSize(800, 600).WithTheme(theme).WithCard(false)
            .AddSet("A", "A", 60).AddSet("B", "B", 55);
        var expected = Enumerable.Range(1, count).Select(index => "N" + index).ToArray();
        foreach (var text in expected) block.AddText(new[] { "A" }, text, text);
        var svg = block.ToSvg();
        var labels = XDocument.Parse(svg).Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value).ToArray();
        foreach (var text in expected) Assert.Contains(text, labels);
        var png = block.ToPng(); Assert.NotEmpty(png);
        await Capture(svg, png, "venn-dense-" + count + "-" + graphite, 800, 600);
    }

    private static async Task Capture(string svg, byte[] png, string name, int width, int height) {
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, height);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
    }
}
