using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void DefaultStaticRenderersStayDeterministic() {
        var chart = Chart.Create()
            .WithTitle("Deterministic chart")
            .WithSize(360, 220)
            .AddLine("Values", Points(10, 20, 15));
        var block = MetricCard.Create()
            .WithMetric("Coverage", "98.2%")
            .WithSize(280, 150);
        var grid = VisualGrid.Create()
            .WithTitle("Deterministic grid")
            .Add(chart)
            .Add(block);

        Assert(chart.ToHtmlFragment() == chart.ToHtmlFragment(), "HTML chart fragments should not depend on process-global counters.");
        Assert(block.ToSvg() == block.ToSvg(), "Visual block SVG should not depend on process-global counters.");
        Assert(block.ToHtmlFragment() == block.ToHtmlFragment(), "Visual block HTML should not depend on process-global counters.");
        Assert(grid.ToSvg() == grid.ToSvg(), "Visual grid SVG should not depend on process-global counters.");
        Assert(grid.ToHtmlFragment() == grid.ToHtmlFragment(), "Visual grid HTML should not depend on process-global counters.");

        AssertNoDuplicateIds(block.ToHtmlFragment("block-a") + block.ToHtmlFragment("block-b"), "Explicitly scoped visual block fragments");
        AssertNoDuplicateIds(grid.ToHtmlFragment("grid-a") + grid.ToHtmlFragment("grid-b"), "Explicitly scoped visual grid fragments");
    }

    private static void RasterRenderingRejectsUnsafeAllocationsEarly() {
        var oversized = Chart.Create()
            .WithSize(int.MaxValue, 1)
            .AddLine("Values", Points(1, 2));

        AssertThrows<ArgumentOutOfRangeException>(
            () => oversized.ToPng(),
            "Raster rendering should reject dimensions that exceed the deterministic canvas allocation budget before array arithmetic overflows.");
    }

    private static void ExtremeFiniteAxisBoundsRenderWithoutInvalidNumbers() {
        var extreme = Chart.Create()
            .WithSize(420, 260)
            .WithYAxisBounds(-double.MaxValue, double.MaxValue)
            .AddLine("Values", new[] {
                new ChartPoint(1, -double.MaxValue),
                new ChartPoint(2, 0),
                new ChartPoint(3, double.MaxValue)
            })
            .ToSvg();
        Assert(!extreme.Contains("NaN", StringComparison.Ordinal) && !extreme.Contains("Infinity", StringComparison.Ordinal), "Extreme finite bounds should not produce invalid SVG numeric values.");

        var subnormal = Chart.Create()
            .WithSize(420, 260)
            .WithYAxisBounds(0, double.Epsilon)
            .AddLine("Values", new[] { new ChartPoint(1, 0), new ChartPoint(2, double.Epsilon) })
            .ToSvg();
        Assert(!subnormal.Contains("NaN", StringComparison.Ordinal) && !subnormal.Contains("Infinity", StringComparison.Ordinal), "Subnormal finite bounds should fall back to stable endpoint ticks.");
    }

    private static void CappedTickGenerationPreservesTheRequestedRange() {
        var ticks = ChartTicks.Generate(0, 20_000, 20_000);
        Assert(ticks.Count <= 10_000, "Generated ticks should stay within the deterministic safety cap.");
        Assert(ticks[0] <= 0 && ticks[ticks.Count - 1] >= 20_000, "Capped tick generation should preserve both ends of the requested axis range.");
        Assert(ticks.Zip(ticks.Skip(1), (left, right) => right > left).All(increasing => increasing), "Capped ticks should remain strictly increasing after preserving the upper endpoint.");

        var inside = ChartTicks.GenerateInside(0, 20_000, int.MaxValue);
        Assert(inside.Count <= 10_000, "Inside ticks should stay within the deterministic safety cap for extreme requested counts.");
        Assert(inside[0] == 0 && inside[inside.Count - 1] == 20_000, "Inside ticks should retain the requested upper endpoint when their safety cap is reached.");

        var extremeMinimum = double.MaxValue - 1e294;
        var extremeTicks = ChartTicks.Generate(extremeMinimum, double.MaxValue, 6);
        Assert(extremeTicks.All(value => !double.IsNaN(value) && !double.IsInfinity(value)), "Extreme finite tick ranges should remain finite after magnitude normalization.");
        Assert(extremeTicks[0] <= extremeMinimum && extremeTicks[extremeTicks.Count - 1] == double.MaxValue, "Extreme finite tick ranges should preserve the exact upper data endpoint.");
        Assert(extremeTicks.Zip(extremeTicks.Skip(1), (left, right) => right > left).All(increasing => increasing), "Extreme finite ticks should remain strictly increasing after normalization.");
    }
}
