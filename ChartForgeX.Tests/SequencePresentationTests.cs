using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SequencePresentationTests {
    private const string Body = "sequenceDiagram\nparticipant API\nparticipant Store\nAPI->>Store: Save request\nNote right of Store: Stored record\nStore-->>API: Complete";

    [Fact]
    public void SharedVersionFixturePreservesParticipantMessageAndPresentationFacts() {
        var root = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures");
        using var expected = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "sequence-presentation.expected.json")));
        var rendered = MermaidRenderer.Render(File.ReadAllText(Path.Combine(root, "sequence-presentation.mmd")));
        Assert.Empty(rendered.Diagnostics);
        var model = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        Assert.Equal(expected.RootElement.GetProperty("participants").EnumerateArray().Select(item => item.GetString()), model.Participants.Select(item => item.Id));
        Assert.Equal(expected.RootElement.GetProperty("messages").EnumerateArray().Select(item => string.Join("|", item.EnumerateArray().Select(value => value.GetString()))),
            model.Messages.Select(item => string.Join("|", item.SourceId, item.TargetId, item.Text)));
        var configuration = expected.RootElement.GetProperty("configuration");
        Assert.Equal(configuration.GetProperty("theme").GetString(), rendered.Document!.Configuration.Theme);
        Assert.Equal(configuration.GetProperty("fontFamily").GetString(), model.Theme.Typography.Family);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void AuthoredSequencePresentationReachesEveryConvenienceExportAndPortableEnvelope(VisualThemeMode mode) {
        var model = SequenceArtifact.Create("presentation").AddParticipant("a").AddParticipant("b").AddMessage("a", "b", "Request");
        model.Theme = VisualTheme.Graphite().WithTypography(new VisualTypography("Georgia, serif", dataLabelSize: 18));
        model.ThemeMode = mode;
        var artifact = model.ToVisualArtifact();
        var expected = model.Theme.Resolve(mode).Background;
        var svg = model.ToSvg();
        Assert.Equal(expected.ToCss(), Background(svg));
        Assert.Contains("Georgia", svg);
        Assert.Contains("font-size=\"18\"", svg);
        Assert.Equal(svg, artifact.ToSvg());
        var pixels = RasterImageDecoder.Decode(model.ToPng()).Pixels;
        Assert.Equal(new[] { expected.R, expected.G, expected.B, expected.A }, pixels.Take(4));
        Assert.Contains("Georgia", artifact.ToHtmlPage());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(expected.ToCss(), envelope.Presentation!.Theme!.Background);
        Assert.Equal("Georgia, serif", envelope.Presentation.Theme.FontFamily);
        AssertStatusColors(model.Theme.Resolve(mode), envelope.Presentation.Theme);
        Assert.Equal(artifact.ToInterchangeJson(), envelope.ToJson());
    }

    [Fact]
    public void ExplicitHostContextOverridesAuthoredDefaultsAndDetachesTheResolvedPresentation() {
        var model = SequenceArtifact.Create("host").AddParticipant("a").AddParticipant("b").AddMessage("a", "b", "Request");
        model.Theme = VisualTheme.Graphite().WithTypography(new VisualTypography("Georgia, serif"));
        model.ThemeMode = VisualThemeMode.Dark;
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)),
            VisualTheme.Graphite(), VisualThemeMode.Light, font: new FontSpec { Family = "Courier New, monospace" });
        var prepared = model.Prepare(context);
        var artifact = prepared.ToArtifact("host", VisualArtifactKind.Sequence);
        var originalJson = artifact.ToInterchangeJson();
        var originalSvg = prepared.ToSvg();
        var theme = artifact.ToInterchangeEnvelope().Presentation!.Theme!;
        Assert.Equal(context.Theme.Resolve(context.ThemeMode).Background.ToCss(), Background(originalSvg));
        Assert.Equal(context.Theme.Resolve(context.ThemeMode).Background.ToCss(), theme.Background);
        Assert.Equal("Courier New, monospace", theme.FontFamily);
        AssertStatusColors(context.Theme.Resolve(context.ThemeMode), theme);
        Assert.Contains("Courier New", originalSvg);
        model.ThemeMode = VisualThemeMode.Light;
        model.Theme = VisualTheme.Graphite();
        theme.Background = "#000000";
        Assert.Equal(originalJson, artifact.ToInterchangeJson());
        Assert.Equal(originalSvg, prepared.ToSvg());
    }

    [Theory]
    [InlineData("dark", VisualThemeMode.Dark)]
    [InlineData("default", VisualThemeMode.Light)]
    public void SourceSettingsReachDirectRendererTypedConversionAndMarkdownFences(string theme, VisualThemeMode mode) {
        var source = Source(theme);
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidSequenceDocument>(rendered.Document);
        var model = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        Assert.Equal(mode, model.ThemeMode);
        Assert.Equal("Georgia, serif", model.Theme.Typography.Family);
        Assert.Equal(rendered.Artifact.ToSvg(), document.ToSequenceArtifact().ToSvg());
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fencedModel = Assert.IsType<SequenceArtifact>(Assert.Single(fenced.Artifacts).Model);
        Assert.Equal(mode, fencedModel.ThemeMode);
        Assert.Equal(model.Theme.Typography.Family, fencedModel.Theme.Typography.Family);
        Assert.Equal(Background(rendered.Artifact.ToSvg()), Background(fencedModel.ToSvg()));
        Assert.Equal(model.Theme.Resolve(mode).Background.ToCss(), rendered.Artifact.ToInterchangeEnvelope().Presentation!.Theme!.Background);
    }

    [Theory]
    [InlineData("dark", 320)]
    [InlineData("default", 960)]
    public async Task SourceSequencePresentationIsObservedInCompactAndWideStaticHosts(string theme, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var rendered = MermaidRenderer.Render(Source(theme));
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 620);
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role=\"sequence-participant\"]").CountAsync());
        Assert.Contains("Georgia", await session.Page.Locator("svg text").First.EvaluateAsync<string>("element => getComputedStyle(element).fontFamily"));
        Assert.Contains("Stored record", await session.Page.Locator("body").InnerTextAsync());
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "sequence-presentation-" + theme + "-" + width + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, "sequence-presentation-" + theme + "-native.png"), artifact.ToPng());
        }
    }

    private static string Source(string theme) => "---\nconfig:\n  theme: default\n  fontFamily: Arial, sans-serif\n  sequence:\n    theme: " + theme + "\n    fontFamily: Georgia, serif\n---\n" + Body;

    private static void AssertStatusColors(VisualThemeColors colors, VisualArtifactInterchangeTheme theme) {
        var status = colors.Status;
        Assert.Equal(status.Pass.Fill.ToCss(), theme.Healthy);
        Assert.Equal(status.Medium.Fill.ToCss(), theme.Warning);
        Assert.Equal(status.Critical.Fill.ToCss(), theme.Critical);
        Assert.Equal(status.Neutral.Fill.ToCss(), theme.Unknown);
        Assert.Equal(status.Maintenance.Fill.ToCss(), theme.Disabled);
    }

    private static string Background(string svg) => (string)XDocument.Parse(svg).Descendants()
        .Single(element => (string?)element.Attribute("data-cfx-role") == "background").Attribute("fill")!;
}
