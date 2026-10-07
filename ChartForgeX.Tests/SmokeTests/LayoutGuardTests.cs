using System;
using System.Globalization;
using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void LegendsAndDataLabelsKeepGuardGaps() {
        var topLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(520, 320)
            .WithLegendPosition(ChartLegendPosition.Top)
            .AddLine("First long legend item", Points(12, 18, 26))
            .AddLine("Second long legend item", Points(8, 14, 22))
            .AddLine("Third long legend item", Points(4, 9, 18));
        var topLegendSvg = topLegend.ToSvg();
        var topPlotY = GetAttribute(topLegendSvg, "plotClip\"><rect", "y");
        var topLegendRowY = GetTranslateY(topLegendSvg, "data-cfx-role=\"legend-row\"");
        Assert(topPlotY - topLegendRowY >= 12, "Top legends should reserve a visible gap before the plot guard.");
        Assert(topLegend.ToPng().Length > 64, "Top legend guard gaps should render PNG output.");

        var bottomLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(520, 320)
            .WithLegendPosition(ChartLegendPosition.Bottom)
            .AddLine("First long legend item", Points(12, 18, 26))
            .AddLine("Second long legend item", Points(8, 14, 22))
            .AddLine("Third long legend item", Points(4, 9, 18));
        var bottomLegendSvg = bottomLegend.ToSvg();
        var bottomLegendRowY = GetTranslateY(bottomLegendSvg, "data-cfx-role=\"legend-row\"");
        Assert(bottomLegendRowY <= 320 - 24, "Bottom legends should stay inside the card guard instead of sitting on the canvas edge.");
        Assert(bottomLegend.ToPng().Length > 64, "Bottom legend guard gaps should render PNG output.");

        var radialLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(520, 320)
            .WithLegendPosition(ChartLegendPosition.Bottom)
            .WithRadialBarCenterLabel(false)
            .AddRadialBar("Readiness", Points(82, 61, 44));
        var radialLegendSvg = radialLegend.ToSvg();
        var radialLegendY = GetAttribute(radialLegendSvg, "data-cfx-role=\"radial-bar-legend-label\"", "y");
        Assert(radialLegendY <= 320 - 24, "Bottom radial-bar legends should stay inside the card guard instead of sitting on the canvas edge.");
        Assert(radialLegend.ToPng().Length > 64, "Bottom radial-bar legend guard gaps should render PNG output.");

        var edgeLabels = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithDataLabels()
            .WithYAxisBounds(0, 100)
            .AddBar("Edge", Points(100));
        var edgeSvg = edgeLabels.ToSvg();
        var plotTop = GetAttribute(edgeSvg, "plotClip\"><rect", "y");
        var plotBottom = plotTop + GetAttribute(edgeSvg, "plotClip\"><rect", "height");
        var labelY = GetAttribute(edgeSvg, "data-cfx-role=\"data-label\"", "y");
        Assert(labelY - plotTop >= 12 && plotBottom - labelY >= 12, "Data labels should be clamped away from plot guard lines.");
        Assert(edgeLabels.ToPng().Length > 64, "Data-label guard gaps should render PNG output.");
    }

    private static double GetTranslateY(string text, string marker) {
        var start = text.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Missing marker: " + marker);
        start = text.IndexOf("translate(", start, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Missing transform for marker: " + marker);
        start += "translate(".Length;
        var separator = text.IndexOf(" ", start, StringComparison.Ordinal);
        if (separator < 0) throw new InvalidOperationException("Missing transform separator for marker: " + marker);
        var end = text.IndexOf(")", separator, StringComparison.Ordinal);
        if (end < 0) throw new InvalidOperationException("Missing transform end for marker: " + marker);
        return double.Parse(text.Substring(separator + 1, end - separator - 1), CultureInfo.InvariantCulture);
    }
}
