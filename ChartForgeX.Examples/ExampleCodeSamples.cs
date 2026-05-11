/// <summary>
/// Writes readable source snippets beside selected generated examples.
/// </summary>
internal static class ExampleCodeSamples {
    public static void Write(string output) {
        WriteCSharp(output, "license-cost-light", """
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

static IEnumerable<ChartPoint> Points(params double[] values) {
    for (var index = 0; index < values.Length; index++) {
        yield return new ChartPoint(index + 1, values[index]);
    }
}

var chart = Chart.Create()
    .WithTitle("License Cost Trend")
    .WithSubtitle("Long formatted y-axis labels keep their own space")
    .WithXAxis("Quarter")
    .WithYAxis("Spend")
    .WithTheme(ChartTheme.ReportLight())
    .WithSize(760, 460)
    .WithValueFormatter(value => "$" + value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture))
    .WithXLabels("Q1", "Q2", "Q3", "Q4")
    .AddColumnAreaCombo(
        "Actual",
        Points(820000, 970000, 1010000, 1210000),
        "Projected",
        Points(860000, 940000, 1050000, 1160000),
        ChartColor.FromRgb(14, 165, 233),
        ChartColor.FromRgb(37, 99, 235));

chart.SaveSvg("license-cost-light.svg");
chart.SavePng("license-cost-light.png");
chart.SaveHtml("license-cost-light.html");
""");

        WriteCSharp(output, "dashboard-premium-trend-style", """
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

static IEnumerable<ChartPoint> Points(params double[] values) {
    for (var index = 0; index < values.Length; index++) {
        yield return new ChartPoint(index + 1, values[index]);
    }
}

var chart = Chart.Create()
    .WithHeader(false)
    .WithTransparentBackground(false)
    .WithTheme(ChartTheme.SaasDashboardLight()
        .WithPalette(new[] { ChartColor.FromHex("#2563EB"), ChartColor.FromHex("#14B8A6") })
        .WithGuideColors(ChartColor.FromHex("#D7DEE8"), ChartColor.FromHex("#94A3B8"))
        .WithMarkerRadius(3.2)
        .WithTextColors(ChartColor.FromHex("#64748B"), ChartColor.FromHex("#8792A5")))
    .WithDashboardPanelStyle()
    .WithLegend(false)
    .WithPadding(36, 30, 26, 58)
    .WithSize(840, 360)
    .WithXLabels("Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday")
    .WithYAxisBounds(0, 100)
    .WithHighlightedXAxisLabel(4, paletteIndex: 0)
    .AddSmoothArea("Saved", Points(24, 36, 48, 58, 46, 68, 76))
    .AddSmoothLine("Outreach", Points(18, 28, 36, 48, 40, 58, 66));

chart.SaveSvg("dashboard-premium-trend-style.svg");
chart.SavePng("dashboard-premium-trend-style.png");
chart.SaveHtml("dashboard-premium-trend-style.html");
""");

        WriteCSharp(output, "control-coverage-heatmap-dark", """
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

static IEnumerable<ChartPoint> Points(params double[] values) {
    for (var index = 0; index < values.Length; index++) {
        yield return new ChartPoint(index + 1, values[index]);
    }
}

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

heatmap.SaveSvg("control-coverage-heatmap-dark.svg");
heatmap.SavePng("control-coverage-heatmap-dark.png");
heatmap.SaveHtml("control-coverage-heatmap-dark.html");
""");

        WriteCSharp(output, "travel-dotted-map-dark", """
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var travelMap = Chart.Create()
    .WithTitle("Travel Map")
    .WithSubtitle("Dotted world layer with highlighted longitude and latitude points")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(980, 560)
    .WithLegend(false)
    .WithDataLabels()
    .AddDottedMap("Visited", new[] {
        new ChartMapPoint("Indonesia", 113.9213, -0.7893, ChartColor.FromRgb(34, 197, 94)),
        new ChartMapPoint("Spain", -3.7038, 40.4168, ChartColor.FromRgb(34, 197, 94)),
        new ChartMapPoint("United States", -98.5795, 39.8283, ChartColor.FromRgb(34, 197, 94)),
        new ChartMapPoint("Norway", 8.4689, 60.4720, ChartColor.FromRgb(59, 130, 246))
    })
    .AddMapRouteBetweenPoints("United States to Spain", "United States", "Spain", ChartColor.FromRgb(34, 197, 94))
    .AddMapRouteBetweenPoints("Spain to Indonesia", "Spain", "Indonesia", ChartColor.FromRgb(59, 130, 246));

travelMap.SaveSvg("travel-dotted-map-dark.svg");
travelMap.SavePng("travel-dotted-map-dark.png");
travelMap.SaveHtml("travel-dotted-map-dark.html");
""");
    }

    private static void WriteCSharp(string output, string name, string code) =>
        File.WriteAllText(Path.Combine(output, name + ".csharp.txt"), code.Replace("\r\n", "\n"));
}
