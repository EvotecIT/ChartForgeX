using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Dense leaves must not erase readable captions on the rest of a native tree.</summary>
public sealed class TreeLabelReadabilityTests {
    [Theory]
    [InlineData(390, 420, false)]
    [InlineData(760, 420, false)]
    [InlineData(760, 700, false)]
    [InlineData(390, 420, true)]
    [InlineData(760, 420, true)]
    [InlineData(760, 700, true)]
    public void DenseTreeKeepsAvailableParentCaptionsAndUsesTallerLeafSpace(int width, int height, bool longNames) {
        var chart = Fixture(longNames); var context = Context(width, height);
        var sourceNodes = chart.Series[0].Nodes.ToArray(); var sourceLinks = chart.Series[0].TreeLinks.ToArray();
        var prepared = chart.Prepare(context); var document = XDocument.Parse(prepared.ToSvg());
        var targets = Targets(document); var labels = Captions(prepared);
        Assert.Equal(22, targets.Length); Assert.Equal(21, Roles(document, "tree-link").Length);
        Assert.Equal(sourceNodes, chart.Series[0].Nodes); Assert.Equal(sourceLinks, chart.Series[0].TreeLinks);
        foreach (var source in sourceNodes) {
            var target = targets.Single(element => (string?)element.Attribute("data-cfx-node") == source.Id);
            Assert.Equal(source.Label, (string?)target.Attribute("data-cfx-label"));
            Assert.Equal("tree-source", (string?)target.Attribute("data-cfx-series-key"));
            Assert.Contains(source.Label, Region(prepared, target).Label!, StringComparison.Ordinal);
            if (source.Id == "root" || sourceLinks.Any(link => link.ParentId == source.Id))
                Assert.True(target.Descendants().Any(element => element.Name.LocalName == "text"), "A dense leaf level must not hide a parent caption that has room at its own depth.");
        }
        if (height == 700) Assert.Equal(22, labels.Length);
        else Assert.True(labels.Length >= 4, "The root and all three group captions have independent vertical space.");
        foreach (var label in labels) {
            Assert.Equal(12, label.Text.Size);
            var bounds = TextBounds(label);
            Assert.Single(prepared.Regions, region => region.Role == "tree-node" && LabelPlacementService.Contains(region.Bounds, bounds));
        }
        AssertNodeAndBranchGeometry(prepared, document, sourceLinks, width, height, context.Theme.AxisStrokeWidth);
        AssertNativeCaptionInk(prepared, labels);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"tree-readable-{longNames}-{width}x{height}");
    }

    [Fact]
    public void CaptionSizingPreservesAuthoredTypePaintAndRecursiveCenters() {
        var chart = Fixture(false); var context = Context(760, 420);
        var ink = ChartColor.FromHex("#9B164B"); var fill = ChartColor.FromHex("#27A5B1");
        chart.Series[0].WithPointColor(0, fill)
            .ConfigurePointDataLabelStyle(0, style => style.WithFontSize(14).WithColor(ink))
            .ConfigurePointDataLabelStyle(1, style => style.WithFontSize(8));
        var prepared = chart.Prepare(context); var document = XDocument.Parse(prepared.ToSvg());
        var targets = Targets(document); var root = targets.Single(element => (string?)element.Attribute("data-cfx-node") == "root");
        var group = targets.Single(element => (string?)element.Attribute("data-cfx-node") == "G0");
        var rootText = Assert.Single(Captions(prepared), text => LabelPlacementService.Contains(Region(prepared, root).Bounds, TextBounds(text)));
        var groupText = Assert.Single(Captions(prepared), text => LabelPlacementService.Contains(Region(prepared, group).Bounds, TextBounds(text)));
        Assert.Equal(14, rootText.Text.Size); Assert.Equal(ink, rootText.Color);
        Assert.Equal(8, groupText.Text.Size);
        Assert.Equal(fill.ToCss(), (string?)root.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "tree-node-mark").Attribute("fill"));

        chart.WithDataLabels(false);
        var withoutCaptions = chart.Prepare(context);
        foreach (var node in prepared.Regions.Where(region => region.Role == "tree-node")) {
            var baseline = withoutCaptions.Regions.Single(region => region.Id == node.Id);
            Assert.Equal(baseline.Bounds.Left, node.Bounds.Left, 8);
            Assert.Equal(baseline.Bounds.Width, node.Bounds.Width, 8);
            Assert.Equal(baseline.Bounds.Top + baseline.Bounds.Height / 2, node.Bounds.Top + node.Bounds.Height / 2, 8);
        }
        AssertNodeAndBranchGeometry(prepared, document, chart.Series[0].TreeLinks, 760, 420, context.Theme.AxisStrokeWidth);
        AssertNativeCaptionInk(prepared, new[] { rootText, groupText });
    }

    private static void AssertNodeAndBranchGeometry(PreparedVisual prepared, XDocument document,
        IReadOnlyList<ChartTreeLink> links, int width, int height, double stroke) {
        var targets = Targets(document);
        var bounds = targets.ToDictionary(element => (string)element.Attribute("data-cfx-node")!, element => Region(prepared, element).Bounds);
        foreach (var box in bounds.Values) {
            Assert.True(box.Width > 0 && box.Height > 0);
            Assert.True(box.Left - stroke / 2 >= 0 && box.Top - stroke / 2 >= 0 && box.Right + stroke / 2 <= width && box.Bottom + stroke / 2 <= height);
        }
        foreach (var level in targets.GroupBy(element => (string)element.Attribute("data-cfx-depth")!)) {
            var rows = level.Select(element => Region(prepared, element).Bounds).OrderBy(box => box.Top).ToArray();
            for (var index = 1; index < rows.Length; index++)
                Assert.True(rows[index - 1].Bottom + stroke <= rows[index].Top, "Painted peers must remain separated when their caption bodies grow.");
        }
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "tree-link-path").ToArray();
        Assert.Equal(links.Count, paths.Length);
        for (var index = 0; index < links.Count; index++) {
            var source = bounds[links[index].ParentId]; var target = bounds[links[index].ChildId];
            var first = paths[index].Commands[0]; var last = paths[index].Commands[paths[index].Commands.Count - 1];
            Assert.Equal(source.Right, first.X, 8); Assert.Equal(source.Top + source.Height / 2, first.Y, 8);
            Assert.Equal(target.Left, last.X, 8); Assert.Equal(target.Top + target.Height / 2, last.Y, 8);
        }
    }

    private static void AssertNativeCaptionInk(PreparedVisual prepared, IEnumerable<VisualSceneText> captions) {
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        foreach (var caption in captions) {
            var scene = prepared.Scene;
            var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size,
                scene.Nodes.Where(node => !ReferenceEquals(node, caption)).ToArray(), scene.Diagnostics, scene.Regions), supersampling: 1);
            var bounds = TextBounds(caption); bool painted = false;
            for (int y = Math.Max(0, (int)Math.Floor(bounds.Top)); y < Math.Min(image.Height, (int)Math.Ceiling(bounds.Bottom)) && !painted; y++)
                for (int x = Math.Max(0, (int)Math.Floor(bounds.Left)); x < Math.Min(image.Width, (int)Math.Ceiling(bounds.Right)) && !painted; x++) {
                    int offset = (y * image.Width + x) * 4;
                    for (int channel = 0; channel < 4; channel++)
                        if (image.Pixels[offset + channel] != without.Pixels[offset + channel]) { painted = true; break; }
                }
            Assert.True(painted, "The readable caption must reach the native raster painter inside its node.");
        }
    }

    private static ChartRect TextBounds(VisualSceneText text) => new(text.Text.Lines.Min(line => text.LineLeft(line)),
        text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "tree-node-label").ToArray();
    private static XElement[] Targets(XDocument document) => Roles(document, "tree-node");
    private static XElement[] Roles(XDocument document, string role) => document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    private static VisualSemanticRegion Region(PreparedVisual prepared, XElement element) => prepared.Regions.Single(region => region.Id == (string?)element.Attribute("data-cfx-source-id"));

    private static VisualRenderContext Context(int width, int height) {
        string font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font), "The existing native font fixture must be available.");
        return new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), VisualThemeMode.Light,
            new VisualFrame("Regional processing structure", showLegend: false), FontSpec.FromFile(font));
    }

    private static Chart Fixture(bool longNames) {
        var nodes = new List<ChartNode> { new("root", longNames ? "Regional operations and reconciliation" : "Root") };
        var links = new List<ChartTreeLink>();
        for (int group = 0; group < 3; group++) {
            string groupId = "G" + group;
            nodes.Add(new(groupId, longNames ? "Regional processing group " + (group + 1) : "Group" + (group + 1)));
            links.Add(new("root", groupId));
            for (int leaf = 0; leaf < 6; leaf++) {
                string id = groupId + "-L" + leaf;
                nodes.Add(new(id, (longNames ? "Regional processing and reconciliation batch " : "Task") + (leaf + 1).ToString("00")));
                links.Add(new(groupId, id));
            }
        }
        return Chart.Create().WithLegend(false).WithDataLabels().AddSeries(ChartSeries.CreateTree(new ChartSeriesId("tree-source"), "Processing structure", nodes, links));
    }
}
