using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeSparseReplayContracts {
    [Theory]
    [InlineData("line")] [InlineData("pie")] [InlineData("donut")] [InlineData("treemap")] [InlineData("sunburst")]
    public void FullNativeCloneRetainsAuthoredSparseSlotsCanonicalSourceAndExactPaint(string family) {
        var chart = Model(family); var series = chart.Series[0];
        series.PointColors.Clear(); series.PointColors.Add(ChartColor.Black); series.PointColors.Add(null);
        series.PointFillPatterns.Clear(); series.PointFillPatterns.Add(null);
        series.PointLabels.Clear(); series.PointLabels.Add("H"); series.PointLabels.Add(null);
        series.PointDataLabelStyles.Clear(); series.PointDataLabelStyles.Add(new TextStyleOverride { FontSize = 12 }); series.PointDataLabelStyles.Add(null);
        var context = VisualExportRequest.ForChart(chart).Context;
        var source = new ChartVisibilitySource(chart, context); var replay = source.CreateNativeView(null);
        SameSparse(series, replay.Series[0]);
        Assert.Equal(source.CanonicalFacts(context), new ChartVisibilitySource(replay, context).CanonicalFacts(context));
        var expected = chart.Prepare(context); var actual = replay.Prepare(context);
        Assert.Equal(expected.ToSvg(), actual.ToSvg()); Assert.Equal(expected.ToPng(), actual.ToPng());
    }
    [Fact]
    public void SelectedRadialReflowRetainsOnlyAuthoredSlotsIncludingAnAuthoredTrailingNull() {
        var chart = Model("pie"); var series = chart.Series[0];
        series.PointColors.Clear(); series.PointColors.Add(ChartColor.Black); series.PointColors.Add(null);
        series.PointFillPatterns.Clear(); series.PointLabels.Clear(); series.PointDataLabelStyles.Clear();
        series.WithPointSliceOffset(1, .1).UseDefaultSliceOffset(1);
        series.WithPointSliceRadiusRatio(1, .7).UseDefaultSliceRadius(1);
        var source = series.CaptureVisibilitySource();
        var selected = ChartSeries.FromVisibilitySource(source, null, new[] { 1, 2 });
        Assert.Single(selected.PointColors); Assert.Null(selected.PointColors[0]);
        Assert.Empty(selected.PointFillPatterns); Assert.Empty(selected.PointLabels); Assert.Empty(selected.PointDataLabelStyles);
        Assert.Single(selected.PointSliceOffsets); Assert.Equal(0, selected.PointSliceOffsets[0]);
        Assert.Single(selected.PointSliceRadiusRatios); Assert.Null(selected.PointSliceRadiusRatios[0]);
    }
    [Fact]
    public void FinancialConfiguredAndClearedTailKeepTheirExactSparseSourceShapeThroughCompleteFrames() {
        Chart ModelAt(double value) {
            var chart = NativeComponentClipBeforeControls.Model("candlestick", value);
            chart.Series[0].ConfigureFinancialValue(1, options => options.StrokeWidth = 4).UseSeriesFinancialStyle(1);
            return chart;
        }
        var renderer = new HtmlInteractiveChartRenderer();
        var from = NativeComponentClipBeforeControls.Section(renderer, ModelAt(40));
        var to = NativeComponentClipBeforeControls.Section(renderer, ModelAt(60));
        var independent = NativeComponentClipBeforeControls.Section(renderer, ModelAt(50));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        var expected = NativeRawValueSourceMotionTests.Metadata(from).GetProperty("nativeValueSource").GetProperty("structureKey").GetString();
        using var json = JsonDocument.Parse(payload);
        Assert.All(json.RootElement.GetProperty("drawings").GetProperty("wide").GetProperty("frames").EnumerateArray(),
            frame => Assert.Equal(expected, frame.GetProperty("nativeValueSource").GetProperty("structureKey").GetString()));
        Assert.Equal(independent.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(independent.PreparedVisual.ToPng(), frames[1].ToPng());
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[^1].ToPng());
    }
    private static Chart Model(string family) => family switch {
        "line" => NativeSeriesVisibilityDefaultControls.Model("line"),
        "pie" => NativeRadialMotionBeforeControls.Model(false, false),
        "donut" => NativeRadialMotionBeforeControls.Model(true, false),
        "treemap" => NativeHierarchyMotionBeforeControls.Model(false, false, 30),
        _ => NativeHierarchyMotionBeforeControls.Model(true, false, 30)
    };
    private static void SameSparse(ChartSeries expected, ChartSeries actual) {
        Assert.Equal(expected.PointColors, actual.PointColors); Assert.Equal(expected.PointFillPatterns, actual.PointFillPatterns);
        Assert.Equal(expected.PointLabels, actual.PointLabels); Assert.Equal(expected.PointDataLabelStyles.Count, actual.PointDataLabelStyles.Count);
        for (var index = 0; index < expected.PointDataLabelStyles.Count; index++) {
            if (expected.PointDataLabelStyles[index] == null) Assert.Null(actual.PointDataLabelStyles[index]);
            else { Assert.Equal(expected.PointDataLabelStyles[index]!.FontSize, actual.PointDataLabelStyles[index]!.FontSize); Assert.NotSame(expected.PointDataLabelStyles[index], actual.PointDataLabelStyles[index]); }
        }
        Assert.Equal(expected.PointSliceOffsets, actual.PointSliceOffsets); Assert.Equal(expected.PointSliceRadiusRatios, actual.PointSliceRadiusRatios);
    }
}
