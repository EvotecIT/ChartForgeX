/// <summary>Builds one service-latency chart per axis option: fixed steps, explicit values, spacing, minor ticks and axis grids.</summary>
public static class AxisControlRecipe {
    /// <summary>Gets the option variants this recipe draws; each also has a compact form.</summary>
    public static readonly string[] Options = { "axis-step", "axis-values", "axis-spacing", "axis-minor", "axis-grid", "axis-time-step", "axis-log-step" };

    /// <summary>Creates the selected axis option example.</summary>
    public static Chart Create(string variant = "axis-step", VisualThemeMode mode = VisualThemeMode.Light) {
        var option = variant.StartsWith("compact-", StringComparison.Ordinal) ? variant.Substring(8) : variant;
        if (!Options.Contains(option)) throw new ArgumentOutOfRangeException(nameof(variant));
        if (option == "axis-time-step") return Weekly();
        if (option == "axis-log-step") return Payloads();
        // Twenty-four hourly p95 latency readings against an 80 ms objective.
        var hours = Enumerable.Range(0, 25).ToArray();
        var chart = Chart.Create().WithXAxis("Hour").WithYAxis("p95 latency (ms)").WithXAxisBounds(0, 24)
            .AddLine("API", hours.Select(hour => new ChartPoint(hour, Math.Round(44 + 22 * Math.Sin((hour - 6) / 24d * 2 * Math.PI) + hour % 5 * 2.5))).ToArray())
            .AddLine("Portal", hours.Select(hour => new ChartPoint(hour, Math.Round(31 + 15 * Math.Sin((hour - 9) / 24d * 2 * Math.PI) + hour % 3 * 3))).ToArray());
        switch (option) {
            case "axis-step":
                // Every six hours across, and every 15 ms up: the automatic value domain ends on the next multiple of 15.
                chart.ConfigureXAxis(axis => axis.WithTickStep(6)).ConfigureYAxis(axis => axis.WithTickStep(15));
                break;
            case "axis-values":
                // Shift changes across, and the latency budget's own thresholds up. The automatic value range starts at the zero
                // baseline and ends on the nearest authored value above the data, 80.
                chart.ConfigureXAxis(axis => axis.WithTickValues(0, 7, 15, 23))
                    .ConfigureYAxis(axis => axis.WithTickValues(20, 50, 80));
                break;
            case "axis-spacing":
                // Ticks at most 45 drawing units apart across, and at least 90 apart up.
                chart.ConfigureXAxis(axis => axis.WithTickSpacing(null, 45)).ConfigureYAxis(axis => axis.WithTickSpacing(90, null));
                break;
            case "axis-minor":
                // Four minor lines per 20 ms interval up; one per three-hour interval across, with the vertical grid shown.
                chart.ConfigureYAxis(axis => axis.WithTickStep(20).WithMinorTicks(4))
                    .ConfigureXAxis(axis => axis.WithTickStep(3).WithMinorTicks(2).ConfigureGrid(grid => grid.WithLines(true)));
                break;
            case "axis-grid":
                // Dashed teal value lines over neutral alternating bands; solid, faint hour lines across.
                var teal = ChartColor.FromHex("#2E8B8B");
                chart.ConfigureYAxis(axis => axis.WithTickStep(20).ConfigureGrid(grid => grid.WithColor(teal).WithOpacity(.55).WithDash(5, 4).WithBands()))
                    .ConfigureXAxis(axis => axis.WithTickStep(4).ConfigureGrid(grid => grid.WithLines(true).WithOpacity(.25).WithStrokeWidth(.75)));
                break;
        }
        return chart;
    }

    // Ninety days of daily deployments with a calendar tick on every second Monday and a minor line on the Monday between.
    private static Chart Weekly() {
        var start = new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
        var days = Enumerable.Range(0, 90).Select(day => new ChartPoint(start.AddDays(day).ToOADate(), 12 + day % 7 * 3 + Math.Round(8 * Math.Sin(day / 9d)))).ToArray();
        return Chart.Create().WithYAxis("Deployments").AddLine("Deployments", days)
            .ConfigureXAxis(axis => axis.WithTimeScale().WithTickStep(new ChartTimeTickInterval(ChartTimeTickUnit.Week, 2))
                .WithMinorTicks(1).ConfigureGrid(grid => grid.WithLines(true).WithOpacity(.8)));
    }

    // Payload sizes across five orders of magnitude on a logarithmic axis, marked on every second power of ten.
    // The automatic range ends on the stepped powers around the data: 0.01 and 1M.
    private static Chart Payloads() {
        var sizes = new[] { 0.8, 3.5, 12, 48, 260, 900, 4_100, 18_000, 72_000 };
        return Chart.Create().WithXAxis("Request").WithYAxis("Payload (KB)")
            .AddLine("Payload", sizes.Select((size, index) => new ChartPoint(index + 1, size)).ToArray())
            .ConfigureYAxis(axis => axis.WithLogarithmicScale().WithTickStep(2));
    }
}
