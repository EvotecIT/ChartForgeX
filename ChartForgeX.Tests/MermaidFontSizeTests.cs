using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFontSizeTests {
    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    public void AcceptedPixelLimitsKeepTopologyLabelTextAvailable(int size) {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontSize':" + size.ToString(CultureInfo.InvariantCulture) + "}}}%%\nflowchart LR\nA[API] --> B[Store]");
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var svg = XDocument.Parse(artifact.ToSvg());
        var labels = svg.Descendants().Where(element => (string?)element.Parent?.Attribute("data-cfx-role") == "topology-node-label").ToArray();
        Assert.Equal(2, labels.Length);
        Assert.Contains(labels, label => label.Value == "API");
        Assert.Contains(labels, label => label.Value == "Store");
        Assert.NotEmpty(artifact.ToPng());
    }

    [Theory]
    [InlineData("flowchart LR\nA[API] --> B[Store]")]
    [InlineData("sequenceDiagram\nAPI->>Store: Save")]
    [InlineData("pie\n\"Saved\": 8\n\"Failed\": 2")]
    [InlineData("packet-beta\n0-7: \"Header\"\n8-15: \"Payload\"")]
    public void SourcePixelSizeReachesNativeOwnersMarkdownAndPortableOutput(string body) {
        var source = Source(body);
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(18.5, result.Document!.Configuration.FontSize);
        var artifact = result.Artifact!;
        Assert.Equal(18.5, FontSize(artifact.Model!));
        Assert.Equal("18.5", artifact.Metadata["mermaid.config.fontSize"]);
        Assert.NotEmpty(artifact.ToPng());
        Assert.False(artifact.ToPng().SequenceEqual(MermaidRenderer.Render(body).Artifact!.ToPng()));
        Assert.Equal("18.5", VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).Extensions["mermaid.config.fontSize"]);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
    }

    [Theory]
    [InlineData("18.5")]
    [InlineData("'18.5'")]
    [InlineData("'18.5px'")]
    public void FrontmatterAndDirectivesKeepPixelValuesAndSourceOrder(string value) {
        var parsed = new MermaidParser().Parse("---\nconfig:\n  themeVariables:\n    fontSize: 14\n---\n%%{init: {'themeVariables':{'fontSize':" + value + "}}}%%\nflowchart LR\nA --> B");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(18.5, parsed.Document!.Configuration.FontSize);
        Assert.Equal(2, parsed.Document.Configuration.Settings.Count(item => item.Path == "themeVariables.fontSize"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-2")]
    [InlineData("257")]
    [InlineData("false")]
    [InlineData("null")]
    [InlineData("''")]
    [InlineData("'garbage'")]
    [InlineData("{'nested':18}")]
    public void InvalidEffectiveSizesProduceLocatedErrors(string value) {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontSize':" + value + "}}}%%\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 1);
    }

    [Theory]
    [InlineData("1.2em")]
    [InlineData("18pt")]
    [InlineData("var(--label-size)")]
    public void UnsupportedCssSizingRetainsItsDeclarationAndWarning(string value) {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontSize':'" + value + "'}}}%%\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.Null(result.Document!.Configuration.FontSize);
        Assert.Equal(value, Assert.Single(result.Document.Configuration.Settings).Value);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void ExplicitNativeContextOverridesAuthoredTypographyWithoutMutatingIt() {
        var topology = Assert.IsType<TopologyChart>(MermaidRenderer.Render(Source("flowchart LR\nA[API] --> B[Store]")).Artifact!.Model);
        topology.Theme!.FontFamily = "Arial";
        Assert.Equal(18.5, topology.Theme.Typography.DataLabelSize);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 360)),
            VisualTheme.Graphite().WithTypography(new VisualTypography("Georgia", dataLabelSize: 14)), frame: new VisualFrame(showLegend: false));
        var prepared = topology.Prepare(context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(item => item.Role == "topology-node-label").ToArray();
        Assert.Equal(2, labels.Length);
        Assert.All(labels, item => Assert.Equal(14, item.Text.Size));
        Assert.Equal(18.5, topology.Theme.Typography.DataLabelSize);
        Assert.NotEmpty(prepared.ToPng());
        var native = XDocument.Parse(topology.ToSvg());
        Assert.Contains(native.Descendants(), element => (string?)element.Parent?.Attribute("data-cfx-role") == "topology-node-label" && (string?)element.Attribute("font-size") == "18.5");
    }

    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task ExplicitFontSizeIsVisibleInBrowserAndNativeOutput(bool dark, int width) {
        var artifact = MermaidRenderer.Render(Source("flowchart LR\nA[API] --> B[Store]", dark)).Artifact!;
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 460);
        Assert.Equal("18.5px", await session.Page.Locator("[data-cfx-role='topology-node-label'] text").First.EvaluateAsync<string>("element => getComputedStyle(element).fontSize"));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "source-font-size-" + (dark ? "dark-" : "light-") + width.ToString(CultureInfo.InvariantCulture);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), artifact.ToPng());
    }

    private static string Source(string body, bool dark = false) => "%%{init: {'theme':'" + (dark ? "dark" : "default") + "','themeVariables':{'fontSize':'18.5px','fontFamily':'Georgia, serif'}}}%%\n" + body;
    private static double FontSize(object model) => model switch {
        TopologyChart topology => topology.Theme!.Typography.DataLabelSize,
        SequenceArtifact sequence => sequence.Theme.Typography.DataLabelSize,
        Chart chart => chart.Options.Theme.DataLabelFontSize,
        IVisualBlock block => block.Options.Theme.DataLabelFontSize,
        _ => throw new InvalidOperationException("Unexpected native family.")
    };
}
