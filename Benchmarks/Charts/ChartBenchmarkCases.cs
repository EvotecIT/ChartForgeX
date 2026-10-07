global using ChartForgeX.Primitives;
using System.Security.Cryptography;
using ChartForgeX.Core;
using ChartForgeX.Themes;

/// <summary>
/// Deterministic report charts shaped like the largest directory-monitoring report: a 60-lane status timeline with about
/// 1,300 periods, an 8-lane overview timeline, a two-series latency line and a two-week calendar heatmap. Every chart
/// carries host colour variables, as report hosts render them, so variable binding is part of the measured work.
/// </summary>
public static class ChartBenchmarkCases {
    private static readonly DateTime Start = new(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Creates a fresh chart before any measured operation.</summary>
    public static Chart Create(string fixture) => fixture switch {
        "timeline-60" => Timeline(60, 22, true),
        "timeline-8" => Timeline(8, 11, false),
        "latency" => Latency(),
        "calendar" => Calendar(),
        "cartesian" => Cartesian(),
        "donut" => Donut(),
        _ => throw new ArgumentOutOfRangeException(nameof(fixture))
    };

    /// <summary>Returns SHA-256 digests of the SVG and PNG outputs, so validation stays outside timing.</summary>
    public static string Digest(string? svg, byte[]? png) =>
        (svg == null ? "-" : Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(svg)))) + "/" +
        (png == null ? "-" : Convert.ToHexString(SHA256.HashData(png)));

    private static Chart Report(int width, int height) {
        var tokens = VisualDesignTokens.GraphiteLight();
        return Chart.Create().WithSize(width, height).WithTheme(tokens.ApplyTo(ChartTheme.Light()))
            .WithSvgColorVariables(tokens.ToSvgColorVariables(path => "--bench-" + path.Replace('.', '-')));
    }

    private static Chart Timeline(int lanes, int periods, bool grouped) {
        var chart = Report(1100, 64 + (lanes + (grouped ? 3 : 0)) * 26 + 44);
        var categories = new List<ChartStateCategory> {
            new("up", "Up", ChartColor.FromHex("#2F9E44"), ChartStatePattern.Solid, ChartStateEmphasis.Quiet),
            new("degraded", "Degraded", ChartColor.FromHex("#E8A317"), ChartStatePattern.Hatched),
            new("down", "Down", ChartColor.FromHex("#D6336C"), ChartStatePattern.CrossHatched),
            new("unknown", "Unknown", ChartColor.FromHex("#868E96"))
        };
        for (var share = 1; share <= 6; share++) categories.Add(new("down-" + share, "Down", ChartColor.FromHex("#D6336C")));
        chart.WithXAxisTimeScale(TimeZoneInfo.Utc, showTimeZone: true).WithStateCategories(categories).WithPadding(0, 10, 16, 0).WithLegend(false);
        chart.Options.LaneSummaryHeader = "Up";
        var random = new Random(20261007);
        string[] groups = { "AMER", "APAC", "EMEA" };
        for (var lane = 0; lane < lanes; lane++) {
            var segments = new List<ChartStateTimelineSegment>();
            var at = Start;
            for (var period = 0; period < periods; period++) {
                var hours = 2 + random.Next(14);
                var end = period == periods - 1 ? Start.AddDays(7) : at.AddHours(hours);
                if (end > Start.AddDays(7)) end = Start.AddDays(7);
                var roll = random.Next(10);
                var state = roll < 5 ? "up" : roll < 7 ? "degraded" : roll < 8 ? "down" : roll < 9 ? "down-" + (1 + random.Next(6)) : "unknown";
                segments.Add(new ChartStateTimelineSegment(at, end, state, (1 + random.Next(9)) + " of 16 probes were not up."));
                at = end;
                if (at >= Start.AddDays(7)) break;
            }

            chart.AddStateTimelineLane("Site " + lane.ToString("00", System.Globalization.CultureInfo.InvariantCulture), segments,
                (90 + random.NextDouble() * 10).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%",
                grouped ? groups[lane * groups.Length / lanes] : null);
        }

        return chart;
    }

    private static Chart Latency() {
        var chart = Report(1100, 360).WithXAxisTimeScale(TimeZoneInfo.Utc, showTimeZone: true).WithLegend();
        var median = new ChartPoint[96];
        var p95 = new ChartPoint[96];
        for (var i = 0; i < median.Length; i++) {
            var x = Start.AddHours(i * 1.75).ToOADate();
            median[i] = new ChartPoint(x, 11 + Math.Sin(i * 0.37) * 1.5);
            p95[i] = new ChartPoint(x, 24 + Math.Cos(i * 0.21) * 6);
        }

        return chart.AddLine("Median", median).AddLine("95th percentile", p95);
    }

    private static Chart Calendar() {
        var items = new List<ChartCalendarHeatmapItem>();
        for (var day = 0; day < 14; day++) items.Add(new ChartCalendarHeatmapItem(Start.AddDays(day), (day * 7) % 11));
        return Report(1100, 260).AddCalendarHeatmap("Changes", items);
    }

    private static Chart Cartesian() => Report(800, 440).WithTitle("Checks over the reporting period").WithLegend()
        .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
        .AddLine("Observed", Values(20, 23, 26, 29, 32, 35, 38))
        .AddArea("Expected", Values(34, 38, 42, 46, 50, 54, 35))
        .AddBar("Capacity", Values(48, 53, 58, 63, 68, 50, 55));

    private static Chart Donut() => Report(800, 440).WithTitle("Findings by category").WithLegend()
        .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
        .AddDonut("Findings", Values(20, 23, 26, 29, 32, 35, 38));

    private static ChartPoint[] Values(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
}
