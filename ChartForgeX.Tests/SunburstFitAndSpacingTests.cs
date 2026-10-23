using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SunburstFitAndSpacingTests {
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 5)]
    public void NullFloorAndZeroGapPreserveNativeBytesAndNeverEnlargeAuthoredType(bool dark, double corner) {
        var chart = Source().ConfigureSunburst(options => options.CornerRadius = corner);
        chart.ConfigureDataLabelStyle(style => style.WithFontSize(12));
        var original = chart.PrepareHierarchy(Context(dark), new ChartHierarchyView("services"));
        chart.ConfigureSunburst(options => { options.MinimumLabelFontSize = null; options.SectorGap = 0; });
        var explicitDefaults = chart.PrepareHierarchy(Context(dark), new ChartHierarchyView("services"));
        Assert.Equal(original.ToSvg(), explicitDefaults.ToSvg());
        Assert.Equal(original.ToPng(), explicitDefaults.ToPng());
        chart.ConfigureSunburst(options => options.MinimumLabelFontSize = 40);
        var higherFloor = chart.PrepareHierarchy(Context(dark), new ChartHierarchyView("services"));
        Assert.Equal(original.ToSvg(), higherFloor.ToSvg());
        Assert.Equal(original.ToPng(), higherFloor.ToPng());
        Assert.DoesNotContain(higherFloor.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.ContainsKey("data-cfx-paint-sweep"));
    }

    [Fact]
    public void OptionBoundsAreExplicitAndActiveSunburstPoliciesRejectOtherFamilies() {
        var options = new ChartSunburstOptions(); Assert.Null(options.MinimumLabelFontSize); Assert.Equal(0, options.SectorGap);
        foreach (var invalid in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
            Assert.Throws<ArgumentOutOfRangeException>(() => options.SectorGap = invalid);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.MinimumLabelFontSize = invalid);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MinimumLabelFontSize = 0);
        options.MinimumLabelFontSize = .5; options.MinimumLabelFontSize = null; options.SectorGap = 0;
        var other = Chart.Create().AddBar("Work", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        other.ConfigureSunburst(sunburst => sunburst.SectorGap = 2);
        Assert.Throws<InvalidOperationException>(() => other.Prepare(Context(false)));
        other.ConfigureSunburst(sunburst => { sunburst.SectorGap = 0; sunburst.MinimumLabelFontSize = 9; });
        Assert.Throws<InvalidOperationException>(() => other.Prepare(Context(false)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OptInFloorRescuesCompletePrimaryWithoutRepeatedFormatterCallsOrEscapingPaint(bool dark) {
        var chart = Source().ConfigureDataLabelStyle(style => style.WithFontSize(30));
        var fixedSize = chart.Prepare(Context(dark));
        Assert.DoesNotContain(Texts(fixedSize, "root"), text => text.Role == "sunburst-label");
        var calls = 0;
        chart.ConfigureSunburst(options => {
            options.MinimumLabelFontSize = 10; options.SectorGap = 4; options.CornerRadius = 4;
            options.SecondaryLabelFormatter = context => { calls++; return "Team " + context.Item.Id; };
        });
        var fitted = chart.Prepare(Context(dark));
        var primary = Assert.Single(Texts(fitted, "root"), text => text.Role == "sunburst-label");
        Assert.Equal("Infrastructure", string.Concat(primary.Text.Lines.Select(line => line.Text)));
        Assert.InRange(primary.Text.Size, 10, 29.999);
        Assert.Equal(chart.Series[0].HierarchyItems.Count, calls);
        AssertCaptionInsideNativeMark(fitted, "root");
        var svg = fitted.ToSvg(); var png = fitted.ToPng();
        chart.ConfigureSunburst(options => {
            options.MinimumLabelFontSize = 3; options.SectorGap = 10000;
            options.SecondaryLabelFormatter = _ => throw new InvalidOperationException("Prepared export must not call formatters.");
        });
        Assert.Equal(svg, fitted.ToSvg()); Assert.Equal(png, fitted.ToPng());
        Assert.Equal(chart.Series[0].HierarchyItems.Count, calls);
        ScatterBubbleColorRenderingTests.Capture(fitted, "sunburst-fitted-gap-" + dark);
    }

    [Fact]
    public void SecondaryTextCannotForceAFittingPrimaryToShrinkAndImpossibleFloorsKeepFacts() {
        var chart = Source().ConfigureDataLabelStyle(style => style.WithFontSize(12));
        chart.ConfigureSunburst(options => {
            options.MinimumLabelFontSize = 8;
            options.SecondaryLabelFormatter = _ => new string('W', 250);
            options.SecondaryLabelStyle.WithFontSize(35);
        });
        var prepared = chart.PrepareHierarchy(Context(false), new ChartHierarchyView("api"));
        var primary = Assert.Single(Texts(prepared, "api"), text => text.Role == "sunburst-label");
        Assert.Equal(12, primary.Text.Size);
        Assert.Equal("API", Assert.Single(primary.Text.Lines).Text);
        chart.Series[0].WithPointLabel(0, new string('W', 250));
        var impossible = chart.Prepare(Context(false));
        Assert.DoesNotContain(Texts(impossible, "root"), text => text.Role == "sunburst-label");
        Assert.Equal(new string('W', 250), Groups(impossible)["root"].Metadata["data-cfx-full-label"]);
        Assert.Contains(impossible.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.label-overflow");
        chart.ConfigureSunburst(options => { options.SecondaryLabelFormatter = _ => "Team"; options.SecondaryLabelStyle.FontSize = 20; });
        var explicitSecondary = chart.PrepareHierarchy(Context(false), new ChartHierarchyView("api"));
        Assert.Equal(20, Assert.Single(Texts(explicitSecondary, "api"), text => text.Role == "sunburst-secondary-label").Text.Size);
        chart.ConfigureSunburst(options => options.SecondaryLabelStyle.FontSize = null);
        var inheritedSecondary = chart.PrepareHierarchy(Context(false), new ChartHierarchyView("api"));
        Assert.Equal(10, Assert.Single(Texts(inheritedSecondary, "api"), text => text.Role == "sunburst-secondary-label").Text.Size);
    }

    [Fact]
    public void RoundedPatternUsesExactlyTheInsetNativeMarkContour() {
        var chart = Source().WithDataLabels(false).ConfigureSunburst(options => { options.SectorGap = 8; options.CornerRadius = 6; });
        chart.Series[0].WithPointFillPattern(1, ChartFillPattern.DiagonalForward).WithNodeState("services", ChartSeriesState.Warning);
        var prepared = chart.Prepare(Context(false));
        var mark = Assert.IsType<VisualScenePath>(Mark(prepared, "services"));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.PathClip != null && group.PathClip.Commands.SequenceEqual(mark.Commands));
        Assert.Contains(Owned(prepared, "services"), node => node.Role == "sunburst-pattern");
        Assert.Equal("Warning", Groups(prepared)["services"].Metadata["data-cfx-state"]);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 5)]
    public void PaintedGapPreservesAllocationAndAuthoredRemaindersAcrossFullAndSubtreeViews(bool dark, double corner) {
        var chart = Source().WithDataLabels(false).ConfigureSunburst(options => options.CornerRadius = corner);
        var original = Groups(chart.Prepare(Context(dark)));
        chart.ConfigureSunburst(options => options.SectorGap = 8);
        var prepared = chart.Prepare(Context(dark)); var spaced = Groups(prepared);
        foreach (var id in original.Keys) foreach (var key in new[] { "data-cfx-start-angle", "data-cfx-sweep", "data-cfx-inner-radius", "data-cfx-outer-radius",
            "data-cfx-value", "data-cfx-authored-value", "data-cfx-percent", "data-cfx-remainder-value", "data-cfx-parent", "data-cfx-depth" }) {
            Assert.Equal(original[id].Metadata.TryGetValue(key, out var before), spaced[id].Metadata.TryGetValue(key, out var after));
            Assert.Equal(before, after);
        }
        var root = spaced["root"].Metadata; var services = spaced["services"].Metadata;
        Assert.Equal(Number(root, "data-cfx-start-angle"), Number(root, "data-cfx-paint-start-angle"));
        Assert.Equal(Math.PI * 2, Number(root, "data-cfx-paint-sweep"), 12);
        Assert.Equal(8, Number(services, "data-cfx-paint-inner-radius") - Number(root, "data-cfx-paint-outer-radius"), 10);
        var inset = Number(services, "data-cfx-paint-start-angle") - Number(services, "data-cfx-start-angle");
        Assert.Equal(8, 2 * Number(services, "data-cfx-paint-inner-radius") * Math.Sin(inset), 10);
        Assert.Equal("30", services["data-cfx-remainder-value"]);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneMark>(), mark => {
            if (mark is VisualSceneSlice slice) Assert.True(slice.Outer > slice.Inner && slice.Sweep > 0);
        });
        var subtree = chart.PrepareHierarchy(Context(dark), new ChartHierarchyView("services"));
        var selected = Groups(subtree)["services"].Metadata;
        Assert.Equal(0, Number(selected, "data-cfx-paint-inner-radius"));
        Assert.Equal(Math.PI * 2, Number(selected, "data-cfx-paint-sweep"), 12);
        Assert.Equal("80", selected["data-cfx-value"]); Assert.Equal("30", selected["data-cfx-remainder-value"]);
        Assert.Equal("root", selected["data-cfx-parent"]);
        ScatterBubbleColorRenderingTests.Capture(prepared, "sunburst-spaced-full-" + dark + "-" + corner);
        ScatterBubbleColorRenderingTests.Capture(subtree, "sunburst-spaced-subtree-" + dark + "-" + corner);
    }

    [Fact]
    public void FullCircleChildAndSingleRootKeepClosedGeometryWithoutAnArtificialSeam() {
        var chart = Chart.Create().WithLegend(false).AddSunburst("One branch", new[] {
            new ChartHierarchyItem("root", "Root"), new ChartHierarchyItem("leaf", "Leaf", "root", 4)
        }).ConfigureSunburst(options => { options.SectorGap = 8; options.CornerRadius = 20; });
        var prepared = chart.Prepare(Context(false));
        Assert.All(Groups(prepared).Values, group => Assert.Equal(Math.PI * 2, Number(group.Metadata, "data-cfx-paint-sweep"), 12));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneSlice>().Count(slice => slice.Role == "sunburst-segment-mark"));
        var leaf = chart.PrepareHierarchy(Context(false), new ChartHierarchyView("leaf"));
        var disk = Assert.Single(leaf.Scene.Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "sunburst-segment-mark");
        Assert.Equal(0, disk.Inner); Assert.Equal(Math.PI * 2, disk.Sweep, 12);
    }

    [Fact]
    public void ZeroThinAndExhaustedInsetsStayTruthfulAndNeverProduceInvertedOrInteractiveGeometry() {
        var chart = Source().WithDataLabels(false).ConfigureSunburst(options => options.CornerRadius = 3);
        var before = Groups(chart.Prepare(Context(false)));
        Assert.Equal("precision-collapse", before["tiny"].Metadata["data-cfx-geometry-status"]);
        chart.ConfigureSunburst(options => options.SectorGap = 12);
        var prepared = chart.Prepare(Context(false)); var groups = Groups(prepared);
        Assert.Equal("zero", groups["zero"].Metadata["data-cfx-geometry-status"]);
        Assert.Equal("padding-collapse", groups["thin"].Metadata["data-cfx-geometry-status"]);
        Assert.Equal("precision-collapse", groups["tiny"].Metadata["data-cfx-geometry-status"]);
        Assert.Equal("1E-20", groups["tiny"].Metadata["data-cfx-value"]);
        foreach (var id in new[] { "zero", "thin", "tiny" }) {
            Assert.Equal("true", groups[id].Metadata["data-cfx-passive"]);
            Assert.Equal("false", groups[id].Metadata["data-cfx-visible"]);
            Assert.Empty(Texts(prepared, id)); Assert.Null(Mark(prepared, id));
        }
        chart.ConfigureSunburst(options => options.SectorGap = double.MaxValue);
        var exhausted = chart.Prepare(Context(false));
        Assert.DoesNotContain(exhausted.Scene.Nodes, node => node.Role == "sunburst-segment-mark");
        Assert.All(Groups(exhausted).Values.Where(group => group.Metadata["data-cfx-value"] != "0" && group.Metadata["data-cfx-node"] != "tiny"),
            group => Assert.Equal("padding-collapse", group.Metadata["data-cfx-geometry-status"]));
        Assert.DoesNotContain("NaN", exhausted.ToSvg()); Assert.DoesNotContain("Infinity", exhausted.ToSvg());
        Assert.NotEmpty(exhausted.ToPng());
    }

    private static Chart Source() => Chart.Create().WithLegend(false).WithDataLabels().AddSunburst("Allocation", new[] {
        new ChartHierarchyItem("root", "Infrastructure", value: 120),
        new ChartHierarchyItem("services", "Services", "root", 80),
        new ChartHierarchyItem("api", "API", "services", 30), new ChartHierarchyItem("worker", "Worker", "services", 20),
        new ChartHierarchyItem("other", "Other", "root", 20), new ChartHierarchyItem("zero", "Reserve", "root", 0),
        new ChartHierarchyItem("thin", "Thin", "root", .01), new ChartHierarchyItem("tiny", "Tiny", "root", 1e-20)
    }).ConfigureSunburst(options => options.ParentValuePolicy = ChartHierarchyValuePolicy.AuthoredTotal);

    private static VisualRenderContext Context(bool dark) => new(new VisualLayoutOptions(new VisualSize(390, 420)),
        themeMode: dark ? VisualThemeMode.Dark : VisualThemeMode.Light, frame: new VisualFrame(showLegend: false),
        font: new FontSpec { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") });

    private static Dictionary<string, VisualSceneGroup> Groups(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>()
        .Where(group => group.Role == "sunburst-segment").ToDictionary(group => group.Metadata["data-cfx-node"]);
    private static double Number(IReadOnlyDictionary<string, string> metadata, string key) => double.Parse(metadata[key], CultureInfo.InvariantCulture);
    private static IEnumerable<VisualSceneNode> Owned(PreparedVisual prepared, string id) {
        var owners = new Stack<VisualSceneGroup>();
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) { owners.Push(group); continue; }
            if (node is VisualSceneEndGroup) { owners.Pop(); continue; }
            if (owners.Any(group => group.Role == "sunburst-segment" && group.Metadata["data-cfx-node"] == id)) yield return node;
        }
    }
    private static IEnumerable<VisualSceneText> Texts(PreparedVisual prepared, string id) => Owned(prepared, id).OfType<VisualSceneText>();
    private static VisualSceneMark? Mark(PreparedVisual prepared, string id) => Owned(prepared, id).OfType<VisualSceneMark>()
        .SingleOrDefault(mark => mark.Role == "sunburst-segment-mark");
    private static void AssertCaptionInsideNativeMark(PreparedVisual prepared, string id) {
        var mark = Assert.IsAssignableFrom<VisualSceneMark>(Mark(prepared, id));
        var contours = mark is VisualScenePath path ? VisualSceneGeometry.Flatten(path, 8)
            : VisualSceneGeometry.Flatten(Assert.IsType<VisualSceneSlice>(mark), 8);
        var text = Assert.Single(Texts(prepared, id), text => text.Role == "sunburst-label");
        Assert.True(new LabelMarkShape(contours, true, 0).Contains(new ChartRect(text.X - text.Text.Metrics.Width / 2,
            text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height)));
    }
}
