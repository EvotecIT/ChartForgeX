using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CaptionLayoutRegressionTests {
    private const string ProofFont = "CFX Visual Brief Captions Carlito";

    [Theory]
    [InlineData(400, 280, VisualThemeMode.Light)]
    [InlineData(400, 280, VisualThemeMode.Dark)]
    [InlineData(800, 440, VisualThemeMode.Light)]
    [InlineData(800, 440, VisualThemeMode.Dark)]
    public void TreemapCatalogPreservesCompleteNamesInItsActualNestedCells(int width, int height, VisualThemeMode mode) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Treemap, "wide", mode);
        var context = Context(width, height, mode, null, 16);
        if (width == 800) context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode,
            new VisualFrame(V2GalleryModels.Title(ChartSeriesKind.Treemap), "Explore the data, then download the chart"), FontSpec.FromFamily(ProofFont));
        var prepared = chart.Prepare(context);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"caption-treemap-catalog-{width}-{mode}");
        var document = XDocument.Parse(prepared.ToSvg());
        var leaves = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "treemap-tile").ToArray();
        Assert.Equal(7, leaves.Length);
        foreach (var leaf in leaves) {
            var name = (string)leaf.Attribute("data-cfx-full-label")!;
            var caption = Assert.Single(leaf.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "treemap-label");
            var displayed = string.Join(" ", caption.DescendantsAndSelf().Where(element => element.Name.LocalName == "text").Select(element => element.Value));
            Assert.StartsWith(name, displayed, StringComparison.Ordinal);
            Assert.DoesNotContain("…", displayed);
            Assert.False(string.IsNullOrEmpty((string?)leaf.Attribute("data-cfx-formatted-value")));
        }
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role is "treemap-label" or "treemap-group-label"),
            label => Assert.True(label.Text.Size >= (width == 400 ? 13.5 : 12)));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void TreemapOmitsAnUnfittingCompleteNameWhileKeepingItsMarkAndSourceFacts() {
        const string name = "A service name that cannot fit in a tiny cell";
        var chart = Chart.Create().WithDataLabels().AddTreemap("Allocation", new[] { new ChartHierarchyItem("service", name, value: 125) });
        var prepared = chart.Prepare(Context(40, 32, VisualThemeMode.Light, false, 0));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "treemap-label");
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "treemap-tile-mark");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.label-overflow");
        var source = XDocument.Parse(prepared.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-target-id") == "service");
        Assert.Equal(name, (string?)source.Attribute("data-cfx-full-label"));
        Assert.Equal("125", (string?)source.Attribute("data-cfx-formatted-value"));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(400, 280, VisualThemeMode.Light)]
    [InlineData(400, 280, VisualThemeMode.Dark)]
    [InlineData(640, 400, VisualThemeMode.Light)]
    [InlineData(640, 400, VisualThemeMode.Dark)]
    public void ProgressRingCenterReservesItsCompleteCaptionWithinTheMeasuredCircularHole(int width, int height, VisualThemeMode mode) {
        var chart = ProgressRingRecipe.Create();
        var prepared = chart.Prepare(Context(width, height, mode, true, 16));
        ScatterBubbleColorRenderingTests.Capture(prepared, $"caption-progress-ring-{width}-{height}-{mode}");
        var hole = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "progress-ring-center");
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "progress-ring-title");
        var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "progress-ring-value");
        Assert.Equal("Completion", Assert.Single(caption.Text.Lines).Text);
        Assert.Equal("34.83", Assert.Single(value.Text.Lines).Text);
        Assert.Equal(13.5, caption.Text.Size);
        Assert.Equal("Completion", Assert.Single(prepared.Regions, region => region.Id == "series-0-center-caption").Label);
        Assert.True(value.Baseline - value.Text.Ascent + value.Text.Metrics.Height <= caption.Baseline - caption.Text.Ascent);
        foreach (var text in new[] { value, caption }) {
            Assert.Equal(hole.Cx, text.X, 8);
            var top = text.Baseline - text.Text.Ascent;
            var vertical = Math.Max(Math.Abs(top - hole.Cy), Math.Abs(top + text.Text.Metrics.Height - hole.Cy));
            Assert.True(Math.Sqrt(Math.Pow(text.Text.Metrics.Width / 2, 2) + vertical * vertical) <= hole.Rx + .001,
                "Both corners of each complete center row must remain inside the ring hole.");
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        Assert.Contains(">Completion</text>", prepared.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(74, VisualThemeMode.Light)]
    [InlineData(74, VisualThemeMode.Dark)]
    [InlineData(320, VisualThemeMode.Light)]
    [InlineData(320, VisualThemeMode.Dark)]
    public void TreemapWrapsThePrimaryCaptionIntoSpareHeightAndKeepsItsValueAndSource(int width, VisualThemeMode mode) {
        var chart = Chart.Create().WithDataLabels().AddTreemap("Allocation", new[] {
            new ChartHierarchyItem("service", "Service team", value: 125)
        });
        var prepared = chart.Prepare(Context(width, 180, mode, false, 0));
        ScatterBubbleColorRenderingTests.Capture(prepared, $"caption-treemap-{width}-{mode}");
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "treemap-label");
        var lines = caption.Text.Lines.Select(line => line.Text).ToArray();
        Assert.Equal(width == 74 ? new[] { "Service", "team", "125" } : new[] { "Service team", "125" }, lines);
        var tile = Assert.Single(prepared.Regions, region => region.Role == "treemap-tile");
        Assert.Equal("Service team: 125", tile.Label);
        Assert.True(caption.X >= tile.Bounds.Left && caption.X + caption.Text.Metrics.Width <= tile.Bounds.Right);
        Assert.True(caption.Baseline - caption.Text.Ascent >= tile.Bounds.Top);
        Assert.True(caption.Baseline - caption.Text.Ascent + caption.Text.Metrics.Height <= tile.Bounds.Bottom);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.label-overflow");
        var source = XDocument.Parse(prepared.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-target-id") == "service");
        Assert.Equal("Service team", (string?)source.Attribute("data-cfx-full-label"));
        Assert.Equal("125", (string?)source.Attribute("data-cfx-formatted-value"));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void TreemapGivesItsValueRowToTheCompletePrimaryCaptionWhenOnlyTwoRowsFit() {
        var chart = Chart.Create().WithDataLabels().AddTreemap("Allocation", new[] {
            new ChartHierarchyItem("service", "Service team", value: 125)
        });
        var prepared = chart.Prepare(Context(74, 64, VisualThemeMode.Light, false, 0));
        ScatterBubbleColorRenderingTests.Capture(prepared, "caption-treemap-short-height");
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "treemap-label");
        Assert.Equal(2, caption.Text.Lines.Count);
        Assert.Equal(new[] { "Service", "team" }, caption.Text.Lines.Select(line => line.Text));
        var tile = Assert.Single(prepared.Regions, region => region.Role == "treemap-tile");
        Assert.Equal("Service team: 125", tile.Label);
        Assert.True(caption.Baseline - caption.Text.Ascent + caption.Text.Metrics.Height <= tile.Bounds.Bottom);
        var source = XDocument.Parse(prepared.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-target-id") == "service");
        Assert.Equal("125", (string?)source.Attribute("data-cfx-formatted-value"));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(400, 280, VisualThemeMode.Light)]
    [InlineData(400, 280, VisualThemeMode.Dark)]
    [InlineData(800, 440, VisualThemeMode.Light)]
    [InlineData(800, 440, VisualThemeMode.Dark)]
    public void SunburstOmitsAnUnfittingCaptionRatherThanPaintingAnIncompleteName(int width, int height, VisualThemeMode mode) {
        var prepared = SunburstRecipe.Create().Prepare(Context(width, height, mode, true, 14));
        ScatterBubbleColorRenderingTests.Capture(prepared, $"caption-sunburst-{width}-{mode}");
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "sunburst-label").ToArray();
        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.DoesNotContain("…", string.Join(" ", label.Text.Lines.Select(line => line.Text))));
        if (width == 800) Assert.Equal(new[] { "All teams", "Engineering", "Operations", "Platform", "Support", "Support" }.OrderBy(name => name),
            labels.Select(label => string.Join(" ", label.Text.Lines.Select(line => line.Text))).OrderBy(name => name));
        var source = XDocument.Parse(prepared.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-target-id") == "engineering");
        Assert.Equal("Engineering", (string?)source.Attribute("data-cfx-full-label"));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    private static VisualRenderContext Context(int width, int height, VisualThemeMode mode, bool? legend, double padding) {
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        foreach (var (file, weight) in new[] { ("Carlito-Regular.ttf", 400), ("Carlito-Bold.ttf", 700) }) {
            var path = Path.Combine(fonts, file);
            Assert.True(File.Exists(path), "The existing Carlito validation fixture must be available.");
            FontRegistry.Register(ProofFont, path, weight);
        }
        return new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), padding),
            VisualTheme.Graphite().WithTypography(new VisualTypography(axisSize: 13.5, legendSize: 13.5, dataLabelSize: 13.5)), mode,
            new VisualFrame("", "", showLegend: legend), FontSpec.FromFamily(ProofFont));
    }
}
