using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class LetterSpacingContracts : IDisposable {
    private const string Family = "CFX Authored Tracking";
    public LetterSpacingContracts() => FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> {
        ['A'] = OpenTypeTestFonts.Box, ['B'] = OpenTypeTestFonts.Box, [0x0628] = OpenTypeTestFonts.Box, [0x062A] = OpenTypeTestFonts.Box, [0x05D0] = OpenTypeTestFonts.Box, [0x05D1] = OpenTypeTestFonts.Box
    }));
    private static TextStyle Style(double spacing = 0) => new() { Font = FontSpec.FromFamily(Family), FontSize = 20, LetterSpacing = spacing };

    [Fact]
    public void CommonStyleMeasuresOneGapBetweenGroupsAndKeepsNoLeadingOrTrailingGap() {
        var ordinary = TextLayoutEngine.Measure("HOO", Style());
        Assert.Equal(ordinary.Width + 2.4, TextLayoutEngine.Measure("HOO", Style(1.2)).Width, 8);
        Assert.Equal(TextLayoutEngine.Measure("H", Style()).Width, TextLayoutEngine.Measure("H", Style(1.2)).Width);
        Assert.Equal(0, TextLayoutEngine.Measure("", Style(1.2)).Width);
        Assert.Equal(ordinary.Width, TextLayoutEngine.Measure("HOO", Style(0)).Width);
        Assert.Equal(ordinary.Width - 2, TextLayoutEngine.Measure("HOO", Style(-1)).Width, 8);
    }
    [Fact]
    public void CombiningAndActuallyJoinedArabicGroupsStayWhole() {
        Assert.Equal(TextLayoutEngine.Measure("e\u0301O", Style()).Width + 1.2, TextLayoutEngine.Measure("e\u0301O", Style(1.2)).Width, 8);
        Assert.Equal(TextLayoutEngine.Measure("\u0628\u062A", Style()).Width, TextLayoutEngine.Measure("\u0628\u062A", Style(1.2)).Width, 8);
        Assert.Equal(TextLayoutEngine.Measure("\u05D0\u05D1 HOO", Style()).Width + 6, TextLayoutEngine.Measure("\u05D0\u05D1 HOO", Style(1.2)).Width, 8);
        var content = TextContent.FromRuns(new[] { new TextContentRun("e\u0301"), new TextContentRun("O", TextInlineEmphasis.Italic) });
        Assert.True(TextLayoutEngine.Measure(content, Style(1.2)).Width > TextLayoutEngine.Measure(content, Style()).Width);
    }
    [Theory]
    [InlineData("arabic", "لا")]
    [InlineData("cursive", "HO")]
    public void ExistingRequiredLigatureAndCursiveFixturesRetainTheSamePublicMeasuredInk(string fixture, string text) {
        var family = "CFX Tracking " + fixture;
        using var input = typeof(LetterSpacingContracts).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + fixture + ".ttf")!;
        using var bytes = new MemoryStream(); input.CopyTo(bytes); FontRegistry.Register(family, bytes.ToArray());
        var ordinary = Style(); ordinary.Font.Family = family; var tracked = ordinary.Clone(); tracked.LetterSpacing = 1.2;
        Assert.Equal(TextLayoutEngine.Measure(text, ordinary).Width, TextLayoutEngine.Measure(text, tracked).Width, 8);
        var first = ImageComposition.Create(100, 64, ChartColor.White).DrawTextLayout(4, 4, 90, TextLayoutEngine.Layout(text, 90, ordinary), ordinary).ToImage();
        var second = ImageComposition.Create(100, 64, ChartColor.White).DrawTextLayout(4, 4, 90, TextLayoutEngine.Layout(text, 90, tracked), tracked).ToImage();
        Assert.Equal(first.Pixels, second.Pixels);
    }
    [Fact]
    public void SignedSpacingRejectsNonfiniteValuesAndReversedGroupProgression() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Style(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Style(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextStyleOverride { LetterSpacing = double.NegativeInfinity });
        Assert.Throws<ArgumentOutOfRangeException>(() => TextLayoutEngine.Measure("HOO", Style(-100)));
    }
    [Fact]
    public void OverrideInheritanceExplicitZeroAndClonesRemainIndependent() {
        var ordinary = Style(1.2); var inherit = new TextStyleOverride(); var reset = new TextStyleOverride { LetterSpacing = 0 };
        Assert.Equal(1.2, inherit.Resolve(ordinary).LetterSpacing); Assert.True(reset.HasOverrides); Assert.Equal(0, reset.Resolve(ordinary).LetterSpacing);
        var copy = ordinary.Clone(); copy.LetterSpacing = 5; Assert.Equal(1.2, ordinary.LetterSpacing);
        var overrideCopy = reset.Clone(); overrideCopy.LetterSpacing = 7; Assert.Equal(0, reset.LetterSpacing);
    }
    [Fact]
    public void MeasurementCacheWrappingAndRasterLayoutUseTheAuthoredWidth() {
        var placement = new LabelPlacementService(); var plain = placement.Measure("HOO", Style()); var tracked = placement.Measure("HOO", Style(2));
        Assert.Equal(plain.Width + 4, tracked.Width, 8); Assert.Equal(plain.Width, placement.Measure("HOO", Style()).Width);
        var wrapped = TextLayoutEngine.Layout("HOO HOO", plain.Width + 1, Style(2), maximumLines: 2);
        Assert.True(wrapped.Trimmed); Assert.All(wrapped.Lines, line => Assert.True(line.Width <= plain.Width + 1));
        var zero = ImageComposition.Create(120, 64, ChartColor.White).DrawTextLayout(4, 4, 110,
            TextLayoutEngine.Layout("HOO", 110, Style()), Style()).ToImage();
        var spacedStyle = Style(2);
        var spaced = ImageComposition.Create(120, 64, ChartColor.White).DrawTextLayout(4, 4, 110,
            TextLayoutEngine.Layout("HOO", 110, spacedStyle), spacedStyle).ToImage();
        Assert.NotEqual(zero.Pixels, spaced.Pixels);
    }
    [Fact]
    public void PreparedNativeOwnedFontsAndPortableJsonRetainSpacingAfterCallerEdits() {
        var chart = TopologyChart.Create().WithTitle("").WithLegend(null).WithViewport(240, 120, 8); chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.Nodes.Add(new TopologyNode { Id = "tracked", Label = "HOO", X = 20, Y = 20, Width = 160, Height = 56,
            TextStyle = new TextStyleOverride { FontFamily = Family, FontSize = 20, LetterSpacing = 1.2 }, ShowStatusBadge = false });
        var options = new TopologyRenderOptions { FitContentToViewport = false, IncludeLegend = false, IncludeTitle = false };
        var prepared = chart.Prepare(options); var png = prepared.ToPng(); var svg = prepared.ToSvg();
        var envelope = prepared.ToInterchangeEnvelope(); var json = envelope.ToJson();
        Assert.Equal(1.2, envelope.Nodes[0].Topology!.TextStyle!.LetterSpacing); Assert.Contains("\"letterSpacing\":1.2", json);
        chart.Nodes[0].TextStyle!.LetterSpacing = 7; Assert.Equal(png, prepared.ToPng()); Assert.Equal(svg, prepared.ToSvg());
        FontRegistry.Clear(); var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        Assert.Equal(1.2, restored.Nodes[0].TextStyle!.LetterSpacing); Assert.Equal(png, restored.Prepare(options).ToPng());
    }
    [Fact]
    public void SourceLengthInheritanceUsesComputedPixelsRatherThanRescalingInheritedEm() {
        string Svg(string child) => "<svg xmlns='http://www.w3.org/2000/svg' width='220' height='90'><g font-family='" + Family
            + "' font-size='20' letter-spacing='.06em'><text x='4' y='60' font-size='40' " + child + ">HOO</text></g></svg>";
        Assert.Equal(SvgRasterizer.ToImage(Svg("")).Pixels, SvgRasterizer.ToImage(Svg("letter-spacing='1.2px'")).Pixels);
        Assert.NotEqual(SvgRasterizer.ToImage(Svg("")).Pixels, SvgRasterizer.ToImage(Svg("letter-spacing='.06em'")).Pixels);
    }
    [Fact]
    public void CanvasTextAndHeroUseOnlyTheOptionalSharedStyleHookAndLeaveZeroRequestsExact() {
        VisualCanvas Make(TextStyleOverride? textStyle) => VisualCanvas.Create(280, 130).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddLayer(new VisualCanvasTextLayer(8, 8, 260, "HOO", 20) { TextStyle = textStyle })
            .AddLayer(new VisualCanvasHeroTitleLayer(8, 48, 260, 28, new[] { new VisualCanvasTextRun("H", ChartColor.Black), new VisualCanvasTextRun("OO", ChartColor.FromHex("#245090")) }) { TextStyle = textStyle });
        var ordinary = Make(null); var zero = Make(new TextStyleOverride { LetterSpacing = 0 });
        Assert.Equal(ordinary.ToSvg(), zero.ToSvg()); Assert.Equal(ordinary.ToPng(), zero.ToPng());
        var tracked = Make(new TextStyleOverride { FontFamily = Family, LetterSpacing = 2 });
        var controlled = Make(new TextStyleOverride { FontFamily = Family, LetterSpacing = 0 });
        Assert.NotEqual(controlled.ToPng(), tracked.ToPng());
        var xml = XDocument.Parse(tracked.ToSvg()); Assert.Contains(xml.Descendants(), item => (string?)item.Attribute("data-cfx-native-text-ink") == "true");
    }
    [Theory]
    [InlineData("flowchart LR\nA[HOO]\nstyle A letter-spacing:.06em,font-size:20px")]
    [InlineData("flowchart LR\nA[HOO]\nstyle A font-size:20px,letter-spacing:.06em")]
    public void MermaidSourceLengthsResolveAfterSameElementSizeRegardlessOfDeclarationOrder(string source) {
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics);
        var node = Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes);
        Assert.Equal(20, node.TextStyle!.FontSize); Assert.Equal(1.2, node.TextStyle.LetterSpacing!.Value, 8);
    }
    [Fact]
    public void MermaidBlockSourceUsesTheSameComputedLengthOwner() {
        var result = MermaidRenderer.Render("block-beta\ncolumns 1\nA[\"HOO\"]\nstyle A letter-spacing:.06em,font-size:20px");
        Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics);
        var item = Assert.Single(Assert.IsType<BlockLayoutBlock>(result.Artifact!.Model).Items);
        Assert.Equal(20, item.TextStyle.FontSize); Assert.Equal(1.2, item.TextStyle.LetterSpacing!.Value, 8);
    }
    public void Dispose() => FontRegistry.Clear();
}
