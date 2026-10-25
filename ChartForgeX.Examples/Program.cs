using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
var output = ExampleProgramOptions.OutputDirectory(args);
ExampleOutputDirectory.Reset(output);
const ChartPngOutputScale DemoPngOutputScale = ChartPngOutputScale.Retina;
if (ExampleProgramOptions.TryHandle(args, output, DemoPngOutputScale)) return;
void SaveChart(Chart chart, string name) => ExampleArtifactWriter.SaveChart(chart, output, name, DemoPngOutputScale);
var passed = ChartSeries.CreateArea(new ChartSeriesId("passed"), "Passed", Points(820, 940, 980, 1040, 1120, 1180, 1230, 1260)).WithSmooth();
var warnings = ChartSeries.CreateLine(new ChartSeriesId("warnings"), "Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36)).WithSmooth();
var failed = ChartSeries.CreateLine(new ChartSeriesId("failed"), "Failed", Points(22, 30, 28, 21, 18, 15, 13, 10), ChartColor.FromRgb(248, 113, 113)).WithSmooth();
var warningTarget = new ChartAnnotationId("warning-target");
var dnssec = Chart.Create()
    .WithTitle("Domain Security Checks")
    .WithSubtitle("Dependency-free SVG, HTML and PNG chart rendering")
    .WithXAxis("Run")
    .WithYAxis("Checks")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
    .WithSize(1180, 640)
    .WithTransparentBackground(true)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Next")
    .AddSeries(passed)
    .AddSeries(warnings)
    .AddSeries(failed)
    .AddHorizontalLine(warningTarget, 100, "warning target", ChartColor.FromRgb(251, 191, 36))
    .AddVerticalBand(6, 7, "weekend", ChartColor.FromRgb(96, 165, 250), 0.10);

SaveChart(dnssec, "domain-security-dark");
dnssec.SaveInteractiveHtml(Path.Combine(output, "domain-security-interactive.html"), options => {
    options.PageTitle = "Domain Security Interactive";
    options.IdScope = "domain-security-interactive";
    options.Interaction.ChartId = "domain-security-interactive";
    options.Interaction.GroupName = "example-gallery";
    options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.FocusTrail | ChartInteractionFeatures.RevealLabels | ChartInteractionFeatures.StateBookmarks);
    ExampleInteractiveScenarios.ConfigureDomainSecurity(options.Interaction, passed.Id, warnings.Id, failed.Id);
    options.Interaction.AddScenario("warning-target", "Warning target", scenario => scenario.AddAnnotationStep(warningTarget));
});

MermaidExamples.Write(output);
ConsumerRenderingExamples.Write(Path.Combine(output, "consumer-regressions"));
DenseSignalExamples.Write(output, DemoPngOutputScale);
CartesianCategoryExamples.Write(output, DemoPngOutputScale);
NumericAxisExamples.Write(output, DemoPngOutputScale);
ReportingExamples.Write(output, DemoPngOutputScale);
SunburstBranchHighlightExamples.Write(output);
HierarchyViewExamples.Write(output);
TopologyHierarchyExamples.Write(output);
DenseLegendExamples.Write(output, DemoPngOutputScale);
PagedFacetExamples.Write(output, DemoPngOutputScale);
AnimatedVisualStoryExamples.Write(output, DemoPngOutputScale);
TerminalStoryExamples.Write(output, DemoPngOutputScale);
VisualStoryExamples.Write(output);
StoryPlaybackExamples.Write(output);
StoryReplayExamples.Write(output);
var bars = Chart.Create()
    .WithTitle("Certificate Transparency Volume")
    .WithSubtitle("Bar-line combo with a secondary y-axis and no JavaScript runtime")
    .WithXAxis("Day")
    .WithYAxis("Certificates")
    .WithSecondaryYAxis("Pass rate", value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithSecondaryYAxisBounds(0, 100)
    .WithTheme(VisualTheme.Report())
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
    .WithTheme(VisualTheme.Report())
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
    .WithTheme(VisualTheme.Report())
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

var heatmap = ControlCoverageRecipe.Create();

SaveChart(heatmap, "control-coverage-heatmap-dark");
FoundationExamples.Write(output, DemoPngOutputScale);
MapExamples.Write(output, DemoPngOutputScale, ExampleProgramOptions.HasArg(args, "--include-external-map-examples"));
if (!ExampleProgramOptions.HasArg(args, "--skip-topology")) { TopologyExamples.Write(output); ReadableTopologyExamples.Write(output); } MarkupExamples.Write(output); GraphExplorerExamples.Write(output);

var gauge = Chart.Create()
    .WithTitle("Security Posture Score")
    .WithSubtitle("Single-value gauge for executive report summaries")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
    .WithSize(760, 460)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddGauge("Overall domain readiness", 87, 0, 100, ChartColor.FromRgb(52, 211, 153));

SaveChart(gauge, "security-posture-gauge-dark");

var policyReadinessCircle = Chart.Create()
    .WithTitle("Policy Readiness Circle")
    .WithSubtitle("Circle charts keep single-value progress KPIs compact")
    .WithTheme(VisualTheme.Report())
    .WithSize(760, 460)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddCircle("Ready", 87, 0, 100, ChartColor.FromRgb(16, 185, 129));

SaveChart(policyReadinessCircle, "policy-readiness-circle-light");

var radialBar = Chart.Create()
    .WithTitle("Control Coverage Rings")
    .WithSubtitle("Progress rings compare core security control completion")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
    .WithSize(760, 460)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("Mail auth", "DNSSEC", "Transport TLS", "Certificate CT")
    .AddProgressRing("Average coverage", Points(92, 74, 88, 96));

SaveChart(radialBar, "control-coverage-radialbar-dark");

var bullet = Chart.Create()
    .WithTitle("Control Targets")
    .WithSubtitle("Bullet rows compare current posture against target thresholds")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
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
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
    .WithSize(920, 540)
    .WithDataLabels()
    .WithXLabels("Opened", "Resolved", "Suppressed", "Accepted", "Regressed")
    .AddWaterfall("Finding delta", Points(24, -68, -18, -9, 11), ChartColor.FromRgb(52, 211, 153));

SaveChart(waterfall, "remediation-impact-waterfall-dark");

var radar = Chart.Create()
    .WithTitle("Security Posture Radar")
    .WithSubtitle("Radial comparison across major domain control areas")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
    .WithSize(920, 560)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy", "Monitoring")
    .AddRadar("Current posture", Points(92, 74, 88, 96, 81, 84), ChartColor.FromRgb(96, 165, 250))
    .AddRadar("Target posture", Points(96, 90, 94, 98, 92, 90), ChartColor.FromRgb(52, 211, 153));

SaveChart(radar, "security-posture-radar-dark");

SaveChart(PolarExamples.CreateIncidentDirection(), "incident-direction-polar-dark");

var polarArea = Chart.Create()
    .WithTitle("Control Contribution")
    .WithSubtitle("Polar area segments compare major control contributions")
    .WithTheme(VisualTheme.Report())
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

ExampleArtifactWriter.SaveGrid(controlScorecardGrid, output, "control-scorecards-grid", DemoPngOutputScale);

var sharedCoverageNorth = Chart.Create()
    .WithTitle("Primary Domains")
    .WithSubtitle("Shared y-axis small multiple")
    .WithTheme(VisualTheme.Report())
    .WithSize(520, 320)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS")
    .AddBar("Coverage", Points(96, 88, 74, 63), ChartColor.FromRgb(37, 99, 235));

var sharedCoverageAcquired = Chart.Create()
    .WithTitle("Acquired Domains")
    .WithSubtitle("Shared y-axis small multiple")
    .WithTheme(VisualTheme.Report())
    .WithSize(520, 320)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS")
    .AddBar("Coverage", Points(72, 54, 46, 31), ChartColor.FromRgb(14, 165, 233));

var sharedAxisGrid = ChartGrid.Create()
    .WithTitle("Shared Axis Coverage Comparison")
    .WithSubtitle("Two panels keep shared x and y ranges for honest visual comparison")
    .WithTheme(VisualTheme.Report())
    .WithColumns(2)
    .WithPadding(32)
    .WithPanelSize(500, 300)
    .Add(sharedCoverageNorth)
    .Add(sharedCoverageAcquired)
    .WithSharedAxes();

ExampleArtifactWriter.SaveGrid(sharedAxisGrid, output, "shared-axis-coverage-grid", DemoPngOutputScale);

ExpressiveExamples.Write(output, DemoPngOutputScale); WellnessDashboardExamples.Write(output, DemoPngOutputScale); PremiumSurfaceExamples.Write(output, DemoPngOutputScale); VisualCanvasTypographyExamples.Write(output); DashboardPortfolioExamples.Write(output, DemoPngOutputScale); DashboardPatternExamples.Write(output, DemoPngOutputScale); AxisQualityExamples.Write(output, DemoPngOutputScale);

var funnel = Chart.Create()
    .WithTitle("Domain Remediation Funnel")
    .WithSubtitle("Stage retention from discovery to monitored remediation")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
    .WithSize(920, 560)
    .WithXLabels("Discovered", "Verified", "Prioritized", "Remediated", "Monitored")
    .AddFunnel("Domains", Points(420, 318, 174, 96, 72));

SaveChart(funnel, "domain-remediation-funnel-dark");

var findingsTreemap = Chart.Create()
    .WithDataLabels()
    .WithTitle("Findings Composition")
    .WithSubtitle("Treemap tiles show proportional contribution by category")
    .WithTheme(VisualTheme.Report())
    .WithSize(920, 560)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddTreemap("Finding share", new[] {
        new ChartHierarchyItem("Authentication", "Authentication", value: 34),
        new ChartHierarchyItem("Certificate lifecycle", "Certificate lifecycle", value: 24),
        new ChartHierarchyItem("DNS hygiene", "DNS hygiene", value: 18),
        new ChartHierarchyItem("Policy drift", "Policy drift", value: 14),
        new ChartHierarchyItem("Monitoring gaps", "Monitoring gaps", value: 10)
    });

SaveChart(findingsTreemap, "findings-composition-treemap-light");

var timeline = Chart.Create()
    .WithTitle("Domain Remediation Timeline")
    .WithSubtitle("Date-range items for certificate and policy rollout planning")
    .WithXAxis("Schedule")
    .WithYAxis("Workstream")
    .WithTheme(VisualTheme.Report())
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
    .WithTheme(VisualTheme.Report())
    .WithSize(1040, 600)
    .WithDataLabels()
    .WithGanttToday(new DateTime(2026, 2, 18))
    .AddGanttTask("Inventory scope", new DateTime(2026, 1, 5), new DateTime(2026, 1, 24), 0.90, color: ChartColor.FromRgb(37, 99, 235))
    .AddGanttTask("Owner remediation", new DateTime(2026, 1, 20), new DateTime(2026, 2, 24), 0.62, color: ChartColor.FromRgb(16, 185, 129))
    .AddGanttTask("Control retesting", new DateTime(2026, 2, 18), new DateTime(2026, 3, 12), 0.28, color: ChartColor.FromRgb(14, 165, 233))
    .AddGanttTask("Monitoring handoff", new DateTime(2026, 3, 8), new DateTime(2026, 3, 26), 0.12, color: ChartColor.FromRgb(168, 85, 247))
    .AddGanttMilestone("Executive sign-off", new DateTime(2026, 3, 30), color: ChartColor.FromRgb(245, 158, 11));
for (var stage = 1; stage < gantt.Series.Count; stage++) gantt.AddGanttDependency(gantt.Series[stage - 1], gantt.Series[stage]);

SaveChart(gantt, "domain-remediation-gantt-light");

var findingFlow = Chart.Create()
    .WithTitle("Finding Flow Sankey")
    .WithSubtitle("Weighted paths from discovery through remediation outcomes")
    .WithTheme(VisualTheme.Report())
    .WithSize(1040, 600)
    .WithDataLabels()
    .AddSankey("Findings", new[] { new ChartNode("Discovered", "Discovered"), new ChartNode("Validated", "Validated"), new ChartNode("Accepted risk", "Accepted risk"), new ChartNode("Owner remediation", "Owner remediation"), new ChartNode("Monitoring", "Monitoring"), new ChartNode("Closed", "Closed"), new ChartNode("Retesting", "Retesting") }, new[] {
        new ChartFlowLink("flow-1", "Discovered", "Validated", 72),
        new ChartFlowLink("flow-2", "Discovered", "Accepted risk", 18),
        new ChartFlowLink("flow-3", "Validated", "Owner remediation", 48),
        new ChartFlowLink("flow-4", "Validated", "Monitoring", 24),
        new ChartFlowLink("flow-5", "Owner remediation", "Closed", 34),
        new ChartFlowLink("flow-6", "Owner remediation", "Retesting", 14),
        new ChartFlowLink("flow-7", "Retesting", "Closed", 10),
        new ChartFlowLink("flow-8", "Retesting", "Monitoring", 4)
    });

SaveChart(findingFlow, "finding-flow-sankey-light");

var controlHierarchy = Chart.Create()
    .WithDataLabels()
    .WithTitle("Control Hierarchy Tree")
    .WithSubtitle("Static hierarchy map for ownership and remediation structure")
    .WithTheme(VisualTheme.Report())
    .WithSize(1040, 600)
    .AddTree("Control hierarchy", new[] { new ChartNode("Security posture", "Security posture"), new ChartNode("Mail authentication", "Mail authentication"), new ChartNode("Certificate lifecycle", "Certificate lifecycle"), new ChartNode("DNS hygiene", "DNS hygiene"), new ChartNode("SPF alignment", "SPF alignment"), new ChartNode("DKIM rotation", "DKIM rotation"), new ChartNode("Expiry monitoring", "Expiry monitoring"), new ChartNode("SAN inventory", "SAN inventory"), new ChartNode("DNSSEC rollout", "DNSSEC rollout"), new ChartNode("Stale record cleanup", "Stale record cleanup") }, new[] {
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
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
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
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
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
    .WithTheme(VisualTheme.Report())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddSlope("DMARC enforcement", 58, 88, "Before", "After", ChartColor.FromRgb(37, 99, 235))
    .AddSlope("DNSSEC coverage", 42, 74, ChartColor.FromRgb(16, 185, 129))
    .AddSlope("MTA-STS deployment", 37, 63, ChartColor.FromRgb(245, 158, 11));

SaveChart(beforeAfter, "control-improvement-slope-light");

var sparkline = Chart.Create()
    .WithTitle("Warnings Sparkline")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
    .WithSize(360, 90)
    .WithSparkline()
    .AddSmoothArea("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36));

SaveChart(sparkline, "warnings-sparkline");

var donut = Chart.Create()
    .WithTitle("Domain Check Result Mix")
    .WithSubtitle("Static donut chart with zero JavaScript")
    .WithTheme(VisualTheme.Report())
    .WithSize(820, 460)
    .WithXLabels("Passed", "Warnings", "Failed")
    .AddDonut("Checks", Points(1260, 68, 10));

SaveChart(donut, "result-mix-donut");

SaveChart(Chart.Create().WithTitle("Domain Check Result Pie").WithSubtitle("Pie slices expose value and percentage metadata for static reports").WithTheme(VisualTheme.Report()).WithSize(820, 460).WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).WithXLabels("Passed", "Warnings", "Failed").AddPie("Checks", Points(1260, 68, 10)), "result-mix-pie-light");
var zeroValueDonut = Chart.Create().WithTitle("Zero-Value Donut").WithSubtitle("Zero slices stay out of the ring without shifting colors or labels").WithTheme(VisualTheme.Report()).WithSize(820, 460).WithXLabels("No findings", "Reviewed", "Escalated").AddDonut("Review mix", Points(0, 82, 18));
zeroValueDonut.Series[0].WithPointColor(1, ChartColor.FromRgb(15, 118, 110)).WithPointSliceOffset(1, 0.08);
SaveChart(zeroValueDonut, "zero-value-donut-light");

SaveChart(PolarExamples.CreateZeroValueArea(), "zero-value-polar-area-light");

var zeroValueFunnel = Chart.Create().WithTitle("Zero-Value Funnel Stage").WithSubtitle("Zero stages stay visible and later stages avoid fake drop-off from zero").WithTheme(VisualTheme.Report()).WithSize(920, 560).WithXLabels("Opened", "Deferred", "Closed").AddFunnel("Review flow", Points(100, 0, 18));
zeroValueFunnel.Series[0].WithPointColor(2, ChartColor.FromRgb(124, 58, 237));
SaveChart(zeroValueFunnel, "zero-value-funnel-stage-light");

var monthly = Chart.Create()
    .WithTitle("Monthly Security Posture")
    .WithSubtitle("Automatic label density with wrapped legend rows")
    .WithXAxis("Month")
    .WithYAxis("Score")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
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
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
    .WithSize(760, 460)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
    .AddSmoothLine("Checks", Points(42, 68, 72, 95, 104, 118), ChartColor.FromRgb(96, 165, 250))
    .AddVerticalLine(6, "right edge marker", ChartColor.FromRgb(251, 191, 36))
    .AddHorizontalLine(100, "target", ChartColor.FromRgb(52, 211, 153));

annotationEdge.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 28, dash: Array.Empty<double>());
annotationEdge.Annotations[1].StrokeStyle = new ChartStrokeStyle(width: 12, opacity: .65);
SaveChart(annotationEdge, "annotation-edge-dark");

var labels = Chart.Create()
    .WithTitle("Data Label Readability")
    .WithSubtitle("Edge-aware labels with SVG text halos")
    .WithXAxis("Day")
    .WithYAxis("Signals")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
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
    .WithTheme(VisualTheme.Report())
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
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
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
    .WithTheme(VisualTheme.Report())
    .WithSize(760, 460)
    .WithDataLabels()
    .WithXLabels("Draft", "Review", "Approved", "Published", "Monitored")
    .AddStepArea("Open controls", Points(31, 26, 26, 18, 12), ChartColor.FromRgb(14, 165, 233))
    .AddStepLine("Release gate", Points(24, 24, 20, 16, 12), ChartColor.FromRgb(37, 99, 235));

SaveChart(policyBacklog, "policy-backlog-step-area-light");

var cost = LicenseCostRecipe.Create();

SaveChart(cost, "license-cost-light");

var regional = Chart.Create()
    .WithTitle("Certificate Transparency by Region")
    .WithSubtitle("Horizontal bars keep region names readable at catalog size")
    .WithXAxis("Certificates")
    .WithYAxis("Region")
    .WithTheme(VisualTheme.Report())
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
    .WithTheme(VisualTheme.Report())
    .WithSize(760, 460)
    .WithDataLabels()
    .AddHistogram("P95 samples", new[] { 28d, 31d, 36d, 42d, 47d, 58d, 64d, 72d, 86d, 91d, 118d, 146d, 182d }, ChartHistogramBinLayout.FromWidth(28, 182, 35), ChartColor.FromRgb(37, 99, 235));

SaveChart(latencyDistribution, "endpoint-latency-histogram-light");

var readinessLollipop = Chart.Create()
    .WithTitle("Control Readiness")
    .WithSubtitle("Lollipop markers keep category comparison lighter than bars")
    .WithXAxis("Control")
    .WithYAxis("Coverage")
    .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
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
    .WithTheme(VisualTheme.Report())
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

StatisticalExamples.Write(output, DemoPngOutputScale);
CompositionShapeExamples.Write(output);
GraphiteExamples.Write(output, DemoPngOutputScale);
GalleryWriter.Write(output);
Console.WriteLine("Generated files in: " + output);

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
}
