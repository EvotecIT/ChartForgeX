using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Incumbent public tracking workflows for the three reviewed regression classes.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class LetterSpacingReviewRegressionTests : IDisposable {
    private const string Family = "CFX Tracking Review Regression";
    public LetterSpacingReviewRegressionTests() {
        // Preserve the authored H/O/space fixture glyphs. Only incidental metric probes
        // and truncation punctuation get bounded synthetic coverage instead of host fallback.
        var incidental = Enumerable.Range(32, 95).Where(value => !"HOxeFChartForgeX 0123456789".Contains((char)value))
            .ToDictionary(value => value, _ => OpenTypeTestFonts.Box);
        incidental[0x2026] = OpenTypeTestFonts.Box;
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(extraGlyphs: incidental));
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(extraGlyphs: incidental, italic: true), italic: true);
    }

    [Theory]
    [InlineData("space", false)] [InlineData("space", true)]
    [InlineData("baseline", false)] [InlineData("baseline", true)]
    public void PresentationNamesPreserveWhitespaceAndBaselineInBothAttributePaths(string kind, bool manyAttributes) {
        var first = SvgSource(kind == "space" ? "xml:space='preserve'" : "alignment-baseline='middle'", manyAttributes);
        var control = SvgSource(kind == "space" ? "style='white-space:pre'" : "dominant-baseline='middle'", manyAttributes);
        var expected = RasterImage(kind + "-" + manyAttributes + "-control", control);
        var ordinary = RasterImage(kind + "-" + manyAttributes + "-ordinary", SvgSource("", manyAttributes));
        var actual = RasterImage(kind + "-" + manyAttributes + "-actual", first);
        Assert.Equal(expected.Pixels, actual.Pixels);
        Assert.NotEqual(ordinary.Pixels, expected.Pixels);
    }
    [Fact]
    public void ExplicitWhiteSpaceStillWinsOverXmlSpaceInItsExistingPresentationOrder() {
        var first = SvgSource("xml:space='preserve' white-space='normal'", false);
        var control = SvgSource("white-space='normal'", false);
        var expected = RasterImage("white-space-order-control", control); var actual = RasterImage("white-space-order-actual", first); Assert.Equal(expected.Pixels, actual.Pixels);
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Icon, false)] [InlineData(TopologyNodeDisplayMode.Icon, true)]
    [InlineData(TopologyNodeDisplayMode.Tile, false)] [InlineData(TopologyNodeDisplayMode.Tile, true)]
    [InlineData(TopologyNodeDisplayMode.Artwork, false)] [InlineData(TopologyNodeDisplayMode.Artwork, true)]
    public void CapturedExternalCaptionTrackingProjectsOnceThroughEnlargementAndShrink(TopologyNodeDisplayMode mode, bool enlarge) {
        var chart = External(mode, enlarge); var node = chart.Nodes[0]; var options = chart.ResolveRenderOptions(null); options.FitContentToViewport = true;
        var before = chart.Prepare(options); CaptureNative(mode + "-" + enlarge + "-direct-before-portable", before.Visual); var saved = before.ToInterchangeEnvelope(); var json = saved.ToJson();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); var after = restored.Prepare();
        Capture(mode + "-" + enlarge + "-direct", before.Visual); Capture(mode + "-" + enlarge + "-restored", after.Visual);
        var units = saved.Nodes[0].Width!.Value / node.Width; Assert.True(enlarge ? units > 1 : units < 1);
        Assert.Equal(1.2 * units, saved.Nodes[0].Topology!.CaptionTextStyle!.LetterSpacing!.Value, 8);
        Assert.Equal(.8 * units, saved.Nodes[0].Topology!.TextStyle!.LetterSpacing!.Value, 8);
        Assert.Equal(.6 * units, saved.Nodes[0].Topology!.SubtitleTextStyle!.LetterSpacing!.Value, 8);
        var directInk = CaptionInk(before.Visual); var restoredInk = CaptionInk(after.Visual); Assert.NotEmpty(directInk); Assert.Equal(directInk.Length, restoredInk.Length);
        for (var index = 0; index < directInk.Length; index++) {
            Assert.Equal(directInk[index].X, restoredInk[index].X, 8); Assert.Equal(directInk[index].Baseline, restoredInk[index].Baseline, 8);
            Assert.Equal(directInk[index].Text.Style.LetterSpacing, restoredInk[index].Text.Style.LetterSpacing, 8); Assert.Equal(directInk[index].Text.Metrics.Width, restoredInk[index].Text.Metrics.Width, 8);
            Assert.Equal(directInk[index].Text.Lines.Select(line => line.Text), restoredInk[index].Text.Lines.Select(line => line.Text));
        }
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(after.ToSvg())); Assert.Equal(before.ToPng(), after.ToPng());
        Assert.Equal(chart.Id, saved.Id); Assert.Equal(TopologyLayoutMode.Manual, restored.LayoutMode); Assert.Equal(1.2, node.CaptionTextStyle!.LetterSpacing);
    }
    [Theory] [InlineData(TopologyNodeDisplayMode.Icon)] [InlineData(TopologyNodeDisplayMode.Tile)] [InlineData(TopologyNodeDisplayMode.Artwork)]
    public void UnitScaleAndExplicitZeroKeepExternalCaptionBytes(TopologyNodeDisplayMode mode) {
        var chart = External(mode, null); var baseStyle = chart.Nodes[0].TextStyle!; var subtitleStyle = chart.Nodes[0].SubtitleTextStyle!; var captionStyle = chart.Nodes[0].CaptionTextStyle!; baseStyle.LetterSpacing = null; subtitleStyle.LetterSpacing = null; captionStyle.LetterSpacing = null;
        var before = chart.Prepare(); Capture(mode + "-null-control", before.Visual);
        baseStyle.LetterSpacing = 0; subtitleStyle.LetterSpacing = 0; captionStyle.LetterSpacing = 0;
        var zero = chart.Prepare(); Capture(mode + "-zero-control", zero.Visual);
        var restored = before.ToInterchangeEnvelope().ToTopologyChart().Prepare(); Capture(mode + "-unit-restored", restored.Visual);
        Assert.Equal(before.ToSvg(), zero.ToSvg()); Assert.Equal(before.ToPng(), zero.ToPng());
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(restored.ToSvg())); Assert.Equal(before.ToPng(), restored.ToPng());
    }

    [Theory] [InlineData("color")] [InlineData("weight")]
    public void PlainDetailNonTrackingOverridesInheritTheWholeNodeTrackingPolicy(string overrideKind) {
        var inherited = Detail(overrideKind, null); var explicitGap = Detail(overrideKind, 1.2); var reset = Detail(overrideKind, 0); var noNodeGap = Detail(overrideKind, 0, 0);
        var first = inherited.Prepare(); var second = explicitGap.Prepare(); var zero = reset.Prepare(); var ordinary = noNodeGap.Prepare();
        Capture("detail-" + overrideKind + "-inherit", first.Visual); Capture("detail-" + overrideKind + "-explicit", second.Visual); Capture("detail-" + overrideKind + "-reset", zero.Visual); Capture("detail-" + overrideKind + "-ordinary", ordinary.Visual);
        var ink = Assert.Single(first.Visual.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-detail");
        Assert.Equal(1.2, ink.Text.Style.LetterSpacing); Assert.Equal(TopologyStaticPaintObservation.Paint(second.ToSvg()), TopologyStaticPaintObservation.Paint(first.ToSvg())); Assert.Equal(second.ToPng(), first.ToPng());
        // Explicit detail zero does not reset the independent heading policy. Compare
        // the actual detail geometry and complete pixels, rather than requiring the
        // two deliberately different heading letter-spacing attributes to match.
        var zeroInk = Assert.Single(zero.Visual.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-detail");
        var ordinaryInk = Assert.Single(ordinary.Visual.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-detail");
        Assert.Equal(0, zeroInk.Text.Style.LetterSpacing); Assert.Equal(ordinaryInk.Text.Style.LetterSpacing, zeroInk.Text.Style.LetterSpacing);
        Assert.Equal(ordinaryInk.X, zeroInk.X); Assert.Equal(ordinaryInk.Baseline, zeroInk.Baseline);
        Assert.Equal(ordinaryInk.Text.Metrics.Width, zeroInk.Text.Metrics.Width);
        Assert.Equal(ordinaryInk.Text.Lines.Select(line => line.Text), zeroInk.Text.Lines.Select(line => line.Text));
        Assert.Equal(1.2, Assert.Single(zero.Visual.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-label").Text.Style.LetterSpacing);
        Assert.Equal(0, Assert.Single(ordinary.Visual.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-label").Text.Style.LetterSpacing);
        Assert.Equal(ordinary.ToPng(), zero.ToPng()); Assert.NotEqual(first.ToPng(), zero.ToPng());
        var restored = first.ToInterchangeEnvelope().ToTopologyChart().Prepare(); Capture("detail-" + overrideKind + "-restored", restored.Visual); Assert.Equal(first.ToPng(), restored.ToPng());
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void CapturedCardSubtitleAndDetailRolesReuseTheSameOriginalUnitConversion(bool enlarge) {
        var chart = Detail("color", 1.2); chart.WithViewport(enlarge ? 1000 : 220, enlarge ? 700 : 140, 24);
        if (enlarge) chart.WithLayout(TopologyLayoutMode.Layered);
        chart.Nodes[0].Subtitle = "HOO"; chart.Nodes[0].SubtitleTextStyle = new TextStyleOverride { FontFamily = Family, FontSize = 12, FontWeight = "400", LetterSpacing = .6 };
        var options = chart.ResolveRenderOptions(null); options.FitContentToViewport = true;
        var before = chart.Prepare(options); CaptureNative("card-roles-" + enlarge + "-before-portable", before.Visual); var saved = before.ToInterchangeEnvelope(); var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart(); var after = restored.Prepare();
        Capture("card-roles-" + enlarge + "-direct", before.Visual); Capture("card-roles-" + enlarge + "-restored", after.Visual);
        var units = saved.Nodes[0].Width!.Value / chart.Nodes[0].Width; Assert.True(enlarge ? units > 1 : units < 1);
        var node = Assert.Single(restored.Nodes); Assert.Equal(.6 * units, node.SubtitleTextStyle!.LetterSpacing!.Value, 8); Assert.Equal(1.2 * units, Assert.Single(node.Details).TextStyle!.LetterSpacing!.Value, 8);
        Assert.Contains(before.Visual.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-detail");
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(after.ToSvg())); Assert.Equal(before.ToPng(), after.ToPng());
    }

    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void ExplicitPlainAndStructuredEdgeBaselinesUseTheSameOutputTrackingAndRestore(bool structured, bool fit) {
        var chart = Edge(structured); var options = new TopologyRenderOptions { FitContentToViewport = fit, IncludeTitle = false, IncludeLegend = false, IncludeEdgeLabelBackplates = false, WrapNodeLabels = true, AllowMultilineNodeLabels = true };
        var before = fit ? chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 280), 20), chart.Theme, frame: new VisualFrame(titleStyle: FrameStyle(chart.Theme.Typography.TitleSize, 700), subtitleStyle: FrameStyle(chart.Theme.Typography.SubtitleSize, 400), legendStyle: FrameStyle(chart.Theme.Typography.LegendSize, 400)), font: FontSpec.FromFamily(Family)), options) : chart.Prepare(options).Visual;
        CaptureNative("baseline-" + structured + "-" + fit + "-before-portable", before); var saved = before.ToArtifact().ToInterchangeEnvelope(); var restored = saved.ToTopologyChart().Prepare(); Capture("baseline-" + structured + "-" + fit + "-direct", before); Capture("baseline-" + structured + "-" + fit + "-restored", restored.Visual);
        var units = saved.Nodes[0].Width!.Value / chart.Nodes[0].Width; if (fit) Assert.NotEqual(1, units); else Assert.Equal(1, units);
        var marks = before.Scene.Nodes.OfType<VisualSceneText>().Where(mark => mark.Role == (structured ? "inline-text-run" : "topology-edge-label-text")).ToArray(); Assert.NotEmpty(marks);
        Assert.Equal(saved.Edges[0].Topology!.LabelPosition!.Point.Y, marks[0].Baseline, 6); Assert.All(marks, mark => Assert.Equal(3 * units, mark.Text.Style.LetterSpacing, 8));
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(restored.ToSvg())); Assert.Equal(before.ToPng(), restored.ToPng());
    }

    private static TopologyChart External(TopologyNodeDisplayMode mode, bool? enlarge) {
        var chart = TopologyChart.Create().WithId("tracking-external").WithTitle("").WithLegend(null).WithViewport(enlarge.HasValue ? enlarge.Value ? 1000 : 180 : 640, enlarge.HasValue ? enlarge.Value ? 700 : 140 : 400, 24)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(Family)).WithLayout(enlarge.HasValue ? TopologyLayoutMode.Layered : TopologyLayoutMode.Manual)
            .AddNode("node", "HOO HOO", 210, 150, width: 180, height: 90);
        var node = chart.Nodes[0]; node.DisplayMode = mode; node.Shape = TopologyNodeShape.Rectangle; node.IconShape = TopologyIconShape.Router; node.PreserveDisplayModeSize = true; node.ShowStatusBadge = false; node.Color = "#2563eb";
        node.TextStyle = new TextStyleOverride { FontFamily = Family, FontSize = 20, FontWeight = "400", LetterSpacing = .8 };
        node.SubtitleTextStyle = new TextStyleOverride { FontFamily = Family, FontSize = 12, FontWeight = "400", LetterSpacing = .6 }; if (mode == TopologyNodeDisplayMode.Tile) node.Subtitle = "HOO";
        node.CaptionPlacement = TopologyCaptionPlacement.Top; node.CaptionGap = 8; node.CaptionSurface = TopologyCaptionSurface.Plate;
        node.CaptionTextStyle = new TextStyleOverride { FontFamily = Family, FontSize = 14, FontWeight = "400", LetterSpacing = 1.2 };
        if (mode == TopologyNodeDisplayMode.Artwork) node.Artwork = TopologyIconArtwork.Portrait(new RgbaImage(2, 1, new byte[] { 255, 0, 0, 255, 0, 255, 0, 128 }));
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeIconLabels = true, IncludeTileSubtitles = true, FitContentToViewport = false, MaxNodeLabelLines = 4, WrapNodeLabels = true }); return chart;
    }
    private static TopologyChart Detail(string kind, double? spacing, double nodeSpacing = 1.2) {
        var chart = TopologyChart.Create().WithId("tracking-detail").WithTitle("").WithLegend(null).WithViewport(480, 300, 24).WithTheme(VisualTheme.Graphite().WithFontFamily(Family)).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("node", "H", 70, 60, width: 300, height: 180);
        var node = chart.Nodes[0]; node.ShowLabel = true; node.ShowStatusBadge = false; node.PreserveDisplayModeSize = true; node.Shape = TopologyNodeShape.Rectangle; node.TextStyle = new TextStyleOverride { FontFamily = Family, FontSize = 20, LetterSpacing = nodeSpacing };
        node.Details.Add(new TopologyNodeDetail { Text = "HOO", TextStyle = kind == "color" ? new TextStyleOverride { Color = chart.Theme.Resolve(chart.ThemeMode).Foreground, LetterSpacing = spacing } : new TextStyleOverride { FontWeight = "400", LetterSpacing = spacing } });
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, FitContentToViewport = false }); return chart;
    }
    private static TopologyChart Edge(bool structured) {
        var chart = TopologyChart.Create().WithId("tracking-baseline").WithTitle("").WithLegend(null).WithViewport(760, 440, 24).WithTheme(VisualTheme.Graphite().WithFontFamily(Family)).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("a", "HOO", 50, 160, width: 150, height: 70).AddNode("b", "HOO", 540, 160, width: 150, height: 70).AddEdge("edge", "a", "b", "HOO HOO HOO\nHOO");
        foreach (var node in chart.Nodes) node.ShowStatusBadge = false;
        var edge = chart.Edges[0]; edge.PathGeometry = new TopologyPathGeometry("M 200 195 C 300 25 440 25 540 195"); edge.LabelPosition = new TopologyLabelPosition(new ChartPoint(370, 70), TopologyLabelVerticalAnchor.TextBaseline);
        edge.TextStyle = new TextStyleOverride { FontFamily = Family, FontSize = 18, FontWeight = "400", LetterSpacing = 3 };
        if (structured) edge.LabelContent = TextContent.FromRuns(new[] { new TextContentRun("HOO HOO "), new TextContentRun("HOO\nHOO", TextInlineEmphasis.Italic) }); return chart;
    }
    private static TextStyle FrameStyle(double size, int weight) => new() { Font = new FontSpec { Family = Family, Weight = weight }, FontSize = size, LineHeight = 1 };
    // Keep the original XML and public diagnostics or exception before any pixel assertion.
    private static RgbaImage RasterImage(string name, string source) {
        var output = Environment.GetEnvironmentVariable("CFX_LETTER_SPACING_REVIEW_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), source); }
        try {
            var result = SvgRasterizer.Rasterize(source);
            CaptureImage(name, source, result.Image);
            if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, name + ".diagnostics.json"), JsonSerializer.Serialize(result.Diagnostics));
            return result.Image;
        } catch (Exception exception) {
            if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, name + ".exception.txt"), exception.ToString());
            throw;
        }
    }
    private static string SvgSource(string attributes, bool many) => "<svg xmlns='http://www.w3.org/2000/svg' width='180' height='80' viewBox='0 0 180 80'><text x='4' y='40' font-family='" + Family + "' font-size='20' " + attributes
        + (many ? string.Concat(Enumerable.Range(0, 33).Select(index => " data-proof-" + index + "='x'")) : "") + ">H  O</text></svg>";
    private static VisualSceneText[] CaptionInk(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(mark => mark.Role == "inline-text-run").ToArray();
    private static void CaptureImage(string name, string source, RgbaImage image) {
        if (Environment.GetEnvironmentVariable("CFX_LETTER_SPACING_REVIEW_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name); File.WriteAllText(stem + ".svg", source); File.WriteAllBytes(stem + ".rgba", image.Pixels); File.WriteAllBytes(stem + ".png", PngWriter.WriteRgba(image)); File.WriteAllText(stem + ".dimensions.txt", image.Width + "x" + image.Height);
    }
    private static void CaptureNative(string name, PreparedVisual prepared) {
        TopologyPortableFontObservation.Capture("tracking-review-" + name, prepared);
        if (Environment.GetEnvironmentVariable("CFX_LETTER_SPACING_REVIEW_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name); File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + ".png", prepared.ToPng());
        File.WriteAllText(stem + ".text.json", JsonSerializer.Serialize(prepared.Scene.Nodes.OfType<VisualSceneText>().Select(mark => new { mark.Role, mark.X, mark.Baseline, mark.Text.Size, mark.Text.Style.LetterSpacing, Width = mark.Text.Metrics.Width, Height = mark.Text.Metrics.Height, Lines = mark.Text.Lines.Select(line => line.Text) })));
    }
    private static void Capture(string name, PreparedVisual prepared) {
        CaptureNative(name, prepared);
        if (Environment.GetEnvironmentVariable("CFX_LETTER_SPACING_REVIEW_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        File.WriteAllText(Path.Combine(output, name + ".json"), prepared.ToArtifact().ToInterchangeEnvelope().ToJson());
    }
    public void Dispose() => FontRegistry.Clear();
}
