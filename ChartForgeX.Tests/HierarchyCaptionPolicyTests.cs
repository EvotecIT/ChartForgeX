using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HierarchyCaptionPolicyTests {
    [Theory]
    [InlineData(420, VisualThemeMode.Light)]
    [InlineData(420, VisualThemeMode.Dark)]
    [InlineData(1100, VisualThemeMode.Light)]
    [InlineData(1100, VisualThemeMode.Dark)]
    public void CurvedPrimaryCaptionsRetainCompleteClustersInsideActualRoundedAnnularMarks(int width, VisualThemeMode mode) {
        var chart = SunburstCurvedLabelsRecipe.Create(mode: mode);
        var context = Context(width, mode); var prepared = chart.Prepare(context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "sunburst-curved-" + width + "-" + mode);
        var svg = XDocument.Parse(prepared.ToSvg());
        var arcs = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-caption-placement") == "arc").ToArray();
        Assert.NotEmpty(arcs);
        foreach (var arc in arcs) {
            var text = string.Concat(arc.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value));
            Assert.Equal((string?)arc.Attribute("data-cfx-full-label"), text);
            Assert.Equal(text, (string?)arc.Attribute("aria-label"));
        }
        AssertArcInkInsideNativeMarks(prepared);
        var source = chart.Series[0].HierarchyItems.Select(item => (item.Id, item.ParentId, item.Label, item.Value)).ToArray();
        var frozenSvg = prepared.ToSvg(); var frozenPng = prepared.ToPng();
        chart.ConfigureSunburst(options => options.CurvedLabels = false);
        var ordinary = chart.Prepare(context);
        Assert.DoesNotContain(ordinary.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.ContainsKey("data-cfx-caption-placement"));
        Assert.Equal(source, chart.Series[0].HierarchyItems.Select(item => (item.Id, item.ParentId, item.Label, item.Value)));
        Assert.Equal(prepared.Regions.Select(region => (region.Id, region.Bounds, region.Label)), ordinary.Regions.Select(region => (region.Id, region.Bounds, region.Label)));
        Assert.Equal(frozenSvg, prepared.ToSvg()); Assert.Equal(frozenPng, prepared.ToPng());
        var subset = chart.ConfigureSunburst(options => options.CurvedLabels = true).PrepareHierarchy(context, new ChartHierarchyView("operations"));
        ScatterBubbleColorRenderingTests.Capture(subset, "sunburst-curved-selected-root-" + width + "-" + mode);
        // The selected root's two equal children centre on vertical tangents. A complete
        // curved run would turn some clusters upside down, so ordinary placement is required.
        Assert.DoesNotContain(subset.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.ContainsKey("data-cfx-caption-placement"));
        Assert.Contains(subset.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.curved-label-fallback"
            && diagnostic.Message.Contains("unfit-geometry", StringComparison.Ordinal));
        var subsetSvg = XDocument.Parse(subset.ToSvg());
        foreach (var item in chart.Series[0].HierarchyItems.Where(item => item.ParentId == "operations")) {
            var owner = Assert.Single(subsetSvg.Descendants(), element => (string?)element.Attribute("data-cfx-target-id") == item.Id);
            var caption = Assert.Single(owner.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "sunburst-label");
            Assert.Equal(item.Label, string.Join(" ", caption.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value)));
        }
        Assert.All(subset.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "sunburst-label"),
            text => Assert.InRange(text.Text.Size, 11, 13));
        AssertArcInkInsideNativeMarks(subset, includeOrdinary: true);
    }

    [Theory]
    [InlineData("joining")]
    [InlineData("decorated")]
    [InlineData("secondary")]
    [InlineData("multiline")]
    public void UnsupportedCurvedRunsUseDiagnosedWholeCaptionFallback(string kind) {
        var chart = Chart.Create().WithLegend(false).WithDataLabels().AddSunburst("Names", new[] {
            new ChartHierarchyItem("root", "All"), new ChartHierarchyItem("target", kind == "joining" ? "العربية" : kind == "multiline" ? "Two\nlines" : "Target", "root", 1)
        }).ConfigureSunburst(options => {
            options.CurvedLabels = true;
            if (kind == "secondary") options.SecondaryLabelFormatter = context => context.Item.Id == "target" ? "Detail" : null;
        });
        if (kind == "decorated") chart.Series[0].ConfigureDataLabelStyle(style => style.Underline = true);
        var prepared = chart.Prepare(Context(520, VisualThemeMode.Light));
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.ContainsKey("data-cfx-caption-placement"));
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.curved-label-fallback");
        Assert.Contains(prepared.Regions, region => region.Id == "series-0-node-target" && region.Label!.Contains(chart.Series[0].HierarchyItems[1].Label, StringComparison.Ordinal));
        if (kind == "secondary") Assert.Contains(prepared.Scene.Nodes, node => node.Role == "sunburst-secondary-label");
    }

    [Fact]
    public void CapturedArcClustersRetainWholeRunGlyphFacesLigaturesOffsetsAndAdvances() {
        var font = Font(); var style = new TextStyle { Font = font, FontSize = 14 };
        var builder = new VisualSceneBuilder(new VisualSize(500, 500), font);
        var captured = builder.PrepareText("office AV fi", style);
        Assert.True(VisualArcTextRuns.TryCreate(captured, out var runs, out var reason), reason);
        Assert.Equal(captured.Lines[0].Text, string.Concat(runs.Select(run => run.Text.Lines[0].Text)));
        Assert.Equal(captured.Lines[0].Glyphs, runs.SelectMany(run => run.Text.Lines[0].Glyphs));
        Assert.All(runs, run => Assert.Same(captured.Face.Font, run.Text.Face.Font));
        var pen = 0d; ChartForgeX.Typography.ShapedGlyph? previous = null;
        foreach (var run in runs) {
            var glyphs = run.Text.Lines[0].Glyphs;
            var adjustment = ChartForgeX.Raster.TrueTypeFont.GlyphAdvance(glyphs[0], previous, captured.Size)
                - ChartForgeX.Raster.TrueTypeFont.GlyphAdvance(glyphs[0], null, captured.Size);
            Assert.Equal(pen + adjustment, run.Offset, 8);
            foreach (var glyph in glyphs) { pen += ChartForgeX.Raster.TrueTypeFont.GlyphAdvance(glyph, previous, captured.Size); previous = glyph; }
        }
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void TreemapPrimaryFloorFitsCompleteNamesBeforeOptionalEllipsisAndKeepsRawFacts(VisualThemeMode mode) {
        const string name = "Regional reconciliation";
        var chart = Chart.Create().WithLegend(false).WithDataLabels().AddTreemap("Teams", new[] {
            new ChartHierarchyItem("root", name), new ChartHierarchyItem("leaf", "Leaf", "root", 7)
        }).ConfigureTreemap(options => options.ShowValues = false);
        chart.Series[0].ConfigureDataLabelStyle(style => style.WithFontSize(16));
        var context = Context(180, mode);
        var original = chart.Prepare(context);
        chart.ConfigureTreemap(options => options.MinimumLabelFontSize = 9);
        var fitted = chart.Prepare(context);
        ScatterBubbleColorRenderingTests.Capture(fitted, "treemap-caption-floor-" + mode);
        var label = Assert.Single(fitted.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "treemap-group-label");
        Assert.Equal(name, Assert.Single(label.Text.Lines).Text); Assert.InRange(label.Text.Size, 9, 15.999);
        Assert.Equal(original.Regions.Select(region => (region.Id, region.Label)), fitted.Regions.Select(region => (region.Id, region.Label)));
        Assert.Equal(7d, chart.Series[0].HierarchyItems[1].Value); Assert.Equal(name, chart.Series[0].HierarchyItems[0].Label);
    }

    [Fact]
    public void TreemapEllipsisKeepsWholeTextElementsAndCompleteSourceFacts() {
        var name = string.Concat(Enumerable.Repeat("North😀e\u0301 region ", 30));
        var chart = Chart.Create().WithLegend(false).WithDataLabels().AddTreemap("Names", new[] {
            new ChartHierarchyItem("root", name), new ChartHierarchyItem("leaf", "Leaf", "root", 7)
        }).ConfigureTreemap(options => { options.MinimumLabelFontSize = 10; options.LabelTrimming = TextTrimming.Ellipsis; });
        var prepared = chart.Prepare(Context(240, VisualThemeMode.Light));
        ScatterBubbleColorRenderingTests.Capture(prepared, "treemap-caption-whole-element-ellipsis");
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "treemap-group-label");
        var displayed = Assert.Single(label.Text.Lines).Text;
        Assert.EndsWith("…", displayed); Assert.NotEqual("…", displayed);
        var prefix = displayed.Substring(0, displayed.Length - 1);
        Assert.StartsWith(prefix, name); Assert.Equal(prefix.Length, TextElementBoundary.Snap(name, prefix.Length));
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.label-overflow");
        var group = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-target-id") == "root");
        Assert.Equal(name, (string?)group.Attribute("data-cfx-full-label")); Assert.Equal("7", (string?)group.Attribute("data-cfx-value"));
    }

    [Fact]
    public void DefaultCaptionOptionsRetainBytesAndInvalidOrWrongFamilyPoliciesReject() {
        var chart = SunburstRecipe.Create().WithLegend(false); var context = Context(520, VisualThemeMode.Light);
        var before = chart.Prepare(context);
        Assert.False(chart.Options.Sunburst.CurvedLabels); chart.ConfigureSunburst(options => options.CurvedLabels = false);
        Assert.Equal(before.ToSvg(), chart.Prepare(context).ToSvg()); Assert.Equal(before.ToPng(), chart.Prepare(context).ToPng());
        var treemap = TreemapRecipe.Create().WithLegend(false); var treemapBefore = treemap.Prepare(context);
        treemap.ConfigureTreemap(value => { value.MinimumLabelFontSize = null; value.LabelTrimming = TextTrimming.None; });
        Assert.Equal(treemapBefore.ToSvg(), treemap.Prepare(context).ToSvg()); Assert.Equal(treemapBefore.ToPng(), treemap.Prepare(context).ToPng());
        var options = new ChartTreemapOptions(); Assert.Null(options.MinimumLabelFontSize); Assert.Equal(TextTrimming.None, options.LabelTrimming);
        foreach (var value in new[] { 0d, -1, double.NaN, double.PositiveInfinity }) Assert.Throws<ArgumentOutOfRangeException>(() => options.MinimumLabelFontSize = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.LabelTrimming = (TextTrimming)42);
        Assert.Throws<InvalidOperationException>(() => Chart.Create().AddLine("Line", new[] { new ChartPoint(0, 1) }).ConfigureSunburst(value => value.CurvedLabels = true).Prepare(context));
        Assert.Throws<InvalidOperationException>(() => Chart.Create().AddLine("Line", new[] { new ChartPoint(0, 1) }).ConfigureTreemap(value => value.LabelTrimming = TextTrimming.Ellipsis).Prepare(context));
    }

    internal static VisualRenderContext Context(int width, VisualThemeMode mode, string? family = null) => new(
        new VisualLayoutOptions(new VisualSize(width, 520)), VisualTheme.Graphite(), mode,
        new VisualFrame(showLegend: false), family == null ? Font() : FontSpec.FromFamily(family));

    private static FontSpec Font() => new() { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") };

    private static void AssertArcInkInsideNativeMarks(PreparedVisual prepared, bool includeOrdinary = false) {
        var groups = new Stack<VisualSceneGroup>(); var transforms = new Stack<VisualSceneTransform>(); var transform = VisualSceneTransform.Identity;
        var marks = new Dictionary<string, VisualSceneMark>(); var checkedRuns = 0;
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) {
                groups.Push(group); transforms.Push(transform);
                if (group.Rotation.HasValue) transform = transform.Rotate(group.Rotation.Value);
                if (group.Translation.HasValue) transform = transform.Translate(group.Translation.Value);
                continue;
            }
            if (node is VisualSceneEndGroup) { groups.Pop(); transform = transforms.Pop(); continue; }
            var owner = groups.FirstOrDefault(group => group.Role == "sunburst-segment");
            if (owner == null) continue;
            if (node is VisualSceneMark mark && node.Role == "sunburst-segment-mark") { marks[owner.Id!] = mark; continue; }
            if (node is not VisualSceneText text || text.Role != "sunburst-label-run" && (!includeOrdinary || text.Role != "sunburst-label")) continue;
            var shape = marks[owner.Id!] is VisualScenePath path ? VisualSceneGeometry.Flatten(path, 8) : VisualSceneGeometry.Flatten((VisualSceneSlice)marks[owner.Id!], 8);
            for (var lineIndex = 0; lineIndex < text.Text.Lines.Count; lineIndex++) {
                var line = text.Text.Lines[lineIndex];
                var ink = text.Text.Face.Font!.MeasureGlyphInk(line.Glyphs, text.Text.Size, text.Text.Face.SynthesizeItalic);
                if (!ink.HasValue) continue;
                checkedRuns++;
                var box = ink.Value;
                foreach (var x in new[] { box.X, box.Right }) foreach (var y in new[] { box.Y, box.Bottom }) {
                    var point = transform.Apply(new ChartPoint(text.LineLeft(line) + x,
                        text.Baseline + lineIndex * text.Text.Metrics.LineHeight - text.Text.Ascent + y));
                    Assert.True(new LabelMarkShape(shape, true, 0).Contains(new ChartRect(point.X - .01, point.Y - .01, .02, .02)), owner.Metadata["data-cfx-full-label"]);
                }
            }
        }
        Assert.True(checkedRuns > 0, "At least one captured glyph run must be checked against its actual native mark.");
    }
}
