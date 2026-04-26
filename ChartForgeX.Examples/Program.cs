using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var output = Path.Combine(AppContext.BaseDirectory, "output");
Directory.CreateDirectory(output);

var dnssec = Chart.Create()
    .WithTitle("Domain Security Checks")
    .WithSubtitle("Dependency-free SVG, HTML and PNG chart rendering")
    .WithXAxis("Run")
    .WithYAxis("Checks")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(1180, 640)
    .WithTransparentBackground(true)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Next")
    .AddSmoothArea("Passed", Points(820, 940, 980, 1040, 1120, 1180, 1230, 1260))
    .AddSmoothLine("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36))
    .AddSmoothLine("Failed", Points(22, 30, 28, 21, 18, 15, 13, 10), ChartColor.FromRgb(248, 113, 113))
    .AddHorizontalLine(100, "warning target", ChartColor.FromRgb(251, 191, 36))
    .AddVerticalBand(6, 7, "weekend", ChartColor.FromRgb(96, 165, 250), 0.10);

dnssec.SaveSvg(Path.Combine(output, "domain-security-dark.svg"));
dnssec.SaveHtml(Path.Combine(output, "domain-security-dark.html"));
dnssec.SavePng(Path.Combine(output, "domain-security-dark.png"));

var bars = Chart.Create()
    .WithTitle("Certificate Transparency Volume")
    .WithSubtitle("Static report chart with no JavaScript runtime")
    .WithXAxis("Day")
    .WithYAxis("Certificates")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(1180, 640)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
    .AddBar("Certificates", Points(4200, 5300, 6100, 5900, 7200, 8100, 7900))
    .AddHorizontalBand(7000, 8500, "high volume", ChartColor.FromRgb(16, 185, 129), 0.12);

bars.AddSmoothLine("7-day baseline", Points(3900, 4550, 5200, 5800, 6500, 7100, 7600), ChartColor.FromRgb(14, 165, 233));

bars.SaveSvg(Path.Combine(output, "ct-volume-light.svg"));
bars.SaveHtml(Path.Combine(output, "ct-volume-light.html"));
bars.SavePng(Path.Combine(output, "ct-volume-light.png"));

var grouped = Chart.Create()
    .WithTitle("Security Findings by Severity")
    .WithSubtitle("Grouped bar comparison across two report runs")
    .WithXAxis("Severity")
    .WithYAxis("Findings")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(920, 560)
    .WithXLabels("Critical", "High", "Medium", "Low", "Informational")
    .AddBar("Current run", Points(8, 32, 84, 126, 210), ChartColor.FromRgb(37, 99, 235))
    .AddBar("Previous run", Points(12, 41, 97, 118, 188), ChartColor.FromRgb(14, 165, 233))
    .AddHorizontalLine(40, "review threshold", ChartColor.FromRgb(245, 158, 11));

grouped.SaveSvg(Path.Combine(output, "security-findings-grouped-light.svg"));
grouped.SaveHtml(Path.Combine(output, "security-findings-grouped-light.html"));
grouped.SavePng(Path.Combine(output, "security-findings-grouped-light.png"));

var horizontal = Chart.Create()
    .WithTitle("Domain Control Coverage")
    .WithSubtitle("Horizontal bars keep long category labels readable")
    .WithXAxis("Coverage")
    .WithYAxis("Control")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(920, 560)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF alignment", "DMARC policy enforcement", "DNSSEC coverage", "Certificate transparency monitoring", "MTA-STS deployment")
    .AddHorizontalBar("Coverage", Points(96, 88, 74, 92, 63), ChartColor.FromRgb(37, 99, 235));

horizontal.SaveSvg(Path.Combine(output, "domain-control-horizontal-light.svg"));
horizontal.SaveHtml(Path.Combine(output, "domain-control-horizontal-light.html"));
horizontal.SavePng(Path.Combine(output, "domain-control-horizontal-light.png"));

var heatmap = Chart.Create()
    .WithTitle("Control Coverage Matrix")
    .WithSubtitle("Heatmap rows for comparing domain groups across security controls")
    .WithXAxis("Control")
    .WithYAxis("Domain group")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(980, 560)
    .WithDataLabels()
    .WithHeatmapScale(ChartHeatmapScale.Semantic)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS", "TLS-RPT", "CT")
    .AddHeatmapRow("Primary domains", Points(96, 88, 74, 63, 58, 92))
    .AddHeatmapRow("Parked domains", Points(74, 62, 51, 42, 38, 66))
    .AddHeatmapRow("Regional domains", Points(82, 77, 68, 54, 49, 80))
    .AddHeatmapRow("Acquired domains", Points(58, 43, 36, 28, 25, 52));

heatmap.SaveSvg(Path.Combine(output, "control-coverage-heatmap-dark.svg"));
heatmap.SaveHtml(Path.Combine(output, "control-coverage-heatmap-dark.html"));
heatmap.SavePng(Path.Combine(output, "control-coverage-heatmap-dark.png"));

var gauge = Chart.Create()
    .WithTitle("Security Posture Score")
    .WithSubtitle("Single-value gauge for executive report summaries")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddGauge("Overall domain readiness", 87, 0, 100, ChartColor.FromRgb(52, 211, 153));

gauge.SaveSvg(Path.Combine(output, "security-posture-gauge-dark.svg"));
gauge.SaveHtml(Path.Combine(output, "security-posture-gauge-dark.html"));
gauge.SavePng(Path.Combine(output, "security-posture-gauge-dark.png"));

var bullet = Chart.Create()
    .WithTitle("Control Targets")
    .WithSubtitle("Bullet rows compare current posture against target thresholds")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(920, 520)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddBullet("DMARC enforcement", 88, 95, 0, 100, new[] { 60d, 80d }, ChartColor.FromRgb(52, 211, 153))
    .AddBullet("DNSSEC coverage", 74, 90, 0, 100, new[] { 55d, 78d }, ChartColor.FromRgb(96, 165, 250))
    .AddBullet("MTA-STS deployment", 63, 85, 0, 100, new[] { 50d, 75d }, ChartColor.FromRgb(251, 191, 36))
    .AddBullet("TLS reporting", 58, 80, 0, 100, new[] { 45d, 70d }, ChartColor.FromRgb(34, 211, 238));

bullet.SaveSvg(Path.Combine(output, "control-targets-bullet-dark.svg"));
bullet.SaveHtml(Path.Combine(output, "control-targets-bullet-dark.html"));
bullet.SavePng(Path.Combine(output, "control-targets-bullet-dark.png"));

var waterfall = Chart.Create()
    .WithTitle("Remediation Impact")
    .WithSubtitle("Waterfall deltas show how findings changed during a cleanup cycle")
    .WithXAxis("Change")
    .WithYAxis("Open findings")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(920, 540)
    .WithDataLabels()
    .WithXLabels("Opened", "Resolved", "Suppressed", "Accepted", "Regressed")
    .AddWaterfall("Finding delta", Points(24, -68, -18, -9, 11), ChartColor.FromRgb(52, 211, 153));

waterfall.SaveSvg(Path.Combine(output, "remediation-impact-waterfall-dark.svg"));
waterfall.SaveHtml(Path.Combine(output, "remediation-impact-waterfall-dark.html"));
waterfall.SavePng(Path.Combine(output, "remediation-impact-waterfall-dark.png"));

var radar = Chart.Create()
    .WithTitle("Security Posture Radar")
    .WithSubtitle("Radial comparison across major domain control areas")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(920, 560)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy", "Monitoring")
    .AddRadar("Current posture", Points(92, 74, 88, 96, 81, 84), ChartColor.FromRgb(96, 165, 250))
    .AddRadar("Target posture", Points(96, 90, 94, 98, 92, 90), ChartColor.FromRgb(52, 211, 153));

radar.SaveSvg(Path.Combine(output, "security-posture-radar-dark.svg"));
radar.SaveHtml(Path.Combine(output, "security-posture-radar-dark.html"));
radar.SavePng(Path.Combine(output, "security-posture-radar-dark.png"));

var funnel = Chart.Create()
    .WithTitle("Domain Remediation Funnel")
    .WithSubtitle("Stage retention from discovery to monitored remediation")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(920, 560)
    .WithXLabels("Discovered", "Verified", "Prioritized", "Remediated", "Monitored")
    .AddFunnel("Domains", Points(420, 318, 174, 96, 72));

funnel.SaveSvg(Path.Combine(output, "domain-remediation-funnel-dark.svg"));
funnel.SaveHtml(Path.Combine(output, "domain-remediation-funnel-dark.html"));
funnel.SavePng(Path.Combine(output, "domain-remediation-funnel-dark.png"));

var timeline = Chart.Create()
    .WithTitle("Domain Remediation Timeline")
    .WithSubtitle("Date-range items for certificate and policy rollout planning")
    .WithXAxis("Schedule")
    .WithYAxis("Workstream")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(980, 560)
    .WithDataLabels()
    .AddTimelineItem("Certificate renewal", new DateTime(2026, 1, 4), new DateTime(2026, 2, 10), ChartColor.FromRgb(37, 99, 235))
    .AddTimelineItem("DMARC enforcement", new DateTime(2026, 1, 18), new DateTime(2026, 3, 5), ChartColor.FromRgb(14, 165, 233))
    .AddTimelineItem("DNSSEC rollout", new DateTime(2026, 2, 1), new DateTime(2026, 3, 22), ChartColor.FromRgb(16, 185, 129))
    .AddTimelineItem("MTA-STS monitoring", new DateTime(2026, 2, 14), new DateTime(2026, 4, 2), ChartColor.FromRgb(245, 158, 11));

timeline.SaveSvg(Path.Combine(output, "domain-remediation-timeline-light.svg"));
timeline.SaveHtml(Path.Combine(output, "domain-remediation-timeline-light.html"));
timeline.SavePng(Path.Combine(output, "domain-remediation-timeline-light.png"));

var stacked = Chart.Create()
    .WithTitle("Domain Findings Composition")
    .WithSubtitle("Stacked bar mode for report totals")
    .WithXAxis("Run")
    .WithYAxis("Findings")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(920, 560)
    .WithStackedBars()
    .WithStackTotals()
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
    .AddBar("Passed", Points(180, 220, 245, 260, 280), ChartColor.FromRgb(52, 211, 153))
    .AddBar("Warnings", Points(42, 38, 32, 28, 24), ChartColor.FromRgb(251, 191, 36))
    .AddBar("Failed", Points(12, 10, 8, 6, 5), ChartColor.FromRgb(248, 113, 113))
    .AddHorizontalLine(300, "capacity", ChartColor.FromRgb(96, 165, 250));

stacked.SaveSvg(Path.Combine(output, "domain-findings-stacked-dark.svg"));
stacked.SaveHtml(Path.Combine(output, "domain-findings-stacked-dark.html"));
stacked.SavePng(Path.Combine(output, "domain-findings-stacked-dark.png"));

var sparkline = Chart.Create()
    .WithTitle("Warnings Sparkline")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(360, 90)
    .WithSparkline()
    .AddSmoothArea("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36));

sparkline.SaveSvg(Path.Combine(output, "warnings-sparkline.svg"));
sparkline.SaveHtml(Path.Combine(output, "warnings-sparkline.html"));
sparkline.SavePng(Path.Combine(output, "warnings-sparkline.png"));

var donut = Chart.Create()
    .WithTitle("Domain Check Result Mix")
    .WithSubtitle("Static donut chart with zero JavaScript")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(820, 460)
    .WithXLabels("Passed", "Warnings", "Failed")
    .AddDonut("Checks", Points(1260, 68, 10));

donut.SaveSvg(Path.Combine(output, "result-mix-donut.svg"));
donut.SaveHtml(Path.Combine(output, "result-mix-donut.html"));
donut.SavePng(Path.Combine(output, "result-mix-donut.png"));

var monthly = Chart.Create()
    .WithTitle("Monthly Security Posture")
    .WithSubtitle("Automatic label density with wrapped legend rows")
    .WithXAxis("Month")
    .WithYAxis("Score")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 540)
    .WithXAxisLabelDensity(ChartLabelDensity.Auto)
    .WithXLabels("January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December")
    .AddSmoothLine("Primary domain checks", Points(82, 84, 86, 87, 88, 89, 91, 92, 93, 94, 95, 96), ChartColor.FromRgb(96, 165, 250))
    .AddSmoothLine("Certificate transparency drift", Points(78, 79, 81, 83, 85, 85, 86, 88, 89, 90, 91, 93), ChartColor.FromRgb(34, 211, 238))
    .AddSmoothLine("Mail authentication alignment", Points(72, 73, 75, 76, 78, 80, 82, 83, 84, 86, 87, 89), ChartColor.FromRgb(52, 211, 153))
    .AddSmoothLine("Dnssec policy posture", Points(69, 71, 73, 74, 76, 78, 79, 81, 83, 84, 86, 88), ChartColor.FromRgb(167, 139, 250))
    .AddHorizontalLine(90, "target", ChartColor.FromRgb(251, 191, 36));

monthly.SaveSvg(Path.Combine(output, "monthly-posture-dark.svg"));
monthly.SaveHtml(Path.Combine(output, "monthly-posture-dark.html"));
monthly.SavePng(Path.Combine(output, "monthly-posture-dark.png"));

var annotationEdge = Chart.Create()
    .WithTitle("Annotation Edge Handling")
    .WithSubtitle("Line labels clamp to the plot instead of overflowing")
    .WithXAxis("Run")
    .WithYAxis("Checks")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
    .AddSmoothLine("Checks", Points(42, 68, 72, 95, 104, 118), ChartColor.FromRgb(96, 165, 250))
    .AddVerticalLine(6, "right edge marker", ChartColor.FromRgb(251, 191, 36))
    .AddHorizontalLine(100, "target", ChartColor.FromRgb(52, 211, 153));

annotationEdge.SaveSvg(Path.Combine(output, "annotation-edge-dark.svg"));
annotationEdge.SaveHtml(Path.Combine(output, "annotation-edge-dark.html"));
annotationEdge.SavePng(Path.Combine(output, "annotation-edge-dark.png"));

var labels = Chart.Create()
    .WithTitle("Data Label Readability")
    .WithSubtitle("Edge-aware labels with SVG text halos")
    .WithXAxis("Day")
    .WithYAxis("Signals")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
    .AddSmoothLine("Detected", Points(980, 760, 880, 920, 1020, 990), ChartColor.FromRgb(96, 165, 250))
    .AddBar("Escalated", Points(240, 320, 280, 410, 390, 450), ChartColor.FromRgb(251, 191, 36));

labels.SaveSvg(Path.Combine(output, "data-label-readability-dark.svg"));
labels.SaveHtml(Path.Combine(output, "data-label-readability-dark.html"));
labels.SavePng(Path.Combine(output, "data-label-readability-dark.png"));

var latency = Chart.Create()
    .WithTitle("Endpoint Latency")
    .WithSubtitle("Custom value formatting for report units")
    .WithXAxis("Probe")
    .WithYAxis("Response time")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
    .WithXLabels("DNS", "TCP", "TLS", "HTTP", "Render")
    .AddSmoothLine("P95", Points(28, 64, 118, 146, 182), ChartColor.FromRgb(37, 99, 235))
    .AddHorizontalLine(150, "budget", ChartColor.FromRgb(245, 158, 11));

latency.SaveSvg(Path.Combine(output, "endpoint-latency-light.svg"));
latency.SaveHtml(Path.Combine(output, "endpoint-latency-light.html"));
latency.SavePng(Path.Combine(output, "endpoint-latency-light.png"));

var cost = Chart.Create()
    .WithTitle("License Cost Trend")
    .WithSubtitle("Long formatted y-axis labels keep their own space")
    .WithXAxis("Quarter")
    .WithYAxis("Spend")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithValueFormatter(value => "$" + value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture))
    .WithXLabels("Q1", "Q2", "Q3", "Q4")
    .AddSmoothArea("Projected", Points(860000, 940000, 1050000, 1160000), ChartColor.FromRgb(37, 99, 235))
    .AddSmoothLine("Actual", Points(820000, 970000, 1010000, 1210000), ChartColor.FromRgb(14, 165, 233));

cost.SaveSvg(Path.Combine(output, "license-cost-light.svg"));
cost.SaveHtml(Path.Combine(output, "license-cost-light.html"));
cost.SavePng(Path.Combine(output, "license-cost-light.png"));

var regional = Chart.Create()
    .WithTitle("Certificate Transparency by Region")
    .WithSubtitle("Rotated category labels and compact million-scale values")
    .WithXAxis("Region")
    .WithYAxis("Certificates")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(880, 560)
    .WithXAxisLabelDensity(ChartLabelDensity.All)
    .WithXAxisLabelAngle(-35)
    .WithXLabels("North America", "Western Europe", "Central Europe", "Asia Pacific", "Latin America", "Middle East")
    .AddBar("Logged certificates", Points(1200000, 2350000, 1840000, 3120000, 980000, 760000))
    .AddHorizontalLine(2000000, "2M benchmark", ChartColor.FromRgb(245, 158, 11));

regional.SaveSvg(Path.Combine(output, "ct-regional-light.svg"));
regional.SaveHtml(Path.Combine(output, "ct-regional-light.html"));
regional.SavePng(Path.Combine(output, "ct-regional-light.png"));

GalleryWriter.Write(output);
Console.WriteLine("Generated files in: " + output);

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
}
