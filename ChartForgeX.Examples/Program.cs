using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var output = Path.Combine(AppContext.BaseDirectory, "output");
if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
Directory.CreateDirectory(output);
const ChartPngOutputScale DemoPngOutputScale = ChartPngOutputScale.Retina;
if (ExampleProgramOptions.TryHandle(args, output, DemoPngOutputScale)) return;
void SaveChart(Chart chart, string name) {
    chart.WithPngOutputScale(DemoPngOutputScale);
    chart.SaveSvg(Path.Combine(output, name + ".svg"));
    chart.SaveHtml(Path.Combine(output, name + ".html"));
    chart.SavePng(Path.Combine(output, name + ".png"));
}
void SaveGrid(ChartGrid grid, string name) {
    grid.WithPngOutputScale(DemoPngOutputScale);
    grid.SaveSvg(Path.Combine(output, name + ".svg"));
    grid.SavePng(Path.Combine(output, name + ".png"));
    grid.SaveHtml(Path.Combine(output, name + ".html"));
}

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

SaveChart(dnssec, "domain-security-dark");
dnssec.SaveInteractiveHtml(Path.Combine(output, "domain-security-interactive.html"), options => {
    options.PageTitle = "Domain Security Interactive";
    options.IdScope = "domain-security-interactive";
    options.Interaction.ChartId = "domain-security-interactive";
    options.Interaction.GroupName = "example-gallery";
    options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.FocusTrail | ChartInteractionFeatures.RevealLabels | ChartInteractionFeatures.StateBookmarks);
    ExampleInteractiveScenarios.ConfigureDomainSecurity(options.Interaction);
});

var bars = Chart.Create()
    .WithTitle("Certificate Transparency Volume")
    .WithSubtitle("Bar-line combo with a secondary y-axis and no JavaScript runtime")
    .WithXAxis("Day")
    .WithYAxis("Certificates")
    .WithSecondaryYAxis("Pass rate", value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithSecondaryYAxisBounds(0, 100)
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(1180, 640)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
    .AddBarLineCombo(
        "Certificates",
        Points(4200, 5300, 6100, 5900, 7200, 8100, 7900),
        "Pass rate",
        Points(87, 89, 91, 90, 93, 95, 94),
        ChartColor.FromRgb(37, 99, 235),
        ChartColor.FromRgb(14, 165, 233),
        smoothLine: true,
        lineAxis: ChartAxisSide.Secondary)
    .AddHorizontalBand(7000, 8500, "high volume", ChartColor.FromRgb(16, 185, 129), 0.12);

SaveChart(bars, "ct-volume-light");

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

SaveChart(grouped, "security-findings-grouped-light");
new[] { dnssec, bars, grouped }.SaveInteractiveHtmlDashboard(Path.Combine(output, "executive-interactive-dashboard.html"), options => {
    options.PageTitle = "Executive Interactive Dashboard";
    options.IdScope = "executive-interactive-dashboard";
    options.Columns = 2;
    options.Interaction.GroupName = "example-gallery";
    options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.FocusTrail | ChartInteractionFeatures.RevealLabels | ChartInteractionFeatures.StateBookmarks);
});

var horizontal = Chart.Create()
    .WithTitle("Domain Control Composition")
    .WithSubtitle("Stacked horizontal bars keep long category labels readable")
    .WithXAxis("Coverage")
    .WithYAxis("Control")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(920, 560)
    .WithStackedHorizontalBars()
    .WithStackTotals()
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF alignment", "DMARC policy enforcement", "DNSSEC coverage", "Certificate transparency monitoring", "MTA-STS deployment")
    .AddHorizontalBar("Complete", Points(72, 64, 52, 78, 44), ChartColor.FromRgb(16, 185, 129))
    .AddHorizontalBar("Partial", Points(24, 24, 22, 14, 19), ChartColor.FromRgb(245, 158, 11))
    .AddHorizontalBar("Missing", Points(4, 12, 26, 8, 37), ChartColor.FromRgb(239, 68, 68));

SaveChart(horizontal, "domain-control-horizontal-light");

var heatmap = Chart.Create()
    .WithTitle("Control Coverage Matrix")
    .WithSubtitle("Heatmap rows for comparing domain groups across security controls")
    .WithXAxis("Control")
    .WithYAxis("Domain group")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(980, 560)
    .WithLegend(false)
    .WithDataLabels()
    .WithHeatmapScale(ChartHeatmapScale.Semantic)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS", "TLS-RPT", "CT")
    .AddHeatmapRow("Primary domains", Points(96, 88, 74, 63, 58, 92))
    .AddHeatmapRow("Parked domains", Points(74, 62, 51, 42, 38, 66))
    .AddHeatmapRow("Regional domains", Points(82, 77, 68, 54, 49, 80))
    .AddHeatmapRow("Acquired domains", Points(58, 43, 36, 28, 25, 52));

SaveChart(heatmap, "control-coverage-heatmap-dark");
MapExamples.Write(output, DemoPngOutputScale, ExampleProgramOptions.HasArg(args, "--include-external-map-examples"));
if (!ExampleProgramOptions.HasArg(args, "--skip-topology")) TopologyExamples.Write(output);

var gauge = Chart.Create()
    .WithTitle("Security Posture Score")
    .WithSubtitle("Single-value gauge for executive report summaries")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddGauge("Overall domain readiness", 87, 0, 100, ChartColor.FromRgb(52, 211, 153));

SaveChart(gauge, "security-posture-gauge-dark");

var policyReadinessCircle = Chart.Create()
    .WithTitle("Policy Readiness Circle")
    .WithSubtitle("Circle charts keep single-value progress KPIs compact")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddCircle("Ready", 87, 0, 100, ChartColor.FromRgb(16, 185, 129));

SaveChart(policyReadinessCircle, "policy-readiness-circle-light");

var radialBar = Chart.Create()
    .WithTitle("Control Coverage Rings")
    .WithSubtitle("Radial bars compare core security control completion")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("Mail auth", "DNSSEC", "Transport TLS", "Certificate CT")
    .AddRadialBar("Average coverage", Points(92, 74, 88, 96));

SaveChart(radialBar, "control-coverage-radialbar-dark");

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

SaveChart(bullet, "control-targets-bullet-dark");

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

SaveChart(waterfall, "remediation-impact-waterfall-dark");

var radar = Chart.Create()
    .WithTitle("Security Posture Radar")
    .WithSubtitle("Radial comparison across major domain control areas")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(920, 560)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy", "Monitoring")
    .AddRadar("Current posture", Points(92, 74, 88, 96, 81, 84), ChartColor.FromRgb(96, 165, 250))
    .AddRadar("Target posture", Points(96, 90, 94, 98, 92, 90), ChartColor.FromRgb(52, 211, 153));

SaveChart(radar, "security-posture-radar-dark");

var polarArea = Chart.Create()
    .WithTitle("Control Contribution")
    .WithSubtitle("Polar area segments compare major control contributions")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy", "Monitoring")
    .AddPolarArea("Control share", Points(92, 74, 88, 96, 81, 84));

SaveChart(polarArea, "control-contribution-polar-area-light");

var controlScorecardGrid = ChartGrid.Create()
    .WithTitle("Control Scorecard Small Multiples")
    .WithSubtitle("Static report grid composed from existing dependency-free chart renderers")
    .WithColumns(2)
    .Add(gauge)
    .Add(bullet)
    .Add(radar)
    .Add(polarArea);

SaveGrid(controlScorecardGrid, "control-scorecards-grid");

var sharedCoverageNorth = Chart.Create()
    .WithTitle("Primary Domains")
    .WithSubtitle("Shared y-axis small multiple")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(520, 320)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS")
    .AddBar("Coverage", Points(96, 88, 74, 63), ChartColor.FromRgb(37, 99, 235));

var sharedCoverageAcquired = Chart.Create()
    .WithTitle("Acquired Domains")
    .WithSubtitle("Shared y-axis small multiple")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(520, 320)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS")
    .AddBar("Coverage", Points(72, 54, 46, 31), ChartColor.FromRgb(14, 165, 233));

var sharedAxisGrid = ChartGrid.Create()
    .WithTitle("Shared Axis Coverage Comparison")
    .WithSubtitle("Two panels keep shared x and y ranges for honest visual comparison")
    .WithTheme(ChartTheme.ReportLight())
    .WithColumns(2)
    .WithPadding(32)
    .WithPanelSize(500, 300)
    .Add(sharedCoverageNorth)
    .Add(sharedCoverageAcquired)
    .WithSharedAxes();

SaveGrid(sharedAxisGrid, "shared-axis-coverage-grid");

ExpressiveExamples.Write(output, DemoPngOutputScale); WellnessDashboardExamples.Write(output, DemoPngOutputScale); DashboardPortfolioExamples.Write(output, DemoPngOutputScale); DashboardPatternExamples.Write(output, DemoPngOutputScale);

var funnel = Chart.Create()
    .WithTitle("Domain Remediation Funnel")
    .WithSubtitle("Stage retention from discovery to monitored remediation")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(920, 560)
    .WithXLabels("Discovered", "Verified", "Prioritized", "Remediated", "Monitored")
    .AddFunnel("Domains", Points(420, 318, 174, 96, 72));

SaveChart(funnel, "domain-remediation-funnel-dark");

var findingsTreemap = Chart.Create()
    .WithTitle("Findings Composition")
    .WithSubtitle("Treemap tiles show proportional contribution by category")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(920, 560)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddTreemap("Finding share", new[] {
        new ChartTreemapItem("Authentication", 34),
        new ChartTreemapItem("Certificate lifecycle", 24),
        new ChartTreemapItem("DNS hygiene", 18),
        new ChartTreemapItem("Policy drift", 14),
        new ChartTreemapItem("Monitoring gaps", 10)
    });

SaveChart(findingsTreemap, "findings-composition-treemap-light");

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

SaveChart(timeline, "domain-remediation-timeline-light");

var gantt = Chart.Create()
    .WithTitle("Domain Remediation Gantt")
    .WithSubtitle("Progress, task dependencies, milestones, and current schedule marker")
    .WithXAxis("Schedule")
    .WithYAxis("Workstream")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(1040, 600)
    .WithDataLabels()
    .WithGanttToday(new DateTime(2026, 2, 18))
    .AddGanttTask("Inventory scope", new DateTime(2026, 1, 5), new DateTime(2026, 1, 24), 0.90, color: ChartColor.FromRgb(37, 99, 235))
    .AddGanttTask("Owner remediation", new DateTime(2026, 1, 20), new DateTime(2026, 2, 24), 0.62, dependsOn: 0, color: ChartColor.FromRgb(16, 185, 129))
    .AddGanttTask("Control retesting", new DateTime(2026, 2, 18), new DateTime(2026, 3, 12), 0.28, dependsOn: 1, color: ChartColor.FromRgb(14, 165, 233))
    .AddGanttTask("Monitoring handoff", new DateTime(2026, 3, 8), new DateTime(2026, 3, 26), 0.12, dependsOn: 2, color: ChartColor.FromRgb(168, 85, 247))
    .AddGanttMilestone("Executive sign-off", new DateTime(2026, 3, 30), dependsOn: 3, color: ChartColor.FromRgb(245, 158, 11));

SaveChart(gantt, "domain-remediation-gantt-light");

var findingFlow = Chart.Create()
    .WithTitle("Finding Flow Sankey")
    .WithSubtitle("Weighted paths from discovery through remediation outcomes")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(1040, 600)
    .WithDataLabels()
    .AddSankey("Findings", new[] {
        new ChartSankeyLink("Discovered", "Validated", 72),
        new ChartSankeyLink("Discovered", "Accepted risk", 18),
        new ChartSankeyLink("Validated", "Owner remediation", 48),
        new ChartSankeyLink("Validated", "Monitoring", 24),
        new ChartSankeyLink("Owner remediation", "Closed", 34),
        new ChartSankeyLink("Owner remediation", "Retesting", 14),
        new ChartSankeyLink("Retesting", "Closed", 10),
        new ChartSankeyLink("Retesting", "Monitoring", 4)
    });

SaveChart(findingFlow, "finding-flow-sankey-light");

var controlHierarchy = Chart.Create()
    .WithTitle("Control Hierarchy Tree")
    .WithSubtitle("Static hierarchy map for ownership and remediation structure")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(1040, 600)
    .AddTree("Control hierarchy", new[] {
        new ChartTreeLink("Security posture", "Mail authentication", 3),
        new ChartTreeLink("Security posture", "Certificate lifecycle", 2),
        new ChartTreeLink("Security posture", "DNS hygiene", 2),
        new ChartTreeLink("Mail authentication", "SPF alignment"),
        new ChartTreeLink("Mail authentication", "DKIM rotation"),
        new ChartTreeLink("Certificate lifecycle", "Expiry monitoring"),
        new ChartTreeLink("Certificate lifecycle", "SAN inventory"),
        new ChartTreeLink("DNS hygiene", "DNSSEC rollout"),
        new ChartTreeLink("DNS hygiene", "Stale record cleanup")
    });

SaveChart(controlHierarchy, "control-hierarchy-tree-light");

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

SaveChart(stacked, "domain-findings-stacked-dark");

var signalMix = Chart.Create()
    .WithTitle("Domain Signal Mix")
    .WithSubtitle("Stacked areas show how checks contribute to total signal volume")
    .WithXAxis("Run")
    .WithYAxis("Signals")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(920, 560)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
    .AddSmoothStackedArea("Passed", Points(820, 940, 980, 1040, 1120, 1180, 1230), ChartColor.FromRgb(52, 211, 153))
    .AddSmoothStackedArea("Warnings", Points(120, 138, 132, 110, 98, 86, 72), ChartColor.FromRgb(251, 191, 36))
    .AddSmoothStackedArea("Failed", Points(22, 30, 28, 21, 18, 15, 13), ChartColor.FromRgb(248, 113, 113))
    .AddHorizontalLine(1300, "review capacity", ChartColor.FromRgb(96, 165, 250));

SaveChart(signalMix, "domain-signal-mix-stacked-area-dark");

var beforeAfter = Chart.Create()
    .WithTitle("Control Improvement Slope")
    .WithSubtitle("Endpoint comparisons make before/after movement easy to scan")
    .WithXAxis("Review")
    .WithYAxis("Coverage")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddSlope("DMARC enforcement", 58, 88, "Before", "After", ChartColor.FromRgb(37, 99, 235))
    .AddSlope("DNSSEC coverage", 42, 74, ChartColor.FromRgb(16, 185, 129))
    .AddSlope("MTA-STS deployment", 37, 63, ChartColor.FromRgb(245, 158, 11));

SaveChart(beforeAfter, "control-improvement-slope-light");

var sparkline = Chart.Create()
    .WithTitle("Warnings Sparkline")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(360, 90)
    .WithSparkline()
    .AddSmoothArea("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36));

SaveChart(sparkline, "warnings-sparkline");

var donut = Chart.Create()
    .WithTitle("Domain Check Result Mix")
    .WithSubtitle("Static donut chart with zero JavaScript")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(820, 460)
    .WithXLabels("Passed", "Warnings", "Failed")
    .AddDonut("Checks", Points(1260, 68, 10));

SaveChart(donut, "result-mix-donut");

SaveChart(Chart.Create().WithTitle("Domain Check Result Pie").WithSubtitle("Pie slices expose value and percentage metadata for static reports").WithTheme(ChartTheme.ReportLight()).WithSize(820, 460).WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).WithXLabels("Passed", "Warnings", "Failed").AddPie("Checks", Points(1260, 68, 10)), "result-mix-pie-light");
var zeroValueDonut = Chart.Create().WithTitle("Zero-Value Donut").WithSubtitle("Zero slices stay out of the ring without shifting colors or labels").WithTheme(ChartTheme.ReportLight()).WithSize(820, 460).WithXLabels("No findings", "Reviewed", "Escalated").AddDonut("Review mix", Points(0, 82, 18));
zeroValueDonut.Series[0].WithPointColor(1, ChartColor.FromRgb(15, 118, 110)).WithPointSliceOffset(1, 0.08);
SaveChart(zeroValueDonut, "zero-value-donut-light");

var zeroValuePolar = Chart.Create().WithTitle("Zero-Value Polar Area").WithSubtitle("Zero segments keep point indexes stable while positive segments remain styled").WithTheme(ChartTheme.ReportLight()).WithSize(820, 460).WithXLabels("No findings", "Reviewed", "Escalated").AddPolarArea("Review mix", Points(0, 82, 18));
zeroValuePolar.Series[0].WithPointColor(1, ChartColor.FromRgb(15, 118, 110));
SaveChart(zeroValuePolar, "zero-value-polar-area-light");

var zeroValueFunnel = Chart.Create().WithTitle("Zero-Value Funnel Stage").WithSubtitle("Zero stages stay visible and later stages avoid fake drop-off from zero").WithTheme(ChartTheme.ReportLight()).WithSize(920, 560).WithXLabels("Opened", "Deferred", "Closed").AddFunnel("Review flow", Points(100, 0, 18));
zeroValueFunnel.Series[0].WithPointColor(2, ChartColor.FromRgb(124, 58, 237));
SaveChart(zeroValueFunnel, "zero-value-funnel-stage-light");

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

SaveChart(monthly, "monthly-posture-dark");

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

SaveChart(annotationEdge, "annotation-edge-dark");

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

SaveChart(labels, "data-label-readability-dark");

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

SaveChart(latency, "endpoint-latency-light");

var stateChanges = Chart.Create()
    .WithTitle("Policy State Changes")
    .WithSubtitle("Step lines show discrete report state transitions")
    .WithXAxis("Stage")
    .WithYAxis("Controls")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithXLabels("Draft", "Review", "Approved", "Published", "Monitored")
    .AddStepLine("Required controls", Points(12, 18, 18, 26, 31), ChartColor.FromRgb(96, 165, 250))
    .AddHorizontalLine(24, "release gate", ChartColor.FromRgb(251, 191, 36));

SaveChart(stateChanges, "policy-state-step-line-dark");

var policyBacklog = Chart.Create()
    .WithTitle("Policy Backlog Step Area")
    .WithSubtitle("Step areas fill discrete state changes without implying smooth interpolation")
    .WithXAxis("Stage")
    .WithYAxis("Open Controls")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithXLabels("Draft", "Review", "Approved", "Published", "Monitored")
    .AddStepArea("Open controls", Points(31, 26, 26, 18, 12), ChartColor.FromRgb(14, 165, 233))
    .AddStepLine("Release gate", Points(24, 24, 20, 16, 12), ChartColor.FromRgb(37, 99, 235));

SaveChart(policyBacklog, "policy-backlog-step-area-light");

var cost = Chart.Create()
    .WithTitle("License Cost Trend")
    .WithSubtitle("Long formatted y-axis labels keep their own space")
    .WithXAxis("Quarter")
    .WithYAxis("Spend")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithValueFormatter(value => "$" + value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture))
    .WithXLabels("Q1", "Q2", "Q3", "Q4")
    .AddColumnAreaCombo("Actual", Points(820000, 970000, 1010000, 1210000), "Projected", Points(860000, 940000, 1050000, 1160000), ChartColor.FromRgb(14, 165, 233), ChartColor.FromRgb(37, 99, 235));

SaveChart(cost, "license-cost-light");

var regional = Chart.Create()
    .WithTitle("Certificate Transparency by Region")
    .WithSubtitle("Horizontal bars keep region names readable at catalog size")
    .WithXAxis("Certificates")
    .WithYAxis("Region")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(880, 560)
    .WithValueFormatter(value => (value / 1000000d).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "M")
    .WithXLabels("North America", "Western Europe", "Central Europe", "Asia Pacific", "Latin America", "Middle East")
    .AddHorizontalBar("Logged certificates", Points(1200000, 2350000, 1840000, 3120000, 980000, 760000))
    .AddVerticalLine(2000000, "2M benchmark", ChartColor.FromRgb(245, 158, 11));

SaveChart(regional, "ct-regional-light");

var latencyDistribution = Chart.Create()
    .WithTitle("Endpoint Latency Distribution")
    .WithSubtitle("Histogram bins raw samples without caller-side preprocessing")
    .WithXAxis("Latency")
    .WithYAxis("Samples")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithDataLabels()
    .AddHistogram("P95 samples", new[] { 28d, 31d, 36d, 42d, 47d, 58d, 64d, 72d, 86d, 91d, 118d, 146d, 182d }, 5, ChartColor.FromRgb(37, 99, 235));

SaveChart(latencyDistribution, "endpoint-latency-histogram-light");

var readinessLollipop = Chart.Create()
    .WithTitle("Control Readiness")
    .WithSubtitle("Lollipop markers keep category comparison lighter than bars")
    .WithXAxis("Control")
    .WithYAxis("Coverage")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS", "TLS-RPT")
    .AddLollipop("Coverage", Points(96, 88, 74, 63, 58), ChartColor.FromRgb(96, 165, 250))
    .AddHorizontalLine(80, "target", ChartColor.FromRgb(251, 191, 36));

SaveChart(readinessLollipop, "control-readiness-lollipop-dark");

var latencyRanges = Chart.Create()
    .WithTitle("Endpoint Latency Ranges")
    .WithSubtitle("Range bars show observed min/max values")
    .WithXAxis("Probe")
    .WithYAxis("Response time")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
    .WithXLabels("DNS", "TCP", "TLS", "HTTP", "Render")
    .AddRangeBar("Observed", new[] {
        new ChartInterval(1, 18, 42),
        new ChartInterval(2, 44, 88),
        new ChartInterval(3, 96, 142),
        new ChartInterval(4, 128, 196),
        new ChartInterval(5, 144, 232)
    }, ChartColor.FromRgb(14, 165, 233));

SaveChart(latencyRanges, "endpoint-latency-range-light");

var latencySpread = Chart.Create()
    .WithTitle("Endpoint Latency Spread")
    .WithSubtitle("Box plots summarize min, quartiles, median, and max")
    .WithXAxis("Probe")
    .WithYAxis("Response time")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
    .WithXLabels("DNS", "TCP", "TLS", "HTTP")
    .AddBoxPlot("Latency", new[] {
        new ChartBoxPlot(1, 18, 24, 31, 38, 48),
        new ChartBoxPlot(2, 42, 56, 64, 82, 104),
        new ChartBoxPlot(3, 86, 102, 118, 146, 188),
        new ChartBoxPlot(4, 112, 128, 146, 184, 228)
    }, ChartColor.FromRgb(96, 165, 250));

SaveChart(latencySpread, "endpoint-latency-boxplot-dark");

var exposureClusters = Chart.Create()
    .WithTitle("Exposure Clusters")
    .WithSubtitle("Bubble size shows affected assets per cluster")
    .WithXAxis("Reachability")
    .WithYAxis("Exploitability")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithDataLabels()
    .AddBubble("Assets", new[] {
        new ChartBubble(1.0, 24, 8),
        new ChartBubble(1.6, 42, 18),
        new ChartBubble(2.3, 31, 42),
        new ChartBubble(3.1, 68, 71),
        new ChartBubble(3.8, 54, 36)
    }, ChartColor.FromRgb(37, 99, 235));

SaveChart(exposureClusters, "exposure-clusters-bubble-light");

var remediationTrendPoints = new[] {
    new ChartPoint(1, 118),
    new ChartPoint(2, 104),
    new ChartPoint(3, 96),
    new ChartPoint(4, 82),
    new ChartPoint(5, 74),
    new ChartPoint(6, 61)
};
var remediationTrend = Chart.Create()
    .WithTitle("Observed Remediation Trend")
    .WithSubtitle("Scatter observations with computed least-squares trend line")
    .WithXAxis("Week")
    .WithYAxis("Open findings")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithXLabels("W1", "W2", "W3", "W4", "W5", "W6")
    .AddScatter("Observed", remediationTrendPoints, ChartColor.FromRgb(37, 99, 235))
    .AddTrendLine("Trend", remediationTrendPoints, ChartColor.FromRgb(245, 158, 11))
    .AddMeanLine("mean", remediationTrendPoints, ChartColor.FromRgb(14, 165, 233))
    .AddStandardDeviationBand("1 sigma", remediationTrendPoints, 1, ChartColor.FromRgb(96, 165, 250), 0.12);

SaveChart(remediationTrend, "observed-remediation-trend-light");

var confidenceIntervals = Chart.Create()
    .WithTitle("Detection Confidence")
    .WithSubtitle("Error bars show observed lower and upper bounds")
    .WithXAxis("Run")
    .WithYAxis("Confidence")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("Baseline", "Tuning", "Canary", "Rollout", "Steady")
    .AddErrorBar("Confidence", new[] {
        new ChartErrorBar(1, 42, 35, 51),
        new ChartErrorBar(2, 58, 49, 66),
        new ChartErrorBar(3, 63, 54, 78),
        new ChartErrorBar(4, 72, 63, 83),
        new ChartErrorBar(5, 81, 74, 88)
    }, ChartColor.FromRgb(96, 165, 250));

SaveChart(confidenceIntervals, "detection-confidence-errorbar-dark");

var signalWindows = Chart.Create()
    .WithTitle("Signal Windows")
    .WithSubtitle("Candlesticks summarize open, high, low, and close values")
    .WithXAxis("Window")
    .WithYAxis("Signal")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithXLabels("W1", "W2", "W3", "W4", "W5", "W6")
    .AddCandlestick("Signal", new[] {
        new ChartCandlestick(1, 42, 51, 35, 48),
        new ChartCandlestick(2, 58, 66, 49, 54),
        new ChartCandlestick(3, 63, 78, 54, 72),
        new ChartCandlestick(4, 72, 84, 67, 76),
        new ChartCandlestick(5, 76, 82, 68, 71),
        new ChartCandlestick(6, 71, 88, 69, 83)
    });

SaveChart(signalWindows, "signal-windows-candlestick-light");

var signalOhlc = Chart.Create()
    .WithTitle("Signal Windows OHLC")
    .WithSubtitle("OHLC ticks keep dense open/high/low/close windows compact")
    .WithXAxis("Window")
    .WithYAxis("Signal")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithXLabels("W1", "W2", "W3", "W4", "W5", "W6")
    .AddOhlc("Signal", new[] {
        new ChartCandlestick(1, 42, 51, 35, 48),
        new ChartCandlestick(2, 58, 66, 49, 54),
        new ChartCandlestick(3, 63, 78, 54, 72),
        new ChartCandlestick(4, 72, 84, 67, 76),
        new ChartCandlestick(5, 76, 82, 68, 71),
        new ChartCandlestick(6, 71, 88, 69, 83)
    });

SaveChart(signalOhlc, "signal-windows-ohlc-dark");

var forecastEnvelope = Chart.Create()
    .WithTitle("Forecast Envelope")
    .WithSubtitle("Range bands show expected lower and upper bounds")
    .WithXAxis("Run")
    .WithYAxis("Expected Range")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithXLabels("R1", "R2", "R3", "R4", "R5", "R6")
    .AddRangeBand("Expected", new[] {
        new ChartRangeBand(1, 32, 44),
        new ChartRangeBand(2, 38, 58),
        new ChartRangeBand(3, 51, 72),
        new ChartRangeBand(4, 63, 86),
        new ChartRangeBand(5, 68, 92),
        new ChartRangeBand(6, 74, 96)
    }, ChartColor.FromRgb(96, 165, 250));

SaveChart(forecastEnvelope, "forecast-envelope-rangeband-dark");

var forecastInterval = Chart.Create()
    .WithTitle("Forecast Interval")
    .WithSubtitle("Range areas emphasize smoothed prediction intervals beside an observed trend")
    .WithXAxis("Run")
    .WithYAxis("Expected Range")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithXLabels("R1", "R2", "R3", "R4", "R5", "R6", "R7")
    .AddRangeArea("Prediction interval", new[] {
        new ChartRangeBand(1, 28, 44),
        new ChartRangeBand(2, 34, 55),
        new ChartRangeBand(3, 42, 68),
        new ChartRangeBand(4, 54, 78),
        new ChartRangeBand(5, 61, 88),
        new ChartRangeBand(6, 66, 94),
        new ChartRangeBand(7, 72, 98)
    }, ChartColor.FromRgb(14, 165, 233))
    .AddSmoothLine("Observed", Points(36, 49, 61, 70, 80, 86, 91), ChartColor.FromRgb(37, 99, 235));

SaveChart(forecastInterval, "forecast-interval-rangearea-light");

var remediationLift = Chart.Create()
    .WithTitle("Remediation Lift")
    .WithSubtitle("Dumbbells compare before and after values")
    .WithXAxis("Control")
    .WithYAxis("Coverage")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS", "TLS-RPT")
    .AddDumbbell("Before/after", new[] {
        new ChartDumbbell(1, 32, 44),
        new ChartDumbbell(2, 38, 58),
        new ChartDumbbell(3, 51, 72),
        new ChartDumbbell(4, 43, 67),
        new ChartDumbbell(5, 58, 81)
    }, ChartColor.FromRgb(37, 99, 235));

SaveChart(remediationLift, "remediation-lift-dumbbell-light");

var findingsPareto = Chart.Create()
    .WithTitle("Findings Pareto")
    .WithSubtitle("Contribution bars plus cumulative percentage line")
    .WithXAxis("Severity")
    .WithYAxis("Share")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddPareto("Findings", new[] {
        new ChartParetoItem("Critical", 50),
        new ChartParetoItem("High", 28),
        new ChartParetoItem("Medium", 14),
        new ChartParetoItem("Low", 8)
    }, ChartColor.FromRgb(96, 165, 250), ChartColor.FromRgb(251, 191, 36));

SaveChart(findingsPareto, "findings-pareto-dark");

GalleryWriter.Write(output);
Console.WriteLine("Generated files in: " + output);

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
}
