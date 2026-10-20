using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Complete authored names use spare hierarchy rows before losing identifying words.</summary>
public sealed class HierarchyCompleteCaptionTests {
    private const string Name = "Regional processing reconciliation and settlement allocation North America";

    [Theory]
    [InlineData(VisualThemeMode.Light, false)]
    [InlineData(VisualThemeMode.Dark, false)]
    [InlineData(VisualThemeMode.Light, true)]
    [InlineData(VisualThemeMode.Dark, true)]
    public void TreeUsesAvailableThirdRowWithoutMovingNodeCenters(VisualThemeMode mode, bool dense) {
        var names = dense ? new[] { Name, Name.Replace("North America", "South America"), Name.Replace("North America", "Western Europe") } : new[] { Name };
        var chart = Tree(dense);
        var prepared = chart.Prepare(Context(760, 420, mode));
        ScatterBubbleColorRenderingTests.Capture(prepared, "tree-complete-three-row-" + mode + (dense ? "-dense" : ""));
        for (var group = 0; group < names.Length; group++) {
            var id = dense ? "group" + group : "group";
            var caption = Caption(prepared, id, "tree-node-label");
            Assert.Equal(names[group], string.Join(" ", caption.Text.Lines.Select(line => line.Text)));
            Assert.Equal(3, caption.Text.Lines.Count);
            Assert.Equal(12, caption.Text.Size);
            var region = prepared.Regions.Single(region => region.Id == "series-0-node-" + id);
            Assert.True(LabelPlacementService.Contains(region.Bounds, Bounds(caption)));
            AssertCaptionInk(prepared, caption, region.Bounds);
        }
        foreach (var leaf in chart.Series[0].Nodes.Where(node => node.Label.StartsWith("Region ", StringComparison.Ordinal))) {
            var caption = Caption(prepared, leaf.Id, "tree-node-label");
            Assert.Equal(leaf.Label, Assert.Single(caption.Text.Lines).Text);
            Assert.Equal(12, caption.Text.Size);
            var bounds = prepared.Regions.Single(region => region.Id == "series-0-node-" + leaf.Id).Bounds;
            Assert.True(LabelPlacementService.Contains(bounds, Bounds(caption)));
            if (leaf.Id == (dense ? "group0-0" : "a")) AssertCaptionInk(prepared, caption, bounds);
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.label-overflow");
        chart.WithDataLabels(false);
        var without = chart.Prepare(Context(760, 420, mode));
        foreach (var node in prepared.Regions.Where(region => region.Role == "tree-node")) {
            var other = without.Regions.Single(region => region.Id == node.Id);
            Assert.Equal(other.Bounds.Left, node.Bounds.Left, 8);
            Assert.Equal(other.Bounds.Top + other.Bounds.Height / 2, node.Bounds.Top + node.Bounds.Height / 2, 8);
        }
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void TreemapUsesAvailableRowsForCompleteNameAndValue(VisualThemeMode mode) {
        var chart = Treemap();
        var prepared = chart.Prepare(Context(390, 420, mode));
        ScatterBubbleColorRenderingTests.Capture(prepared, "treemap-complete-extra-rows-" + mode);
        var caption = Caption(prepared, "region", "treemap-label");
        Assert.Equal(Name, string.Join(" ", caption.Text.Lines.Take(caption.Text.Lines.Count - 1).Select(line => line.Text)));
        Assert.Equal("28", caption.Text.Lines.Last().Text);
        Assert.Equal(12, caption.Text.Size);
        Assert.True(caption.Text.Lines.Count > 3);
        var region = prepared.Regions.Single(region => region.Id == "series-0-node-region");
        Assert.True(LabelPlacementService.Contains(region.Bounds, Bounds(caption)));
        Assert.Contains(Name, region.Label!, StringComparison.Ordinal);
        AssertCaptionInk(prepared, caption, region.Bounds);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void TreeAuthoredCaptionControlsSizingPaintAndDescriptionWithoutChangingTheNodeName() {
        var chart = Tree(false);
        chart.Series[0].WithPointLabel(0, Name);
        var prepared = chart.Prepare(Context(760, 420, VisualThemeMode.Light));
        var caption = Caption(prepared, "root", "tree-node-label");
        Assert.Equal(Name, string.Join(" ", caption.Text.Lines.Select(line => line.Text)));
        Assert.Equal(3, caption.Text.Lines.Count);
        Assert.Equal(12, caption.Text.Size);
        var source = XDocument.Parse(prepared.ToSvg()).Descendants().Single(node =>
            (string?)node.Attribute("data-cfx-role") == "tree-node" && (string?)node.Attribute("data-cfx-target-id") == "root");
        Assert.Equal("Operations", (string?)source.Attribute("data-cfx-label"));
        Assert.Equal(Name, (string?)source.Attribute("data-cfx-full-label"));
        var region = prepared.Regions.Single(region => region.Id == "series-0-node-root");
        Assert.Equal(Name + ": level 0", region.Label);
        AssertCaptionInk(prepared, caption, region.Bounds);
        ScatterBubbleColorRenderingTests.Capture(prepared, "tree-authored-caption");
        var frozen = prepared.ToSvg();
        chart.Series[0].UseFormattedPointLabel(0);
        Assert.Equal("Operations", Assert.Single(Caption(chart.Prepare(Context(760, 420, VisualThemeMode.Light)), "root", "tree-node-label").Text.Lines).Text);
        Assert.Equal("Operations", chart.Series[0].Nodes[0].Label);
        Assert.Equal(frozen, prepared.ToSvg());
    }

    internal static Chart Tree(bool dense) {
        var nodes = new List<ChartNode> { new("root", "Operations") };
        var links = new List<ChartTreeLink>();
        var names = dense ? new[] { Name, Name.Replace("North America", "South America"), Name.Replace("North America", "Western Europe") } : new[] { Name };
        for (var group = 0; group < names.Length; group++) {
            var id = dense ? "group" + group : "group";
            nodes.Add(new ChartNode(id, names[group])); links.Add(new ChartTreeLink("root", id));
            for (var leaf = 0; leaf < 4; leaf++) {
                var child = dense ? id + "-" + leaf : ((char)('a' + leaf)).ToString();
                nodes.Add(new ChartNode(child, "Region " + (char)('A' + leaf))); links.Add(new ChartTreeLink(id, child));
            }
        }
        var chart = Chart.Create().WithLegend(false).WithDataLabels().AddTree("Operations", nodes, links);
        chart.Series[0].ConfigureDataLabelStyle(style => style.WithFontSize(12));
        return chart;
    }

    internal static Chart Treemap() {
        var chart = Chart.Create().WithLegend(false).WithDataLabels().AddTreemap("Operations", new[] {
            new ChartHierarchyItem("group", "Operations"),
            new ChartHierarchyItem("region", Name, "group", 28),
            new ChartHierarchyItem("other", "Other regions", "group", 72)
        });
        chart.Series[0].ConfigureDataLabelStyle(style => style.WithFontSize(12));
        return chart;
    }

    private static VisualSceneText Caption(PreparedVisual prepared, string id, string role) {
        var stack = new Stack<string?>();
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) stack.Push(group.Id);
            else if (node is VisualSceneEndGroup) stack.Pop();
            else if (node is VisualSceneText text && text.Role == role && stack.Contains("series-0-node-" + id)) return text;
        }
        Assert.Fail("The complete caption must have a painted text run for " + id + ".");
        return null!;
    }

    private static ChartRect Bounds(VisualSceneText text) => new(text.Text.Lines.Min(line => text.LineLeft(line)),
        text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);

    private static void AssertCaptionInk(PreparedVisual prepared, VisualSceneText caption, ChartRect mark) {
        var scene = prepared.Scene;
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size,
            scene.Nodes.Where(node => !ReferenceEquals(node, caption)).ToArray(), scene.Diagnostics, scene.Regions), supersampling: 1);
        var painted = new bool[caption.Text.Lines.Count];
        var top = caption.Baseline - caption.Text.Ascent;
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) {
            var offset = (y * image.Width + x) * 4;
            if (image.Pixels[offset] == without.Pixels[offset] && image.Pixels[offset + 1] == without.Pixels[offset + 1]
                && image.Pixels[offset + 2] == without.Pixels[offset + 2] && image.Pixels[offset + 3] == without.Pixels[offset + 3]) continue;
            Assert.InRange(x + .5, mark.Left - 1, mark.Right + 1);
            Assert.InRange(y + .5, mark.Top - 1, mark.Bottom + 1);
            var row = (int)Math.Floor((y + .5 - top) / caption.Text.Metrics.LineHeight);
            if (row >= 0 && row < painted.Length) painted[row] = true;
        }
        Assert.All(painted, line => Assert.True(line, "Every complete caption row must reach the native raster painter."));
    }

    internal static VisualRenderContext Context(int width, int height, VisualThemeMode mode, FontSpec? font = null) => new(
        new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode,
        new VisualFrame("Regional operations", showLegend: false),
        font ?? FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf")));
}
