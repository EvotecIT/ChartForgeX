using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Report-oriented examples that protect monitoring and assessment scenarios.</summary>
internal static class ReportingExamples {
    private static readonly DateTime WindowStart = new(2026, 9, 24, 6, 0, 0, DateTimeKind.Utc);

    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
        WriteTimeAxis(output, pngOutputScale);
        WriteStateTimeline(output, pngOutputScale);
        WriteStatusMatrix(output, pngOutputScale);
        WriteIncidentLanes(output, pngOutputScale);
        WriteHourWeekday(output, pngOutputScale);
    }

    private static void WriteHourWeekday(string output, ChartPngOutputScale pngOutputScale) {
        // Four weeks of synthetic sign-in failures: business-hour peaks, a Monday-morning spike, and quiet weekends.
        var events = new List<ChartTimedValue>();
        var monday = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);
        for (var day = 0; day < 28; day++) {
            var weekday = day % 7;
            for (var hour = 0; hour < 24; hour++) {
                var business = hour is >= 7 and <= 17 ? 6 + (hour == 9 ? 4 : 0) : 1;
                var weekend = weekday >= 5 ? 0.25 : 1;
                var spike = weekday == 0 && hour is 8 or 9 ? 6 : 0;
                var total = (int)Math.Round(business * weekend + spike + ((day * 31 + hour * 17) % 5) * 0.4);
                for (var i = 0; i < total; i++) events.Add(new ChartTimedValue(monday.AddDays(day).AddHours(hour).AddMinutes(i * 7 % 60)));
            }
        }

        var chart = Chart.Create()
            .WithTitle("Sign-in failures by hour and weekday")
            .WithSubtitle("Four weeks of events; darker cells had more failures")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1180, 440)
            .WithPngOutputScale(pngOutputScale)
            .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Hidden)
            .AddHourWeekdayHeatmap(events, color: ChartColor.FromHex("#2a78d6"));

        chart.SaveSvg(Path.Combine(output, "reporting-hour-weekday.svg"));
        chart.SaveHtml(Path.Combine(output, "reporting-hour-weekday.html"));
        chart.SavePng(Path.Combine(output, "reporting-hour-weekday.png"));
    }

    private static void WriteIncidentLanes(string output, ChartPngOutputScale pngOutputScale) {
        // Severity colours come from the Graphite palette v1 defaults of VisualStatusTokens.
        var start = WindowStart;
        ChartGanttLaneItem Incident(double fromHours, double? toHours, string severity, string label, string? detail = null) =>
            new(start.AddHours(fromHours), toHours.HasValue ? start.AddHours(toHours.Value) : null, severity, label, detail);
        var chart = Chart.Create()
            .WithTitle("Incidents by service")
            .WithSubtitle("Last 48 hours; overlapping incidents stack within a lane and open incidents run to now")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1180, 560)
            .WithPngOutputScale(pngOutputScale)
            .WithXAxisTimeScale(showTimeZone: true)
            .WithGanttToday(start.AddHours(44))
            .WithStateCategories(new VisualStatusTokens().SeverityCategories());
        chart.Options.LaneSummaryHeader = "Incidents";
        chart.AddGanttLane("LDAP", new[] { Incident(2, 5.5, "high", "Bind latency", "p95 above 250 ms"), Incident(20, 21, "low", "Slow search") }, "Warsaw", "2")
            .AddGanttLane("Replication", new[] { Incident(8, 30, "critical", "USN rollback suspected"), Incident(12, 16, "medium", "Queue backlog"), Incident(26, null, "medium", "Link flapping") }, "Warsaw", "3")
            .AddGanttLane("DNS", Array.Empty<ChartGanttLaneItem>(), "Warsaw", "0")
            .AddGanttLane("Kerberos", new[] { Incident(33, 36, "high", "KDC errors") }, "Frankfurt", "1")
            .AddGanttLane("Time sync", new[] { Incident(1, 3, "info", "Drift 2 s"), Incident(40, null, "low", "Drift 4 s") }, "Frankfurt", "2")
            .AddGanttLane("Certificates", new[] { Incident(14, 38, "medium", "Template expiring") }, "London", "1");

        chart.SaveSvg(Path.Combine(output, "reporting-incident-lanes.svg"));
        chart.SaveHtml(Path.Combine(output, "reporting-incident-lanes.html"));
        chart.SavePng(Path.Combine(output, "reporting-incident-lanes.png"));
    }

    private static void WriteStatusMatrix(string output, ChartPngOutputScale pngOutputScale) {
        // Graphite palette v1 (light): a failed check is coloured by its severity; not evaluated is neutral and hatched.
        var chart = Chart.Create()
            .WithTitle("Directory health by domain controller")
            .WithSubtitle("Worst finding per check; select a cell to open its evidence")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1180, 520)
            .WithPngOutputScale(pngOutputScale)
            .WithStateCategories(
                new ChartStateCategory("pass", "Passed", ChartColor.FromHex("#1d8a52")),
                new ChartStateCategory("low", "Low", ChartColor.FromHex("#0c8aa8")),
                new ChartStateCategory("medium", "Medium", ChartColor.FromHex("#c78404")),
                new ChartStateCategory("high", "High", ChartColor.FromHex("#dd5a17")),
                new ChartStateCategory("critical", "Critical", ChartColor.FromHex("#d4302f")),
                new ChartStateCategory("notEvaluated", "Not evaluated", ChartColor.FromHex("#7c818a"), hatched: true))
            .WithXLabels("Replication", "SYSVOL", "DNS", "Time sync", "LDAP", "Kerberos", "Certificates", "Backups", "Services", "Disk");
        var severities = new[] { "pass", "pass", "pass", "low", "pass", "medium", "pass", "high", "pass", "critical" };
        var names = new[] { "DC01-WAW", "DC02-WAW", "DC03-KRK", "DC04-GDN", "DC05-FRA", "DC06-FRA", "DC07-LON", "DC08-NYC" };
        for (var row = 0; row < names.Length; row++) {
            var cells = new ChartHeatmapCell?[10];
            for (var column = 0; column < cells.Length; column++) {
                if (row == 6 && column == 7) continue;
                var state = row == 3 && column >= 8 ? "notEvaluated" : severities[(row * 7 + column * 3) % severities.Length];
                var findings = state is "pass" or "notEvaluated" ? null : ((row + column) % 4 + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                cells[column] = new ChartHeatmapCell(state, findings, href: "#" + names[row].ToLowerInvariant() + "-check-" + (column + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            chart.AddHeatmapCategoryRow(names[row], cells);
        }

        chart.SaveSvg(Path.Combine(output, "reporting-status-matrix.svg"));
        chart.SaveHtml(Path.Combine(output, "reporting-status-matrix.html"));
        chart.SavePng(Path.Combine(output, "reporting-status-matrix.png"));
    }

    private static void WriteStateTimeline(string output, ChartPngOutputScale pngOutputScale) {
        // Graphite palette v1 (light): operational states reuse the severity and outcome hues, never series colours.
        var states = new[] {
            new ChartStateCategory("up", "Up", ChartColor.FromHex("#1d8a52")),
            new ChartStateCategory("degraded", "Degraded", ChartColor.FromHex("#c78404")),
            new ChartStateCategory("down", "Down", ChartColor.FromHex("#d4302f")),
            new ChartStateCategory("recovering", "Recovering", ChartColor.FromHex("#0c8aa8")),
            new ChartStateCategory("maintenance", "Maintenance", ChartColor.FromHex("#6b5bd2")),
            new ChartStateCategory("notObservable", "Not observable", ChartColor.FromHex("#7c818a"), hatched: true)
        };
        var chart = Chart.Create()
            .WithTitle("Domain controller availability")
            .WithSubtitle("15-minute rollups over the last 24 hours; gaps mean no data was collected")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1180, 520)
            .WithPngOutputScale(pngOutputScale)
            .WithXAxisTimeScale(showTimeZone: true)
            .WithStateCategories(states);
        chart.Options.LaneSummaryHeader = "Available";
        var start = WindowStart.AddHours(12);
        var names = new[] { "DC01-WAW", "DC02-WAW", "DC03-KRK", "DC04-GDN", "DC05-FRA", "DC06-FRA", "DC07-LON", "DC08-NYC" };
        for (var lane = 0; lane < names.Length; lane++) {
            var segments = new List<ChartStateTimelineSegment>();
            var up = 0.0;
            var observed = 0.0;
            for (var bucket = 0; bucket < 96; bucket++) {
                var state = BucketState(lane, bucket);
                if (state == null) continue;
                var from = start.AddMinutes(bucket * 15);
                segments.Add(new ChartStateTimelineSegment(from, from.AddMinutes(15), state, state == "down" ? "LDAP bind failed" : null));
                if (state != "notObservable") observed += 15;
                if (state == "up") up += 15;
            }

            chart.AddStateTimelineLane(names[lane], segments, (up / observed).ToString("0.0%", System.Globalization.CultureInfo.InvariantCulture));
        }

        chart.SaveSvg(Path.Combine(output, "reporting-state-timeline.svg"));
        chart.SaveHtml(Path.Combine(output, "reporting-state-timeline.html"));
        chart.SavePng(Path.Combine(output, "reporting-state-timeline.png"));
        chart.SaveInteractiveHtml(Path.Combine(output, "reporting-state-timeline-interactive.html"), options => {
            options.PageTitle = "Domain controller availability";
            options.IdScope = "reporting-state-timeline-interactive";
        });
    }

    private static string? BucketState(int lane, int bucket) {
        if (lane == 3 && bucket is >= 40 and < 46) return null;
        if (lane == 5 && bucket is >= 8 and < 16) return "maintenance";
        if (lane == 2 && bucket is >= 60 and < 63) return "down";
        if (lane == 2 && bucket is >= 63 and < 66) return "recovering";
        if (lane == 6 && bucket is >= 70 and < 80) return "notObservable";
        var hash = (lane * 7919 + bucket * 104729) % 97;
        return hash < 4 ? "degraded" : "up";
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
