using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidMarkupDefaultThemeTests {
    [Theory]
    [InlineData("pie\n\"Done\" : 7\n\"Open\" : 3", VisualThemeMode.Light)]
    [InlineData("pie\n\"Done\" : 7\n\"Open\" : 3", VisualThemeMode.Dark)]
    [InlineData("flowchart LR\na[Start] --> b[Done]", VisualThemeMode.Dark)]
    [InlineData("sequenceDiagram\nAlice->>Bob: Done", VisualThemeMode.Dark)]
    [InlineData("packet-beta\n0-3: \"Flags\"\n4-7: \"Payload\"", VisualThemeMode.Dark)]
    [InlineData("railroad-abnf-beta\nvalue = %s\"Exact\" ;", VisualThemeMode.Dark)]
    public void HostFallbackMatchesDirectSourceThemeBeforeAppearanceCapture(string source, VisualThemeMode mode) {
        var document = new MermaidParser().Parse(source).Document!;
        if (mode == VisualThemeMode.Dark) document.Theme = "dark";
        var expected = document.ToVisualArtifact();
        var result = new MermaidVisualMarkupParser(new MermaidRenderOptions(), mode).Parse("```mermaid\n" + source + "\n```");
        Assert.False(result.HasErrors);
        var actual = Assert.Single(result.Artifacts);
        Assert.Equal(Normalize(expected.ToSvg()), Normalize(actual.ToSvg()));
        Assert.Equal(expected.ToPng(), actual.ToPng());
        Assert.False(actual.Metadata.ContainsKey("mermaid.config.theme"));
    }

    [Theory]
    [InlineData("default", VisualThemeMode.Dark)]
    [InlineData("dark", VisualThemeMode.Light)]
    public void AuthoredPaletteOverridesFallbackAndKeepsSourceOwnership(string theme, VisualThemeMode fallback) {
        var source = "---\nconfig:\n  theme: " + theme + "\n---\npie\n\"Done\" : 7";
        var expected = MermaidRenderer.Render(source).Artifact!;
        var result = new MermaidVisualMarkupParser(new MermaidRenderOptions(), fallback).Parse("```mermaid\n" + source + "\n```");
        Assert.False(result.HasErrors);
        var actual = Assert.Single(result.Artifacts);
        Assert.Equal(theme, actual.Metadata["mermaid.config.theme"]);
        Assert.Equal(Normalize(expected.ToSvg()), Normalize(actual.ToSvg()));
        Assert.Equal(expected.ToPng(), actual.ToPng());
    }

    [Fact]
    public void MixedFencesRetainIndependentOwnershipAndLocatedRejections() {
        const string source = "```mermaid\n---\nconfig:\n  theme: default\n---\npie\n\"First\" : 1\n```\n\n```mermaid\npie\n\"Second\" : 1\n```";
        var parser = new MermaidVisualMarkupParser(new MermaidRenderOptions(), VisualThemeMode.Dark);
        var result = parser.Parse(source);
        Assert.False(result.HasErrors); Assert.Equal(2, result.Artifacts.Count);
        Assert.Equal("default", result.Artifacts[0].Metadata["mermaid.config.theme"]);
        Assert.False(result.Artifacts[1].Metadata.ContainsKey("mermaid.config.theme"));
        var invalid = parser.Parse("intro\n\n```mermaid\nrailroad-abnf-beta\nvalue = %s 'x' ;\n```");
        Assert.True(invalid.HasErrors); Assert.Empty(invalid.Artifacts);
        Assert.Contains(invalid.Diagnostics, d => d.Code == "CFXM008" && d.Line == 5);
    }

    [Fact]
    public void InvalidFallbackModesRejectAtBothPublicEntrypoints() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MermaidVisualMarkupParser(new MermaidRenderOptions(), (VisualThemeMode)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MermaidVisualMarkupBlockParser(new MermaidRenderOptions(), (VisualThemeMode)99));
    }

    private static string Normalize(string svg) {
        var id = (string?)XDocument.Parse(svg).Root?.Attribute("id");
        if (!string.IsNullOrEmpty(id)) svg = svg.Replace(id, "theme-proof", StringComparison.Ordinal);
        return Regex.Replace(svg, "cfx-pg-[0-9]+", "theme-proof");
    }
}
