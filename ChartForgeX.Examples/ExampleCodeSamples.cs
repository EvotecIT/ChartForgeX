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
    }

    private static void WriteCSharp(string output, string name, string code) =>
        File.WriteAllText(Path.Combine(output, name + ".csharp.txt"), code.Replace("\r\n", "\n"));
}
