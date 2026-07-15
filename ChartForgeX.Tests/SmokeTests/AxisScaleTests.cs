using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void AxisObjectsCentralizeBoundsTicksAndScales() {
        var chart = Chart.Create()
            .ConfigureXAxis(axis => {
                axis.WithBounds(1, 1000).WithScale(ChartScaleKind.Logarithmic);
                axis.TickCount = 5;
            })
            .ConfigureYAxis(axis => axis.WithBounds(-10, 10).WithScale(ChartScaleKind.SymmetricLogarithmic))
            .AddLine("Values", new[] { new ChartPoint(1, -10), new ChartPoint(10, 0), new ChartPoint(1000, 10) });

        Assert(chart.Options.XAxisMinimum == 1 && chart.Options.XAxisMaximum == 1000, "Legacy bounds should delegate to the shared x-axis object during migration.");
        Assert(chart.Options.XAxis.Scale == ChartScaleKind.Logarithmic, "The x-axis should own its mathematical scale.");
        Assert(chart.Options.YAxis.Scale == ChartScaleKind.SymmetricLogarithmic, "The y-axis should own its mathematical scale.");
    }

    private static void LogarithmicAxesRenderWithSvgPngParity() {
        var chart = Chart.Create()
            .WithSize(560, 340)
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddLine("Orders", new[] { new ChartPoint(1, 1), new ChartPoint(2, 10), new ChartPoint(3, 100), new ChartPoint(4, 1000) });

        var svg = chart.ToSvg();
        var png = chart.ToPng();
        Assert(svg.Contains(">10</text>", StringComparison.Ordinal) && svg.Contains(">100</text>", StringComparison.Ordinal), "Logarithmic SVG axes should render power-of-ten ticks.");
        Assert(png.Length > 200 && png[0] == 137 && png[1] == 80, "Logarithmic axes should render through the PNG path.");
    }

    private static void LogarithmicAxesRejectNonPositiveData() {
        var chart = Chart.Create()
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddLine("Invalid", new[] { new ChartPoint(1, 0), new ChartPoint(2, 10) });

        AssertThrows<InvalidOperationException>(() => chart.ToSvg(), "Logarithmic axes should reject zero and negative values instead of producing invalid geometry.");
        AssertThrows<InvalidOperationException>(() => chart.ToPng(), "SVG and PNG should enforce the same logarithmic data contract.");
    }

    private static void TimeAxesProvideDeterministicDefaultLabels() {
        var chart = Chart.Create()
            .WithSize(620, 320)
            .WithXAxisScale(ChartScaleKind.Time)
            .AddLine("Daily", new[] {
                new ChartPoint(new DateTime(2026, 7, 1), 10),
                new ChartPoint(new DateTime(2026, 7, 15), 18)
            });

        Assert(chart.ToSvg().Contains("2026-07", StringComparison.Ordinal), "Time axes should provide deterministic invariant date labels when no formatter is supplied.");
    }
}
