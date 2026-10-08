using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void WordCloudTermsRenderWeightedLayout() {
        var chart = Chart.Create()
            .WithSize(760, 430)
            .WithTheme(ChartTheme.Editorial())
            .AddWordCloud("Topics", new[] {
                new ChartWordCloudItem("Security", 100),
                new ChartWordCloudItem("Automation", 82),
                new ChartWordCloudItem("Reports", 64),
                new ChartWordCloudItem("Charts", 48),
                new ChartWordCloudItem("DNS", 32)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"word-cloud\"", StringComparison.Ordinal), "Word clouds should expose a chart role marker.");
        Assert(svg.Contains("data-cfx-edge-padding=", StringComparison.Ordinal), "Word clouds should expose the inner placement padding used to keep terms away from plot borders.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"word-cloud-term\"") == 5, "Word clouds should place each weighted term when there is enough room.");
        Assert(svg.Contains("data-cfx-text=\"Security\" data-cfx-value=\"100\"", StringComparison.Ordinal), "Word cloud terms should expose data metadata.");
        Assert(svg.Contains("font-size=\"46\"", StringComparison.Ordinal), "Word cloud terms should scale important words up.");
        Assert(chart.ToPng().Length > 64, "Word clouds should render PNG output.");

        var expressive = Chart.Create()
            .WithSize(760, 430)
            .WithWordCloudFontRange(18, 54)
            .WithWordCloudAngles(-30, 0, 30)
            .AddWordCloud("Topics", new[] {
                new ChartWordCloudItem("Security", 100),
                new ChartWordCloudItem("Automation", 50),
                new ChartWordCloudItem("Reports", 10)
            });
        var expressiveSvg = expressive.ToSvg();
        Assert(expressiveSvg.Contains("font-size=\"54\"", StringComparison.Ordinal), "Word cloud font range should control the largest term size.");
        Assert(expressiveSvg.Contains("font-size=\"18\"", StringComparison.Ordinal), "Word cloud font range should control the smallest term size.");
        Assert(expressiveSvg.Contains("rotate(-30", StringComparison.Ordinal), "Word cloud angles should control deterministic term rotation.");
        Assert(expressive.ToPng().Length > 64, "Word cloud customization should render PNG output.");

        var colored = Chart.Create()
            .WithSize(760, 430)
            .AddWordCloud("Topics", new[] {
                new ChartWordCloudItem("Security", 100, ChartColor.FromRgb(14, 165, 233)),
                new ChartWordCloudItem("Automation", 50, ChartColor.FromRgb(236, 72, 153)),
                new ChartWordCloudItem("Reports", 10)
            });
        var coloredSvg = colored.ToSvg();
        Assert(coloredSvg.Contains("data-cfx-text=\"Security\"", StringComparison.Ordinal) && coloredSvg.Contains("fill=\"#0EA5E9\"", StringComparison.Ordinal), "Word cloud term colors should override the theme palette.");
        Assert(coloredSvg.Contains("data-cfx-text=\"Automation\"", StringComparison.Ordinal) && coloredSvg.Contains("fill=\"#EC4899\"", StringComparison.Ordinal), "Word cloud term colors should apply per term.");
        Assert(colored.ToPng().Length > 64, "Word cloud term colors should render PNG output.");

        var curated = Chart.Create()
            .WithSize(760, 430)
            .WithWordCloudMaximumTerms(3)
            .WithWordCloudDensity(1.6)
            .AddWordCloud("Topics", new[] {
                new ChartWordCloudItem("Security", 100),
                new ChartWordCloudItem("Automation", 90),
                new ChartWordCloudItem("Reports", 80),
                new ChartWordCloudItem("Charts", 70),
                new ChartWordCloudItem("DNS", 60)
            });
        var curatedSvg = curated.ToSvg();
        Assert(curatedSvg.Contains("data-cfx-density=\"1.6\"", StringComparison.Ordinal), "Word cloud SVG output should expose configured layout density.");
        Assert(curatedSvg.Contains("data-cfx-maximum-terms=\"3\"", StringComparison.Ordinal), "Word cloud SVG output should expose configured term limits.");
        Assert(CountOccurrences(curatedSvg, "data-cfx-role=\"word-cloud-term\"") == 3, "Word cloud term limits should render only the highest-weight terms.");
        var curatedPrepared = PreparedFamily(curated);
        Assert(!System.Linq.Enumerable.Any(FamilyGroups(curatedPrepared, "word-cloud-term"), term => term.Metadata["data-cfx-text"] == "Charts"), "Word cloud term limits should omit lower-weight painted terms.");
        Assert(System.Linq.Enumerable.Any(FamilyGroups(curatedPrepared, "word-cloud-source"), term => term.Metadata["data-cfx-text"] == "Charts"), "Omitted word-cloud terms should remain available as source data.");
        Assert(curated.ToPng().Length > 64, "Word cloud term limits and density should render PNG output.");
    }
}
