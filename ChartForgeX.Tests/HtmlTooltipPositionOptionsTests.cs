using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlTooltipPositionOptionsTests {
    [Fact]
    public void DefaultsAndOptionGroupsRemainIndependent() {
        var chart = new HtmlChartInteractionOptions().Tooltip.Position;
        var dashboard = new HtmlInteractiveDashboardOptions().Tooltip.Position;
        Assert.NotSame(chart, dashboard);
        Assert.Equal(HtmlChartTooltipAnchor.Pointer, chart.Anchor);
        Assert.Equal(new[] { HtmlChartTooltipPlacement.BottomRight }, chart.Placements);
        Assert.Equal(14, chart.Gap);
        Assert.Equal(0, chart.OffsetX);
        Assert.Equal(0, chart.OffsetY);
        chart.Anchor = HtmlChartTooltipAnchor.Node;
        chart.Gap = 0;
        Assert.Equal(HtmlChartTooltipAnchor.Pointer, dashboard.Anchor);
        Assert.Equal(14, dashboard.Gap);
    }

    [Fact]
    public void PlacementAssignmentCopiesOrderAndRejectsUnusableSequencesAtomically() {
        var options = new HtmlChartTooltipPositionOptions();
        var directions = new List<HtmlChartTooltipPlacement> { HtmlChartTooltipPlacement.Left, HtmlChartTooltipPlacement.Top, HtmlChartTooltipPlacement.Left };
        options.Placements = directions;
        directions[0] = HtmlChartTooltipPlacement.Right;
        directions.Clear();
        var expected = new[] { HtmlChartTooltipPlacement.Left, HtmlChartTooltipPlacement.Top, HtmlChartTooltipPlacement.Left };
        Assert.Equal(expected, options.Placements);
        Assert.Throws<NotSupportedException>(() => ((IList)options.Placements)[0] = HtmlChartTooltipPlacement.Right);
        Assert.Throws<ArgumentNullException>(() => options.Placements = null!);
        Assert.Throws<ArgumentException>(() => options.Placements = Array.Empty<HtmlChartTooltipPlacement>());
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Placements = new[] { HtmlChartTooltipPlacement.Bottom, (HtmlChartTooltipPlacement)500 });
        Assert.Equal(expected, options.Placements);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Anchor = (HtmlChartTooltipAnchor)500);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PositionRejectsNonFinitePixels(double value) {
        var options = new HtmlChartTooltipPositionOptions();
        Assert.Equal("Gap", Assert.Throws<ArgumentOutOfRangeException>(() => options.Gap = value).ParamName);
        Assert.Equal("OffsetX", Assert.Throws<ArgumentOutOfRangeException>(() => options.OffsetX = value).ParamName);
        Assert.Equal("OffsetY", Assert.Throws<ArgumentOutOfRangeException>(() => options.OffsetY = value).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Gap = -1);
        options.Gap = 0;
        options.OffsetX = -8;
        options.OffsetY = -16;
        Assert.Equal(0, options.Gap);
        Assert.Equal(-8, options.OffsetX);
        Assert.Equal(-16, options.OffsetY);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Pointer, "pointer")]
    [InlineData(HtmlChartTooltipAnchor.Node, "node")]
    [InlineData(HtmlChartTooltipAnchor.Chart, "chart")]
    public void PositionReachesAllHtmlSurfacesAndEveryDashboardChild(HtmlChartTooltipAnchor anchor, string serialized) {
        var culture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            void Configure(HtmlChartTooltipPositionOptions position) {
                position.Anchor = anchor;
                position.Placements = Enum.GetValues<HtmlChartTooltipPlacement>().AsEnumerable().Reverse().ToArray();
                position.Gap = 8.5;
                position.OffsetX = -4.25;
                position.OffsetY = 6.75;
            }
            void ConfigureChart(HtmlChartInteractionOptions options) { Configure(options.Tooltip.Position); options.Interaction.Features = ChartInteractionFeatures.None; }
            var surfaces = new[] {
                Chart().ToInteractiveHtmlPage(ConfigureChart), Chart().ToInteractiveHtmlFragment(ConfigureChart), Chart().ToInteractiveHtmlFragmentWithoutAssets(ConfigureChart),
                new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => { Configure(options.Tooltip.Position); options.Interaction.Features = ChartInteractionFeatures.None; })
            };
            foreach (var html in surfaces) {
                var roots = Regex.Matches(html, "<section[^>]*class=\"cfx-interactive-chart\"[^>]*>");
                Assert.NotEmpty(roots.Cast<Match>());
                Assert.All(roots.Cast<Match>(), root => {
                    Assert.Contains("data-cfx-tooltip-anchor=\"" + serialized + "\"", root.Value, StringComparison.Ordinal);
                    Assert.Contains("data-cfx-tooltip-placements=\"top-left,left,bottom-left,bottom,bottom-right,right,top-right,top\"", root.Value, StringComparison.Ordinal);
                    Assert.Contains("data-cfx-tooltip-gap=\"8.5\"", root.Value, StringComparison.Ordinal);
                    Assert.Contains("data-cfx-tooltip-offset-x=\"-4.25\"", root.Value, StringComparison.Ordinal);
                    Assert.Contains("data-cfx-tooltip-offset-y=\"6.75\"", root.Value, StringComparison.Ordinal);
                    Assert.Contains("data-cfx-interaction-features=\"None\"", root.Value, StringComparison.Ordinal);
                });
            }
        } finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public void HtmlPositioningLeavesNativeExportsUnchanged() {
        var chart = Chart();
        var svg = chart.ToSvg();
        var png = chart.ToPng();
        chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Chart;
            options.Tooltip.Position.Placements = new[] { HtmlChartTooltipPlacement.Top, HtmlChartTooltipPlacement.Bottom };
            options.Tooltip.Position.Gap = 12;
            options.Tooltip.Position.OffsetX = -8;
            options.Tooltip.Position.OffsetY = 4;
        });
        Assert.Equal(svg, chart.ToSvg());
        Assert.Equal(png, chart.ToPng());
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "native-position-default.svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, "native-position-default.png"), png);
    }

    private static Chart Chart() => ChartForgeX.Core.Chart.Create().WithSize(520, 320).AddLine("Readings", ChartPoints.FromValues(1, 3, 2));
}
