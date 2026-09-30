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
