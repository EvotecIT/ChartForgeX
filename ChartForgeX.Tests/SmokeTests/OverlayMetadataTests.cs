using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void OverlaySvgElementsExposeDataMetadata() {
        var annotations = Chart.Create()
            .WithSize(640, 360)
            .AddLine("Values", Points(42, 84, 126))
            .AddHorizontalLine(100, "target")
            .AddVerticalBand(1.5, 2.5, "window", opacity: 0.1)
            .ToSvg();
        Assert(annotations.Contains("data-cfx-role=\"annotation-line\" data-cfx-kind=\"horizontal-line\" data-cfx-value=\"100\" data-cfx-label=\"target\"", System.StringComparison.Ordinal), "Annotation lines should expose kind, value, and label metadata.");
        Assert(annotations.Contains("data-cfx-role=\"annotation-band\" data-cfx-kind=\"vertical-band\" data-cfx-value=\"1.5\" data-cfx-end=\"2.5\" data-cfx-label=\"window\"", System.StringComparison.Ordinal), "Annotation bands should expose kind, start, end, and label metadata.");
        Assert(annotations.Contains("data-cfx-role=\"annotation-label\" data-cfx-label=\"target\"", System.StringComparison.Ordinal), "Annotation label pills should expose label metadata.");
        Assert(annotations.Contains("data-cfx-role=\"annotation-label-text\" data-cfx-label=\"window\"", System.StringComparison.Ordinal), "Annotation label text should expose label metadata.");

        var secondary = Chart.Create()
            .WithSize(640, 360)
            .WithSecondaryYAxis("Rate", value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .WithSecondaryYAxisBounds(0, 100)
            .AddLine("Rate", Points(88, 93, 91));
        secondary.Series[0].UseSecondaryYAxis();
        var secondarySvg = secondary.ToSvg();
        Assert(secondarySvg.Contains("data-cfx-role=\"secondary-y-axis-tick\" data-cfx-value=\"100\"", System.StringComparison.Ordinal), "Secondary axis ticks should expose raw numeric values.");
        Assert(secondarySvg.Contains("data-cfx-role=\"secondary-y-axis-title\" data-cfx-label=\"Rate\"", System.StringComparison.Ordinal), "Secondary axis titles should expose the full configured label.");

        var legend = Chart.Create()
            .AddBar("Logged", Points(12, 18))
            .AddLine("Trend", Points(10, 20))
            .ToSvg();
        Assert(legend.Contains("data-cfx-role=\"legend-item\" data-cfx-series=\"0\" data-cfx-series-name=\"Logged\" data-cfx-series-key=\"Logged\" data-cfx-kind=\"Bar\" data-cfx-label=\"Logged\"", System.StringComparison.Ordinal), "Legend items should expose series, semantic identity, kind, and label metadata.");
        Assert(legend.Contains("data-cfx-role=\"legend-label\" data-cfx-series=\"1\" data-cfx-series-name=\"Trend\" data-cfx-series-key=\"Trend\"", System.StringComparison.Ordinal), "Legend labels should expose the associated series index and semantic identity.");
    }
}
