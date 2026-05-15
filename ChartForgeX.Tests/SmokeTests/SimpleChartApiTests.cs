using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Simple;
using System;
using System.IO;
using SimpleChartAnnotationDefinition = ChartForgeX.Simple.ChartAnnotationDefinition;
using SimpleChartBubbleSeries = ChartForgeX.Simple.ChartBubbleSeries;

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

        var chart = Charts.Build(
            new ChartDefinition[] {
                new ChartBar("Memory", new double[] { 62, 68, 71 }, ChartColor.FromHex("#3B82F6"))
            },
            width: 360,
            height: 220,
            options: new ChartRenderOptions {
                UseOverlay = true,
                ShowLegend = true,
                ShowDataLabels = true
            });

        Assert(chart.Series.Count == 1, "Simple chart API should build native ChartForgeX charts without saving first.");
        Assert(chart.Options.TransparentBackground, "Overlay mode should keep the raster background transparent.");
        Assert(!chart.Options.ShowCard, "Overlay mode should remove card chrome.");
        Assert(!chart.Options.ShowPlotBackground, "Overlay mode should remove plot background chrome.");
        Assert(chart.Options.ShowLegend, "Explicit simple options should still override overlay defaults.");
        Assert(chart.ToSvg().Contains("<svg", StringComparison.Ordinal), "Simple-built charts should render through native ChartForgeX extensions.");

        var svgOutput = Path.Combine(Path.GetTempPath(), "chartforgex-simple-chart-api-" + Guid.NewGuid().ToString("N") + ".svg");
        Charts.Save(chart, svgOutput);
        Assert(File.Exists(svgOutput), "Simple chart API should save native ChartForgeX charts by extension.");
        Assert(File.ReadAllText(svgOutput).Contains("<svg", StringComparison.Ordinal), "Simple chart API should save SVG output.");

        var polarArea = Charts.Build(new ChartDefinition[] { new ChartPolarArea("Coverage", new double[] { 12, 18, 9 }, new[] { "Low", "Medium", "High" }) });
        Assert(polarArea.Series[0].Kind == ChartSeriesKind.PolarArea, "Simple polar area definitions should build native polar area series.");
        Assert(polarArea.Options.XAxisLabels.Count == 3, "Simple polar area definitions should preserve optional segment labels.");

        var radar = Charts.Build(new ChartDefinition[] { new ChartRadar("Coverage", new double[] { 1, 2, 3 }, new double[] { 12, 18, 9 }) });
        Assert(radar.Series[0].Kind == ChartSeriesKind.Radar, "Simple radar definitions should build native radar series.");

        var area = Charts.Build(new ChartDefinition[] { new ChartArea("Coverage", new double[] { 2, 4, 3 }) });
        Assert(!area.Series[0].Smooth, "Simple area definitions should create plain area series unless a smooth area model exists.");

        var histograms = Charts.Build(new ChartDefinition[] {
            new ChartHistogram("Fast", new double[] { 1, 2, 3, 4 }, 1),
            new ChartHistogram("Slow", new double[] { 3, 4, 5, 6 }, 1)
        });
        Assert(histograms.Series.Count == 2, "Simple histogram definitions should support comparable multi-series histograms.");
        Assert(histograms.Options.XAxisLabels.Count == histograms.Series[0].Points.Count && histograms.Options.XAxisLabels.Count == histograms.Series[1].Points.Count, "Simple histogram definitions should use one shared binning scheme.");

        Assert(
            Charts.Build(new ChartDefinition[] { new ChartHorizontalBar("Disk", new double[] { 72, 28 }) }).Series[0].Kind == ChartSeriesKind.HorizontalBar,
            "Simple horizontal bar definitions should build native horizontal bar series.");
        Assert(
            Charts.Build(new ChartDefinition[] { new ChartStepLine("Requests", new double[] { 4, 8, 7 }) }).Series[0].Kind == ChartSeriesKind.StepLine,
            "Simple step line definitions should build native step line series.");
        Assert(
            Charts.Build(new ChartDefinition[] { new ChartStepArea("Capacity", new double[] { 4, 8, 7 }) }).Series[0].Kind == ChartSeriesKind.StepArea,
            "Simple step area definitions should build native step area series.");
        Assert(
            Charts.Build(new ChartDefinition[] { new ChartStackedArea("A", new double[] { 2, 3 }), new ChartStackedArea("B", new double[] { 1, 2 }, smooth: true) }).Series.All(series => series.Kind == ChartSeriesKind.StackedArea),
            "Simple stacked area definitions should build native stacked area series.");
        Assert(
            Charts.Build(new ChartDefinition[] { new ChartLollipop("Latency", new double[] { 12, 18, 9 }) }).Series[0].Kind == ChartSeriesKind.Lollipop,
            "Simple lollipop definitions should build native lollipop series.");
        Assert(
            Charts.Build(new ChartDefinition[] { new ChartSlope("Current", 62, 71, "Before", "After") }).Series[0].Kind == ChartSeriesKind.Slope,
            "Simple slope definitions should build native slope series.");

        var slopeLabels = Charts.Build(new ChartDefinition[] {
            new ChartSlope("Previous", 52, 61),
            new ChartSlope("Current", 62, 71, "Before", "After")
        });
        Assert(slopeLabels.Options.XAxisLabels[0].Text == "Before" && slopeLabels.Options.XAxisLabels[1].Text == "After", "Simple slope definitions should preserve explicit axis labels even when the first series omits them.");

        var partialSlopeLabels = Charts.Build(new ChartDefinition[] {
            new ChartSlope("Previous", 52, 61, startLabel: "Before"),
            new ChartSlope("Current", 62, 71, endLabel: "After")
        });
        Assert(partialSlopeLabels.Options.XAxisLabels[0].Text == "Before" && partialSlopeLabels.Options.XAxisLabels[1].Text == "After", "Simple slope definitions should merge compatible partial axis labels.");

        var rangeBar = Charts.Build(new ChartDefinition[] { new ChartRangeBar("Maintenance", new double[] { 1, 2 }, new double[] { 2, 4 }, new double[] { 5, 8 }) });
        Assert(rangeBar.Series[0].Kind == ChartSeriesKind.RangeBar, "Simple range bar definitions should build native range bar series.");

        var rangeBand = Charts.Build(new ChartDefinition[] { new ChartRangeBandSeries("Expected", new double[] { 1, 2 }, new double[] { 8, 9 }, new double[] { 12, 15 }) });
        Assert(rangeBand.Series[0].Kind == ChartSeriesKind.RangeBand, "Simple range band definitions should build native range band series.");

        var rangeArea = Charts.Build(new ChartDefinition[] { new ChartRangeBandSeries("Expected", new double[] { 1, 2 }, new double[] { 8, 9 }, new double[] { 12, 15 }, area: true, smooth: false) });
        Assert(rangeArea.Series[0].Kind == ChartSeriesKind.RangeArea && !rangeArea.Series[0].Smooth, "Simple range band definitions should optionally build plain range area series.");

        var boxPlot = Charts.Build(new ChartDefinition[] { new ChartBoxPlotSeries("Latency", new double[] { 1, 2 }, new double[] { 2, 3 }, new double[] { 4, 5 }, new double[] { 6, 7 }, new double[] { 8, 9 }, new double[] { 10, 11 }) });
        Assert(boxPlot.Series[0].Kind == ChartSeriesKind.BoxPlot, "Simple box plot definitions should build native box plot series.");

        var bullet = Charts.Build(new ChartDefinition[] {
            new ChartBullet("Servers", 92, 95, rangeEnds: new double[] { 70, 85 }),
            new ChartBullet("Workstations", 86, 90)
        });
        Assert(bullet.Series.Count == 2 && bullet.Series.All(series => series.Kind == ChartSeriesKind.Bullet), "Simple bullet definitions should build native bullet rows.");

        var waterfall = Charts.Build(new ChartDefinition[] { new ChartWaterfall("Change", new double[] { 120, -24, 42 }, new[] { "Start", "Churn", "Expansion" }) });
        Assert(waterfall.Series[0].Kind == ChartSeriesKind.Waterfall && waterfall.Options.XAxisLabels.Count == 3, "Simple waterfall definitions should build native waterfall series and preserve labels.");

        var funnel = Charts.Build(new ChartDefinition[] { new ChartFunnel("Visitors", 1000), new ChartFunnel("Trials", 220), new ChartFunnel("Customers", 64) });
        Assert(funnel.Series[0].Kind == ChartSeriesKind.Funnel && funnel.Options.XAxisLabels.Count == 3, "Simple funnel item definitions should build a labeled native funnel chart.");

        var treemap = Charts.Build(new ChartDefinition[] { new ChartTreemap("Servers", 42), new ChartTreemap("Workstations", 120), new ChartTreemap("Laptops", 80) });
        Assert(treemap.Series[0].Kind == ChartSeriesKind.Treemap && treemap.Options.XAxisLabels.Count == 3, "Simple treemap item definitions should build a labeled native treemap chart.");

        var points = ChartPoints.FromValues(10, 20, 30);
        Assert(points.Length == 3 && points[0].X == 1 && points[2].Y == 30, "Core point helpers should create one-based chart points from raw values.");

        var bubbles = ChartBubbles.FromXYSize(new[] { 1d, 2d }, new[] { 3d, 4d }, new[] { 5d, 6d });
        Assert(bubbles.Length == 2 && bubbles[1].Size == 6, "Core bubble helpers should create native bubble values from raw sequences.");

        var annotated = Charts.Build(
            new ChartDefinition[] { new ChartLine("CPU", new double[] { 35, 42, 58 }) },
            annotations: new[] { new SimpleChartAnnotationDefinition(3, 58, "Peak", arrow: true) });
        Assert(annotated.Series.Any(series => string.Equals(series.SemanticRole, "point-callout", StringComparison.Ordinal)), "Arrow annotations should preserve point-callout semantics.");

        var existing = Chart.Create();
        var background = ChartColor.FromRgba(1, 2, 3, 200);
        Charts.ApplySettings(existing, background: background);
        Assert(existing.Options.Theme.Background.Equals(background), "ApplySettings should apply the requested background color.");
        Assert(!existing.Options.TransparentBackground, "ApplySettings should disable transparency when a concrete background is requested.");

        var reusableTheme = ChartForgeX.Themes.ChartTheme.ReportLight();
        var originalBackground = reusableTheme.Background;
        var themed = Charts.Build(
            new ChartDefinition[] { new ChartLine("CPU", new double[] { 1, 2, 3 }, markerSize: 8) },
            theme: reusableTheme,
            background: ChartColor.FromRgba(10, 20, 30, 220));
        Assert(themed.Options.Theme.Background.Equals(ChartColor.FromRgba(10, 20, 30, 220)), "Simple chart background should apply to the chart theme copy.");
        Assert(reusableTheme.Background.Equals(originalBackground), "Simple chart background should not mutate caller-provided themes.");
        Assert(Math.Abs(reusableTheme.MarkerRadius - ChartForgeX.Themes.ChartTheme.ReportLight().MarkerRadius) < 0.000001, "Simple chart marker settings should not mutate caller-provided themes.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(new ChartDefinition[] { new ChartScatter("Bad", new double[] { 1, 2 }, new double[] { 1 }) }),
            "Simple scatter definitions should reject mismatched X/Y value counts instead of truncating data.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(new ChartDefinition[] { null! }),
            "Simple chart definitions should reject null definition entries with a clear argument error.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(
                new ChartDefinition[] { new ChartLine("CPU", new double[] { 35, 42, 58 }) },
                annotations: new SimpleChartAnnotationDefinition[] { null! }),
            "Simple annotations should reject null entries with a clear argument error.");

        AssertThrows<ArgumentOutOfRangeException>(
            () => Charts.Build(new ChartDefinition[] { new ChartRadial("Bad", 140) }),
            "Simple radial definitions should reject out-of-range percentage values instead of clamping.");

        AssertThrows<ArgumentOutOfRangeException>(
            () => Charts.Build(new ChartDefinition[] { new SimpleChartBubbleSeries("Bad", new double[] { 1 }, new double[] { 1 }, new double[] { 0 }) }),
            "Simple bubble definitions should reject non-positive bubble sizes instead of coercing them.");

        AssertThrows<ArgumentException>(
            () => ChartPoints.FromXY(new double[] { 1, 2 }, new double[] { 1 }),
            "Core point helpers should reject mismatched X/Y value counts.");

        AssertThrows<ArgumentOutOfRangeException>(
            () => Charts.Build(new ChartDefinition[] { new ChartHistogram("Bad", new double[] { 1, 2 }, 0) }),
            "Simple histogram definitions should reject non-positive bin sizes.");

        AssertThrows<ArgumentNullException>(
            () => Charts.Build(new ChartDefinition[] { new ChartHistogram("Bad", null!) }),
            "Simple histogram definitions should reject null values with a clear argument error.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(new ChartDefinition[] {
                new ChartHistogram("A", new double[] { 1, 2, 3 }, 1),
                new ChartHistogram("B", new double[] { 1, 2, 3 }, 2)
            }),
            "Simple histogram definitions should reject conflicting bin sizes instead of rendering incomparable bins.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(new ChartDefinition[] { new ChartRadar("Bad", new double[] { 1, 2 }, new double[] { 12, 18 }) }),
            "Simple radar definitions should reject fewer than three categories before render-time validation.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(new ChartDefinition[] { new ChartRangeBar("Bad", new double[] { 1, 2 }, new double[] { 1 }, new double[] { 2, 3 }) }),
            "Simple range bar definitions should reject mismatched interval arrays.");

        AssertThrows<ArgumentOutOfRangeException>(
            () => Charts.Build(new ChartDefinition[] { new ChartBoxPlotSeries("Bad", new double[] { 1 }, new double[] { 5 }, new double[] { 4 }, new double[] { 3 }, new double[] { 2 }, new double[] { 1 }) }),
            "Simple box plot definitions should reject unordered summaries.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(new ChartDefinition[] {
                new ChartPolarArea("A", new double[] { 1, 2, 3 }),
                new ChartPolarArea("B", new double[] { 4, 5, 6 })
            }),
            "Simple polar area definitions should reject multiple definitions before render-time validation.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(
                new ChartDefinition[] { new ChartPie("A", 1), new ChartPie("B", 2) },
                annotations: new[] { new SimpleChartAnnotationDefinition(1, 1, "Peak", arrow: true) }),
            "Simple point-callout annotations should reject exclusive chart kinds before render-time validation.");

        var radial = Charts.Build(new ChartDefinition[] {
            new ChartRadial("Healthy", 70),
            new ChartRadial("Warning", 20)
        });
        Assert(radial.Options.XAxisLabels.Count == 2 && radial.Options.XAxisLabels[0].Text == "Healthy", "Simple radial definitions should preserve item labels.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(new ChartDefinition[] {
                new ChartLine("A", new double[] { 1, 2 }, markerSize: 8),
                new ChartLine("B", new double[] { 2, 3 }, markerSize: 14)
            }),
            "Simple line definitions should reject conflicting marker sizes because marker radius is chart-wide.");

        AssertThrows<ArgumentException>(
            () => Charts.Build(new ChartDefinition[] {
                new ChartSlope("A", 1, 2, "Before", "After"),
                new ChartSlope("B", 3, 4, "Old", "New")
            }),
            "Simple slope definitions should reject conflicting explicit axis labels because slope labels are chart-wide.");

        AssertThrows<ArgumentException>(
            () => Charts.Save(chart, Path.Combine(Path.GetTempPath(), "chartforgex-simple-chart-api-" + Guid.NewGuid().ToString("N") + ".txt")),
            "Simple save helper should reject unsupported file extensions.");
    }
}
