using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SunburstLabelLayoutTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void BroadThreeLevelHierarchyKeepsEveryFullCaptionInsideItsOwnSegment(VisualThemeMode mode) {
        var chart = Teams();
        chart.Series[0].WithDataLabelStyle(style => style.WithFontSize(13));
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(fontPath), "The existing Carlito validation fixture must be available.");
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440)), themeMode: mode,
            frame: new VisualFrame("Allocation by team", "Share of the supplied hierarchy", showLegend: false),
            font: new FontSpec { Family = "Carlito", FilePath = fontPath });
        var prepared = chart.Prepare(context);
        var groups = new Stack<VisualSceneGroup>();
        var labels = new Dictionary<string, string>();
        var marks = new Dictionary<string, VisualSceneSlice>();
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) { groups.Push(group); continue; }
            if (node is VisualSceneEndGroup) { groups.Pop(); continue; }
            var owner = groups.FirstOrDefault(group => group.Role == "sunburst-segment");
            if (owner == null) continue;
            if (node is VisualSceneSlice mark) marks.Add(owner.Id!, mark);
            if (node is not VisualSceneText text || text.Role != "sunburst-label") continue;
            var original = owner.Metadata["data-cfx-label"];
            labels.Add(owner.Id!, string.Join(" ", text.Text.Lines.Select(line => line.Text)));
            Assert.Equal(original, labels[owner.Id!]);
            Assert.Equal(13, text.Text.Size);
            var contours = VisualSceneGeometry.Flatten(marks[owner.Id!], 8);
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
        Assert.Equal(chart.Options.TreeNodeLabels.Count, labels.Count);
        Assert.Equal(labels.Count, prepared.Regions.Count(region => region.Role == "sunburst-segment"));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.label-overflow");
    }

    [Fact]
    public void ExplicitlyHiddenCaptionsRetainHierarchySemantics() {
        var chart = Teams(); chart.Series[0].WithDataLabels(false);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440)),
            frame: new VisualFrame(showLegend: false)));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "sunburst-label");
        Assert.Equal(chart.Options.TreeNodeLabels.Count, prepared.Regions.Count(region => region.Role == "sunburst-segment"));
        Assert.All(chart.Options.TreeNodeLabels, label => Assert.Contains(prepared.Regions, region => region.Label!.StartsWith(label + ":", StringComparison.Ordinal)));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.label-overflow");
    }

    private static Chart Teams() => Chart.Create().AddSunburst("Teams", new[] {
        new ChartTreeLink("All teams", "Engineering", 60), new ChartTreeLink("All teams", "Operations", 40),
        new ChartTreeLink("Engineering", "Platform", 35), new ChartTreeLink("Engineering", "Services", 25),
        new ChartTreeLink("Operations", "Support", 40)
    });
}
