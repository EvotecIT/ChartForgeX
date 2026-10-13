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
    [InlineData("0x12")]
    [InlineData("0o22")]
    [InlineData("0b10010")]
    public void TypedYamlFontNumbersUseTheSharedNumericConfigurationOwner(string value) {
        var rendered = MermaidRenderer.Render("---\nconfig:\n  themeVariables:\n    fontSize: " + value + "\n---\npacket-beta\n0-31: \"Header\"");
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(18, rendered.Document!.Configuration.FontSize);
        Assert.Equal("18", rendered.Artifact!.Metadata["mermaid.config.fontSize"]);
    }

    [Theory]
    [InlineData("participant A\nparticipant B\nparticipant C\nparticipant D\nD->>D: WWW")]
    [InlineData("participant A as i\nparticipant B as i\nparticipant C as i\nparticipant D as i\nparticipant E as i\nparticipant F as i\nparticipant G as i\nparticipant H as i\nautonumber\nA->>B: i")]
    public async Task LargestAcceptedSizeFitsLastLaneSelfMessagesAndGeneratedNumberText(string body) {
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\nsequenceDiagram\n" + body);
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        Assert.NotEmpty(artifact.ToSvg());
        Assert.NotEmpty(artifact.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), 960, 720);
        if (!body.Contains("autonumber", StringComparison.Ordinal))
            await session.Page.Locator(".chartforgex-visual-artifact").EvaluateAsync("element => element.scrollLeft = element.scrollWidth");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = body.Contains("autonumber", StringComparison.Ordinal) ? "sequence-font-256-autonumber" : "sequence-font-256-last-self";
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), artifact.ToPng());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    public async Task AcceptedPixelLimitsKeepSequenceParticipantsMessagesAndNotesRenderable(int size) {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontSize':" + size.ToString(CultureInfo.InvariantCulture)
            + "}}}%%\nsequenceDiagram\nparticipant API\nparticipant Store\nAPI->>Store: WWW\nNote right of Store: WWW\nAPI->>API: WWW");
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var svg = XDocument.Parse(artifact.ToSvg());
        Assert.Contains(svg.Descendants(), item => item.Name.LocalName == "text" && item.Value.Contains("A", StringComparison.Ordinal));
        var headings = svg.Descendants().Where(item => ((string?)item.Attribute("data-cfx-role"))?.StartsWith("frame-heading", StringComparison.Ordinal) == true)
            .SelectMany(item => item.Descendants().Where(child => child.Name.LocalName == "text")).ToArray();
        var participants = svg.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == "sequence-participant")
            .SelectMany(item => item.Descendants().Where(child => child.Name.LocalName == "rect")).ToArray();
        Assert.NotEmpty(participants);
        var participantTop = participants.Min(item => double.Parse(item.Attribute("y")!.Value, CultureInfo.InvariantCulture));
        Assert.All(headings, item => Assert.True(double.Parse(item.Attribute("y")!.Value, CultureInfo.InvariantCulture)
            + double.Parse(item.Attribute("font-size")!.Value, CultureInfo.InvariantCulture) * 0.3 <= participantTop));
        Assert.NotEmpty(artifact.ToPng());
        if (!InteractiveChartBrowser.Enabled || size != 256) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), 960, 720);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "sequence-font-256-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, "sequence-font-256-native.png"), artifact.ToPng());
    }

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
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, diagnostic.Code);
        Assert.Equal(1, diagnostic.Span.Line);
    }

    [Theory]
    [InlineData("1.2em")]
    [InlineData(".5em")]
    [InlineData("+.5rem")]
    [InlineData(".5vh")]
    [InlineData("5e-1em")]
    [InlineData("18pt")]
    [InlineData("1lh")]
    [InlineData("1rlh")]
    [InlineData("1dvh")]
    [InlineData("1Q")]
    [InlineData(".5rcap")]
    [InlineData("1svmin")]
    [InlineData("1cqi")]
    [InlineData("var(--label-size)")]
    [InlineData("math")]
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
