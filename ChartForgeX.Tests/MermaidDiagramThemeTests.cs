using System.Text.RegularExpressions;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidDiagramThemeTests {
    private const string Flowchart = "flowchart LR\na[Start] --> b[Done]";
    private const string Sequence = "sequenceDiagram\nAlice->>Bob: Done";

    [Theory]
    [InlineData(Flowchart, "default", "")]
    [InlineData(Flowchart, "dark", "dark")]
    [InlineData(Sequence, "default", "")]
    [InlineData(Sequence, "dark", "dark")]
    public void MermaidDefaultAndDarkThemesRenderTheNativeTheme(string source, string theme, string mode) {
        var named = Render("%%{init: {'theme':'" + theme + "'}}%%\n" + source);
        var native = mode.Length == 0 ? Render(source) : RenderDark(source);
        Assert.Equal(native.ToPng(), named.ToPng());
    }

    [Fact]
    public void NamedAlternativeThemeStaysAnExplicitOptIn() {
        var forest = Render("%%{init: {'theme':'forest'}}%%\n" + Flowchart).ToSvg();
        Assert.Contains("#CDE498", forest, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#CDE498", Render(Flowchart).ToSvg(), System.StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(Flowchart)]
    [InlineData(Sequence)]
    [InlineData("gitGraph\ncommit id: \"init\"")]
    public void DiagramCanvasUsesTheChartCardSurface(string source) {
        Assert.Equal("#FFFFFF", Canvas(Render(source).ToSvg()));
        Assert.Equal("#16181C", Canvas(RenderDark(source).ToSvg()));
    }

    [Fact]
    public void ContentSizedFamiliesTrimToWhatTheyDrawUnlessTheHostSetsASize() {
        const string packet = "packet-beta\n0-15: \"Source Port\"\n16-31: \"Destination Port\"\n32-63: \"Sequence Number\"";
        var trimmed = Render(packet);
        var fields = Regex.Matches(trimmed.ToSvg(), "data-cfx-role=\"packet-field\"[^>]*\\by=\"([\\d.]+)\"[^>]*\\bheight=\"([\\d.]+)\"");
        Assert.NotEmpty(fields);
        var lastRowBottom = fields.Cast<Match>().Max(match => double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)
            + double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.InRange(trimmed.NaturalSize!.Value.Height - lastRowBottom, 29, 34);
        var fixedFrame = MermaidRenderer.Render(packet, new MermaidRenderOptions { Packet = new MermaidPacketRenderOptions { Height = 420 } }).Artifact!;
        Assert.Equal(420, fixedFrame.NaturalSize!.Value.Height);

        const string tree = "treeView-beta\n\"src\"\n    \"a.cs\"";
        Assert.True(Render(tree).NaturalSize!.Value.Width < 300, "A short tree trims its empty right margin.");
        var wide = MermaidRenderer.Render(tree, new MermaidRenderOptions { TreeView = new MermaidTreeViewRenderOptions { Width = 720 } }).Artifact!;
        Assert.Equal(720, wide.NaturalSize!.Value.Width);
    }

    private static VisualArtifact Render(string source) => MermaidRenderer.Render(source).Artifact!;

    private static VisualArtifact RenderDark(string source) {
        var document = new MermaidParser().Parse(source).Document!;
        document.Theme = "dark";
        return document.ToVisualArtifact();
    }

    private static string Canvas(string svg) {
        var background = Regex.Match(svg, "data-cfx-role=\"(?:background|visual-background)\"[^>]*fill=\"(#[0-9A-Fa-f]{6})\"");
        Assert.True(background.Success, "The diagram paints no canvas.");
        return background.Groups[1].Value.ToUpperInvariant();
    }
}
