using System;
using System.Linq;
using System.Runtime.InteropServices;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void DenseSeriesDecimationStaysDeterministicAndHonest() {
        var points = Enumerable.Range(0, 1000)
            .Select(index => new ChartPoint(index, Math.Sin(index / 20d) * 20d + (index == 517 ? 120d : 0d)))
            .ToArray();

        var first = ChartDecimator.Decimate(points, 80);
        var second = ChartDecimator.Decimate(points, 80);
        Assert(first.Points.Count <= 80, "LTTB decimation should respect the requested maximum.");
        Assert(first.SourcePointCount == points.Length && first.WasDecimated, "Decimation should report the original point count and whether reduction occurred.");
        Assert(first.SourceIndices.SequenceEqual(second.SourceIndices), "Decimation should be deterministic for identical ordered input.");
        Assert(first.SourceIndices[0] == 0 && first.SourceIndices[first.SourceIndices.Count - 1] == points.Length - 1, "Decimation should preserve the first and last source points.");
        Assert(first.SourceIndices.Contains(517), "LTTB decimation should retain the material spike in a dense series.");

        var minMax = ChartDecimator.Decimate(points, 3, ChartDecimationMode.MinMax);
        Assert(minMax.Points.Count == 3, "Min/max decimation should respect a three-point maximum.");
        Assert(minMax.SourceIndices[0] == 0 && minMax.SourceIndices[2] == points.Length - 1, "Three-point min/max decimation should preserve endpoints.");

        var identity = ChartDecimator.Decimate(points.Take(4), 10);
        Assert(!identity.WasDecimated && identity.SourceIndices.SequenceEqual(new[] { 0, 1, 2, 3 }), "Sequences already inside the budget should preserve identity.");
        AssertThrows<ArgumentNullException>(() => ChartDecimator.Decimate(null!, 10), "Decimation should reject null input.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartDecimator.Decimate(points, 2), "Decimation should reject budgets below three points.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartDecimator.Decimate(points, 10, (ChartDecimationMode)99), "Decimation should reject unknown algorithms.");
        var invalid = points.ToArray();
        invalid[517] = MemoryMarshal.Cast<double, ChartPoint>(new[] { invalid[517].X, double.NaN }.AsSpan())[0];
        AssertThrows<ArgumentOutOfRangeException>(() => ChartDecimator.Decimate(invalid, 3), "Decimation should validate every source point even when an invalid point would not be retained.");
    }

    private static void DecimatedChartsPreserveSourcePointIdentities() {
        var points = Enumerable.Range(0, 120).Select(index => new ChartPoint(index, Math.Cos(index / 8d) * 10d)).ToArray();
        var chart = Chart.Create().WithSize(640, 360).AddDecimatedLine("Latency", points, 24, ChartDecimationMode.MinMax);
        var series = chart.Series[0];
        Assert(series.IsDecimated && series.SourcePointCount == 120 && series.Points.Count <= 24, "Decimated series should expose truthful source and rendered counts.");
        Assert(series.DecimationMode == ChartDecimationMode.MinMax && series.SourcePointIndices.Count == series.Points.Count, "Decimated series should expose its algorithm and source-index mapping.");

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-series-source-points-0=\"120\"", StringComparison.Ordinal), "SVG should expose the source point count.");
        Assert(svg.Contains("data-cfx-series-rendered-points-0=\"", StringComparison.Ordinal), "SVG should expose the rendered point count.");
        Assert(svg.Contains("data-cfx-series-decimation-0=\"MinMax\"", StringComparison.Ordinal), "SVG should expose the selected decimation algorithm.");
        Assert(svg.Contains("data-cfx-series-source-indices-0=\"0,", StringComparison.Ordinal), "SVG should expose source-index provenance for rendered points.");

        var html = chart.ToInteractiveHtmlFragment();
        Assert(html.Contains("const sourcePointIndex = (node)", StringComparison.Ordinal), "Interactive HTML should resolve retained points back to source identities.");
        Assert(html.Contains("sourcePoint: sourcePointIndex(node)", StringComparison.Ordinal), "Interactive events should report the original point identity alongside the rendered ordinal.");
    }

    private static void AdaptiveSeriesUseReusableResolutionPolicy() {
        var defaultPolicy = ChartResolutionPolicy.Trend();
        Assert(defaultPolicy.ResolvePointBudget(0) == 64, "Adaptive trend rendering should retain a useful minimum point budget.");
        Assert(defaultPolicy.ResolvePointBudget(240) == 480, "Adaptive trend rendering should resolve two points per horizontal pixel by default.");
        Assert(defaultPolicy.ResolvePointBudget(int.MaxValue) == int.MaxValue, "Adaptive trend rendering should saturate safely at the configured maximum.");

        var points = Enumerable.Range(0, 10_000)
            .Select(index => new ChartPoint(index, index == 5_000 ? 1_000 : Math.Sin(index / 20d)))
            .ToArray();
        var chart = Chart.Create().AddAdaptiveLine("Signal", points, 320);
        var series = chart.Series[0];
        Assert(series.IsDecimated && series.SourcePointCount == points.Length && series.Points.Count <= 640, "Adaptive series should decimate dense sources against the reusable width-aware budget.");
        Assert(series.SourcePointIndices.Contains(5_000), "Adaptive trend rendering should preserve material spikes through its default LTTB policy.");
        Assert(series.MarkerRadius == 0d, "Adaptive trend rendering should suppress optional markers when retained point density would obscure the line.");
        var svg = chart.ToSvg();
        Assert(!svg.Contains("data-cfx-role=\"line-marker\"", StringComparison.Ordinal), "Dense adaptive trends should avoid thousands of visible SVG marker nodes.");
        Assert(!svg.Contains("data-cfx-role=\"line-point-target\"", StringComparison.Ordinal), "Static adaptive SVG should stay lean by omitting interaction-only point targets.");
        var interactiveHtml = chart.ToInteractiveHtmlFragment();
        Assert(interactiveHtml.Contains("data-cfx-role=\"line-point-target\"", StringComparison.Ordinal) && interactiveHtml.Contains("class=\"cfx-point-interaction-target\"", StringComparison.Ordinal), "Interactive adaptive trends should retain invisible point targets for tooltips, scenarios, and keyboard interaction.");
        var sparklineSvg = Chart.Create().WithSparkline().AddAdaptiveLine("Signal", points, 320).ToSvg();
        Assert(!sparklineSvg.Contains("data-cfx-role=\"line-point-target\"", StringComparison.Ordinal), "Static adaptive sparklines should omit interaction-only point targets.");
        var sparklineHtml = Chart.Create().WithSparkline().AddAdaptiveLine("Signal", points, 320).ToInteractiveHtmlFragment();
        Assert(sparklineHtml.Contains("data-cfx-role=\"line-point-target\"", StringComparison.Ordinal), "Interactive adaptive sparklines should preserve invisible point targets even though sparkline chrome and visible markers stay suppressed.");

        var compact = Chart.Create().AddAdaptiveArea("Compact", points.Take(32), 320);
        Assert(!compact.Series[0].IsDecimated && compact.Series[0].Points.Count == 32, "Adaptive series should preserve sources already inside the resolved budget.");
        Assert(!compact.Series[0].MarkerRadius.HasValue, "Short adaptive series should retain the chart theme marker treatment.");

        var bounded = ChartResolutionPolicy.Trend();
        bounded.MaximumPointCount = 128;
        Assert(bounded.ResolvePointBudget(4_000) == 128, "Adaptive resolution policy should honor an explicit maximum.");
        AssertThrows<ArgumentOutOfRangeException>(() => bounded.PointsPerPixel = 0, "Adaptive resolution policy should reject non-positive density.");
        AssertThrows<ArgumentOutOfRangeException>(() => bounded.MaximumMarkerCount = -1, "Adaptive resolution policy should reject negative marker thresholds.");
        AssertThrows<ArgumentOutOfRangeException>(() => bounded.ResolvePointBudget(-1), "Adaptive resolution policy should reject negative viewport widths.");
    }
}
