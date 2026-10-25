using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidSequenceLayoutTypographyCausalTests {
    public static IEnumerable<object[]> Settings() {
        yield return new object[] { "actor-spacing", "{\"actorMargin\":64}" };
        yield return new object[] { "message-spacing", "{\"messageMargin\":48}" };
        yield return new object[] { "note-spacing", "{\"noteMargin\":24}" };
        yield return new object[] { "canvas-insets", "{\"diagramMarginX\":36,\"diagramMarginY\":28}" };
        yield return new object[] { "actor-font", "{\"actorFontSize\":18,\"actorFontWeight\":700}" };
        yield return new object[] { "message-font", "{\"messageFontSize\":\"20px\",\"messageFontWeight\":\"bold\"}" };
        yield return new object[] { "note-font", "{\"noteFontSize\":16,\"noteFontWeight\":400}" };
        yield return new object[] { "no-wrap", "{\"wrap\":false,\"wrapPadding\":12}" };
        yield return new object[] { "activation-width", "{\"activationWidth\":14}" };
    }
    [Theory]
    [MemberData(nameof(Settings))]
    public void SupportedNativeLayoutAndFontSettingsReachTheSequenceOwner(string id, string settings) {
        var source = "%%{init: {\"sequence\":" + settings + "}}%%\n" + Body;
        var result = MermaidRenderer.Render(source);
        var artifact = result.Artifact;
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "sequence-settings-causal-" + id);
            File.WriteAllText(stem + ".mmd", source);
            File.WriteAllText(stem + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message, Line = item.Span.Line, Column = item.Span.Column })));
            if (artifact != null) { File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".png", artifact.ToPng()); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson()); }
        }
        Assert.False(result.HasErrors); Assert.NotNull(artifact); Assert.Empty(result.Diagnostics);
    }
    private const string Body = "sequenceDiagram\nparticipant A as Review service\nparticipant B as Storage\nA->>B: Review a longer authored request\nactivate B\nNote over B: Retain native typography\nB-->>A: Ready\ndeactivate B";
}
