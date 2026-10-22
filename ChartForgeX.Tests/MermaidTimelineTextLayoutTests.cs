using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidTimelineTextLayoutTests {
    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void CompactSectionWordsStayWholeWithoutChangingTheAuthoredFrame(bool dark, int width) {
        var options = Options(dark, width);
        var rendered = MermaidRenderer.Render(MermaidJourneyTimelineNativeTests.Timeline, options);
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var block = Assert.IsType<TimelineDiagramBlock>(artifact.Model);
        Assert.Equal(width, block.Options.Size.Width); Assert.Equal(640, block.Options.Size.Height);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var xml = XDocument.Parse(svg);
        var sections = Roles(xml, "timeline-section"); Assert.Equal(3, sections.Length);
        Assert.All(sections, section => Assert.Equal(Caption(section), Assert.Single(Lines(section))));
        Assert.Equal(new[] { "Discovery", "Delivery", "Discovery" }, sections.Select(Caption));
        Assert.Equal(3, Roles(xml, "timeline-period-label").Length); Assert.Equal(5, Roles(xml, "timeline-event-label").Length);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + MermaidJourneyTimelineNativeTests.Timeline + "\n```");
        Assert.Empty(fence.Diagnostics); var fenced = Assert.Single(fence.Artifacts);
        Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
        Assert.Equal(svg, artifact.ToSvg()); Assert.Equal(png, artifact.ToPng());
        var image = RasterImageDecoder.Decode(png); Assert.Equal(width, image.Width); Assert.Equal(640, image.Height);
        Save("timeline-whole-section", dark, width, svg, png);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OversizedFixedFrameTokensUseExplicitEllipsisAndKeepTheirCompleteSemanticCaptions(bool dark) {
        var token = new string('W', 80);
        var source = "timeline TD\ntitle Bounded captions\nsection " + token + "\n" + token + ": " + token + "\n : First line<br>Second line";
        var rendered = MermaidRenderer.Render(source, Options(dark, 360)); Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var block = Assert.IsType<TimelineDiagramBlock>(artifact.Model);
        Assert.Equal(token, Assert.Single(block.Sections).Label);
        Assert.Equal(token, Assert.Single(block.Periods).Label);
        Assert.Equal(new[] { token, "First line\nSecond line" }, block.Periods[0].Events);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var xml = XDocument.Parse(svg);
        foreach (var role in new[] { "timeline-section", "timeline-period-label", "timeline-event-label" }) {
            var caption = Assert.Single(Roles(xml, role), node => Caption(node) == token);
            var line = Assert.Single(Lines(caption)); Assert.EndsWith("…", line);
            Assert.DoesNotContain(token, line, StringComparison.Ordinal);
            Assert.All(caption.Descendants().Where(node => node.Name.LocalName == "text"), node => Assert.True(Number(node, "font-size") >= 12));
        }
        var explicitBreak = Assert.Single(Roles(xml, "timeline-event-label"), node => Caption(node) == "First line\nSecond line");
        Assert.Equal(new[] { "First line", "Second line" }, Lines(explicitBreak));
        var image = RasterImageDecoder.Decode(png); Assert.Equal(360, image.Width); Assert.Equal(640, image.Height);
        Assert.Equal(svg, artifact.ToSvg()); Assert.Equal(png, artifact.ToPng());
        Save("timeline-constrained-token", dark, 360, svg, png);
    }

    private static MermaidRenderOptions Options(bool dark, int width) => new() {
        Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family },
        Timeline = new MermaidTimelineRenderOptions { Width = width, Height = 640 }
    };
    private static XElement[] Roles(XDocument xml, string role) => xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
    private static string Caption(XElement group) => group.Element(group.Name.Namespace + "title")!.Value;
    private static string[] Lines(XElement group) => group.Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value).ToArray();
    private static double Number(XElement node, string name) => double.Parse(node.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static void Save(string name, bool dark, int width, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, name + "-" + width + "-" + (dark ? "dark" : "light"));
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png);
    }
}
