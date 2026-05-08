using System;
using System.Globalization;
using System.IO;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void HistogramValuesRenderAsBinnedBars() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddHistogram("Latency samples", new[] { 1d, 2d, 2d, 3d, 5d }, 2, ChartColor.FromRgb(37, 99, 235));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar\"") == 2, "Histogram values should render one bar per requested bin.");
        Assert(svg.Contains(">1-3</text>", StringComparison.Ordinal), "Histogram bins should render range labels.");
        Assert(svg.Contains(">3-5</text>", StringComparison.Ordinal), "Histogram bins should render range labels.");
        Assert(svg.Contains(">3</text>", StringComparison.Ordinal), "Histogram data labels should render bin counts.");
        Assert(chart.ToPng().Length > 64, "Histogram charts should render PNG output.");
    }

    private static void LollipopSeriesRenderStemsAndMarkers() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .WithXLabels("A", "B", "C")
            .AddLollipop("Coverage", Points(74, 88, 96), ChartColor.FromRgb(37, 99, 235));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"lollipop-stem\"") == 3, "Lollipop charts should render one stem per point.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"lollipop-marker\"") == 3, "Lollipop charts should render one marker per point.");
        Assert(svg.Contains(">96</text>", StringComparison.Ordinal), "Lollipop data labels should render values when enabled.");
        Assert(chart.ToPng().Length > 64, "Lollipop charts should render PNG output.");
    }

    private static void RangeBarSeriesRenderIntervals() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .WithXLabels("DNS", "TCP", "TLS")
            .AddRangeBar("Observed range", new[] {
                new ChartInterval(1, 20, 42),
                new ChartInterval(2, 44, 88),
                new ChartInterval(3, 96, 142)
            }, ChartColor.FromRgb(37, 99, 235));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"range-bar\"") == 3, "Range bar charts should render one interval bar per interval.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"range-bar-cap\"") == 6, "Range bar charts should render two caps per interval.");
        Assert(svg.Contains(">20-42</text>", StringComparison.Ordinal), "Range bar data labels should render interval bounds when enabled.");
        Assert(chart.ToPng().Length > 64, "Range bar charts should render PNG output.");
    }

    private static void BoxPlotSeriesRenderStatisticalSummaries() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .WithXLabels("DNS", "TCP")
            .AddBoxPlot("Latency spread", new[] {
                new ChartBoxPlot(1, 18, 24, 31, 38, 48),
                new ChartBoxPlot(2, 42, 56, 64, 82, 104)
            }, ChartColor.FromRgb(37, 99, 235));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"box-plot\"") == 2, "Box plots should render one group per summary.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"box-body\"") == 2, "Box plots should render quartile boxes.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"box-median\"") == 2, "Box plots should render median lines.");
        Assert(svg.Contains("data-cfx-q1=\"24\"", StringComparison.Ordinal), "Box plots should expose quartile metadata for HTML inspection.");
        Assert(svg.Contains("<title>18-48, median 31</title>", StringComparison.Ordinal), "Box plots should expose browser tooltip metadata.");
        Assert(svg.Contains(">31</text>", StringComparison.Ordinal), "Box plot data labels should render medians when enabled.");
        var raw = Chart.Create().WithSize(640, 360).WithDataLabels().AddBoxPlot("Raw samples", 1, new[] { 18d, 24d, 31d, 38d, 48d }, ChartColor.FromRgb(37, 99, 235)).ToSvg();
        Assert(raw.Contains(">31</text>", StringComparison.Ordinal), "Raw-value box plot overload should compute the median.");
        Assert(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Raster", "PngChartRenderer.BoxPlot.cs")).Contains("PngStrokeHalo", StringComparison.Ordinal), "Box plot PNG strokes should keep readable raster halos.");
        Assert(chart.ToPng().Length > 64, "Box plots should render PNG output.");
    }

    private static void BubbleSeriesRenderScaledMarkers() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddBubble("Risk clusters", new[] {
                new ChartBubble(1, 24, 8),
                new ChartBubble(2, 42, 18),
                new ChartBubble(3, 31, 42)
            }, ChartColor.FromRgb(37, 99, 235));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"bubble\"") == 3, "Bubble charts should render one scaled marker per bubble.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bubble-highlight\"") == 3, "Bubble charts should render marker highlights.");
        Assert(svg.Contains(">42</text>", StringComparison.Ordinal), "Bubble data labels should render size values when enabled.");
        Assert(chart.ToPng().Length > 64, "Bubble charts should render PNG output.");
    }

    private static void TrendLineSeriesRenderComputedRegressionLines() {
        var points = new[] {
            new ChartPoint(1, 12),
            new ChartPoint(2, 19),
            new ChartPoint(3, 24),
            new ChartPoint(4, 33),
            new ChartPoint(5, 41)
        };
        var chart = Chart.Create()
            .WithSize(640, 360)
            .AddScatter("Observed", points, ChartColor.FromRgb(37, 99, 235))
            .AddTrendLine("Trend", points, ChartColor.FromRgb(245, 158, 11));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"trend-line\"") == 1 && CountOccurrences(svg, "data-cfx-role=\"trend-line-halo\"") == 1 && CountOccurrences(svg, "data-cfx-role=\"trend-line-highlight\"") == 1, "Trend lines should render one computed line with reusable premium lighting layers.");
        Assert(svg.Contains("stroke-dasharray=\"8 6\"", StringComparison.Ordinal), "Trend lines should render as dashed reference lines.");
        Assert(svg.Contains("data-cfx-slope=\"", StringComparison.Ordinal), "Trend lines should expose computed slope metadata.");
        Assert(svg.Contains(">Trend</text>", StringComparison.Ordinal), "Trend lines should participate in the legend.");
        Assert(chart.ToPng().Length > 64, "Trend lines should render PNG output.");
    }

    private static void ErrorBarSeriesRenderBoundsAndMarkers() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddErrorBar("Confidence", new[] {
                new ChartErrorBar(1, 42, 35, 51),
                new ChartErrorBar(2, 58, 49, 66),
                new ChartErrorBar(3, 63, 54, 78)
            }, ChartColor.FromRgb(37, 99, 235));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"error-bar\"") == 3, "Error-bar charts should render one group per point estimate.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"error-cap\"") == 6, "Error-bar charts should render two caps per point estimate.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"error-marker\"") == 3, "Error-bar charts should render point estimate markers.");
        Assert(svg.Contains(">63</text>", StringComparison.Ordinal), "Error-bar data labels should render point estimates when enabled.");
        Assert(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Raster", "PngChartRenderer.ErrorBar.cs")).Contains("PngStrokeHalo", StringComparison.Ordinal), "Error-bar PNG strokes should keep readable raster halos.");
        Assert(chart.ToPng().Length > 64, "Error-bar charts should render PNG output.");
    }

    private static void CandlestickSeriesRenderOhlcBodiesAndWicks() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddCandlestick("Windows", new[] {
                new ChartCandlestick(1, 42, 51, 35, 48),
                new ChartCandlestick(2, 58, 66, 49, 54),
                new ChartCandlestick(3, 63, 78, 54, 72)
            });
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"candlestick\"") == 3, "Candlestick charts should render one group per OHLC value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"candlestick-wick\"") == 3, "Candlestick charts should render one wick per OHLC value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"candlestick-body\"") == 3, "Candlestick charts should render one body per OHLC value.");
        Assert(svg.Contains("data-cfx-open=\"42\"", StringComparison.Ordinal), "Candlestick charts should expose OHLC metadata for HTML inspection.");
        Assert(svg.Contains("data-cfx-status=\"rising\"", StringComparison.Ordinal), "Candlestick charts should expose rising/falling status metadata.");
        Assert(svg.Contains("<title>open 42, high 51, low 35, close 48</title>", StringComparison.Ordinal), "Candlestick charts should expose browser tooltip metadata.");
        Assert(svg.Contains(">72</text>", StringComparison.Ordinal), "Candlestick data labels should render close values when enabled.");
        Assert(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Raster", "PngChartRenderer.Candlestick.cs")).Contains("PngStrokeHalo", StringComparison.Ordinal), "Candlestick PNG strokes should keep readable raster halos.");
        Assert(chart.ToPng().Length > 64, "Candlestick charts should render PNG output.");
    }

    private static void OhlcSeriesRenderOpenAndCloseTicks() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddOhlc("Windows", new[] {
                new ChartCandlestick(1, 42, 51, 35, 48),
                new ChartCandlestick(2, 58, 66, 49, 54),
                new ChartCandlestick(3, 63, 78, 54, 72)
            });
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"ohlc\"") == 3, "OHLC charts should render one group per OHLC value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"ohlc-stem\"") == 3, "OHLC charts should render one high-low stem per value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"ohlc-open\"") == 3, "OHLC charts should render one open tick per value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"ohlc-close\"") == 3, "OHLC charts should render one close tick per value.");
        Assert(!svg.Contains("data-cfx-role=\"candlestick-body\"", StringComparison.Ordinal), "OHLC charts should not render candlestick bodies.");
        Assert(svg.Contains("data-cfx-close=\"48\"", StringComparison.Ordinal), "OHLC charts should expose open-high-low-close metadata for HTML inspection.");
        Assert(svg.Contains("<title>open 42, high 51, low 35, close 48</title>", StringComparison.Ordinal), "OHLC charts should expose browser tooltip metadata.");
        Assert(svg.Contains(">72</text>", StringComparison.Ordinal), "OHLC data labels should render close values when enabled.");
        Assert(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Raster", "PngChartRenderer.Ohlc.cs")).Contains("PngStrokeHalo", StringComparison.Ordinal), "OHLC PNG strokes should keep readable raster halos.");
        Assert(chart.ToPng().Length > 64, "OHLC charts should render PNG output.");
    }

    private static void RangeBandSeriesRenderFilledEnvelopes() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddRangeBand("Forecast band", new[] {
                new ChartRangeBand(1, 32, 44),
                new ChartRangeBand(2, 38, 58),
                new ChartRangeBand(3, 51, 72)
            }, ChartColor.FromRgb(37, 99, 235));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"range-band\"") == 1, "Range-band charts should render one filled envelope.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"range-band-upper\"") == 1, "Range-band charts should render an upper boundary.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"range-band-lower\"") == 1, "Range-band charts should render a lower boundary.");
        Assert(svg.Contains(">51-72</text>", StringComparison.Ordinal), "Range-band data labels should render lower and upper values when enabled.");
        Assert(chart.ToPng().Length > 64, "Range-band charts should render PNG output.");
    }

    private static void RangeAreaSeriesRenderFilledIntervalAreas() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddRangeArea("Prediction interval", new[] {
                new ChartRangeBand(1, 32, 44),
                new ChartRangeBand(2, 38, 58),
                new ChartRangeBand(3, 51, 72)
            }, ChartColor.FromRgb(14, 165, 233));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"range-area\"") == 1, "Range-area charts should render one filled interval area.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"range-area-upper\"") == 1, "Range-area charts should render an emphasized upper boundary.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"range-area-lower\"") == 1, "Range-area charts should render an emphasized lower boundary.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"range-area-midline\"") == 1, "Range-area charts should render an interval midpoint guide.");
        Assert(svg.Contains(">51-72</text>", StringComparison.Ordinal), "Range-area data labels should render lower and upper values when enabled.");
        Assert(chart.ToPng().Length > 64, "Range-area charts should render PNG output.");
    }

    private static void StackedAreaSeriesRenderCumulativeFilledBands() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .WithXLabels("Mon", "Tue", "Wed")
            .AddStackedArea("Passed", Points(120, 160, 190), ChartColor.FromRgb(16, 185, 129))
            .AddSmoothStackedArea("Warnings", Points(24, 18, 12), ChartColor.FromRgb(251, 191, 36));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"stacked-area\"") == 2, "Stacked area charts should render one filled band per stacked series.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"stacked-area-line\"") == 2, "Stacked area charts should render one foreground upper boundary per series.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"stacked-area-line-halo\"") == 2 && CountOccurrences(svg, "data-cfx-role=\"stacked-area-line-highlight\"") == 2, "Stacked area charts should render reusable boundary halos and highlights.");
        Assert(GetAttribute(svg, "<clipPath", "x") > 0 && svg.Contains(">Warnings</text>", StringComparison.Ordinal), "Stacked area charts should keep using the shared cartesian plot clip and render legend labels.");
        Assert(chart.ToPng().Length > 64, "Stacked area charts should render PNG output.");
    }

    private static void SlopeSeriesRenderEndpointComparisons() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddSlope("DMARC", 58, 88, "Before", "After", ChartColor.FromRgb(37, 99, 235))
            .AddSlope("DNSSEC", 42, 74, ChartColor.FromRgb(16, 185, 129));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"slope\"") == 2, "Slope charts should render one comparison group per slope series.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"slope-line\"") == 2 && CountOccurrences(svg, "data-cfx-role=\"slope-line-halo\"") == 2 && CountOccurrences(svg, "data-cfx-role=\"slope-line-highlight\"") == 2, "Slope charts should render foreground, halo, and highlight layers with distinct roles.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"slope-start\"") == 2, "Slope charts should render one start marker per comparison.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"slope-end\"") == 2, "Slope charts should render one end marker per comparison.");
        Assert(svg.Contains(">Before</text>", StringComparison.Ordinal), "Slope charts should apply endpoint labels to the x-axis.");
        Assert(svg.Contains(">After</text>", StringComparison.Ordinal), "Slope charts should apply endpoint labels to the x-axis.");
        Assert(svg.Contains(">88</text>", StringComparison.Ordinal), "Slope data labels should render endpoint values when enabled.");
        Assert(chart.ToPng().Length > 64, "Slope charts should render PNG output.");
    }

    private static void DumbbellSeriesRenderPairedComparisons() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddDumbbell("Before/after", new[] {
                new ChartDumbbell(1, 32, 44),
                new ChartDumbbell(2, 38, 58),
                new ChartDumbbell(3, 51, 72)
            }, ChartColor.FromRgb(37, 99, 235));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"dumbbell\"") == 3, "Dumbbell charts should render one group per paired value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dumbbell-connector\"") == 3, "Dumbbell charts should render one connector per paired value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dumbbell-start\"") == 3, "Dumbbell charts should render one start marker per paired value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dumbbell-end\"") == 3, "Dumbbell charts should render one end marker per paired value.");
        Assert(svg.Contains(">51-72</text>", StringComparison.Ordinal), "Dumbbell data labels should render paired values when enabled.");
        Assert(chart.ToPng().Length > 64, "Dumbbell charts should render PNG output.");
    }

    private static void ParetoItemsRenderSortedBarsAndCumulativeLine() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .AddPareto("Findings", new[] {
                new ChartParetoItem("Medium", 30),
                new ChartParetoItem("Critical", 50),
                new ChartParetoItem("Low", 20)
            }, ChartColor.FromRgb(37, 99, 235), ChartColor.FromRgb(245, 158, 11));
        var svg = chart.ToSvg();
        Assert(chart.Series.Count == 2, "Pareto charts should add contribution bars and a cumulative line.");
        Assert(chart.Series[0].Kind == ChartSeriesKind.Bar, "Pareto contribution values should render as bars.");
        Assert(chart.Series[1].Kind == ChartSeriesKind.Line, "Pareto cumulative values should render as a line.");
        Assert(chart.Options.XAxisLabels[0].Text == "Critical", "Pareto charts should sort categories by descending value.");
        Assert(Math.Abs(chart.Series[0].Points[0].Y - 50) < 0.001, "Pareto contribution bars should use percentage share.");
        Assert(Math.Abs(chart.Series[1].Points[2].Y - 100) < 0.001, "Pareto cumulative line should end at one hundred percent.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar\"") == 3, "Pareto charts should render one bar per item.");
        Assert(CountOccurrences(svg, ">50%</text>") == 1, "Pareto charts should avoid duplicate first-point bar and cumulative labels.");
        Assert(svg.Contains(">100%</text>", StringComparison.Ordinal), "Pareto data labels should include cumulative percentages.");
        Assert(svg.Contains(">Cumulative Findings</text>", StringComparison.Ordinal), "Pareto charts should render the cumulative series legend.");
        Assert(chart.ToPng().Length > 64, "Pareto charts should render PNG output.");
    }

    private static void BarLineComboRendersBarsAndLine() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithYAxis("Volume")
            .WithSecondaryYAxis("Rate", value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .WithSecondaryYAxisBounds(0, 100)
            .WithXLabels("Mon", "Tue", "Wed")
            .AddBarLineCombo(
                "Volume",
                Points(4200, 6400, 5800),
                "Pass rate",
                Points(88, 93, 91),
                ChartColor.FromRgb(37, 99, 235),
                ChartColor.FromRgb(14, 165, 233),
                smoothLine: true,
                lineAxis: ChartAxisSide.Secondary);

        var svg = chart.ToSvg();
        Assert(chart.Series.Count == 2, "Bar-line combos should add exactly two series.");
        Assert(chart.Series[0].Kind == ChartSeriesKind.Bar, "Bar-line combos should add bars first so lines render on top.");
        Assert(chart.Series[1].Kind == ChartSeriesKind.Line && chart.Series[1].Smooth, "Bar-line combos should add the requested smooth line series.");
        Assert(chart.Series[1].YAxis == ChartAxisSide.Secondary, "Bar-line combos should allow the overlaid line to use the secondary y-axis.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar\"") == 3, "Bar-line combos should render one bar per bar point.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"line\"") == 1, "Bar-line combos should render one visible line path.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"line-marker\"") == 3, "Bar-line combos should render one marker per line point.");
        Assert(svg.Contains("data-cfx-role=\"secondary-y-axis\"", StringComparison.Ordinal), "Secondary-axis combos should render a right-side SVG axis.");
        Assert(svg.Contains("data-cfx-role=\"secondary-y-axis-title\"", StringComparison.Ordinal), "Secondary-axis combos should render the right-side axis title.");
        Assert(svg.Contains(">100%</text>", StringComparison.Ordinal), "Secondary-axis combos should use the secondary tick formatter.");
        Assert(svg.Contains(">Volume</text>", StringComparison.Ordinal), "Bar-line combos should render the bar legend label.");
        Assert(svg.Contains(">Pass rate</text>", StringComparison.Ordinal), "Bar-line combos should render the line legend label.");
        Assert(chart.ToPng().Length > 64, "Bar-line combos should render PNG output.");

        var secondaryOnly = Chart.Create().WithSize(420, 280).WithSecondaryYAxis("Rate").AddLine("Rate", Points(88, 93, 91));
        secondaryOnly.Series[0].UseSecondaryYAxis();
        Assert(!secondaryOnly.ToSvg().Contains("Infinity", StringComparison.Ordinal), "Secondary-axis-only charts should still render finite SVG geometry.");
    }

    private static void MultipleBarSeriesRenderAsGroupedBars() {
        var svg = Chart.Create().WithSize(640, 360).WithXLabels("Mon", "Tue", "Wed").AddBar("Current", Points(10, 20, 30)).AddBar("Previous", Points(8, 18, 22)).ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar\"") == 6, "Two bar series with three points each should render six bar rectangles.");
        var firstCurrent = GetAttribute(svg, "data-cfx-series=\"0\" data-cfx-point=\"0\"", "x");
        var firstPrevious = GetAttribute(svg, "data-cfx-series=\"1\" data-cfx-point=\"0\"", "x");
        Assert(Math.Abs(firstCurrent - firstPrevious) > 1, "Grouped bars for the same category should render at distinct x positions.");
    }

    private static void HorizontalBarSeriesRenderCategoryBars() {
        var svg = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .WithXLabels("SPF alignment", "DMARC policy", "DNSSEC coverage")
            .AddHorizontalBar("Coverage", Points(100, 76, 58), ChartColor.FromRgb(37, 99, 235))
            .ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"horizontal-bar\"") == 3, "Horizontal bar series should render one rectangle per category.");
        Assert(svg.Contains(">SPF alignment</text>", StringComparison.Ordinal), "Horizontal bar charts should render category labels on the y-axis.");
        Assert(svg.Contains(">100</text>", StringComparison.Ordinal), "Horizontal bar charts should render data labels when enabled.");
        Assert(GetAttribute(svg, "data-cfx-role=\"horizontal-bar\" data-cfx-series=\"0\" data-cfx-point=\"0\"", "width") > GetAttribute(svg, "data-cfx-role=\"horizontal-bar\" data-cfx-series=\"0\" data-cfx-point=\"2\"", "width"), "Larger horizontal bar values should produce wider bars.");
    }

    private static void StackedHorizontalBarsRenderSegmentsAndTotals() {
        var chart = Chart.Create()
            .WithSize(700, 380)
            .WithStackedHorizontalBars()
            .WithStackTotals()
            .WithXLabels("Mail authentication", "Transport security")
            .AddHorizontalBar("Complete", Points(40, 55), ChartColor.FromRgb(16, 185, 129))
            .AddHorizontalBar("Partial", Points(15, 20), ChartColor.FromRgb(245, 158, 11))
            .AddHorizontalBar("Missing", Points(5, 10), ChartColor.FromRgb(239, 68, 68));

        var svg = chart.ToSvg();
        Assert(chart.Options.BarMode == ChartBarMode.Stacked, "Stacked horizontal bars should use stacked bar mode.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"horizontal-bar\"") == 6, "Stacked horizontal bars should render one segment per series point.");
        var firstCompleteY = GetAttribute(svg, "data-cfx-role=\"horizontal-bar\" data-cfx-series=\"0\" data-cfx-point=\"0\"", "y");
        var firstPartialY = GetAttribute(svg, "data-cfx-role=\"horizontal-bar\" data-cfx-series=\"1\" data-cfx-point=\"0\"", "y");
        var firstCompleteX = GetAttribute(svg, "data-cfx-role=\"horizontal-bar\" data-cfx-series=\"0\" data-cfx-point=\"0\"", "x");
        var firstPartialX = GetAttribute(svg, "data-cfx-role=\"horizontal-bar\" data-cfx-series=\"1\" data-cfx-point=\"0\"", "x");
        Assert(Math.Abs(firstCompleteY - firstPartialY) < 0.001, "Stacked horizontal segments for one category should share the same vertical lane.");
        Assert(firstPartialX > firstCompleteX, "Stacked horizontal segments should begin after earlier same-sign segments.");
        Assert(svg.Contains(">60</text>", StringComparison.Ordinal), "Stacked horizontal bars should render positive totals when requested.");
        Assert(chart.ToPng().Length > 64, "Stacked horizontal bars should render PNG output.");
    }

    private static void HeatmapRowsRenderMatrixCells() {
        var svg = Chart.Create()
            .WithSize(720, 420)
            .WithDataLabels()
            .WithXAxis("Control")
            .WithYAxis("Domain group")
            .WithHeatmapScale(ChartHeatmapScale.Semantic)
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .WithXLabels("SPF", "DMARC", "DNSSEC")
            .AddHeatmapRow("Primary", Points(100, 60, 0))
            .AddHeatmapRow("Parked", Points(82, 40, 20))
            .ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"heatmap-cell\"") == 6, "Two heatmap rows with three values each should render six cells.");
        Assert(svg.Contains("data-cfx-role=\"heatmap\"", StringComparison.Ordinal), "Heatmaps should expose a role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"heatmap-row-label\"") == 2, "Heatmaps should expose row label markers.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"heatmap-column-label\"") == 3, "Heatmaps should expose column label markers.");
        Assert(svg.Contains("data-cfx-role=\"heatmap-x-axis-title\"", StringComparison.Ordinal), "Heatmaps should mark the x-axis title.");
        Assert(svg.Contains("data-cfx-role=\"heatmap-y-axis-title\"", StringComparison.Ordinal), "Heatmaps should mark the y-axis title.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"heatmap-scale-label\"") == 2, "Heatmap scales should expose min and max labels.");
        Assert(svg.Contains("data-cfx-status=\"positive\"", StringComparison.Ordinal), "Semantic heatmaps should expose positive cell status.");
        Assert(svg.Contains("data-cfx-status=\"warning\"", StringComparison.Ordinal), "Semantic heatmaps should expose warning cell status.");
        Assert(svg.Contains("data-cfx-status=\"negative\"", StringComparison.Ordinal), "Semantic heatmaps should expose negative cell status.");
        Assert(svg.Contains("fill=\"#10B981\"", StringComparison.Ordinal), "Semantic heatmap high values should use the positive theme color.");
        Assert(svg.Contains("fill=\"#F59E0B\"", StringComparison.Ordinal), "Semantic heatmap warning values should use the warning theme color.");
        Assert(svg.Contains("fill=\"#EF4444\"", StringComparison.Ordinal), "Semantic heatmap low values should use the negative theme color.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Primary, SPF: 100%, positive\"", StringComparison.Ordinal), "Semantic heatmap cells should expose accessible summaries.");
        Assert(svg.Contains("<title>Primary, SPF: 100%, positive</title>", StringComparison.Ordinal), "Heatmap cells should expose native SVG hover titles.");
        Assert(svg.Contains(">Primary</text>", StringComparison.Ordinal), "Heatmaps should render row labels.");
        Assert(svg.Contains(">DMARC</text>", StringComparison.Ordinal), "Heatmaps should render column labels.");
        Assert(svg.Contains(">100%</text>", StringComparison.Ordinal), "Heatmaps should render optional data labels.");
        var edgeSvg = Chart.Create()
            .WithSize(520, 320)
            .WithXLabels("Very long first control", "Middle", "Very long final control")
            .AddHeatmapRow("Domains", Points(100, 60, 0))
            .ToSvg();
        Assert(edgeSvg.Contains("data-cfx-role=\"heatmap-column-label\"", StringComparison.Ordinal) && edgeSvg.Contains("text-anchor=\"start\"", StringComparison.Ordinal), "Left-edge heatmap column labels should start-align.");
        Assert(edgeSvg.Contains("data-cfx-role=\"heatmap-column-label\"", StringComparison.Ordinal) && edgeSvg.Contains("text-anchor=\"end\"", StringComparison.Ordinal), "Right-edge heatmap column labels should end-align.");
    }

    private static void GaugeSeriesRenderValueArcs() {
        var svg = Chart.Create()
            .WithSize(640, 420)
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .AddGauge("Security score", 87, 0, 100)
            .ToSvg();
        Assert(svg.Contains("data-cfx-role=\"gauge\"", StringComparison.Ordinal), "Gauges should expose a role marker.");
        Assert(svg.Contains("data-cfx-status=\"positive\"", StringComparison.Ordinal), "Gauges should expose status metadata.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Security score: 87%, positive\"", StringComparison.Ordinal), "Gauges should expose accessible summaries.");
        Assert(svg.Contains("data-cfx-role=\"gauge-track\"", StringComparison.Ordinal), "Gauges should render a track arc.");
        Assert(svg.Contains("data-cfx-role=\"gauge-value\"", StringComparison.Ordinal), "Gauges should render a value arc.");
        Assert(svg.Contains("data-cfx-role=\"gauge-status-marker\"", StringComparison.Ordinal), "Gauges should render a visible status marker.");
        Assert(svg.Contains("data-cfx-role=\"gauge-status-label\"", StringComparison.Ordinal), "Gauges should render a visible status label.");
        Assert(svg.Contains("stroke=\"#10B981\"", StringComparison.Ordinal), "Positive gauges should use the positive theme color when no explicit color is set.");
        Assert(svg.Contains(">87%</text>", StringComparison.Ordinal), "Gauges should render the formatted value label.");
        var statuses = Chart.Create().AddGauge("Low", 42).ToSvg() + Chart.Create().AddGauge("Warning", 72).ToSvg();
        Assert(statuses.Contains("data-cfx-status=\"negative\"", StringComparison.Ordinal), "Low gauges should expose negative status.");
        Assert(statuses.Contains("data-cfx-status=\"warning\"", StringComparison.Ordinal), "Mid-range gauges should expose warning status.");
    }

    private static void RadialBarSeriesRenderProgressRings() {
        var chart = Chart.Create()
            .WithSize(720, 460)
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .WithXLabels("Mail auth", "DNSSEC", "TLS")
            .AddRadialBar("Control coverage", Points(92, 74, 88));

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"radial-bar-chart\"", StringComparison.Ordinal), "Radial bars should expose a chart role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"radial-bar-track\"") == 3, "Radial bars should render one track per value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"radial-bar-ring\"") == 3, "Radial bars should render one progress ring per value.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Mail auth: 92%\"", StringComparison.Ordinal), "Radial bars should expose accessible ring summaries.");
        Assert(svg.Contains("data-cfx-role=\"radial-bar-total\"", StringComparison.Ordinal), "Radial bars should render a center total label.");
        Assert(svg.Contains("data-cfx-role=\"radial-bar-legend-label\"", StringComparison.Ordinal), "Radial bars should render ring labels.");
        Assert(svg.Contains(">DNSSEC</text>", StringComparison.Ordinal), "Radial bars should render axis labels through the legend.");
        Assert(chart.ToPng().Length > 64, "Radial bars should render PNG output.");
    }

    private static void GanttTasksRenderProgressDependenciesAndMilestones() {
        var chart = Chart.Create()
            .WithSize(900, 480)
            .WithDataLabels()
            .WithGanttToday(new DateTime(2026, 2, 10))
            .AddGanttTask("Inventory scope", new DateTime(2026, 1, 5), new DateTime(2026, 1, 24), 0.75, color: ChartColor.FromRgb(37, 99, 235))
            .AddGanttTask("Owner remediation", new DateTime(2026, 1, 20), new DateTime(2026, 2, 24), 0.55, dependsOn: 0, color: ChartColor.FromRgb(16, 185, 129))
            .AddGanttMilestone("Executive sign-off", new DateTime(2026, 3, 2), dependsOn: 1, color: ChartColor.FromRgb(245, 158, 11));

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"gantt-chart\"", StringComparison.Ordinal), "Gantt charts should expose a chart role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"gantt-task\"") == 2, "Gantt charts should render task bars.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"gantt-progress\"") == 2, "Gantt charts should render progress fills.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"gantt-dependency\"") == 2, "Gantt charts should render dependency connectors.");
        Assert(svg.Contains("data-cfx-role=\"gantt-milestone\"", StringComparison.Ordinal), "Gantt charts should render milestones.");
        Assert(svg.Contains("data-cfx-role=\"gantt-today\"", StringComparison.Ordinal), "Gantt charts should render today markers.");
        Assert(svg.Contains("data-cfx-progress=\"0.75\"", StringComparison.Ordinal), "Gantt task bars should expose progress metadata.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Inventory scope: Jan 5 to Jan 24, 75% complete\"", StringComparison.Ordinal), "Gantt tasks should expose accessible summaries.");
        Assert(svg.Contains(">75%</text>", StringComparison.Ordinal), "Gantt charts should render progress labels when enabled.");
        Assert(chart.ToPng().Length > 64, "Gantt charts should render PNG output.");
    }

    private static void SankeyLinksRenderWeightedFlows() {
        var chart = Chart.Create()
            .WithSize(900, 520)
            .WithDataLabels()
            .AddSankey("Finding flow", new[] {
                new ChartSankeyLink("Discovered", "Validated", 70),
                new ChartSankeyLink("Discovered", "Accepted risk", 20),
                new ChartSankeyLink("Validated", "Remediated", 44),
                new ChartSankeyLink("Validated", "Monitoring", 26)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"sankey-chart\"", StringComparison.Ordinal), "Sankey charts should expose a chart role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"sankey-link\"") == 4, "Sankey charts should render one weighted ribbon per link.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"sankey-node\"") == 5, "Sankey charts should render deduplicated named nodes.");
        Assert(svg.Contains("data-cfx-value=\"70\"", StringComparison.Ordinal), "Sankey links should expose value metadata.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Discovered to Validated: 70\"", StringComparison.Ordinal), "Sankey links should expose accessible summaries.");
        Assert(svg.Contains(">Validated</text>", StringComparison.Ordinal), "Sankey charts should render node labels when data labels are enabled.");
        Assert(chart.ToPng().Length > 64, "Sankey charts should render PNG output.");
    }

    private static void TreeLinksRenderHierarchy() {
        var chart = Chart.Create()
            .WithSize(900, 520)
            .AddTree("Control hierarchy", new[] {
                new ChartTreeLink("Security posture", "Mail authentication"),
                new ChartTreeLink("Security posture", "Certificate lifecycle"),
                new ChartTreeLink("Mail authentication", "SPF"),
                new ChartTreeLink("Mail authentication", "DKIM"),
                new ChartTreeLink("Certificate lifecycle", "Expiry monitoring")
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"tree-chart\"", StringComparison.Ordinal), "Tree charts should expose a chart role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"tree-link\"") == 5, "Tree charts should render one connector per link.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"tree-node\"") == 6, "Tree charts should render deduplicated named nodes.");
        Assert(svg.Contains("data-cfx-depth=\"2\"", StringComparison.Ordinal), "Tree nodes should expose depth metadata.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Security posture: level 0\"", StringComparison.Ordinal), "Tree nodes should expose accessible summaries.");
        Assert(svg.Contains(">Mail authentication</text>", StringComparison.Ordinal) || (svg.Contains(">Mail</text>", StringComparison.Ordinal) && svg.Contains(">authentication</text>", StringComparison.Ordinal)), "Tree charts should render readable node labels.");
        Assert(chart.ToPng().Length > 64, "Tree charts should render PNG output.");
    }

    private static void BulletSeriesRenderTargetAndRangeBars() {
        var svg = Chart.Create()
            .WithSize(720, 420)
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .AddBullet("DMARC enforcement", 88, 95, 0, 100, new[] { 60d, 80d }, ChartColor.FromRgb(37, 99, 235))
            .AddBullet("DNSSEC coverage", 74, 90, 0, 100, new[] { 60d, 80d }, ChartColor.FromRgb(14, 165, 233))
            .ToSvg();
        Assert(svg.Contains("data-cfx-role=\"bullet-chart\"", StringComparison.Ordinal), "Bullet charts should expose a role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bullet-row\"") == 2, "Two bullet series should render two bullet rows.");
        Assert(svg.Contains("data-cfx-role=\"bullet-row\" data-cfx-series=\"0\" data-cfx-status=\"below-target\"", StringComparison.Ordinal), "Bullet rows should expose value-versus-target status.");
        Assert(svg.Contains("data-cfx-role=\"bullet-row\" data-cfx-series=\"1\" data-cfx-status=\"below-target\"", StringComparison.Ordinal), "Bullet rows should expose value-versus-target status.");
        Assert(svg.Contains("role=\"group\" aria-label=\"DMARC enforcement: 88%, target 95%, below target\"", StringComparison.Ordinal), "Bullet rows should expose accessible summaries.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bullet-status-marker\"") == 2, "Bullet rows should render visible status markers.");
        Assert(svg.Contains("fill=\"#EF4444\"", StringComparison.Ordinal), "Below-target bullet status markers should use the negative theme color.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bullet-row-label\"") == 2, "Bullet rows should expose row label markers.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bullet-target\"") == 2, "Bullet rows should render target markers.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bullet-target-label\"") == 2, "Bullet rows should render target value labels.");
        Assert(svg.Contains("data-cfx-role=\"bullet-axis\"", StringComparison.Ordinal), "Bullet charts should expose an axis group marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bullet-axis-tick\"") == 3, "Bullet charts should render three axis ticks.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bullet-axis-label\"") == 3, "Bullet charts should render three axis tick labels.");
        Assert(svg.Contains("data-cfx-role=\"bullet-axis-label\" x=\"", StringComparison.Ordinal) && svg.Contains("text-anchor=\"start\"", StringComparison.Ordinal), "Left-edge bullet axis labels should start-align.");
        Assert(svg.Contains("data-cfx-role=\"bullet-axis-label\" x=\"", StringComparison.Ordinal) && svg.Contains("text-anchor=\"end\"", StringComparison.Ordinal), "Right-edge bullet axis labels should end-align.");
        Assert(svg.Contains(">DMARC enforcement</text>", StringComparison.Ordinal), "Bullet charts should render row labels.");
        Assert(svg.Contains(">88%</text>", StringComparison.Ordinal), "Bullet charts should render value labels.");
        Assert(svg.Contains(">target 95%</text>", StringComparison.Ordinal), "Bullet charts should render target labels.");
        var edgeSvg = Chart.Create().AddBullet("Edge low", 5, 0).AddBullet("Edge high", 95, 100).ToSvg();
        Assert(edgeSvg.Contains("data-cfx-role=\"bullet-target-label\"", StringComparison.Ordinal) && edgeSvg.Contains("text-anchor=\"start\"", StringComparison.Ordinal), "Left-edge bullet target labels should start-align.");
        Assert(edgeSvg.Contains("data-cfx-role=\"bullet-target-label\"", StringComparison.Ordinal) && edgeSvg.Contains("text-anchor=\"end\"", StringComparison.Ordinal), "Right-edge bullet target labels should end-align.");
        var statusSvg = Chart.Create().AddBullet("Below", 80, 90).AddBullet("Meets", 90, 90).AddBullet("Above", 95, 90).ToSvg();
        Assert(statusSvg.Contains("data-cfx-status=\"below-target\"", StringComparison.Ordinal), "Bullet rows should identify below-target values.");
        Assert(statusSvg.Contains("data-cfx-status=\"meets-target\"", StringComparison.Ordinal), "Bullet rows should identify target-matching values.");
        Assert(statusSvg.Contains("data-cfx-status=\"above-target\"", StringComparison.Ordinal), "Bullet rows should identify above-target values.");
        Assert(Chart.Create().AddBullet("Score", 80, 90).ToPng().Length > 64, "Bullet charts should render PNG output.");
    }

    private static void SpecializedChartsEscapeTextLabels() {
        const string unsafeLabel = "A < B & C \"quoted\"";
        const string escapedLabel = "A &lt; B &amp; C &quot;quoted&quot;";
        var outputs = new[] {
            ("bullet", Chart.Create().AddBullet(unsafeLabel, 80, 90).ToSvg()),
            ("heatmap", Chart.Create().WithXLabels(unsafeLabel).AddHeatmapRow(unsafeLabel, Points(96)).ToSvg()),
            ("funnel", Chart.Create().WithXLabels(unsafeLabel).AddFunnel("Funnel", Points(96)).ToSvg()),
            ("radar", Chart.Create().WithXLabels(unsafeLabel, "Safe", "Also safe").AddRadar("Radar", Points(96, 88, 74)).ToSvg()),
            ("polar area", Chart.Create().WithXLabels(unsafeLabel, "Safe", "Also safe").AddPolarArea("Polar", Points(96, 88, 74)).ToSvg()),
            ("timeline", Chart.Create().AddTimelineItem(unsafeLabel, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1)).ToSvg()),
            ("gantt", Chart.Create().AddGanttTask(unsafeLabel, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1), 0.5).ToSvg()),
            ("sankey", Chart.Create().WithDataLabels().AddSankey("Flow", new[] { new ChartSankeyLink(unsafeLabel, "Safe", 10) }).ToSvg()),
            ("tree", Chart.Create().AddTree("Tree", new[] { new ChartTreeLink(unsafeLabel, "Safe") }).ToSvg()),
            ("gauge", Chart.Create().AddGauge(unsafeLabel, 87).ToSvg()),
            ("donut", Chart.Create().WithXLabels(unsafeLabel).AddDonut(unsafeLabel, Points(100)).ToSvg())
        };
        foreach (var output in outputs) {
            Assert(output.Item2.Contains(escapedLabel, StringComparison.Ordinal), output.Item1 + " labels should be escaped in SVG text nodes.");
            Assert(!output.Item2.Contains(">" + unsafeLabel + "</text>", StringComparison.Ordinal), output.Item1 + " labels should not render raw text.");
        }
    }

    private static void SpecializedChartsFitLongSvgLabels() {
        const string longLabel = "Extremely long remediation status label that must fit";
        var funnel = Chart.Create()
            .WithSize(320, 220)
            .WithXLabels(longLabel, "Verified")
            .AddFunnel("Funnel", Points(96, 72))
            .ToSvg();
        Assert(funnel.Contains("data-cfx-role=\"funnel-label\"", StringComparison.Ordinal), "Funnel charts should mark fitted segment labels.");
        Assert(funnel.Contains("...</text>", StringComparison.Ordinal), "Funnel segment labels should shorten when a stage is narrower than the label.");

        var gauge = Chart.Create()
            .WithSize(260, 180)
            .WithValueFormatter(_ => longLabel)
            .AddGauge(longLabel, 87)
            .ToSvg();
        Assert(gauge.Contains("data-cfx-role=\"gauge-label\"", StringComparison.Ordinal), "Gauges should mark fitted value labels.");
        Assert(gauge.Contains("...</text>", StringComparison.Ordinal), "Gauge labels should shorten when values or names exceed the dial label width.");

        var donut = Chart.Create()
            .WithSize(280, 180)
            .WithXLabels(longLabel)
            .AddDonut(longLabel, Points(100))
            .ToSvg();
        Assert(donut.Contains("...</text>", StringComparison.Ordinal), "Donut center and legend labels should shorten when their available width is constrained.");

        var bullet = Chart.Create()
            .WithSize(320, 220)
            .WithValueFormatter(_ => longLabel)
            .AddBullet(longLabel, 82, 90)
            .ToSvg();
        Assert(bullet.Contains("data-cfx-role=\"bullet-row-label\"", StringComparison.Ordinal), "Bullet charts should mark fitted row labels.");
        Assert(bullet.Contains("...</text>", StringComparison.Ordinal), "Bullet row, value, and target labels should shorten when reserves are constrained.");

        var heatmap = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .WithValueFormatter(_ => longLabel)
            .WithXLabels(longLabel)
            .AddHeatmapRow(longLabel, Points(96))
            .ToSvg();
        Assert(heatmap.Contains("data-cfx-role=\"heatmap-row-label\"", StringComparison.Ordinal), "Heatmaps should mark fitted row labels.");
        Assert(heatmap.Contains("...</text>", StringComparison.Ordinal), "Heatmap row, column, and cell labels should shorten inside constrained regions.");

        var timeline = Chart.Create()
            .WithSize(320, 220)
            .WithXDateLabels(new[] { new DateTime(2026, 1, 1), new DateTime(2026, 1, 15) }, longLabel)
            .AddTimelineItem(longLabel, new DateTime(2026, 1, 1), new DateTime(2026, 1, 15))
            .ToSvg();
        Assert(timeline.Contains("data-cfx-role=\"timeline-row-label\"", StringComparison.Ordinal), "Timelines should mark fitted row labels.");
        Assert(timeline.Contains("...</text>", StringComparison.Ordinal), "Timeline row and tick labels should shorten when their reserved regions are constrained.");
    }

    private static void WaterfallSeriesRenderCumulativeChangeBars() {
        var svg = Chart.Create()
            .WithSize(760, 420)
            .WithDataLabels()
            .WithXAxis("Change")
            .WithYAxis("Open findings")
            .WithXLabels("Opened", "Resolved", "Suppressed", "New risk")
            .AddWaterfall("Finding delta", Points(18, -42, -12, 9), ChartColor.FromRgb(52, 211, 153))
            .ToSvg();
        Assert(svg.Contains("data-cfx-role=\"waterfall-chart\"", StringComparison.Ordinal), "Waterfall charts should expose a role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"waterfall-bar\"") == 5, "Four waterfall steps should render four change bars plus a total bar.");
        Assert(svg.Contains("data-cfx-status=\"positive\"", StringComparison.Ordinal), "Waterfall positive deltas should expose status metadata.");
        Assert(svg.Contains("data-cfx-status=\"negative\"", StringComparison.Ordinal), "Waterfall negative deltas should expose status metadata.");
        Assert(svg.Contains("data-cfx-status=\"total\"", StringComparison.Ordinal), "Waterfall total bars should expose status metadata.");
        Assert(svg.Contains("fill=\"#10B981\"", StringComparison.Ordinal), "Waterfall positive bars should use the positive theme color.");
        Assert(svg.Contains("fill=\"#EF4444\"", StringComparison.Ordinal), "Waterfall negative bars should use the negative theme color.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Opened: +18, positive\"", StringComparison.Ordinal), "Waterfall bars should expose accessible summaries.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"waterfall-connector\"") == 4, "Waterfall bars should render connectors between cumulative steps.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"waterfall-x-axis-label\"") == 5, "Waterfall charts should mark all category labels.");
        Assert(svg.Contains("data-cfx-role=\"waterfall-x-axis-title\"", StringComparison.Ordinal), "Waterfall charts should mark the x-axis title.");
        Assert(svg.Contains("data-cfx-role=\"waterfall-y-axis-title\"", StringComparison.Ordinal), "Waterfall charts should mark the y-axis title.");
        Assert(svg.Contains(">Opened</text>", StringComparison.Ordinal), "Waterfall charts should render category labels.");
        Assert(svg.Contains(">Total</text>", StringComparison.Ordinal), "Waterfall charts should render a total category.");
        Assert(svg.Contains(">+18</text>", StringComparison.Ordinal), "Waterfall charts should render positive delta labels.");
        Assert(Chart.Create().AddWaterfall("Delta", Points(18, -42, -12, 9)).ToPng().Length > 64, "Waterfall charts should render PNG output.");
    }

    private static void RadarSeriesRenderPolarPolygons() {
        var svg = Chart.Create()
            .WithSize(760, 460)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy")
            .AddRadar("Current", Points(92, 74, 88, 96, 81), ChartColor.FromRgb(37, 99, 235))
            .AddRadar("Target", Points(96, 90, 92, 98, 90), ChartColor.FromRgb(52, 211, 153))
            .ToSvg();
        Assert(svg.Contains("data-cfx-role=\"radar-chart\"", StringComparison.Ordinal), "Radar charts should expose a role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"radar-area\"") == 2, "Two radar series should render two filled polygons.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Current: Mail auth 92%, DNSSEC 74%, TLS 88%, CT 96%, Policy 81%\"", StringComparison.Ordinal), "Radar series should expose accessible summaries.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"radar-spoke\"") == 5, "Five radar categories should render five spokes.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"radar-axis-label\"") == 5, "Radar charts should expose category label markers.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"radar-ring-label\"") > 0, "Radar charts should expose ring value label markers.");
        Assert(GetAttribute(svg, "data-cfx-role=\"radar-axis-label\"", "x") >= 24, "Radar axis labels should stay inside the SVG viewport.");
        Assert(svg.Contains(">Mail auth</text>", StringComparison.Ordinal), "Radar charts should render category labels.");
        Assert(svg.Contains(">92%</text>", StringComparison.Ordinal), "Radar charts should render optional data labels.");
        Assert(Chart.Create().AddRadar("Current", Points(92, 74, 88, 96, 81)).ToPng().Length > 64, "Radar charts should render PNG output.");
        Assert(Chart.Create().WithSize(300, 220).WithDataLabels().WithValueFormatter(_ => "very long radar label value").AddRadar("Current", Points(92, 74, 88)).ToPng().Length > 64, "Radar PNG data labels should fit bounded plot space.");
    }

    private static void PolarAreaSeriesRenderRadialSegments() {
        var chart = Chart.Create()
            .WithSize(760, 460)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT")
            .AddPolarArea("Control share", Points(92, 74, 88, 96));
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"polar-area-chart\"", StringComparison.Ordinal), "Polar-area charts should expose a role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"polar-area-segment\"") == 4, "Polar-area charts should render one radial segment per positive value.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"polar-area-ring\"") == 4, "Polar-area charts should render radial reference rings.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Mail auth: 92%, 26.3%\"", StringComparison.Ordinal), "Polar-area segments should expose accessible summaries.");
        Assert(svg.Contains(">92%</text>", StringComparison.Ordinal), "Polar-area data labels should render values when enabled.");
        Assert(Chart.Create().WithXLabels("A", "B", "C").AddPolarArea("Polar", Points(30, 50, 80)).ToPng().Length > 64, "Polar-area charts should render PNG output.");
        Assert(Chart.Create().WithSize(300, 220).WithDataLabels().WithValueFormatter(_ => "very long polar label value").AddPolarArea("Polar", Points(30, 50, 80)).ToPng().Length > 64, "Polar-area PNG data labels should fit bounded plot space.");
    }

    private static void CircleSeriesRenderSingleProgressRings() {
        var chart = Chart.Create()
            .WithSize(760, 460)
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .AddCircle("Readiness", 87, 0, 100, ChartColor.FromRgb(52, 211, 153));
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"circle-chart\"", StringComparison.Ordinal), "Circle charts should expose a role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"circle-track\"") == 1, "Circle charts should render one track ring.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"circle-value\"") == 1, "Circle charts should render one value ring.");
        Assert(svg.Contains("data-cfx-ratio=\"0.87\"", StringComparison.Ordinal), "Circle charts should expose normalized progress metadata.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Readiness: 87%, positive\"", StringComparison.Ordinal), "Circle charts should expose accessible summaries.");
        Assert(svg.Contains(">87%</text>", StringComparison.Ordinal), "Circle charts should render the central value label.");
        Assert(Chart.Create().AddCircle("Circle", 72).ToPng().Length > 64, "Circle charts should render PNG output.");
    }

    private static void FunnelSeriesRenderStagedSegments() {
        var svg = Chart.Create().WithSize(760, 460).WithXLabels("Discovered", "Verified", "Prioritized", "Remediated").AddFunnel("Domain remediation funnel", Points(420, 318, 174, 96)).ToSvg();
        Assert(svg.Contains("data-cfx-role=\"funnel-chart\"", StringComparison.Ordinal), "Funnel charts should expose a role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"funnel-segment\"") == 4, "Four funnel values should render four funnel segments.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"funnel-retention\"") == 3, "Funnel charts should render retention labels after the first stage.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"funnel-dropoff\"") == 3, "Funnel charts should render drop-off labels after the first stage.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"funnel-dropoff-line\"") == 3, "Funnel charts should render drop-off guide lines after the first stage.");
        Assert(svg.Contains("data-cfx-retention=\"0.757\"", StringComparison.Ordinal), "Funnel segments should expose retention metadata.");
        Assert(svg.Contains("data-cfx-dropoff=\"0.243\"", StringComparison.Ordinal), "Funnel segments should expose drop-off metadata.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Verified: 318, retained 75.7%, drop-off 24.3%\"", StringComparison.Ordinal), "Funnel segments should expose accessible summaries.");
        Assert(svg.Contains(">Discovered</text>", StringComparison.Ordinal), "Funnel charts should render stage labels.");
        Assert(svg.Contains(">420</text>", StringComparison.Ordinal), "Funnel charts should render stage values.");
        Assert(GetAttribute(svg, "data-cfx-role=\"funnel-retention\"", "x") < 760, "Funnel retention labels should stay inside the SVG viewport.");
        Assert(svg.Contains(">75.7% retained</text>", StringComparison.Ordinal), "Funnel charts should render retained percentage labels.");
        Assert(svg.Contains(">-24.3% from prev</text>", StringComparison.Ordinal), "Funnel charts should render previous-stage drop-off labels.");
        Assert(Chart.Create().AddFunnel("Funnel", Points(420, 318, 174, 96)).ToPng().Length > 64, "Funnel charts should render PNG output.");
    }

    private static void TreemapItemsRenderProportionalTiles() {
        var chart = Chart.Create()
            .WithSize(720, 420)
            .WithDataLabels()
            .AddTreemap("Findings", new[] {
                new ChartTreemapItem("Critical", 50),
                new ChartTreemapItem("High", 28),
                new ChartTreemapItem("Medium", 14),
                new ChartTreemapItem("Low", 8)
            });
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"treemap\"", StringComparison.Ordinal), "Treemaps should expose a chart role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"treemap-tile\"") == 4, "Treemaps should render one tile per positive item.");
        Assert(svg.Contains("data-cfx-role=\"treemap-tile-highlight\"", StringComparison.Ordinal), "Treemap SVG tiles should render polished highlights.");
        Assert(svg.Contains("data-cfx-role=\"treemap-label\"", StringComparison.Ordinal), "Treemaps should render tile labels when space allows.");
        Assert(svg.Contains(">Critical</text>", StringComparison.Ordinal), "Treemaps should render item labels.");
        Assert(svg.Contains(">50</text>", StringComparison.Ordinal), "Treemaps should render item values when space allows.");
        var criticalArea = GetAttribute(svg, "data-cfx-role=\"treemap-tile\" data-cfx-point=\"0\"", "width") * GetAttribute(svg, "data-cfx-role=\"treemap-tile\" data-cfx-point=\"0\"", "height");
        var lowArea = GetAttribute(svg, "data-cfx-role=\"treemap-tile\" data-cfx-point=\"3\"", "width") * GetAttribute(svg, "data-cfx-role=\"treemap-tile\" data-cfx-point=\"3\"", "height");
        Assert(criticalArea > lowArea, "Larger treemap values should receive larger tile areas.");
        Assert(chart.ToPng().Length > 64, "Treemap charts should render PNG output.");
    }

    private static void TimelineItemsRenderDateRanges() {
        var svg = Chart.Create()
            .WithSize(760, 420)
            .WithDataLabels()
            .WithXAxis("Schedule")
            .WithYAxis("Workstream")
            .AddTimelineItem("Certificate renewal", new DateTime(2026, 1, 1), new DateTime(2026, 2, 1), ChartColor.FromRgb(37, 99, 235))
            .AddTimelineItem("DMARC rollout", new DateTime(2026, 1, 15), new DateTime(2026, 3, 1), ChartColor.FromRgb(14, 165, 233))
            .ToSvg();
        Assert(svg.Contains("data-cfx-role=\"timeline\"", StringComparison.Ordinal), "Timelines should expose a role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"timeline-item\"") == 2, "Two timeline items should render two ranges.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"timeline-row-label\"") == 2, "Timelines should mark row labels for layout regression checks.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Certificate renewal: Jan 1 to Feb 1, duration 31d\"", StringComparison.Ordinal), "Timeline items should expose accessible summaries.");
        Assert(svg.Contains("data-cfx-duration=\"31d\"", StringComparison.Ordinal), "Timeline items should expose duration metadata.");
        Assert(svg.Contains("data-cfx-role=\"timeline-x-axis-title\"", StringComparison.Ordinal), "Timelines should mark the x-axis title.");
        Assert(svg.Contains("data-cfx-role=\"timeline-y-axis-title\"", StringComparison.Ordinal), "Timelines should mark the y-axis title.");
        Assert(GetAttribute(svg, "data-cfx-role=\"timeline-row-label\"", "x") > 100, "Timeline row labels should reserve enough left-side space.");
        Assert(GetAttribute(svg, "data-cfx-role=\"timeline-tick-label\"", "x") >= GetAttribute(svg, "data-cfx-role=\"timeline-row-label\"", "x") + 14, "Timeline tick labels should stay inside the plotted timeline area.");
        Assert(svg.Contains(">Certificate renewal</text>", StringComparison.Ordinal), "Timelines should render row labels.");
        Assert(svg.Contains(">31d</text>", StringComparison.Ordinal), "Timelines should render optional duration labels.");

        var numericTimeline = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .WithXAxisValueFormatter(value => "W" + value.ToString("0", CultureInfo.InvariantCulture))
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "w")
            .AddTimelineRange("Numeric rollout", 1, 4)
            .ToSvg();
        var numericGantt = Chart.Create()
            .WithSize(640, 360)
            .WithXAxisValueFormatter(value => "W" + value.ToString("0", CultureInfo.InvariantCulture))
            .AddGanttTask("Numeric task", 1, 4, 0.5)
            .ToSvg();
        Assert(numericTimeline.Contains("Numeric rollout: W1 to W4, duration 3w", StringComparison.Ordinal), "Numeric timelines should use custom x-axis and duration formatters in summaries.");
        Assert(numericGantt.Contains("Numeric task: W1 to W4, 50% complete", StringComparison.Ordinal), "Numeric Gantt tasks should use custom x-axis formatters in summaries.");
    }

    private static void MultipleBarSeriesCanRenderAsStackedBars() {
        var svg = Chart.Create().WithSize(640, 360).WithStackedBars().WithXLabels("Mon", "Tue", "Wed").AddBar("Passed", Points(40, 55, 65)).AddBar("Warnings", Points(15, 25, 20)).AddBar("Failed", Points(5, 8, 10)).ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar\"") == 9, "Three stacked bar series with three points each should render nine bar segments.");
        Assert(Math.Abs(GetAttribute(svg, "data-cfx-series=\"0\" data-cfx-point=\"0\"", "x") - GetAttribute(svg, "data-cfx-series=\"1\" data-cfx-point=\"0\"", "x")) < 0.001, "Stacked bars for the same category should share the same x position.");
        Assert(GetAttribute(svg, "data-cfx-series=\"1\" data-cfx-point=\"0\"", "y") < GetAttribute(svg, "data-cfx-series=\"0\" data-cfx-point=\"0\"", "y"), "Second stacked segment should render above the first positive segment.");
    }

    private static void StackedBarsCanRenderTotalLabels() {
        var svg = Chart.Create().WithSize(640, 360).WithStackedBars().WithStackTotals().WithXLabels("Mon", "Tue", "Wed").AddBar("Passed", Points(40, 55, 65)).AddBar("Warnings", Points(15, 25, 20)).AddBar("Failed", Points(5, 8, 10)).ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"stack-total-label\"") == 3, "Stacked bars should render one total label per category when enabled.");
        Assert(svg.Contains(">60</text>", StringComparison.Ordinal), "Stacked bar total labels should render summed category values.");
        Assert(!svg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Stack total labels should not implicitly enable segment data labels.");
    }

    private static void DonutChartRendersSlices() {
        var chart = Chart.Create().WithTitle("Result mix").WithSize(640, 360).WithXLabels("Passed", "Warning", "Failed").AddDonut("Checks", Points(70, 20, 10));
        var svg = chart.ToSvg();
        Assert(svg.Contains(" A ", StringComparison.Ordinal), "Donut chart should render SVG arc paths.");
        Assert(svg.Contains(">Passed</text>", StringComparison.Ordinal), "Donut chart should render slice labels.");
        Assert(svg.Contains(">Checks</text>", StringComparison.Ordinal), "Donut chart should render center series label.");
        Assert(chart.ToPng().Length > 64, "Donut chart should render PNG output.");
    }

    private static void SingleSliceDonutRendersFullRing() {
        var svg = Chart.Create().WithSize(360, 240).WithXLabels("All").AddDonut("Total", Points(100)).ToSvg();
        Assert(svg.Contains(" A ", StringComparison.Ordinal), "Single-slice donut should render arc commands.");
        Assert(!svg.Contains("NaN", StringComparison.Ordinal), "Single-slice donut should not contain invalid numeric values.");
    }
}
