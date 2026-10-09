using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using System.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SunburstItemsRenderRadialHierarchy() {
        var chart = Chart.Create()
            .WithDataLabels()
            .WithSize(760, 520)
            .WithTheme(ChartTheme.Aurora())
            .AddSunburst("Control partition", new[] { new ChartHierarchyItem("Security posture", "Security posture"), new ChartHierarchyItem("Mail authentication", "Mail authentication", "Security posture", 42), new ChartHierarchyItem("Certificate lifecycle", "Certificate lifecycle", "Security posture", 28), new ChartHierarchyItem("DNS hygiene", "DNS hygiene", "Security posture", 18), new ChartHierarchyItem("SPF", "SPF", "Mail authentication", 16), new ChartHierarchyItem("DKIM", "DKIM", "Mail authentication", 14), new ChartHierarchyItem("Expiry monitoring", "Expiry monitoring", "Certificate lifecycle", 17) });

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
