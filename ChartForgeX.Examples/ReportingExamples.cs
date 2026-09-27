using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Report-oriented examples that protect monitoring and assessment scenarios.</summary>
internal static class ReportingExamples {
    private static readonly DateTime WindowStart = new(2026, 9, 24, 6, 0, 0, DateTimeKind.Utc);

    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
        WriteTimeAxis(output, pngOutputScale);
        WriteTimeAxisRegressions(output, pngOutputScale);
    }

    private static void WriteTimeAxisRegressions(string output, ChartPngOutputScale pngOutputScale) {
        var start = WindowStart.ToOADate();
        var end = start + 200.0 / 86400000;
        var fast = Chart.Create().WithTitle("Sub-second time-axis fallback")
            .WithSubtitle("Fractional OLE Automation dates keep distinct values below one second")
            .WithTheme(ChartTheme.ReportLight()).WithSize(1000, 340).WithPngOutputScale(pngOutputScale)
            .WithXAxisTimeScale().WithXAxis("Observed (OLE Automation days)").WithYAxis("Sample")
            .AddLine("Signal", new[] { new ChartPoint(start, 1), new ChartPoint(end, 2) });
        fast.Options.XAxis.WithBounds(start, end);
        fast.SaveSvg(Path.Combine(output, "reporting-time-axis-subsecond.svg"));
        fast.SaveHtml(Path.Combine(output, "reporting-time-axis-subsecond.html"));
        fast.SavePng(Path.Combine(output, "reporting-time-axis-subsecond.png"));

        var zone = TimeZoneInfo.CreateCustomTimeZone("Example/Plus5", TimeSpan.FromHours(5), "UTC+05", "UTC+05");
        var schedule = Chart.Create().WithTitle("Classic wall-clock schedule")
            .WithSubtitle("Date-based schedules retain wall-clock dates and do not advertise an unapplied display zone")
            .WithTheme(ChartTheme.ReportLight()).WithSize(1000, 340).WithPngOutputScale(pngOutputScale)
            .WithXAxis("Window").WithXAxisTimeScale(zone, showTimeZone: true, label: "UTC+05")
            .WithTickCount(3)
            .AddGanttTask("Maintenance", WindowStart.Date, WindowStart.Date.AddDays(2));
        schedule.SaveSvg(Path.Combine(output, "reporting-classic-wall-clock.svg"));
        schedule.SaveHtml(Path.Combine(output, "reporting-classic-wall-clock.html"));
        schedule.SavePng(Path.Combine(output, "reporting-classic-wall-clock.png"));
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
