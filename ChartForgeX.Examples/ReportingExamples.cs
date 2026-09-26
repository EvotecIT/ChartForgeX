using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Report-oriented examples that protect monitoring and assessment scenarios.</summary>
internal static class ReportingExamples {
    private static readonly DateTime WindowStart = new(2026, 9, 24, 6, 0, 0, DateTimeKind.Utc);

    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
        WriteTimeAxis(output, pngOutputScale);
    }

    private static void WriteTimeAxis(string output, ChartPngOutputScale pngOutputScale) {
        // 36 hours of 5-minute LDAP bind latency with a 50-minute collection outage.
        var samples = Enumerable.Range(0, 36 * 12 + 1)
            .Where(index => index < 200 || index >= 210)
            .Select(index => (Index: index, Time: WindowStart.AddMinutes(index * 5)))
            .ToArray();
        ChartPoint Point((int Index, DateTime Time) sample, double value) => new(sample.Time, value, sample.Index == 210);
        double P50(int index) => 18 + Math.Sin(index / 24d) * 3 + Math.Sin(index / 3.1) * 0.8;
        double P95(int index) => P50(index) + 14 + Math.Sin(index / 9d) * 4 + (index is > 300 and < 318 ? 38 : 0);

        var chart = Chart.Create()
            .WithTitle("LDAP bind latency")
            .WithSubtitle("5-minute rollups; the collection outage stays empty instead of being bridged")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1180, 480)
            .WithPngOutputScale(pngOutputScale)
            .WithXAxis("Observed")
            .WithYAxis("Latency (ms)")
            .WithXAxisTimeScale(showTimeZone: true)
            .AddLine("p50", samples.Select(sample => Point(sample, P50(sample.Index))), ChartColor.FromHex("#2a78d6"))
            .AddLine("p95", samples.Select(sample => Point(sample, P95(sample.Index))), ChartColor.FromHex("#0f9f8c"));
        foreach (var series in chart.Series) series.WithStrokeWidth(2).WithMarkerRadius(0);

        chart.SaveSvg(Path.Combine(output, "reporting-time-axis-utc.svg"));
        chart.SaveHtml(Path.Combine(output, "reporting-time-axis-utc.html"));
        chart.SavePng(Path.Combine(output, "reporting-time-axis-utc.png"));
    }
}
