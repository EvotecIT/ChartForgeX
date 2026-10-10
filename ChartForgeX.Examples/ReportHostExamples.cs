using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>
/// Examples that protect what report hosts ask of charts drawn into a card: narrow widths, rotated labels, and short
/// heights.
/// </summary>
internal static class ReportHostExamples {
    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
        WriteRotatedStatusMatrix(output, pngOutputScale);
        WriteShortCardCalendar(output, pngOutputScale);
        WriteFlatHistogram(output, pngOutputScale);
        WriteThemedCalendar(output, pngOutputScale);
        WriteThemedStatusCounts(output, pngOutputScale);
    }

    private static void WriteThemedStatusCounts(string output, ChartPngOutputScale pngOutputScale) {
        // A status matrix with a count in every cell, its SVG colours written as the custom properties of the design
        // tokens: the count takes the card colour on solid marks and the text colour on quiet ones, by role, so a host that
        // draws the matrix without card or plot surface can ship one SVG for its light and dark themes.
        var tokens = VisualDesignTokens.Dark();
        var states = tokens.Status.OperationalStateCategories()
            .Select(state => state.Key == "up" ? new ChartStateCategory(state.Key, state.Label, state.Color, state.Pattern, ChartStateEmphasis.Quiet) : state)
            .ToArray();
        string[] checks = { "Replication", "DNS", "Time", "SYSVOL", "LDAP", "Backup" };
        var chart = Chart.Create()
            .WithTitle("Checks per site")
            .WithSubtitle("Counts take the card or text colour by role, not a fixed contrast colour")
            .WithDesignTokens(tokens)
            .WithSize(760, 360)
            .WithPngOutputScale(pngOutputScale)
            .WithMarkBackdrop(ChartMarkBackdrop.Card)
            .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always)
            .WithSvgColorVariables(tokens.ToSvgColorVariables())
            .WithStateCategories(states)
            .WithXLabels(checks);
        string[] sites = { "Warsaw", "Berlin", "Madrid", "Oslo" };
        for (var row = 0; row < sites.Length; row++) {
            var cells = new ChartHeatmapCell?[checks.Length];
            for (var column = 0; column < checks.Length; column++) {
                var state = states[(row * 3 + column * 5) % states.Length];
                cells[column] = new ChartHeatmapCell(state.Key, ((row * 7 + column * 3) % 12 + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            chart.AddHeatmapCategoryRow(sites[row], cells);
        }

        chart.SaveSvg(Path.Combine(output, "report-host-themed-status-counts.svg"));
        chart.SaveHtml(Path.Combine(output, "report-host-themed-status-counts.html"));
        chart.SavePng(Path.Combine(output, "report-host-themed-status-counts.png"));
    }

    private static void WriteThemedCalendar(string output, ChartPngOutputScale pngOutputScale) {
        // A dark calendar whose SVG writes token colours as CSS custom properties: ramp steps, zero and empty days are
        // color-mix() of the properties, so a host page switching themes recolours the same SVG.
        var tokens = VisualDesignTokens.Dark();
        var items = Enumerable.Range(0, 70).Select(day => new ChartCalendarHeatmapItem(new DateTime(2026, 7, 6).AddDays(day), day % 5 == 0 ? 0 : (day * 7) % 11)).ToArray();
        var chart = Chart.Create()
            .WithTitle("Changes per day")
            .WithSubtitle("SVG colours are the custom properties of the design tokens")
            .WithDesignTokens(tokens)
            .WithSize(760, 300)
            .WithPngOutputScale(pngOutputScale)
            .WithSvgColorVariables(tokens.ToSvgColorVariables())
            .AddCalendarHeatmap("Changes", items);

        chart.SaveSvg(Path.Combine(output, "report-host-themed-calendar.svg"));
        chart.SaveHtml(Path.Combine(output, "report-host-themed-calendar.html"));
        chart.SavePng(Path.Combine(output, "report-host-themed-calendar.png"));
    }

    private static void WriteFlatHistogram(string output, ChartPngOutputScale pngOutputScale) {
        // A histogram in a report card: flat bars are exactly the series colour, with no derived gradient or highlight.
        var latencies = Enumerable.Range(0, 400).Select(i => 18 + (i * 37 % 23) + (i * 11 % 7) * 1.5 + (i % 41 == 0 ? 28 : 0)).ToArray();
        var chart = Chart.Create()
            .WithTitle("LDAP bind latency")
            .WithSubtitle("Flat bars: one colour per bar")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(760, 360)
            .WithPngOutputScale(pngOutputScale)
            .WithBarStyle(ChartBarStyle.Flat)
            .WithXAxis("Latency (ms)")
            .WithYAxis("Samples")
            .AddHistogram("Samples", latencies, 14, ChartColor.FromHex("#2a78d6"));

        chart.SaveSvg(Path.Combine(output, "report-host-flat-histogram.svg"));
        chart.SaveHtml(Path.Combine(output, "report-host-flat-histogram.html"));
        chart.SavePng(Path.Combine(output, "report-host-flat-histogram.png"));
    }

    private static void WriteShortCardCalendar(string output, ChartPngOutputScale pngOutputScale) {
        // A quarter of changes in a 230 px card without an in-chart header: the calendar takes the padding it does not
        // need, and each day is named in the report's language instead of by its ISO date.
        var french = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
        var items = Enumerable.Range(0, 91)
            .Select(day => new ChartCalendarHeatmapItem(new DateTime(2026, 7, 1).AddDays(day), (day * 7 + day / 7) % 6 == 0 ? 0 : (day * 5) % 9))
            .ToArray();
        var chart = Chart.Create()
            .WithTitle("Changements par jour")
            .WithHeader(false)
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(760, 230)
            .WithPngOutputScale(pngOutputScale)
            .WithCalendarHeatmapCells(maximumSize: 18)
            .ConfigureLabels(labels => {
                labels.Less = "Moins";
                labels.More = "Plus";
                labels.NoData = "Aucune donnée";
                labels.DateFormatter = day => day.ToString("D", french);
            })
            .AddCalendarHeatmap("Changements", items, ChartColor.FromHex("#2a78d6"), french.DateTimeFormat.FirstDayOfWeek,
                french.DateTimeFormat.AbbreviatedDayNames, french.DateTimeFormat.AbbreviatedMonthNames.Take(12).ToArray());

        chart.SaveSvg(Path.Combine(output, "report-host-short-calendar.svg"));
        chart.SaveHtml(Path.Combine(output, "report-host-short-calendar.html"));
        chart.SavePng(Path.Combine(output, "report-host-short-calendar.png"));
    }

    private static void WriteRotatedStatusMatrix(string output, ChartPngOutputScale pngOutputScale) {
        // Twelve checks in a narrow card: the column labels rotate instead of shrinking and being cut off.
        string[] checks = {
            "Replication health", "SYSVOL sharing", "DNS scavenging", "Time source", "LDAP signing", "Kerberos armoring",
            "Certificate expiry", "Backup age", "Service accounts", "Disk space", "Event log size", "Secure channel"
        };
        var chart = Chart.Create()
            .WithTitle("Checks by domain controller")
            .WithSubtitle("Column labels follow the x-axis label angle")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 440)
            .WithPngOutputScale(pngOutputScale)
            .WithXAxisLabelAngle(-45)
            .WithStateCategories(
                new ChartStateCategory("pass", "Passed", ChartColor.FromHex("#1d8a52"), emphasis: ChartStateEmphasis.Quiet),
                new ChartStateCategory("medium", "Medium", ChartColor.FromHex("#c78404")),
                new ChartStateCategory("critical", "Critical", ChartColor.FromHex("#d4302f")),
                new ChartStateCategory("couldNotEvaluate", "Could not evaluate", ChartColor.FromHex("#7c818a"), ChartStatePattern.Outlined))
            .WithXLabels(checks);
        string[] states = { "pass", "pass", "medium", "pass", "critical", "pass", "couldNotEvaluate" };
        for (var row = 0; row < 5; row++) {
            var cells = new ChartHeatmapCell?[checks.Length];
            for (var column = 0; column < checks.Length; column++) cells[column] = new ChartHeatmapCell(states[(row * 5 + column * 3) % states.Length]);
            chart.AddHeatmapCategoryRow("DC0" + (row + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), cells);
        }

        chart.SaveSvg(Path.Combine(output, "report-host-rotated-matrix.svg"));
        chart.SaveHtml(Path.Combine(output, "report-host-rotated-matrix.html"));
        chart.SavePng(Path.Combine(output, "report-host-rotated-matrix.png"));
    }
}
