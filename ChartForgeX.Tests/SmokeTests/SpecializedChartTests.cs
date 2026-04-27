using System;
using System.Globalization;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
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
            ("timeline", Chart.Create().AddTimelineItem(unsafeLabel, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1)).ToSvg()),
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
