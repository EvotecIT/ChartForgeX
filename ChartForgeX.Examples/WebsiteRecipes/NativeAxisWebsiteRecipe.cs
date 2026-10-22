#region imports
using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
#endregion

/// <summary>Ordinary axis examples keep source signs and UTC instants while selecting an explicit native projection.</summary>
public static class NativeAxisWebsiteRecipe {
    /// <summary>Creates a positive/negative logarithmic, ordinal or calendar-month sample.</summary>
    public static Chart Create(string variant, VisualThemeMode mode = VisualThemeMode.Light) => CreateChart(variant, mode);

    /// <summary>Writes the same four source-backed examples shown in the API guide.</summary>
    public static void Save(VisualThemeMode mode = VisualThemeMode.Light) {
#region views
        foreach (var variant in new[] { "positive-log", "negative-log", "ordinal", "calendar-month" }) {
            var chart = CreateChart(variant, mode);
            chart.SaveSvg("axis-" + variant + ".svg");
            chart.SavePng("axis-" + variant + ".png");
        }
#endregion
    }

#region helpers
    static Chart CreateChart(string variant, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(760, 440).WithThemeMode(mode).WithLegend(false);
        if (variant is "positive-log" or "negative-log") {
            var negative = variant == "negative-log";
            var magnitudes = new[] { 1d, 8, 64 };
            var values = negative ? magnitudes.Reverse().Select(value => -value) : magnitudes.AsEnumerable();
            return chart.WithTitle(negative ? "Negative logarithmic domain" : "Positive logarithmic domain")
                .WithSubtitle("Base two; the authored sign remains part of the data")
                .AddLine(negative ? "Loss" : "Capacity", values.Select((value, index) => new ChartPoint(index + 1, value)))
                .ConfigureYAxis(axis => axis.WithLogarithmicScale(2, negative ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive)
                    .WithBounds(negative ? -64 : 1, negative ? -1 : 64));
        }
        if (variant is not ("ordinal" or "calendar-month")) throw new ArgumentOutOfRangeException(nameof(variant));
        return chart.WithTitle(variant == "ordinal" ? "Ordinal observations" : "Calendar-month bands")
            .WithSubtitle(variant == "ordinal" ? "January and March are adjacent supplied observations" : "An empty February band remains visible")
            .AddBar("Orders", new[] {
                new ChartPoint(new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), 18),
                new ChartPoint(new DateTime(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc), 27)
            })
            .ConfigureXAxis(axis => axis.WithTimeScale(TimeZoneInfo.Utc).WithTimeSpacing(variant == "ordinal"
                ? ChartTimeSpacing.Ordinal : ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month))));
    }
#endregion
}
