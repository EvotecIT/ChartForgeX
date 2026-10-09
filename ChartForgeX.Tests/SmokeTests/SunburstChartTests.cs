using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using System.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SunburstLinksRenderRadialHierarchy() {
        var chart = Chart.Create()
            .WithDataLabels()
            .WithSize(760, 520)
            .WithTheme(ChartTheme.Aurora())
            .AddSunburst("Control partition", new[] { new ChartNode("Security posture", "Security posture"), new ChartNode("Mail authentication", "Mail authentication"), new ChartNode("Certificate lifecycle", "Certificate lifecycle"), new ChartNode("DNS hygiene", "DNS hygiene"), new ChartNode("SPF", "SPF"), new ChartNode("DKIM", "DKIM"), new ChartNode("Expiry monitoring", "Expiry monitoring") }, new[] {
                new ChartTreeLink("Security posture", "Mail authentication", 42),
                new ChartTreeLink("Security posture", "Certificate lifecycle", 28),
                new ChartTreeLink("Security posture", "DNS hygiene", 18),
                new ChartTreeLink("Mail authentication", "SPF", 16),
                new ChartTreeLink("Mail authentication", "DKIM", 14),
                new ChartTreeLink("Certificate lifecycle", "Expiry monitoring", 17)
            });

        var svg = chart.ToSvg();
        var prepared = PreparedFamily(chart);
        Assert(FamilyGroups(prepared, "hierarchy-series").Length == 1, "Sunbursts should retain their hierarchy series identity.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"sunburst-segment\"") == 7, "Sunburst charts should render one segment per hierarchy node.");
        Assert(svg.Contains("data-cfx-depth=\"2\"", StringComparison.Ordinal), "Sunburst segments should expose depth metadata.");
        Assert(prepared.Regions.Any(region => region.Role == "sunburst-segment" && region.Label!.StartsWith("Mail authentication:", StringComparison.Ordinal)), "Sunburst segments should expose complete descriptive summaries.");
        Assert(svg.Contains("data-cfx-role=\"sunburst-label\"", StringComparison.Ordinal), "Sunburst charts should render readable labels.");
        Assert(chart.ToPng().Length > 64, "Sunburst charts should render PNG output.");
    }
}
