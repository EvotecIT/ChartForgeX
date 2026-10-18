using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SunburstLabelLayoutTests {
    [Theory]
    [InlineData(VisualThemeMode.Light, 0)]
    [InlineData(VisualThemeMode.Dark, 0)]
    [InlineData(VisualThemeMode.Light, 3)]
    [InlineData(VisualThemeMode.Dark, 3)]
    public void NarrowSectorKeepsCaptionWhenAngularEdgesRequireOuterRadialLane(VisualThemeMode mode, double cornerRadius) {
        var chart = Chart.Create().WithDataLabels().AddSunburst("Teams", new[] {
            new ChartHierarchyItem("root", "All"),
            new ChartHierarchyItem("before", "Before", "root", 21),
            new ChartHierarchyItem("team", "Team", "root"),
            new ChartHierarchyItem("services", "Services", "team"),
            new ChartHierarchyItem("online", "Online", "services", 8),
            new ChartHierarchyItem("after", "After", "root", 71)
        }).ConfigureSunburst(options => options.CornerRadius = cornerRadius);
        chart.Series[0].ConfigureDataLabelStyle(style => style.WithFontSize(12));
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 400)),
            themeMode: mode, frame: new VisualFrame(showLegend: false),
            font: new FontSpec { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") }));
        ScatterBubbleColorRenderingTests.Capture(prepared, "sunburst-angular-lane-" + mode + "-" + cornerRadius);
        Assert.Contains("Services", AssertCaptionsInsideSegments(prepared, 12).Values);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void WideChartWithHeadingAndColorKeyKeepsCompleteInnerRingNames(VisualThemeMode mode) {
        var chart = SunburstRecipe.Create("options", mode).ConfigureDataLabelStyle(style => style.WithFontSize(12));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440)), themeMode: mode,
            frame: new VisualFrame(V2GalleryModels.Title(ChartSeriesKind.Sunburst), "Leaf totals size sectors; measured color stays independent"),
            font: new FontSpec { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") });
        var prepared = chart.Prepare(context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "sunburst-framed-color-key-" + mode);
        var names = AssertCaptionsInsideSegments(prepared, 12).Values.ToArray();
        Assert.Contains("Engineering", names);
        Assert.Contains("Operations", names);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "sunburst-color-scale");
    }

    [Theory]
    [InlineData(VisualThemeMode.Light, 0)]
    [InlineData(VisualThemeMode.Dark, 0)]
    [InlineData(VisualThemeMode.Light, 6)]
    [InlineData(VisualThemeMode.Dark, 6)]
    public void BroadThreeLevelHierarchyKeepsEveryFullCaptionInsideItsOwnSegment(VisualThemeMode mode, double cornerRadius) {
        var chart = Teams();
        chart.ConfigureSunburst(options => options.CornerRadius = cornerRadius);
        chart.Series[0].ConfigureDataLabelStyle(style => style.WithFontSize(13));
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(fontPath), "The existing Carlito validation fixture must be available.");
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440)), themeMode: mode,
            frame: new VisualFrame("Allocation by team", "Share of the supplied hierarchy", showLegend: false),
            font: new FontSpec { Family = "Carlito", FilePath = fontPath });
        var prepared = chart.Prepare(context);
        var labels = AssertCaptionsInsideSegments(prepared, 13);
        Assert.Equal(chart.Series[0].Nodes.Count, labels.Count);
        Assert.Equal(labels.Count, prepared.Regions.Count(region => region.Role == "sunburst-segment"));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.label-overflow");
    }

    private static Dictionary<string, string> AssertCaptionsInsideSegments(PreparedVisual prepared, double fontSize) {
        var groups = new Stack<VisualSceneGroup>();
        var labels = new Dictionary<string, string>();
        var marks = new Dictionary<string, VisualSceneMark>();
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) { groups.Push(group); continue; }
            if (node is VisualSceneEndGroup) { groups.Pop(); continue; }
            var owner = groups.FirstOrDefault(group => group.Role == "sunburst-segment");
            if (owner == null) continue;
            if (node is VisualSceneSlice or VisualScenePath) marks.Add(owner.Id!, (VisualSceneMark)node);
            if (node is not VisualSceneText text || text.Role != "sunburst-label") continue;
            var original = owner.Metadata["data-cfx-label"];
            labels.Add(owner.Id!, string.Join(" ", text.Text.Lines.Select(line => line.Text)));
            Assert.Equal(original, labels[owner.Id!]);
            Assert.Equal(fontSize, text.Text.Size);
            var contours = marks[owner.Id!] is VisualScenePath path ? VisualSceneGeometry.Flatten(path, 8)
                : VisualSceneGeometry.Flatten((VisualSceneSlice)marks[owner.Id!], 8);
            var rotation = groups.Select(group => group.Rotation).FirstOrDefault(value => value.HasValue);
            if (rotation.HasValue) {
                Assert.InRange(rotation.Value.Degrees, -90, 90);
                var radians = -rotation.Value.Degrees * Math.PI / 180;
                foreach (var contour in contours) for (var index = 0; index < contour.Count; index++) {
                    var point = contour[index]; var dx = point.X - rotation.Value.X; var dy = point.Y - rotation.Value.Y;
                    contour[index] = new ChartPoint(rotation.Value.X + dx * Math.Cos(radians) - dy * Math.Sin(radians),
                        rotation.Value.Y + dx * Math.Sin(radians) + dy * Math.Cos(radians));
                }
            }
            var caption = new ChartRect(text.X - text.Text.Metrics.Width / 2, text.Baseline - text.Text.Ascent,
                text.Text.Metrics.Width, text.Text.Metrics.Height);
            Assert.True(new LabelMarkShape(contours, true, 0).Contains(caption), original + " must fit its actual rendered segment.");
        }
        return labels;
    }

    [Fact]
    public void ExplicitlyHiddenCaptionsRetainHierarchySemantics() {
        var chart = Teams(); chart.Series[0].WithDataLabels(false);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440)),
            frame: new VisualFrame(showLegend: false)));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "sunburst-label");
        Assert.Equal(chart.Series[0].Nodes.Count, prepared.Regions.Count(region => region.Role == "sunburst-segment"));
        Assert.All(chart.Series[0].Nodes.Select(node => node.Label), label => Assert.Contains(prepared.Regions, region => region.Label!.StartsWith(label + ":", StringComparison.Ordinal)));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.label-overflow");
    }

    private static Chart Teams() => Chart.Create().WithDataLabels().AddSunburst("Teams", new[] { new ChartHierarchyItem("All teams", "All teams"), new ChartHierarchyItem("Engineering", "Engineering", "All teams", 60), new ChartHierarchyItem("Operations", "Operations", "All teams", 40), new ChartHierarchyItem("Platform", "Platform", "Engineering", 35), new ChartHierarchyItem("Services", "Services", "Engineering", 25), new ChartHierarchyItem("Support", "Support", "Operations", 40) });
}
