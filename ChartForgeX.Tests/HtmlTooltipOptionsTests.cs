using System.Text.RegularExpressions;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlTooltipOptionsTests {
    [Fact]
    public void SharedXIsTheDefaultForChartsAndDashboardChildren() {
        Assert.Equal(HtmlChartTooltipMode.SharedX, new HtmlChartInteractionOptions().Tooltip.Mode);
        Assert.Equal(HtmlChartTooltipMode.SharedX, new HtmlInteractiveDashboardOptions().Tooltip.Mode);
        Assert.Equal(HtmlChartTooltipRange.WithinDistance(120), new HtmlChartInteractionOptions().Tooltip.Range);
        Assert.Equal(HtmlChartTooltipRange.WithinDistance(120), new HtmlInteractiveDashboardOptions().Tooltip.Range);
        Assert.True(new HtmlChartInteractionOptions().Crosshair.ShowLabel);
        Assert.True(new HtmlInteractiveDashboardOptions().Crosshair.ShowLabel);
        AssertModes(Chart().ToInteractiveHtmlPage(), "shared-x", 1);
        AssertModes(new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(), "shared-x", 2);
    }

    [Theory]
    [InlineData(HtmlChartTooltipMode.Single, "single")]
    [InlineData(HtmlChartTooltipMode.SharedX, "shared-x")]
    public void ExplicitModeReachesPagesFragmentsAndEveryDashboardChild(HtmlChartTooltipMode mode, string serialized) {
        AssertModes(Chart().ToInteractiveHtmlPage(options => options.Tooltip.Mode = mode), serialized, 1);
        AssertModes(Chart().ToInteractiveHtmlFragment(options => options.Tooltip.Mode = mode), serialized, 1);
        AssertModes(Chart().ToInteractiveHtmlFragmentWithoutAssets(options => options.Tooltip.Mode = mode), serialized, 1);
        AssertModes(new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => options.Tooltip.Mode = mode), serialized, 2);
    }

    [Fact]
    public void InvalidModesFailAtEveryRenderBoundary() {
        var invalid = (HtmlChartTooltipMode)123;
        Assert.Equal("Mode", Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlPage(options => options.Tooltip.Mode = invalid)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlFragment(options => options.Tooltip.Mode = invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlFragmentWithoutAssets(options => options.Tooltip.Mode = invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => options.Tooltip.Mode = invalid));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DistanceRejectsUnusableCssPixelValues(double distance) {
        Assert.Equal("cssPixels", Assert.Throws<ArgumentOutOfRangeException>(() => HtmlChartTooltipRange.WithinDistance(distance)).ParamName);
    }

    [Fact]
    public void RangeAndLabelPolicyReachEveryRenderSurfaceWithInvariantDistances() {
        var culture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            foreach (var range in new[] { HtmlChartTooltipRange.Exact, HtmlChartTooltipRange.Nearest, HtmlChartTooltipRange.WithinDistance(0), HtmlChartTooltipRange.WithinDistance(48.5) }) {
                void Configure(HtmlChartInteractionOptions options) { options.Tooltip.Range = range; options.Crosshair.ShowLabel = false; }
                var surfaces = new[] {
                    Chart().ToInteractiveHtmlPage(Configure), Chart().ToInteractiveHtmlFragment(Configure), Chart().ToInteractiveHtmlFragmentWithoutAssets(Configure),
                    new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => { options.Tooltip.Range = range; options.Crosshair.ShowLabel = false; })
                };
                foreach (var html in surfaces) {
                    var roots = Regex.Matches(html, "<section[^>]*class=\"cfx-interactive-chart\"[^>]*>");
                    Assert.NotEmpty(roots.Cast<Match>());
                    Assert.All(roots.Cast<Match>(), root => {
                        Assert.Contains("data-cfx-crosshair-label=\"false\"", root.Value, StringComparison.Ordinal);
                        if (range == HtmlChartTooltipRange.Exact) Assert.Contains("data-cfx-tooltip-range=\"exact\"", root.Value, StringComparison.Ordinal);
                        else if (range == HtmlChartTooltipRange.Nearest) Assert.Contains("data-cfx-tooltip-range=\"nearest\"", root.Value, StringComparison.Ordinal);
                        else {
                            Assert.Contains("data-cfx-tooltip-range=\"distance\"", root.Value, StringComparison.Ordinal);
                            Assert.Contains("data-cfx-tooltip-distance=\"" + (range == HtmlChartTooltipRange.WithinDistance(0) ? "0" : "48.5") + "\"", root.Value, StringComparison.Ordinal);
                        }
                    });
                }
            }
        } finally { CultureInfo.CurrentCulture = culture; }
    }

    private static Chart Chart() => ChartForgeX.Core.Chart.Create().WithSize(520, 320).AddLine("Readings", ChartPoints.FromValues(1, 3, 2));

    private static void AssertModes(string html, string mode, int count) {
        var roots = Regex.Matches(html, "<section[^>]*class=\"cfx-interactive-chart\"[^>]*>");
        Assert.Equal(count, roots.Count);
        Assert.All(roots.Cast<Match>(), root => Assert.Contains("data-cfx-tooltip-mode=\"" + mode + "\"", root.Value, StringComparison.Ordinal));
    }
}
