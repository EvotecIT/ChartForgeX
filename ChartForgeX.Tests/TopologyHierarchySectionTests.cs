using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyHierarchySectionTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void GenuineTopologySectionsKeepSourceIdsNativeExportsAndDetachedAdaptiveViews(VisualThemeMode mode) {
        var source = Source(mode); var renderer = new HtmlInteractiveTopologyRenderer();
        var ordinary = renderer.RenderFragmentWithoutAssets(source); var options = source.DefaultRenderOptions;
        var section = renderer.PrepareHierarchySection(source, new TopologyHierarchyView(new[] { "engineering" }), Configure);
        Assert.Same(options, source.DefaultRenderOptions); Assert.Null(Assert.IsType<TopologyRenderOptions>(source.DefaultRenderOptions).HierarchyView);
        Assert.Equal(ordinary, renderer.RenderFragmentWithoutAssets(source));
        using var metadata = JsonDocument.Parse(Metadata(section.Html)); var root = metadata.RootElement;
        Assert.Equal(0, root.GetProperty("series").GetArrayLength()); Assert.Equal("topology", root.GetProperty("hierarchy").GetProperty("kind").GetString());
        // Both installable drawings satisfy the ordinary prepared-section collection contract;
        // an empty category collection does not manufacture a numeric topology axis.
        var drawings = Regex.Matches(section.Html, "data-cfx-prepared-chart=\"([^\"]*)\""); Assert.Equal(2, drawings.Count);
        foreach (Match match in drawings) {
            using var drawingMetadata = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
            var drawingRoot = drawingMetadata.RootElement;
            Assert.Equal(JsonValueKind.Array, drawingRoot.GetProperty("xLabels").ValueKind);
            Assert.Equal(0, drawingRoot.GetProperty("xLabels").GetArrayLength());
            Assert.Equal(0, drawingRoot.GetProperty("series").GetArrayLength());
            Assert.False(drawingRoot.TryGetProperty("xProjection", out _)); Assert.False(drawingRoot.TryGetProperty("yProjection", out _));
        }
        Assert.Equal(5, root.GetProperty("hierarchy").GetProperty("nodes").GetArrayLength());
        Assert.False(root.TryGetProperty("xProjection", out _)); Assert.False(root.TryGetProperty("yProjection", out _));
        Assert.Null(section.PreparedVisual.XProjection); Assert.Null(section.PreparedVisual.YProjection); Assert.Null(section.PreparedVisual.HierarchyView);
        Assert.Equal(new[] { "engineering" }, section.PreparedVisual.TopologyHierarchyView!.CollapsedNodeIds);
        Assert.Equal(new[] { "lead", "engineering", "operations" }, section.PreparedVisual.Regions.Where(region => region.Role == "topology-node").Select(region => region.Id));
        Assert.NotNull(section.NarrowPreparedVisual); Assert.Equal(new[] { "engineering" }, section.NarrowPreparedVisual!.TopologyHierarchyView!.CollapsedNodeIds);
        Assert.Contains("data-cfx-hierarchy-node=\"engineering\"", section.Html); Assert.Contains("Expand Engineering", section.Html);
        var raw = section.PreparedVisual.SemanticInterchange!.ToTopologyHierarchySource(); Assert.Equal(5, raw.Nodes.Count);
        Assert.Equal("Owner", raw.Nodes.Single(node => node.Id == "engineering").Badge);
        var svg = section.PreparedVisual.ToSvg(); var png = section.PreparedVisual.ToPng(); var narrowSvg = section.NarrowPreparedVisual.ToSvg();
        source.Nodes.Clear(); source.Edges.Clear(); Assert.IsType<TopologyRenderOptions>(source.DefaultRenderOptions).IncludeLegend = true;
        Assert.Equal(svg, section.PreparedVisual.ToSvg()); Assert.Equal(png, section.PreparedVisual.ToPng()); Assert.Equal(narrowSvg, section.NarrowPreparedVisual.ToSvg());
        Assert.Equal(5, section.PreparedVisual.SemanticInterchange!.ToTopologyHierarchySource().Nodes.Count);
    }

    [Fact]
    public void AnUnconfiguredTopologyUsesTheExistingNativeDefaultOptionsWithoutChangingItsSource() {
        var source = Source(VisualThemeMode.Light); source.DefaultRenderOptions = null;
        var section = new HtmlInteractiveTopologyRenderer().PrepareHierarchySection(source);
        Assert.Null(source.DefaultRenderOptions); Assert.Equal(5, source.Nodes.Count);
        Assert.Equal(5, section.PreparedVisual.SemanticInterchange!.ToTopologyHierarchySource().Nodes.Count);
        Assert.Null(section.PreparedVisual.XProjection); Assert.Null(section.PreparedVisual.YProjection);
        Assert.NotEmpty(section.PreparedVisual.ToSvg()); Assert.NotEmpty(section.PreparedVisual.ToPng());
    }

    [Theory]
    [InlineData(ChartInteractionFeatures.Brush)]
    [InlineData(ChartInteractionFeatures.Crosshair)]
    [InlineData(ChartInteractionFeatures.CompareMarkers)]
    [InlineData(ChartInteractionFeatures.LegendToggles)]
    [InlineData(ChartInteractionFeatures.Scenarios)]
    public void NumericOnlyOptionsRejectBeforeChangingTheTopology(ChartInteractionFeatures feature) {
        var source = Source(VisualThemeMode.Light); var renderer = new HtmlInteractiveTopologyRenderer(); var before = source.ToSvg();
        Assert.Throws<NotSupportedException>(() => renderer.PrepareHierarchySection(source, configure: options => options.Interaction.Enable(feature)));
        Assert.Equal(before, source.ToSvg()); Assert.Null(Assert.IsType<TopologyRenderOptions>(source.DefaultRenderOptions).HierarchyView);
    }

    [Fact]
    public void AxisNavigatorAndProgressEditingRemainExplicitlyUnavailable() {
        var source = Source(VisualThemeMode.Light); var renderer = new HtmlInteractiveTopologyRenderer();
        Assert.Throws<NotSupportedException>(() => renderer.PrepareHierarchySection(source, configure: options => options.XNavigation.Enabled = true));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareHierarchySection(source, configure: options => options.ProgressEditing.Enabled = true));
        Assert.Equal(5, source.Nodes.Count);
    }

    internal static string Metadata(string html) => WebUtility.HtmlDecode(Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]*)\"").Groups[1].Value);
    internal static void Configure(HtmlChartInteractionOptions options) {
        options.Interaction.ChartId = "native-organization"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 580;
    }
    internal static TopologyChart Source(VisualThemeMode mode) {
        var pixels = new byte[8 * 4 * 4]; for (var index = 0; index < 32; index++) { pixels[index * 4] = 36; pixels[index * 4 + 1] = 112; pixels[index * 4 + 2] = 168; pixels[index * 4 + 3] = 255; }
        var portrait = TopologyIconArtwork.Portrait(new RgbaImage(8, 4, pixels));
        var chart = TopologyChart.Create().WithId("organization-source").WithViewport(1100, 580).WithLegend(null).AddHierarchy(new[] {
            new TopologyHierarchyItem("lead", "Avery Quinn") { Artwork = portrait },
            new TopologyHierarchyItem("engineering", "Engineering", "lead") { Artwork = portrait },
            new TopologyHierarchyItem("operations", "Operations", "lead") { Artwork = portrait },
            new TopologyHierarchyItem("platform", "Platform", "engineering") { Artwork = portrait },
            new TopologyHierarchyItem("research", "Research", "engineering") { Artwork = portrait }
        }, new TopologyHierarchyOptions { NodeDisplayMode = TopologyNodeDisplayMode.Card, LayoutPolicy = TopologyHierarchyLayoutPolicy.Standard });
        chart.Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family); chart.ThemeMode = mode;
        chart.DefaultRenderOptions = new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true };
        chart.Nodes.Single(node => node.Id == "engineering").Badge = "Owner"; return chart;
    }
}
