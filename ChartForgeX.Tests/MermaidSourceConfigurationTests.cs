using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Core;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSourceConfigurationTests {
    [Theory]
    [InlineData("flowchart LR\nA --> B")]
    [InlineData("pie\n\"Success\" : 8\n\"Failure\" : 2")]
    [InlineData("packet-beta\n0-7: \"Header\"\n8-15: \"Payload\"")]
    public void SourceFontsReachTheExistingNativeModelOwner(string body) {
        var rendered = MermaidRenderer.Render("%%{init: {'fontFamily': 'Arial, sans-serif'}}%%\n" + body);
        Assert.Empty(rendered.Diagnostics);
        var model = rendered.Artifact!.Model;
        var family = model is TopologyChart topology ? topology.Theme!.FontFamily
            : model is Chart chart ? chart.Options.Theme.FontFamily : Assert.IsAssignableFrom<IVisualBlock>(model).Options.Theme.FontFamily;
        Assert.Equal("Arial, sans-serif", family);
        Assert.Contains("Arial", rendered.Artifact.ToSvg());
        Assert.NotEmpty(rendered.Artifact.ToPng());
    }

    [Theory]
    [InlineData("dark", 320)]
    [InlineData("default", 960)]
    public async Task ConfiguredSourceRemainsReadableInStaticHosts(string theme, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = "---\nconfig:\n  theme: " + theme + "\n  fontFamily: Arial, sans-serif\n---\nflowchart LR\nA[API] --> B[Database]";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 460);
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role=\"topology-node\"]").CountAsync());
        Assert.Contains("API", await session.Page.Locator("body").InnerTextAsync());
        Assert.Contains("Database", await session.Page.Locator("body").InnerTextAsync());
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(captures, "configuration-" + theme + "-" + width + ".png"), FullPage = true
            });
            await File.WriteAllBytesAsync(Path.Combine(captures, "configuration-" + theme + ".png"), artifact.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Fact]
    public void DirectivesMergeAfterFrontmatterAndFamilyValuesWinOverGlobalValues() {
        const string source = "---\nconfig:\n  theme: dark\n  flowchart:\n    theme: default\n    look: classic\n---\n%%{init: {'theme': 'default', 'flowchart': {'theme': 'dark'}}}%%\n%%{initialize: {\n  \"theme\": \"default\",\n  \"fontFamily\": \"Arial, sans-serif\"\n}}%%\nflowchart LR\nA --> B";
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.False(parsed.HasErrors);
        var document = parsed.Document!;
        Assert.Equal("dark", document.Configuration.Theme);
        Assert.Equal("classic", document.Configuration.Look);
        Assert.Equal("Arial, sans-serif", document.Configuration.FontFamily);
        Assert.Equal(2, document.Directives.Count);
        Assert.Equal(14, document.Nodes[0].Span.Line);
        Assert.Contains(document.Configuration.Settings, setting => setting.Path == "flowchart.look" && setting.Span.Line == 6);
        Assert.Contains(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Span.Line == 6);
        var artifact = ((MermaidDocument)document).ToVisualArtifact();
        var topology = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal("#0B1120", topology.Theme!.Background);
        Assert.Equal("Arial, sans-serif", topology.Theme.FontFamily);
        Assert.Equal("classic", artifact.Metadata["mermaid.config.look"]);
        Assert.Contains("#0B1120", artifact.ToSvg());
        Assert.NotEmpty(artifact.ToPng());
    }

    [Fact]
    public void BodyDirectivesAreBlankedWithoutMovingFollowingNodeSpans() {
        const string source = "flowchart LR\nA --> B\n%%{initialize: {\n  'theme': 'dark'\n}}%%\nB --> C";
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", "C" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal(6, parsed.Document.Nodes[2].Span.Line);
        Assert.Equal("dark", parsed.Document.Theme);
        Assert.Equal(source, parsed.Document.SourceText);
        Assert.Equal(3, Assert.Single(parsed.Document.Directives).Span.Line);
    }

    [Fact]
    public void YamlQuotesCommentsAndUnrelatedMetadataDoNotChangeConfiguration() {
        const string source = "---\nmetadata:\n  config:\n    theme: dark\nconfig:\n  theme: 'default' # printable\n  fontFamily: 'Arial, ''Quoted'' # family'\n---\nflowchart LR\nA --> B";
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal("default", parsed.Document!.Theme);
        Assert.Equal("Arial, 'Quoted' # family", parsed.Document.Configuration.FontFamily);
        Assert.Equal(2, parsed.Document.Configuration.Settings.Count);
    }

    [Fact]
    public void UnsupportedYamlBlocksStayOpaqueToThemeExtraction() {
        const string source = "---\nconfig:\n  themeVariables: |\n    theme: dark\n  layout: elk\n---\nflowchart LR\nA --> B";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        Assert.Null(result.Document!.Theme);
        Assert.DoesNotContain(result.Document.Configuration.Settings, item => item.Path == "theme");
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Span.Line == 3);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedLayout && item.Span.Line == 5);
        Assert.NotNull(result.Artifact);
    }

    [Theory]
    [InlineData("%%{init: {'theme': 'dark', 'theme': 'default'}}%%\nflowchart LR\nA --> B")]
    [InlineData("%%{init: {'theme': true}}%%\nflowchart LR\nA --> B")]
    [InlineData("%%{init: {'theme': 'dark'}}\nflowchart LR\nA --> B")]
    [InlineData("---\nconfig:\n  theme: dark\n  theme: default\n---\nflowchart LR\nA --> B")]
    [InlineData("---\nconfig:\n  theme: dark\n    fontFamily: Arial\n---\nflowchart LR\nA --> B")]
    [InlineData("---\nconfig:\n  theme:\n---\nflowchart LR\nA --> B")]
    public void InvalidSupportedConfigurationDoesNotProduceAnArtifact(string source) {
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line > 0);
    }

    [Theory]
    [InlineData("length")]
    [InlineData("keys")]
    [InlineData("depth")]
    public void SourceConfigurationHasEnforcedResourceBounds(string limit) {
        var payload = limit == "length" ? "{'unknown':'" + new string('x', 65536) + "'}"
            : limit == "keys" ? "{" + string.Join(",", Enumerable.Range(0, 257).Select(index => "'k" + index + "': 1")) + "}"
            : string.Concat(Enumerable.Repeat("{'nested':", 9)) + "1" + new string('}', 9);
        var result = MermaidRenderer.Render("%%{init: " + payload + "}%%\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 1);
    }

    [Fact]
    public void UnknownAndInactiveSettingsAreRetainedWithStableDiagnostics() {
        const string source = "%%{init: {'sequence': {'theme': 'dark'}, 'theme': 'forest', 'look': 'handDrawn', 'flowchart': {'defaultRenderer': 'elk'}, 'htmlLabels': true}}%%\nflowchart LR\nA --> B";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        Assert.Equal("forest", result.Document!.Configuration.Theme);
        Assert.Equal(5, result.Document.Configuration.Settings.Count);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedLayout && item.Message.Contains("defaultRenderer"));
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Message.Contains("sequence.theme"));
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Message.Contains("forest"));
        Assert.Equal("#FFFFFF", Assert.IsType<TopologyChart>(result.Artifact!.Model).Theme!.Background);
    }
}
