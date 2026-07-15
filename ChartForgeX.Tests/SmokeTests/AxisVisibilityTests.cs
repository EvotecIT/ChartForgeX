using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void AxisVisibilityCanBeConfiguredIndependently() {
        var horizontal = Chart.Create()
            .WithSize(520, 320)
            .WithXAxis("Score")
            .WithXAxisVisible(false)
            .WithAxisLines(false)
            .WithLegend(false)
            .WithXLabels("Awareness", "Preference")
            .AddHorizontalBar("Metric", Points(82, 64));
        var horizontalSvg = horizontal.ToSvg();
        Assert(horizontal.Options.ShowAxes && !horizontal.Options.ShowXAxis && horizontal.Options.ShowYAxis && !horizontal.Options.ShowAxisLines, "Axis visibility should support hiding one axis while keeping the other and suppressing axis rules separately.");
        Assert(horizontalSvg.Contains(">Awareness</text>", StringComparison.Ordinal), "Hiding the x-axis should keep horizontal bar category labels visible.");
        Assert(!horizontalSvg.Contains(">Score</text>", StringComparison.Ordinal), "Hiding the x-axis should suppress x-axis titles.");
        Assert(horizontal.ToPng().Length > 64, "Independent axis visibility should render PNG output.");

        var vertical = Chart.Create()
            .WithSize(520, 320)
            .WithYAxis("Count")
            .WithYAxisVisible(false)
            .AddBar("Values", Points(12, 24));
        var verticalSvg = vertical.ToSvg();
        Assert(!verticalSvg.Contains(">Count</text>", StringComparison.Ordinal), "Hiding the y-axis should suppress y-axis titles.");
        Assert(verticalSvg.Contains("text-anchor=\"middle\"", StringComparison.Ordinal), "Hiding the y-axis should keep x-axis labels visible.");
        Assert(vertical.ToPng().Length > 64, "Independent y-axis visibility should render PNG output.");

        var theme = ChartTheme.ReportLight();
        theme.Axis = ChartColor.FromHex("#FF00FF");
        var independentRules = Chart.Create()
            .WithSize(360, 220)
            .WithTheme(theme)
            .WithGrid(false)
            .WithLegend(false)
            .AddLine("Values", Points(12, 24));
        independentRules.Options.XAxis.ShowLine = false;
        independentRules.Options.YAxis.ShowLine = true;
        var independentRulesSvg = independentRules.ToSvg();
        Assert(CountOccurrences(independentRulesSvg, "stroke=\"#FF00FF\"") == 1, "Disabling one axis rule should not suppress independently enabled axis rules.");
        Assert(independentRules.ToPng().Length > 64, "Independent SVG and PNG axis-rule visibility should stay aligned.");

        var horizontalRules = Chart.Create()
            .WithSize(420, 260)
            .WithTheme(theme)
            .WithGrid(false)
            .WithLegend(false)
            .WithXAxisVisible(false)
            .AddHorizontalBar("Change", Points(-40, 40));
        horizontalRules.Options.XAxis.ShowLine = false;
        horizontalRules.Options.YAxis.ShowLine = true;
        var horizontalRulePixels = ReadPngRgba(horizontalRules.ToPng(), out var horizontalRuleWidth, out _);
        var horizontalRuleBounds = FindNearColorBounds(horizontalRulePixels, horizontalRuleWidth, 255, 0, 255, 4);
        Assert(!horizontalRuleBounds.IsEmpty && horizontalRuleBounds.Right - horizontalRuleBounds.Left > 80, "PNG horizontal zero lines should follow the visible y-axis even when x-axis labels are hidden.");

        var hiddenSecondaryTheme = ChartTheme.ReportLight();
        hiddenSecondaryTheme.Axis = ChartColor.FromHex("#FF00FF");
        hiddenSecondaryTheme.MutedText = ChartColor.FromHex("#FF00FF");
        var hiddenSecondary = Chart.Create()
            .WithSize(420, 240)
            .WithTheme(hiddenSecondaryTheme)
            .WithGrid(false)
            .WithLegend(false)
            .WithXAxisVisible(false)
            .WithYAxisVisible(false)
            .WithSecondaryYAxis("Rate")
            .AddLine("Primary", Points(2, 4))
            .AddLine("Secondary", Points(20, 40));
        hiddenSecondary.Series[1].UseSecondaryYAxis();
        hiddenSecondary.Options.SecondaryYAxis.Visible = false;
        var hiddenSecondarySvg = hiddenSecondary.ToSvg();
        Assert(!hiddenSecondarySvg.Contains("secondary-y-axis", StringComparison.Ordinal), "A hidden secondary axis should suppress its rule, ticks, title, and layout reserve.");
        var hiddenSecondaryPixels = ReadPngRgba(hiddenSecondary.ToPng(), out var hiddenSecondaryWidth, out _);
        Assert(FindNearColorBounds(hiddenSecondaryPixels, hiddenSecondaryWidth, 255, 0, 255, 4).IsEmpty, "A hidden secondary axis should suppress its PNG rule, labels, and title.");
    }
}
