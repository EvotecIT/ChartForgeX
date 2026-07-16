using System;
using System.Diagnostics;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private const long ChartAllocationBudgetBytes = 64L * 1024 * 1024;
    private const long WallpaperAllocationBudgetBytes = 384L * 1024 * 1024;
    private const long TopologyAllocationBudgetBytes = 256L * 1024 * 1024;
    private const long ChartTimeBudgetMilliseconds = 15_000;
    private const long WallpaperTimeBudgetMilliseconds = 15_000;
    private const long TopologyTimeBudgetMilliseconds = 15_000;

    private static void RepresentativeRenderingStaysWithinReleaseBudgets() {
        var chart = Chart.Create()
            .WithTitle("Release performance chart")
            .WithSubtitle("Representative line, area, and bar marks")
            .WithSize(960, 540)
            .WithTheme(ChartTheme.ReportDark())
            .WithLegend()
            .AddLine("Observed", PerformancePoints(72, 48, 19))
            .AddSmoothArea("Forecast", PerformancePoints(72, 42, 13), ChartColor.FromHex("#38BDF8"))
            .AddBar("Capacity", PerformancePoints(72, 56, 7), ChartColor.FromHex("#A78BFA"));
        var chartMeasurement = MeasureRender(() => {
            var svg = chart.ToSvg();
            var png = chart.ToPng();
            Assert(svg.Length > 1_000 && png.Length > 1_000, "Representative chart workload should produce substantive SVG and PNG output.");
        });

        var wallpaper = VisualCanvas.CreateDesktopWallpaper()
            .WithPngOutputScale((int)ChartPngOutputScale.Retina)
            .WithBackground(ChartColor.FromHex("#020617"), ChartColor.FromHex("#0B2447"))
            .WithBackdrop(VisualCanvasBackdropStyle.TechHorizon)
            .AddHeroTitle(520, 354, 880, 78, new[] {
                new VisualCanvasTextRun("DEV-WKS-", ChartColor.FromHex("#F8FAFC")),
                new VisualCanvasTextRun("01", ChartColor.FromHex("#38BDF8"))
            })
            .AddInfoTile(70, 146, 390, 118, "OS", "OPERATING SYSTEM", "Windows 11 Enterprise", "24H2 / build 26100", ChartColor.FromHex("#38BDF8"))
            .AddInfoTile(1460, 146, 390, 118, "CPU", "PROCESSOR", "Intel Core i7", "Average load 23%", ChartColor.FromHex("#38BDF8"), 0.23)
            .AddFeatureStrip(540, 906, 840, 70, new[] {
                new VisualCanvasFeatureItem("OK", "COMPLIANT"),
                new VisualCanvasFeatureItem("VPN", "CONNECTED"),
                new VisualCanvasFeatureItem("EDR", "HEALTHY"),
                new VisualCanvasFeatureItem("IT", "SUPPORTED")
            });
        var wallpaperMeasurement = MeasureRender(() => {
            var png = wallpaper.ToPng();
            Assert(png.Length > 10_000, "Representative 2x desktop wallpaper should produce substantive PNG output.");
        });

        var topology = CreateBusyForceGraphFixture("release-performance-topology", 8, 15)
            .WithTitle("Release performance topology");
        var topologyOptions = new TopologyRenderOptions { IncludeLegend = false }
            .WithForceGraphStyle()
            .WithFitContentToViewport();
        var topologyMeasurement = MeasureRender(() => {
            var svg = topology.ToSvg(topologyOptions);
            var png = topology.ToPng(topologyOptions);
            Assert(svg.Length > 10_000 && png.Length > 10_000, "Representative dense topology should produce substantive SVG and PNG output.");
        });

        var withinBudget =
            chartMeasurement.AllocatedBytes <= ChartAllocationBudgetBytes &&
            chartMeasurement.ElapsedMilliseconds <= ChartTimeBudgetMilliseconds &&
            wallpaperMeasurement.AllocatedBytes <= WallpaperAllocationBudgetBytes &&
            wallpaperMeasurement.ElapsedMilliseconds <= WallpaperTimeBudgetMilliseconds &&
            topologyMeasurement.AllocatedBytes <= TopologyAllocationBudgetBytes &&
            topologyMeasurement.ElapsedMilliseconds <= TopologyTimeBudgetMilliseconds;
        Assert(
            withinBudget,
            "Representative release rendering exceeded a time or allocation budget. " +
            "Chart=" + chartMeasurement + "; Wallpaper=" + wallpaperMeasurement + "; Topology=" + topologyMeasurement + ".");
    }

    private static ChartPoint[] PerformancePoints(int count, double baseline, double amplitude) {
        var points = new ChartPoint[count];
        for (var i = 0; i < points.Length; i++) {
            var wave = Math.Sin(i * 0.31) * amplitude;
            points[i] = new ChartPoint(i + 1, baseline + wave + i * 0.08);
        }

        return points;
    }

    private static RenderBudgetMeasurement MeasureRender(Action render) {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        render();
        stopwatch.Stop();
        var allocatedBytes = Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
        return new RenderBudgetMeasurement(stopwatch.ElapsedMilliseconds, allocatedBytes);
    }

    private readonly struct RenderBudgetMeasurement {
        public RenderBudgetMeasurement(long elapsedMilliseconds, long allocatedBytes) {
            ElapsedMilliseconds = elapsedMilliseconds;
            AllocatedBytes = allocatedBytes;
        }

        public long ElapsedMilliseconds { get; }

        public long AllocatedBytes { get; }

        public override string ToString() => ElapsedMilliseconds + " ms / " + AllocatedBytes + " bytes";
    }
}
