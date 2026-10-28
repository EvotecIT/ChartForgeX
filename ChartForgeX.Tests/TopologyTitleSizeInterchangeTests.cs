using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyTitleSizeInterchangeTests {
    [Fact]
    public void GraphiteOmittedAndResetTitleSizesKeepVersionOneAndIdenticalOrdinaryBytes() {
        var chart = Source(VisualTheme.Graphite()); var baseline = chart.Prepare();
        var json = baseline.ToInterchangeEnvelope().ToJson(); var svg = baseline.ToSvg(); var png = baseline.ToPng();
        Assert.Equal(1, baseline.ToInterchangeEnvelope().Version); Assert.DoesNotContain("\"titleSize\"", json);
        chart.Theme = VisualTheme.Graphite().WithTypography(new VisualTypography(titleSize: 29));
        Assert.Equal(2, chart.Prepare().ToInterchangeEnvelope().Version);
        chart.Theme = VisualTheme.Graphite().WithTypography(new VisualTypography(titleSize: VisualTheme.Graphite().Typography.TitleSize));
        Assert.Equal(json, chart.Prepare().ToInterchangeEnvelope().ToJson()); Assert.Equal(svg, chart.ToSvg()); Assert.Equal(png, chart.ToPng());
    }
    [Fact]
    public void NondefaultTopologyAndPresetTitleSizeUsesTheExistingFrameOwnerAndRestoresThroughVersionTwo() {
        foreach (var theme in new[] { VisualTheme.Report(), VisualTheme.Product(), VisualTheme.Graphite().WithTypography(new VisualTypography(titleSize: 29)) }) {
            var chart = Source(theme);
            // Test the preset/title contract with an explicitly bounded caller face; host Avenir collections
            // may exceed the separate 2 MiB portable-root admission while remaining valid native output.
            var request = VisualExportRequest.ForTopology(chart, chart.DefaultRenderOptions ?? new TopologyRenderOptions()).Context;
            var context = new VisualRenderContext(request.Layout, request.Theme, request.ThemeMode, request.Frame,
                FontSpec.FromFamily(BrowserProofFonts.Family));
            var prepared = chart.Prepare(context);
            TopologyPortableFontObservation.Capture("title-size-" + theme.Typography.Family + "-" + theme.Typography.TitleSize, prepared);
            var saved = prepared.ToArtifact().ToInterchangeEnvelope();
            var expected = theme.Typography.TitleSize; var nondefault = expected != VisualTheme.Graphite().Typography.TitleSize;
            Assert.Equal(nondefault ? 2 : 1, saved.Version);
            Assert.Equal(nondefault ? expected : (double?)null, saved.Presentation!.Theme!.TitleSize);
            var restored = VisualArtifactInterchangeEnvelope.FromUtf8Json(saved.ToUtf8Json()).ToTopologyChart();
            Assert.Equal(expected, restored.Theme.Typography.TitleSize);
            Assert.NotEmpty(restored.ToPng()); Assert.Contains("Topology title", restored.ToSvg());
        }
    }
    [Fact]
    public void HierarchyRetainsAuthoredTitleSizeAndPreparedContextCapturesItsOwnTitleSizeWithoutScalingItWithContent() {
        var chart = Source(VisualTheme.Graphite().WithTypography(new VisualTypography(titleSize: 23)));
        chart.Nodes.Clear();
        chart.AddHierarchy(new[] { new TopologyHierarchyItem("lead", "Avery Quinn"), new TopologyHierarchyItem("team", "Engineering", "lead") });
        var theme = VisualTheme.Graphite().WithTypography(new VisualTypography(titleSize: 31));
        var saved = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(420, 300), 16), theme),
            new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true, HierarchyView = new TopologyHierarchyView() }).ToArtifact().ToInterchangeEnvelope();
        Assert.Equal(31, saved.Presentation!.Theme!.TitleSize); Assert.Equal(23, saved.Topology!.Hierarchy!.Source.Presentation!.Theme!.TitleSize);
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson());
        Assert.Equal(31, loaded.ToTopologyChart().Theme.Typography.TitleSize); Assert.Equal(23, loaded.ToTopologyHierarchySource().Theme.Typography.TitleSize);
        saved.Presentation!.Theme!.TitleSize = 45; Assert.Equal(31, loaded.Presentation!.Theme!.TitleSize);
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    public void InvalidTitleSizesFailTheSharedPortableValidation(double value) {
        var envelope = Source(VisualTheme.Graphite()).Prepare().ToInterchangeEnvelope();
        envelope.Presentation!.Theme!.TitleSize = value;
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
    }
    private static TopologyChart Source(VisualTheme theme) => TopologyChart.Create().WithId("title-size").WithTitle("Topology title")
        .WithViewport(1000, 650, 24).WithTheme(theme).WithLegend(null).AddNode("service", "Service", 100, 130, width: 220, height: 100);
}
