using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Safe editable content retains real native face/ink decisions and self-contained resource lifetimes.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeInlineContentTests : IDisposable {
    private const string Family = "CFX Native Inline Snapshot";
    public NativeInlineContentTests() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> { ['H'] = OpenTypeTestFonts.H }));
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> { ['H'] = OpenTypeTestFonts.O }, weight: 700), 700);
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> { ['H'] = OpenTypeTestFonts.E }, italic: true), 400, italic: true);
    }
    [Fact]
    public void RunsAreDetachedCanonicalAndThePlainCaptionCannotContradictThem() {
        var list = new List<TextContentRun> { new("Re", TextInlineEmphasis.Bold), new("view", TextInlineEmphasis.Bold), new(" safely") };
        var content = TextContent.FromRuns(list); list.Clear();
        Assert.Equal("Review safely", content.PlainText); Assert.Equal(2, content.Runs.Count);
        var node = new TopologyNode { LabelContent = content }; Assert.Equal(content.PlainText, node.Label);
        node.Label = "Edited"; Assert.False(node.LabelContent.IsStructured); Assert.Equal("Edited", node.LabelContent.PlainText);
        var detail = new TopologyNodeDetail { ValueContent = content }; Assert.Equal(content.PlainText, detail.Value);
        detail.Value = "Changed"; Assert.False(detail.ValueContent.IsStructured);
        detail.Content = content; Assert.Equal(content.PlainText, detail.Text); detail.Text = null; Assert.Null(detail.Content);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextContentRun("x", (TextInlineEmphasis)4));
        Assert.Throws<ArgumentException>(() => TextContent.FromRuns(new[] { default(TextContentRun) }));
    }
    [Theory]
    [InlineData(TextInlineEmphasis.Normal, 400, false)]
    [InlineData(TextInlineEmphasis.Bold, 700, false)]
    [InlineData(TextInlineEmphasis.Italic, 400, true)]
    public void RegisteredRegularBoldAndItalicAreRealCapturedFacesAndDifferentInk(TextInlineEmphasis emphasis, int weight, bool italic) {
        var style = Style(); var prepared = Prepare(TextContent.FromRuns(new[] { new TextContentRun("H", emphasis) }), style);
        var text = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>());
        Assert.Equal(weight, text.Text.Style.Font.Weight); Assert.Equal(italic, text.Text.Style.Font.Italic);
        Assert.NotNull(text.Text.Face.Font); Assert.False(text.Text.Face.SynthesizeBold); Assert.False(text.Text.Face.SynthesizeItalic);
        var regular = Prepare(TextContent.FromRuns(new[] { new TextContentRun("H") }), style);
        if (emphasis != TextInlineEmphasis.Normal) Assert.NotEqual(regular.ToPng(), prepared.ToPng());
        var svg = XDocument.Parse(prepared.ToSvg()); Assert.Contains(svg.Descendants(), node => node.Name.LocalName == "use");
        Assert.Contains(svg.Descendants(), node => node.Name.LocalName == "path" && !string.IsNullOrEmpty((string?)node.Attribute("d")));
    }
    [Fact]
    public void ExplicitStructuredBaseIsRegularAndAuthoredWeightStillPrecedesStrong() {
        var native = Model(); var node = native.Nodes[0]; node.LabelContent = TextContent.FromRuns(new[] { new TextContentRun("H"), new TextContentRun("H", TextInlineEmphasis.Bold) });
        var prepared = nodeScene(native); Assert.Equal(new[] { 400, 700 }, prepared.Select(text => text.Text.Style.Font.Weight));
        node.TextStyle = new TextStyleOverride { FontWeight = "850", Underline = true };
        Assert.All(nodeScene(native), text => { Assert.Equal(850, text.Text.Style.Font.Weight); Assert.NotEqual(TextDecorationStyle.None, text.Text.Style.UnderlineStyle); });
        node.Label = "HH"; node.TextStyle = null; Assert.All(nodeScene(native), text => Assert.True(text.Text.Style.Font.Weight >= 600));
    }
    [Fact]
    public void OrdinaryPlainContentKeepsTheExistingSvgPngAndPortableBytes() {
        var a = Model(); a.Nodes[0].Label = "HH"; var b = Model(); b.Nodes[0].LabelContent = TextContent.Plain("HH");
        Assert.Equal(a.Prepare().ToSvg(), b.Prepare().ToSvg()); Assert.Equal(a.Prepare().ToPng(), b.Prepare().ToPng());
        Assert.Equal(a.ToVisualArtifact().ToInterchangeJson(), b.ToVisualArtifact().ToInterchangeJson());
        Assert.DoesNotContain("labelRuns", a.ToVisualArtifact().ToInterchangeJson(), StringComparison.Ordinal);
        Assert.Contains(XDocument.Parse(a.Prepare().ToSvg()).Descendants(), node => node.Name.LocalName == "text" && node.Value == "HH");
    }
    [Theory]
    [InlineData(TextAlignment.Left)]
    [InlineData(TextAlignment.Center)]
    [InlineData(TextAlignment.Right)]
    public void CapturedPlainPlacementReusesTheExistingFaceAndDecorationOwner(TextAlignment alignment) {
        var style = Style(); style.Alignment = alignment; style.UnderlineStyle = TextDecorationStyle.Double;
        style.StrikethroughStyle = TextDecorationStyle.Dashed;
        var face = new VisualSceneTextFace(style); var text = face.Prepare("HH", style.EffectiveFontSize);
        var direct = new VisualSceneBuilder(new VisualSize(240, 90), style.Font); direct.Text("HH", 120, 48, style, "captured-control", "value");
        var expected = new PreparedVisual(direct.Build()); var svg = expected.ToSvg(); var png = expected.ToPng();
        style.Color = ChartColor.White; style.Font.Weight = 900; FontRegistry.Clear();
        var captured = new VisualSceneBuilder(new VisualSize(240, 90), style.Font);
        captured.PreparedText(text, 120, 48, ChartColor.Black, alignment, "captured-control", "value", includeDecorations: true);
        var actual = new PreparedVisual(captured.Build());
        Assert.Equal(svg, actual.ToSvg()); Assert.Equal(png, actual.ToPng());
        Assert.DoesNotContain("data-cfx-caption", actual.ToSvg(), StringComparison.Ordinal);
        var existingDefault = new VisualSceneBuilder(new VisualSize(240, 90), style.Font);
        existingDefault.PreparedText(text, 120, 48, ChartColor.Black, alignment, "captured-control", "value");
        Assert.Single(existingDefault.Build().Nodes); Assert.Equal(alignment, text.Style.Alignment);
    }
    [Fact]
    public void WrapAndEllipsisKeepEmphasisAndNeverSplitASurrogateOrCombiningCluster() {
        var content = TextContent.FromRuns(new[] { new TextContentRun("H😀é", TextInlineEmphasis.Bold), new TextContentRun(" H H") });
        var style = Style(); var layout = TextLayoutEngine.Layout(content, 24, style, TextWrapMode.Character, 2);
        Assert.True(layout.Trimmed); Assert.InRange(layout.Lines.Count, 1, 2);
        Assert.All(layout.Lines, line => { Assert.Equal(line.Text, line.Content.PlainText); Assert.False(line.Text.Length > 0 && char.IsLowSurrogate(line.Text[0])); Assert.False(line.Text.StartsWith("́", StringComparison.Ordinal)); });
        Assert.Contains("…", layout.Lines.Last().Text, StringComparison.Ordinal);
    }
    [Fact]
    public void NativeResourcesStayReachableInOrdinaryAndPngCapturesAfterSourceAndRegistryMutation() {
        var model = Model(); model.Nodes[0].LabelContent = TextContent.FromRuns(new[] { new TextContentRun("HH"), new TextContentRun("HH", TextInlineEmphasis.Bold) });
        var prepared = model.Prepare(); var ordinary = prepared.ToSvg(); var pixels = prepared.ToPng(); var captured = prepared.Visual.ForInteractivePngExport().ToSvg();
        AssertResources(ordinary); AssertResources(captured);
        model.Nodes[0].Label = "New"; model.Nodes[0].TextStyle = new TextStyleOverride { FontWeight = "900" }; FontRegistry.Clear();
        Assert.Equal(ordinary, prepared.ToSvg()); Assert.Equal(pixels, prepared.ToPng()); Assert.Equal(captured, prepared.Visual.ForInteractivePngExport().ToSvg());
        var resources = XDocument.Parse(ordinary).Descendants().Where(node => node.Name.LocalName == "path" && node.Attribute("id") != null).ToArray();
        Assert.True(resources.Length < XDocument.Parse(ordinary).Descendants().Count(node => node.Name.LocalName == "use"));
    }
    [Fact]
    public void PortableRestorationKeepsEditableRunsAndRejectsContradictoryPlainLabels() {
        var model = Model(); var content = TextContent.FromRuns(new[] { new TextContentRun("H", TextInlineEmphasis.Bold), new TextContentRun(" "), new TextContentRun("H", TextInlineEmphasis.Bold | TextInlineEmphasis.Italic) });
        model.Nodes[0].LabelContent = content; model.Nodes[0].Details.Add(new TopologyNodeDetail { Label = "Text:", ValueContent = content });
        var saved = VisualArtifactInterchangeEnvelope.FromJson(model.ToVisualArtifact().ToInterchangeJson());
        Assert.Equal(content.PlainText, saved.Nodes[0].Topology!.LabelContent!.PlainText);
        var restored = saved.ToTopologyChart(); Assert.Equal(content.Runs.Select(run => run.Emphasis), restored.Nodes[0].LabelContent.Runs.Select(run => run.Emphasis));
        Assert.Equal(content.Runs.Select(run => run.Emphasis), restored.Nodes[0].Details[0].ValueContent.Runs.Select(run => run.Emphasis));
        var frozen = saved.ToJson(); model.Nodes[0].Label = "Other"; Assert.Equal(frozen, saved.ToJson());
        saved.Nodes[0].Label = "Contradiction"; Assert.Throws<ArgumentException>(() => saved.ToJson());
    }
    [Fact]
    public void CrossRunArabicJoiningUsesOneNativeShapingContext() {
        var glyphs = new Dictionary<int, int> { [0x0628] = OpenTypeTestFonts.E, [0xFE8F] = OpenTypeTestFonts.E, [0xFE91] = OpenTypeTestFonts.H, [0xFE90] = OpenTypeTestFonts.O };
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(extraGlyphs: glyphs));
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(extraGlyphs: glyphs, weight: 700), 700);
        var all = Prepare(TextContent.FromRuns(new[] { new TextContentRun("بب") }), Style());
        var split = Prepare(TextContent.FromRuns(new[] { new TextContentRun("ب"), new TextContentRun("ب", TextInlineEmphasis.Bold) }), Style());
        Assert.Equal(all.ToRgba().Pixels, split.ToRgba().Pixels);
    }
    [Fact]
    public void BuiltinFontlessInkIsCapturedWithoutBrowserFontOrInstalledFontRequirements() {
        var style = Style(); var glyphFace = new ResolvedTypeface(null, true, true);
        var prepared = new VisualScenePreparedText(glyphFace, style, 16, 14,
            new[] { new VisualSceneTextLine("HH", 24, Array.Empty<ShapedGlyph>()) }, new TextMetrics(24, 16, 16), nativeGlyphInk: true);
        var scene = new VisualScene(new VisualSize(90, 50), new VisualSceneNode[] { new VisualSceneText(prepared, 10, 30, ChartColor.Black, TextAlignment.Left, "fontless-rich", null) }, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var visual = new PreparedVisual(scene); AssertResources(visual.ToSvg()); Assert.NotEmpty(visual.ToPng());
        var control = new RgbaCanvas(90, 50, 2, null, outputScale: 1, useDefaultOutlineFont: false);
        control.DrawBitmapText(10, 16, "HH", ChartColor.Black, 16, true, RgbaCanvas.EmphasisOffset(16));
        Assert.Equal(control.ToOutputPixels(), visual.ToRgba().Pixels);
        Assert.NotEqual(visual.ToPng(), new PreparedVisual(new VisualScene(new VisualSize(90, 50), Array.Empty<VisualSceneNode>(), Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>())).ToPng());
    }
    [Fact]
    public void NestedIndependentSvgNamespacesKeepAllPermanentResourcesSelfContained() {
        var visual = Prepare(TextContent.FromRuns(new[] { new TextContentRun("HH", TextInlineEmphasis.Bold) }), Style());
        var nested = "<svg xmlns='http://www.w3.org/2000/svg' width='480' height='90'>" + visual.ToSvg("left") + visual.ToSvg("right") + "</svg>";
        AssertResources(nested); var ids = XDocument.Parse(nested).Descendants().Attributes("id").Select(id => id.Value).ToArray(); Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.NotEmpty(ChartForgeX.SvgRaster.SvgRasterizer.ToImage(nested).Pixels);
    }
    [Theory]
    [InlineData("color-colr0.ttf")]
    [InlineData("color-colr1.ttf")]
    public void UnsupportedStructuredColorGlyphSvgFailsExplicitlyWithoutChangingPlainFallback(string fixture) {
        using var stream = typeof(NativeInlineContentTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + fixture)
            ?? throw new InvalidOperationException("Missing existing color font fixture.");
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); FontRegistry.Register(Family, bytes.ToArray());
        var rich = Prepare(TextContent.FromRuns(new[] { new TextContentRun("😀") }), Style());
        Assert.Throws<NotSupportedException>(() => rich.ToSvg()); Assert.NotEmpty(rich.ToPng());
        var plain = new VisualSceneBuilder(new VisualSize(240, 90), Style().Font); plain.Text("😀", 12, 48, Style());
        Assert.Contains("😀", new PreparedVisual(plain.Build()).ToSvg(), StringComparison.Ordinal);
    }
    internal static void AssertResources(string svg) {
        var document = XDocument.Parse(svg); var ids = document.Descendants().Attributes("id").Select(id => id.Value).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(document.Descendants(), node => node.Attribute("data-cfx-native-text-resources") != null);
        var uses = document.Descendants().Where(node => node.Name.LocalName == "use").ToArray(); Assert.NotEmpty(uses);
        Assert.All(uses, node => { var href = (string?)node.Attribute("href"); Assert.NotNull(href); Assert.StartsWith("#", href); Assert.Contains(href![1..], ids); });
        Assert.DoesNotContain(document.Descendants(), node => node.Name.LocalName is "script" or "foreignObject" or "image");
    }
    private static TextStyle Style() => new() { Font = FontSpec.FromFamily(Family), FontSize = 20, Color = ChartColor.Black, Hinting = TextHinting.None, LineHeight = 1 };
    private static PreparedVisual Prepare(TextContent content, TextStyle style) {
        var builder = new VisualSceneBuilder(new VisualSize(240, 90), style.Font); builder.InlineText(content, new ChartRect(12, 12, 210, 65), style, TextWrapMode.Word, 3, "rich-caption", "caption"); return new PreparedVisual(builder.Build());
    }
    private static TopologyChart Model() {
        var model = TopologyChart.Create().WithViewport(400, 240).WithTheme(VisualTheme.Report().WithFontFamily(Family + ", " + BrowserProofFonts.Family));
        model.Nodes.Add(new TopologyNode { Id = "node", Label = "HH", X = 20, Y = 30, Width = 300, Height = 160, ShowStatusBadge = false, PreserveDisplayModeSize = true }); return model;
    }
    private static IEnumerable<VisualSceneText> nodeScene(TopologyChart model) => model.Prepare().Visual.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "inline-text-run" || text.Role == "topology-node-label");
    public void Dispose() => FontRegistry.Clear();
}
