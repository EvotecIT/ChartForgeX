using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void LineVisualStyleDefaultsStayCrispAndGlowIsOptIn() {
        var premiumSvg = Chart.Create()
            .WithSize(420, 260)
            .AddLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235))
            .ToSvg();
        Assert(!premiumSvg.Contains("stroke-width=\"13\"", StringComparison.Ordinal), "Default premium line styling should stay crisp instead of adding glow-heavy halo widths.");

        var plainSvg = Chart.Create()
            .WithSize(420, 260)
            .WithPlainLineStyle()
            .AddLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235))
            .ToSvg();
        Assert(!plainSvg.Contains("line-ambient-halo", StringComparison.Ordinal) && !plainSvg.Contains("line-halo", StringComparison.Ordinal) && !plainSvg.Contains("line-highlight", StringComparison.Ordinal), "Plain line style should render crisp single-stroke series lines.");

        var luminousSvg = Chart.Create()
            .WithSize(420, 260)
            .WithLuminousLineStyle()
            .AddLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235))
            .ToSvg();
        Assert(luminousSvg.Contains("stroke-width=\"13\"", StringComparison.Ordinal), "Luminous line style should keep glow-heavy line polish as an explicit opt-in.");
    }
}
