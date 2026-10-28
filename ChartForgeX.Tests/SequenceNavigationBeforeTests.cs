using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceNavigationBeforeTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void SafeMenuSettingsAreValidatedSourceFactsWithoutUnknownLoss(bool expanded) {
        var source="%%{init:{\"sequence\":{\"forceMenus\":"+(expanded?"true":"false")+"}}}%%\nsequenceDiagram\nparticipant A\nparticipant B\nlinks A: {\"Guide\":\"#guide\",\"Status\":\"#status\"}\nA->>B: Save";var result=MermaidRenderer.Render(source);Capture("sequence-navigation-before-"+expanded,source,result);Assert.False(result.HasErrors);Assert.Empty(result.Diagnostics);Assert.NotNull(result.Artifact);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OrdinaryStaticPrimaryHrefRemainsScriptFreeAndInert(bool dark) {
        var source="sequenceDiagram\nparticipant A\nparticipant B\nlink A: Guide @ #guide\nA->>B: Save";var result=MermaidRenderer.Render(source,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light}});Capture("sequence-navigation-before-ordinary-"+dark,source,result);Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact ?? throw new InvalidOperationException("The primary-link source did not produce an artifact.");
        Assert.Contains("#guide", artifact.ToSvg());
        Assert.DoesNotContain("<script", artifact.ToSvg());
        Assert.DoesNotContain("<details", artifact.ToHtmlPage());
    }
    [Theory] [InlineData("javascript:alert(1)")] [InlineData("data:text/html,unsafe")]
    public void UnsafeSourceNavigationKeepsLocatedWarningAndNeverCreatesAnAnchor(string href) {
        var source="sequenceDiagram\nparticipant A\nparticipant B\nlink A: Guide @ "+href+"\nA->>B: Save";var result=MermaidRenderer.Render(source);Capture("sequence-navigation-before-unsafe-"+href.Substring(0,4),source,result);Assert.False(result.HasErrors);Assert.Contains(result.Diagnostics,item=>item.Span.Line==4);Assert.Null(Assert.IsType<SequenceArtifact>(result.Artifact!.Model).Participants[0].Href);
    }
    private static void Capture(string stem,string source,MermaidRenderResult result){var output=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".mmd"),source);File.WriteAllText(Path.Combine(output,stem+".diagnostics.json"),JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Span.Line,item.Span.Column})));if(result.Artifact is {} artifact){File.WriteAllText(Path.Combine(output,stem+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),artifact.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),artifact.ToInterchangeJson());}}
}
