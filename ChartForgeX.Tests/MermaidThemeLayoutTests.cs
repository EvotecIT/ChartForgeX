using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidThemeLayoutTests {
    private const string Timeline = "timeline\n  title Rendering history\n  2024 : Fluent chart API : SVG and PNG output\n  2025 : Topology diagrams";
    private const string Packet = "packet-beta\n0-7: \"Header\"\n8-15: \"Payload\"";
    private static readonly HashSet<string> GeometryAttributes = new(StringComparer.Ordinal) {
        "x", "y", "x1", "x2", "y1", "y2", "cx", "cy", "r", "rx", "ry", "width", "height", "viewBox",
        "d", "points", "transform", "font-family", "font-size", "font-weight", "font-style", "text-anchor"
    };

    [Theory]
    [InlineData(false, "default", VisualThemeMode.Light)]
    [InlineData(false, "dark", VisualThemeMode.Dark)]
    [InlineData(false, "DARK", VisualThemeMode.Dark)]
    [InlineData(false, "unknown", VisualThemeMode.Light)]
    [InlineData(true, "default", VisualThemeMode.Light)]
    [InlineData(true, "dark", VisualThemeMode.Dark)]
    [InlineData(true, "DARK", VisualThemeMode.Dark)]
    [InlineData(true, "unknown", VisualThemeMode.Light)]
    public void SourceThemeKeepsNativePaletteAndEmittedLayoutWhileSelectingFiniteColorRoles(bool packet, string sourceTheme, VisualThemeMode expectedMode) {
        var body = packet ? Packet : Timeline;
        var baseline = Render(body);
        var rendered = Render("%%{init: {'theme':'" + sourceTheme + "'}}%%\n" + body, sourceTheme == "unknown");
        var (baselineTheme, _) = Presentation(baseline, packet);
        var (theme, mode) = Presentation(rendered, packet);
        var native = VisualTheme.Graphite().Resolve(expectedMode);
        // Literal named presets materialize their finite common color roles; legacy native-mode
        // aliases and unknown names retain their existing fallback palette and diagnostics.
        var named = sourceTheme is "default" or "dark";
        var background = named ? ChartForgeX.Primitives.ChartColor.FromHex(sourceTheme == "dark" ? "#333333" : "#FFFFFF") : native.Background;
        var foreground = named ? ChartForgeX.Primitives.ChartColor.FromHex(sourceTheme == "dark" ? "#E0DFDF" : "#131300") : native.Foreground;
        if (!named) Assert.Equal(baselineTheme.ToJson(), theme.ToJson());
        Assert.Equal(expectedMode, mode);
        Assert.Equal(background, theme.Resolve(mode).Background);
        Assert.Equal(foreground, theme.Resolve(mode).Foreground);
        Assert.Equal(native.Palette, theme.Resolve(mode).Palette);

        var baselineSvg = XDocument.Parse(baseline.ToSvg());
        var svg = XDocument.Parse(rendered.ToSvg());
        Assert.Equal(Geometry(baselineSvg), Geometry(svg));
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" &&
            element.Attribute("fill")?.Value == foreground.ToHex());
        if (!packet) Assert.Equal(named, svg.Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "visual-background"));
    }

    private static VisualArtifact Render(string source, bool unsupported = false) {
        var rendered = MermaidRenderer.Render(source);
        var appearanceDiagnostics = rendered.Diagnostics.Where(item => item.Code != MermaidDiagnosticCodes.ApproximateMapping).ToArray();
        if (unsupported) {
            var diagnostic = Assert.Single(appearanceDiagnostics);
            Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, diagnostic.Code);
            Assert.Equal(MermaidDiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Contains("'unknown'", diagnostic.Message);
        } else Assert.Empty(appearanceDiagnostics);
        Assert.False(rendered.HasErrors);
        return Assert.IsType<VisualArtifact>(rendered.Artifact);
    }

    private static (VisualTheme Theme, VisualThemeMode Mode) Presentation(VisualArtifact artifact, bool packet) {
        if (packet) {
            var block = Assert.IsType<PacketLayoutBlock>(artifact.Model);
            return (block.Options.Theme, block.Options.ThemeMode);
        }
        var timeline = Assert.IsType<TimelineDiagramBlock>(artifact.Model);
        return (timeline.Options.Theme, timeline.Options.ThemeMode);
    }

    // A requested source canvas adds one paint-only background; all content/frame/text geometry stays exact.
    private static string[] Geometry(XDocument svg) => svg.Root!.DescendantsAndSelf()
        .Where(element => (string?)element.Attribute("data-cfx-role") != "visual-background").Select(element =>
        element.Name.LocalName + "|" + string.Join("|", element.Attributes()
            .Where(attribute => GeometryAttributes.Contains(attribute.Name.LocalName))
            .OrderBy(attribute => attribute.Name.LocalName, StringComparer.Ordinal)
            .Select(attribute => attribute.Name.LocalName + "=" + attribute.Value)) +
        (element.Name.LocalName == "text" ? "|" + element.Value : "")).ToArray();
}
