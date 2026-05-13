using ChartForgeX.Primitives;
using ChartForgeX.Simple;
using System;
using System.IO;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SimpleChartApiRendersCommonDefinitions() {
        var output = Path.Combine(Path.GetTempPath(), "chartforgex-simple-chart-api-" + Guid.NewGuid().ToString("N") + ".png");
        Charts.Generate(
            new ChartDefinition[] {
                new ChartLine("CPU", new double[] { 35, 42, 58, 61 }, ChartColor.FromHex("#22C55E"), smooth: true)
            },
            output,
            width: 420,
            height: 260,
            showGrid: true,
            options: new ChartRenderOptions {
                TransparentBackground = true,
                ShowLegend = true,
                Palette = new[] { ChartColor.FromHex("#22C55E"), ChartColor.FromHex("#3B82F6") }
            });

        Assert(File.Exists(output), "Simple chart API should save PNG output.");
        Assert(new FileInfo(output).Length > 64, "Simple chart API should render non-empty PNG output.");
    }
}
