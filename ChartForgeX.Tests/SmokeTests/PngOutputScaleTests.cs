using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PngOutputScaleEmitsHighDensityAssets() {
        var normal = BareLineChart().WithPngOutputScale(1);
        var highDensity = BareLineChart().WithPngOutputScale(3);
        var presetDensity = BareLineChart().WithPngOutputScale(ChartPngOutputScale.Print);

        var normalPng = normal.ToPng();
        var highDensityPng = highDensity.ToPng();
        var presetPng = presetDensity.ToPng();
        Assert(ReadBigEndianInt32(normalPng, 16) == 180 && ReadBigEndianInt32(normalPng, 20) == 120, "Default PNG output scale should preserve requested dimensions.");
        Assert(ReadBigEndianInt32(highDensityPng, 16) == 540 && ReadBigEndianInt32(highDensityPng, 20) == 360, "PNG output scale should multiply emitted dimensions.");
        Assert(ReadBigEndianInt32(presetPng, 16) == 720 && ReadBigEndianInt32(presetPng, 20) == 480, "Named PNG output-scale presets should multiply emitted dimensions.");
        Assert(highDensityPng.Length > normalPng.Length, "High-density PNG output should contain more raster data than a 1x export.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPngOutputScale(0), "PNG output scale should reject values below one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.PngOutputScale = 5, "PNG output scale should reject values above four.");

        var normalGrid = ChartGrid.Create().WithPadding(10).WithPanelSize(180, 120).Add(BareLineChart());
        var highDensityGrid = ChartGrid.Create().WithPadding(10).WithPanelSize(180, 120).WithPngOutputScale(ChartPngOutputScale.Retina).Add(BareLineChart());
        var normalGridPng = normalGrid.ToPng();
        var highDensityGridPng = highDensityGrid.ToPng();
        Assert(ReadBigEndianInt32(normalGridPng, 16) == 200 && ReadBigEndianInt32(normalGridPng, 20) == 140, "Default PNG grid output scale should preserve composed dimensions.");
        Assert(ReadBigEndianInt32(highDensityGridPng, 16) == 400 && ReadBigEndianInt32(highDensityGridPng, 20) == 280, "PNG grid output scale should multiply composed dimensions.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithPngOutputScale(0), "PNG grid output scale should reject values below one.");
    }

    private static void PngPrintScaleSurvivesDenseChartStress() {
        foreach (var sample in HighDensityStressCharts()) {
            var png = sample.Chart.WithPngSupersampling(1).WithPngOutputScale(ChartPngOutputScale.Print).ToPng();
            var encodedWidth = ReadBigEndianInt32(png, 16);
            var encodedHeight = ReadBigEndianInt32(png, 20);
            Assert(encodedWidth == sample.Width * 4 && encodedHeight == sample.Height * 4, sample.Name + " should emit exact 4x print-scale PNG dimensions.");

            var rgba = ReadPngRgba(png, out var decodedWidth, out var decodedHeight);
            Assert(decodedWidth == encodedWidth && decodedHeight == encodedHeight, sample.Name + " should decode to the same dimensions advertised by PNG headers.");
            Assert(CountDistinctVisibleColorBuckets(rgba) >= sample.MinimumColorBuckets, sample.Name + " should retain visible high-density detail instead of flattening to a small color set.");
            Assert(png.Length > sample.Width * sample.Height / 3, sample.Name + " should contain enough compressed raster data to indicate rendered chart detail.");
        }
    }

    private static void OverlayPresetEmitsTransparentChromeFreePng() {
        var chart = Chart.Create()
            .WithSize(220, 90)
            .WithOverlay()
            .AddSmoothArea("CPU", Points(28, 34, 31, 45, 72, 66), ChartColor.FromRgb(20, 184, 166));

        Assert(chart.Options.TransparentBackground, "Overlay charts should keep the PNG canvas transparent.");
        Assert(!chart.Options.ShowCard && !chart.Options.ShowPlotBackground, "Overlay charts should not render report card or plot panel surfaces.");
        Assert(!chart.Options.ShowAxes && !chart.Options.ShowGrid && !chart.Options.ShowLegend, "Overlay charts should suppress report axes, grid, and legend by default.");

        var rgba = ReadPngRgba(chart.ToPng(), out var width, out var height);
        Assert(width == 220 && height == 90, "Overlay PNG output should preserve requested dimensions.");
        Assert(CountAlphaInRect(rgba, width, 0, 0, 12, 12) == 0, "Overlay PNG corners should stay fully transparent.");
        Assert(CountAlphaInRect(rgba, width, 40, 24, 140, 42) > 0, "Overlay PNG should still draw visible chart marks.");

        var svg = chart.ToSvg();
        Assert(!svg.Contains("data-cfx-role=\"chart-card\"", StringComparison.Ordinal), "Overlay SVG should not render the report card surface.");
        Assert(!svg.Contains("data-cfx-role=\"plot-background\"", StringComparison.Ordinal), "Overlay SVG should not render the plot background surface.");
    }

    private static Chart BareLineChart() {
        var chart = Chart.Create()
            .WithSize(180, 120)
            .WithPngSupersampling(1)
            .AddLine("Diagonal", new[] { new ChartPoint(1, 1), new ChartPoint(3, 3) }, ChartColor.FromRgb(96, 165, 250));
        chart.Options.ShowAxes = false;
        chart.Options.ShowCard = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;
        return chart;
    }

    private static (string Name, Chart Chart, int Width, int Height, int MinimumColorBuckets)[] HighDensityStressCharts() {
        return new[] {
            ("dense labeled line", Chart.Create()
                .WithTitle("Dense Label Stress")
                .WithSubtitle("Long labels, markers, grid, legend")
                .WithTheme(ChartTheme.ReportLight())
                .WithSize(340, 230)
                .WithXLabels("Primary domains", "Parked domains", "Regional domains", "Acquired domains", "Delegated domains", "Legacy domains")
                .WithDataLabels()
                .AddLine("Coverage", Points(78, 82, 75, 68, 73, 88), ChartColor.FromRgb(37, 99, 235))
                .AddLine("Target", Points(90, 90, 90, 90, 90, 90), ChartColor.FromRgb(16, 185, 129)), 340, 230, 18),
            ("dense heatmap", Chart.Create()
                .WithTitle("Coverage Matrix Stress")
                .WithSubtitle("Compact cell text at print density")
                .WithTheme(ChartTheme.ReportDark())
                .WithSize(360, 240)
                .WithXLabels("SPF", "DKIM", "DMARC", "TLS", "DNSSEC")
                .WithValueFormatter(v => v.ToString("0") + "%")
                .AddHeatmapRow("Primary", Points(94, 88, 76, 71, 64))
                .AddHeatmapRow("Regional", Points(82, 75, 68, 61, 54))
                .AddHeatmapRow("Acquired", Points(68, 52, 44, 37, 31)), 360, 240, 18),
            ("dense treemap", Chart.Create()
                .WithTitle("Finding Mix Stress")
                .WithSubtitle("Tile labels and fractional borders")
                .WithTheme(ChartTheme.ReportLight())
                .WithSize(360, 240)
                .WithValueFormatter(v => v.ToString("0") + "%")
                .AddTreemap("Share", new[] {
                    new ChartTreemapItem("Authentication", 34),
                    new ChartTreemapItem("Certificate lifecycle", 24),
                    new ChartTreemapItem("DNS hygiene", 18),
                    new ChartTreemapItem("Policy drift", 14),
                    new ChartTreemapItem("Monitoring", 10)
                }), 360, 240, 18),
            ("dense sankey", Chart.Create()
                .WithTitle("Flow Stress")
                .WithSubtitle("Curves, nodes, and readable labels")
                .WithTheme(ChartTheme.ReportLight())
                .WithSize(380, 250)
                .WithDataLabels()
                .AddSankey("Findings", new[] {
                    new ChartSankeyLink("Discovered", "Validated", 64),
                    new ChartSankeyLink("Discovered", "Accepted risk", 16),
                    new ChartSankeyLink("Validated", "Owner remediation", 42),
                    new ChartSankeyLink("Validated", "Monitoring", 22),
                    new ChartSankeyLink("Owner remediation", "Closed", 30),
                    new ChartSankeyLink("Owner remediation", "Retesting", 12)
                }), 380, 250, 16)
        };
    }

    private static int CountDistinctVisibleColorBuckets(byte[] rgba) {
        var buckets = new HashSet<int>();
        for (var i = 0; i < rgba.Length; i += 4 * 13) {
            if (rgba[i + 3] < 16) continue;
            var red = rgba[i] >> 4;
            var green = rgba[i + 1] >> 4;
            var blue = rgba[i + 2] >> 4;
            buckets.Add((red << 8) | (green << 4) | blue);
            if (buckets.Count >= 64) break;
        }

        return buckets.Count;
    }
}
