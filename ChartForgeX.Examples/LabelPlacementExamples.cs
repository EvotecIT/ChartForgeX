using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

/// <summary>Dense chart fixtures shared by the gallery and rendering correctness checks.</summary>
public static class LabelPlacementExamples {
    /// <summary>Creates bullet, funnel, gauge, route-map and Sankey panels in the requested theme.</summary>
    public static IReadOnlyList<(string Name, Chart Chart)> Charts(bool dark) {
        var theme = dark ? ChartTheme.ReportDark() : ChartTheme.ReportLight();
        var bullet = Chart.Create().WithTitle("Control targets").WithSize(640, 360).WithTheme(theme)
            .WithValueFormatter(v => v.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .AddBullet("DMARC enforcement", 88, 95, 0, 100, new[] { 60d, 80d })
            .AddBullet("DNSSEC coverage", 74, 90, 0, 100, new[] { 55d, 78d })
            .AddBullet("MTA-STS deployment", 63, 85, 0, 100, new[] { 50d, 75d });
        var funnel = Chart.Create().WithTitle("Remediation funnel").WithSize(640, 420).WithTheme(theme)
            .WithXLabels("Discovered", "Verified", "Prioritized", "Remediated", "Monitored")
            .AddFunnel("Domains", Points(420, 318, 174, 96, 72));
        var gauge = Chart.Create().WithTitle("Policy readiness").WithSize(480, 340).WithTheme(theme)
            .AddGauge("Policy readiness", 86, 0, 100);
        var map = Chart.Create().WithTitle("European routes").WithSize(760, 460).WithTheme(theme).WithDataLabels().WithLegend(false)
            .WithMapViewport(ChartMapViewport.Europe())
            .AddDottedMap("Places", new[] {
                new ChartMapPoint("Madrid", -3.7038, 40.4168), new ChartMapPoint("Paris", 2.3522, 48.8566),
                new ChartMapPoint("London", -0.1278, 51.5074), new ChartMapPoint("Warsaw", 21.0122, 52.2297),
                new ChartMapPoint("Berlin", 13.4050, 52.5200), new ChartMapPoint("Rome", 12.4964, 41.9028)
            }).AddMapRouteBetweenPoints("Madrid to Paris", "Madrid", "Paris")
            .AddMapRouteBetweenPoints("London to Warsaw", "London", "Warsaw")
            .AddMapRouteBetweenPoints("Berlin to Rome", "Berlin", "Rome");
        var sankey = Chart.Create().WithTitle("Finding flow").WithSize(760, 460).WithTheme(theme).WithDataLabels()
            .AddSankey("Findings", new[] {
                new ChartSankeyLink("Discovered", "Validated", 72), new ChartSankeyLink("Discovered", "Accepted risk", 18),
                new ChartSankeyLink("Validated", "Remediation", 48), new ChartSankeyLink("Validated", "Monitoring", 24),
                new ChartSankeyLink("Remediation", "Closed", 34), new ChartSankeyLink("Remediation", "Retesting", 14)
            });
        return new[] { ("bullet", bullet), ("funnel", funnel), ("gauge", gauge), ("europe-routes", map), ("sankey", sankey) };
    }

    /// <summary>Creates a dashboard with dense labels at the target PNG density.</summary>
    public static ChartGrid Scorecard(bool dark) {
        var panels = Charts(dark);
        return ChartGrid.Create().WithTitle("Measured control scorecards").WithColumns(2).WithPngOutputScale(2)
            .Add(panels[2].Chart).Add(panels[0].Chart).Add(panels[1].Chart).Add(panels[3].Chart);
    }

    /// <summary>Creates a topology with close captions, multiline nodes and labelled routes.</summary>
    public static TopologyChart Topology(bool dark) => TopologyChart.Create().WithViewport(1000, 500)
        .WithTheme(dark ? TopologyTheme.Dark() : TopologyTheme.Light()).WithTitle("Measured service topology")
        .AddNode("gateway", "Regional gateway", 70, 170, width: 180, height: 96, subtitle: "Traffic and routing")
        .AddNode("identity", "Identity service", 380, 120, width: 190, height: 96, subtitle: "Authentication")
        .AddNode("policy", "Policy evaluation", 380, 290, width: 190, height: 96, subtitle: "Control checks")
        .AddNode("reports", "Reporting service", 730, 190, width: 190, height: 96, subtitle: "Measured output")
        .AddEdge("auth", "gateway", "identity", "Authentication traffic")
        .AddEdge("controls", "gateway", "policy", "Policy checks")
        .AddEdge("identity-report", "identity", "reports", "Identity results")
        .AddEdge("policy-report", "policy", "reports", "Control results");

    internal static void Write(string output) {
        foreach (var dark in new[] { false, true }) {
            var suffix = dark ? "dark" : "light";
            foreach (var (name, chart) in Charts(dark)) {
                chart.WithPngOutputScale(2);
                var chartPath = Path.Combine(output, "label-placement-" + name + "-" + suffix);
                chart.SaveSvg(chartPath + ".svg"); chart.SavePng(chartPath + ".png"); chart.SaveHtml(chartPath + ".html");
            }
            ExampleArtifactWriter.SaveGrid(Scorecard(dark), output, "label-placement-scorecards-" + suffix, ChartPngOutputScale.Retina);
            var path = Path.Combine(output, "label-placement-topology-" + suffix);
            var topology = Topology(dark); var options = new TopologyRenderOptions { IncludeLegend = false, PngOutputScale = 2 };
            File.WriteAllText(path + ".svg", topology.ToSvg(options)); File.WriteAllBytes(path + ".png", topology.ToPng(options));
            File.WriteAllText(path + ".html", topology.ToHtmlPage(options));
        }
    }
    private static ChartPoint[] Points(params double[] values) => values.Select((v, i) => new ChartPoint(i + 1, v)).ToArray();
}
